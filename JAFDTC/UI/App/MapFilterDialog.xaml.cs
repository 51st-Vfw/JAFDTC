// ********************************************************************************************************************
//
// MapFilterDialog.xaml.cs -- ui c# for dialog to grab a map element filter
//
// Copyright(C) 2025 ilominar/raven
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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// ContentDialog to allow the user to specify the filter criteria for a map window. this controls which
    /// elements (routes, pois, threats, etc) are visible on the map.
    /// </summary>
    public sealed partial class MapFilterDialog : ContentDialog
    {

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public MapFilterSpec Filter => new((MapFilterSpec.ImportFilter)uiComboUnitImport.SelectedIndex,
                                           (MapFilterSpec.ImportFilter)uiComboRingImport.SelectedIndex,
                                           (uiComboCampaign.SelectedItems.Count == uiComboCampaign.Items.Count)
                                                ? null
                                                : [.. uiComboCampaign.SelectedItems ],
                                           (bool)uiCkbxPoIDCS.IsChecked,
                                           (bool)uiCkbxPoIUsr.IsChecked,
                                           (bool)(uiComboCampaign.SelectedItems.Count > 0),
                                           (bool)uiCkbxNavRoutes.IsChecked);

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MapFilterDialog(MapFilterSpec filter, List<string> campaigns = null)
        {
            InitializeComponent();

            uiComboUnitImport.SelectedIndex = (int)filter.ShowUnits;
            uiComboRingImport.SelectedIndex = (int)filter.ShowThreatRings;

            uiComboCampaign.SelectAllText = "Any campaign";
            uiComboCampaign.SelectNoneText = "No campaigns";
            uiComboCampaign.ItemDescription = "campaign";
            if ((campaigns == null) || (campaigns.Count == 0))
                campaigns = PointOfInterestDbase.Instance.KnownCampaigns;
            foreach (string name in campaigns)
                uiComboCampaign.Items.Add(name);
            if (filter.ShowPOICamp && ((filter.Campaigns == null) || (filter.Campaigns.Count == 0)))
                uiComboCampaign.SelectAllItems();
            else if ((filter.Campaigns != null) && (filter.Campaigns.Count > 0))
                uiComboCampaign.SelectedItems = filter.Campaigns;

            uiCkbxPoIDCS.IsChecked = filter.ShowPOIDCS;
            uiCkbxPoIUsr.IsChecked = filter.ShowPOIUsr;
            uiCkbxPoICamp.IsChecked = filter.ShowPOICamp;
            uiCkbxNavRoutes.IsChecked = filter.ShowNavRoutes;

            if (!uiCkbxPoICamp.IsChecked.GetValueOrDefault(false))
                uiComboCampaign.IsEnabled = false;

            IsSecondaryButtonEnabled = !filter.IsDefault;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        public void Combo_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            IsSecondaryButtonEnabled = !Filter.IsDefault;
        }

        public void CheckBox_Click(object sender, RoutedEventArgs args)
        {
            if (!(bool)uiCkbxPoICamp.IsChecked)
            {
                uiComboCampaign.SelectNoItems();
                uiComboCampaign.IsEnabled = false;
            }
            else
            {
                uiComboCampaign.SelectAllItems();
                uiComboCampaign.IsEnabled = true;
            }
            IsSecondaryButtonEnabled = !Filter.IsDefault;
        }
    }
}
