// ********************************************************************************************************************
//
// ConfigurationBase.cs -- abstract base class for airframe configuration
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023-2026 ilominar/raven
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

using JAFDTC.Kneeboard.Generate;
using JAFDTC.Kneeboard.Models;
using JAFDTC.Models.A10C;
using JAFDTC.Models.Base;
using JAFDTC.Models.Core;
using JAFDTC.Models.CoreApp;
using JAFDTC.Models.F14AB;
using JAFDTC.Models.F15E;
using JAFDTC.Models.F16C;
using JAFDTC.Models.FA18C;
using JAFDTC.Models.Planning;
using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;

using static JAFDTC.Models.IConfiguration;

namespace JAFDTC.Models
{
    /// <summary>
    /// abstract base class for an object that carries configuration information for an airframe. derived classes
    /// must provide implementations for a number of abstract methods (too many to fit in this margin).
    /// </summary>
    public abstract class ConfigurationBase : IConfiguration, INotifyPropertyChanged
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // constants
        //
        // ------------------------------------------------------------------------------------------------------------

        // default options when serializing configurations/systems to json.
        //
        public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public string Version { get; set; }

        public AirframeTypes Airframe { get; private set; }

        public string UID { get; protected set; }

        public string Filename { get; set; }

        private bool _isFavorite;
        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (_isFavorite != value)
                {
                    _isFavorite = value;
                    FavoriteGlyphUI = (_isFavorite) ? "\xE735" : "";
                }
            }
        }

        public Dictionary<string, string> LinkedSysMap { get; private set; }

        public int LastSystemEdited { get; set; }

        public MapFilterSpec LastMapFilter { get; set; }

        public MapImportSpec LastMapMarkerImport { get; set; }

        // ---- public properties, posts change/validation events

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        private string _favoriteGlyphUI;
        [JsonIgnore]
        public string FavoriteGlyphUI
        {
            get => _favoriteGlyphUI;
            set
            {
                if (_favoriteGlyphUI != value)
                {
                    _favoriteGlyphUI = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        private string _updatesInfoTextUI;
        [JsonIgnore]
        public string UpdatesInfoTextUI
        {
            get => _updatesInfoTextUI;
            set
            {
                if (_updatesInfoTextUI != value)
                {
                    _updatesInfoTextUI = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        private string _updatesIconsUI;
        [JsonIgnore]
        public string UpdatesIconsUI
        {
            get => _updatesIconsUI;
            set
            {
                if (_updatesIconsUI != value)
                {
                    _updatesIconsUI = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        private string _updatesIconBadgesUI;
        [JsonIgnore]
        public string UpdatesIconBadgesUI
        {
            get => _updatesIconBadgesUI;
            set
            {
                if (_updatesIconBadgesUI != value)
                {
                    _updatesIconBadgesUI = value;
                    OnPropertyChanged();
                }
            }
        }

        // ---- properties, virtual

        [JsonIgnore]
        public virtual List<string> MergeableSysTags => [ ];

        [JsonIgnore]
        public virtual IUploadAgent UploadAgent { get; }

        // ---- properties, private

        private const string JAFDTCConfigMergeMutexName = "JAFDTC_ConfigMergeMutex";

        private static readonly Mutex MutexJAFDTC = new(false, JAFDTCConfigMergeMutexName);

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        [JsonConstructor]
        public ConfigurationBase(string version, AirframeTypes airframe, string uid, string name,
                                 Dictionary<string, string> linkedSysMap)
            => (Version, Airframe, UID, Name, LinkedSysMap) = (version, airframe, uid, name, linkedSysMap);

        // NOTE: when cloning, derived classes must call ResetUID() on the clone prior to returning the new instance.
        //
        public abstract IConfiguration Clone();

        public abstract void CloneSystemFrom(string systemTag, IConfiguration other);

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event ConfigurationSavedEventHandler ConfigurationSaved;
        protected virtual void OnConfigurationSaved(object invokedBy = null, string systemTagHint = null)
        {
            ConfigurationSaved?.Invoke(this, new ConfigurationSavedEventArgs(invokedBy, this, systemTagHint));
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // system merge support, virtual methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// merge configuration into the dcs dtc json "data" object and return the updated object. this method may
        /// update the object in-place.
        /// </summary>
        protected virtual JsonNode MergeConfigToSimDTC(JsonNode dataRoot, CoreSimDTCSystem dtcSys)
        {
            foreach (string tag in MergeableSysTags)
            {
                ISystem system = SystemForTag(tag);
                if (dtcSys.MergedSystemTags.Contains(tag) && !system.IsDefault)
                    dataRoot = system.MergeIntoSimDTC(dataRoot);
            }
            return dataRoot;
        }

        /// <summary>
        /// merge configuration into the planning mission data structure and return the updated mission. this method
        /// may update mission in-place.
        /// </summary>
        protected virtual Mission MergeConfigToMission(Mission mission)
        {
            foreach (string tag in MergeableSysTags)
            {
                ISystem system = SystemForTag(tag);
                if (!system.IsDefault)
                    mission = system.MergeIntoMission(mission);
            }
            return mission;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // system merge support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// persist the merged configuration into the dcs dtc file at the output path. the dcs dtc file is built by
        /// merging the configuration of mergable systems into the base template. returns true on success, false on
        /// failure.
        /// </summary>
        protected bool SaveMergedSimDTC(CoreSimDTCSystem dtcSys)
        {
            if (!string.IsNullOrEmpty(dtcSys.OutputPath))
            {
                try
                {
                    string name = Path.GetFileNameWithoutExtension(dtcSys.OutputPath);
                    string json = FileManager.LoadDTCTemplate(Airframe, dtcSys.Template)
                        ?? throw new Exception($"Cannot load DTC template {dtcSys.Template}");
                    JsonNode dom = JsonNode.Parse(json)
                        ?? throw new Exception($"Cannot parse DTC template {dtcSys.Template}");

                    dom["name"] = name;
                    dom["data"] = MergeConfigToSimDTC(dom["data"], dtcSys);
                    dom["data"]["name"] = name;

                    json = dom.ToJsonString(Globals.JSONOptions)
                        ?? throw new Exception($"Cannot create DTC file");
                    FileManager.WriteFile(dtcSys.OutputPath, json);

                    FileManager.Log($"Successfully merged \"{Name}\" into {dtcSys.OutputPath}");
                }
                catch (Exception ex)
                {
                    FileManager.Log($"Configuration:SaveMergedSimDTC exception {ex}");
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// merge data from the configuration into kneeboard template(s) to build a set of kneeboards at the output
        /// path. returns true on success, false on failure.
        /// </summary>
        protected bool SaveMergedKboards(CoreKboardSystem kbSys)
        {
            if (!string.IsNullOrEmpty(kbSys.OutputPath))
            {
                try
                {
                    // build mission data from the information in the configuration, starting from a skeleton plan.
                    // add a single pilot to flights which did not build out pilots during the merge pass.
                    //
                    Mission mission = MergeConfigToMission(new()
                    {
                        Name = "Untitled",
                        Theater = "Unknown",
                        Owner = new Ownship() {
                            Name = Settings.Callsign
                        },
                        Packages = [
                            new Package() {
                                Name = "Alpha",
                                Flights = [
                                    new Flight() {
                                        Name = "VENOM1",
                                        Aircraft = Globals.AirframeDTCTypes[Airframe],
                                        Pilots = [ ]
                                    }
                                ]
                            }
                        ]
                    });
                    foreach (Package package in mission.Packages)
                        foreach (Flight flight in package.Flights)
                            if (flight.Pilots.Count == 0)
                                flight.Pilots = [ new Pilot() { Name = Settings.Callsign, Position = 1 } ];

                    // unpack the template .zip capturing the .svg files in a temp directory we can point the
                    // builder at below. only build the kneeboards the user is asking to generate.
                    //
                    List<string> paths = FileManager.ExtractKBTemplatePackage(Airframe, kbSys.Template,
                                                                              [.. kbSys.KneeboardTags ]);
                    if (paths.Count == 0)
                        throw new Exception("LoadKboardTemplate returns no templates");

                    // create the kneeboard builder, build criteria, and let it do the thing. dump the created
                    // paths to the log for posterity.
                    //
                    Generate builder = new();
                    GenerateCriteria criteria = new()
                    {
                        PathTemplates = Path.GetDirectoryName(paths[0]),
                        PathOutput = kbSys.OutputPath,
                        Mission = mission,
                        IsNightMode = kbSys.EnableNightValue,
                        IsSVGMode = kbSys.EnableSVGValue
                    };
                    IReadOnlyList<string> kbPaths = builder.GenerateKneeboards(criteria);
                    foreach (string path in kbPaths)
                        FileManager.Log($"Successfully generated kneeboard {path}");
                }
                catch (Exception ex)
                {
                    FileManager.Log($"Configuration:SaveMergedSimKboards exception {ex}");
                    return false;
                }
            }
            return true;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IConfiguration methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public void ResetUID()
        {
            UID = Guid.NewGuid().ToString();
        }

        public bool MergeMutexAcquire()
        {
            bool haveMutex = MutexJAFDTC.WaitOne(100);
            if (!haveMutex)
                FileManager.Log("ConfigurationBase:MergeMutexAcquire cannot acquire mutex");
            return haveMutex;
        }

        public void MergeMutexRelease()
        {
            MutexJAFDTC.ReleaseMutex();
        }

        public void Sanitize(bool isResetUID = false)
        {
            UID = null;
            Filename = null;
            IsFavorite = false;
            UnlinkSystem(null);
            LastSystemEdited = 0;
            LastMapFilter = null;
            LastMapMarkerImport = null;

            if (isResetUID)
                ResetUID();
        }

        public virtual string RoleHelpText() => null;

        public virtual bool ValidateRole(string role) => false;

        public virtual void AdjustForRole(string role) { }

        public virtual ISystem SystemForTag(string tag) => null;

        public bool IsDefault(string systemTag)
        {
            ISystem system = SystemForTag(systemTag);
            return system == null || system.IsDefault;
        }

        public void LinkSystemTo(string systemTag, IConfiguration linkedConfig)
        {
            LinkedSysMap ??= [ ];
            LinkedSysMap[systemTag] = linkedConfig.UID;
            CloneSystemFrom(systemTag, linkedConfig);
            ConfigurationUpdated();
        }

        public void UnlinkSystem(string systemTag)
        {
            if ((LinkedSysMap != null) && (systemTag == null))
            {
                LinkedSysMap.Clear();
                ConfigurationUpdated();
            }
            else if ((LinkedSysMap != null) && (LinkedSysMap.ContainsKey(systemTag)))
            {
                LinkedSysMap.Remove(systemTag);
                ConfigurationUpdated();
            }
        }

        public void Save(object invokedBy = null, string syncSysTag = null)
        {
            FileManager.SaveConfigurationFile(this);
            ConfigurationUpdated();
            OnConfigurationSaved(invokedBy, syncSysTag);
        }

        public virtual bool SaveMergedSimDTC() => true;

        public virtual bool SaveMergedKboards() => true;

        public string SystemLinkedTo(string systemTag)
        {
            return ((LinkedSysMap != null) && LinkedSysMap.TryGetValue(systemTag, out string value)) ? value : null;
        }

        public bool IsLinked(string systemTag) => !string.IsNullOrEmpty(SystemLinkedTo(systemTag));

        public void CleanupSystemLinks(List<string> validUIDs)
        {
            List<string> invalidSystems = [ ];
            foreach (KeyValuePair<string, string> kvp in LinkedSysMap)
                if (!validUIDs.Contains(kvp.Value))
                    invalidSystems.Add(kvp.Key);
            foreach (string system in invalidSystems)
                LinkedSysMap.Remove(system);
        }

        public abstract void ConfigurationUpdated();

        public abstract string Serialize(string systemTag = null);

        public abstract bool Deserialize(string systemTag, string json);

        public abstract void AfterLoadFromJSON();

        public virtual void AfterSystemEditorCompletes(string systemTag) { }

        public abstract bool CanAcceptPasteForSystem(string cboardTag, string systemTag = null);

        // ------------------------------------------------------------------------------------------------------------
        //
        // factories
        //
        // ------------------------------------------------------------------------------------------------------------

        // factory to create a new configuration instance of the proper type bassed on airframe. the configuration is
        // set up with avionics defaults.
        //
        static public IConfiguration Factory(AirframeTypes airframe, string name)
        {
            return airframe switch
            {
                AirframeTypes.A10C  => new A10CConfiguration(Guid.NewGuid().ToString(), name, [ ]),
                AirframeTypes.F14AB => new F14ABConfiguration(Guid.NewGuid().ToString(), name, [ ]),
                AirframeTypes.F15E  => new F15EConfiguration(Guid.NewGuid().ToString(), name, [ ]),
                AirframeTypes.F16C  => new F16CConfiguration(Guid.NewGuid().ToString(), name, [ ]),
                AirframeTypes.FA18C => new FA18CConfiguration(Guid.NewGuid().ToString(), name, [ ]),
                _                   => null,
            };
        }

        // factory to create a new configuration instance of the proper type bassed on airframe. the configuration is
        // set up from json representing a serialized configuration instance. the name can be replaced with the given
        // name parameter. returns null on error.
        //
        static public IConfiguration FactoryJSON(AirframeTypes airframe, string json, string name = null)
        {
            IConfiguration config;
            try
            {
                config = airframe switch
                {
                    AirframeTypes.A10C  => JsonSerializer.Deserialize<A10CConfiguration>(json),
                    AirframeTypes.F14AB => JsonSerializer.Deserialize<F14ABConfiguration>(json),
                    AirframeTypes.F15E  => JsonSerializer.Deserialize<F15EConfiguration>(json),
                    AirframeTypes.F16C  => JsonSerializer.Deserialize<F16CConfiguration>(json),
                    AirframeTypes.FA18C => JsonSerializer.Deserialize<FA18CConfiguration>(json),
                    _                   => null,
                };
            }
            catch (Exception ex)
            {
                FileManager.Log($"Configuration:FactoryJSON exception {ex}");
                config = null;
            }
            if ((config != null) && !string.IsNullOrEmpty(name))
            {
                // if we are changing the name, null-out the filename to make sure the new configuration gets its own
                // unique filename once persisted and doesn't over-write an existing file we may have cloned from.
                //
                config.Name = name;
                config.Filename = null;
            }
            config?.AfterLoadFromJSON();
            return config;
        }
    }
}
