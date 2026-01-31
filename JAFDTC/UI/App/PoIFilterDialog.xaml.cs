// ********************************************************************************************************************
//
// PoIFilterDialog.xaml.cs -- ui c# for dialog to grab a poi filter
//
// Copyright(C) 2024-2026 ilominar/raven
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
    /// ContentDialog to allow the user to specify the filter criteria for points of interest. this builds a
    /// POIFilterSpec that specifies a theater (null => any theater), campaign(s) (null => any campaign), and poi
    /// types (dcs, user, campaign) to display.
    /// </summary>
    public sealed partial class PoIFilterDialog : ContentDialog
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public POIFilterSpec Filter => new((uiComboTheater.SelectedIndex == 0)
                                                ? null
                                                : uiComboTheater.SelectedItem.ToString(),
                                           (uiComboCampaign.SelectedItems.Count == uiComboCampaign.Items.Count)
                                                ? null
                                                : [.. uiComboCampaign.SelectedItems ],
                                           PointOfInterest.SanitizedTags(uiTextBoxTags.Text),
                                           ((((bool)uiCkbxDCSPoI.IsChecked)
                                                ? PointOfInterestTypeMask.SYSTEM
                                                : PointOfInterestTypeMask.NONE) |
                                            (((bool)uiCkbxUserPoI.IsChecked)
                                                ? PointOfInterestTypeMask.USER
                                                : PointOfInterestTypeMask.NONE) |
                                            ((uiComboCampaign.SelectedItems.Count > 0)
                                                ? PointOfInterestTypeMask.CAMPAIGN
                                                : PointOfInterestTypeMask.NONE)));

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public PoIFilterDialog(POIFilterSpec filter, List<string> allowedTheaters = null)
        {
            InitializeComponent();

            uiComboTheater.Items.Add("Any Theater");
            foreach (string name in PointOfInterestDbase.KnownTheaters)
                uiComboTheater.Items.Add(name);
            if (string.IsNullOrEmpty(filter.Theater))
                uiComboTheater.SelectedIndex = 0;
            else
                uiComboTheater.SelectedItem = filter.Theater;

            bool isCampaignsVisible = filter.IncludeTypes.HasFlag(PointOfInterestTypeMask.CAMPAIGN);

            uiComboCampaign.SelectAllText = "Any campaign";
            uiComboCampaign.SelectNoneText = "No campaigns";
            uiComboCampaign.ItemDescription = "campaign";
            foreach (string name in PointOfInterestDbase.Instance.KnownCampaigns)
                uiComboCampaign.Items.Add(name);
            if (isCampaignsVisible && ((filter.Campaigns == null) || (filter.Campaigns.Count == 0)))
                uiComboCampaign.SelectAllItems();
            else if ((filter.Campaigns != null) && (filter.Campaigns.Count > 0))
                uiComboCampaign.SelectedItems = filter.Campaigns;

            uiTextBoxTags.Text = filter.Tags;

            uiCkbxDCSPoI.IsChecked = ((filter.IncludeTypes & PointOfInterestTypeMask.SYSTEM) != 0);
            uiCkbxUserPoI.IsChecked = ((filter.IncludeTypes & PointOfInterestTypeMask.USER) != 0);
        }
    }
}
