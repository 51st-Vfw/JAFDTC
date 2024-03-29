﻿// ********************************************************************************************************************
//
// RadioSystem.cs -- f-15e radio system
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

using JAFDTC.Models.Base;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace JAFDTC.Models.F15E.Radio
{
    /// <summary>
    /// enum encoding radios in the mudhen.
    /// </summary>
    public enum Radios
    {
        COMM1 = 0,      // UHF AN/ARC-164
        COMM2 = 1,      // UHF/VHF AN/ARC-210
    };

    /// <summary>
    /// mudhen radio system configuration using a custom mudhen radio preset (RadioPreset) along with the base radio
    /// system (RadioSystemBase). the modeled configuration includes guard monitoring, preset/frequency mode, and
    /// default tuning.
    /// </summary>
    public class RadioSystem : RadioSystemBase<RadioPreset>, ISystem
    {
        public const string SystemTag = "JAFDTC:F15E:RADIO";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public bool IsCOMM1MonitorGuard { get; set; }

        public bool IsCOMM2MonitorGuard { get; set; }

        public bool IsCOMM1PresetMode { get; set; }

        public bool IsCOMM2PresetMode { get; set; }

        public string COMM1DefaultTuning { get; set; }

        public string COMM2DefaultTuning { get; set; }

        // ---- public properties, computed

        public override bool IsDefault
        {
            get
            {
                foreach (ObservableCollection<RadioPreset> presets in Presets)
                {
                    if (presets.Count > 0)
                    {
                        return false;
                    }
                }
                return string.IsNullOrEmpty(COMM1DefaultTuning) &&
                       string.IsNullOrEmpty(COMM2DefaultTuning) &&
                       !IsCOMM1PresetMode &&
                       !IsCOMM2PresetMode &&
                       !IsCOMM1MonitorGuard &&
                       !IsCOMM2MonitorGuard;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public RadioSystem()
        {
            Presets = new ObservableCollection<ObservableCollection<RadioPreset>>
            {
                new(),
                new()
            };
            IsCOMM1PresetMode = false;
            IsCOMM2PresetMode = false;
            IsCOMM1MonitorGuard = false;
            IsCOMM2MonitorGuard = false;
            COMM1DefaultTuning = "";
            COMM2DefaultTuning = "";
        }

        public RadioSystem(RadioSystem other)
        {
            Presets = new ObservableCollection<ObservableCollection<RadioPreset>>();
            foreach (ObservableCollection<RadioPreset> radio in other.Presets)
            {
                ObservableCollection<RadioPreset> newPresets = new();
                foreach (RadioPreset preset in radio)
                {
                    RadioPreset newPreset = new()
                    {
                        Preset = preset.Preset,
                        Frequency = preset.Frequency,
                        Description = preset.Description,
                    };
                    newPresets.Add(newPreset);
                }
                Presets.Add(newPresets);
            }
            IsCOMM1PresetMode = other.IsCOMM1PresetMode;
            IsCOMM2PresetMode = other.IsCOMM2PresetMode;
            IsCOMM1MonitorGuard = other.IsCOMM1MonitorGuard;
            IsCOMM2MonitorGuard = other.IsCOMM2MonitorGuard;
            COMM1DefaultTuning = new(other.COMM1DefaultTuning);
            COMM2DefaultTuning = new(other.COMM2DefaultTuning);
        }

        public virtual object Clone() => new RadioSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // Methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the instance to defaults by clearing all presets from all radios.
        /// </summary>
        public override void Reset()
        {
            foreach (ObservableCollection<RadioPreset> radio in Presets)
            {
                radio.Clear();
            }
            IsCOMM1PresetMode = false;
            IsCOMM2PresetMode = false;
            IsCOMM1MonitorGuard = false;
            IsCOMM2MonitorGuard = false;
            COMM1DefaultTuning = "";
            COMM2DefaultTuning = "";
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public bool ImportFromCSV(Radios radio, string filename)
        {
            // TODO: implement radio import
            return true;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public bool ExportToCSV(Radios radio, string filename)
        {
            // TODO: implement radio export
            return true;
        }
    }
}
