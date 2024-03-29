--[[
********************************************************************************************************************

FA18CFunctions.lua -- hornet airframe-specific lua functions

Copyright(C) 2021-2023 the-paid-actor & dcs-dtc contributors
Copyright(C) 2023-2024 ilominar/raven

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

dofile(lfs.writedir() .. 'Scripts/JAFDTC/commonFunctions.lua')

function JAFDTC_FA18C_GetLeftDDI()
	return JAFDTC_ParseDisplay(2)
end

function JAFDTC_FA18C_GetRightDDI()
	return JAFDTC_ParseDisplay(3)
end

function JAFDTC_FA18C_GetMPCD()
	return JAFDTC_ParseDisplay(4)
end

function JAFDTC_FA18C_GetIFEI()
	return JAFDTC_ParseDisplay(5)
end

function JAFDTC_FA18C_CheckCondition_NotAtWp0()
	local table = JAFDTC_FA18C_GetRightDDI();
	local str = table["WYPT_Page_Number"]
	if str == "0" then
		return false
	end 
	return true
end

function JAFDTC_FA18C_CheckCondition_BingoIsZero()
	local table = JAFDTC_FA18C_GetIFEI();
	local str = table["txt_BINGO"]
	if str == "0" then
		return true
	end 
	return false
end

function JAFDTC_FA18C_CheckCondition_InSequence(i)
	local table =JAFDTC_FA18C_GetRightDDI();
	local str = table["WYPT_SequenceData"]
	local noSpaces = str:gsub("%s+", "")
	for token in string.gmatch(noSpaces, "[^-]+") do
		if token == i then 
			return true
		end
	end
	return false
end

function JAFDTC_FA18C_CheckCondition_CheckStore(wpn, station)
	local table = JAFDTC_FA18C_GetLeftDDI();
	local str = table["STA"..station.."_Label_TYPE"] or ""
	if str == wpn then
		return true
	end
	return false
end

function JAFDTC_FA18C_CheckCondition_StationSelected(station)
	local table = JAFDTC_FA18C_GetLeftDDI();
	local str = table["STA"..station.."_Selective_Box_Line_02"] or "x"
	if str == "" then
		return true
	end
	return false
end

function JAFDTC_FA18C_CheckCondition_IsTargetOfOpportunity()
	local table = JAFDTC_FA18C_GetLeftDDI();
	local str = table["Miss_Type"] or ""
	if str == "TOO1" then
		return true
	end
	return false
end

function JAFDTC_FA18C_CheckCondition_IsPPNotSelected(number)
	local table = JAFDTC_FA18C_GetLeftDDI();
	local str = table["MISSION_Type"] or ""
	if str ~= "PP"..number then
		return true
	end
	return false
end

function JAFDTC_FA18C_CheckCondition_LmfdNotTac()
	local table = JAFDTC_FA18C_GetLeftDDI();
	local str = table["TAC_id:23"] or ""
	if str == "TAC" then
		return false
	end 
	return true
end

function JAFDTC_FA18C_CheckCondition_RmfdNotSupt()
	local table = JAFDTC_FA18C_GetRightDDI();
	local str = table["SUPT_id:13"] or ""
	if str == "SUPT" then
		return false
	end 
	return true
end

function JAFDTC_FA18C_CheckCondition_NotBullseye()
	local table = JAFDTC_FA18C_GetRightDDI();
	local str = table["A/A WP_1_box__id:12"] or "x"
	if str == "x" then
		return true
	end 
	return false
end

function JAFDTC_FA18C_CheckCondition_MapBoxed()
	local table = JAFDTC_FA18C_GetRightDDI();
	local str = table["MAP_1_box__id:30"] or "x"
	if str == "" then
		return true
	end 
	return false
end

function JAFDTC_FA18C_CheckCondition_MapUnboxed()
	local table = JAFDTC_FA18C_GetRightDDI();
	local root = table["HSI_Main_Root"] or "x"
	local str = table["MAP_1_box__id:30"] or "x"
	if root == "x" and str == "x" then
		return true
	end 
	return false
end

function JAFDTC_FA18C_CheckCondition_IsBankLimitOnNav()
	local table = JAFDTC_FA18C_GetRightDDI();
	local str = table["_1__id:13"] or ""
	if str == "N" then
		return true
	end 
	return false
end

function JAFDTC_FA18C_CheckCondition_DispenserOff()
	local table = JAFDTC_FA18C_GetLeftDDI();
	local str = table["EW_ALE47_MODE_label_cross_Root"] or "x"
	if str == "" then
		return true
	end 
	return false
end

function JAFDTC_FA18C_AfterNextFrame(params)
	local mainPanel = GetDevice(0);
	local ipButton = mainPanel:get_argument_value(99);
	local hudVideoSwitch = mainPanel:get_argument_value(144);

	if ipButton == 1 then params["uploadCommand"] = "1" end
	if hudVideoSwitch >= 0.2 then params["showJAFDTCCommand"] = "1" end
	if hudVideoSwitch > 0 and hudVideoSwitch < 0.2 then params["hideJAFDTCCommand"] = "1" end
end