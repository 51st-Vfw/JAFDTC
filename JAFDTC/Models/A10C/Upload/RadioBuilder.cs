﻿// ********************************************************************************************************************
//
// RadioBuilder.cs -- a-10c radio command builder
//
// Copyright(C) 2024 ilominar/raven
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

using JAFDTC.Models.DCS;
using JAFDTC.Models.A10C.Radio;
using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Collections.Generic;

namespace JAFDTC.Models.A10C.Upload
{
    /// <summary>
    /// command builder for the radio system in the warthog. translates cmds setup in A10CConfiguration into
    /// commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class RadioBuilder : A10CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public RadioBuilder(A10CConfiguration cfg, A10CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure radio system via the cdu according to the non-default programming settings (this function is
        /// safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// </summary>
        public override void Build(Dictionary<string, object> state = null)
        {
            if (_cfg.Radio.IsDefault)
                return;

            AddExecFunction("NOP", new() { "==== RadioBuilder:Build()" });

            AirframeDevice cdu = _aircraft.GetDevice("CDU");
            AirframeDevice lmfd = _aircraft.GetDevice("LMFD");
            AirframeDevice rmfd = _aircraft.GetDevice("RMFD");
            AirframeDevice ufc = _aircraft.GetDevice("UFC");
            AirframeDevice arc210 = _aircraft.GetDevice("UHF_ARC210");
            AirframeDevice arc164 = _aircraft.GetDevice("UHF_ARC164");
            AirframeDevice arc186 = _aircraft.GetDevice("VHF_ARC186");

            BuildARC210(cdu, ufc, lmfd, rmfd, arc210, _cfg.Radio);
            BuildARC164(arc164, _cfg.Radio);
            BuildARC186(arc186, _cfg.Radio);
        }

        /// <summary>
        /// configure the primary ARC-210 UHF/VHF radio according to the non-default programming settings (this
        /// function is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// this includes presets, default freq/preset, preset mode, guard monitor, and HUD status display.
        /// </summary>
        private void BuildARC210(AirframeDevice cdu, AirframeDevice ufc, AirframeDevice lmfd, AirframeDevice rmfd, AirframeDevice arc210,
                                 RadioSystem radios)
        {
            // Nice to do this first, so you can e.g. monitor guard and the initial freq while preset programming is happening.
            BuildARC210Initial(ufc, arc210, radios);

            if (radios.Presets[(int)RadioSystem.Radios.COMM1].Count > 0)
            {
                AddIfBlock("IsCommPageOnDefaultButton", true, null, delegate ()
                {
                    // If COMM is in its default location on the left MFD, use it there.
                    AddActions(lmfd, new() { "LMFD_13", "LMFD_19" });
                    BuildARC210Presets(cdu, arc210, lmfd, "LMFD_", radios);
                });
                AddIfBlock("IsCommPageOnDefaultButton", false, null, delegate ()
                {
                    // If COMM isn't in its default location on the left MFD, put it in place of MSG on the right.
                    AddActions(rmfd, new() { "RMFD_12_LONG", "RMFD_06", "RMFD_12", "RMFD_12", "RMFD_19" });
                    BuildARC210Presets(cdu, arc210, rmfd, "RMFD_", radios);

                    // Restore MSG on its usual button on the right MFD.
                    AddActions(rmfd, new() { "RMFD_12_LONG", "RMFD_17", "RMFD_12", "RMFD_12" });
                });
            }
        }

        /// <summary>
        /// configure the primary ARC-164 UHF radio according to the non-default programming settings (this
        /// function is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// this includes presets, default freq/preset, preset mode, and guard monitor,.
        /// </summary>
        private void BuildARC164(AirframeDevice arc164, RadioSystem radios)
        {
            if (radios.Presets[(int)RadioSystem.Radios.COMM2].Count > 0)
            {
                AddActions(arc164, new() { "UHF_COVER_OPEN", "UHF_MODE_PRESET" });
                foreach (RadioPreset preset in radios.Presets[(int)RadioSystem.Radios.COMM2])
                {
                    double presetValue = (double)(preset.Preset - 1) * 0.05;
                    AddDynamicAction(arc164, "UHF_PRESET_SEL", presetValue, presetValue);
                    AddWait(WAIT_BASE);
                    BuildARC164ManualFrequency(arc164, preset.Frequency);
                    AddWait(WAIT_BASE);
                    AddAction(arc164, "UHF_LOAD");
                    AddWait(WAIT_BASE);
                }
                AddAction(arc164, "UHF_COVER_CLOSED");
            }

            string defSetting = radios.DefaultSetting[(int)RadioSystem.Radios.COMM2];
            if (!string.IsNullOrEmpty(radios.DefaultSetting[(int)RadioSystem.Radios.COMM2]))
            {
                if (int.TryParse(defSetting, out int defPreset))
                {
                    AddAction(arc164, "UHF_MODE_PRESET");
                    double presetValue = (double)(defPreset - 1) * 0.05;
                    AddDynamicAction(arc164, "UHF_PRESET_SEL", presetValue, presetValue);
                }
                else
                {
                    BuildARC164ManualFrequency(arc164, defSetting);
                }
            }

            AddAction(arc164, (radios.IsMonitorGuard[(int)RadioSystem.Radios.COMM2]) ? "UHF_FUNCTION_BOTH"
                                                                                     : "UHF_FUNCTION_MAIN");
            AddAction(arc164, (radios.IsPresetMode[(int)RadioSystem.Radios.COMM2]) ? "UHF_MODE_PRESET"
                                                                                   : "UHF_MODE_MNL");
        }

        /// <summary>
        /// configure the primary ARC-186 VHF radio according to the non-default programming settings (this
        /// function is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// this includes presets, default freq/preset, and preset mode.
        /// </summary>
        private void BuildARC186(AirframeDevice arc186, RadioSystem radios)
        {
            // Ensure radio is on. No Guard on the ARC-186.
            AddAction(arc186, "VHFFM_MODE_TR");

            ARC186Tuner tuner = new ARC186Tuner(this, arc186);

            if (radios.Presets[(int)RadioSystem.Radios.COMM3].Count > 0)
            {
                // Have to be in manual mode to set presets on the ARC-186.
                AddAction(arc186, "VHFFM_FREQEMER_MAN");

                foreach (RadioPreset preset in radios.Presets[(int)RadioSystem.Radios.COMM3])
                {
                    tuner.TuneTo(preset.Frequency);
                    tuner.SetPresetWheel(preset.Preset);
                    AddAction(arc186, "VHFFM_LOAD");
                }
            }

            string defSetting = radios.DefaultSetting[(int)RadioSystem.Radios.COMM3];
            if (!string.IsNullOrEmpty(radios.DefaultSetting[(int)RadioSystem.Radios.COMM3]))
            {
                if (int.TryParse(defSetting, out int defPreset))
                    tuner.SetPresetWheel(defPreset);
                else
                    tuner.TuneTo(defSetting);
            }

            AddAction(arc186, (radios.IsPresetMode[(int)RadioSystem.Radios.COMM3]) ? "VHFFM_FREQEMER_PRE"
                                                                                   : "VHFFM_FREQEMER_MAN");
        }

        /// <summary>
        /// returns the preset with the specified number from the list of presets; null if no such preset exists.
        /// </summary>
        private static RadioPreset RadioHasPreset(int presetNum, ObservableCollection<RadioPreset> presets)
        {
            foreach (RadioPreset preset in presets)
            {
                if (preset.Preset == presetNum)
                {
                    return preset;
                }
            }
            return null;
        }

        /// <summary>
        /// returns the maximum preset defined, 0 if no presets are defined.
        /// </summary>
        private static int RadioMaxPreset(ObservableCollection<RadioPreset> presets)
        {
            int max = 0;
            foreach (RadioPreset preset in presets)
            {
                max = Math.Max(preset.Preset, max);
            }
            return max;
        }

        /// <summary>
        ///  Build all the ARC-210 presets on the left or right MFD.
        /// </summary>
        private void BuildARC210Presets(AirframeDevice cdu, AirframeDevice arc210, AirframeDevice mfd, string mfdPrefix, RadioSystem radios)
        {
            int maxPreset = RadioMaxPreset(radios.Presets[(int)RadioSystem.Radios.COMM1]);

            for (int i = 1; (i <= 18) && (i <= maxPreset); i++)
            {
                RadioPreset preset = RadioHasPreset(i, radios.Presets[(int)RadioSystem.Radios.COMM1]);
                if (preset != null)
                {
                    BuildARC210Preset(cdu, mfd, mfdPrefix, preset);
                }
                AddAction(mfd, mfdPrefix + "19");
            }
            AddAction(mfd, mfdPrefix + "02");
            for (int i = 19; (i <= 25) && (i <= maxPreset); i++)
            {
                RadioPreset preset = RadioHasPreset(i, radios.Presets[(int)RadioSystem.Radios.COMM1]);
                if (preset != null)
                {
                    BuildARC210Preset(cdu, mfd, mfdPrefix, preset);
                }
                AddAction(mfd, mfdPrefix + "19");
            }
            AddAction(mfd, mfdPrefix + "01");

            AddAction(arc210, (radios.IsPresetMode[(int)RadioSystem.Radios.COMM1]) ? "ARC210_SEC_SW_PRST"
                                                                                   : "ARC210_SEC_SW_MAN");
        }

        /// <summary>
        /// build the commands to set up the arc-210 preset including the description, frequency, and modulation.
        /// </summary>
        private void BuildARC210Preset(AirframeDevice cdu, AirframeDevice mfd, string mfdPrefix, RadioPreset preset)
        {
            string descr = preset.Description.ToUpper();
            if (string.IsNullOrEmpty(descr))
            {
                descr = $"SP{preset.Preset}";
            }
            AddActions(cdu, new() { "CLR", "CLR" }, ActionsForString(AdjustOnlyAlphaNum(descr)));
            AddAction(mfd, mfdPrefix + "16");

            AddActions(cdu, new() { "CLR", "CLR" }, ActionsForString(AdjustNoSeparators(preset.Frequency)));
            AddAction(mfd, mfdPrefix + "17");

            if (!RadioSystem.IsModulationDefaultForFreq(RadioSystem.Radios.COMM1, preset.Frequency, preset.Modulation))
            {
                AddAction(mfd, mfdPrefix + "05");
            }
        }

        /// <summary>
        /// Build the settings for the ARC-210 that aren't presets.
        /// </summary>
        private void BuildARC210Initial(AirframeDevice ufc, AirframeDevice arc210, RadioSystem radios)
        {
            AddAction(arc210, (radios.IsMonitorGuard[(int)RadioSystem.Radios.COMM1]) ? "ARC210_MASTER_TR_G"
                                                                                     : "ARC210_MASTER_TR");

            string defSetting = radios.DefaultSetting[(int)RadioSystem.Radios.COMM1];
            if (!string.IsNullOrEmpty(radios.DefaultSetting[(int)RadioSystem.Radios.COMM1]))
            {
                if (int.TryParse(defSetting, out _))
                {
                    AddActions(ufc, ActionsForString(defSetting), new() { "UFC_COM1" });
                }
                else if (double.TryParse(defSetting, out double val))
                {
                    int val100MHz = (int)(val / 100.0);
                    val -= val100MHz * 100.0;
                    int val010MHz = (int)(val / 10.0);
                    val -= val010MHz * 10.0;
                    int val001MHz = (int)(val);
                    val = Math.Round((val - val001MHz) * 1000.0, 0);
                    int val100KHz = (int)(val / 100.0);
                    val -= val100KHz * 100.0;
                    int val025KHz = (int)(val / 25.0) % 4;
                    AddDynamicAction(arc210, "ARC210_100MHZ_SEL", (double)val100MHz * 0.1, (double)val100MHz * 0.1);
                    AddDynamicAction(arc210, "ARC210_10MHZ_SEL", (double)val010MHz * 0.1, (double)val010MHz * 0.1);
                    AddDynamicAction(arc210, "ARC210_1MHZ_SEL", (double)val001MHz * 0.1, (double)val001MHz * 0.1);
                    AddDynamicAction(arc210, "ARC210_100KHZ_SEL", (double)val100KHz * 0.1, (double)val100KHz * 0.1);
                    AddDynamicAction(arc210, "ARC210_25KHZ_SEL", (double)val025KHz * 0.1, (double)val025KHz * 0.1);
                }
            }

            // Show/hide ARC-210 COMM1 status on the HUD based on the setting and the current state.
            if (radios.IsCOMM1StatusOnHUD)
            {
                AddIfBlock("Arc210Com1IsOnHUD", false, null, delegate ()
                {
                    AddAction(ufc, "UFC_COM1_LONG"); // Set to be on, currently off, turn it on.
                });
            }
            else
            {
                AddIfBlock("Arc210Com1IsOnHUD", true, null, delegate ()
                {
                    AddAction(ufc, "UFC_COM1_LONG"); // Set to be off, currently on, turn it off.
                });
            }

            // Always hide COM2: it's unimplemented and just HUD clutter.
            AddIfBlock("Arc210Com2IsOnHUD", true, null, delegate ()
            {
                AddAction(ufc, "UFC_COM2_LONG");
            });
        }

        /// <summary>
        /// build the commands to set up the arc-164 frequency manually.
        /// </summary>
        private void BuildARC164ManualFrequency(AirframeDevice arc164, string freq)
        {
            if (double.TryParse(freq, out double val))
            {
                int val100MHz = (int)(val / 100.0);
                val -= val100MHz * 100.0;
                int val010MHz = (int)(val / 10.0);
                val -= val010MHz * 10.0;
                int val001MHz = (int)(val);
                val = Math.Round((val - val001MHz) * 1000.0, 0);
                int val100KHz = (int)(val / 100.0);
                val -= val100KHz * 100.0;
                int val025KHz = (int)(val / 25.0) % 4;
                AddDynamicAction(arc164, "UHF_100MHZ_SEL", (double)(val100MHz - 2) * 0.1, (double)(val100MHz - 2) * 0.1);
                AddDynamicAction(arc164, "UHF_10MHZ_SEL", (double)val010MHz * 0.1, (double)val010MHz * 0.1);
                AddDynamicAction(arc164, "UHF_1MHZ_SEL", (double)val001MHz * 0.1, (double)val001MHz * 0.1);
                AddDynamicAction(arc164, "UHF_POINT1MHZ_SEL", (double)val100KHz * 0.1, (double)val100KHz * 0.1);
                AddDynamicAction(arc164, "UHF_POINT025_SEL", (double)val025KHz * 0.1, (double)val025KHz * 0.1);
            }
        }

        /// <summary>
        /// Helper class to remember the state of the ARC-186 FM radio's wheels.
        /// </summary>
        private class ARC186Tuner
        {
            private readonly RadioBuilder _builder;
            private readonly AirframeDevice _arc186;

            private int _freqWheel1 = 3;
            private int _freqWheel2 = 0;
            private int _freqWheel3 = 0;
            private int _freqWheel4 = 0;

            private int _presetWheel = 1;

            public ARC186Tuner(RadioBuilder builder, AirframeDevice arc186) 
            {
                _arc186 = arc186;
                _builder = builder;
            }

            public void TuneTo(string freq)
            {
                if (double.TryParse(freq, out double val))
                {
                    int val010MHz = (int)(val / 10.0);
                    SetFreqWheel1(val010MHz);
                    val -= val010MHz * 10.0;

                    int val001MHz = (int)(val);
                    SetFreqWheel2(val001MHz);
                    val = Math.Round((val - val001MHz) * 1000.0, 0);

                    int val100KHz = (int)(val / 100.0);
                    SetFreqWheel3(val100KHz);
                    val -= val100KHz * 100.0;

                    int val025KHz = (int)(val / 25.0) % 4;
                    SetFreqWheel4(val025KHz);
                }
            }

            public void SetPresetWheel(int val)
            {
                if (val < 1 || val > 20) 
                    throw new ArgumentOutOfRangeException(nameof(val));

                if (val < _presetWheel)
                    _builder.AddActions(_arc186, "VHFFM_PRESET_DN", _presetWheel - val);
                else if (val > _presetWheel)
                    _builder.AddActions(_arc186, "VHFFM_PRESET_UP", val - _presetWheel);
                _presetWheel = val;
            }

            private void SetFreqWheel1(int val)
            {
                if (val < 3 || val > 15) 
                    throw new ArgumentOutOfRangeException(nameof(val));

                if (val < _freqWheel1)
                    _builder.AddActions(_arc186, "VHFFM_FREQ1_DN", _freqWheel1 - val);
                else if (val > _freqWheel1)
                    _builder.AddActions(_arc186, "VHFFM_FREQ1_UP", val - _freqWheel1);
                _freqWheel1 = val;
            }

            private void SetFreqWheel2(int val)
            {
                if (val < 0 || val > 9) 
                    throw new ArgumentOutOfRangeException(nameof(val));

                if (val < _freqWheel2)
                    _builder.AddActions(_arc186, "VHFFM_FREQ2_DN", _freqWheel2 - val);
                else if (val > _freqWheel2)
                    _builder.AddActions(_arc186, "VHFFM_FREQ2_UP", val - _freqWheel2);
                _freqWheel2 = val;
            }

            private void SetFreqWheel3(int val)
            {
                if (val < 0 || val > 9) 
                    throw new ArgumentOutOfRangeException(nameof(val));

                if (val < _freqWheel3)
                    _builder.AddActions(_arc186, "VHFFM_FREQ3_DN", _freqWheel3 - val);
                else if (val > _freqWheel3)
                    _builder.AddActions(_arc186, "VHFFM_FREQ3_UP", val - _freqWheel3);
                _freqWheel3 = val;
            }

            private void SetFreqWheel4(int val)
            {
                if (val < 0 || val > 3) 
                    throw new ArgumentOutOfRangeException(nameof(val));

                if (val < _freqWheel4)
                    _builder.AddActions(_arc186, "VHFFM_FREQ4_DN", _freqWheel4 - val);
                else if (val > _freqWheel4)
                    _builder.AddActions(_arc186, "VHFFM_FREQ4_UP", val - _freqWheel4);
                _freqWheel4 = val;
            }
        }
    }
}
