// ********************************************************************************************************************
//
// FileManager.cs : file management abstraction layer
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

// define JAFDTC_LOG to enable logging to the jafdtc-log.txt file in the jafdtc documents area.
//
#define JAFDTC_LOG

using JAFDTC.Models;
using JAFDTC.Models.A10C;
using JAFDTC.Models.Core;
using JAFDTC.Models.F16C;
using JAFDTC.Models.POI;
using JAFDTC.Models.Threats;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JAFDTC.Utilities
{
    /// <summary>
    /// FileManager provides a set of static functions used for interacting with various internal files for jafdtc.
    /// these functions support settings, internal databases, and so on.
    /// </summary>
    public class FileManager
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private static readonly string _appDirPath = AppContext.BaseDirectory;

        private static string _settingsDirPath
            = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JAFDTC");

        private static string _commonDirPath
            = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Saved Games\\JAFDTC");

        private static readonly string _mapTileCachePath
            = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                           "JAFDTC", "MapControlTileCache");

        private static string _settingsPath = null;

        private static string _logPath = null;

        private static StreamWriter _logStream = null;

        private static readonly string[] _sizeSuffixes = [ "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" ];

        // ------------------------------------------------------------------------------------------------------------
        //
        // setup
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// pre-flight the file manager by checking paths and creating the settings folder if necessary. throws an
        /// exception on issues.
        /// </summary>
        public static void Preflight()
        {
            Debug.Assert(Directory.Exists(_appDirPath));

            try
            {
                Directory.CreateDirectory(_settingsDirPath);
                _settingsPath = Path.Combine(_settingsDirPath, "jafdtc-settings.json");
            }
            catch (Exception ex)
            {
                string msg = $"Unable to create settings folder: {_settingsDirPath}. Make sure the path is correct" +
                             $" and that you have appropriate permissions ({ex}).";
                _settingsDirPath = null;
                throw new Exception(msg, ex);
            }

#if JAFDTC_LOG
            try
            {
                _logPath = Path.Combine(_settingsDirPath, "jafdtc-log.txt");
                FileStream stream = new(_logPath, FileMode.OpenOrCreate);
                if (stream.Seek(0, SeekOrigin.End) > 32768)
                    stream.SetLength(0);
                _logStream = new StreamWriter(stream);
                string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                FileManager.Log($"==== JAFDTC {Globals.BuildJAFDTC} launched on {now}");
            }
            catch (Exception ex)
            {
                _settingsDirPath = null;
                string msg = $"Unable to create log file: {_logPath} ({ex}).";
                throw new Exception(msg, ex);
            }
#endif

            if (Directory.Exists(_commonDirPath))
            {
                FileManager.Log($"Common directory {_commonDirPath} is available.");
            }
            else
            {
                FileManager.Log($"Common directory {_commonDirPath} is not available.");
                _commonDirPath = null;
            }
        }

        /// <summary>
        /// return the path to the internal dcs data directory in the application package.
        /// </summary>
        public static string AppDCSDataDirPath()
        {
            return Path.Combine(_appDirPath, "DCS");
        }

        /// <summary>
        /// returns the path to the airframe-specific directory for a given type of data for a given
        /// airframe in the settings directory
        /// </summary>
        private static string AirframeDataDirPath(AirframeTypes airframe, string dataType)
        {
            return airframe switch
            {
                AirframeTypes.A10C => Path.Combine(_settingsDirPath, dataType, "A10C"),
                AirframeTypes.AH64D => Path.Combine(_settingsDirPath, dataType, "AH64D"),
                AirframeTypes.AV8B => Path.Combine(_settingsDirPath, dataType, "AV8B"),
                AirframeTypes.F14AB => Path.Combine(_settingsDirPath, dataType, "F14AB"),
                AirframeTypes.F15E => Path.Combine(_settingsDirPath, dataType, "F15E"),
                AirframeTypes.F16C => Path.Combine(_settingsDirPath, dataType, "F16C"),
                AirframeTypes.FA18C => Path.Combine(_settingsDirPath, dataType, "FA18C"),
                AirframeTypes.M2000C => Path.Combine(_settingsDirPath, dataType, "M2000C"),
                _ => Path.Combine(_settingsDirPath, dataType, "Other"),
            };
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // logging
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// log to the log file in the documents directory. this is a nop unless JAFDTC_LOG is defined
        /// </summary>
        public static void Log(string msg)
        {
            _logStream?.WriteLine(msg);
            _logStream?.Flush();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // core file i/o
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return the contents of the text file at the specified path. callers should protect this with a try/catch.
        /// </summary>
        public static string ReadFile(string path)
        {
            return System.IO.File.ReadAllText(path);
        }

        /// <summary>
        /// return the contents of the text file at the specified path within a zip file at the specified path. callers
        /// should protect this with a try/catch.
        /// </summary>
        public static string ReadFileFromZip(string zipPath, string filePath)
        {
            using ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Read);
            ZipArchiveEntry entry = archive.GetEntry(filePath);
            return new StreamReader(entry?.Open()).ReadToEnd();
        }

        /// <summary>
        /// write the text content to a file at teh specified path. callers should protect this with a try/catch.
        /// </summary>
        public static void WriteFile(string path, string content)
        {
            System.IO.File.WriteAllText(path, content);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // map tile cache directory
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return the path to the map tile cache directory.
        /// </summary>
        public static string MapTileCachePath => _mapTileCachePath;

        /// <summary>
        /// returns the size of the files in a folder.
        /// </summary>
        private static long GetSizeOfFilesInFolder(string path, long size)
        {
            try
            {
                foreach (string dir in Directory.GetDirectories(path))
                    try
                    {
                        if (Directory.Exists(dir))
                            size += GetSizeOfFilesInFolder(dir, 0);
                    }
                    catch
                    {
                        // on exceptions, just skip directory.
                    }
                foreach (string file in Directory.GetFiles(path))
                {
                    try
                    {
                        if (System.IO.File.Exists(file))
                            size += new FileInfo(file).Length;
                    }
                    catch
                    {
                        // on exceptions, just skip file.
                    }
                }
            }
            catch
            {
                // on exeptions, exit
            }
            return size;
        }

        /// <summary>
        /// returns a size string ("200 KB" for the current size of the map tile cache.
        /// </summary>
        public static string GetCurrentMapTileCacheSize()
        {
            long size = GetSizeOfFilesInFolder(MapTileCachePath, 0);

            int i = 0;
            decimal dValue = (decimal)size;
            while (Math.Round(dValue, 2) >= 1000)
            {
                dValue /= 1024;
                i++;
            }
            return string.Format("{0:n2} {1}", dValue, _sizeSuffixes[i]);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // settings
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return the settings object for the settings. if the settings file doesn't exist, create it. reads the json
        /// from the settings file, deserializes it, and returns the resulting object, null on error.
        /// </summary>
        public static SettingsData ReadSettings()
        {
            SettingsData settings = null;
            string json;
            try
            {
                json = ReadFile(_settingsPath);
            }
            catch (Exception ex)
            {
                json = null;
                FileManager.Log($"Settings:ReadSettings settings not found, attempting to create new file, exception {ex}");
            }
            try
            {
                if (json == null)
                {
                    settings = new SettingsData();
                    WriteSettings(settings);
                    json = ReadFile(_settingsPath);
                    FileManager.Log($"Created new settings");
                }
                if (json != null)
                    settings = JsonSerializer.Deserialize<SettingsData>(json);
                FileManager.Log($"Loaded settings from {_settingsPath}");
            }
            catch (Exception ex)
            {
                settings = new SettingsData();
                FileManager.Log($"Settings:ReadSettings unable to create empty settings, exception {ex}");
            }
            return settings;
        }

        /// <summary>
        /// serialize the settings object to json and write the json to the settings file.
        /// </summary>
        public static void WriteSettings(SettingsData settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, Globals.JSONOptions);
                WriteFile(_settingsPath, json);
            }
            catch (Exception ex)
            {
                FileManager.Log($"Settings:WriteSettings failed, exception {ex}");
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // parameter dictionaries
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return the parameters dictionary with the given name. the dictionary is located in the {name}.json file
        /// in the "Data" directory of the app package and is read-only. returns the parameters dictionary read from
        /// the file, the dictionary is empty on error.
        /// </summary>
        public static Dictionary<string, string> LoadParametersDictionary(string name)
        {
            Dictionary<string, string> paramMap = [ ];
            string path = Path.Combine(_appDirPath, "Data", $"{name}.json");
            try
            {
                string json = ReadFile(path);
                paramMap = (Dictionary<string, string>)JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }
            catch (Exception ex)
            {
                FileManager.Log($"FileManager:ReadParameters exception reading {path}, {ex}");
            }
            return paramMap;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // aircraft configurations
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns the path to the configurations directory for a given airframe in the settings directory
        /// </summary>
        private static string AirframeConfigDirPath(AirframeTypes airframe) => AirframeDataDirPath(airframe, "Configs");

        /// <summary>
        /// returns the configuration file name for the given airframe. configuration file name is built by removing
        /// invalid path chars from lowercase config name. a 4-digit hex int is appended to this to uniquify names
        /// like "A / B" and "A ? B".
        /// </summary>
        private static string ConfigFileame(AirframeTypes airframe, string name)
        {
            name = name.ToLower();

            string path = AirframeConfigDirPath(airframe);
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string cleanFilenameBase = new([.. name.Where(m => !invalidChars.Contains(m)) ]);
            string cleanFilename = cleanFilenameBase + ".json";

            int index = 1;
            while (System.IO.File.Exists(Path.Combine(path, cleanFilename)))
            {
                cleanFilename = string.Format("{0} {1:X4}.json", cleanFilenameBase, index);
                index++;
            }
            return cleanFilename;
        }

        /// <summary>
        /// load all .json configuration files found in the per-airframe configuration directory for the specified
        /// airframe type.
        /// </summary>
        public static Dictionary<string, IConfiguration> LoadConfigurationFiles(AirframeTypes airframe)
        {
            string path = AirframeConfigDirPath(airframe);
            Dictionary<string, IConfiguration> dict = [ ];
            if (Directory.Exists(path))
            {
                var files = Directory.EnumerateFiles(path, "*.json");
                foreach (var file in files)
                {
                    var json = System.IO.File.ReadAllText(file);
                    IConfiguration config = Configuration.FactoryJSON(airframe, json);
                    if (config != null)
                    {
                        dict.Add(file, config);
                        FileManager.Log($"Loaded {Globals.AirframeShortNames[config.Airframe]} {config.UID} '{config.Name}' from '{config.Filename}'");
                    }
                    else
                    {
                        // TODO: handle failure to load a configuration
                        FileManager.Log($"Skipped loading configuration from '{file}' due to ERROR");
                    }
                }
            }
            return dict;
        }

        /// <summary>
        /// reads an "unmanaged" configuration file (one outside of the standard config directory) from the file
        /// system. after read, the uid and filename for the configuration are reset. returns a configuration
        /// object, null on error.
        /// </summary>
        public static IConfiguration ReadUnmanagedConfigurationFile(string path)
        {
            if (System.IO.File.Exists(path))
            {
                var json = System.IO.File.ReadAllText(path);
                JsonNode dom = JsonNode.Parse(json);
                int? airframe = (int?)dom["Airframe"];
                if (airframe != null)
                {
                    IConfiguration config = Configuration.FactoryJSON((AirframeTypes)airframe, json);
                    config.Sanitize();
                    return config;
                }
            }
            return null;
        }

        /// <summary>
        /// save a configuration file to the per-airframe configuration directory. the destination path is based
        /// on the current name of the configuration.
        /// </summary>
        public static void SaveConfigurationFile(IConfiguration config)
        {
            string path = AirframeConfigDirPath(config.Airframe);
            Directory.CreateDirectory(path);
            config.Filename ??= ConfigFileame(config.Airframe, config.Name);
            System.IO.File.WriteAllText(Path.Combine(path, config.Filename), config.Serialize());
        }

        /// <summary>
        /// delete a configuration file from the per-airframe configuration directory.
        /// </summary>
        public static void DeleteConfigurationFile(IConfiguration config)
        {
            string path = Path.Combine(AirframeConfigDirPath(config.Airframe), config.Filename);
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }

        /// <summary>
        /// rename the configuration file in the per-airframe configuration directory to match the current
        /// configuration name. the pre-rename filename is provided to locate the source.
        /// </summary>
        public static void RenameConfigurationFile(IConfiguration config, string oldFilename)
        {
            string path = AirframeConfigDirPath(config.Airframe);
            if (Directory.Exists(path))
            {
                config.Filename = ConfigFileame(config.Airframe, config.Name);
                System.IO.File.Move(Path.Combine(path, oldFilename), Path.Combine(path, config.Filename));
                SaveConfigurationFile(config);
            }
            else
            {
                config.Filename = null;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // aircraft dtc templates
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns the path to the dtc template directory for a given airframe in the settings directory
        /// </summary>
        private static string AirframeDTCTemplateDirPath(AirframeTypes airframe) => AirframeDataDirPath(airframe, "DTC");

        /// <summary>
        /// returns true if the airframe has a template file with the specified name, false otherwise. a name of ""
        /// indicates the default airframe template file and is always valid.
        /// </summary>
        public static bool IsValidDTCTemplate(AirframeTypes airframe, string name)
            => (string.IsNullOrEmpty(name) ||
                System.IO.File.Exists(Path.Combine(AirframeDTCTemplateDirPath(airframe), $"{name}.dtc")));

        /// <summary>
        /// returns a list of the dtc template names for an airframe (excluding the default template).
        /// </summary>
        public static List<string> ListDTCTemplates(AirframeTypes airframe)
        {
            List<string> list = [ ];
            string path = AirframeDTCTemplateDirPath(airframe);
            if (Directory.Exists(path))
                foreach (string srcFile in Directory.GetFiles(path))
                    list.Add(Path.GetFileNameWithoutExtension(srcFile));
            return list;
        }

        /// <summary>
        /// import a dcs dtc file into jafdtc as a template file by copying the source template file to the airframe's
        /// dtc template area. returns the template name, "" on error. this operation will over-write an existing
        /// template with the same name and create the template directory if it does not yet exist.
        /// </summary>
        public static string ImportDTCTemplate(AirframeTypes airframe, string srcPath)
        {
            string destPath = AirframeDTCTemplateDirPath(airframe);
            string destName = Path.GetFileNameWithoutExtension(srcPath);
            try
            {
                Directory.CreateDirectory(destPath);
                string template = ReadFile(srcPath);
                WriteFile(Path.Combine(destPath, $"{destName}.dtc"), template);
            }
            catch (Exception ex)
            {
                FileManager.Log($"FileManager:SaveDTCTemplateFile exception copying {srcPath} to {destPath}, {ex}");
                destName = "";
            }
            return destName;
        }

        /// <summary>
        /// returns the contents of a dtc template file with the given name for the specified airframe, null on
        /// error. the name "" indicates the default airframe template, by convention.
        /// </summary>
        public static string LoadDTCTemplate(AirframeTypes airframe, string name)
        {
            string srcPath;
            if (string.IsNullOrEmpty(name))
                srcPath = Path.Combine(_appDirPath, "DCS", "DTC", $"{Globals.AirframeDTCTypes[airframe]}.dtc");
            else
                srcPath = Path.Combine(AirframeDTCTemplateDirPath(airframe), $"{name}.dtc");
            try
            {
                return ReadFile(srcPath);
            }
            catch (Exception ex)
            {
                FileManager.Log($"FileManager:LoadDTCTemplateFile exception loading from {srcPath}, {ex}");
            }
            return null;
        }

        /// <summary>
        /// remove the dtc template file with a given name for the specified airframe. operations on the default
        /// template (name "", by convention) are ignored.
        /// </summary>
        public static void DeleteDTCTemplate(AirframeTypes airframe, string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                string path = Path.Combine(AirframeDTCTemplateDirPath(airframe), $"{name}.dtc");
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // kneeboard template packagess
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns the path to the airframe-specific or generic kneeboard template directory in the settings
        /// directory. an airframe of UNKNOWN implies the generic directory.
        /// </summary>
        private static string KboardTemplateDirPath(AirframeTypes airframe)
            => (airframe != AirframeTypes.UNKNOWN) ? AirframeDataDirPath(airframe, "Kneeboards")
                                                   : Path.Combine(_settingsDirPath, "Kneeboards");

        /// <summary>
        /// returns true if the kneeboard package template exists as a generic or airframe-specific template,
        /// false otherwise. a name of "" indicates the default airframe template file and is always unique.
        /// </summary>
        public static bool IsUniqueKboardTemplate(AirframeTypes airframe, string name)
            => (string.IsNullOrEmpty(name) ||
                (System.IO.File.Exists(Path.Combine(KboardTemplateDirPath(AirframeTypes.UNKNOWN), $"{name}.zip")) ||
                 System.IO.File.Exists(Path.Combine(KboardTemplateDirPath(airframe), $"{name}.zip"))));

        /// <summary>
        /// returns true if the .zip file at the given path is a valid template package (no hierarhcy, all files
        /// are .svg files).
        /// </summary>
        public static bool IsValidKboardTemplatePackage(string path)
        {
            try
            {
                using ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Read);
                if (archive.Entries.Count > 0)
                {
                    char[] pathChars = [ Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar ];
                    foreach (ZipArchiveEntry entry in archive.Entries)
                        if ((entry.FullName.Split(pathChars).Length != 1) ||
                            (!entry.Name.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)))
                            return false;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"FileManager:IsValidKboardTemplatePackage fails on {path}, {ex}");
            }
            return false;
        }

        /// <summary>
        /// returns a list of the kneeboard template names for an airframe (including generic templates, but
        /// excluding the default template) along with the number of generic templates in the list. the
        /// generic templates always start at the beginning of the list.
        /// </summary>
        public static List<string> ListKboardTemplates(AirframeTypes airframe, out int numGeneric)
        {
            List<string> list = [ ];
            string path = KboardTemplateDirPath(AirframeTypes.UNKNOWN);
            if (Directory.Exists(path))
                foreach (string srcFile in Directory.GetFiles(path))
                    if (Path.GetExtension(srcFile).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                        list.Add(Path.GetFileNameWithoutExtension(srcFile));
            numGeneric = list.Count;

            path = KboardTemplateDirPath(airframe);
            if (Directory.Exists(path))
                foreach (string srcFile in Directory.GetFiles(path))
                    if (Path.GetExtension(srcFile).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                        list.Add(Path.GetFileNameWithoutExtension(srcFile));
            return list;
        }

        /// <summary>
        /// import a dcs dtc file into jafdtc as a template file by copying the source template file to the airframe's
        /// dtc template area. returns the template name, "" on error. this operation will over-write an existing
        /// template with the same name and create the template directory if it does not yet exist.
        /// </summary>
        public static string ImportKboardTemplate(AirframeTypes airframe, string srcPath)
        {
            string destPath = KboardTemplateDirPath(airframe);
            string destName = Path.GetFileNameWithoutExtension(srcPath);
            try
            {
                Directory.CreateDirectory(destPath);
                System.IO.File.Copy(srcPath, Path.Combine(destPath, $"{destName}.zip"), overwrite: true) ;
            }
            catch (Exception ex)
            {
                Log($"FileManager:ImportKboardTemplate exception copying {srcPath} to {destPath}, {ex}");
                destName = "";
            }
            return destName;
        }

        /// <summary>
        /// returns list of paths to the extracted contents of a kneeboard template package file with the given
        /// name for the specified airframe, an empty list on error. the name "" indicates the default airframe
        /// template, by convention. the method checks for a matching generic template if an airframe-specific
        /// version is not found.
        /// </summary>
        public static List<string> LoadKboardTemplate(AirframeTypes airframe, string name)
        {
            string srcPath = Path.Combine(_appDirPath, "Data", $"kboard-default-templates.zip");
            try
            {
                if (!string.IsNullOrEmpty(name))
                {
                    srcPath = Path.Combine(KboardTemplateDirPath(airframe), $"{name}.zip");
                    if (!System.IO.File.Exists(srcPath))
                        srcPath = Path.Combine(KboardTemplateDirPath(AirframeTypes.UNKNOWN), $"{name}.zip");
                }
                if (!System.IO.File.Exists(srcPath) || !IsValidKboardTemplatePackage(srcPath))
                    throw new Exception("template package check fails");

                string tempPath = Path.Combine(Path.GetTempPath(), $"JAFDTC-KBB-{Guid.NewGuid()}");
                Directory.CreateDirectory(tempPath);

                List<string> paths = [ ];
                using ZipArchive archive = ZipFile.Open(srcPath, ZipArchiveMode.Read);
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.Name.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                    {
                        // ignore the path (FullName), everything in the package should be flat.
                        //
                        string destPath = Path.GetFullPath(Path.Combine(tempPath, entry.Name));
                        entry.ExtractToFile(destPath, overwrite: true);
                        paths.Add(destPath);
                        Log($"FileManager:LoadKboardTemplate extracts {destPath}");
                    }
                }
                return paths;
            }
            catch (Exception ex)
            {
                Log($"FileManager:LoadKboardTemplate exception loading from {srcPath}, {ex}");
            }
            return [ ];
        }

        /// <summary>
        /// remove the kneeboard template package file with a given name for the specified airframe. operations on
        /// the default template (name "", by convention) are ignored. an airframe of UNKNOWN deletes the generic
        /// template.
        /// </summary>
        public static void DeleteKboardTemplate(AirframeTypes airframe, string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                string destPath = KboardTemplateDirPath(airframe);
                string path = Path.Combine(destPath, $"{name}.zip");
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // databases
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return the type and data information from an internal or sharable database file as a ( type, json )
        /// tuple. the file is either in the format "{json}" (internal) or "<{type}> {json}". on error, returns
        /// the tuple ( null, null ).
        /// </summary>
        private static Tuple<string, string> CoreCrackDbaseFile(string path, string typeStr = null)
        {
            try
            {
                string data = ReadFile(path).Trim();
                if (data[0] == '<')
                {
                    int index = data.IndexOf(' ');
                    if ((index < 3) || (data[index - 1] != '>'))
                        throw new Exception("Invalid <type> marker");
                    typeStr = data[1..(index - 1)];
                    data = data[(index + 1)..];
                }
                return new(typeStr, data);
            }
            catch (Exception ex)
            {
                FileManager.Log($"FileManager:LoadDbaseCore exception reading {path}, {ex}");
            }
            return new(null, null);
        }

        /// <summary>
        /// return the database (List<T> of database objects) extracted from the file. this handles internal
        /// databases (List<T> serialized to .json) as well as sharable database (type string followed by List<T>
        /// serialized to .json) formats. on error, the database contents are an empty list.
        /// </summary>
        private static List<T> CoreLoadDbase<T>(string path)
        {
            Tuple<string, string> tuple = CoreCrackDbaseFile(path, typeof(T).Name);
            try
            {
                if (typeof(T).Name == tuple.Item1)
                    return (List<T>)JsonSerializer.Deserialize<List<T>>(tuple.Item2);
            }
            catch (Exception ex)
            {
                FileManager.Log($"FileManager:LoadDbaseCore exception reading {path}, {ex}");
            }
            return [ ];
        }

        /// <summary>
        /// return the type encoded in a sharable database, null if there is no type or an error happened.
        /// </summary>
        public static string GetSharableDbaseType(string path) => CoreCrackDbaseFile(path).Item1;

        /// <summary>
        /// load a shared databse (type string followed by List<T> serialzed to .json) from the given path. returns
        /// and empty list on error.
        /// </summary>
        public static List<T> LoadSharableDbase<T>(string path) => CoreLoadDbase<T>(path);

        /// <summary>
        /// save a shared databse (type string followed by List<T> serialzed to .json) to the given path. returns
        /// true on success, false on failure.
        /// </summary>
        public static bool SaveSharableDatabase<T>(string path, List<T> dbase, Func<T, Boolean> fnFilter = null)
        {
            fnFilter ??= entry => true;
            List<T> dbFilter = [.. dbase.Where(fnFilter) ];

            try
            {
                string data = $"<{typeof(T).Name}> " + JsonSerializer.Serialize<List<T>>(dbFilter, Configuration.JsonOptions);
                WriteFile(path, data);
                return true;
            }
            catch (Exception ex)
            {
                FileManager.Log($"FileManager:SaveSharableDatabase exception saving {path}, {ex}");
            }
            return false;
        }

        /// <summary>
        /// load a system database (List<T> serialized to .json) from the "Data" directory in the app bundle. system
        /// databases are immutable and cannot be updated. returns an empty list on error.
        /// </summary>
        public static List<T> LoadSystemDbase<T>(string name)
        {
            string path = Path.Combine(_appDirPath, "Data", name);
            return (System.IO.File.Exists(path)) ? CoreLoadDbase<T>(path) : [ ];
        }

        /// <summary>
        /// load the user database (List<T> serialized to .json) with the given name from the database area in the
        /// jafdtc settings directory. user databases are mutable and may be updated. returns an empty list on error.
        /// </summary>
        public static List<T> LoadUserDbase<T>(string name)
        {
            string path = Path.Combine(_settingsDirPath, "Dbase");
            Directory.CreateDirectory(path);
            return CoreLoadDbase<T>(Path.Combine(path, name));
        }

        /// <summary>
        /// save the user database (List<T> serialized to .json) with the given name to the database area in the
        /// jafdtc settings directory (creating the file if necessary). entries in the database are only persisted
        /// if the given filter function returns true when passed the entry (default filter function always returns
        /// true). returns true on success, false on failure.
        /// </summary>
        public static bool SaveUserDbase<T>(string name, List<T> dbase, Func<T,Boolean> fnFilter = null)
        {
            fnFilter ??= entry => true;
            List<T> dbFilter = [.. dbase.Where(fnFilter) ];

            string path = Path.Combine(_settingsDirPath, "Dbase");
            Directory.CreateDirectory(path);
            path = Path.Combine(path, name);
            try
            {
                string json = JsonSerializer.Serialize<List<T>>(dbFilter, Configuration.JsonOptions);
                WriteFile(path, json);
                return true;
            }
            catch (Exception ex)
            {
                FileManager.Log($"FileManager:SaveUserDbase exception saving {path}, {ex}");
            }
            return false;
        }

        /// <summary>
        /// delete the user database (List<T> serialized to .json) with the given name from the database area in
        /// the jafdtc settings directory.
        /// </summary>
        public static void DeleteUserDatabase(string name)
        {
            string path = Path.Combine(Path.Combine(_settingsDirPath, "Dbase"), name);
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // poi database
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return a sanitized filename for a campaign point of interest database.
        /// </summary>
        private static string CampaignPoIFilename(string campaign)
        {
            campaign = campaign.ToLower().Replace(' ', '-');
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string cleanFilenameBase = new([.. campaign.Where(m => !invalidChars.Contains(m)) ]);
            Debug.Assert(cleanFilenameBase.Length > 0);
            return $"jafdtc-pois-campaign-{cleanFilenameBase}.json";
        }

        /// <summary>
        /// return a list of points of interest in the database that provides coordinates on known points in the
        /// world. this list is the union of a read-only system dbase that carries fixed dcs points (such as airbases),
        /// a user dbase that holds editable user-specified points, and any number of per-campaign databases.
        /// </summary>
        public static List<PointOfInterest> LoadPointsOfInterest()
        {
            List<PointOfInterest> dbase = LoadSystemDbase<PointOfInterest>("db-pois-airbases.json");

            string path = Path.Combine(_settingsDirPath, "Dbase");
            Directory.CreateDirectory(path);
            foreach (string srcFile in Directory.GetFiles(path))
            {
                string fileName = Path.GetFileName(srcFile);
                if (fileName.StartsWith("jafdtc-pois-", StringComparison.CurrentCultureIgnoreCase) &&
                    fileName.ToLower().EndsWith(".json", StringComparison.CurrentCultureIgnoreCase))
                {
                    dbase.AddRange(LoadUserDbase<PointOfInterest>(fileName));
                }
            }
            return dbase;
        }

        /// <summary>
        /// saves points of interest to the user point of interest database. database is persisted as a list of
        /// PointOfInterest instances. returns true on success, false otherwise.
        /// </summary>
        public static bool SaveUserPointsOfInterest(List<PointOfInterest> userPoIs)
        {
            return SaveUserDbase<PointOfInterest>("jafdtc-pois-user.json", userPoIs);
        }

        /// <summary>
        /// saves points of interest to a per-campaign point of interest database. database is persisted as a list
        /// of PointOfInterest instances. returns true on success, false otherwise.
        /// </summary>
        public static bool SaveCampaignPointsOfInterest(string campaign, List<PointOfInterest> campaignPoIs)
        {
            return SaveUserDbase<PointOfInterest>(CampaignPoIFilename(campaign), campaignPoIs);
        }

        /// <summary>
        /// deletes the per-campaign points of interest.
        /// </summary>
        public static void DeleteCampaignPointsOfInterest(string campaign)
        {
            DeleteUserDatabase(CampaignPoIFilename(campaign));
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // threat database
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return a list of threats in the database that provides parameters on known threats. this list is the union
        /// of a read-only system dbase that carries base dcs threats and a user dbase that holds editable
        /// user-specified threats.
        /// </summary>
        public static List<Threat> LoadThreats()
        {
            List<Threat> dbase = LoadSystemDbase<Threat>("db-threats.json");

            string path = Path.Combine(_settingsDirPath, "Dbase");
            Directory.CreateDirectory(path);
            foreach (string srcFile in Directory.GetFiles(path))
            {
                string fileName = Path.GetFileName(srcFile);
                if (fileName.ToLower().Equals("jafdtc-threats-user.json", StringComparison.CurrentCultureIgnoreCase))
                {
                    dbase.AddRange(LoadUserDbase<Threat>(fileName));
                    break;
                }
            }
            return dbase;
        }

        /// <summary>
        /// saves threats to the user threat database. database is persisted as a list of Threat instances. returns
        /// true on success, false otherwise.
        /// </summary>
        public static bool SaveUserThreats(List<Threat> userThreats)
        {
            return SaveUserDbase<Threat>("jafdtc-threats-user.json", userThreats);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // system databases
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return the A-10C munitions database that provides information on weapons for the hawg.
        /// </summary>
        public static List<A10CMunition> LoadA10Munitions() => LoadSystemDbase<A10CMunition>("db-a10c-munitions.json");

        /// <summary>
        /// return the emitter database that provides information on known emitters for harm alic/hts systems.
        /// </summary>
        public static List<F16CEmitter> LoadF16CEmitters() => LoadSystemDbase<F16CEmitter>("db-f16c-emitters.json");

        /// <summary>
        /// return the F-16C munitions database that provides information on weapons for the viper.
        /// </summary>
        public static List<F16CMunition> LoadF16CMunitions() => LoadSystemDbase<F16CMunition>("db-f16c-munitions.json");
    }
}