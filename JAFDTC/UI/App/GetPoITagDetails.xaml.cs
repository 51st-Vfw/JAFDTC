// ********************************************************************************************************************
//
// GetPoITagDetails.xaml.cs -- ui c# for dialog to grab poi tag details (campaign, theater)
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

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// dialog to request campaign (if requested) and theater (if requested) information for use in a point of
    /// interest tag.
    /// </summary>
    public sealed partial class GetPoITagDetails : ContentDialog
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public string Theater => uiComboTheater.SelectedItem?.ToString();

        public string Campaign => uiComboCampaign.SelectedItem?.ToString();

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public GetPoITagDetails(List<string> campaigns, string selCampaign, List<string> theaters, string selTheater)
        {
            InitializeComponent();

            if ((campaigns == null) || (campaigns.Count == 0))
            {
                uiLabelCampaign.Visibility = Visibility.Collapsed;
                uiComboCampaign.Visibility = Visibility.Collapsed;
            }
            else
            {
                uiLabelCampaign.Visibility = Visibility.Visible;
                uiComboCampaign.Visibility = Visibility.Visible;
                uiComboCampaign.ItemsSource = campaigns;
                int i = campaigns.IndexOf(selCampaign);
                uiComboCampaign.SelectedIndex = Math.Max(0, (selCampaign != null) ? campaigns.IndexOf(selCampaign) : 0);
            }
            if ((theaters == null) || (theaters.Count == 0))
            {
                uiLabelTheater.Visibility = Visibility.Collapsed;
                uiComboTheater.Visibility = Visibility.Collapsed;
            }
            else
            {
                uiLabelTheater.Visibility = Visibility.Visible;
                uiComboTheater.Visibility = Visibility.Visible;
                uiComboTheater.ItemsSource = theaters;
                int i = theaters.IndexOf(selTheater);
                uiComboTheater.SelectedIndex = Math.Max(0, (selTheater != null) ? theaters.IndexOf(selTheater) : 0);
            }
        }
    }
}
