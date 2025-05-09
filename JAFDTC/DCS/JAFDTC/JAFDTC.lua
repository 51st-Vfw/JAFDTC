--[[
********************************************************************************************************************

JAFDTC.lua -- dcs export handlers for jafdtc

Copyright(C) 2023-2025 ilominar/raven

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General
Public License as published by the Free Software Foundation, either version 3 of the License, or (at your
option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the
implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
for more details.

You should have received a copy of the GNU General Public License along with this program.  If not, see
<https://www.gnu.org/licenses/>.

********************************************************************************************************************
--]]

package.path  = package.path .. ";" .. lfs.currentdir() .. "/LuaSocket/?.lua"
package.cpath = package.cpath .. ";" .. lfs.currentdir() .. "/LuaSocket/?.dll"
package.path  = package.path .. ";" .. lfs.currentdir() .. "/Scripts/?.lua"

JAFDTC_LogFile = io.open(lfs.writedir() .. [[Logs\JAFDTC.log]], "w")

local socket = require("socket")
local JSON = loadfile("Scripts\\JSON.lua")()

dofile(lfs.writedir() .. 'Scripts/JAFDTC/CommonFunctions.lua')
dofile(lfs.writedir() .. 'Scripts/JAFDTC/A10CFunctions.lua')
dofile(lfs.writedir() .. 'Scripts/JAFDTC/AV8BFunctions.lua')
dofile(lfs.writedir() .. 'Scripts/JAFDTC/F14ABFunctions.lua')
dofile(lfs.writedir() .. 'Scripts/JAFDTC/F15EFunctions.lua')
dofile(lfs.writedir() .. 'Scripts/JAFDTC/F16CFunctions.lua')
dofile(lfs.writedir() .. 'Scripts/JAFDTC/FA18CFunctions.lua')
dofile(lfs.writedir() .. 'Scripts/JAFDTC/M2000CFunctions.lua')

-- --------------------------------------------------------------------------------------------------------------------
--
-- locals
--
-- --------------------------------------------------------------------------------------------------------------------

local cmdResumeTime = 0.0
local cmdList = nil
local cmdListIndex = 1
local cmdCurCort = nil
local cmdCurProgress = 0.0

local whileTout = { }

local markerVal = ""
local responseVal = ""

local tcpCmdServerSock = nil
local tcpCmdServerPort = 42001

local udpTelemSockTx = nil
local udpTelemPortTx = 42002

local upstreamLuaExportStart = LuaExportStart
local upstreamLuaExportAfterNextFrame = LuaExportAfterNextFrame
local upstreamLuaExportBeforeNextFrame = LuaExportBeforeNextFrame

-- --------------------------------------------------------------------------------------------------------------------
--
-- core functions (global)
--
-- --------------------------------------------------------------------------------------------------------------------

-- core function to perform clickable action. expected to be called from main processing loop as it will yield
-- the current co-routine to handle the down-up delay.
--
function JAFDTC_Core_PerformAction(dev, code, valDn, valUp, dt)
    GetDevice(dev):performClickableAction(code, valDn)
    coroutine.yield(0, dt)
    if valDn ~= valUp then
        GetDevice(dev):performClickableAction(code, valUp)
    end
end

-- core function to perform wait by yielding. expected to be called from main processing loop.
--
function JAFDTC_Core_Wait(dt)
    coroutine.yield(0, dt)
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- utility
--
-- --------------------------------------------------------------------------------------------------------------------

local function JAFDTC_CallUpstream(fn, what)
    if fn then
        local success, retVal = pcall(fn)
        if not success then
            JAFDTC_Log("JAFDTC", log.ERROR, "ERROR: Upstream export " .. what .. " failed")
        end
    end
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- sockets
--
-- --------------------------------------------------------------------------------------------------------------------

-- returns new tcp socket, use pcall() to catch any errors thrown on failures
--
local function JAFDTC_TCPServerSockOpen(port)
    local sock, retVal, err = nil, nil, nil

    sock, err = socket.tcp()
    if not sock then error(err) end

    sock:settimeout(0)

    retVal, err = sock:bind("127.0.0.1", port)
    if not retVal then error(err) end

    retVal, err = sock:listen(1)
    if not retVal then error(err) end

    return sock
end

local function JAFDTC_TCPServerSockRx(sock)
    local client, data, err = nil, nil, nil

    client, err = sock:accept()
    if client then
        client:settimeout(10)
        data, err = client:receive()
        if not err then
            return data
        end
        JAFDTC_Log("ERROR: TCP socket rx failed: " .. tostring(err))
    end
    return nil
end

-- returns new udp socket, use pcall() to catch any errors thrown on failures
--
local function JAFDTC_UDPSockOpen()
    local sock, err = nil, nil
    
    sock, err = socket.udp()
    if not sock then error(err) end

    sock:settimeout(0)

    return sock
end

local function JAFDTC_UDPSockTx(sock, port, data)
    local retVal, err = sock:sendto(data, "127.0.0.1", port)
    if not retVal then
        JAFDTC_Log("ERROR: UDP socket tx failed: " .. tostring(err))
    end
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- command processing
--
-- --------------------------------------------------------------------------------------------------------------------

-- command processing functions take an array of commands (table) and index within the array of the current command.
-- each command array element has a "f" key with the processor name (string) and an "a" key with a table of
-- arguments to the processor. processors return a (di, dt) tuple indicating the amount to advance the index to
-- reach the next command and an optional duration (in ms) to pause before the next command. a di of 0 incidates the
-- processor is not finished.

local function JAFDTC_Debug_DumpTable(tbl)

    -- code set up to handle arrays as that's typically the kinds of tables we'll find in commands.

    local output = "[ "
    local separator = ""
    local i = 0
    for _ in pairs(tbl) do
        i = i + 1
        if tbl[i] == nil then
            return "{ <???> }"
        elseif type(tbl[i]) == "number" then
            output = output .. separator .. tbl[i]
        elseif type(tbl[i]) == "string" then
            output = output .. separator .. "\"" .. tbl[i] .. "\""
        else
            output = output .. separator .. "<???>"
        end
        separator = ", "
    end
    return output .. " ]"
end

local function JAFDTC_Debug_DumpCmdArgs(list, index)
    local args = list[index]["a"]
    local output = "( "
    local separator = ""
    for k, v in pairs(args) do
        if type(k) == "number" then
            output = output .. separator .. "[" .. k .. "] = "
        elseif type(k) == "string" then
            output = output .. separator .. k .. " = "
        else
            output = output .. separator .. "???"
        end
        if type(v) == "number" then
            output = output .. v
        elseif type(v) == "string" then
            output = output .. "\"" .. v .. "\""
        elseif type(v) == "table" then
            output = output .. JAFDTC_Debug_DumpTable(v)
        else
            output = output .. "<???>"
        end
        separator = ", "
    end
    return output .. " )"
end

-- Abort(string msg)
--
-- abort execution by skipping all remaining commands in the command list. returns the msg to jafdtc through the
-- Marker field of the telemetry.
--
function JAFDTC_Cmd_Abort(list, index)
    local args = list[index]["a"]

    markerVal = args["msg"]
    return (#list - index + 1), 0
end

-- Actn(number dev, number code, number dn, number up = 0, number dt = 0)
--
-- perform the clickable action on device:code. the action sets the state to dn, waits dt ms, then sets the state to
-- up. if up is not specified, it is assumed to be the same as dn.
--
function JAFDTC_Cmd_Actn(list, index)
    local args = list[index]["a"]
    local dev, code, valDn = tostring(args["dev"]), tostring(args["code"]), args["dn"]
    local valUp = args["up"] or 0
    local dt = args["dt"] or 0

    JAFDTC_Core_PerformAction(dev, code, valDn, valUp, dt)
    return 1, 0
end

-- Query(string fn, list<string> prm = nil)
--
-- invokes the function JAFDTC_<airframe>_Fn_<fn> with the specified parameters (list of strings). the function must
-- return a string. the return value is set as the responseVal in the next telemetry update.
--
function JAFDTC_Cmd_Query(list, index)
    local args = list[index]["a"]
    local fn = args["fn"]
    local fnPrm = args["prm"] or { }

    local fnName = "JAFDTC_" .. JAFDTC_GetPlayerAircraftType() .. "_Fn_" .. fn;
    responseVal = _G[fnName](unpack(fnPrm))
    JAFDTC_Log(string.format("[%.3f] Query returns '%s'", socket.gettime(), tostring(responseVal)))
    return 1, 0
end

-- Exec(string fn, list<string> prm = nil)
--
-- invokes the function JAFDTC_<airframe>_Fn_<fn> with the specified parameters (list of strings). any return value
-- from the function is discarded.
--
function JAFDTC_Cmd_Exec(list, index)
    local args = list[index]["a"]
    local fn = args["fn"]
    local fnPrm = args["prm"] or { }

    local fnName = "JAFDTC_" .. JAFDTC_GetPlayerAircraftType() .. "_Fn_" .. fn;
    _G[fnName](unpack(fnPrm))
    return 1, 0
end

-- If(string cond, string expt, list<string> prm = nil)
--
-- performs an "if" block conditional on the function JAFDTC_<airframe>_Fn_<cond> with the specified parameters (list
-- of strings). this function must return a boolean. if the return value does not match the expected value, expt,
-- skip ahead to the next EndIf block in the command list with a matching cond.
--
function JAFDTC_Cmd_If(list, index)
    local args = list[index]["a"]
    local cond = args["cond"]
    local expt = (string.lower(args["expt"]) == "true")
    local fnPrm = args["prm"] or { }
    local di = 1

    local fnName = "JAFDTC_" .. JAFDTC_GetPlayerAircraftType() .. "_Fn_" .. cond;
    if _G[fnName](unpack(fnPrm)) ~= expt then
        for i = index + 1, #list do
            if list[i]["f"] == "EndIf" and list[i]["a"]["cond"] == cond then
                return i - index, 0
            end
        end
        JAFDTC_Log("ERROR: Missing closing EndIf for "..cond)
        di = #list - index + 1
    end
    return di, 0
end

-- EndIf(string cond)
--
-- marks end of an "if" block.
--
function JAFDTC_Cmd_EndIf(list, index)
    return 1, 0
end

-- While(string cond, string expt, number tout = 50, list<string> prm = nil)
--
-- performs a "while" block conditional on the function JAFDTC_<airframe>_Fn_<cond> with the specified parameters
-- (list of strings). this function must return a boolean. if this return value does not match the expected value,
-- expt, skip ahead to the next EndWhile block in the command list with a matching cond. the while loop will iterate
-- for at most tout iterations.
--
function JAFDTC_Cmd_While(list, index)
    local args = list[index]["a"]
    local cond = args["cond"]
    local expt = (string.lower(args["expt"]) == "true")
    local tout = args["tout"] or 50
    local fnPrm = args["prm"] or { }
    local di = 1

    if whileTout[index] == nil then
        whileTout[index] = tout
    else
        whileTout[index] = whileTout[index] - 1
    end

    local fnName = "JAFDTC_" .. JAFDTC_GetPlayerAircraftType() .. "_Fn_" .. cond;
    if _G[fnName](unpack(fnPrm)) ~= expt or whileTout[index] == 0 then
        if whileTout[index] == 0 then
            JAFDTC_Log("ERROR: Timeout skips to EndWhile of While for " .. cond)
        end
        for i = index + 1, #list do
            if list[i]["f"] == "EndWhile" and list[i]["a"]["cond"] == cond then
                return (i + 1) - index, 0
            end
        end
        JAFDTC_Log("ERROR: Missing closing EndWhile for " .. cond)
        di = #list - index + 1
    end
    return di, 0
end

-- EndWhile(string cond)
--
-- marks end of a "while" block.
--
function JAFDTC_Cmd_EndWhile(list, index)
    local args = list[index]["a"]
    local cond = args["cond"]

    for i = index - 1, 1, -1 do
        if list[i]["f"] == "While" and list[i]["a"]["cond"] == cond then
            return i - index, 0
        end
    end
    JAFDTC_Log("ERROR: Missing opening While for " .. cond)
    return 1, 0
end

-- Marker(string mark)
--
-- emit the string mark in the Marker field of the telemetry.
--
function JAFDTC_Cmd_Marker(list, index)
    local args = list[index]["a"]
    markerVal = args["mark"]
    return 1, 0
end

-- Wait(number dt)
--
-- pauses for the dt ms before executing the next command.
--
function JAFDTC_Cmd_Wait(list, index)
    local args = list[index]["a"]
    JAFDTC_Core_Wait(args["dt"])
    return 1, 0
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- dcs export hooks
--
-- --------------------------------------------------------------------------------------------------------------------

function LuaExportStart()
    JAFDTC_CallUpstream(upstreamLuaExportStart, "start")
    
    local status
    status, udpTelemSockTx = pcall(JAFDTC_UDPSockOpen)
    if not status then
        cmdResumeTime = socket.gettime() + 3600000
        udpTelemSockTx = nil
        JAFDTC_Log("ERROR: Unable to open UDP tx socket")
    end

    status, tcpCmdServerSock = pcall(JAFDTC_TCPServerSockOpen, tcpCmdServerPort)
    if not status then
        cmdResumeTime = socket.gettime() + 3600000
        tcpCmdServerSock = nil
        JAFDTC_Log("ERROR: Unable to open TCP rx socket")
    end

    JAFDTC_Log(string.format("Export hooks starts at %.3f", socket.gettime()))
end

-- receive incoming command streams from jafdtc, crack, and execute them. commands are run as co-routines so a stream
-- executes over multiple frames. commands functions are JAFDCT_Cmd_<command_name>, see documentation above.
--
function LuaExportBeforeNextFrame()
    JAFDTC_CallUpstream(upstreamLuaExportBeforeNextFrame, "bframe")

    local curTime = socket.gettime()
    if curTime >= cmdResumeTime then
        if not cmdList then
            local data = JAFDTC_TCPServerSockRx(tcpCmdServerSock)
            if data then
                cmdCurProgress = 0
                cmdListIndex = 1
                cmdList = JSON:decode(data)
                if not cmdList and data then
                    JAFDTC_Log(string.format("[%.3f] ERROR: JSON decode failed", curTime))
                elseif cmdList and data then
                    JAFDTC_Log(string.format("[%.3f] Process rx cmdList[1:%d] %dB", curTime, #cmdList, string.len(data)))
                    whileTout = { }
                end
            end
        end

        if not cmdCurCort and cmdList then
            if cmdListIndex <= #cmdList then
                local cmdName = "JAFDTC_Cmd_" .. cmdList[cmdListIndex]["f"]
                local cmdArgs = JAFDTC_Debug_DumpCmdArgs(cmdList, cmdListIndex)
                JAFDTC_Log(string.format("[%.3f] Create [%d] %s%s", curTime, cmdListIndex, cmdName, cmdArgs))
                cmdCurCort = coroutine.create(_G[cmdName])
            else
                cmdList = nil
                JAFDTC_Log(string.format("[%.3f] Reached end of cmdList", curTime))
            end
        end

        if cmdCurCort and coroutine.status(cmdCurCort) == 'suspended' then
            local status, di, dt = coroutine.resume(cmdCurCort, cmdList, cmdListIndex)
            if not status then
                local cmdName = "JAFDTC_Cmd_" .. cmdList[cmdListIndex]["f"]
                local cmdMsgInfo = string.format("[%d] %s", cmdListIndex, cmdName)
                JAFDTC_Log(string.format("[%.3f] ERROR: Resume %s failed", curTime, cmdMsgInfo))
                cmdList = nil
                cmdCurCort = nil
                markerVal = string.format("ERROR: Command Failed, %s", cmdMsgInfo)
            else
                cmdListIndex = cmdListIndex + di
                cmdResumeTime = curTime + (dt / 1000.0)
                cmdCurProgress = math.max(cmdCurProgress, (cmdListIndex / #cmdList) * 100)
            end
        elseif cmdCurCort and coroutine.status(cmdCurCort) == 'dead' then
            cmdCurCort = nil
        end
    end
end

-- package telemetry to send upstream to jafdtc. calls JAFDTC_<model>_AfterNextFrame to perform modle-specific
-- operations prior to constructing the telemetry.
--
function LuaExportAfterNextFrame()
    JAFDTC_CallUpstream(upstreamLuaExportAfterNextFrame, "aframe")

    local camPos = LoGetCameraPosition()
    local loX = camPos['p']['x']
    local loZ = camPos['p']['z']
    local elevation = LoGetAltitude(loX, loZ)
    local coords = LoLoCoordinatesToGeoCoordinates(loX, loZ)
    local model = JAFDTC_GetPlayerAircraftType()

    local markerValTx = string.gsub(markerVal, "<upload_prog>", string.format("%d", cmdCurProgress))

    local funcName = "JAFDTC_" .. model .. "_AfterNextFrame";
    local params = {}
    params["uploadCommand"] = "0"
    params["incCommand"] = "0"
    params["decCommand"] = "0"
    params["showJAFDTCCommand"] = "0"
    params["hideJAFDTCCommand"] = "0"
    params["toggleJAFDTCCommand"] = "0"
    pcall(_G[funcName], params)

    local txData = "{"..
        '"Model": "' .. model .. '",' ..
        '"Marker": "' .. markerValTx .. '",' ..
        '"Response": "' .. responseVal .. '",' ..
-- NOTE: exclude lat/lon/elev for now as its unused on the jafdtc side
--[[
        '"Lat": "' .. coords.latitude .. '",' ..
        '"Lon": "' .. coords.longitude .. '",' ..
        '"Elev": "' .. elevation .. '",' ..
--]]
        '"CmdUpload": "' .. params["uploadCommand"] .. '",' ..
        '"CmdIncr": "' .. params["incCommand"] .. '",' ..
        '"CmdDecr": "' .. params["decCommand"] .. '",' ..
        '"CmdShow": "' .. params["showJAFDTCCommand"] .. '",' ..
        '"CmdHide": "' .. params["hideJAFDTCCommand"] .. '",' ..
        '"CmdToggle": "' .. params["toggleJAFDTCCommand"] .. '"' ..
    "}"
    JAFDTC_UDPSockTx(udpTelemSockTx, udpTelemPortTx, txData)

    if string.sub(markerVal, 1, #"ERROR:") == "ERROR:" then
        JAFDTC_Log("Marker forced to empty after reporting " .. markerVal)
        markerVal = ""
    end
    responseVal = ""
end