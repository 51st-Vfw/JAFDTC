// ********************************************************************************************************************
//
// PoIChooseDialog.xaml.cs -- ui c# for dialog to grab a poi filter
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
    /// ContentDialog to allow the user to specify the filter criteria for points of interest. the dialog has two
    /// modes: a mode to filter pois and a mode to choose pois.
    /// </summary>
    public sealed partial class PoIChooseDialog : ContentDialog
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public POIFilterSpec Filter => new(null,
                                           (uiComboCampaign.SelectedIndex == 0)
                                                ? null
                                                : [ uiComboCampaign.SelectedItem as string ],
                                           PointOfInterest.SanitizedTags(uiTextBoxTags.Text),
                                           (uiComboCampaign.SelectedIndex != 0)
                                                ? PointOfInterestTypeMask.CAMPAIGN
                                                : PointOfInterestTypeMask.NONE);

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public PoIChooseDialog(POIFilterSpec filter, List<string> allowedTheaters = null)
        {
            InitializeComponent();

            allowedTheaters ??= [ ];
            foreach (string name in allowedTheaters)
                uiComboTheater.Items.Add(name);
            uiComboTheater.SelectedIndex = 0;

            uiComboCampaign.Items.Add("None (create user points of interest)");
            foreach (string name in PointOfInterestDbase.Instance.KnownCampaigns)
                uiComboCampaign.Items.Add(name);
            if ((filter.Campaigns == null) || (filter.Campaigns.Count == 0))
                uiComboCampaign.SelectedIndex = 0;
            else
                uiComboCampaign.SelectedItem = filter.Campaigns[0];

            uiTextBoxTags.Text = filter.Tags;
        }
    }
}
