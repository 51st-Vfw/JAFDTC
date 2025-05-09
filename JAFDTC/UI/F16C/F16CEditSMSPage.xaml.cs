// ********************************************************************************************************************
//
// F16CEditSMSPage.xaml.cs : ui c# for viper sms editor page
//
// Copyright(C) 2024-2025 ilominar/raven
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
using JAFDTC.Models.F16C;
using JAFDTC.Models.F16C.SMS;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using static JAFDTC.Models.F16C.SMS.SMSSystem;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// Page obejct for the system editor page that handles the ui for the viper sms munition setup editor. this
    /// handles setups for munition parameters like ripple modes, employment modes, etc.
    /// </summary>
    public sealed partial class F16CEditSMSPage : SystemEditorPageBase
    {
        public static ConfigEditorPageInfo PageInfo
            => new(SMSSystem.SystemTag, "Munitions", "SMS", Glyphs.SMS, typeof(F16CEditSMSPage));

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- overrides of base SystemEditorPage properties

        protected override SystemBase SystemConfig => ((F16CConfiguration)Config).SMS;

        protected override String SystemTag => SMSSystem.SystemTag;

        protected override string SystemName => "SMS munition setup";

        protected override bool IsPageStateDefault => ((F16CConfiguration)Config).SMS.IsDefault;

        // ---- internal properties

        private MunitionSettings EditSetup { get; set; }

        private SMSSystem.Munitions EditMuniID { get; set; }

        private string EditProfileID { get; set; }

        // ---- private read-only properties

        private readonly List<F16CMunition> _munitions;
        private readonly string[] _textForProfile;
        private readonly string[] _textForEmplMode;
        private readonly string[] _textForRippleMode;
        private readonly string[] _textForFuzeMode;
        
        private readonly List<FrameworkElement> _elemsProfile;
        private readonly List<FrameworkElement> _elemsRelease;
        private readonly List<FrameworkElement> _elemsSpin;
        private readonly List<FrameworkElement> _elemsFuze;
        private readonly List<FrameworkElement> _elemsArmDelay;
        private readonly List<FrameworkElement> _elemsArmDelay2;
        private readonly List<FrameworkElement> _elemsArmDelayMode;
        private readonly List<FrameworkElement> _elemsBurstAlt;
        private readonly List<FrameworkElement> _elemsReleaseAng;
        private readonly List<FrameworkElement> _elemsImpactAng;
        private readonly List<FrameworkElement> _elemsImpactAzi;
        private readonly List<FrameworkElement> _elemsImpactVel;
        private readonly List<FrameworkElement> _elemsCueRange;
        private readonly List<FrameworkElement> _elemsAutoPwr;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public F16CEditSMSPage()
        {
            EditSetup = new MunitionSettings();
            EditMuniID = SMSSystem.Munitions.CBU_87;
            EditProfileID = ((int)MunitionSettings.Profiles.PROF1).ToString();

            _munitions = FileManager.LoadF16CMunitions();

            _textForProfile = new[] { "PROF1", "PROF2", "PROF3", "PROF4" };
            _textForEmplMode = new[] { "CCIP", "CCRP", "DTOS", "LADD", "MAN", "PRE", "VIS", "BORE" };
            _textForRippleMode = new[] { "SGL", "PAIR", "Single", "Front/Back", "Left/Right", "1", "2",
                                         "SGL", "RP 1", "RP 2", "RP 3", "RP 4" };
            _textForFuzeMode = new[] { "NSTL", "NOSE", "TAIL", "NSTL (HI)", "NOSE (LO)", "TAIL (HI)" };

            InitializeComponent();
            InitializeBase(EditSetup, uiValueRippleQty, uiCtlLinkResetBtns);

            _elemsProfile = new() { uiLabelProfile, uiComboProfile, uiCkboxProfileEnb };
            _elemsRelease = new() { uiLabelRelMode, uiComboRelMode, uiStackRelMode };
            _elemsSpin = new() { uiLabelSpin, uiComboSpin, uiLabelSpinUnits };
            _elemsFuze = new() { uiLabelFuzeMode, uiComboFuzeMode };
            _elemsArmDelay = new() { uiLabelArmDelay, uiValueArmDelay, uiLabelArmDelayUnits };
            _elemsArmDelay2 = new() { uiLabelArmDelay2, uiValueArmDelay2, uiLabelArmDelay2Units };
            _elemsArmDelayMode = new() { uiLabelArmDelayMode, uiComboArmDelayMode, uiLabelArmDelayModeUnits };
            _elemsBurstAlt = new() { uiLabelBurstAlt, uiValueBurstAlt, uiLabelBurstAltUnits };
            _elemsReleaseAng = new() { uiLabelReleaseAng, uiValueReleaseAng, uiLabelReleaseAngUnits };
            _elemsImpactAng = new() { uiLabelImpactAng, uiValueImpactAng, uiLabelImpactAngUnits };
            _elemsImpactAzi = new() { uiLabelImpactAzi, uiValueImpactAzi, uiLabelImpactAziUnits };
            _elemsImpactVel = new() { uiLabelImpactVel, uiValueImpactVel, uiLabelImpactVelUnits };
            _elemsCueRange = new() { uiLabelCueRng, uiValueCueRng, uiLabelCueRngUnits };
            _elemsAutoPwr = new() { uiLabelAutoPwr, uiComboAutoPwr, null, uiStackAutoPwr };
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // data marshalling
        //
        // ------------------------------------------------------------------------------------------------------------

        private static void CopyPropertyHonorDefault(BindableObject next, BindableObject edit, BindableObject dflt,
                                                     string propName)
        {
            if ((propName != null) && (next != null) && (edit != null) && (dflt != null))
            {
                PropertyInfo prop = next.GetType().GetProperty(propName);
                string editVal = (string)prop.GetValue(edit);
                string dfltVal = (string)prop.GetValue(dflt);
                prop.SetValue(next, (!string.IsNullOrEmpty(editVal) && (editVal != dfltVal)) ? editVal : "");
            }
        }

        private void CopyPropertyHonorDefaultComboVal(BindableObject next, BindableObject edit, BindableObject dflt,
                                                      string propName)
        {
            if ((propName != null) && (next != null) && (edit != null) && (dflt != null))
            {
                PropertyInfo prop = next.GetType().GetProperty(propName);
                string editVal = (string)prop.GetValue(edit);
                CrackComboBoxSpec((string)prop.GetValue(dflt), out string dfltVal, out _);
                prop.SetValue(next, (!string.IsNullOrEmpty(editVal) && (editVal != dfltVal)) ? editVal : "");
            }
        }

        /// <summary>
        /// Copy data from the system configuration object to the edit object the page interacts with.
        /// </summary>
        protected override void CopyConfigToEditState()
        {
            if (EditState != null)
            {
                EditSetup.ID = EditMuniID;
                EditSetup.Profile = EditProfileID;
                base.CopyConfigToEditState();
            }
        }

        /// <summary>
        /// Copy data from the edit object the page interacts with to the system configuration object and persist the
        /// updated configuration to disk.
        /// </summary>
        protected override void SaveEditStateToConfig()
        {
            if ((EditState != null) && !EditState.HasErrors)
            {
                F16CConfiguration config = (F16CConfiguration)Config;

                // in the event we are enabling IsProfileSelected, ensure the munition has at most one profile with
                // IsProfileSelected set.
                //
                if (EditSetup.IsProfileSelected == "True")
                {
                    Dictionary<string, MunitionSettings> profiles = config.SMS.GetProfilesForMunition(EditMuniID);
                    foreach (MunitionSettings muni in profiles.Values)
                        if (muni.Profile != EditSetup.Profile)
                            muni.IsProfileSelected = "";
                }

                // all mav variants share the same munition settings. we'll replicate the EditSetup properties
                // across all mav variants.
                //
                List<Munitions> muniIDs = new() { EditMuniID };
                if ((EditMuniID == Munitions.AGM_65D) || (EditMuniID == Munitions.AGM_65G) ||
                    (EditMuniID == Munitions.AGM_65H) || (EditMuniID == Munitions.AGM_65K))
                {
                    muniIDs = new() { Munitions.AGM_65D, Munitions.AGM_65G, Munitions.AGM_65H, Munitions.AGM_65K };
                }

                // remap the EditSetup properties based on defaults from _munitions. if the edit settings match the
                // default value, we will update to "". we do this here because it is easier at this point to enforce
                // setting a default field to "" than to determine later when a non-"" field is default.
                //
                foreach (Munitions muniID in muniIDs)
                {
                    MunitionSettings settings = config.SMS.GetSettingsForMunitionProfile(muniID, EditProfileID);
                    MunitionSettings defaults = _munitions[(int)EditMuniID].MunitionInfo;

                    CopyPropertyHonorDefault(settings, EditState, defaults, "IsProfileSelected");
                    CopyPropertyHonorDefault(settings, EditState, defaults, "RipplePulse");
                    CopyPropertyHonorDefault(settings, EditState, defaults, "RippleSpacing");
                    CopyPropertyHonorDefault(settings, EditState, defaults, "ArmDelay");
                    CopyPropertyHonorDefault(settings, EditState, defaults, "ArmDelay2");
                    CopyPropertyHonorDefault(settings, EditState, defaults, "BurstAlt");
                    CopyPropertyHonorDefault(settings, EditState, defaults, "ReleaseAng");
                    CopyPropertyHonorDefault(settings, EditState, defaults, "ImpactAng");
                    CopyPropertyHonorDefault(settings, EditState, defaults, "ImpactAzi");
                    CopyPropertyHonorDefault(settings, EditState, defaults, "ImpactVel");
                    CopyPropertyHonorDefault(settings, EditState, defaults, "CueRange");
                    CopyPropertyHonorDefault(settings, EditState, defaults, "AutoPwrSP");

                    CopyPropertyHonorDefaultComboVal(settings, EditState, defaults, "EmplMode");
                    CopyPropertyHonorDefaultComboVal(settings, EditState, defaults, "ReleaseMode");
                    CopyPropertyHonorDefaultComboVal(settings, EditState, defaults, "RippleDelayMode");
                    CopyPropertyHonorDefaultComboVal(settings, EditState, defaults, "FuzeMode");
                    CopyPropertyHonorDefaultComboVal(settings, EditState, defaults, "ArmDelayMode");
                    CopyPropertyHonorDefaultComboVal(settings, EditState, defaults, "Spin");
                    CopyPropertyHonorDefaultComboVal(settings, EditState, defaults, "AutoPwrMode");

                    // MAN employment mode has different RippleSpacing defaults than other modes. we'll just
                    // unilaterally slam RippleSpaing to "" here for MAN employment just to be safe.
                    //
                    if (settings.EmplMode == ((int)MunitionSettings.EmploymentModes.MAN).ToString())
                        settings.RippleSpacing = "";

                    // cannot have a ripple delay without more than one ripple pulse.
                    //
                    if (string.IsNullOrEmpty(settings.RipplePulse) || (settings.RipplePulse == "1"))
                        settings.RippleDelayMode = "";
                }

                config.SMS.CleanUp();
                config.Save(this, SystemTag);
                UpdateUIFromEditState();
            }
        }

        /// <summary>
        /// Find and return the property information and encapsulating object corresponding to the provided control
        /// in the persisted configuration objects. We need to remap onto the specific munition instance for the
        /// current munition and profile.
        /// </summary>
        protected override void GetControlConfigProperty(FrameworkElement ctrl,
                                                         out PropertyInfo prop, out BindableObject obj)
        {
            F16CConfiguration config = (F16CConfiguration)Config;
            MunitionSettings settings = config.SMS.GetSettingsForMunitionProfile(EditMuniID, EditProfileID);
            prop = settings.GetType().GetProperty(ctrl.Tag.ToString());
            obj = settings;

            if (prop == null)
                throw new ApplicationException($"Unexpected {ctrl.GetType()}: Tag {ctrl.Tag}");
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// crack a ComboBox item list spec of the form "{defaults_csv};{tags_csv}" where {tags_csv} is a csv list of
        /// tags to populate the list and {defaults_csv} is a csv list of the default tags for each profile a munition
        /// supports (a single entry implies the default tag is independent of profile). method returns the appropriate
        /// default tag for the current profile from {defaults_csv} (null if unknown) along with the cracked list of
        /// tags from {tags_csv} (empty list if unknown).
        /// </summary>
        private void CrackComboBoxSpec(string spec, out string defaultTag, out List<string> tags)
        {
            int profileID = int.Parse(EditProfileID);
            if (string.IsNullOrEmpty(spec))
                spec = ";";
            List<string> fields = spec.Split(';').ToList();
            List<string> defaults = fields[0].Split(',').ToList();

            defaultTag = (defaults.Count > 0) ? defaults[(profileID < defaults.Count) ? profileID : 0] : "";
            tags = fields[1].Split(',').ToList();
        }

        /// <summary>
        /// select the item from a ComboBox that has a tag matching the given value. selects the index of the default
        /// item (indicated by a tag with a "+" prefix) if no matching item is found.
        /// </summary>
        private static void SelectComboItemWIthTag(ComboBox combo, string tag)
        {
            int selIndex = 0;
            for (int i = 0; i < combo.Items.Count; i++)
            {
                FrameworkElement elem = (FrameworkElement)combo.Items[i];
                if ((elem != null) && (elem.Tag != null))
                {
                    string elemTag = elem.Tag.ToString();
                    if ((elemTag == tag) || (elemTag == $"+{tag}"))
                    {
                        selIndex = i;
                        break;
                    }
                    else if (elemTag[0] == '+')
                        selIndex = i;
                }
            }
            if (selIndex != combo.SelectedIndex)
                combo.SelectedIndex = selIndex;
        }

        /// <summary>
        /// set the items of a ComboBox for the current profile from a specification of the form
        /// "{defaults_csv};{tags_csv}" as parsed by CrackComboBoxSpec(). if textMap is specified, the tags are
        /// assumed to be string-serialized integers suitable for indexing the textMap array to determine the
        /// text to display in the item.
        /// </summary>
        private void SetComboItemsFromSpec(ComboBox combo, string spec, string[] textMap,
                                           Func<string, string, FrameworkElement> buildItem)
        {
            List<FrameworkElement> items = new();
            if (!string.IsNullOrEmpty(spec))
            {
                CrackComboBoxSpec(spec, out string defaultTag, out List<string> tags);
                Debug.Assert(!string.IsNullOrEmpty(defaultTag) && (tags.Count > 0));
                for (int i = 0; i < tags.Count; i++)
                    items.Add(buildItem((textMap != null) ? textMap[int.Parse(tags[i])] : tags[i],
                                        (tags[i] == defaultTag) ? $"+{tags[i]}" : tags[i]));
            }
            combo.ItemsSource = items;
        }

        /// <summary>
        /// sets the visibility of FrameworkElements in a list according to the value of a spec string from munition
        /// information. elements are hidden if the spec string is null/empty or they follow a null item in the list,
        /// visible otherwise.
        /// </summary>
        private static Visibility SetVisibilityFromSpec(List<FrameworkElement> elems, string spec)
        {
            Visibility visible = (!string.IsNullOrEmpty(spec)) ? Visibility.Visible : Visibility.Collapsed;
            foreach (FrameworkElement elem in elems)
                if (elem != null)
                    elem.Visibility = visible;
                else
                    visible = Visibility.Collapsed;
            return visible;
        }

        /// <summary>
        /// set the default/non-default icons in the munition list according to whether or not a munition has a
        /// default configuration. a munition is non-default if it has any non-default profiles.
        /// </summary>
        private void UpdateNonDefaultMunitionItems()
        {
            F16CConfiguration config = (F16CConfiguration)Config;
            foreach (F16CMunition munition in uiListMunition.Items.Cast<F16CMunition>())
            {
                UIElement container = (UIElement)uiListMunition.ContainerFromItem(munition);
                FontIcon icon = Utilities.FindControl<FontIcon>(container, typeof(FontIcon), "DefaultBadgeIcon");
                if (icon != null)
                {
                    Dictionary<string, MunitionSettings> profiles = config.SMS.GetProfilesForMunition(munition.ID);
                    Visibility visibility = Visibility.Collapsed;
                    foreach (MunitionSettings settings in profiles.Values)
                        if (!settings.IsDefault)
                            visibility = Visibility.Visible;
                    icon.Visibility = visibility;
                }
            }
        }

        /// <summary>
        /// set the default/non-default icons in the profile list according to whether or not the profile has a
        /// default configuration.
        /// </summary>
        private void UpdateNonDefaultProfileItems()
        {
            F16CConfiguration config = (F16CConfiguration)Config;
            foreach (Grid grid in uiComboProfile.Items.Cast<Grid>())
            {
                string profileID = grid.Tag.ToString();
                profileID = (profileID[0] == '+') ? profileID[1..] : profileID;
                MunitionSettings settings = config.SMS.GetSettingsForMunitionProfile(EditMuniID, profileID, false);
                FontIcon icon = Utilities.FindControl<FontIcon>(grid, typeof(FontIcon), "BadgeIcon");
                if (icon != null)
                    icon.Visibility = Utilities.HiddenIfDefault(settings);
            }
        }

        /// <summary>
        /// set up visibility of the ripple-related fields (quantity, spacing, and delay) based on the current
        /// release mode selected in the settings along with the munition type.
        /// </summary>
        private void SetVisibilityRippleUI(MunitionSettings.ReleaseModes relMode, string ripplePulse, Munitions muniID)
        {
            Boolean isRippleFeetViz;
            Boolean isRippleDtViz;

            switch (relMode)
            {
                case MunitionSettings.ReleaseModes.TRI_PAIR_F2B:
                case MunitionSettings.ReleaseModes.TRI_PAIR_L2R:
                    uiStackRelMode.Visibility = Visibility.Visible;
                    uiLabelRippleQty.Visibility = Visibility.Collapsed;
                    uiValueRippleQty.Visibility = Visibility.Collapsed;
                    uiValueRippleFt.Visibility = Visibility.Visible;
                    uiLabelRippleFtUnits.Visibility = Visibility.Visible;
                    uiComboRippleQty.Visibility = Visibility.Collapsed;
                    uiComboRippleDt.Visibility = Visibility.Collapsed;
                    uiLabelRippleDtUnits.Visibility = Visibility.Collapsed;
                    break;
                default:
                    if (string.IsNullOrEmpty(ripplePulse))
                    {
                        uiStackRelMode.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        isRippleFeetViz = false;
                        isRippleDtViz = false;

                        uiStackRelMode.Visibility = Visibility.Visible;
                        uiLabelRippleQty.Visibility = Visibility.Visible;
                        if (muniID == Munitions.GBU_24)
                        {
                            uiValueRippleQty.Visibility = Visibility.Collapsed;
                            uiComboRippleQty.Visibility = Visibility.Visible;
                            if (!string.IsNullOrEmpty(EditSetup.RipplePulse) && (EditSetup.RipplePulse != "1"))
                                isRippleDtViz = true;
                        }
                        else
                        {
                            uiValueRippleQty.Visibility = Visibility.Visible;
                            uiComboRippleQty.Visibility = Visibility.Collapsed;
                            if (EditSetup.EmplMode != ((int)MunitionSettings.EmploymentModes.MAN).ToString())
                                isRippleFeetViz = true;
                        }

                        uiLabelRippleAt.Visibility = (isRippleFeetViz || isRippleDtViz) ? Visibility.Visible
                                                                                        : Visibility.Collapsed;
                        uiValueRippleFt.Visibility = (isRippleFeetViz) ? Visibility.Visible : Visibility.Collapsed;
                        uiLabelRippleFtUnits.Visibility = (isRippleFeetViz) ? Visibility.Visible : Visibility.Collapsed;

                        uiComboRippleDt.Visibility = (isRippleDtViz) ? Visibility.Visible : Visibility.Collapsed;
                        uiLabelRippleDtUnits.Visibility = (isRippleDtViz) ? Visibility.Visible : Visibility.Collapsed;
                    }
                    break;
            }
        }

        /// <summary>
        /// rebuild the core interface for a munition or profile change. this involves setting up initial visibility
        /// on the controls relevant to the selected munition along with regenerating things like ComboBox content,
        /// placeholder strings, etc. this function sets up munition-specific state that is *not* dependent on
        /// specific settings (UpdateUICustom() handles state that depends on specific settings).
        /// </summary>
        private void UpdateUIForCoreChange(F16CMunition newMunition)
        {
            StartUIRebuild();

            SMSSystem.Munitions muniID = newMunition.ID;
            bool isMav = ((muniID == Munitions.AGM_65D) || (muniID == Munitions.AGM_65G) ||
                          (muniID == Munitions.AGM_65H) || (muniID == Munitions.AGM_65K));

            // rebuild the combo items in the profile, employment, release, fuze, spin, and arm delay combos
            // according to the selected munition.
            //
            MunitionSettings info = newMunition.MunitionInfo;
            SetComboItemsFromSpec(uiComboProfile, info.Profile, _textForProfile, Utilities.BulletComboBoxItem);
            SetComboItemsFromSpec(uiComboEmploy, info.EmplMode, _textForEmplMode, Utilities.TextComboBoxItem);
            SetComboItemsFromSpec(uiComboRelMode, info.ReleaseMode, _textForRippleMode, Utilities.TextComboBoxItem);
            SetComboItemsFromSpec(uiComboFuzeMode, info.FuzeMode, _textForFuzeMode, Utilities.TextComboBoxItem);
            SetComboItemsFromSpec(uiComboSpin, info.Spin, null, Utilities.TextComboBoxItem);
            SetComboItemsFromSpec(uiComboArmDelayMode, info.ArmDelayMode, null, Utilities.TextComboBoxItem);

            // set baseline visibility based on the newly selected munition (eg, hide controls that are not
            // relevant and show those that are). this only handles visibility that is a function of the munition,
            // not visibility that is a function of the settings (UpdateUiCustom() takes care of that).
            //
            // NOTE: employment mode row is always visible as all munitions have an employment mode setting.
            //
            SetVisibilityFromSpec(_elemsProfile, info.Profile);
            SetVisibilityFromSpec(_elemsRelease, info.ReleaseMode);
            SetVisibilityFromSpec(_elemsSpin, info.Spin);
            SetVisibilityFromSpec(_elemsFuze, info.FuzeMode);
            SetVisibilityFromSpec(_elemsArmDelay, info.ArmDelay);
            SetVisibilityFromSpec(_elemsArmDelay2, info.ArmDelay2);
            SetVisibilityFromSpec(_elemsArmDelayMode, info.ArmDelayMode);
            SetVisibilityFromSpec(_elemsBurstAlt, info.BurstAlt);
            SetVisibilityFromSpec(_elemsReleaseAng, info.ReleaseAng);
            SetVisibilityFromSpec(_elemsImpactAng, info.ImpactAng);
            SetVisibilityFromSpec(_elemsImpactAzi, info.ImpactAzi);
            SetVisibilityFromSpec(_elemsImpactVel, info.ImpactVel);
            SetVisibilityFromSpec(_elemsCueRange, info.CueRange);
            SetVisibilityFromSpec(_elemsAutoPwr, info.AutoPwrMode);

            SetVisibilityRippleUI(EditSetup.ReleaseModeEnum, info.RipplePulse, muniID);

            // only show mav note when mavs are being edited.
            //
            uiLabelMavNote.Visibility = (isMav) ? Visibility.Visible : Visibility.Collapsed;

            // change release mode label to line up with the way mavs handle ripples versus other munitions.
            //
            uiLabelRelMode.Text = (isMav) ? "Ripple Quantity" : "Release Mode";

            // change text field placeholders and maximum lengths to line up with the parameter ranges for the
            // selected munition.
            //
            if (!string.IsNullOrEmpty(info.ArmDelay))
            {
                uiValueArmDelay.PlaceholderText = info.ArmDelay;
            }
            if (!string.IsNullOrEmpty(info.BurstAlt))
            {
                uiValueBurstAlt.PlaceholderText = info.BurstAlt;
                uiValueBurstAlt.MaxLength = ((info.ID == Munitions.CBU_87) || (info.ID == Munitions.CBU_97)) ? 5 : 4;
            }
            if (!string.IsNullOrEmpty(info.ReleaseAng))
            {
                uiValueReleaseAng.PlaceholderText = info.ReleaseAng;
                uiValueReleaseAng.MaxLength = (info.ID == Munitions.GBU_24) ? 3 : 2;
            }
            if (!string.IsNullOrEmpty(info.RippleSpacing))
            {
                uiValueRippleFt.PlaceholderText = info.RippleSpacing;
                uiValueRippleFt.MaxLength = ((info.ID == Munitions.CBU_103) || (info.ID == Munitions.CBU_105)) ? 4 : 3;
            }

            FinishUIRebuild();
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        protected override void UpdateUICustom(bool isEditable)
        {
            UpdateNonDefaultMunitionItems();
            UpdateNonDefaultProfileItems();

            F16CMunition muni = (F16CMunition)uiListMunition.SelectedItem;
            uiTextMuniDesc.Text = (muni != null) ? muni.DescrUI : "No Munition Selected";
            uiMuniBtnResetTitle.Text = (string.IsNullOrEmpty(muni.MunitionInfo.Profile)) ? "Reset Parameters to Defaults"
                                                                                         : "Reset Profile to Defaults";

            // base class does not manage the profile number since this property is immutable once a set of munition
            // settings are added to the configuration.
            //
            SelectComboItemWIthTag(uiComboProfile, EditProfileID);

            // set the per-munition reset button's enable state based on whether or not there are any changes to any
            // of the munition's profile.
            //
            Utilities.SetEnableState(uiMuniBtnReset, !EditSetup.IsDefault);

            // set up visibility of the ripple-related fields (quantity, spacing, and delay) based on the current
            // release mode selected in the settings along with the munition type.
            //
            SetVisibilityRippleUI(EditSetup.ReleaseModeEnum, muni.MunitionInfo.RipplePulse, muni.MunitionInfo.ID);

            // auto power steerpoint fields are only visible if auto power mode is not off.
            //
            uiStackAutoPwr.Visibility = ((EditSetup.AutoPwrModeEnum != MunitionSettings.AutoPowerModes.Unknown) &&
                                         (EditSetup.AutoPwrModeEnum != MunitionSettings.AutoPowerModes.OFF))
                                        ? Visibility.Visible : Visibility.Collapsed;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // ui events
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- munition list -----------------------------------------------------------------------------------------

        /// <summary>
        /// munition list selection change: save the state of the previously-selected muntion and update the ui to
        /// display the just-selected munition.
        /// </summary>
        private void ListMunition_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if ((args.RemovedItems.Count > 0) && ((F16CMunition)args.RemovedItems[0] != null))
                SaveEditStateToConfig();

            if (args.AddedItems.Count > 0)
            {
                F16CMunition newSelectedMunition = (F16CMunition)args.AddedItems[0];
                if (newSelectedMunition != null)
                {
                    EditMuniID = (SMSSystem.Munitions)newSelectedMunition.ID;
                    EditProfileID = ((int)MunitionSettings.Profiles.PROF1).ToString();
                    CopyConfigToEditState();
                    UpdateUIForCoreChange(newSelectedMunition);
                }
            }

            UpdateUIFromEditState();
        }

        // ---- munition parameters -----------------------------------------------------------------------------------

        /// <summary>
        /// profile id selection change: save the state of the previously-selected profile and update the ui to
        /// display the just-selected profile.
        /// </summary>
        private void ComboProfile_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (!IsUIRebuilding)
            {
                ComboBox comboBox = (ComboBox)sender;
                FrameworkElement item = (FrameworkElement)comboBox.SelectedItem;
                if (item != null)
                {
                    Debug.Assert(item.Tag != null);
                    SaveEditStateToConfig();

                    string tag = item.Tag.ToString();
                    EditProfileID = (tag[0] == '+') ? tag[1..] : tag;
                    CopyConfigToEditState();
                    UpdateUIForCoreChange(_munitions[(int)EditMuniID]);
                    UpdateUIFromEditState();
                }
            }
        }

        /// <summary>
        /// reset munition click: reset the current munition/profile and persist the configuration.
        /// </summary>
        private void MuniBtnReset_Click(object sender, RoutedEventArgs args)
        {
            EditSetup.Reset();
            SaveEditStateToConfig();
            UpdateUIFromEditState();
        }

        // ---- page-level event handlers -----------------------------------------------------------------------------

        /// <summary>
        /// on navigation to the page, select the first muinition from the munition list to get something set up.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            base.OnNavigatedTo(args);

            // TODO: consider preserving selected munition across visits?            
            uiListMunition.SelectedIndex = 0;
        }
    }
}
