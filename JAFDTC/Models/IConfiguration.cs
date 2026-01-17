// ********************************************************************************************************************
//
// IConfiguration.cs -- interface for airframe configuration class
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

using JAFDTC.Models.Core;
using JAFDTC.Models.CoreApp;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace JAFDTC.Models
{
    /// <summary>
    /// an interface to class that represents an avionics configuration for a particular airframe.
    /// </summary>
    public interface IConfiguration
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // events and handlers
        //
        // ------------------------------------------------------------------------------------------------------------

        public event ConfigurationSavedEventHandler ConfigurationSaved;
        public delegate void ConfigurationSavedEventHandler(object sender, ConfigurationSavedEventArgs args);

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// provides the current version of the configuration.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// provides the airframe type that the configuration targets.
        /// </summary>
        public AirframeTypes Airframe { get; }

        /// <summary>
        /// provides the unique identifier for the configuration.
        /// </summary>
        public string UID { get; }

        /// <summary>
        /// provides a file-system friendly name (see FileManager). this will not necessarily match Name and may be
        /// null if the configuration hasn't been persisted.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// boolen indicating wheter or not the configuration is marked as a "favorite".
        /// </summary>
        public bool IsFavorite { get; set; }

        /// <summary>
        /// provides the name of the configuration. this must be unique within an airframe (name comparisons are
        /// always case-insensitive).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// provides a map from a system tag (string) to a uid (string) that establishes a link between a system on
        /// this configuration and a different source configuration. linked systems will always track the setup of the
        /// system in the source configuration.
        /// </summary>
        public Dictionary<string, string> LinkedSysMap { get; }

        /// <summary>
        /// index of the system editor page last used. this property is used to persist ui state.
        /// </summary>
        public int LastSystemEdited { get; set; }

        /// <summary>
        /// last map filter applied to a map window used when editing the configuration.
        /// </summary>
        public MapFilterSpec LastMapFilter { get; set; }

        /// <summary>
        /// last import specification for file imported for map window markers used when editing the configuration.
        /// </summary>
        public MapImportSpec LastMapMarkerImport { get; set; }

        /// <summary>
        /// returns list of system tags for all systems that can be merged to build a dcs dtc tape or kneeboard.
        /// </summary>
        [JsonIgnore]
        public List<string> MergeableSysTags { get; }

        /// <summary>
        /// provides an instance of an upload agent object that implements IUploadAgent. this object handles creating
        /// a command set that will set up the jet (according to the configuration) and uploading it to the jet.
        /// </summary>
        [JsonIgnore]
        public IUploadAgent UploadAgent { get; }

        /// <summary>
        /// provides a Segoe Fluent Icon glyph to use to mark the favorite status of the configuration in the ui.
        /// </summary>
        [JsonIgnore]
        public string FavoriteGlyphUI { get; set; }

        /// <summary>
        /// provides a human-readable summary of which of the configuration's systems have been updated from their
        /// default setup for use in the ui.
        /// </summary>
        [JsonIgnore]
        public string SystemInfoTextUI { get; set; }

        /// <summary>
        /// provides a string of Segoe Fluent Icon glyphs representing the systems that have been updated from their
        /// default setup for use in the ui.
        /// </summary>
        [JsonIgnore]
        public string SystemInfoIconsUI { get; set; }

        /// <summary>
        /// provides a string of Segoe Fluent Icon glyphs representing the badges to apply to the icons from
        /// SystemInfoIconsUI for use in the ui.
        /// </summary>
        [JsonIgnore]
        public string SystemInfoIconBadgesUI { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns a deep copy of the configuration. the uid of the clone is updated via ResetUID() to ensure that
        /// the uid of the configuration clone is unique.
        /// </summary>
        public IConfiguration Clone();

        /// <summary>
        /// acquire the configuration merge mutex. this mutex should be held before backgrounding any configuration
        /// merge operations via SaveMergedSimDTC or SaveMergedKboards.
        /// </summary>
        public bool MergeMutexAcquire();

        /// <summary>
        /// release the configuration merge mutex. this mutex should be released after the merge finishes when
        /// holding the merge mutex.
        /// </summary>
        public void MergeMutexRelease();

        /// <summary>
        /// sanatize a configuration instance by clearing UID, Filename, IsFavorite, LinkedSysMap, LastSystemEdited
        /// properties. typically this is done on a Clone() of a configuration that is to be exported. uid may be
        /// optionally reset (note a sanitized config cannot be used until uid is reset).
        /// </summary>
        public void Sanitize(bool isReestUID = false);

        /// <summary>
        /// reset the uid of the configuration.
        /// 
        /// NOTE: use this method with caution. it can break links if not used carefully.
        /// </summary>
        public void ResetUID();

        /// <summary>
        /// returns help text for valid role specifications, null if configuration does not support role adjustemnt.
        /// </summary>
        public string RoleHelpText();

        /// <summary>
        /// returns true if the role string is valid, false otherwise. validating an empty string should return true
        /// if the configuration supports role adjustment, false otherwise.
        /// </summary>
        public bool ValidateRole(string role);

        /// <summary>
        /// adjust the systems in the configuration for a given role. along with the specific adjustments, the
        /// information in the role is airframe-specific and may include things like slot, callsigns, etc.
        /// </summary>
        public void AdjustForRole(string role);

        /// <summary>
        /// returns the system associated with the given tag in the configuration, null if there is no such system.
        /// </summary>
        public ISystem SystemForTag(string tag);

        /// <summary>
        /// returns true if the system with specified tag is default, false otherwise.
        /// </summary>
        public bool IsDefault(string systemTag);

        /// <summary>
        /// update a system in this configuration to match the system in a target configuration. this method creates
        /// a deep copy of the system.
        /// </summary>
        public void CloneSystemFrom(string systemTag, IConfiguration other);

        /// <summary>
        /// link the configuration of a system with the specified tag to the system in another configuration.
        /// </summary>
        public void LinkSystemTo(string systemTag, IConfiguration linkedConfig);

        /// <summary>
        /// unlink the configuration of a system with the specified tag. all systems are unlinked if tag is null.
        /// </summary>
        public void UnlinkSystem(string systemTag);

        /// <summary>
        /// return the uid for the configuration that the system with the specified tag is linked to, null if the
        /// system is not linked.
        /// </summary>
        public string SystemLinkedTo(string systemTag);

        /// <summary>
        /// returns true if the system with specified tag is linked to a different configuration, false otherwise.
        /// </summary>
        public bool IsLinked(string systemTag);

        /// <summary>
        /// clean up the system links in the configuration removing any stale links for the systems.
        /// </summary>
        public void CleanupSystemLinks(List<string> validUIDs);

        /// <summary>
        /// persist the configuration to storage, posting a ConfigurationSaved event with an argument set up
        /// appropriately. the invoked by parameter identifies the object that invoked the save. the sync system tag
        /// identifies the system that was updated and may need to be synchronized with configurations that link to
        /// this configuration (null tag indicates no systems should be sync'd).
        /// </summary>
        public void Save(object invokedBy = null, string syncSysTag = null);

        /// <summary>
        /// persist the merged configuration into the dcs dtc file according to internal dte system configuration.
        /// this should do nothing (and indicate success) if the dte configuration is default or dte is not
        /// supported. returns true on success, false on error.
        /// 
        /// this method typically invokes the protected ConfigurationBase:SaveMergedSimDTC(...) method.
        /// </summary>
        public bool SaveMergedSimDTC();

        /// <summary>
        /// persist the merged configuration into kneeboards according to internal system configuration. this
        /// should do nothing (and indicate success) if the kneeboard configuration is default or kneeboards are
        /// not supported. returns true on success, false on error.
        /// 
        /// this method typically invokes the protected ConfigurationBase:SaveMergedboards(...) method.
        /// </summary>
        public bool SaveMergedKboards();

        /// <summary>
        /// updates the SystemInfoTextUI, SystemInfoIconsUI, and SystemInfoIconBadgesUI properties in response to an
        /// update to the configuration.
        /// </summary>
        public void ConfigurationUpdated();

        /// <summary>
        /// returns a json serialization of the entire configuration or a single system (identified by the system tag)
        /// from the Configuration. a null system tag requests a serialization of the entire configuration.
        /// </summary>
        public string Serialize(string systemTag = null);

        /// <summary>
        /// returns true if able to deserialize the json string into the configuration for a system with the specified
        /// system tag, false on error.
        /// </summary>
        public bool Deserialize(string systemTag, string json);

        /// <summary>
        /// called after deserializing a configuration from json to allow opportunities to update versions, etc.
        /// </summary>
        public void AfterLoadFromJSON();

        /// <summary>
        /// called after the indicated system editor has finished operating on the configuration.
        /// </summary>
        public void AfterSystemEditorCompletes(string systemTag);

        /// <summary>
        /// returns true if the given clipboard content can be consumed by the configuration or system (as identified
        /// by a non-null tag), false otherwise. valid clipboard content is text, starting with a system tag on the
        /// first line, followed by json serialized objects.
        /// </summary>
        public bool CanAcceptPasteForSystem(string cboardTag, string systemTag = null);
    }
}
