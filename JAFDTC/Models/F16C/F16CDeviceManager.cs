﻿// ********************************************************************************************************************
//
// F16DeviceManager.cs -- f-16c airframe device manager
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023-2024 ilominar/raven
//
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU General
// Public License as published by the Free Software Foundation, either version 3 of the License, or (at your
// option) any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
// for more details.
//
// You should have received a copy of the GNU General Public License along with this program.  If not, see
// <https://www.gnu.org/licenses/>.
//
// ********************************************************************************************************************

using JAFDTC.Utilities;
using JAFDTC.Models.DCS;
using System.Diagnostics;

namespace JAFDTC.Models.F16C
{
    /// <summary>
    /// manages the set of dcs airframe devices and associated commands/actions for the viper.
    /// </summary>
    internal class F16CDeviceManager : AirframeDeviceManagerBase, IAirframeDeviceManager
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------
        
        public F16CDeviceManager()
        {
            // TODO: this is ok for default, but needs to be hardened once changes are allowed
            int delay = Settings.CommandDelaysMs[AirframeTypes.F16C];
            int delayDobber = delay;
            int delayIncDec = delay;
            int delayEntr = delay / 2;
            int delayMFDs = delay / 4;
            int delayList = delay / 4;
            int delayHOTAS = delay / 4;
            int delayKey = delay / 10;

            // ---- sms

            AirframeDevice sms = new(22, "SMS");
            sms.AddAction(3002, "LEFT_HDPT", delay, 1);
            sms.AddAction(3003, "RIGHT_HDPT", delay, 1);
            AddDevice(sms);

            // ---- ufc

            AirframeDevice ufc = new(17, "UFC");
            ufc.AddAction(3002, "0", delayKey, 1);
            ufc.AddAction(3003, "1", delayKey, 1);
            ufc.AddAction(3004, "2", delayKey, 1);
            ufc.AddAction(3005, "3", delayKey, 1);
            ufc.AddAction(3006, "4", delayKey, 1);
            ufc.AddAction(3007, "5", delayKey, 1);
            ufc.AddAction(3008, "6", delayKey, 1);
            ufc.AddAction(3009, "7", delayKey, 1);
            ufc.AddAction(3010, "8", delayKey, 1);
            ufc.AddAction(3011, "9", delayKey, 1);
            ufc.AddAction(3012, "COM1", delayKey, 1);
            ufc.AddAction(3013, "COM2", delayKey, 1);
            ufc.AddAction(3015, "LIST", delayList, 1);
            ufc.AddAction(3016, "ENTR", delayEntr, 1);
            ufc.AddAction(3017, "RCL", delayKey, 1);
            ufc.AddAction(3018, "AA", delay, 1);
            ufc.AddAction(3019, "AG", delay, 1);
            ufc.AddAction(3030, "INC", delayIncDec, 1);
            ufc.AddAction(3031, "DEC", delayIncDec, 1);
            ufc.AddAction(3032, "RTN", delayDobber, -1);
            ufc.AddAction(3033, "SEQ", delayDobber, 1);
            ufc.AddAction(3034, "UP", delayDobber, 1);
            ufc.AddAction(3035, "DOWN", delayDobber, -1);
            AddDevice(ufc);

            // ---- hotas buttons

            AirframeDevice hotas = new(16, "HOTAS");
            hotas.AddAction(3030, "DGFT", delayHOTAS, 1, 1);
            hotas.AddAction(3030, "MSL", delayHOTAS, -1, -1);
            hotas.AddAction(3030, "CENTER", delayHOTAS, 0, 0);
            AddDevice(hotas);

            // ---- left mfd

            // NOTE: left and right mfds should use same osb button names

            AirframeDevice leftMFD = new(24, "LMFD");
            leftMFD.AddAction(3001, "OSB-01", delayMFDs, 1);
            leftMFD.AddAction(3002, "OSB-02", delayMFDs, 1);
            leftMFD.AddAction(3003, "OSB-03", delayMFDs, 1);
            leftMFD.AddAction(3004, "OSB-04", delayMFDs, 1);
            leftMFD.AddAction(3005, "OSB-05", delayMFDs, 1);
            leftMFD.AddAction(3006, "OSB-06", delayMFDs, 1);
            leftMFD.AddAction(3007, "OSB-07", delayMFDs, 1);
            leftMFD.AddAction(3008, "OSB-08", delayMFDs, 1);
            leftMFD.AddAction(3009, "OSB-09", delayMFDs, 1);
            leftMFD.AddAction(3010, "OSB-10", delayMFDs, 1);
            leftMFD.AddAction(3011, "OSB-11", delayMFDs, 1);
            leftMFD.AddAction(3012, "OSB-12", delayMFDs, 1);
            leftMFD.AddAction(3013, "OSB-13", delayMFDs, 1);
            leftMFD.AddAction(3014, "OSB-14", delayMFDs, 1);
            leftMFD.AddAction(3015, "OSB-15", delayMFDs, 1);
            leftMFD.AddAction(3016, "OSB-16", delayMFDs, 1);
            leftMFD.AddAction(3017, "OSB-17", delayMFDs, 1);
            leftMFD.AddAction(3018, "OSB-18", delayMFDs, 1);
            leftMFD.AddAction(3019, "OSB-19", delayMFDs, 1);
            leftMFD.AddAction(3020, "OSB-20", delayMFDs, 1);
            AddDevice(leftMFD);

            // ---- right mfd

            // NOTE: left and right mfds should use same osb button names

            AirframeDevice rightMFD = new(25, "RMFD");
            rightMFD.AddAction(3001, "OSB-01", delayMFDs, 1);
            rightMFD.AddAction(3002, "OSB-02", delayMFDs, 1);
            rightMFD.AddAction(3003, "OSB-03", delayMFDs, 1);
            rightMFD.AddAction(3004, "OSB-04", delayMFDs, 1);
            rightMFD.AddAction(3005, "OSB-05", delayMFDs, 1);
            rightMFD.AddAction(3006, "OSB-06", delayMFDs, 1);
            rightMFD.AddAction(3007, "OSB-07", delayMFDs, 1);
            rightMFD.AddAction(3008, "OSB-08", delayMFDs, 1);
            rightMFD.AddAction(3009, "OSB-09", delayMFDs, 1);
            rightMFD.AddAction(3010, "OSB-10", delayMFDs, 1);
            rightMFD.AddAction(3011, "OSB-11", delayMFDs, 1);
            rightMFD.AddAction(3012, "OSB-12", delayMFDs, 1);
            rightMFD.AddAction(3013, "OSB-13", delayMFDs, 1);
            rightMFD.AddAction(3014, "OSB-14", delayMFDs, 1);
            rightMFD.AddAction(3015, "OSB-15", delayMFDs, 1);
            rightMFD.AddAction(3016, "OSB-16", delayMFDs, 1);
            rightMFD.AddAction(3017, "OSB-17", delayMFDs, 1);
            rightMFD.AddAction(3018, "OSB-18", delayMFDs, 1);
            rightMFD.AddAction(3019, "OSB-19", delayMFDs, 1);
            rightMFD.AddAction(3020, "OSB-20", delayMFDs, 1);
            AddDevice(rightMFD);

            // ---- ehsi

            AirframeDevice ehsi = new(28, "EHSI");
            ehsi.AddAction(3001, "MODE", delay, 1);
            AddDevice(ehsi);

            // ---- hmcs panel

            AirframeDevice hmcsInt = new(30, "HMCS_INT");
            hmcsInt.AddAction(3001, "INT", 1, 0.0, 0.0);
            AddDevice(hmcsInt);

            // ---- interior lights

            AirframeDevice intl = new(12, "INTL");
            intl.AddAction(3002, "MAL_IND_LTS_TEST", delay, 1);
            AddDevice(intl);
        }
    }
}