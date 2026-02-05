// ********************************************************************************************************************
//
// MultipassComboControl.xaml : ui c# for multiple-selection combo controls
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

using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using Windows.UI.Text;

namespace JAFDTC.UI.Controls
{
    /// <summary>
    /// bindable object to provide ui content for the combo box that underlies a MultipassComboControl. this
    /// content includes a title and (for selected items) a checkmark.
    /// </summary>
    internal sealed partial class MultipassComboItem : BindableObject
    {
        // ---- properties that post change notifications

        private Visibility _checkVisibility;
        public Visibility CheckVisibility
        {
            get => _checkVisibility;
            set => SetProperty(ref _checkVisibility, value);
        }

        private Thickness _textSpacing;
        public Thickness TextSpacing
        {
            get => _textSpacing;
            set => SetProperty(ref _textSpacing, value);
        }

        private string _text;
        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        private FontStyle _textStyle;
        public FontStyle TextStyle
        {
            get => _textStyle;
            set => SetProperty(ref _textStyle, value);
        }

        // ---- properties that do not post change notifications

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    CheckVisibility = (_isSelected) ? Visibility.Visible : Visibility.Collapsed;
                    TextSpacing = (_isSelected) ? new(12, 0, 0, 0) : new(28, 0, 0, 0);
                }
            }
        }

        public bool IsSelectAll { get; set; }

        public bool IsSelectNone { get; set; }

        public MultipassComboItem(string text)
            => (CheckVisibility, Text, TextSpacing, TextStyle) 
                   = (Visibility.Collapsed, text, new(28, 0, 0, 0), FontStyle.Normal);
    }

    // ================================================================================================================

    /// <summary>
    /// multi-pass combo control is a user control that contains a single combo box that displays a list of string
    /// items from which multiple items may be selected simultaneously (this is a little like the old school
    /// checkmarks on mac menus). automatically includes "all" and "none" elements in the combobox.
    /// </summary>
    public sealed partial class MultipassComboControl : UserControl
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // constants
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly List<string> _ordinalNumbers =
            ["zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten"];

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public IList<string> Items
        {
            get => _items;
            set => SetItems(value);
        }

        public IList<string> SelectedItems
        {
            get => GetSelectedItems();
            set => SetSelectedItems(value);
        }

        public string ItemDescription { get; set; }

        public string SelectAllText { get; set; }

        public string SelectNoneText { get; set; }

        // ---- private properties

        private ObservableCollection<MultipassComboItem> ItemsUI { get; } = [ ];

        private int CallerItemIndex { get; set; }

        // ---- private, readonly properties

        private readonly ObservableCollection<string> _items = [ ];

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MultipassComboControl()
        {
            _items.CollectionChanged += Items_CollectionChanged;
            IsEnabledChanged += MultipassComboControl_IsEnabledChanged;

            ItemDescription = "item";

            InitializeComponent();

            UpdateItemForeground();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// select all items currently defined in the combo.
        /// </summary>
        public void SelectAllItems()
        {
            for (int i = CallerItemIndex; i < ItemsUI.Count; i++)
                ItemsUI[i].IsSelected = true;
            RebuildPlaceholder(uiComboBox);
        }

        /// <summary>
        /// deselect all items currently defined in the combo.
        /// </summary>
        public void SelectNoItems()
        {
            for (int i = CallerItemIndex; i < ItemsUI.Count; i++)
                ItemsUI[i].IsSelected = false;
            RebuildPlaceholder(uiComboBox);
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void MultipassComboControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateItemForeground();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // utility
        //
        // ------------------------------------------------------------------------------------------------------------

        private void UpdateItemForeground()
        {
            string resource = (IsEnabled) ? "TextFillColorPrimary" : "TextFillColorDisabled";
            if (Application.Current.Resources.TryGetValue(resource, out object value))
                uiComboBox.PlaceholderForeground = (Brush)new SolidColorBrush((Windows.UI.Color)value);
        }

        private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            ClearItemsUI();
            foreach (string item in _items)
                ItemsUI.Add(new(item));
            RebuildPlaceholder(uiComboBox);
        }

        private void ClearItemsUI()
        {
            UpdateItemForeground();

            ItemsUI.Clear();
            CallerItemIndex = 0;
            if (!string.IsNullOrEmpty(SelectAllText))
            {
                ItemsUI.Add(new($"{SelectAllText}"));
                ItemsUI[CallerItemIndex].TextStyle = FontStyle.Italic;
                ItemsUI[CallerItemIndex].IsSelectAll = true;
                CallerItemIndex++;
            }
            if (!string.IsNullOrEmpty(SelectNoneText))
            {
                ItemsUI.Add(new($"{SelectNoneText}"));
                ItemsUI[CallerItemIndex].TextStyle = FontStyle.Italic;
                ItemsUI[CallerItemIndex].IsSelectNone = true;
                CallerItemIndex++;
            }
        }

        private void SetItems(IList<string> items)
        {
            ClearItemsUI();

            _items.CollectionChanged -= Items_CollectionChanged;
            _items.Clear();
            foreach (string item in items)
            {
                _items.Add(item);
                ItemsUI.Add(new(item));
            }
            _items.CollectionChanged += Items_CollectionChanged;

            RebuildPlaceholder(uiComboBox);
        }

        private List<string> GetSelectedItems()
        {
            List<string> items = [ ];
            foreach (MultipassComboItem uiItem in ItemsUI)
                if (uiItem.IsSelected)
                    items.Add(uiItem.Text);
            return items;
        }

        private void SetSelectedItems(IList<string> items)
        {
            foreach (MultipassComboItem uiItem in ItemsUI)
                uiItem.IsSelected = items.Contains(uiItem.Text);
            RebuildPlaceholder(uiComboBox);
        }

        /// <summary>
        /// rebuild the placeholder text that indicates the state of the selection in the combo. selected item is
        /// forced to -1 to show placeholder.
        /// </summary>
        private void RebuildPlaceholder(ComboBox combo)
        {
            string textSelected = null;
            int nSelected = 0;
            for (int i = CallerItemIndex; i < ItemsUI.Count; i++)
                if (ItemsUI[i].IsSelected)
                {
                    textSelected = ItemsUI[i].Text;
                    nSelected++;
                }

            if (nSelected == (ItemsUI.Count - CallerItemIndex))
                combo.PlaceholderText = $"Any {ItemDescription}";
            else if (nSelected == 0)
                combo.PlaceholderText = $"No {ItemDescription}s";
            else if (nSelected == 1)
                combo.PlaceholderText = $"{textSelected}";
            else if (nSelected <= 10)
                combo.PlaceholderText = $"Any of {_ordinalNumbers[nSelected]} {ItemDescription}s";
            else
                combo.PlaceholderText = $"Any of {nSelected} {ItemDescription}s";

            combo.SelectedIndex = -1;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on changes to the selection combo, handle the select all, select none, or individual item selection as
        /// is appropriate.
        /// </summary>
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            ComboBox combo = sender as ComboBox;
            MultipassComboItem item = combo.SelectedItem as MultipassComboItem;
            if ((item != null) && item.IsSelectAll)             // select all
                SelectAllItems();
            else if ((item != null) && item.IsSelectNone)       // select none
                SelectNoItems();
            else if (item != null)
                ItemsUI[combo.SelectedIndex].IsSelected = !ItemsUI[combo.SelectedIndex].IsSelected;
            RebuildPlaceholder(combo);
        }
    }
}
