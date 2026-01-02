// ********************************************************************************************************************
//
// EditPointsOfInterestPage.xaml.cs : ui c# point of interest editor
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

using JAFDTC.Models.CoreApp;
using JAFDTC.Models.DCS;
using JAFDTC.Models.POI;
using JAFDTC.UI.Base;
using JAFDTC.UI.Controls.Map;
using JAFDTC.Utilities;
using JAFDTC.Utilities.Networking;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// item class for an item in the point of interest ListView. this provides ui-friendly views of properties
    /// suitable for display in the ui via bindings.
    /// </summary>
    internal partial class PoIListItem : BindableObject
    {
        public PointOfInterest PoI { get; set; }

        public LLFormat LLDisplayFmt { get; set; }

        public string TagsUI
        {
            get
            {
                string tags = (string.IsNullOrEmpty(PoI.Tags)) ? "—" : PoI.Tags.Replace(";", ", ");
                return (string.IsNullOrEmpty(PoI.Campaign)) ? tags : $"{PoI.Campaign} : {tags}";
            }
        }

        public string _latUI;
        public string LatUI
        {
            get => Coord.ConvertFromLatDD(PoI.Latitude, LLDisplayFmt);
            set
            {
                PoI.Latitude = Coord.ConvertToLatDD(value, LLDisplayFmt);
                SetProperty(ref _latUI, value);
            }
        }

        private string _lonUI;
        public string LonUI
        { 
            get => Coord.ConvertFromLonDD(PoI.Longitude, LLDisplayFmt);
            set
            {
                PoI.Longitude = Coord.ConvertToLonDD(value, LLDisplayFmt);
                SetProperty(ref _lonUI, value);
            }
        }
        
        public string Glyph
            => PoI.Type switch
            {
                PointOfInterestType.USER => "\xE718",
                PointOfInterestType.CAMPAIGN => "\xE7C1",
                _ => ""
            };

        public PoIListItem(PointOfInterest poi, LLFormat fmt) => (PoI, LLDisplayFmt) = (poi, fmt);
    }

    // ================================================================================================================

    /// <summary>
    /// point of interest lat/lon helper. this provides for translation between the user-facing presentation of the
    /// lat/lon (where settings in the ui specify the lat/lon display format) and the internal decimal degrees format.
    /// </summary>
    internal partial class PoILL : BindableObject
    {
        public LLFormat Format { get; set; }

        public string Lat { get; set; }             // string, dd format

        public string Lon { get; set; }             // string, dd format

        private string _latUI;                      // string, format per property
        public string LatUI
        {
            get => Coord.ConvertFromLatDD(Lat, Format);
            set
            {
                string error = "Invalid latitude format";
                if (IsRegexFieldValid(value, Coord.LatRegexFor(Format)))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lat = Coord.ConvertToLatDD(value, Format);
                SetProperty(ref _latUI, value, error);
            }
        }

        private string _lonUI;                      // string, format per property
        public string LonUI
        {
            get => Coord.ConvertFromLonDD(Lon, Format);
            set
            {
                string error = "Invalid longitude format";
                if (IsRegexFieldValid(value, Coord.LonRegexFor(Format)))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lon = Coord.ConvertToLonDD(value, Format);
                SetProperty(ref _lonUI, value, error);
            }
        }

        public bool IsEmpty => (string.IsNullOrEmpty(Lat) && string.IsNullOrEmpty(Lon));

        public PoILL(LLFormat format) => (Format) = (format);

        public List<string> GetErrorsWithEmpty(bool isEmptyOK)
        {
            List<string> errors = [ ];
            if (!IsRegexFieldValid(LatUI, Coord.LatRegexFor(Format), isEmptyOK))
                errors.Add("LatUI");
            if (!IsRegexFieldValid(LonUI, Coord.LonRegexFor(Format), isEmptyOK))
                errors.Add("LonUI");
            return errors;
        }

        public void Reset()
        {
            LatUI = "";
            LonUI = "";
        }
    }

    // ================================================================================================================

    /// <summary>
    /// backing object for editing a point of interest. this provides bindings for the text fields at the bottom of
    /// the poi page that set name, tags, lat/lon/elev information.
    /// </summary>
    internal partial class PoIDetails : BindableObject
    {
        public string SourceUID { get; set; }

        public int CurIndexLL { get; set; }

        public List<string> CurTheaters { get; set; }

        // HACK: we will use per-format PoILL instances to avoid binding multiple controls to the same property
        // HACK: (which doesn't seem to work well). should be a way to dynamically bind/unbind properties that we
        // HACK: could use to avoid this, but...
        //
        public PoILL[] LL { get; set; }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _tags;
        public string Tags
        {
            get => _tags;
            set => SetProperty(ref _tags, value);
        }

        private string _alt;
        public string Alt
        {
            get => _alt;
            set
            {
                string error = "Invalid altitude format";
                if (IsIntegerFieldValid(value, -1500, 80000))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _alt, value, error);
            }
        }

        public bool IsEmpty
            => (string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Tags) && string.IsNullOrEmpty(Alt) &&
                LL[CurIndexLL].IsEmpty);

        public bool IsDirty
        {
            get
            {
                PointOfInterest poi = PointOfInterestDbase.Instance.Find(SourceUID);
                if (poi != null)
                    return poi.Name != Name ||
                           poi.Tags != Tags ||
                           poi.Elevation != Alt ||
                           poi.Latitude != LL[CurIndexLL].Lat ||
                           poi.Longitude != LL[CurIndexLL].Lon;
                else
                    return !IsEmpty;
            }
        }

        // NOTE: format order of PoILL must be kept in sync with EditPointsOfInterestPage and xaml.
        //
        public PoIDetails()
            => (CurIndexLL, LL) = (0, [ new(LLFormat.DDU), new(LLFormat.DMS), new(LLFormat.DDM_P3ZF) ]);

        public PoIDetails(PointOfInterest poi, LLFormat llDisplayFmt, int llIndex)
        {
            CurIndexLL = 0;
            LL = [ new(LLFormat.DDU), new(LLFormat.DMS), new(LLFormat.DDM_P3ZF) ];
            SourceUID = null;
            Name = poi.Name;
            Tags = PointOfInterest.SanitizedTags(poi.Tags);
            LL[llIndex].LatUI = Coord.ConvertFromLatDD(poi.Latitude, llDisplayFmt);
            LL[llIndex].LonUI = Coord.ConvertFromLonDD(poi.Longitude, llDisplayFmt);
            Alt = poi.Elevation;
        }

        public List<string> GetErrorsWithEmpty(bool isEmptyOK)
        {
            List<string> errors = LL[CurIndexLL].GetErrorsWithEmpty(isEmptyOK);
            if (!IsIntegerFieldValid(Alt, -1500, 80000, isEmptyOK))
                errors.Add("Alt");
            return errors;
        }

        public void Reset()
        {
            SourceUID = "";
            Name = "";
            Tags = "";
            Alt = "";
            for (int i = 0; i < LL.Length; i++)
                LL[i].Reset();
        }
    }

    // ================================================================================================================

    /// <summary>
    /// main page for the point of interest editor. this provides the ui view of the poi database in jafdtc and
    /// implements typical editor actions to create, modify, and delete points of interest.
    /// </summary>
    public sealed partial class EditPointsOfInterestPage : Page, IMapControlVerbHandler, IMapControlMarkerExplainer
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- regular expressions

        [GeneratedRegex(@"^[a-zA-Z0-9 ]+$")]
        private static partial Regex CampaignNameRegex();

        // ---- internal properties

        private MapWindow _mapWindow;
        private MapWindow MapWindow
        {
            get => _mapWindow;
            set
            {
                if (_mapWindow != value)
                {
                    _mapWindow = value;
                    VerbMirror = value;
                }
            }
        }

        private ObservableCollection<PoIListItem> CurPoIItems { get; set; }

        private PoIDetails EditPoI { get; set; }

        private bool IsRebuildPending { get; set; }

        private bool IsVerbEvent { get; set; }

        private LLFormat LLDisplayFmt { get; set; }

        private string LastLat { get; set; }

        private string LastLon { get; set; }

        private string LastAddTheater { get; set; }

        private string LastAddCampaign { get; set; }

        private POIFilterSpec POIFilter { get; set; }

        // ---- read-only properties

        private readonly Dictionary<LLFormat, int> _llFmtToIndexMap;
        private readonly Dictionary<string, LLFormat> _llFmtTextToFmtMap;

        private readonly Dictionary<string, TextBox> _curPoIFieldValueMap;
        private readonly List<TextBox> _poiFieldValues;
        private readonly Brush _defaultBorderBrush;
        private readonly Brush _defaultBkgndBrush;

        // ---- constructed properties

        private bool IsFiltered => !POIFilter.IsDefault;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public EditPointsOfInterestPage()
        {
            JAFDTC.App curApp = Application.Current as JAFDTC.App;
            curApp.Window.Closed += AppWindow_Closed;

            InitializeComponent();

            POIFilter = new(Settings.LastPOIFilter);

            LLDisplayFmt = LLFormat.DDM_P3ZF;
            CurPoIItems = [ ];

            EditPoI = new PoIDetails();
            EditPoI.ErrorsChanged += PoIField_DataValidationError;
            for (int i = 0; i < EditPoI.LL.Length; i++)
                EditPoI.LL[i].ErrorsChanged += PoIField_DataValidationError;

            // NOTE: these need to be kept in sync with PoIDetails and the xaml.
            //
            _llFmtToIndexMap = new Dictionary<LLFormat, int>()
            {
                [LLFormat.DDU] = 0,
                [LLFormat.DMS] = 1,
                [LLFormat.DDM_P3ZF] = 2
            };
            _llFmtTextToFmtMap = new Dictionary<string, LLFormat>()
            {
                ["Decimal Degrees"] = LLFormat.DDU,
                ["Degrees, Minutes, Seconds"] = LLFormat.DMS,
                ["Degrees, Decimal Minutes"] = LLFormat.DDM_P3ZF,
            };

            _curPoIFieldValueMap = new Dictionary<string, TextBox>()
            {
                ["LatUI"] = uiPoIValueLatDDM,
                ["LonUI"] = uiPoIValueLonDDM,
                ["Alt"] = uiPoIValueAlt,
                ["Name"] = uiPoIValueName,
                ["Tags"] = uiPoIValueTags
            };
            _poiFieldValues =
            [
                uiPoIValueLatDD, uiPoIValueLatDDM, uiPoIValueLatDMS, uiPoIValueLonDD, uiPoIValueLonDDM, uiPoIValueLonDMS
            ];
            _defaultBorderBrush = uiPoIValueLatDDM.BorderBrush;
            _defaultBkgndBrush = uiPoIValueLatDDM.Background;

            EditPoI.CurIndexLL = _llFmtToIndexMap[LLDisplayFmt];

            uiBarBtnFilter.IsChecked = IsFiltered;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // field validation
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// set the border brush and background for a TextBox based on validity. valid fields use the defaults, invalid
        /// fields use ErrorFieldBorderBrush from the resources.
        /// </summary>
        private void SetFieldValidState(TextBox field, bool isValid)
        {
            field.BorderBrush = (isValid) ? _defaultBorderBrush : (SolidColorBrush)Resources["ErrorFieldBorderBrush"];
            field.Background = (isValid) ? _defaultBkgndBrush : (SolidColorBrush)Resources["ErrorFieldBackgroundBrush"];
        }

        private void ValidateAllFields(Dictionary<string, TextBox> fields, IEnumerable errors)
        {
            Dictionary<string, bool> map = [ ];
            foreach (string error in errors)
                map[error] = true;
            foreach (KeyValuePair<string, TextBox> kvp in fields)
                SetFieldValidState(kvp.Value, !map.ContainsKey(kvp.Key) || EditPoI.IsEmpty);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void PoIField_DataValidationError(object sender, DataErrorsChangedEventArgs args)
        {
            if (args.PropertyName == null)
            {
// TODO: this is not right for LatUI, LonUI
                ValidateAllFields(_curPoIFieldValueMap, EditPoI.GetErrors(null));
            }
            else
            {
                bool isValid = ((List<string>)EditPoI.GetErrors(args.PropertyName)).Count == 0;
                if ((args.PropertyName == "LatUI") || (args.PropertyName == "LonUI"))
                {
                    int index = _llFmtToIndexMap[LLDisplayFmt];
                    isValid = ((List<string>)EditPoI.LL[index].GetErrors(args.PropertyName)).Count == 0;
                }
                if (_curPoIFieldValueMap.TryGetValue(args.PropertyName, out TextBox value))
                    SetFieldValidState(value, isValid || EditPoI.IsEmpty);
            }
            RebuildInterfaceState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui utility
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// build out the data source and so on necessary for the map window and create it.
        /// </summary>
        private void CoreOpenMap(bool isMapWindowActive)
        {
            MapWindow = new()
            {
                MarkerExplainer = this,
                OpenMask = MapMarkerInfo.MarkerTypeMask.POI_USER |
                           MapMarkerInfo.MarkerTypeMask.POI_CAMPAIGN |
                           MapMarkerInfo.MarkerTypeMask.POI_SYSTEM,
                EditMask = MapMarkerInfo.MarkerTypeMask.POI_USER | MapMarkerInfo.MarkerTypeMask.POI_CAMPAIGN,
                CoordFormat = LLDisplayFmt,
                MaxRouteLength = 0,
                CanOpenMarker = false
            };
            MapWindow.Closed += MapWindow_Closed;
            MapWindow.RegisterMapControlVerbObserver(this);

            RebuildPoIList();

            MapWindow.Activate();

            // TODO: mirror selection? presently, RebuildPoIList clears the selection
            //              if (uiPoIListView.SelectedItem is PoIListItem item)
            //                  VerbMirror?.MirrorVerbMarkerSelected(this, new((MapMarkerInfo.MarkerType)item.PoI.Type,
            //                                                                 item.PoI.UniqueID, -1));
        }

        /// <summary>
        /// returns the index of the poi in the poi list with the given uid, -1 if no matching poi is found.
        /// </summary>
        private int FindIndexOfPoIByUID(string uid)
        {
            for (int i = 0; i < uiPoIListView.Items.Count; i++)
                if (uiPoIListView.Items[i] is PoIListItem item && (item.PoI.UniqueID == uid))
                    return i;
            return -1;
        }

        /// <summary>
        /// sets the lat/lon of the editor to match the specified lat/lon.
        /// </summary>
        private void SetEditObjectLatLon(string lat, string lon)
        {
            int index = _llFmtToIndexMap[LLDisplayFmt];
            EditPoI.LL[index].LatUI = Coord.ConvertFromLatDD(lat, LLDisplayFmt);
            EditPoI.LL[index].LonUI = Coord.ConvertFromLonDD(lon, LLDisplayFmt);

            int indexItem = FindIndexOfPoIByUID(EditPoI.SourceUID);
            if (indexItem != -1)
            {
                (uiPoIListView.Items[indexItem] as PoIListItem).LatUI = EditPoI.LL[index].LatUI;
                (uiPoIListView.Items[indexItem] as PoIListItem).LonUI = EditPoI.LL[index].LonUI;
            }
        }

        /// <summary>
        /// prompt the user to select an existing camapign for a subsequent opeation. the what parameter should
        /// be an uppercase description of what the function does ("Delete", "Export"). returns the selected
        /// campaign name, null if cancelled.
        /// </summary>
        private async Task<string> PromptForCampaign(string what)
        {
            GetListDialog listDialog = new(PointOfInterestDbase.Instance.KnownCampaigns, "Campaign")
            {
                XamlRoot = Content.XamlRoot,
                Title = $"{what} Campaign",
                PrimaryButtonText = what
            };
            ContentDialogResult result = await listDialog.ShowAsync();
            return (result == ContentDialogResult.Primary) ? listDialog.SelectedItem : null;
        }

        /// <summary>
        /// inform the user of a name collision in response to an attempt to add a new poi or edit an existing
        /// poi.
        /// </summary>
        private async void PromptForNameCollision(string name, string theater, string campaign, string tags)
        {
            string msg = (string.IsNullOrEmpty(tags)) ? "" : $" with tags “{tags}”";
            if (string.IsNullOrEmpty(campaign))
                msg = $"The user POI “{name}” in {theater}{msg}";
            else
                msg = $"The “{campaign}” campaign POI “{name}” in {theater}{msg}";
            await Utilities.Message1BDialog(Content.XamlRoot, "POI is not Unique",
                                            $"POIs must have unique name, campaign, theater, and tags within" +
                                            $" a POI type. {msg} is not unique.");
        }

        /// <summary>
        /// commit edit changes to a template point of interest. if the source uid of the template poi is null,
        /// the method creates a new point of interest, otherwise it applies the template to the poi identified
        /// by the source uid (note the uid may change depending on the edits in the template).
        /// </summary>
        private string CoreCommitEditChanges(PoIDetails tmpltPoI, string theater, string campaign,
                                             bool isForceUniqueName = false)
        {
            string editUID = null;

            PointOfInterest srcPoI = PointOfInterestDbase.Instance.Find(tmpltPoI.SourceUID);

            int index = _llFmtToIndexMap[LLDisplayFmt];
            PointOfInterest newPoI = new((string.IsNullOrEmpty(campaign)) ? PointOfInterestType.USER
                                                                          : PointOfInterestType.CAMPAIGN,
                                         theater, campaign, tmpltPoI.Name, tmpltPoI.Tags,
                                         tmpltPoI.LL[index].Lat, tmpltPoI.LL[index].Lon, tmpltPoI.Alt);

            string nameBase = newPoI.Name;
            int sequence = 1;
            bool isUnique;
            do
            {
                isUnique = (PointOfInterestDbase.Instance.Find(newPoI.UniqueID) == null);
                if ((srcPoI != null) && (srcPoI.UniqueID == newPoI.UniqueID))
                    isUnique = true;
                else if (!isUnique)
                    newPoI.Name = $"{nameBase} {sequence++}";
            }
            while (!isUnique && isForceUniqueName);

            if (isUnique && (srcPoI != null))
            {
                // updating existing poi (srcPoI). apply the edits and update the other verb observers. if the uid
                // changes, we'll delete the old uid and add the marker back with the new uid; otherwise, we'll move
                // it to its location.
                //
                string origUID = srcPoI.UniqueID;
                PointOfInterestDbase.Instance.EditPointOfInterestBegins(srcPoI);
                srcPoI.Theater = theater;
                srcPoI.Campaign = campaign;
                srcPoI.Name = newPoI.Name;
                srcPoI.Tags = PointOfInterest.SanitizedTags(tmpltPoI.Tags);
                srcPoI.Elevation = tmpltPoI.Alt;
                srcPoI.Latitude = tmpltPoI.LL[index].Lat;
                srcPoI.Longitude = tmpltPoI.LL[index].Lon;
                PointOfInterestDbase.Instance.EditPointOfInterestEnds(srcPoI);
                PointOfInterestDbase.Instance.Save(srcPoI.Campaign);
                editUID = srcPoI.UniqueID;

                if (origUID != editUID)
                {
                    VerbMirror?.MirrorVerbMarkerDeleted(this, new((MapMarkerInfo.MarkerType)newPoI.Type, origUID, -1));
                    VerbMirror?.MirrorVerbMarkerAdded(this, new((MapMarkerInfo.MarkerType)newPoI.Type, editUID, 
                                                                -1, newPoI.Latitude, newPoI.Longitude));
                }
                else
                {
                    VerbMirror?.MirrorVerbMarkerMoved(this, new((MapMarkerInfo.MarkerType)newPoI.Type, newPoI.UniqueID,
                                                                -1, newPoI.Latitude, newPoI.Longitude));
                }
            }
            else if (isUnique)
            {
                // adding new poi. do that, then update verb observers.
                //
                PointOfInterestDbase.Instance.AddPointOfInterest(newPoI);
                editUID = newPoI.UniqueID;

                VerbMirror?.MirrorVerbMarkerAdded(this, new((MapMarkerInfo.MarkerType)newPoI.Type, newPoI.UniqueID, -1,
                                                            newPoI.Latitude, newPoI.Longitude));
            }

            return editUID;
        }

        /// <summary>
        /// return a dictionary, keyed by PointOfInterestType that breaks the selection up by point of interest type.
        /// the dictionary will only contain a key of a given type if there were points of interest of that type in
        /// the selection.
        /// </summary>
        private Dictionary<PointOfInterestType, List<PointOfInterest>> CrackSelectedPoIsByType()
        {
            Dictionary<PointOfInterestType, List<PointOfInterest>> cracked = [ ];
            foreach (PoIListItem item in uiPoIListView.SelectedItems.Cast<PoIListItem>())
            {
                if (!cracked.ContainsKey(item.PoI.Type))
                    cracked[item.PoI.Type] = [ ];
                cracked[item.PoI.Type].Add(item.PoI);
            }
            return cracked;
        }

        /// <summary>
        /// returns list of campaigns in selection.
        /// </summary>
        private List<string> CrackSelectedPoIsByCampaign(Dictionary<PointOfInterestType,
                                                                    List<PointOfInterest>> crackedSel = null)
        {
            List<string> campaigns = [ ];
            crackedSel ??= CrackSelectedPoIsByType();
            foreach (PointOfInterest poi in crackedSel.GetValueOrDefault(PointOfInterestType.CAMPAIGN, [ ]))
                if (!campaigns.Contains(poi.Campaign))
                    campaigns.Add(poi.Campaign);
            return campaigns;
        }

        /// <summary>
        /// return a list of points of interest matching the current filter configuration with a name that
        /// containst the provided name fragment.
        /// </summary>
        private List<PointOfInterest> GetPoIsMatchingFilter(string name = null)
        {
            PointOfInterestDbQuery query = new(POIFilter.IncludeTypes, POIFilter.Theater, POIFilter.Campaign,
                                               name, POIFilter.Tags, PointOfInterestDbQueryFlags.NAME_PARTIAL_MATCH);
            return PointOfInterestDbase.Instance.Find(query, true);
        }

        /// <summary>
        /// update the coordinate format used in the poi lat/lon specification. there are per-format fields to
        /// deal with the apparent inability to change masks dynamically. show the field for the current format,
        /// hide all others. updates LLDisplayFmt.
        /// </summary>
        private void ChangeCoordFormat(LLFormat fmt)
        {
            switch (fmt)
            {
                case LLFormat.DDM_P3ZF:
                    _curPoIFieldValueMap["LatUI"] = uiPoIValueLatDDM;
                    _curPoIFieldValueMap["LonUI"] = uiPoIValueLonDDM;
                    break;
                case LLFormat.DMS:
                    _curPoIFieldValueMap["LatUI"] = uiPoIValueLatDMS;
                    _curPoIFieldValueMap["LonUI"] = uiPoIValueLonDMS;
                    break;
                default:
                    _curPoIFieldValueMap["LatUI"] = uiPoIValueLatDD;
                    _curPoIFieldValueMap["LonUI"] = uiPoIValueLonDD;
                    break;
            }

            foreach (TextBox tbox in _poiFieldValues)
                tbox.Visibility = Visibility.Collapsed;
            _curPoIFieldValueMap["LatUI"].Visibility = Visibility.Visible;
            _curPoIFieldValueMap["LonUI"].Visibility = Visibility.Visible;

            LLDisplayFmt = fmt;
            EditPoI.CurIndexLL = _llFmtToIndexMap[LLDisplayFmt];

            if (MapWindow != null)
                MapWindow.CoordFormat = fmt;
        }

        /// <summary>
        /// rebuild the content of the point of interest list based on the current contents of the poi database
        /// along with the currently selected theater, tags, and included types from the filter specification.
        /// name specifies the partial name to match, null if no match on name.
        /// </summary>
        private void RebuildPoIList(string name = null)
        {
// TODO: preserve selection, or nah?
            CurPoIItems.Clear();
            Dictionary<string, PointOfInterest> marks = [];
            foreach (PointOfInterest poi in GetPoIsMatchingFilter(name))
            {
                CurPoIItems.Add(new PoIListItem(poi, LLDisplayFmt));
                marks[poi.UniqueID] = poi;
            }
            if (MapWindow != null)
            {
                MapWindow.Theater = (string.IsNullOrEmpty(POIFilter.Theater)) ? "All Theaters" : POIFilter.Theater;
                MapWindow.SetupMapContent([ ], marks);
            }
        }

        /// <summary>
        /// rebuild the theater text to match coordinates in the editor.
        /// </summary>
        private void RebuildEditorTheater()
        {
            int index = _llFmtToIndexMap[LLDisplayFmt];
            if (!string.IsNullOrEmpty(EditPoI.SourceUID))
            {
                PointOfInterest poi = PointOfInterestDbase.Instance.Find(EditPoI.SourceUID);
                uiPoITextTheater.Text = poi.Theater;
            }
            else
            {
                EditPoI.CurTheaters = Theater.TheatersForCoords(EditPoI.LL[index].Lat, EditPoI.LL[index].Lon);
                if ((EditPoI.CurTheaters == null) || (EditPoI.CurTheaters.Count == 0))
                    uiPoITextTheater.Text = "Unknown Theater";
                else if (EditPoI.CurTheaters.Count == 1)
                    uiPoITextTheater.Text = EditPoI.CurTheaters[0];
                else
                    uiPoITextTheater.Text = "Multiple Theaters";
            }
        }

        /// <summary>
        /// rebuild the title of the action button based on the current values in the point of interest editor.
        /// if we have a source uid, we are updating; otherwise we are adding.
        /// </summary>
        private void RebuildActionButtonTitles()
        {
            uiPoITextBtnAdd.Text = (string.IsNullOrEmpty(EditPoI.SourceUID)) ? "Add" : "Update";
            uiPoITextBtnClear.Text = (EditPoI.IsDirty) ? "Reset" : "Clear";
        }

        /// <summary>
        /// rebuild the enable state of controls on the page.
        /// </summary>
        private void RebuildEnableState()
        {
            JAFDTC.App curApp = Application.Current as JAFDTC.App;

            Dictionary<PointOfInterestType, List<PointOfInterest>> selectionByType = CrackSelectedPoIsByType();
            List<string> selectionByCampaign = CrackSelectedPoIsByCampaign(selectionByType);
            bool isCoreInSel = selectionByType.ContainsKey(PointOfInterestType.SYSTEM);
            bool isUserInSel = selectionByType.ContainsKey(PointOfInterestType.USER);
            bool isCampaignInSel = selectionByType.ContainsKey(PointOfInterestType.CAMPAIGN);
            bool isMultCampaignSel = (selectionByCampaign.Count > 1);

            bool isExportable = !isCoreInSel && (( isUserInSel && !isCampaignInSel) ||
                                                 (!isUserInSel &&  isCampaignInSel && !isMultCampaignSel));

            foreach (TextBox elem in _curPoIFieldValueMap.Values)
                Utilities.SetEnableState(elem, (uiPoIListView.SelectedItems.Count <= 1) && !isCoreInSel);

            bool isPoIValid = !string.IsNullOrEmpty(uiPoIValueName.Text) &&
                              !string.IsNullOrEmpty(uiPoIValueAlt.Text) &&
                              ((EditPoI.CurTheaters != null) && (EditPoI.CurTheaters.Count > 0)) &&
                              !EditPoI.HasErrors;

            Utilities.SetEnableState(uiPoIBtnAdd, EditPoI.IsDirty && isPoIValid);
            Utilities.SetEnableState(uiPoIBtnClear, !EditPoI.IsEmpty);
            Utilities.SetEnableState(uiPoIBtnCapture, curApp.IsDCSAvailable);

            bool isCampaigns = (PointOfInterestDbase.Instance.KnownCampaigns.Count > 0);

            Utilities.SetEnableState(uiBarBtnCopyUser, uiPoIListView.SelectedItems.Count == 1);
            Utilities.SetEnableState(uiBarBtnCopyCampaign, isCampaigns && (uiPoIListView.SelectedItems.Count > 0));
            Utilities.SetEnableState(uiBarBtnDelete, !isCoreInSel);
            Utilities.SetEnableState(uiBarBtnExport, isExportable);

            Utilities.SetEnableState(uiBarBtnDeleteCamp, isCampaigns);

            List<string> errors = EditPoI.GetErrorsWithEmpty(EditPoI.IsEmpty);
            ValidateAllFields(_curPoIFieldValueMap, errors);
        }

        /// <summary>
        /// rebuild user interface state such as the enable state of the command bars.
        /// </summary>
        private void RebuildInterfaceState()
        {
            if (!IsRebuildPending)
            {
                IsRebuildPending = true;
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    RebuildActionButtonTitles();
                    RebuildEditorTheater();
                    RebuildEnableState();
                    IsRebuildPending = false;
                });
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- buttons -----------------------------------------------------------------------------------------------

        /// <summary>
        /// back button click: navigate back to the configuration list.
        /// </summary>
        private void HdrBtnBack_Click(object sender, RoutedEventArgs args)
        {
            Frame.GoBack();
        }

        // ---- name search box ---------------------------------------------------------------------------------------

        /// <summary>
        /// filter box text changed: update the items in the search box based on the value in the field.
        /// </summary>
        private void PoINameFilterBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                List<string> suitableItems = [ ];
                List<PointOfInterest> pois = GetPoIsMatchingFilter(sender.Text);
                if (pois.Count == 0)
                    suitableItems.Add("No Matching Points of Interest Found");
                else
                    foreach (PointOfInterest poi in pois)
                        suitableItems.Add(poi.Name);
                sender.ItemsSource = suitableItems;
            }
        }

        /// <summary>
        /// filter box query submitted: apply the query text filter to the pois listed in the poi list.
        /// </summary>
        private void PoINameFilterBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            RebuildPoIList(args.QueryText);
            RebuildInterfaceState();
        }

        // ---- command bar / commands --------------------------------------------------------------------------------

        /// <summary>
        /// filter command click: change the filter configuration via the filter specification dialog and update the
        /// displayed pois appropriately.
        /// </summary>
        private async void CmdFilter_Click(object sender, RoutedEventArgs args)
        {
            AppBarToggleButton button = (AppBarToggleButton)sender;
            if (button.IsChecked != IsFiltered)
                button.IsChecked = IsFiltered;

            PoIFilterDialog filterDialog = new(POIFilter)
            {
                XamlRoot = Content.XamlRoot,
                Title = $"Point of Interest Database Filter"
            };
            ContentDialogResult result = await filterDialog.ShowAsync(ContentDialogPlacement.Popup);
            if (result == ContentDialogResult.Primary)
                POIFilter = new(filterDialog.Filter);
            else if (result == ContentDialogResult.Secondary)
                POIFilter = new();
            else
                return;                                         // EXIT: cancelled, no change...

            button.IsChecked = IsFiltered;

            Settings.LastPOIFilter = POIFilter;

            uiPoIListView.SelectedItems.Clear();
            RebuildPoIList();
            RebuildInterfaceState();
        }

        /// <summary>
        /// edit command click: copy the name, latitude, longitude, and elevation of the currnetly selected point of
        /// interest into the poi editor fields and rebuild the interface state to reflect the change. this should
        /// only be called on read-only campaign and system pois.
        /// </summary>
        private void CmdCopyUser_Click(object sender, RoutedEventArgs args)
        {
            int index = _llFmtToIndexMap[LLDisplayFmt];
            foreach (PoIListItem item in uiPoIListView.SelectedItems.Cast<PoIListItem>())
            {
                PoIDetails newPoI = new(item.PoI, LLDisplayFmt, index)
                {
                    Name = $"{item.PoI.Name} - User Copy",
                    SourceUID = null                                        // null creates new poi
                };
                if (CoreCommitEditChanges(newPoI, item.PoI.Theater, null, true) == null)
                {
                    PromptForNameCollision(newPoI.Name, item.PoI.Theater, null, newPoI.Tags);
                    break;
                }
            }
            RebuildPoIList();
            RebuildInterfaceState();
        }

        /// <summary>
        /// copy to campaign click: prompt the user for a campaign then copy the points of interest that are not
        /// already part of that campaign to the campaign.
        /// </summary>
        private async void CmdCopyCampaign_Click(object sender, RoutedEventArgs args)
        {
            string campaign = await PromptForCampaign("Copy To");
            if (campaign != null)
            {
                int index = _llFmtToIndexMap[LLDisplayFmt];
                foreach (PoIListItem item in uiPoIListView.SelectedItems.Cast<PoIListItem>())
                    if (string.IsNullOrEmpty(item.PoI.Campaign) || (item.PoI.Campaign != campaign))
                    {
                        PoIDetails newPoI = new(item.PoI, LLDisplayFmt, index)
                        {
                            Name = $"{item.PoI.Name} - {campaign} Copy",
                            SourceUID = null                                // null creates new poi
                        };
                        if (CoreCommitEditChanges(newPoI, item.PoI.Theater, campaign, true) == null)
                        {
                            PromptForNameCollision(newPoI.Name, item.PoI.Theater, campaign, newPoI.Tags);
                            break;
                        }
                    }
                RebuildPoIList();
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// delete command click: remove the selected points of interest from the points of interest database.
        /// dcs core pois are skipped.
        /// </summary>
        private async void CmdDelete_Click(object sender, RoutedEventArgs args)
        {
            if (uiPoIListView.SelectedItems.Count > 0)
            {
                string message = (uiPoIListView.SelectedItems.Count > 1) ? "delete these points of interest?"
                                                                         : "delete this point of interest?";
                ContentDialogResult result = await Utilities.Message2BDialog(
                    Content.XamlRoot,
                    "Delete Point" + ((uiPoIListView.SelectedItems.Count > 1) ? "s" : "") + " of Interest?",
                    $"Are you sure you want to {message} This action cannot be undone.",
                    "Delete"
                );
                if (result == ContentDialogResult.Primary)
                {
                    Dictionary<string, bool> campaignsModified = [];
                    Dictionary<string, bool> campaignsDeleted = [];
                    foreach (PoIListItem item in uiPoIListView.SelectedItems.Cast<PoIListItem>())
                        if (item.PoI.Type != PointOfInterestType.SYSTEM)
                        {
                            campaignsModified[(string.IsNullOrEmpty(item.PoI.Campaign)) ? "<u>" :  item.PoI.Campaign] = true;
                            if (PointOfInterestDbase.Instance.CountPoIInCampaign(item.PoI.Campaign) == 1)
                            {
                                campaignsDeleted[item.PoI.Campaign] = true;
                                PointOfInterestDbase.Instance.DeleteCampaign(item.PoI.Campaign);
                            }
                            else
                            {
                                PointOfInterestDbase.Instance.RemovePointOfInterest(item.PoI, false);

                                VerbMirror?.MirrorVerbMarkerDeleted(this, new((MapMarkerInfo.MarkerType)item.PoI.Type,
                                                                              item.PoI.UniqueID));
                            }
                        }
                    foreach (string campaign in campaignsModified.Keys)
                        PointOfInterestDbase.Instance.Save((campaign != "<u>") ? campaign : null);

                    RebuildPoIList();
                    RebuildInterfaceState();

                    if (campaignsDeleted.Count > 0)
                    {
                        string msg;
                        List<string> campaigns = [.. campaignsDeleted.Keys ];
                        if (campaigns.Count == 1)
                            msg = $"campaign {campaigns[0]}. This campaign has";
                        else if (campaigns.Count == 2)
                            msg = $"campaigns {campaigns[0]} and {campaigns[1]}. These campaigns have";
                        else
                            msg = $"campaigns " + string.Join(", ", campaigns.GetRange(0, campaigns.Count - 1)) +
                                  $", and {campaigns[^1]}. These campaigns have";

                        await Utilities.Message1BDialog(
                            Content.XamlRoot,
                            $"Deleted Empty Campaign" + ((campaignsDeleted.Count > 1) ? "s" : ""),
                            $"The delete removed all points of interest from the {msg} been deleted as well.");
                    }
                }
            }
        }

        /// <summary>
        /// map command: if the map window is not currently open, build out the data source and so on necessary for
        /// the window and create it. otherwise, activate the window.
        /// </summary>
        private void CmdMap_Click(object sender, RoutedEventArgs args)
        {
            if (MapWindow == null)
                CoreOpenMap(true);
            else
                MapWindow.Activate();
        }

        /// <summary>
        /// import command click: prompt the user for a file to import points of interest from and deserialize
        /// the contents of the file into the points of interest database. the database is saved following the
        /// import.
        /// </summary>
        private async void CmdImport_Click(object sender, RoutedEventArgs args)
        {
// TODO: could probably handle this without closing map window...
            MapWindow?.Close();

            bool? isSuccess = await ExchangePOIUIHelper.ImportFile(Content.XamlRoot, PointOfInterestDbase.Instance);
            if (isSuccess == true)
            {
                RebuildPoIList();
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// export command click: prompt the user for a file to export the selected points of interest to and
        /// serialize the selected pois to a file. we will make some modifications to what is exported based
        /// on user responses: campaign pois may be added to export entire campaigns if not all campaign pois
        /// are selected.
        /// 
        /// we assume contorls triggering CmdExport_Click are disabled when exports are not allowed to ensure
        /// that export files either (1) contain only user pois, or (2) contain only pois from a single campign.
        /// </summary>
        private void CmdExport_Click(object sender, RoutedEventArgs args)
        {
            Dictionary<PointOfInterestType, List<PointOfInterest>> selectionByType = CrackSelectedPoIsByType();

            if (selectionByType.TryGetValue(PointOfInterestType.USER, out List<PointOfInterest> userPoIs))
                ExchangePOIUIHelper.ExportFileForUser(Content.XamlRoot, userPoIs);
            else if (selectionByType.TryGetValue(PointOfInterestType.CAMPAIGN, out List<PointOfInterest> campaignPoIs))
                ExchangePOIUIHelper.ExportFileForCampaign(Content.XamlRoot, campaignPoIs);
        }

        // ---- campaign commands -------------------------------------------------------------------------------------

        /// <summary>
        /// add campaign command click: prompt the user for the name of a new campaign and add it to the point of
        /// interest database.
        /// </summary>
        private async void CmdAddCampaign_Click(object sender, RoutedEventArgs args)
        {
            GetNameDialog nameDialog = new(null, $"Select a name for the campaign to add")
            {
                XamlRoot = Content.XamlRoot,
                Title = $"Add Campaign"
            };
            ContentDialog errDialog = new()
            {
                XamlRoot = Content.XamlRoot,
                Title = "Invalid Campaign Name",
                PrimaryButtonText = "OK",
            };

            string campaignName = null;
            ContentDialogResult result;
            while (true)
            {
                result = await nameDialog.ShowAsync();
                if (result == ContentDialogResult.None)
                    break;
                campaignName = nameDialog.Value.Trim(' ');
                string message = null;
                foreach (string campaign in PointOfInterestDbase.Instance.KnownCampaigns)
                    if (campaignName.Equals(campaign, StringComparison.CurrentCultureIgnoreCase))
                    {
                        message = $"There is already a campaign named “{campaignName}” in the database. Please select a different name.";
                        break;
                    }
                if (!CampaignNameRegex().IsMatch(campaignName))
                    message = $"Campaign name “{campaignName}” may only contain alphanumeric characters. Please select a different name.";
                if (message == null)
                    break;
                errDialog.Content = message;
                await errDialog.ShowAsync();
            }
            if (result == ContentDialogResult.Primary)
            {
                PointOfInterestDbase.Instance.AddCampaign(campaignName);
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// delete campaign command click: prompt the user to select an existing campaign and delete it from the point
        /// of interest database.
        /// </summary>
        private async void CmdDeleteCampaign_Click(object sender, RoutedEventArgs args)
        {
            string campaign = await PromptForCampaign("Delete");
            if (campaign != null)
            {
                PointOfInterestDbQuery query = new(PointOfInterestTypeMask.CAMPAIGN, null, campaign);
                foreach (PointOfInterest poi in PointOfInterestDbase.Instance.Find(query))
                    VerbMirror?.MirrorVerbMarkerDeleted(this, new((MapMarkerInfo.MarkerType)poi.Type, poi.UniqueID));

                PointOfInterestDbase.Instance.DeleteCampaign(campaign);
                RebuildPoIList();
                RebuildInterfaceState();
            }
        }

        // ---- coordinate setup --------------------------------------------------------------------------------------

        /// <summary>
        /// select coordinate format click: present the user with a list dialog to select the display format for poi
        /// coordinates and update the ui to reflect the new choice.
        /// </summary>
        private async void CmdCoords_Click(object sender, RoutedEventArgs args)
        {
            GetListDialog coordList = new([.. _llFmtTextToFmtMap.Keys ], null, 0, _llFmtToIndexMap[LLDisplayFmt])
            {
                XamlRoot = Content.XamlRoot,
                Title = $"Select a Coordinate Format",
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel"
            };
            ContentDialogResult result = await coordList.ShowAsync(ContentDialogPlacement.Popup);
            if (result == ContentDialogResult.Primary)
            {
                Settings.LastPOICoordFmtSelection = _llFmtTextToFmtMap[coordList.SelectedItem];
                ChangeCoordFormat(_llFmtTextToFmtMap[coordList.SelectedItem]);
                RebuildPoIList();
                RebuildInterfaceState();
            }
        }

        // ---- poi list ----------------------------------------------------------------------------------------------

        /// <summary>
        /// poi list view right click: show context menu
        /// </summary>
        private void PoIListView_RightTapped(object sender, RightTappedRoutedEventArgs args)
        {
            ListView listView = sender as ListView;
            PoIListItem poi = ((FrameworkElement)args.OriginalSource).DataContext as PoIListItem;

            // check if the tapped item is selected. if the right tap occurs outside of the selection, change
            // up the selection to just be the tapped item.
            //
            int index = CurPoIItems.IndexOf(poi);
            bool isTappedItemSelected = false;
            foreach (ItemIndexRange range in listView.SelectedRanges)
                if ((index >= range.FirstIndex) && (index <= range.LastIndex))
                {
                    isTappedItemSelected = true;
                    break;
                }
            if (!isTappedItemSelected)
            {
                listView.SelectedIndex = CurPoIItems.IndexOf(poi);
                RebuildInterfaceState();
            }
            
            // set up enables based on items selected. rely on the command handlers to properly handle situations
            // where there are a mix of types and behave correctly (e.g., not delete core items selected).
            //
            Dictionary<PointOfInterestType, List<PointOfInterest>> selectionByType = CrackSelectedPoIsByType();
            List<string> selectionByCampaign = CrackSelectedPoIsByCampaign(selectionByType);
            bool isCoreInSel = selectionByType.ContainsKey(PointOfInterestType.SYSTEM);
            bool isUserInSel = selectionByType.ContainsKey(PointOfInterestType.USER);
            bool isCampaignInSel = selectionByType.ContainsKey(PointOfInterestType.CAMPAIGN);
            bool isMultCampaignSel = (selectionByCampaign.Count > 1);

            bool isExportable = !isCoreInSel && ((isUserInSel && !isCampaignInSel) ||
                                                 (!isUserInSel && isCampaignInSel && !isMultCampaignSel));

            bool isSelect = (uiPoIListView.SelectedItems.Count > 0);
            uiPoiListCtxMenuFlyout.Items[0].IsEnabled = isSelect;                                   // copy to user
            uiPoiListCtxMenuFlyout.Items[1].IsEnabled = isSelect;                                   // copy to campaign
            uiPoiListCtxMenuFlyout.Items[3].IsEnabled = isExportable;                               // export
            uiPoiListCtxMenuFlyout.Items[5].IsEnabled = !isCoreInSel;                               // delete

            uiPoiListCtxMenuFlyout.ShowAt((ListView)sender, args.GetPosition(listView));
        }

        /// <summary>
        /// poi list view selection changed: rebuild the interface state to reflect the newly selected poi(s).
        /// </summary>
        private void PoIListView_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (uiPoIListView.SelectedItems.Count != 1)
            {
                EditPoI.Reset();
// TODO: this will clear map window selection when we have multiple things selected in the poi list.
// TODO: this likely needs to change if multi-selection is ever supported in the map window.
                if (!IsVerbEvent)
                    VerbMirror?.MirrorVerbMarkerSelected(this, new());
            }
            else if (uiPoIListView.SelectedItems.Count == 1)
            {
                PoIListItem item = uiPoIListView.SelectedItem as PoIListItem;
                int index = _llFmtToIndexMap[LLDisplayFmt];

                EditPoI.CurTheaters = Theater.TheatersForCoords(item.PoI.Latitude, item.PoI.Longitude);
                EditPoI.SourceUID = item.PoI.UniqueID;
                EditPoI.Name = item.PoI.Name;
                EditPoI.Tags = PointOfInterest.SanitizedTags(item.PoI.Tags);
                EditPoI.LL[index].LatUI = Coord.ConvertFromLatDD(item.PoI.Latitude, LLDisplayFmt);
                EditPoI.LL[index].LonUI = Coord.ConvertFromLonDD(item.PoI.Longitude, LLDisplayFmt);
                EditPoI.Alt = item.PoI.Elevation;

                if (!IsVerbEvent)
                    VerbMirror?.MirrorVerbMarkerSelected(this, new((MapMarkerInfo.MarkerType)item.PoI.Type,
                                                                   item.PoI.UniqueID, -1));
            }
            RebuildInterfaceState();
        }

        /// <summary>
        /// poi add/update button click: add a new poi or update an existing poi with the values from the poi editor.
        /// </summary>
        private async void PoIBtnAdd_Click(object sender, RoutedEventArgs args)
        {
            string newUID;

            if (string.IsNullOrEmpty(EditPoI.SourceUID))
            {
                // no source uid on the edit poi implies we are creating a new poi from the data in the editor
                // fields. determine if the new poi should be a user poi or part of a campaign and what theater
                // it should be placed in by asking the user in situations where there is ambiguity. once we have
                // that information, create the poi from the edit poi, ensuring new poi has unique parameters.

                List<string> campaigns = PointOfInterestDbase.Instance.KnownCampaigns;
                campaigns.Insert(0, $"Add “{EditPoI.Name}” as a User POI");

                if ((campaigns.Count > 1) || (EditPoI.CurTheaters.Count > 1))
                {
                    GetPoITagDetails tagDialog = new(campaigns, LastAddCampaign, EditPoI.CurTheaters, LastAddTheater)
                    {
                        XamlRoot = Content.XamlRoot,
                        Title = "Select POI Parameters",
                        PrimaryButtonText = "OK",
                        CloseButtonText = "Cancel"
                    };
                    ContentDialogResult result = await tagDialog.ShowAsync(ContentDialogPlacement.Popup);
                    if (result == ContentDialogResult.None)
                        return;                                         // **** EXITS: user cancel

                    LastAddCampaign = (tagDialog.Campaign.StartsWith("Add “")) ? null : tagDialog.Campaign;
                    LastAddTheater = tagDialog.Theater;
                }
                if (campaigns.Count == 1)
                    LastAddCampaign = null;
                if (EditPoI.CurTheaters.Count == 1)
                    LastAddTheater = EditPoI.CurTheaters[0];

                newUID = CoreCommitEditChanges(EditPoI, LastAddTheater, LastAddCampaign);
            }
            else
            {
                // a source uid on the edit poi implies we are updating an existing poi in the database. this
                // action may change the theater, but cannot change the campaign. prompt for a new theater if
                // potential theaters in edit poi differ from the potential theaters for the poi's current
                // location; otherwise, we'll use the poi's current theater. once we have that information,
                // update the poi from the edit poi, ensuring new poi has unique parameters.

                PointOfInterest poi = PointOfInterestDbase.Instance.Find(EditPoI.SourceUID);
                List<string> poiTheaters = Theater.TheatersForCoords(poi.Latitude, poi.Longitude);

                if (!poiTheaters.SequenceEqual(EditPoI.CurTheaters) || !poiTheaters.Contains(poi.Theater))
                {
                    GetPoITagDetails tagDialog = new(null, null, EditPoI.CurTheaters, LastAddTheater)
                    {
                        XamlRoot = Content.XamlRoot,
                        Title = "Select POI Theater",
                        PrimaryButtonText = "OK",
                        CloseButtonText = "Cancel"
                    };
                    ContentDialogResult result = await tagDialog.ShowAsync(ContentDialogPlacement.Popup);
                    if (result == ContentDialogResult.None)
                        return;                                         // **** EXITS: user cancel

                    LastAddTheater = tagDialog.Theater;
                }
                if (EditPoI.CurTheaters.Count == 1)
                    LastAddTheater = EditPoI.CurTheaters[0];
                else if (poiTheaters.Contains(poi.Theater))
                    LastAddTheater = poi.Theater;

                newUID = CoreCommitEditChanges(EditPoI, LastAddTheater, poi.Campaign);
            }

            if (newUID == null)
            {
                PromptForNameCollision(EditPoI.Name, LastAddTheater, LastAddCampaign, EditPoI.Tags);
            }
            else
            {
                EditPoI.Reset();
                RebuildPoIList();
                RebuildInterfaceState();
            }
        }

        /// <summary>
        /// poi clear button click: reset (if editor is dirty and there's a selected item) or reset (otherwise)
        /// the poi editor.
        /// </summary>
        private void PoIBtnClear_Click(object sender, RoutedEventArgs args)
        {
            if (EditPoI.IsDirty && (uiPoIListView.SelectedItem is PoIListItem item))
            {
                int index = _llFmtToIndexMap[LLDisplayFmt];
                EditPoI.Name = item.PoI.Name;
                EditPoI.Tags = PointOfInterest.SanitizedTags(item.PoI.Tags);
                EditPoI.LL[index].LatUI = Coord.ConvertFromLatDD(item.PoI.Latitude, LLDisplayFmt);
                EditPoI.LL[index].LonUI = Coord.ConvertFromLonDD(item.PoI.Longitude, LLDisplayFmt);
                EditPoI.Alt = item.PoI.Elevation;
            }
            else
            {
                uiPoIListView.SelectedItems.Clear();
                EditPoI.Reset();
            }
            RebuildInterfaceState();
        }

        /// <summary>
        /// poi capture button click: launch jafdtc side of coordinate capture ui.
        /// </summary>
        private async void PoIBtnCapture_Click(object sender, RoutedEventArgs args)
        {
            WyptCaptureDataRx.Instance.WyptCaptureDataReceived += PoIBtnCapture_WyptCaptureDataReceived;
            await Utilities.CaptureSingleDialog(Content.XamlRoot, "Steerpoint");
            WyptCaptureDataRx.Instance.WyptCaptureDataReceived -= PoIBtnCapture_WyptCaptureDataReceived;
        }

        /// <summary>
        /// event handler for data received from the F10 waypoint capture. update the edited poi with the position of
        /// the location selected in dcs.
        /// </summary>
        private void PoIBtnCapture_WyptCaptureDataReceived(WyptCaptureData[] wypts)
        {
            // TODO: want to add multiple pois if multiple waypoints selected from f10?
            if ((wypts.Length > 0) && !wypts[0].IsTarget)
            {
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    int index = _llFmtToIndexMap[LLDisplayFmt];
                    EditPoI.Name = "DCS Capture";
                    EditPoI.LL[index].LatUI = Coord.ConvertFromLatDD(wypts[0].Latitude, LLDisplayFmt);
                    EditPoI.LL[index].LonUI = Coord.ConvertFromLonDD(wypts[0].Longitude, LLDisplayFmt);
                    EditPoI.Alt = wypts[0].Elevation.ToString();
                });
            }
        }

        // ---- text field changes ------------------------------------------------------------------------------------

        /// <summary>
        /// poi editor value changed: update the interface state to reflect changes in the text value.
        /// </summary>
        private void PoITextBox_TextChanged(object sender, TextChangedEventArgs args)
        {
            RebuildInterfaceState();
        }

        /// <summary>
        /// poi editor value field lost focus: update the interface state to reflect changes in the text value.
        /// </summary>
        private void PoITextBox_LostFocus(object sender, RoutedEventArgs args)
        {
            // HACK: 100% uncut cya. as the app is shutting down we can get lost focus events that may try to
            // HACK: operate on ui that has been torn down. in that case, return without doing anything.
            // HACK: this potentially prevents persisting changes made to the control prior to focus loss.
            //
            if ((Application.Current as JAFDTC.App).IsAppShuttingDown)
                return;

            RebuildInterfaceState();
        }

        // ---- window changes ----------------------------------------------------------------------------------------

        /// <summary>
        /// on closing the map window, null out the MapWindow instance and dump any event handlers.
        /// </summary>
        private void MapWindow_Closed(object sender, WindowEventArgs args)
        {
            MapWindow.Closed -= MapWindow_Closed;
            MapWindow = null;
        }

        /// <summary>
        /// on closing the main app window, close any open map window too.
        /// </summary>
        private void AppWindow_Closed(object sender, WindowEventArgs args)
        {
            MapWindow?.Close();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IMapControlMarkerExplainer
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns the display type of the marker with the specified information.
        /// </summary>
        public string MarkerDisplayType(MapMarkerInfo info) => NavpointUIHelper.MarkerDisplayType(info);

        /// <summary>
        /// returns the display name of the marker with the specified information.
        /// </summary>
        public string MarkerDisplayName(MapMarkerInfo info) => NavpointUIHelper.MarkerDisplayName(info);

        /// <summary>
        /// returns the elevation of the marker with the specified information.
        /// </summary>
        public string MarkerDisplayElevation(MapMarkerInfo info, string units = "")
            => NavpointUIHelper.MarkerDisplayElevation(info, units);

        // ------------------------------------------------------------------------------------------------------------
        //
        // IWorldMapControlVerbHandler
        //
        // ------------------------------------------------------------------------------------------------------------

        public string VerbHandlerTag => "EditPointsOfInterestPage";

        public IMapControlVerbMirror VerbMirror { get; set; }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerSelected(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"EPP:VerbMarkerSelected({param}) {info.Type}, {info.TagStr}, {info.TagInt}");
            if (info.Type == MapMarkerInfo.MarkerType.UNKNOWN)
            {
                IsVerbEvent = true;
                uiPoIListView.SelectedIndex = -1;
                IsVerbEvent = false;
            }
            else
            {
                IsVerbEvent = true;
                uiPoIListView.SelectedIndex = FindIndexOfPoIByUID(info.TagStr);
                uiPoIListView.ScrollIntoView(uiPoIListView.SelectedItem);
                IsVerbEvent = false;
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerOpened(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0) { }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerMoved(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"EPP:VerbMarkerMoved({param}) {info.Type}, {info.TagStr}, {info.TagInt}, {info.Lat}, {info.Lon}");
            PointOfInterest poi = PointOfInterestDbase.Instance.Find(info.TagStr);
            if ((poi != null) && (poi.UniqueID == EditPoI.SourceUID))
            {
                SetEditObjectLatLon(info.Lat, info.Lon);

                if (param == -1)
                {
                    LastLat = info.Lat;
                    LastLon = info.Lon;
                }
                else if ((param == 1) && Theater.TheatersForCoords(info.Lat, info.Lon).Contains(poi.Theater))
                {
                    PointOfInterestDbase.Instance.Save(poi.Campaign);
                }
                else if ((param == 1) && !string.IsNullOrEmpty(LastLat) && !string.IsNullOrEmpty(LastLon))
                {
                    DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
                    {
                        await Utilities.Message1BDialog(Content.XamlRoot, "Elvis Has Left the Building",
                                                        $"Point of interest has moved out of the {poi.Theater} theater." +
                                                        $" Restoring previous position.");

                        SetEditObjectLatLon(LastLat, LastLon);
                        VerbMirror?.MirrorVerbMarkerMoved(this, new((MapMarkerInfo.MarkerType) poi.Type, info.TagStr,
                                                                    info.TagInt, LastLat, LastLon), 1);
                        LastLat = null;
                        LastLon = null;
                        PointOfInterestDbase.Instance.Save(poi.Campaign);
                    });
                }
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerAdded(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"EPP:VerbMarkerAdded({param}) {info.Type}, {info.TagStr}, {info.TagInt}, {info.Lat}, {info.Lon}");
// TODO: nothing to do here until we support add via map window
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public void VerbMarkerDeleted(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0)
        {
            Debug.WriteLine($"EPP:VerbMarkerDeleted({param}) {info.Type}, {info.TagStr}, {info.TagInt}");
            PointOfInterest poi = PointOfInterestDbase.Instance.Find(info.TagStr);

            if (PointOfInterestDbase.Instance.CountPoIInCampaign(poi.Campaign) == 1)
                PointOfInterestDbase.Instance.DeleteCampaign(poi.Campaign);
            PointOfInterestDbase.Instance.RemovePointOfInterest(poi);

            RebuildPoIList();
            RebuildInterfaceState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // handlers
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on file activations, import the poi database via the exchange helper and update the interface.
        /// </summary>
        private async void Window_FileActivation(object sender, FileActivationEventArgs args)
        {
            bool? isSuccess = await ExchangePOIUIHelper.ImportFile(Content.XamlRoot,
                                                                   PointOfInterestDbase.Instance, args.Path) ?? false;
            if (isSuccess == true)
            {
                RebuildPoIList();
                RebuildInterfaceState();
            }
            args.IsReportSuccess = null;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on navigating to/from this page, set up and tear down our internal and ui state based on the configuration
        /// we are editing.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            ChangeCoordFormat(Settings.LastPOICoordFmtSelection);
            EditPoI.ClearErrors();
            EditPoI.Reset();

            RebuildPoIList();
            RebuildInterfaceState();

            base.OnNavigatedTo(args);

            if (Settings.MapSettings.IsAutoOpen)
                Utilities.DispatchAfterDelay(DispatcherQueue, 1.0, false, (s, e) => CoreOpenMap(false));

            (Application.Current as JAFDTC.App).Window.POIDbFileActivation += Window_FileActivation;
        }

        /// <summary>
        /// on navigating from this page, close any open map window.
        /// </summary>
        protected override void OnNavigatedFrom(NavigationEventArgs args)
        {
            (Application.Current as JAFDTC.App).Window.POIDbFileActivation -= Window_FileActivation;

            MapWindow?.Close();

            base.OnNavigatedFrom(args);
        }
    }
}
