// ********************************************************************************************************************
//
// PilotComboControl.xaml : ui c# for common pilot list combo controls
//
// Copyright(C) 2026 ilominar/raven
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

using JAFDTC.Models.Pilots;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;

namespace JAFDTC.UI.Controls
{
    /// <summary>
    /// pilot combo control is a user control that contains a single combo box that displays a list of pilots
    /// from the pilot database. the sentinel pilot, UnassignedPilot, may be used to add an "unassigned" pilot
    /// to the combo box.
    /// </summary>
    public sealed partial class PilotComboControl : UserControl
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        public event EventHandler<SelectionChangedEventArgs> SelectionChanged;

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public static readonly DependencyProperty SelectedPilotProperty = DependencyProperty.Register(
            nameof(SelectedPilot),
            typeof(Pilot),
            typeof(PilotComboControl),
            new PropertyMetadata(default));

        private string _ownshipCallsign;
        public string OwnshipCallsign
        {
            get => _ownshipCallsign;
            set
            {
                _ownshipCallsign = value;
                uiComboBox.ItemsSource = BuildCallsignComboItems();
            }
        }

        private IReadOnlyList<Pilot> _pilots;
        public IReadOnlyList<Pilot> Pilots
        {
            get => _pilots;
            set
            {
                _pilots = value;
                uiComboBox.ItemsSource = BuildCallsignComboItems();
            }
        }

        public Pilot SelectedPilot
        {
            get => (uiComboBox.SelectedIndex != -1) ? Pilots[uiComboBox.SelectedIndex] : null;
            set
            {
                int index = -1;
                if ((value != null) && (Pilots != null))
                    for (int i = 0; i < Pilots.Count; i++)
                        if (value.UniqueID == Pilots[i].UniqueID)
                        {
                            index = i;
                            break;
                        }
                uiComboBox.SelectedIndex = index;
                SetValue(SelectedPilotProperty, (index != -1) ? Pilots[index] : null);
            }
        }

        public new bool IsEnabled
        {
            get => uiComboBox.IsEnabled;
            set => uiComboBox.IsEnabled = value;
        }

        public static Pilot UnassignedPilot => new()
        {
            Airframe = Models.Core.AirframeTypes.UNKNOWN,
            Name = "Unassigned"
        };

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public PilotComboControl()
        {
            InitializeComponent();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns a StackPanel for use as a pilot item in the callsign combo boxes. items are a pilot name with the
        /// current callsign from the settings getting an icon prefix. the stack panel tag is set to the uid of the
        /// pilot. a pilot matching UnassignedPilot uses a tag of "0".
        /// </summary>
        private StackPanel BuildPilotItemStackPanel(Pilot pilot)
        {
            bool isOwnship = (pilot.Name == OwnshipCallsign);
            bool isAssigned = (pilot.Name != UnassignedPilot.Name);

            StackPanel itemPanel = new()
            {
                Orientation = Orientation.Horizontal,
                Tag = (isAssigned) ? pilot.UniqueID : "0",
            };
            FontIcon itemIcon = new()
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily("Segoe Fluent Icons"),
                FontSize = 16.0,
                Glyph = (isOwnship) ? Glyphs.Pilot : Glyphs.None,
                Width = 18,
                Margin = (isOwnship) ? new Thickness(0, 0, 8, 0) : new Thickness(0, 0, 4, 0)
            };
            TextBlock itemText = new()
            {
                Text = pilot.Name,
                FontStyle = (isAssigned) ? Windows.UI.Text.FontStyle.Normal : Windows.UI.Text.FontStyle.Italic,
                FontWeight = (isOwnship) ? FontWeights.Bold : FontWeights.Normal,
            };
            itemPanel.Children.Add(itemIcon);
            itemPanel.Children.Add(itemText);
            return itemPanel;
        }

        /// <summary>
        /// returns a list of StackPanel instances to serve as the menu items for the callsign format combo controls.
        /// the tags are set to the uid of the pilot from the pilot database. first element is blank with a "0" tag.
        /// </summary>
        private List<StackPanel> BuildCallsignComboItems()
        {
            List<StackPanel> pilotItems = [ ];
            if (Pilots != null)
                for (int i = 0; i < Pilots.Count; i++)
                    pilotItems.Add(BuildPilotItemStackPanel(Pilots[i]));
            return pilotItems;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on changes to the selection combo, pass them along as a SelectionChanged event from us.
        /// </summary>
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            ComboBox combo = sender as ComboBox;
            SelectedPilot = (combo.SelectedIndex != -1) ? Pilots[combo.SelectedIndex] : null;
            SelectionChanged?.Invoke(this, args);
        }
    }
}
