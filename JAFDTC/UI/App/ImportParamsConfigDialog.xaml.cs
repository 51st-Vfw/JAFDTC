// ********************************************************************************************************************
//
// ImportParamsConfigDialog.xaml.cs -- ui c# for dialog to grab base import parameters
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

using JAFDTC.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// TODO: document.
    /// </summary>
    public sealed partial class ImportParamsConfigDialog : ContentDialog
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public string ConfigName { get => uiNameText.Text; }

        public string ConfigRole { get => uiRoleText.Text; }

        // ---- private properties

        private readonly ConfigurationList _configList = null;
        private readonly IConfiguration _config = null;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public ImportParamsConfigDialog(ConfigurationList configList, IConfiguration config, string name = null)
        {
            InitializeComponent();

            _configList = configList;
            _config = config;

            uiNameText.Text = name;
            string helpText = config.RoleHelpText();
            if (helpText == null)
            {
                uiRoleText.Visibility = Visibility.Collapsed;
                uiRoleHelpText.Visibility = Visibility.Collapsed;
            }
            else if (helpText.Length > 0)
            {
                uiRoleHelpText.Text = helpText;
            }

            uiNameText.Header = $"Enter a name for the new {Globals.AirframeNames[config.Airframe]} configuration:";

            IsPrimaryButtonEnabled = (!string.IsNullOrEmpty(ConfigName) && (_configList.IsNameUnique(ConfigName)));
        }

        private void NameText_TextChanged(object sender, TextChangedEventArgs _)
        {
            IsPrimaryButtonEnabled = (!string.IsNullOrEmpty(ConfigName) && (_configList.IsNameUnique(ConfigName)));
        }

        private void RoleText_TextChanged(object sender, TextChangedEventArgs _)
        {
            IsPrimaryButtonEnabled = _config.ValidateRole(ConfigRole);
        }
    }
}
