// ********************************************************************************************************************
//
// GetNameDialog.xaml.cs -- ui c# for dialog to grab a name
//
// Copyright(C) 2023-2024 ilominar/raven
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

using Microsoft.UI.Xaml.Controls;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// a dialog with a single text field to accept a name. callers can cusomize with a prompt, title, and
    /// placeholder strings.
    /// </summary>
    public sealed partial class GetNameDialog : ContentDialog
    {
        public string Value { get => uiNameText.Text; }

        public GetNameDialog(string name = null, string prompt = null, string placeholder = null)
        {
            InitializeComponent();

            if (name != null)
                uiNameText.Text = name;
            if (prompt != null)
                uiNameText.Header = prompt;
            if (placeholder != null)
                uiNameText.PlaceholderText = placeholder;
            IsPrimaryButtonEnabled = ((Value != null) && (Value.Length > 0));
        }

        private void NameText_TextChanged(object sender, TextChangedEventArgs _)
        {
            IsPrimaryButtonEnabled = ((Value != null) && (Value.Length > 0));
        }
    }
}
