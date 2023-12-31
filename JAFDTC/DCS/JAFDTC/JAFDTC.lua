local tcpServer = nil
local udpSpeaker = nil
package.path  = package.path..";"..lfs.currentdir().."/LuaSocket/?.lua"
package.cpath = package.cpath..";"..lfs.currentdir().."/LuaSocket/?.dll"
package.path  = package.path..";"..lfs.currentdir().."/Scripts/?.lua"
local socket = require("socket")
local JSON = loadfile("Scripts\\JSON.lua")()

JAFDTC_logFile = io.open(lfs.writedir() .. [[Logs\JAFDTC.log]], "w")

dofile(lfs.writedir()..'Scripts/JAFDTC/CommonFunctions.lua')
dofile(lfs.writedir()..'Scripts/JAFDTC/A10CFunctions.lua')
dofile(lfs.writedir()..'Scripts/JAFDTC/AV8BFunctions.lua')
dofile(lfs.writedir()..'Scripts/JAFDTC/F15EFunctions.lua')
dofile(lfs.writedir()..'Scripts/JAFDTC/F16CFunctions.lua')
dofile(lfs.writedir()..'Scripts/JAFDTC/FA18CFunctions.lua')

local needDelay = false
local keypressinprogress = false
local data
local delay = 0
local delayNeeded = 0
local delayStart = 0
local code = ""
local device = ""
local nextIndex = 1
local markerVal = ""

local skipCondition
local skip = false

local tcpPort = 42001
local udpPort = 42002

local upstreamLuaExportStart = LuaExportStart
local upstreamLuaExportAfterNextFrame = LuaExportAfterNextFrame
local upstreamLuaExportBeforeNextFrame = LuaExportBeforeNextFrame

function LuaExportStart()
    if upstreamLuaExportStart ~= nil then
        local successful, err = pcall(upstreamLuaExportStart)
        if not successful then
            log.write("JAFDTC", log.ERROR, "Error in upstream LuaExportStart function"..tostring(err))
        end
    end
    
    udpSpeaker = socket.udp()
    udpSpeaker:settimeout(0)
    tcpServer = socket.tcp()
    local successful, err = tcpServer:bind("127.0.0.1", tcpPort)
    tcpServer:listen(1)
    tcpServer:settimeout(0)
    if not successful then
        log.write("JAFDTC", log.ERROR, "Error opening tcp socket - "..tostring(err))
    else
        log.write("JAFDTC", log.INFO, "Opened connection")
    end
end

local function checkCondition(condition, param1, param2)
    local ac = JAFDTC_GetPlayerAircraftType();
    local funcName = 'JAFDTC_'..ac..'_CheckCondition_'..condition;
    local res = _G[funcName](param1, param2)
    return res
end

function LuaExportBeforeNextFrame()
    if upstreamLuaExportBeforeNextFrame ~= nil then
        local successful, err = pcall(upstreamLuaExportBeforeNextFrame)
        if not successful then
           log.write("JAFDTC", log.ERROR, "Error in upstream LuaExportBeforeNextFrame function"..tostring(err))
        end
    end

    if needDelay then
        local currentTime = socket.gettime()
        if ((currentTime - delayStart) > delayNeeded) then
            needDelay = false
            if device ~= "wait" then
                GetDevice(device):performClickableAction(code, 0)
            end
        end
    else
        if keypressinprogress then
            local keys = JSON:decode(data)
            for i=nextIndex, #keys do
                local keyObj = keys[i]
                local startCondition = keyObj["start_condition"]
                local endCondition = keyObj["end_condition"]
                local marker = keyObj["marker"]
                
                if endCondition then
                    if endCondition == skipCondition then
                        skipCondition = nil
                        skip = false
                        nextIndex = i+1
                    end

                elseif skip then
                    nextIndex = i+1	

                elseif startCondition then
                    skipCondition = startCondition
                    local param1 = keyObj["param1"]
                    local param2 = keyObj["param2"]
                    skip = not checkCondition(startCondition, param1, param2)
                    nextIndex = i+1
                
                elseif marker then
                    markerVal = marker

                else
                    device = keyObj["device"]
                    code = keyObj["code"]
                    delay = tonumber(keyObj["delay"])

                    local activate = tonumber(keyObj["activate"])

                    if delay > 0 then
                        needDelay = true
                        delayNeeded = delay / 1000
                        delayStart = socket.gettime()
                        if device ~= "wait" then
                            GetDevice(device):performClickableAction(code, activate)
                        end
                        nextIndex = i+1
                        break
                    else
                        GetDevice(device):performClickableAction(code, activate)
                        if delay == 0 then
                            GetDevice(device):performClickableAction(code, 0)
                        end
                    end
                end
            end
            if not needDelay then
                keypressinprogress = false
                nextIndex = 1
            end
        else
            local client, err = tcpServer:accept()

            if client ~= nil then
                client:settimeout(10)
                data, err = client:receive()
                if err then
                    log.write("JAFDTC", log.ERROR, "Error at receiving: "..err)  
                end

                if data then 
                    keypressinprogress = true
                end
            end
        end
    end
end

function LuaExportAfterNextFrame()
    if upstreamLuaExportAfterNextFrame ~= nil then
        local successful, err = pcall(upstreamLuaExportAfterNextFrame)
        if not successful then
            log.write("JAFDTC", log.ERROR, "Error in upstream LuaExportAfterNextFrame function"..tostring(err))
        end
    end

    local camPos = LoGetCameraPosition()
    local loX = camPos['p']['x']
    local loZ = camPos['p']['z']
    local elevation = LoGetAltitude(loX, loZ)
    local coords = LoLoCoordinatesToGeoCoordinates(loX, loZ)
    local model = JAFDTC_GetPlayerAircraftType();

    local params = {};
    params["uploadCommand"] = "0";
    params["incCommand"] = "0";
    params["decCommand"] = "0";
    params["showJAFDTCCommand"] = "0";
    params["hideJAFDTCCommand"] = "0";
    params["toggleJAFDTCCommand"] = "0";

    if model == "AV8B" then
        JAFDTC_AV8B_AfterNextFrame(params)
    end

    if model ==	"F16CM" then
        JAFDTC_F16CM_AfterNextFrame(params)
    end

    if model == "F15E" then
        JAFDTC_F15E_AfterNextFrame(params)
    end

    if model == "FA18C" then
        JAFDTC_FA18C_AfterNextFrame(params)
    end

    local toSend = "{"..
        "\"Model\": ".."\""..model.."\""..
        ", ".."\"Marker\": ".."\""..markerVal.."\""..
        ", ".."\"Latitude\": ".."\""..coords.latitude.."\""..
        ", ".."\"Longitude\": ".."\""..coords.longitude.."\""..
        ", ".."\"Elevation\": ".."\""..elevation.."\""..
        ", ".."\"Upload\": ".."\""..params["uploadCommand"].."\""..
        ", ".."\"Increment\": ".."\""..params["incCommand"].."\""..
        ", ".."\"Decrement\": ".."\""..params["decCommand"].."\""..
        ", ".."\"ShowJAFDTC\": ".."\""..params["showJAFDTCCommand"].."\""..
        ", ".."\"HideJAFDTC\": ".."\""..params["hideJAFDTCCommand"].."\""..
        ", ".."\"ToggleJAFDTC\": ".."\""..params["toggleJAFDTCCommand"].."\""..
        "}"

    if pcall(function()
        socket.try(udpSpeaker:sendto(toSend, "127.0.0.1", udpPort)) 
    end) then
    else
        log.write("JAFDTC", log.ERROR, "Unable to send data")
    end
end