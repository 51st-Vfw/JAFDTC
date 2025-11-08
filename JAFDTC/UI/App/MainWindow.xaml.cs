// ********************************************************************************************************************
//
// MainWindow.xaml.cs -- ui c# for main window
//
// Copyright(C) 2023-2025 ilominar/raven
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

using JAFDTC.UI.Base;
using JAFDTC.Utilities;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Graphics;

namespace JAFDTC.UI.App
{
    /// <summary>
    /// main window for jafdtc.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // windoze interfaces & data structs
        //
        // ------------------------------------------------------------------------------------------------------------

        private delegate IntPtr WinProc(IntPtr hWnd, WindowMessage Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll")]
        internal static extern int GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, WindowLongIndexFlags nIndex, WinProc newProc);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, WindowLongIndexFlags nIndex, WinProc newProc);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, WindowMessage Msg, IntPtr wParam, IntPtr lParam);

        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPOS
        {
            public nint hwnd;
            public nint hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        [Flags]
        private enum WindowLongIndexFlags : int
        {
            GWL_WNDPROC = -4,
        }

        private enum WindowMessage : int
        {
            WM_GETMINMAXINFO = 0x0024,
            WM_WINDOWPOSCHANGED = 0x0047
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public ConfigurationListPage ConfigListPage { get; private set; }

        // ---- internal properties

        private AutoSuggestBox AutoSuggestionBox { get; set; }

        private static WinProc _newWndProc = null;
        private static IntPtr _oldWndProc = IntPtr.Zero;

        private static readonly SizeInt32 _windSizeBase = new() { Width = 1000, Height = 700 };
        private static readonly SizeInt32 _windSizeMin = new() { Width = 1000, Height = 675 };
        private static SizeInt32 _windSizeMax = new() { Width = 1800, Height = 1600 };

        private static PointInt32 _windPosnCur = new() { X = 0, Y = 0 };
        private static SizeInt32 _windSizeCur = new() { Width = 0, Height = 0 };

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MainWindow()
        {
            InitializeComponent();

            AutoSuggestionBox = uiAppConfigFilterBox;

            Title = "JAFDTC";

            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Tall;

            Closed += MainWindow_Closed;
            SizeChanged += MainWindow_SizeChanged;

            // ---- window setup

            string lastSetup = Settings.LastWindowSetupMain;

            nint hWnd = GetWindowHandleForCurrentWindow(this);
            double dpiWnd = GetDpiForWindow(hWnd);
            SizeInt32 baseSize = Utilities.BuildWindowSize(dpiWnd, _windSizeBase, lastSetup);
            AppWindow.Resize(baseSize);

            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWind = AppWindow.GetFromWindowId(windowId);
            if (appWind != null)
            {
                appWind.SetIcon(@"Images/JAFDTC_Icon.ico");
                DisplayArea dispArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
                if (dispArea != null)
                {
                    PointInt32 posn = Utilities.BuildWindowPosition(dpiWnd, dispArea.WorkArea, baseSize, lastSetup);
                    appWind.Move(posn);
                    _windSizeMax.Width = Math.Max(_windSizeMax.Width, dispArea.WorkArea.Width);
                    _windSizeMax.Height = Math.Max(_windSizeMax.Height, dispArea.WorkArea.Height);
                }
            }

            // sets up min/max window sizes using the right magic. code pulled from stackoverflow:
            //
            // https://stackoverflow.com/questions/72825683/wm-getminmaxinfo-in-winui-3-with-c
            //
            _newWndProc = new WinProc(WndProc);
            _oldWndProc = SetWindowLongPtr(hWnd, WindowLongIndexFlags.GWL_WNDPROC, _newWndProc);

            OverlappedPresenter presenter = (OverlappedPresenter)AppWindow.Presenter;
            presenter.IsAlwaysOnTop = Settings.IsAlwaysOnTop;
            // presenter.IsResizable = false;
            // presenter.IsMaximizable = false;

            if ((Application.Current as JAFDTC.App).IsAppStartupGood)
            {
                uiAppContentFrame.Navigate(typeof(ConfigurationListPage), null);
                ConfigListPage = (ConfigurationListPage)uiAppContentFrame.Content;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // window sizing support
        //
        // ------------------------------------------------------------------------------------------------------------

        // sets up min/max window sizes using the right magic, see
        //
        // https://stackoverflow.com/questions/72825683/wm-getminmaxinfo-in-winui-3-with-c

        private static IntPtr GetWindowHandleForCurrentWindow(object target) =>
            WinRT.Interop.WindowNative.GetWindowHandle(target);

        private static IntPtr WndProc(IntPtr hWnd, WindowMessage Msg, IntPtr wParam, IntPtr lParam)
        {
            switch (Msg)
            {
                case WindowMessage.WM_WINDOWPOSCHANGED:
                    var windPos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
                    _windPosnCur.X = windPos.x;
                    _windPosnCur.Y = windPos.y;
                    break;
                case WindowMessage.WM_GETMINMAXINFO:
                    var dpi = GetDpiForWindow(hWnd);
                    var scalingFactor = (float)dpi / 96;

                    var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    minMaxInfo.ptMinTrackSize.x = (int)(_windSizeMin.Width * scalingFactor);
                    minMaxInfo.ptMaxTrackSize.y = (int)(_windSizeMax.Height * scalingFactor);
                    minMaxInfo.ptMaxTrackSize.x = (int)(_windSizeMax.Width * scalingFactor);
                    minMaxInfo.ptMinTrackSize.y = (int)(_windSizeMin.Height * scalingFactor);

                    Marshal.StructureToPtr(minMaxInfo, lParam, true);
                    break;

            }
            return CallWindowProc(_oldWndProc, hWnd, Msg, wParam, lParam);
        }

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, WindowLongIndexFlags nIndex, WinProc newProc)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, newProc);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, newProc));
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // title bar
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// change the visibility of the filter box. this is used to hide/show the filter box so it is only visible
        /// when the configuration list page is visible.
        /// </summary>
        public void SetConfigFilterBoxVisibility(Visibility visibility)
        {
            uiAppConfigFilterBox.Visibility = visibility;
            uiAppConfigFilterBox.IsEnabled = (visibility == Visibility.Visible);
            uiAppTitleBar.Content = (visibility == Visibility.Visible) ? AutoSuggestionBox : null;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // startup support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// content frame loaded: check for and handle the dcs lua install along with splash now that we have a
        /// xamlroot at which we can target dialogs. hand off the config filter control to the config list page.
        /// </summary>
        private async void AppContentFrame_Loaded(object sender, RoutedEventArgs args)
        {
            JAFDTC.App app = Application.Current as JAFDTC.App;
            if (app.IsAppStartupGood)
            {
                ConfigListPage.ConfigFilterBox = uiAppConfigFilterBox;

                if (Settings.IsVersionUpdated)
                    await Utilities.Message1BDialog(Content.XamlRoot, "Welcome to JAFDTC!", $"Version {Settings.VersionJAFDTC}");

                await CheckForPulledPork(app.CmdLnArgValuePack);

                ConfigListPage.FileActivations(app.CmdLnArgPathsWithConfigs);

                if (!Settings.IsNewVersCheckDisabled)
                    await CheckForUpdates();

                Sploosh(DCSLuaManager.LuaCheck());
            }
            else
            {
                uiAppConfigFilterBox.IsEnabled = false;
                await Utilities.Message1BDialog(Content.XamlRoot, "Bad Joss Captain...",
                                                "Unable to launch JAFDTC due to a fatal error and reasons...");
            }
        }

        /// <summary>
        /// internal sequencing for use at splash (and changes to lua installations) that that installs or updates lua
        /// on new versions. before calling, use DCSLuaManager.LuaCheck() to determine the current situation with lua,
        /// the result of this check is passed in through the parameter.
        /// </summary>
        public async void Sploosh(DCSLuaManagerCheckResult result)
        {
            if (result.ErrorTitle != null)
            {
                FileManager.Log($"Lua check fails, {result.ErrorTitle}: {result.ErrorMessage}");
                await Utilities.Message1BDialog(Content.XamlRoot, result.ErrorTitle, result.ErrorMessage);
                Settings.IsSkipDCSLuaInstall = true;
            }
            else if ((result.InstallPaths.Count == 0) && (result.UpdatePaths.Count == 0))
            {
                foreach (KeyValuePair<string, bool> kvp in DCSLuaManager.LuaInstallStatus())
                    if (kvp.Value)
                        FileManager.Log($"Lua install found at {kvp.Key}");
            }
            else if (!Settings.IsSkipDCSLuaInstall)
            {
                string title = "Install JAFDTC DCS Lua Support?";
                bool didUpdate;
                string successes = "";
                string sucPlural = "";
                string errors = "";
                string errPlural = "";
                foreach (string path in result.InstallPaths)
                {
                    string msg = $"Would you like to add the JAFDTC Lua support to the DCS installation at:\n\n" +
                                 $"      {path}\n\n" +
                                 $"This support is necessary to allow JAFDTC to upload configurations to your jet. " +
                                 $"Please make sure DCS is not running before proceeding. " +
                                 $"You can install Lua support later through the JAFDTC Settings page.";
                    ContentDialogResult choice = await Utilities.Message2BDialog(Content.XamlRoot, title, msg, "Install");
                    if ((choice == ContentDialogResult.Primary) && DCSLuaManager.InstallOrUpdateLua(path, out didUpdate))
                    {
                        sucPlural = (successes.Length > 0) ? "s" : "";
                        successes += $"      {path} (Installed)\n";
                        FileManager.Log($"Successfully installed Lua at {path}");
                    }
                    else if (choice == ContentDialogResult.Primary)
                    {
                        errPlural = (successes.Length > 0) ? "s" : "";
                        errors += $"      {path}\n";
                        FileManager.Log($"Failed to install Lua at {path}");
                    }
                    else
                    {
                        Settings.IsSkipDCSLuaInstall = true;
                    }
                }

                foreach (string path in result.UpdatePaths)
                {
                    bool isSuccess = DCSLuaManager.InstallOrUpdateLua(path, out didUpdate);
                    if (isSuccess && didUpdate)
                    {
                        sucPlural = (successes.Length > 0) ? "s" : "";
                        successes += $"      {path} was Updated\n";
                        FileManager.Log($"Successfully updated Lua at {path}");
                    }
                    else if (isSuccess && !didUpdate)
                    {
                        FileManager.Log($"Using Lua install found at {path}");
                    }
                    else if (!isSuccess)
                    {
                        errPlural = (successes.Length > 0) ? "s" : "";
                        errors += $"      {path}\n";
                        FileManager.Log($"Failed to update Lua at {path}");
                    }
                }

                if (errors.Length > 0)
                {
                    string msg = $"Unable to add or update JAFDTC Lua support in the DCS installation{errPlural} at:\n\n" +
                                 $"{errors}\n" +
                                 $"You can install Lua support later through the JAFDTC Settings page.";
                    await Utilities.Message1BDialog(Content.XamlRoot, "Installation or Update Failed", msg);
                    Settings.IsSkipDCSLuaInstall = false;
                }
                else if (successes.Length > 0)
                {
                    string msg = $"JAFDTC Lua support successfully added to or updated in the DCS installation{sucPlural} at:\n\n" +
                                 $"{successes}\n" +
                                 $"If DCS is currently running, please restart it to apply these changes. You can uninstall Lua " +
                                 $"support later through the JAFDTC Settings page.";
                    await Utilities.Message1BDialog(Content.XamlRoot, "Qapla'!", msg);
                }
            }

            ConfigListPage.RebuildInterfaceState();
        }

        /// <summary>
        /// check for, and process, jafdtc updates. if there is a new version, and user wants to pull it, download
        /// from github. returns the version string for the latest version.
        /// </summary>
        private async Task<string> CheckForUpdates()
        {
            string githubVersion = Globals.VersionJAFDTC;
            try
            {
                using HttpClient client = new();
                githubVersion = await client.GetStringAsync("https://raw.githubusercontent.com/51st-Vfw/JAFDTC/main/VERSION.txt");
            }
            catch (System.Exception ex)
            {
                FileManager.Log($"Update check got exception pulling current remote version number {ex}");
                githubVersion = "Unknown";
            }

            string skipVers = (string.IsNullOrEmpty(Settings.SkipJAFDTCVersion)) ? "None" : Settings.SkipJAFDTCVersion;
            FileManager.Log($"Update check found local {Globals.VersionJAFDTC}, latest remote {githubVersion}, skip {skipVers}");

            if ((githubVersion != Settings.SkipJAFDTCVersion) && (githubVersion != Globals.VersionJAFDTC))
            {
                FileManager.Log($"Update check could update, asking permission...");

                ContentDialogResult actionUpdate = await Utilities.Message3BDialog(
                    Content.XamlRoot,
                    $"New Version Available",
                    $"JAFDTC version {githubVersion} and its release notes are available on GitHub at\n\n" +
                    $"    https://github.com/51st-Vfw/JAFDTC/releases/tag/{githubVersion}\n\n" +
                    $"JAFDTC can download the installer package to your “Downloads” folder now if you would like.",
                    $"Download",
                    $"Skip This Version",
                    $"Later");
                if (actionUpdate == ContentDialogResult.Primary)
                {
                    FileManager.Log($"Version check request to pull \"{githubVersion}\"");
                    await PullPackage(githubVersion);
                }
                else if (actionUpdate == ContentDialogResult.Secondary)
                {
                    FileManager.Log($"Update check sets SkipJAFDTCVersion to {githubVersion}");
                    Settings.SkipJAFDTCVersion = githubVersion;
                }
            }
            return githubVersion;
        }

        /// <summary>
        /// pull a jafdtc package with the given version from github.
        /// </summary>
        private async Task<string> CheckForPulledPork(string version)
        {
            if (!string.IsNullOrEmpty(version))
            {
                FileManager.Log($"Command line request to pull \"{version}\"");
                version = await PullPackage(version);
            }
            return version;
        }

        /// <summary>
        /// pull a jafdtc package with the given version from github.
        /// </summary>
        private async Task<string> PullPackage(string version)
        {
            try
            {
                string url = $"https://github.com/51st-Vfw/JAFDTC/releases/download/{version}/JAFDTC.Installer.msi";
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                           "Downloads", $"JAFDTC.{version}.Installer.msi");
                FileManager.Log($"Pull package remote: {url}");
                FileManager.Log($"Pull package local : {path}");

                ContentDialogResult actionReplace = ContentDialogResult.Primary;
                if (File.Exists(path))
                {
                    FileManager.Log($"Pull package {path} exists, asking permission...");

                    actionReplace = await Utilities.Message2BDialog(
                        Content.XamlRoot,
                        $"JAFDTC Package Exists",
                        $"There is already a “JAFDTC.{version}.Installer.msi” file in your Downloads folder. Would you like to replace it?",
                        $"Replace",
                        $"Cancel Download");
                }

                if (actionReplace == ContentDialogResult.Primary)
                {
                    FileManager.Log($"Pull package moves update package from remote to local");

                    using HttpClient client = new();
                    Stream msiStream = await client.GetStreamAsync(url);

                    using FileStream fileStream = new(path, FileMode.Create);
                    await msiStream.CopyToAsync(fileStream);

                    string msg = $"JAFDTC {version} package successfully copied to your Downloads folder,\n\n" +
                                 $"    {path}\n\n" +
                                 $"Share and enjoy!";
                    await Utilities.Message1BDialog(Content.XamlRoot, "Qapla'!", msg);
                }
                else
                {
                    FileManager.Log($"Pull package user cancels download");
                }
            }
            catch (System.Exception ex)
            {
                FileManager.Log($"Pull package got .msi exception {ex}");

                await Utilities.Message1BDialog(
                    Content.XamlRoot,
                    $"Sad Trombone",
                    $"Unable to download JAFDTC package with version “{version}”. Maybe try again later?");
            }
            return version;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // handlers
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// on window closed, persist the last window location and size to settings.
        /// </summary>
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            Settings.LastWindowSetupMain = Utilities.BuildWindowSetupString(_windPosnCur, _windSizeCur);
        }

        /// <summary>
        /// on size changed, stash the current window size into our window size for later persistance.
        /// </summary>
        private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs evt)
        {
            _windSizeCur.Width = (int)evt.Size.Width;
            _windSizeCur.Height = (int)evt.Size.Height;
        }
    }
}
