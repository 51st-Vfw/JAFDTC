// ********************************************************************************************************************
//
// GetPoIFilterDialog.xaml.cs -- ui c# for dialog to grab a poi filter
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

using JAFDTC.Models.CoreApp;
using JAFDTC.Models.DCS;
using JAFDTC.Models.POI;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// ContentDialog to allow the user to specify the filter criteria for points of interest. the dialog has two
    /// modes: a mode to filter pois and a mode to choose pois.
    /// </summary>
    public sealed partial class GetPoIFilterDialog : ContentDialog
    {
        public enum Mode
        {
            FILTER,
            CHOOSE
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public POIFilterSpec Filter => new(((_mode == Mode.FILTER) && (uiComboTheater.SelectedIndex == 0))
                                                ? null : uiComboTheater.SelectedItem.ToString(),
                                           (uiComboCampaign.SelectedIndex == 0)
                                                ? null : uiComboCampaign.SelectedItem.ToString(),
                                           PointOfInterest.SanitizedTags(uiTextBoxTags.Text),
                                           ((((bool)uiCkbxDCSPoI.IsChecked) ? PointOfInterestTypeMask.SYSTEM
                                                                            : PointOfInterestTypeMask.NONE) |
                                            (((bool)uiCkbxUserPoI.IsChecked) ? PointOfInterestTypeMask.USER
                                                                             : PointOfInterestTypeMask.NONE) |
                                            (((bool)uiCkbxCampaignPoI.IsChecked) ? PointOfInterestTypeMask.CAMPAIGN
                                                                                 : PointOfInterestTypeMask.NONE)));

        // ---- private properties

        private readonly Mode _mode;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public GetPoIFilterDialog(POIFilterSpec filter, Mode mode = Mode.FILTER, List<string> allowedTheaters = null)
        {
            _mode = mode;

            InitializeComponent();

            if (mode == Mode.FILTER)
            {
                uiComboTheater.Items.Add("Any Theater");
                foreach (string name in PointOfInterestDbase.KnownTheaters)
                    uiComboTheater.Items.Add(name);
                if (string.IsNullOrEmpty(filter.Theater))
                    uiComboTheater.SelectedIndex = 0;
                else
                    uiComboTheater.SelectedItem = filter.Theater;
            }
            else
            {
                allowedTheaters ??= [ ];
                foreach (string name in allowedTheaters)
                    uiComboTheater.Items.Add(name);
                uiComboTheater.SelectedIndex = 0;
            }

            if (mode == Mode.FILTER)
                uiComboCampaign.Items.Add("Any Campaign");
            else
                uiComboCampaign.Items.Add("None (User Point of Interest)");
            foreach (string name in PointOfInterestDbase.Instance.KnownCampaigns)
                uiComboCampaign.Items.Add(name);
            if (string.IsNullOrEmpty(filter.Campaign) ||
                !PointOfInterestDbase.Instance.KnownCampaigns.Contains(filter.Campaign))
                uiComboCampaign.SelectedIndex = 0;
            else
                uiComboCampaign.SelectedItem = filter.Campaign;

            uiTextBoxTags.Text = filter.Tags;

            if (mode == Mode.FILTER)
            {
                uiCkbxDCSPoI.IsChecked = ((filter.IncludeTypes & PointOfInterestTypeMask.SYSTEM) != 0);
                uiCkbxUserPoI.IsChecked = ((filter.IncludeTypes & PointOfInterestTypeMask.USER) != 0);
                uiCkbxCampaignPoI.IsChecked = ((filter.IncludeTypes & PointOfInterestTypeMask.CAMPAIGN) != 0);
            }
            else
            {
                uiCkbxDCSPoI.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                uiCkbxUserPoI.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                uiCkbxCampaignPoI.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            }
        }
    }
}
