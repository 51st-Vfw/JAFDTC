﻿// ********************************************************************************************************************
//
// TGPSystem.cs -- a-10c tgp system
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023 ilominar/raven
// Copyright(C) 2024 fizzle
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

using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.A10C.TGP
{
    // defines the coordinate display options
    //
    public enum CoordDisplayOptions
    {
        LL = 0,
        MGRS = 1,
        OFF = 2
    }

    // defines the video mode options
    //
    public enum VideoModeOptions
    {
        CCD = 0,
        WHOT = 1,
        BHOT = 2
    }

    // defines the yardstick unit options
    //
    public enum YardstickOptions
    {
        METRIC = 0,
        USA = 1,
        OFF = 2
    }

    // defines the laser designator options
    //
    public enum LaserDesignatorOptions
    {
        LASER = 0, // laser only designation
        IR = 1,    // infrared only designation 
        BTH = 2    // both laser and infrared designation
    }

    public class TGPSystem : SystemBase
    {
        public const string SystemTag = "JAFDTC:A10C:TGP";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- properties that post change and validation events

        private string _coordDisplay;                              // integer [0, 2]
        public string CoordDisplay
        {
            get => _coordDisplay;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _coordDisplay, value, error);
            }
        }

        private string _videoMode;                              // integer [0, 2]
        public string VideoMode
        {
            get => _videoMode;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _videoMode, value, error);
            }
        }

        private string _laserCode;                           // laser code, [1][1-7][1-8][1-8]
        public string LaserCode
        {
            get => _laserCode;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsRegexFieldValid(value, _laserCodeRegex))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _laserCode, value, error);
            }
        }
        private static readonly Regex _laserCodeRegex = new(@"^1[1-7][1-8][1-8]$");

        private string _LSS;                           // laser code, [1][1-7][1-8][1-8]
        public string LSS
        {
            get => _LSS;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsRegexFieldValid(value, _laserCodeRegex))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _LSS, value, error);
            }
        }

        private string _latch;                              // bool
        public string Latch
        {
            get => _latch;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsBooleanFieldValid(value)) ? null : "Invalid format";
                SetProperty(ref _latch, value, error);
            }
        }

        private string _taaf;                              // integer [0..65000]
        public string TAAF
        {
            get => _taaf;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 65000)) ? null : "Invalid format";
                SetProperty(ref _taaf, value, error);
            }
        }

        private string _frnd;                              // boolean
        public string FRND
        {
            get => _frnd;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsBooleanFieldValid(value)) ? null : "Invalid format";
                SetProperty(ref _frnd, value, error);
            }
        }

        private string _yardstick;                              // integer [0, 2]
        public string Yardstick
        {
            get => _yardstick;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2) ? null : "Invalid format");
                SetProperty(ref _yardstick, value, error);
            }
        }

        private string _laserDesignator;                              // integer [0, 2]
        public string LaserDesignator
        {
            get => _laserDesignator;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2) ? null : "Invalid format");
                SetProperty(ref _laserDesignator, value, error);
            }
        }

        // ---- synthesized properties

        [JsonIgnore]
        public override bool IsDefault => CoordDisplayIsDefault && VideoModeIsDefault && LaserCodeIsDefault && LSSIsDefault && LatchIsDefault
            && TAAFIsDefault && FrndIsDefault && YardstickIsDefault && LaserDesignatorIsDefault;

        [JsonIgnore]
        public bool CoordDisplayIsDefault => string.IsNullOrEmpty(CoordDisplay) || CoordDisplay == ExplicitDefaults.CoordDisplay;
        [JsonIgnore]
        public int CoordDisplayValue => string.IsNullOrEmpty(CoordDisplay) ? int.Parse(ExplicitDefaults.CoordDisplay) : int.Parse(CoordDisplay);

        [JsonIgnore]
        public bool VideoModeIsDefault => string.IsNullOrEmpty(VideoMode) || VideoMode == ExplicitDefaults.VideoMode;
        [JsonIgnore]
        public int VideoModeValue => string.IsNullOrEmpty(VideoMode) ? int.Parse(ExplicitDefaults.VideoMode) : int.Parse(VideoMode);

        [JsonIgnore]
        public bool LaserCodeIsDefault => string.IsNullOrEmpty(LaserCode) || LaserCode == ExplicitDefaults.LaserCode;

        [JsonIgnore]
        public bool LSSIsDefault => string.IsNullOrEmpty(LSS) || LSS == ExplicitDefaults.LSS;

        [JsonIgnore]
        public bool LatchIsDefault => string.IsNullOrEmpty(Latch) || Latch == ExplicitDefaults.Latch;
        [JsonIgnore]
        public bool LatchValue => string.IsNullOrEmpty(Latch) ? true : Latch == ExplicitDefaults.Latch;

        [JsonIgnore]
        public bool TAAFIsDefault => string.IsNullOrEmpty(TAAF) || TAAF == ExplicitDefaults.TAAF;
        [JsonIgnore]
        public int TAAFValue => string.IsNullOrEmpty(TAAF) ? int.Parse(ExplicitDefaults.TAAF) : int.Parse(TAAF);

        [JsonIgnore]
        public bool FrndIsDefault => string.IsNullOrEmpty(FRND) || FRND == ExplicitDefaults.FRND;
        [JsonIgnore]
        public bool FRNDValue => string.IsNullOrEmpty(FRND) ? true : FRND == ExplicitDefaults.FRND;

        [JsonIgnore]
        public bool YardstickIsDefault => string.IsNullOrEmpty(Yardstick) || Yardstick == ExplicitDefaults.Yardstick;
        [JsonIgnore]
        public int YardstickValue => string.IsNullOrEmpty(Yardstick) ? int.Parse(ExplicitDefaults.Yardstick) : int.Parse(Yardstick);

        [JsonIgnore]
        public bool LaserDesignatorIsDefault => string.IsNullOrEmpty(LaserDesignator) || LaserDesignator == ExplicitDefaults.LaserDesignator;
        [JsonIgnore]
        public int LaserDesignatorValue => string.IsNullOrEmpty(LaserDesignator) ? int.Parse(ExplicitDefaults.LaserDesignator) : int.Parse(LaserDesignator);

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public TGPSystem()
        {
            Reset();
        }

        public TGPSystem(TGPSystem other)
        {
            CoordDisplay = other.CoordDisplay;
            VideoMode = other.VideoMode;
            LaserCode = other.LaserCode;
            LSS = other.LSS;
            Latch = other.Latch;
            TAAF = other.TAAF;
            FRND = other.FRND;
            Yardstick = other.Yardstick;
            LaserDesignator = other.LaserDesignator;
        }

        public virtual object Clone() => new TGPSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // member methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override void Reset()
        {
            Reset(this);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // static members
        //
        // ------------------------------------------------------------------------------------------------------------

        private static TGPSystem _default;
        public static TGPSystem ExplicitDefaults
        {
            get
            {
                if (_default == null)
                {
                    _default = new TGPSystem();
                    Reset(_default);
                }
                return _default;
            }
        }

        private static void Reset(TGPSystem tgp)
        {
            tgp.CoordDisplay = "0"; // LL
            tgp.VideoMode = "0";    // CCD
            tgp.LaserCode = "1688";
            tgp.LSS = "1688";
            tgp.Latch = "True";     // ON
            tgp.TAAF = "0";
            tgp.FRND = "True";      // ON
            tgp.Yardstick = "0";    // METRIC
            tgp.LaserDesignator = "0"; // LASER
        }
    }
}
