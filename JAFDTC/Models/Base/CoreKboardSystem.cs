// ********************************************************************************************************************
//
// SimKboardSystem.cs : simulator kneeboard system
//
// Copyright(C) 2026 ilominar/raven
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

using JAFDTC.Models.Core;
using JAFDTC.Utilities;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.Base
{
    /// <summary>
    /// class to capture the settings of the kneeboard generation "system". this serves as a helper to build
    /// kneeboards from system setups. this system is airframe-agnostic
    /// </summary>
    public partial class SimKboardSystem : SystemBase
    {
        public const string SystemTag = "JAFDTC:Generic:Kboard";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change or validation events.

        private string _template;
        public string Template
        {
            get => _template;
            set => SetProperty(ref _template, value, null);
        }

        private string _outputPath;
        public string OutputPath
        {
            get => _outputPath;
            set => SetProperty(ref _outputPath, value, null);
        }

        public ObservableCollection<string> KneeboardTags { get; set; }

        private string _enableRebuild;
        public string EnableRebuild
        {
            get => _enableRebuild;
            set => ValidateAndSetBoolProp(value, ref _enableRebuild);
        }

        private string _enableNight;
        public string EnableNight
        {
            get => _enableNight;
            set => ValidateAndSetBoolProp(value, ref _enableNight);
        }

        private string _enableSVG;
        public string EnableSVG
        {
            get => _enableSVG;
            set => ValidateAndSetBoolProp(value, ref _enableSVG);
        }

        /// <summary>
        /// returns true if the instance indicates a default setup, false otherwise.
        /// </summary>
        [JsonIgnore]
        public override bool IsDefault => ((Template.Length == 0) &&
                                           (OutputPath.Length == 0) &&
                                           (KneeboardTags.Count == 0) &&
                                           (EnableRebuildValue == false) &&
                                           (EnableNightValue == false) &&
                                           (EnableSVGValue == false));

        // ---- following accessors get the current value (default or non-default) for various properties

        [JsonIgnore]
        public bool EnableRebuildValue => !string.IsNullOrEmpty(EnableRebuild) && bool.Parse(EnableRebuild);

        [JsonIgnore]
        public bool EnableNightValue => !string.IsNullOrEmpty(EnableNight) && bool.Parse(EnableNight);

        [JsonIgnore]
        public bool EnableSVGValue => !string.IsNullOrEmpty(EnableSVG) && bool.Parse(EnableSVG);

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public SimKboardSystem()
        {
            Template = "";
            OutputPath = "";
            EnableRebuild = false.ToString();
            EnableNight = false.ToString();
            EnableSVG = false.ToString();
            KneeboardTags = [ ];
        }

        public SimKboardSystem(SimKboardSystem other)
        {
            Template = new(other.Template);
            OutputPath = new(other.OutputPath);
            EnableRebuild = new(other.EnableRebuild);
            EnableNight = new(other.EnableNight);
            EnableSVG = new(other.EnableSVG);
            KneeboardTags = [ ];
        }

        public virtual object Clone() => new SimKboardSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// validates the Template and OutputPath fields are valid. if not, they are reset to default values.
        /// </summary>
        public void ValidateForAirframe(AirframeTypes airframe)
        {
            if (!FileManager.IsUniqueKBTemplatePackage(airframe, Template))
                Template = "";
            if (!string.IsNullOrEmpty(OutputPath))
            {
                string path = Path.GetDirectoryName(OutputPath);
                if (!Directory.Exists(path))
                    OutputPath = "";
            }
        }

        /// <summary>
        /// reset the instance to defaults.
        /// </summary>
        public override void Reset()
        {
            Template = "";
            OutputPath = "";
            EnableRebuild = false.ToString();
            EnableSVG = false.ToString();
            EnableNight = false.ToString();
            KneeboardTags.Clear();
        }
    }
}
