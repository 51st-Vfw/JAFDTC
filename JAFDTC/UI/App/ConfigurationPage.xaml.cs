// ********************************************************************************************************************
//
// ConfigurationPage.xaml.cs -- ui c# for configuration page that enables editing of some configuration
//
// Copyright(C) 2023 ilominar/raven
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
using JAFDTC.Utilities;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.UI;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// holds information on the editor page for a section of an airframe configuration. ConfigurationPage
    /// uses this data to dynamically build the content of the top configuration editor page.
    /// </summary>
    public sealed class ConfigEditorPageInfo : BindableObject
    {
        public string Tag { get; }

        public string Label { get; }

        public string ShortName { get; }
        
        public string Glyph { get; }
        
        public Type EditorPageType { get; }

        public Type EditorHelperType { get; }

        // this property is bound by the ConfigurationPage ui to provide the foreground color for the icon in the nav
        // list used to select the configuration.

        private Brush _editorPageIconFg;
        public Brush EditorPageIconFg
        {
            get => _editorPageIconFg;
            set => SetProperty(ref _editorPageIconFg, value);
        }

        // this property is bound by the ConfigurationPage ui to provide the foreground color for the badge in the nav
        // list used to select the configuration.

        private Brush _editorPageBadgeFg;
        public Brush EditorPageBadgeFg
        {
            get => _editorPageBadgeFg;
            set => SetProperty(ref _editorPageBadgeFg, value);
        }

        public ConfigEditorPageInfo(string tag, string label, string name, string glyph, Type pageType, Type helpType = null)
            => (Tag, Label, ShortName, Glyph,
                EditorPageType, EditorHelperType,
                EditorPageIconFg, EditorPageBadgeFg) = (tag, label, name, glyph, pageType, helpType, null, null);
    }

    /// <summary>
    /// class encapsulating arguments/parameters to pass in to a system editor page.
    /// </summary>
    internal sealed class ConfigEditorPageNavArgs
    {
        public IConfiguration Config { get; }

        public Dictionary<string, IConfiguration> UIDtoConfigMap { get; }

        public Type EditorHelperType { get; }

        public AppBarButton BackButton { get; }

        public ConfigEditorPageNavArgs(IConfiguration config, Type type, Dictionary<string, IConfiguration> map,
                                       AppBarButton backButton)
            => (Config, EditorHelperType, UIDtoConfigMap, BackButton) = (config, type, map, backButton);
    }

    /// <summary>
    /// top level airframe-independed naviagation page to edit sections of the configuration. the various
    /// editors are dynamically defined through ConfigurationEditorPage instances.
    /// </summary>
    public sealed partial class ConfigurationPage : Page
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private JAFDTC.App CurApp { get; set; }

        private ObservableCollection<ConfigEditorPageInfo> EditorPages { get; set; }
        
        private IConfiguration Config { get; set; }

        private Dictionary<string, IConfiguration> UIDtoConfigMap { get; set; }

        private IConfigurationEditor ConfigEditor { get; set; }

        private bool IsRefreshingNavList { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public ConfigurationPage()
        {
            CurApp = Application.Current as JAFDTC.App;
            CurApp.PropertyChanged += AppPropertyChangedHandler;

            InitializeComponent();

            IsRefreshingNavList = false;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        // TODO: document
        private void RebuildIconForeground(ConfigEditorPageInfo info)
        {
            info.EditorPageIconFg = (ConfigEditor.IsSystemDefault(Config, info.Tag))
                                    ? (SolidColorBrush)Resources["EditorListIconNormalBrush"]
                                    : (SolidColorBrush)Resources["EditorListIconHighlightBrush"];
            info.EditorPageBadgeFg = (ConfigEditor.IsSystemLinked(Config, info.Tag))
                                    ? new SolidColorBrush(Color.FromArgb(0xFF, 0xB8, 0x86, 0x0B))       // DarkGoldenrod
                                    : new SolidColorBrush(Color.FromArgb(0x00, 0x00, 0x00, 0x00));      // Transparent
        }

        // TODO: document
        private void RebuildInterfaceState()
        {
            foreach (ConfigEditorPageInfo info in EditorPages)
            {
                RebuildIconForeground(info);
            }
            uiNavBtnToJet.IsEnabled = CurApp.IsDCSAvailable && (CurApp.DCSActiveAirframe == Config.Airframe);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- buttons -----------------------------------------------------------------------------------------------

        // back button click: navigate back to the configuration list.
        //
        private void HdrBtnBack_Click(object sender, RoutedEventArgs args)
        {
            Frame.GoBack();
        }
        
        // to jet button click: upload the current configuration to the jet.
        //
        private void NavBtnToJet_Click(object sender, RoutedEventArgs args)
        {
            CurApp.UploadConfigurationToJet(Config);
        }

        // copy click: serialize the selected system and copy it to the clipboard.
        //
        private void CmdCopy_Click(object sender, RoutedEventArgs args)
        {
            if (uiNavListEditors.SelectedItem is ConfigEditorPageInfo info)
            {
                General.DataToClipboard(info.Tag, Config.Serialize(info.Tag));
            }
        }

        // TODO: implement
        private async void CmdPaste_Click(object sender, RoutedEventArgs args)
        {
            if (uiNavListEditors.SelectedItem is ConfigEditorPageInfo info)
            {
                ClipboardData cboard = await General.ClipboardDataAsync();
                if (Config.CanAcceptPasteForSystem(cboard?.SystemTag, info.Tag) &&
                    Config.Deserialize(cboard?.SystemTag, cboard?.Data))
                {
                    Config.Save(info);
                }
            }
        }

        // reset click: reset the avionics component to their default values if the user confirms. updates will be
        // persisted to storage.
        //
        private async void CmdReset_Click(object sender, RoutedEventArgs args)
        {
            if (uiNavListEditors.SelectedItem is ConfigEditorPageInfo info)
            {
                ContentDialogResult result = await Utilities.Message2BDialog(
                    Content.XamlRoot,
                    "Reset System Configruation?",
                    "Are you sure you want to reset this to avionics defaults? This action cannot be undone.",
                    "Reset"
                );
                if (result == ContentDialogResult.Primary)
                {
                    ISystem system = ConfigEditor.SystemForConfig(Config, info.Tag);
                    if (system != null)
                    {
                        system.Reset();
                        Config.Save(info);
                    }
                }
            }
        }

        // ---- editor list -------------------------------------------------------------------------------------------

        // nav left panel selection change: navigate to the corresponding editor page.
        //
        // NavigationEventArgs.Parameter will be the configuaraiont object conforming to IConfiguration.
        //
        private void NavListEditors_SelectionChanged(object sender, RoutedEventArgs args)
        {
            ConfigEditorPageInfo info = (ConfigEditorPageInfo)uiNavListEditors.SelectedItem;
            if (!IsRefreshingNavList && (info != null))
            {
                ConfigEditorPageNavArgs navArgs = new(Config, info.EditorHelperType, UIDtoConfigMap, uiHdrBtnBack);
                ((Frame)uiNavSplitView.Content).Navigate(info.EditorPageType, navArgs);
                Config.LastSystemEdited = uiNavListEditors.SelectedIndex;
            }
        }

        // TODO: document
        private async void NavListEditors_RightTapped(object sender, RightTappedRoutedEventArgs args)
        {
            ListView listView = (ListView)sender;
            ConfigEditorPageInfo info = (ConfigEditorPageInfo)((FrameworkElement)args.OriginalSource).DataContext;
            if (uiNavListEditors.SelectedItem != info)
            {
                listView.SelectedItem = info;
                RebuildInterfaceState();
            }

            uiNavListEditorsCtxMenuFlyout.Items[0].IsEnabled = false;                   // copy
            uiNavListEditorsCtxMenuFlyout.Items[1].IsEnabled = false;                   // paste
            uiNavListEditorsCtxMenuFlyout.Items[3].IsEnabled = false;                   // reset

            // NOTE: make sure the child editor has not done further navigation (if it has, it should have disabled
            // NOTE: the header back button). this allows us to avoid rewinding the child's nav stack on reset/paste
            // NOTE: and avoids some confusion with what copy/paste means.
            //
            if (uiHdrBtnBack.IsEnabled && (info == null))
            {
                ClipboardData cboard = await General.ClipboardDataAsync();
                bool isPasteValid = (cboard != null) && Config.CanAcceptPasteForSystem(cboard.SystemTag);

                uiNavListEditorsCtxMenuFlyout.Items[1].IsEnabled = isPasteValid;        // paste
            }
            else if (uiHdrBtnBack.IsEnabled && (info != null))
            {
                ClipboardData cboard = await General.ClipboardDataAsync();
                bool isPasteValid = (cboard != null) && Config.CanAcceptPasteForSystem(cboard.SystemTag, info.Tag);
                bool isDefault = ConfigEditor.IsSystemDefault(Config, info.Tag);

                uiNavListEditorsCtxMenuFlyout.Items[0].IsEnabled = !isDefault;          // copy
                uiNavListEditorsCtxMenuFlyout.Items[1].IsEnabled = isPasteValid;        // paste
                uiNavListEditorsCtxMenuFlyout.Items[3].IsEnabled = !isDefault;          // reset
            }
            uiNavListEditorsCtxMenuFlyout.ShowAt(listView, args.GetPosition(listView));
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        // when configuration is saved, rebuild the interface state to catch up with any changes.
        //
        private void ConfigurationSavedHandler(object sender, ConfigurationSavedEventArgs args)
        {
            ConfigEditorPageInfo modifiedInfo = null;
            if (args.InvokedBy.GetType() == typeof(ConfigEditorPageInfo))
            {
                modifiedInfo = (ConfigEditorPageInfo)args.InvokedBy;
            }
            else
            {
                foreach (ConfigEditorPageInfo info in EditorPages)
                {
                    if (info.EditorPageType == args.InvokedBy.GetType())
                    {
                        modifiedInfo = info;
                        break;
                    }
                }
            }
            if (modifiedInfo != null)
            {
                // HACK: this is a total hack as bindings don't seem to work the way they are supposed to; e.g.,
                // HACK: as in
                // HACK: https://stackoverflow.com/questions/59473945/update-display-of-one-item-in-a-listviews-observablecollection/59506197#59506197
                // HACK: There appears to be some kinda race going on though, so put the reinsert on the dispatch
                // HACK: queue with low priority.
                //
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    RebuildIconForeground(modifiedInfo);
                    IsRefreshingNavList = true;
                    int index = uiNavListEditors.SelectedIndex;
                    EditorPages[EditorPages.IndexOf(modifiedInfo)] = modifiedInfo;
                    uiNavListEditors.SelectedIndex = index;
                    IsRefreshingNavList = false;
                });
            }
        }

        // TODO: document
        //
        private void AppPropertyChangedHandler(object sender, object args)
        {
            RebuildInterfaceState();
        }

        // on navigating to/from this page, set up and tear down our internal and ui state based on the configuration
        // we are editing.
        //
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            ConfigEditorPageNavArgs navArgs = (ConfigEditorPageNavArgs)args.Parameter;
            Config = navArgs.Config;
            UIDtoConfigMap = navArgs.UIDtoConfigMap;
            ConfigEditor = ConfigurationEditor.Factory(Config.Airframe);

            Config.ConfigurationSaved += ConfigurationSavedHandler;

            EditorPages = ConfigEditor.ConfigEditorPageInfo();
            uiNavListEditors.SelectedIndex = Config.LastSystemEdited;
            uiHdrTxtConfigName.Text = Config.Name;
            uiHdrTxtConfigIsFav.Visibility = (Config.IsFavorite) ? Visibility.Visible : Visibility.Collapsed;
            uiNavTxtAirframeName.Text = Globals.AirframeNames[Config.Airframe];

            // create a new frame to hold the editors and replace the split view content with the frame. setting
            // SelectedIndex above will trigger EditorList_SelectionChanged() and cause navigation to happen.
            //
            uiNavSplitView.Content = new Frame();

            RebuildInterfaceState();

            base.OnNavigatedTo(args);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs args)
        {
            Config.ConfigurationSaved -= ConfigurationSavedHandler;

            base.OnNavigatedFrom(args);
        }
    }
}
