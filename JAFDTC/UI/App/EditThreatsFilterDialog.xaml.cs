// ********************************************************************************************************************
//
// EditThreatsFilterDialog.xaml.cs -- ui c# for dialog to grab a threat editor filter
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

using JAFDTC.Models.Core;
using JAFDTC.Models.CoreApp;
using JAFDTC.Models.Units;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed partial class EditThreatsFilterDialog : ContentDialog
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public ThreatFilterSpec Filter => new(uiComboCoalition.SelectedIndex switch
                                              {
                                                  0 => CoalitionType.BLUE,
                                                  1 => CoalitionType.RED,
                                                  _ => CoalitionType.UNKNOWN,
                                              },
                                              uiComboCategory.SelectedIndex switch
                                              {
                                                  0 => UnitCategoryType.GROUND,
                                                  1 => UnitCategoryType.NAVAL,
                                                  _ => UnitCategoryType.UNKNOWN,
                                              },
                                              (bool)uiCkbxThreatsDCS.IsChecked,
                                              (bool)uiCkbxThreatsUsr.IsChecked);

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public EditThreatsFilterDialog(ThreatFilterSpec filter)
        {
            InitializeComponent();

            uiComboCoalition.SelectedIndex = filter.Coalition switch
            {
                CoalitionType.BLUE => 0,
                CoalitionType.RED => 1,
                _ => 2
            };
            uiComboCategory.SelectedIndex = filter.Category switch
            {
                UnitCategoryType.GROUND  => 0,
                UnitCategoryType.NAVAL => 1,
                _ => 2
            };
            
            uiCkbxThreatsDCS.IsChecked = filter.ShowThreatsDCS;
            uiCkbxThreatsUsr.IsChecked = filter.ShowThreatsUser;

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

        private void CheckBox_Click(object sender, RoutedEventArgs args)
        {
            IsSecondaryButtonEnabled = !Filter.IsDefault;
        }
    }
}
