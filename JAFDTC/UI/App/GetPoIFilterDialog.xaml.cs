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

using JAFDTC.Models.DCS;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// TODO
    /// </summary>
    public sealed partial class GetPoIFilterDialog : ContentDialog
    {
        public enum Mode
        {
            Filter,
            Choose
        }
        private readonly Mode _mode;

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public string Theater => (_mode == Mode.Filter && uiComboTheater.SelectedIndex == 0) ? null : uiComboTheater.SelectedItem.ToString();

        public string Campaign => (uiComboCampaign.SelectedIndex == 0) ? null : uiComboCampaign.SelectedItem.ToString();

        public string Tags => uiTextBoxTags.Text;

        public PointOfInterestTypeMask IncludeTypes
            => ((((bool)uiCkbxDCSPoI.IsChecked) ? PointOfInterestTypeMask.DCS_CORE : PointOfInterestTypeMask.NONE) |
                (((bool)uiCkbxUserPoI.IsChecked) ? PointOfInterestTypeMask.USER : PointOfInterestTypeMask.NONE) |
                (((bool)uiCkbxCampaignPoI.IsChecked) ? PointOfInterestTypeMask.CAMPAIGN : PointOfInterestTypeMask.NONE));

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public GetPoIFilterDialog(string theater = null, string campaign = null, string tags = null,
                                  PointOfInterestTypeMask includeTypes = PointOfInterestTypeMask.ANY,
                                  Mode mode = Mode.Filter, List<string> allowedTheaters = null)
        {
            InitializeComponent();
            _mode = mode;

            if (mode == Mode.Filter)
            {
                uiComboTheater.Items.Add("Any Theater");
                foreach (string name in PointOfInterestDbase.KnownTheaters)
                    uiComboTheater.Items.Add(name);
                if (string.IsNullOrEmpty(theater))
                    uiComboTheater.SelectedIndex = 0;
                else
                    uiComboTheater.SelectedItem = theater;
            }
            else
            {
                foreach (string name in allowedTheaters)
                    uiComboTheater.Items.Add(name);
                uiComboTheater.SelectedIndex = 0;
            }

            if (mode == Mode.Filter)
                uiComboCampaign.Items.Add("Any Campaign");
            else
                uiComboCampaign.Items.Add("None (User Point of Interest)");
            foreach (string name in PointOfInterestDbase.Instance.KnownCampaigns)
                uiComboCampaign.Items.Add(name);
            if (string.IsNullOrEmpty(campaign) || !PointOfInterestDbase.Instance.KnownCampaigns.Contains(campaign))
                uiComboCampaign.SelectedIndex = 0;
            else
                uiComboCampaign.SelectedItem = campaign;

            uiTextBoxTags.Text = tags;

            if (mode == Mode.Choose)
            {
                uiCkbxDCSPoI.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                uiCkbxUserPoI.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                uiCkbxCampaignPoI.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                uiCkbxDCSPoI.IsChecked = ((includeTypes & PointOfInterestTypeMask.DCS_CORE) != 0);
                uiCkbxUserPoI.IsChecked = ((includeTypes & PointOfInterestTypeMask.USER) != 0);
                uiCkbxCampaignPoI.IsChecked = ((includeTypes & PointOfInterestTypeMask.CAMPAIGN) != 0);
            }
        }
    }
}
