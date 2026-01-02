// ********************************************************************************************************************
//
// NavpointUIHelper.cs : helper classes for navpoint ui
//
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

using JAFDTC.File;
using JAFDTC.File.Models;
using JAFDTC.Models.Base;
using JAFDTC.Models.Core;
using JAFDTC.Models.CoreApp;
using JAFDTC.Models.DCS;
using JAFDTC.Models.POI;
using JAFDTC.Models.Units;
using JAFDTC.UI.App;
using JAFDTC.UI.Controls.Map;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// wrapper around the point of interest object to present the ui view of the point of interest in the selection
    /// list from the poi filter box in navpoint editors.
    /// </summary>
    public sealed class PoIListItem
    {
        public PointOfInterest PoI { get; set; }

        public string Name => PoI.Name;

        public string Info
        {
            get
            {
                string tags = (string.IsNullOrEmpty(PoI.Tags)) ? "—" : PoI.Tags.Replace(";", ", ");
                return (string.IsNullOrEmpty(PoI.Campaign)) ? $"{PoI.Theater} : {tags}"
                                                            : $"{PoI.Theater} [{PoI.Campaign}] : {tags}";
            }
        }

        public string Glyph => (PoI.Type == PointOfInterestType.USER)
                               ? "\xE718" : ((PoI.Type == PointOfInterestType.CAMPAIGN) ? "\xE7C1" : "");

        public PoIListItem(PointOfInterest poi) => (PoI) = (poi);
    }

    // ================================================================================================================

    /// <summary>
    /// helper class to provide a number of static support functions for use in the navpoint user interface. this
    /// includes things like common dialogs, import/export core operations, etc.
    /// </summary>
    public class NavpointUIHelper
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // navpoint location functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns a list of theaters that cover the list of navpoints. the list is sorted in order of membership:
        /// first index is the theater with the most matches, last index is the theater with the least.
        /// </summary>
        public static List<string> TheatersForNavpoints(List<INavpointInfo> navpts)
        {
            Dictionary<string, int> theaterMap = [ ];
            foreach (INavpointInfo navpt in navpts)
                foreach (string theater in Theater.TheatersForCoords(navpt.Lat, navpt.Lon))
                    theaterMap[theater] = theaterMap.GetValueOrDefault(theater, 0) + 1;

            Dictionary<int, List<string>> freqMap = [];
            foreach (KeyValuePair<string, int> kvp in theaterMap)
                if (freqMap.ContainsKey(kvp.Value))
                    freqMap[kvp.Value].Add(kvp.Key);
                else
                    freqMap[kvp.Value] = [ kvp.Key ];

            List<string> theaters = [ ];
            foreach (int freq in freqMap.Keys.OrderByDescending(k => k))
                foreach (string theater in freqMap[freq])
                    theaters.Add(theater);

            return theaters;
        }

        /// <summary>
        /// propose a location for a new navpoint bast on the current set of navpoints. there are three cases:
        /// (1) with no navpoints, prompts to select a theater and locates the navpoint in the center of the
        /// bounds, (2) with one navpoint, places the navpoint slightly to the east, and (3) with two or more
        /// navpoints, places the navpoint slightly beyond along a line from the next-to-last and last navpoint.
        /// returns a tuple { lat, lon }, null if the user cancels.
        /// </summary>
        public static async Task<Tuple<string, string>> ProposeNewNavptLatLon(XamlRoot root, List<INavpointInfo> navpts)
        {
            double lat;
            double lon;

            if (navpts.Count == 0)
            {
                // no navpoints: prompt for a theater then locate the new navpoint in the center of the
                // area for that theater.
                //
                GetListDialog theaterDialog = new(Theater.Theaters, "Theater", 0, 0)
                {
                    XamlRoot = root,
                    Title = $"Select a Theater for the Navpoint",
                    PrimaryButtonText = "OK",
                    CloseButtonText = "Cancel"
                };
                ContentDialogResult result = await theaterDialog.ShowAsync(ContentDialogPlacement.Popup);
                if (result == ContentDialogResult.Primary)
                {
                    TheaterInfo info = Theater.TheaterInfo[theaterDialog.SelectedItem];
                    lat = info.LatMin + ((info.LatMax - info.LatMin) / 2.0);
                    lon = info.LonMin + ((info.LonMax - info.LonMin) / 2.0);
                }
                else
                {
                    return null;                                    // EXIT: cancelled, no change...
                }
            }
            else if (navpts.Count == 1)
            {
                // single navpoint: proposed new navpoint is a bit east of the existing navpoint.
                //
                lat = double.Parse(navpts[0].Lat);
                lon = double.Parse(navpts[0].Lon) + 0.5;
            }
            else
            {
                // at least two navpoints: proposed new navpoint is a little bit down the line connecting the
                // last two navpoints.
                //
                double p0Lat = double.Parse(navpts[^1].Lat);
                double p0Lon = double.Parse(navpts[^1].Lon);

                double p1Lat = double.Parse(navpts[^2].Lat);
                double p1Lon = double.Parse(navpts[^2].Lon);

                double len = Math.Sqrt(Math.Pow(p1Lat - p0Lat, 2) + Math.Pow(p1Lon - p0Lon, 2));

                lat = p0Lat + ((p0Lat - p1Lat) / len) * 0.5;
                lon = p0Lon + ((p0Lon - p1Lon) / len) * 0.5;
            }

            return new Tuple<string, string>($"{lat:F8}", $"{lon:F8}");
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // navpoint poi functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// display the filter dialog and gather a new filter spec to use.
        /// </summary>
        public static async Task<POIFilterSpec> FilterSpecDialog(XamlRoot root, POIFilterSpec filter, ToggleButton button)
        {
            if (button.IsChecked != !filter.IsDefault)
                button.IsChecked = !filter.IsDefault;

            PoIFilterDialog filterDialog = new(filter)
            {
                XamlRoot = root,
                Title = $"Set a Filter for Points of Interest"
            };
            ContentDialogResult result = await filterDialog.ShowAsync(ContentDialogPlacement.Popup);
            if (result == ContentDialogResult.Primary)
                filter = new(filterDialog.Filter);
            else if (result == ContentDialogResult.Secondary)
                filter = new();
            else
                return null;                                    // EXIT: cancelled, no change...

            button.IsChecked = !filter.IsDefault;
            return filter;
        }

        /// <summary>
        /// return the point of interest list to display in the filter box candidates list.
        /// </summary>
        public static List<PoIListItem> RebuildPointsOfInterest(POIFilterSpec filter, string name = null)
        {
            List<PoIListItem> suitableItems = [ ];
            PointOfInterestDbQuery query = new(filter.IncludeTypes, filter.Theater, null, name, filter.Tags,
                                               PointOfInterestDbQueryFlags.NAME_PARTIAL_MATCH);
            foreach (PointOfInterest poi in PointOfInterestDbase.Instance.Find(query, true))
                suitableItems.Add(new PoIListItem(poi));
            return suitableItems;
        }

        /// <summary>
        /// copy list of navpoints to POIs. Opens modal to prompt for campaign, tags, etc.
        /// </summary>
        public static async void CopyNavpointsAsPoIs(XamlRoot root, List<INavpointInfo> navpts, bool isCopyAll)
        {
            List<string> allowedTheaters = TheatersForNavpoints(navpts);

            POIFilterSpec filter = new()
            {
                IncludeTypes = PointOfInterestTypeMask.ANY
            };
            PoIFilterDialog filterDialog = new(filter, PoIFilterDialog.Mode.CHOOSE, allowedTheaters)
            {
                XamlRoot = root,
                Title = $"Copy to Points of Interest",
                PrimaryButtonText = isCopyAll ? "Copy All" : "Copy Selected",
                SecondaryButtonText = null,
                CloseButtonText = "Cancel"
            };
            ContentDialogResult result = await filterDialog.ShowAsync(ContentDialogPlacement.Popup);
            if (result == ContentDialogResult.Primary)
            {
                filter = filterDialog.Filter;
                filter.IncludeTypes = PointOfInterestTypeMask.ANY;

                // set common POI properties
                PointOfInterestType poiType = PointOfInterestType.USER;
                if (filter.Campaign != null)
                    poiType = PointOfInterestType.CAMPAIGN;

                // save POIs
                List<string> dupes = [ ];
                foreach (INavpointInfo nav in navpts)
                {
                    bool success = PointOfInterestDbase.Instance.AddPointOfInterest(new()
                    {
                        Type = poiType,
                        Theater = filter.Theater,
                        Campaign = filter.Campaign,
                        Tags = PointOfInterest.SanitizedTags(filter.Tags),
                        Name = nav.Name,
                        Latitude = nav.Lat,
                        Longitude = nav.Lon,
                        Elevation = nav.Alt
                    });
                    if (!success)
                        dupes.Add(nav.Name);
                }
                PointOfInterestDbase.Instance.Save(filter.Campaign);

                if (dupes.Count > 0)
                {
                    string dupeMsg = (dupes.Count == 1)
                        ? $"The point of interest “{dupes[0]}” already exists and was not copied."
                        : $"The following points of interest already exist and were not copied:\n" +
                          $"- {string.Join("\n- ", dupes)}";
                    await Utilities.Message1BDialog(root, "Duplicate Points of Interest", dupeMsg);
                }
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // navpoint command functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// display a reset navpoints dialog and return the result. the return value is true if the user accepts
        /// the reset, false if they cancel. the what parameter should be capitalized and singular.
        /// </summary>
        public static async Task<bool> ResetDialog(XamlRoot root, string what)
        {
            return await new ContentDialog()
            {
                XamlRoot = root,
                Title = $"Reset {what}s?",
                Content = $"Are you sure you want to delete all {what.ToLower()}s? This action cannot be undone.",
                PrimaryButtonText = "Delete All",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            }.ShowAsync(ContentDialogPlacement.Popup) == ContentDialogResult.Primary;
        }

        /// <summary>
        /// display a delete navpoint dialog and return the result. the return value is true if the user accepts
        /// the delete, false if they cancel. the what parameter should be capitalized and singular.
        /// </summary>
        public static async Task<bool> DeleteDialog(XamlRoot root, string what, int count)
        {
            string title = (count == 1) ? $"Delete {what}?" : $"Delete {what}s?";
            string content = (count == 1)
                ? $"Are you sure you want to delete this {what.ToLower()}? This action cannot be undone."
                : $"Are you sure you want to delete these {what.ToLower()}s? This action cannot be undone.";
            return await Utilities.Message2BDialog(root, title, content, "Delete") == ContentDialogResult.Primary;
        }

        /// <summary>
        /// display a renumber navpoints dialog and return the result. the return value is the starting navpoint
        /// number (if accepted) or -1 (if cancelled). the what parameter should be capitalized and singular.
        /// </summary>
        public static async Task<int> RenumberDialog(XamlRoot root, string what, int min, int max)
        {
            GetNumberDialog dlg = new(null, null, min, max)
            {
                XamlRoot = root,
                Title = $"Select New Starting {what} Number",
                PrimaryButtonText = "Renumber",
                CloseButtonText = "Cancel",
            };
            return (await dlg.ShowAsync(ContentDialogPlacement.Popup) == ContentDialogResult.Primary) ? dlg.Value : -1;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // navpoint list import functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// present and handle an import fail dialog that indicates jafdtc was unable to read/import from the file.
        /// what parameter should be capitalized and singular (e.g., "Steerpoints").
        /// </summary>
        private static async void ImportFailDialog(XamlRoot root, string what)
        {
            await Utilities.Message1BDialog(root, "Import Failed", $"Unable to read {what.ToLower()}s from the file.");
        }

        /// <summary>
        /// present and handle the ui to import from the contents of a .cf/.miz file. the navpoints either
        /// replace or are appended to the current list of navpoints. what parameter should be capitalized and
        /// singular (e.g., "Steerpoint"). returns true on success, false on failure (user is notified on failures).
        /// </summary>
        public static async Task<bool> Import(XamlRoot root, AirframeTypes airframe, INavpointSystemImport navptSys,
                                              string what)
        {
            // select file to import navpoints from with FileOpenPicker.
            //
            FileOpenPicker picker = new((Application.Current as JAFDTC.App).Window.AppWindow.Id)
            {
                CommitButtonText = $"Import {what}s",
                SuggestedStartLocation = PickerLocationId.Desktop,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add(".cf");
            picker.FileTypeFilter.Add(".miz");
            PickFileResult resultPick = await picker.PickSingleFileAsync();
            if (resultPick == null)
                return false;                                   // **** EXITS: cancel

            try
            {
                // build extractor and import groups/units from the file.
                //
                IExtractor extractor = Path.GetExtension(resultPick.Path).ToLower() switch
                {
                    ".acmi" => new JAFDTC.File.ACMI.Extractor(),
                    ".cf" => new JAFDTC.File.CF.Extractor(),
                    ".miz" => new JAFDTC.File.MIZ.Extractor(),
                    _ => (IExtractor) null
                } ?? throw new Exception($"File type “{Path.GetExtension(resultPick.Path)}” is not supported.");

                ExtractCriteria criteria = new()
                {
                    FilePath = resultPick.Path,
                    UnitCategories = [UnitCategoryType.AIRCRAFT, UnitCategoryType.HELICOPTER],
                    UnitTypes = ((Settings.IsNavPtImportIgnoreAirframe) ? AirframeTypes.UNKNOWN : airframe) switch
                    {
                        AirframeTypes.A10C => ["A-10C_2"],
                        AirframeTypes.AH64D => ["AH-64D_BLK_II"],
                        AirframeTypes.AV8B => ["AV8BNA"],
                        AirframeTypes.F14AB => ["F-14A-135-GR", "F-14B"],
                        AirframeTypes.F15E => ["F-15ESE"],
                        AirframeTypes.F16C => ["F-16C_50"],
                        AirframeTypes.FA18C => ["FA-18C_hornet"],
                        AirframeTypes.M2000C => ["M-2000C"],
                        _ => [],
                    }
                };
                IReadOnlyList<UnitGroupItem> groups = extractor.Extract(criteria)
                    ?? throw new Exception($"Encountered an error while importing from path\n\n{resultPick.Path}");

                // build a list of flights available in the extracted data, then prompt the user to select the
                // flight and setup import parameters before performing the actual import.
                //
                Dictionary<string, UnitGroupItem> flightMap = [ ];
                foreach (UnitGroupItem group in groups)
                    flightMap[group.Name] = group;
                if (flightMap.Count == 0)
                    throw new Exception($"There were no flights for the {Globals.AirframeNames[airframe]} airframe" +
                                        $" found in the import file from path\n\n{resultPick.Path}");

                ContentDialogResult result = ContentDialogResult.None;
                List<string> flights = [.. flightMap.Keys ];
                flights.Sort();
                ImportParamsNavptDialog flightList = new(what, flights)
                {
                    XamlRoot = root,
                    Title = $"Select a Flight to Import {what}s From",
                    PrimaryButtonText = "Replace",
                    SecondaryButtonText = "Append",
                    CloseButtonText = "Cancel"
                };
                result = await flightList.ShowAsync(ContentDialogPlacement.Popup);
                if (result != ContentDialogResult.None)
                {
                    List<UnitPositionItem> route = [.. flightMap[flightList.SelectedFlight].Route ];
                    if (route.Count > 0)
                    {
                        if (!flightList.IsUsingTOS)
                            foreach (UnitPositionItem posn in route)
                                posn.TimeOn = -1.0;
                        if (!flightList.IsIncludingFirst)
                            route.RemoveAt(0);
                        if (!navptSys.ImportUnitPositionList(route, (result == ContentDialogResult.Primary)))
                            throw new Exception("The world is mysterious. Import fails for... Reasons.");
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                await Utilities.Message1BDialog(root, "Import Failed", ex.Message);
            }

            return false;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // navpoint list map functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// create and active a map window with the specified content and observers.
        /// </summary>
        public static MapWindow OpenMap(IMapControlVerbHandler observer, int maxRouteLen, LLFormat coordFormat,
                                        MapMarkerInfo.MarkerTypeMask openMask, MapMarkerInfo.MarkerTypeMask editMask,
                                        Dictionary<string, List<INavpointInfo>> routes,
                                        MapImportSpec mapImport = null, MapFilterSpec mapFilter = null)
        {
            List<INavpointInfo> allRoutes = [ ];
            foreach (string route in routes.Keys)
                allRoutes.AddRange(routes[route]);
            List<string> theaters = TheatersForNavpoints(allRoutes);
            string theater = (theaters.Count > 0) ? theaters[0] : null;

            Dictionary<string, PointOfInterest> marks = [];
            PointOfInterestDbQuery query = new(PointOfInterestTypeMask.ANY, theater);
            foreach (PointOfInterest poi in PointOfInterestDbase.Instance.Find(query))
                marks[poi.UniqueID] = poi;

            MapWindow mapWindow = new(true, true)
            {
                Theater = theater,
                OpenMask = openMask,
                EditMask = editMask,
                CoordFormat = coordFormat,
                MaxRouteLength = maxRouteLen
            };
            mapWindow.SetupMapContent(routes, marks, mapImport, mapFilter);
            mapWindow.RegisterMapControlVerbObserver(observer);

            mapWindow.Activate();

            return mapWindow;
        }

        /// <summary>
        /// returns the display type of the marker with the specified information. this only handles poi marker
        /// types, reuturning null for other types
        /// </summary>
        public static string MarkerDisplayType(MapMarkerInfo info)
            => info.Type switch
            {
                MapMarkerInfo.MarkerType.POI_SYSTEM => $"Core POI",
                MapMarkerInfo.MarkerType.POI_USER => $"User POI",
                MapMarkerInfo.MarkerType.POI_CAMPAIGN => $"Campaign POI",
                _ => null
            };

        /// <summary>
        /// returns the display name of the marker with the specified information.
        /// </summary>
        public static string MarkerDisplayName(MapMarkerInfo info)
        {
            string name = null;
            if ((info.Type == MapMarkerInfo.MarkerType.POI_SYSTEM) ||
                (info.Type == MapMarkerInfo.MarkerType.POI_USER) ||
                (info.Type == MapMarkerInfo.MarkerType.POI_CAMPAIGN))
            {
                PointOfInterest poi = PointOfInterestDbase.Instance.Find(info.TagStr);
                if (poi != null)
                    name = poi.Type switch
                    {
                        PointOfInterestType.SYSTEM => $"POI: {poi.Name}",
                        PointOfInterestType.USER => $"User: {poi.Name}",
                        PointOfInterestType.CAMPAIGN => $"{poi.Campaign}: {poi.Name}",
                        _ => throw new NotImplementedException(),
                    };
            }
            return name;
        }

        /// <summary>
        /// returns the elevation of the marker with the specified information.
        /// </summary>
        public static string MarkerDisplayElevation(MapMarkerInfo info, string units = "")
        {
            string elev = null;
            if ((info.Type == MapMarkerInfo.MarkerType.POI_SYSTEM) ||
                (info.Type == MapMarkerInfo.MarkerType.POI_USER) ||
                (info.Type == MapMarkerInfo.MarkerType.POI_CAMPAIGN))
            {
                PointOfInterest poi = PointOfInterestDbase.Instance.Find(info.TagStr);
                if (poi != null)
                    elev = $"{poi.Elevation}{units}";
            }
            return elev;
        }
    }
}
