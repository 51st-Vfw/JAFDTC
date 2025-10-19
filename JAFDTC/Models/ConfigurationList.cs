// ********************************************************************************************************************
//
// ConfigurationListModel.cs -- model for a list of configurations for a particualr airframe.
//
// Copyright(C) 2023-2025 ilominar/raven
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace JAFDTC.Models
{
    /// <summary>
    /// model object to manage a list of configurations for a specific airframe. the class supports a name-based
    /// filter to select only a subset of known configurations.
    /// </summary>
    public class ConfigurationList
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public ObservableCollection<IConfiguration> ConfigsFiltered { get; private set; }

        public Dictionary<string, IConfiguration> UIDtoConfigMap { get; private set; }

        public AirframeTypes Airframe { get; private set; }
        
        // ---- private properties

        private List<IConfiguration> Configs { get; set; }

        private string CurFilterText { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public ConfigurationList(AirframeTypes airframe)
        {
            Airframe = airframe;
            Configs = [ ];
            ConfigsFiltered = [ ];
            UIDtoConfigMap = [ ];
            CurFilterText = "";
            LoadConfigurationsForAirframe(airframe);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// walk through all configurations looking for a those configurations that link to the specified system in the
        /// source configuration. updates the list with all such configurations. note that this is recursive, so if
        /// A --> B and B --> C, then A --> C. 
        /// </summary>
        private List<IConfiguration> FindConfigsLinking(IConfiguration srcConfig, string systemTag)
        {
            List<IConfiguration> listLinks = [ ];
            foreach (IConfiguration config in Configs)
                if (config.LinkedSysMap.TryGetValue(systemTag, out string value) &&
                    (value == srcConfig.UID) &&
                    (config.UID != srcConfig.UID))
                {
                    listLinks.Add(config);
                }

            List<IConfiguration> list = [ ];
            foreach (IConfiguration link in listLinks)
            {
                if (!list.Contains(link))
                    list.Add(link);
                List<IConfiguration> listChild = FindConfigsLinking(link, systemTag);
                foreach (IConfiguration child in listChild)
                    if (!list.Contains(child))
                        list.Add(child);
            }
            return list;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void ConfigurationSavedHandler(object sender, ConfigurationSavedEventArgs args)
        {
            if (args.SyncSysTag != null)
            {
                // Debug.WriteLine("--------");
                List<IConfiguration> list = FindConfigsLinking(args.Config, args.SyncSysTag);
                foreach (IConfiguration config in list)
                {
                    // Debug.WriteLine($"push {args.SyncSysTag} from {args.Config.Name} --> {config.Name}");
                    config.CloneSystemFrom(args.SyncSysTag, args.Config);
                    config.Save(this);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// sort the current filtered list of configurations by name in place in the configs list. the sort is done in
        /// place to avoid changing the ConfigsFiltered instance in case any ui elements bind to it.
        /// </summary>
        private void SortConfigsFiltered()
        {
            var sortableList = new List<IConfiguration>(ConfigsFiltered);
            sortableList.Sort((a, b)
                => (a.IsFavorite == b.IsFavorite) ? a.Name.CompareTo(b.Name) : ((a.IsFavorite) ? -1 : 1));
            for (int i = 0; i < sortableList.Count; i++)
                ConfigsFiltered.Move(ConfigsFiltered.IndexOf(sortableList[i]), i);
        }

        /// <summary>
        /// returns true if the configuration name is unique (case-insensitive) across all known configurations;
        /// false otherwise.
        /// </summary>
        public bool IsNameUnique(string name)
        {
            foreach (IConfiguration config in Configs)
                if (config.Name.Equals(name, System.StringComparison.CurrentCultureIgnoreCase))
                    return false;
            return true;
        }

        /// <summary>
        /// returns a unique name based on the starting base name. if the base name is unique, we're done. if not,
        /// we add a sequence number to the end of the base until we're unique.
        /// </summary>
        public string UniquifyName(string baseName)
        {
            string uniqueName = baseName;
            int index = 1;
            while (!IsNameUnique(uniqueName))
                uniqueName = $"{baseName} {index++}";
            return uniqueName;
        }

        /// <summary>
        /// add a new configuration to the model by creating a new configuration instance for the given airframe
        /// and saving it to the filesystem.
        ///
        /// this method implicitly clears any filtering that is in place so we don't have configurations
        /// disappearing from the ui on creates.
        /// </summary>
        public IConfiguration Create(AirframeTypes airframe, string name)
        {
            IConfiguration config = Configuration.Factory(airframe, name);
            config.ConfigurationSaved += ConfigurationSavedHandler;
            config.Save(this);
            Configs.Add(config);
            UIDtoConfigMap[config.UID] = config;
            FilterConfigs(null);
            return config;
        }

        /// <summary>
        /// inject a configuration from an external source into the system. here, "external" implies that the
        /// configuration is not currently part of the config database the FileManager maintains. as injected
        /// configurations are external, before handling, their uids are reset and their system linkages and
        /// filename is cleared. the injected configuration is then persisted via IConfiguration.Save().
        /// 
        /// if the configuration's airframe matches the airframe of this instance, we will also add the
        /// configuration to the list we track and set it up appropriately. in this case, any filtering is
        /// implicitly cleared so we don't have configurations disappearing from the ui on injects.
        /// 
        /// returns the configuration if it was added to this list, null if not.
        /// </summary>
        public IConfiguration Inject(IConfiguration config)
        {
            config.Sanitize(true);

            if (config.Airframe == Airframe)
            {
                config.ConfigurationSaved += ConfigurationSavedHandler;
                config.Save(this);
                Configs.Add(config);
                UIDtoConfigMap[config.UID] = config;
                FilterConfigs(null);
            }
            else
            {
                config.Save(this);
                config = null;
            }
            return config;
        }

        /// <summary>
        /// delete a configuration from the model by removing it from the colections and filesystem.
        /// </summary>
        public void Delete(IConfiguration config)
        {
            config.ConfigurationSaved -= ConfigurationSavedHandler;
            UIDtoConfigMap.Remove(config.UID);
            Configs.Remove(config);
            ConfigsFiltered.Remove(config);
            FileManager.DeleteConfigurationFile(config);
        }

        /// <summary>
        /// clone an existing configuration and add it to the configuration list under a new name. the caller must
        /// guarantee that the new name is unique prior to calling this method. returns the new config.
        /// 
        /// this method implicitly clears any filtering that is in place so we don't have configurations disappearing
        /// from the ui on copies.
        /// </summary>
        public IConfiguration Copy(IConfiguration config, string newName)
        {
            IConfiguration clone = config.Clone();
            clone.Name = newName;
            clone.Filename = null;
            clone.ConfigurationSaved += ConfigurationSavedHandler;
            clone.Save(this);
            Configs.Add(clone);
            UIDtoConfigMap[clone.UID] = clone;
            FilterConfigs(null);
            return clone;
        }

        /// <summary>
        /// rename a configuration by updating its name (and implicitly filename) and moving the backing file on the 
        /// filesystem. the caller must guarantee that the new name is unique prior to calling this method.
        ///
        /// this method implicitly clears any filtering that is in place so we don't have configurations disappearing
        /// from the ui on renames.
        /// </summary>
        public void Rename(IConfiguration config, string newName)
        {
            if (!config.Name.Equals(newName, System.StringComparison.CurrentCultureIgnoreCase))
            {
                string oldFilename = config.Filename;
                config.Name = newName;
                FileManager.RenameConfigurationFile(config, oldFilename);
                FilterConfigs(null);
            }
        }

        /// <summary>
        /// re-insert an item in the filtered configurations list to get thigns to redraw. note that the assumption
        /// here is that the ui is bound to the filtered list, not the complete list.
        /// 
        /// HACK: this is a total hack as bindings don't seem to work the way they are supposed to; e.g., as in
        /// HACK: https://stackoverflow.com/questions/59473945/update-display-of-one-item-in-a-listviews-observablecollection/59506197#59506197
        /// /// </summary>
        public void Reinsert(IConfiguration config)
        {
            if (ConfigsFiltered.IndexOf(config) != -1)
                ConfigsFiltered[ConfigsFiltered.IndexOf(config)] = config;
        }

        /// <summary>
        /// filter the configurations and update ConfigsFiltered/CurFilterText as necessary. to meet the filter
        /// constraint and appear in the filtered list, the configuration must contain the filter string in its name
        /// (comparison is case-insensitive). a filter of null forces the function to rebuild the filter list without
        /// updating the current filter text. filtered configurations are sorted by name.
        /// </summary>
        public void FilterConfigs(string filter = "", bool isForce = false)
        {
            if ((filter == null) || (filter != CurFilterText) || isForce)
            {
                ConfigsFiltered.Clear();
                foreach (IConfiguration config in Configs)
                    if (string.IsNullOrEmpty(filter) ||
                        config.Name.Contains(filter, System.StringComparison.CurrentCultureIgnoreCase))
                    {
                        ConfigsFiltered.Add(config);
                    }
                SortConfigsFiltered();
                if (filter != null)
                    CurFilterText = filter;
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public List<string> FilterHits(string filter)
        {
            List<string> hits = [ ];
            foreach (IConfiguration config in Configs)
                if (string.IsNullOrEmpty(filter) ||
                    config.Name.Contains(filter, System.StringComparison.CurrentCultureIgnoreCase))
                {
                    hits.Add(config.Name);
                }
            hits.Sort();
            return hits;
        }

        /// <summary>
        /// load the configurations for the specified airframe into the model, removing any previous contents first. 
        /// </summary>
        public void LoadConfigurationsForAirframe(AirframeTypes airframe)
        {
            foreach (IConfiguration config in Configs)
                config.ConfigurationSaved -= ConfigurationSavedHandler;
            Configs.Clear();
            ConfigsFiltered.Clear();
            UIDtoConfigMap.Clear();

            Dictionary<string, IConfiguration> fileDict = FileManager.LoadConfigurationFiles(airframe);
            List<string> uidBlacklist = [ ];
            foreach (KeyValuePair<string, IConfiguration> kvp in fileDict)
            {
                Debug.WriteLine($"{kvp.Key}: {kvp.Value.Name}");
                if (kvp.Value.UID == null)
                {
                    FileManager.Log($"Configuration {kvp.Value.Name} in {kvp.Value.Filename} has invalid UID");
                    continue;
                }

                kvp.Value.ConfigurationSaved += ConfigurationSavedHandler;
                Configs.Add(kvp.Value);
                if (UIDtoConfigMap.TryGetValue(kvp.Value.UID, out IConfiguration dupUIDConfig))
                {
                    // CYA: possible uids might be duplicated (eg, by directly moving around configuration .json
                    // CYA: files in the configuration area). this can move links around to different targets.
                    // CYA: to avoid this, we will reset uids when we run into duplicated uids. this will break
                    // CYA: links, but is preferable to links moving around to other configurations.

                    uidBlacklist.Add(kvp.Value.UID);

                    string dupUID = kvp.Value.UID;
                    while (UIDtoConfigMap.ContainsKey(dupUIDConfig.UID))
                        dupUIDConfig.ResetUID();
                    UIDtoConfigMap.Remove(dupUID);
                    UIDtoConfigMap[dupUIDConfig.UID] = dupUIDConfig;
                    dupUIDConfig.Save();

                    while (UIDtoConfigMap.ContainsKey(kvp.Value.UID))
                        kvp.Value.ResetUID();
                    UIDtoConfigMap[kvp.Value.UID] = kvp.Value;
                    kvp.Value.Save();
                }
                UIDtoConfigMap[kvp.Value.UID] = kvp.Value;
            }

            // CYA: make sure there are no blacklisted uids to break all links that might be compromised by a
            // CYA: duplicated uid.

            foreach (string uid in uidBlacklist)
            {
                IConfiguration config = UIDtoConfigMap[uid];
                while (UIDtoConfigMap.ContainsKey(config.UID))
                    config.ResetUID();
                UIDtoConfigMap[config.UID] = config;
            }

            FilterConfigs(null);
        }
    }
}
