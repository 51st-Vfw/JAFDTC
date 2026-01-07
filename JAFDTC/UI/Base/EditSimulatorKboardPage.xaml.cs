// ********************************************************************************************************************
//
// EditSimulatorKboardPage.cs : ui c# for for general kneeboard builder editor page
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

using JAFDTC.Models;
using JAFDTC.Models.Base;
using JAFDTC.Models.Core;
using JAFDTC.UI.App;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// page to edit kneeboard builder fields. this is a general-purpose class that is instatiated in combination
    /// a IEditSimulatorKboardPageHelper class to provide airframe-specific specialization.
    /// </summary>
    public sealed partial class EditSimulatorKboardPage : SystemEditorPageBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // private classes
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// item object for the template selection combo box.
        /// </summary>
        private sealed class TemplateComboItem(string templateName, string displayName, string prefix)
        {
            public string TemplateName { get; } = templateName;
            public string DisplayName { get; } = displayName;
            public string Prefix { get; } = prefix;

            public override string ToString() => $"{Prefix}{DisplayName}";
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- overrides of base SystemEditorPage properties

        protected override SystemBase SystemConfig => PageHelper.GetSystemConfig(Config);

        protected override string SystemTag => SimKboardSystem.SystemTag;

        protected override string SystemName => "Kneeboards";

        protected override bool IsPageStateDefault => EditKboard.IsDefault;

        // ---- internal properties

        private IEditSimulatorKboardPageHelper PageHelper { get; set; }

        private readonly SimKboardSystem EditKboard;

        private readonly ObservableCollection<SystemGridItem> GridItems;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public EditSimulatorKboardPage()
        {
            EditKboard = new();

            InitializeComponent();
            InitializeBase(EditKboard, null, uiCtlLinkResetBtns, null);

            GridItems = [ ];
            uiGridSystemItems.ItemsSource = GridItems;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// marshall data between the kneeboard configuration and our local kneeboard state.
        /// </summary>
        protected override void CopyConfigToEditState()
        {
            if (EditState != null)
            {
                PageHelper.CopyConfigToEdit(Config, EditKboard);
                CopyAllSettings(SettingLocation.Config, SettingLocation.Edit);
            }
            UpdateUIFromEditState();
        }

        /// <summary>
        /// marshall data between our local kneeboard state and the kneeboard configuration.
        /// </summary>
        protected override void SaveEditStateToConfig()
        {
            if ((EditState != null) && !IsUIRebuilding)
            {
                PageHelper.CopyEditToConfig(EditKboard, Config);
                CopyAllSettings(SettingLocation.Edit, SettingLocation.Config, true);
                Config.Save(this, SystemTag);
            }
            UpdateUIFromEditState();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// update the dtc output path if it is changing from what is stored in our local kneeboard state. if the path
        /// is changing and the old path is valid, ask if the luser wants to delete the old path.
        /// </summary>
        private async void UpdateKboardOutputPath(string newPath)
        {
            string oldPath = EditKboard.OutputPath;
            if (EditKboard.OutputPath != newPath)
            {
                EditKboard.OutputPath = newPath;
                SaveEditStateToConfig();
            }
            if ((oldPath.Length > 0) && (newPath != oldPath) && System.IO.File.Exists(oldPath))
            {
                ContentDialogResult result = await Utilities.Message2BDialog(Content.XamlRoot,
                    "Save Location Changing",
                    $"You are changing the save location, would you like to delete the output previously saved at “{oldPath}”?",
                    "No",
                    "Delete"
                );
                try
                {
                    if (result == ContentDialogResult.None)
                        System.IO.File.Delete(oldPath);
                }
                catch (Exception ex)
                {
                    FileManager.Log($"EditSimulatorKboardPage:UpdateKboardOutputPath exception {ex}");
                }
            }
        }

        /// <summary>
        /// rebuild the template list from the known templates and update the selection to be consistent with the
        /// edit state.
        /// </summary>
        private void RebuildTemplateList()
        {
            List<string> templates = FileManager.ListKBTemplatePackages(Config.Airframe, out int numGeneric);
            List<string> airframeTemplates = templates[numGeneric..];
            templates = templates[..numGeneric];
            templates.Sort();
            airframeTemplates.Sort();

            uiComboTemplate.Items.Clear();
            uiComboTemplate.Items.Add(new TemplateComboItem("", "Default kneeboard template package", ""));
            foreach (string template in templates)
                uiComboTemplate.Items.Add(new TemplateComboItem(template, template, ""));
            string prefix = $"{Globals.AirframeShortNames[Config.Airframe]} – ";
            foreach (string template in airframeTemplates)
                uiComboTemplate.Items.Add(new TemplateComboItem(template, template, prefix));

            templates.AddRange(airframeTemplates);
            int selIndex = 0;
            if (!string.IsNullOrEmpty(EditKboard.Template))
                foreach (string template in templates)
                {
                    selIndex++;
                    if (template == EditKboard.Template)
                        break;
                }
            uiComboTemplate.SelectedIndex = selIndex;
        }

        /// <summary>
        /// rebuild the kneeboard list from the selected template and update the items grid.
        /// </summary>
        private void RebuildKneeboardList(bool isResetSelection = false)
        {
            List<string> kboards = FileManager.ListKBTemplates(Config.Airframe, EditKboard.Template);
            if (kboards.Count == 0)
                kboards = FileManager.ListKBTemplates(AirframeTypes.UNKNOWN, EditKboard.Template);
            if (isResetSelection)
            {
                EditKboard.KneeboardTags.Clear();
                foreach (string kb in kboards)
                    EditKboard.KneeboardTags.Add(kb);
                SaveEditStateToConfig();
            }
            else
            {
                List<string> selected = [.. EditKboard.KneeboardTags ];
                foreach (string kb in selected)
                    if (!kboards.Contains(kb))
                    {
                        EditKboard.KneeboardTags.Clear();
                        SaveEditStateToConfig();
                        break;
                    }
            }
            GridItems.Clear();
            foreach (string kb in kboards)
                GridItems.Add(new(kb, "\xF0E3", kb.Replace("_", " "), EditKboard.KneeboardTags.Contains(kb)));
        }

        /// <summary>
        /// rebuild the enable state of the buttons in the ui based on current configuration setup.
        /// </summary>
        private void RebuildEnableState()
        {
            Utilities.SetEnableState(uiBtnDelTmplt, (uiComboTemplate.SelectedIndex > 0));
            Utilities.SetEnableState(uiBtnSetOutput, (EditKboard.KneeboardTags.Count > 0));
            Utilities.SetEnableState(uiBtnClearOutput, (uiValueOutput.Text.Length > 0));

            Utilities.SetEnableState(uiCkbxEnableRebuild, (EditKboard.KneeboardTags.Count > 0));
            Utilities.SetEnableState(uiCkbxEnableNight, true);
            Utilities.SetEnableState(uiCkbxEnableSVG, true);
        }

        /// <summary>
        /// rebuild the state of controls on the page in response to a change in the configuration.
        /// </summary>
        protected override void UpdateUICustom(bool isEditable)
        {
            RebuildTemplateList();
            RebuildEnableState();
        }

        /// <summary>
        /// reset to defaults. check all of the systems buttons.
        /// </summary>
        protected override void ResetConfigToDefault()
        {
            SystemConfig.Reset();
            CopyConfigToEditState();
            //
            // HACK: can't get the bindings to work for some reason, have to do this the brute force way...
            //
            List<ToggleButton> tbtns = [ ];
            Utilities.FindDescendantControls<ToggleButton>(tbtns, uiGridSystemItems);
            foreach (ToggleButton tbtn in tbtns)
                tbtn.IsChecked = false;
            foreach (SystemGridItem item in uiGridSystemItems.Items.Cast<SystemGridItem>())
                item.IsChecked = false;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui interactions
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- buttons -----------------------------------------------------------------------------------------------

        /// <summary>
        /// add template click: add a dcs dtc template to the known templates available for the airframe. the
        /// template file is copied into the dtc area if necessary. the selected template is not changed.
        /// </summary>
        private async void BtnAddTmplt_Click(object sender, RoutedEventArgs args)
        {
            FileOpenPicker picker = new((Application.Current as JAFDTC.App).Window.AppWindow.Id)
            {
                CommitButtonText = "Add Template Package",
                SuggestedStartLocation = PickerLocationId.Desktop,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add(".zip");

            PickFileResult resultPick = await picker.PickSingleFileAsync();
            if (resultPick != null)
            {
                string name = Path.GetFileNameWithoutExtension(resultPick.Path);
                try
                {
                    if (!FileManager.IsValidKBTemplatePackage(resultPick.Path))
                        throw new Exception($"The file “{Path.GetFileName(resultPick.Path)}” is not suitable for a" +
                                            $" kneeboard template package as it is not a .zip archive that includes" +
                                            $" only .svg files.");

                    ContentDialogResult result = await Utilities.Message2BDialog(Content.XamlRoot,
                        "Import Destination",
                        $"Would you like to import the kneeboard template pacakge “{name}” as a generic template (for" +
                        $" use with any airframe) or a template specific to the {Globals.AirframeNames[Config.Airframe]}?",
                        $"Generic",
                        $"{Globals.AirframeNames[Config.Airframe]}");

                    AirframeTypes type = (result == ContentDialogResult.None) ? Config.Airframe : AirframeTypes.UNKNOWN;

                    result = ContentDialogResult.Primary;
                    if (FileManager.IsUniqueKBTemplatePackage(Config.Airframe, name))
                    {
                        result = await Utilities.Message2BDialog(Content.XamlRoot,
                            "Template Package Exists",
                            $"There is already either a generic or a airframe-specific kneeboard template package" +
                            $" with the name “{name}”. Would you like to replace it?",
                            "Replace"
                        );
                        if (result == ContentDialogResult.Primary)
                        {
                            FileManager.DeleteKBTemplatePackage(Config.Airframe, name);
                            FileManager.DeleteKBTemplatePackage(AirframeTypes.UNKNOWN, name);
                        }
                    }
                    if (result == ContentDialogResult.Primary)
                    {
                        FileManager.ExtractKBTemplatePackage(type, resultPick.Path);
                        EditKboard.Template = name;
                        SaveEditStateToConfig();
                        RebuildKneeboardList();
                    }
                }
                catch (Exception ex)
                {
                    string msg = (ex.Source == "JAFDTC") ? ex.Message
                                                         : $"The kneeboard template package “{name}” is not a valid package.";
                    FileManager.Log($"EditSimulatorKboardPage:BtnAddTmplt_Click exception {ex}");
                    await Utilities.Message1BDialog(Content.XamlRoot, "Kneeboard Template Import Failed", msg);
                }
            }
        }

        /// <summary>
        /// delete template click: remove the currently selected kneeboard template package from the known
        /// templates. the template file is removed from the kneeboard area if the user approves. the default template is
        /// selected upon deletions.
        /// </summary>
        private async void BtnDelTmplt_Click(object sender, RoutedEventArgs args)
        {
            if (uiComboTemplate.SelectedIndex > 0)
            {
                TemplateComboItem item = uiComboTemplate.SelectedItem as TemplateComboItem;
                ContentDialogResult result = await Utilities.Message2BDialog(Content.XamlRoot,
                    "Delete Kneeboard Template Package",
                    $"Are you sure you want to delete the kneeboard template package “{item.DisplayName}”?" +
                    $" This action cannot be undone.",
                    "Cancel",
                    "Delete Template Package"
                );
                if (result == ContentDialogResult.None)
                {
                    FileManager.DeleteKBTemplatePackage(Config.Airframe, item.TemplateName);
                    FileManager.DeleteKBTemplatePackage(AirframeTypes.UNKNOWN, item.TemplateName);
                    EditKboard.Template = "";
                    SaveEditStateToConfig();
                    RebuildKneeboardList();
                }
            }
        }

        /// <summary>
        /// set output click: if there is no output path selected, use the picker to specify a file for the
        /// merged tape. save the merged tape to the specified file.
        /// </summary>
        private async void BtnSetOutput_Click(object sender, RoutedEventArgs args)
        {
            bool shouldMerge = false;
            if (string.IsNullOrEmpty(EditKboard.OutputPath))
            {
                try
                {
                    FolderPicker picker = new((Application.Current as JAFDTC.App).Window.AppWindow.Id)
                    {
                        CommitButtonText = "Select for Kneeboards",
                        SuggestedStartLocation = PickerLocationId.Desktop,
                    };

                    PickFolderResult resultPick = await picker.PickSingleFolderAsync();
                    if (resultPick != null)
                    {
                        UpdateKboardOutputPath(resultPick.Path);
                        SaveEditStateToConfig();
                        shouldMerge = true;
                    }
                }
                catch (Exception ex)
                {
                    FileManager.Log($"EditSimulatorKboardPage:BtnSetOutput_Click exception {ex}");
                    await Utilities.Message1BDialog(Content.XamlRoot, "Selection Failed", "Unable to select that file for output.");
                    shouldMerge = false;
                }
            }
            else
            {
                shouldMerge = true;
            }

            if (shouldMerge)
            {
                try
                {
                    Config.SaveMergedKboards();
                    string format = (EditKboard.EnableSVGValue) ? "SVG" : "PNG";
                    await Utilities.Message1BDialog(Content.XamlRoot, "Kneeboards Created",
                                                    $"Successfully saved kneeboards in {format} format in the directory\n\n" +
                                                    $"{EditKboard.OutputPath}");
                }
                catch (Exception ex)
                {
                    FileManager.Log($"EditSimulatorDTCPage:BtnSetOutput_Click exception {ex}");
                    await Utilities.Message1BDialog(Content.XamlRoot, "Kneeboard Creation Failed",
                                                    $"Windows says you can thank {ex}");
                }
            }
        }

        /// <summary>
        /// clear output click: clear the output file.
        /// </summary>
        private void BtnClearOutput_Click(object sender, RoutedEventArgs args)
        {
            UpdateKboardOutputPath("");
        }

        /// <summary>
        /// system item click: change the state of the include content item.
        /// </summary>
        private void BtnSystemItem_Click(object sender, RoutedEventArgs args)
        {
            ToggleButton tbtn = (ToggleButton)sender;
            if ((tbtn.IsChecked == true) && !EditKboard.KneeboardTags.Contains(tbtn.Tag.ToString()))
                EditKboard.KneeboardTags.Add(tbtn.Tag.ToString());
            else if ((tbtn.IsChecked == false) && EditKboard.KneeboardTags.Contains((string)tbtn.Tag.ToString()))
                EditKboard.KneeboardTags.Remove(tbtn.Tag.ToString());

            if (EditKboard.KneeboardTags.Count == 0)
            {
                EditKboard.EnableRebuild = bool.FalseString;
                uiCkbxEnableRebuild.IsChecked = false;
            }

            SaveEditStateToConfig();
        }

        // ---- combos ------------------------------------------------------------------------------------------------

        /// <summary>
        /// combo selection: update the selection for the kneeboard template package combo.
        /// </summary>
        private void ComboTemplate_SelectionChanged(object sender, RoutedEventArgs args)
        {
            if (!IsUIRebuilding && (uiComboTemplate.SelectedItem != null))
            {
                TemplateComboItem item = uiComboTemplate.SelectedItem as TemplateComboItem;
                EditKboard.Template = item.TemplateName;
                SaveEditStateToConfig();
                RebuildKneeboardList(true);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on navigating to/from this page, set up and tear down our internal and ui state based on the configuration
        /// we are editing.
        /// 
        /// we do not use page caching here as we're just tracking the configuration state.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            ConfigEditorPageNavArgs navArgs = (ConfigEditorPageNavArgs)args.Parameter;
            PageHelper = (IEditSimulatorKboardPageHelper)Activator.CreateInstance(navArgs.EditorHelperType);

            base.OnNavigatedTo(args);

            PageHelper.ValidateKboardSystem(Config);

            CopyConfigToEditState();

            RebuildKneeboardList();
        }
    }
}
