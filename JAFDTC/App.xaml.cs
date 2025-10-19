﻿// ********************************************************************************************************************
//
// App.xaml.cs -- ui c# for main application
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

// define this to enable the file activation handling in the application.
//
#define ENABLE_FILE_ACTIVATION

using JAFDTC.Models;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using JAFDTC.Utilities;
using JAFDTC.Utilities.Networking;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using static JAFDTC.Utilities.SettingsData;

namespace JAFDTC
{
    /// <summary>
    /// encodes the jafdtc command line.
    /// 
    ///     jafdtc [--open {path}] [--pack {version}] [path] .. [path]
    ///
    /// where
    /// 
    ///     --open      open the .jafdtc file at {path} and act appropriately (expect no user interaction)
    ///     --pack      download the .msi package from github for version {version}
    ///     [path]      .jafdtc file to open and process (may have user interaction)
    ///     
    /// </summary>
    public sealed class CmdLnArgInfo
    {
        public string Summary { get; }
        public string ArgValueOpen { get; }
        public string ArgValuePack { get; }
        public List<string> ArgPaths { get; }

        public CmdLnArgInfo(string _sum = null, string _avOpen = null, string _avPack = null, List<string> _path = null)
            => ( Summary, ArgValueOpen, ArgValuePack, ArgPaths ) = ( _sum, _avOpen, _avPack, _path );
    }

    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// 
    /// HACK: this class uses a hack to handle file activations on Microsoft.UI.Xaml.Application as the base class
    /// HACK: does not provide OnFileActivated unlike Windows.UI.Xaml.Application). file activations, as a result
    /// HACK: are kinky wonky.
    /// </summary>
    public partial class App : Application
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // windoze interfaces & data structs
        //
        // ------------------------------------------------------------------------------------------------------------

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, WindowShowStyle nCmdShow);

        // Full enum def is at https://github.com/dotnet/pinvoke/blob/main/src/User32/User32+WindowShowStyle.cs
        public enum WindowShowStyle : uint
        {
            SW_NORMAL = 1,
            SW_MAXIMIZE = 3,
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // statics
        //
        // ------------------------------------------------------------------------------------------------------------

#if ENABLE_FILE_ACTIVATION

        private const string JAFDTCFileActMutexName = "JAFDTC_FileActivationMutex";
        private const string JAFDTCFileActPipeName = "JAFDTC_FileActivationPipe";

        private static readonly Mutex MutexJAFDTC = new (false, JAFDTCFileActMutexName);

#endif

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public MainWindow Window { get; private set; }

        public CmdLnArgInfo CmdLnArgs { get; private set; }

        public bool IsAppStartupGood { get; private set; }

        public bool IsMainWindowBuilt { get; private set; }

        public bool IsAppShuttingDown { get; set; }

        public IConfiguration CurrentConfig { get; set; }

#if DCS_TELEM_INCLUDES_LAT_LON
        public double DCSLastLat { get; private set; }

        public double DCSLastLon { get; private set; }
#endif

        // ---- private properties

        private DispatcherTimer CheckDCSTimer { get; set; }

        private System.DateTimeOffset LastDCSExportCheck { get; set; }

        private long LastDCSExportPacketCount { get; set; }

        private bool IsJAFDTCPinnedToTop { get; set; }

        private bool IncPressed { get; set; }

        private bool DecPressed { get; set; }

        private bool TogglePressed { get; set; }

        private long UploadPressedTimestamp { get; set; }

        private long IncDecPressedTimestamp { get; set; }

        private long MarkerUpdateTimestamp { get; set; }

        // ---- public events, posts change/validation events

        // NOTE: this is a static to save us having to pass around App references for use. App should be a singleton
        // NOTE: anyway, so shouldn't be a big deal to have a static here given what is being reported.

        public static event EventHandler<string> DCSQueryResponseReceived;

        // NOTE: these can be called from non-ui threads but may trigger ui actions. we will dispatch the handler
        // NOTE: invocations on a ui thread to avoid the chaos that will ensue.

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Window?.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        private AirframeTypes _dcsActiveAirframe;
        public AirframeTypes DCSActiveAirframe
        {
            get => _dcsActiveAirframe;
            set
            {
                if (_dcsActiveAirframe != value)
                {
                    _dcsActiveAirframe = value;
                    OnPropertyChanged(nameof(DCSActiveAirframe));
                }
            }
        }

        /// <summary>
        /// reading IsDCSRunning will implicitly first set the property to the correct current value based on the state
        /// of dcs. this property does not have an explict set handler and generates property change events.
        /// </summary>
        private bool _isDCSRunning;
        public bool IsDCSRunning
        {
            get
            {
                if (_isDCSRunning && (Process.GetProcessesByName("DCS").Length == 0))
                {
                    _isDCSRunning = false;
                    OnPropertyChanged(nameof(IsDCSRunning));
                }
                else if (!_isDCSRunning && (Process.GetProcessesByName("DCS").Length > 0))
                {
                    _isDCSRunning = true;
                    OnPropertyChanged(nameof(IsDCSRunning));
                }
                return _isDCSRunning;
            }
        }

        /// <summary>
        /// reading IsDCSExporting will implicitly first set the property to the correct current value based on the
        /// state of dcs. this property does not have an explict set handler and generates property change events.
        /// </summary>
        private bool _isDCSExporting;
        public bool IsDCSExporting
        {
            get
            {
                if ((System.DateTimeOffset.Now - LastDCSExportCheck) > System.TimeSpan.FromSeconds(1.0))
                {
                    if (_isDCSExporting && (TelemDataRx.Instance.NumPackets == LastDCSExportPacketCount))
                    {
                        DCSActiveAirframe = AirframeTypes.None;
                        _isDCSExporting = false;
                        OnPropertyChanged(nameof(IsDCSExporting));
                    }
                    else if(!_isDCSExporting && (TelemDataRx.Instance.NumPackets != LastDCSExportPacketCount))
                    {
                        _isDCSExporting = true;
                        OnPropertyChanged(nameof(IsDCSExporting));
                    }
                    LastDCSExportPacketCount = TelemDataRx.Instance.NumPackets;
                }
                LastDCSExportCheck = System.DateTimeOffset.Now;
                return _isDCSExporting;
            }
        }

        /// <summary>
        /// reading IsDCSAvailable will implicitly first set the property to the correct current value based on the
        /// state of dcs. this property does not have an explict set handler and generates property change events.
        /// </summary>
        private bool _isDCSAvailable;
        public bool IsDCSAvailable
        {
            get
            {
                bool isAvail = (DCSLuaManager.IsLuaInstalled() && IsDCSRunning && IsDCSExporting);
                if (_isDCSAvailable != isAvail)
                {
                    _isDCSAvailable = isAvail;
                    OnPropertyChanged(nameof(IsDCSAvailable));
                }
                return _isDCSAvailable;
            }
        }

        /// <summary>
        /// returns current upload state for dcs. this property does not have an explict set handler and generates
        /// property change events.
        /// </summary>
        private bool _isDCSUploadInFlight;
        public bool IsDCSUploadInFlight
        {
            get => _isDCSUploadInFlight;
            private set
            {
                if (_isDCSUploadInFlight != value)
                {
                    _isDCSUploadInFlight = value;
                    OnPropertyChanged(nameof(IsDCSUploadInFlight));
                }
            }
        }

        // ---- private properties, read-only

        private readonly Dictionary<string, AirframeTypes> _dcsToJAFDTCTypeMap;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes the singleton application object. this is the first line of authored code executed, and as
        /// such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            IsAppShuttingDown = false;
            IsAppStartupGood = false;
            IsMainWindowBuilt = false;

            // TODO: this likely needs changes to work for packaged applications. see the discussion around
            // TODO: IApplicationActivationManager.ActivateApplication
            //
            CmdLnArgs = ParseCommandLine([.. Environment.GetCommandLineArgs() ]);
            if (FileActivationSetup(CmdLnArgs))
            {
                InitializeComponent();

#if DCS_TELEM_INCLUDES_LAT_LON
                DCSLastLat = 0.0;
                DCSLastLon = 0.0;
#endif

                LastDCSExportCheck = System.DateTimeOffset.Now;
                LastDCSExportPacketCount = 0;
                IncPressed = false;
                DecPressed = false;
                TogglePressed = false;
                UploadPressedTimestamp = 0;
                IncDecPressedTimestamp = 0;
                MarkerUpdateTimestamp = 0;

                _dcsToJAFDTCTypeMap = new Dictionary<string, AirframeTypes>()
                {
                    ["A10C"] = AirframeTypes.A10C,
                    ["AH64D"] = AirframeTypes.AH64D,
                    ["AV8B"] = AirframeTypes.AV8B,
                    ["F14AB"] = AirframeTypes.F14AB,
                    ["F15E"] = AirframeTypes.F15E,
                    ["F16CM"] = AirframeTypes.F16C,
                    ["FA18C"] = AirframeTypes.FA18C,
                    ["M2000C"] = AirframeTypes.M2000C,
                };

                this.UnhandledException += (sender, args) =>
                {
// TODO: what if exception happens before FileManager is preflighted in OnLaunched?
                    FileManager.Log($"App:App Unhandled exception: {args.Exception.Message}\n");
                    FileManager.Log(args.Exception.StackTrace);
                    IsAppShuttingDown = true;
                };
            }
            else
            {
                // NOTE: this is super icky, but we don't really want the app to continue its startup sequence in
                // NOTE: the event we have a file activation as there's a bunch of assumptions baked in that a ui
                // NOTE: is coming up, which is not the case if we've got a file activiation.
                // NOTE:
                // NOTE: throwing an exception here kills the process, now that we've done our thing and handed
                // NOTE: off paths to the activated files to the OG JAFDTC process that is handling bidness.
                //
                throw new Exception("Aborting launch, sent JAFDTC file activation to running instance");
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // file activation hack
        //
        // ------------------------------------------------------------------------------------------------------------

        // HACK: this is a hack to handle file activations on Microsoft.UI.Xaml.Application as that class does not
        // HACK: provide OnFileActivated unlike Windows.UI.Xaml.Application). we create and grab a named mutex so
        // HACK: that launches while another jafdtc instance is running will send their path arg to the running
        // HACK: instance via a named pipe.
        //
        // HACK: a launch without an instance running will create the named pipe and server to listen for incoming
        // HACK: path args from subsequent launches.
        //
        // HACK: gotta be a better way here? have i mentioned lately how much this platform sucks?

        /// <summary>
        /// handle a file activation event for a set of files. this function should be called on the main thread and
        /// will re-launch the file activation server thread if requested.
        /// 
        /// paths parameter has individual paths, a path may be prefixed by "--noui" to supress user interaction.
        /// </summary>
        public void FileActivationHandler(List<string> paths, bool isNoUI, bool isRestart)
        {
            // NOTE: this method should not be called before the universe is mature. note that the server thread
            // NOTE: cannot call us until the ui is up as its invocations rely on event loop dispatch.

            Window.ConfigListPage.FileActivations(paths, isNoUI);

            if (isRestart)
            {
                FileManager.Log($"FileActivationHandler: restarts server thread");
                Thread serverThread = new(this.FileActivationServerThread);
                serverThread.Start();
            }
        }

        /// <summary>
        /// server thread to run under the initial instance of jafdtc. sets up a named pipe to accepts paths
        /// from subsequent app launches that specify files. when data arrives, thread exits after invoking
        /// the handler.
        /// </summary>
        private void FileActivationServerThread()
        {
#if ENABLE_FILE_ACTIVATION

            // NOTE: this thread can be started basically right after the big bang. do not expect fully
            // NOTE: formed galaxies and so forth to exist for a while. need to ensure we don't start
            // NOTE: processing activation requests until things are more mature...

            try
            {
                using NamedPipeServerStream server = new(JAFDTCFileActPipeName);
                server.WaitForConnectionAsync();
                while (!IsAppShuttingDown && (!IsMainWindowBuilt || !server.IsConnected))
                    Thread.Sleep(750);
                if (server.IsConnected)
                {
                    using StreamReader reader = new(server);
                    string filePaths = reader.ReadLine();
                    if (!string.IsNullOrEmpty(filePaths))
                    {
                        // NOTE: at this point, we need to have the ui up as we're about to use the event
                        // NOTE: loop. should be fine as the sleep loop above will not exit until the app
                        // NOTE: startup is noted.

                        List<string> paths = [.. filePaths.Split('|') ];
                        List<string> pathsUI = [];
                        List<string> pathsNI = [];
                        foreach (string path in paths)
                            if (path.StartsWith("--noui "))
                                pathsNI.Add(path["--noui ".Length..]);
                            else
                                pathsUI.Add(path);
                        Window.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                        {
                            FileActivationHandler(pathsNI, true, false);
                            FileActivationHandler(pathsUI, false, true);
                        });
                    }
                    server.Disconnect();
                }
            }
            catch (Exception ex)
            {
                FileManager.Log($"App:FileActivationServerThread exception: {ex.Message}\n{ex.StackTrace}");
            }

#endif // #if ENABLE_FILE_ACTIVATION
        }

        /// <summary>
        /// check to see if the application is already running, if so, and we have a path or --open arg, send
        /// it to the running instance to handle (at that point, our work here will be done). returns true if
        /// an instance of the app is not currently running, false otherwise.
        /// </summary>
        public bool FileActivationSetup(CmdLnArgInfo args)
        {

#if ENABLE_FILE_ACTIVATION

            // NOTE: this method is called basically right after the big bang. do not expect fully formed
            // NOTE: galaxies and so forth to exist for a while. this code should only rely on foundational
            // NOTE: frameworks (e.g., ui event loops and dispatching are not a thing at this point).

            if (MutexJAFDTC.WaitOne(10, false))
            {
                // we are the og jafdtc. we are legion. fear us.
                //
                Thread serverThread = new(this.FileActivationServerThread);
                serverThread.Start();
            }
            else
            {
                // there is already a jafdtc instance running. the og will have started an activation server
                // that we will pass our work (i.e., command line arguments) on to before bouncing. the og
                // instance will then do us a solid and do the work.
                //
                if ((args.ArgPaths.Count > 0) || !string.IsNullOrEmpty(args.ArgValueOpen))
                {
                    using NamedPipeClientStream client = new(JAFDTCFileActPipeName);
                    client.Connect(500);
                    if (client.IsConnected)
                    {
                        string msg = string.Join("|", args.ArgPaths);
                        string sep = (msg.Length > 0) ? "|" : "";
                        if (!string.IsNullOrEmpty(args.ArgValueOpen))
                            msg = $"--noui {args.ArgValueOpen}{sep}{msg}";
                        using StreamWriter writer = new(client);
                        writer.WriteLine(msg);
                        writer.Flush();
                    }
                }
                return false;
            }

#endif // #if ENABLE_FILE_ACTIVATION

            return true;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // command line support
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns string representation of command line, excluding the application path.
        /// </summary>
        private static CmdLnArgInfo ParseCommandLine(List<string> args)
        {
            string summary = "";
            string argValueOpen = null;
            string argValuePack = null;
            List<string> argPath = [ ];

            for (int i = 1; i < args.Count; i++)
            {
                if ((args[i].Equals("--open", StringComparison.CurrentCultureIgnoreCase)) && ((i + 1) < args.Count))
                    argValueOpen = args[++i];
                else if ((args[i].Equals("--pack", StringComparison.CurrentCultureIgnoreCase)) && ((i + 1) < args.Count))
                    argValuePack = args[++i];
                else if (!args[i].StartsWith('-'))
                    argPath.Add(args[i]);
                else
                    return null;
                summary = summary + " " + args[i];
            }
            return new CmdLnArgInfo(summary, argValueOpen, argValuePack, argPath);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // upload button behaviors
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// upload given configuration to dcs. the configuration is only uploaded if dcs is available, the configuration
        /// is valid, and the current dcs airframe matches the airframe of the configuration.
        /// </summary>
        public async void UploadConfigurationToJet(IConfiguration cfg)
        {
            string error = null;
            if (cfg == null)
            {
                error = "No Configuration Selected";
            }
            else if (!IsDCSAvailable || (cfg.Airframe != DCSActiveAirframe))
            {
                error = "DCS or Airframe Unavailable";
            }
            else
            {
                if (Settings.UploadFeedback == UploadFeedbackTypes.AUDIO_PROGRESS)
                    StatusMessageTx.Send("Avionics Setup Starting");
                if (Settings.UploadFeedback != UploadFeedbackTypes.LIGHTS)
                {
                    Window.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                    {
                        General.PlayAudio("ux_action.wav");
                    });
                }
                FileManager.Log($"Upload triggered, {cfg.Name}");
                if (!await cfg.UploadAgent.Load())
                    error = $"Upload Failed for {cfg.Name}";
            }
            if (error != null)
            {
                Window.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    FileManager.Log($"Configuration upload reports error: {error}");
                    StatusMessageTx.Send(error);
                    General.PlayAudio("ux_error.wav");
                });
            }
        }

        /// <summary>
        /// open dcs dtc editor.
        /// </summary>
        private async static void OpenDCSDTCEditor(IConfiguration cfg)
        {
            if (cfg != null)
                await cfg.UploadAgent.OpenDCSDTCEditor();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // export data processing
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// process query replies from the dcs event stream. parties interested in the reply to a query will subscribe
        /// to DCSQueryResponseReceived events. event handlers are handled on the current thread and should not use ui.
        /// event handlers are cleared after each response.
        /// </summary>
        private void ProcessQueryResponse(TelemDataRx.TelemData data)
        {
            if (!string.IsNullOrEmpty(data.Response) &&
                (DCSQueryResponseReceived != null) &&
                (DCSQueryResponseReceived.GetInvocationList().Length > 0))
            {
                DCSQueryResponseReceived?.Invoke(this, data.Response);
                DCSQueryResponseReceived = null;
            }
        }

        /// <summary>
        /// process markers from the dcs event stream. play a sound to indicate uploading has started or ended based
        /// on the marker. actions are carried out on the main thread via dispatch.
        /// </summary>
        private void ProcessMarker(TelemDataRx.TelemData data)
        {
            if (!IsDCSUploadInFlight && !string.IsNullOrEmpty(data.Marker))
            {
                IsDCSUploadInFlight = true;
                MarkerUpdateTimestamp = 0;
                FileManager.Log($"Upload starts, marker '{data.Marker}'");
            }
            else if (IsDCSUploadInFlight && data.Marker.StartsWith("ERROR: "))
            {
                IsDCSUploadInFlight = false;
                StatusMessageTx.Send(data.Marker["ERROR: ".Length..]);
                Window.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    General.PlayAudio("ux_error.wav");
                });
                FileManager.Log($"Upload fails, reporting '{data.Marker}'");
            }
            else if (IsDCSUploadInFlight &&
                     !string.IsNullOrEmpty(data.Marker) &&
                     (Settings.UploadFeedback == UploadFeedbackTypes.AUDIO_PROGRESS))
            {
                TimeSpan dt = new(DateTime.Now.Ticks - MarkerUpdateTimestamp);
                if (dt.TotalMilliseconds > 1000)
                {
                    StatusMessageTx.Send($"Setup {data.Marker}% Complete");
                    MarkerUpdateTimestamp = DateTime.Now.Ticks;
                }
            }
            else if (IsDCSUploadInFlight && string.IsNullOrEmpty(data.Marker))
            {
                IsDCSUploadInFlight = false;
                if ((Settings.UploadFeedback == UploadFeedbackTypes.AUDIO_DONE) ||
                    (Settings.UploadFeedback == UploadFeedbackTypes.AUDIO_PROGRESS))
                {
                    StatusMessageTx.Send("Avionics Setup Complete");
                }
                if (Settings.UploadFeedback != UploadFeedbackTypes.LIGHTS)
                {
                    Window.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
                    {
                        General.PlayAudio("ux_action.wav");
                        await Task.Delay(100);
                        General.PlayAudio("ux_action.wav");
                    });
                }
                FileManager.Log($"Upload completes");
            }
        }

        /// <summary>
        /// process upload commands from the dcs event stream. triggers a configuration upload once the upload button
        /// has been pressed for the specified amount of time.
        /// </summary>
        private void ProcessUploadCommand(TelemDataRx.TelemData data)
        {
            if (IsDCSUploadInFlight)
            {
                UploadPressedTimestamp = 0;
            }
            else
            {
                if ((data.CmdUpload == "1") && (UploadPressedTimestamp == 0))
                {
                    UploadPressedTimestamp = DateTime.Now.Ticks;
                }
                else if ((data.CmdUpload == "0") && (UploadPressedTimestamp != 0))
                {
                    TimeSpan dt = new(DateTime.Now.Ticks - UploadPressedTimestamp);
                    Window.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                    {
                        if (dt.TotalMilliseconds > 1000)
                            OpenDCSDTCEditor(CurrentConfig);
                        else if (dt.TotalMilliseconds > 200)
                            UploadConfigurationToJet(CurrentConfig);
                    });
                    UploadPressedTimestamp = 0;
                }
            }
        }

        /// <summary>
        /// process the pin/unpin commands from the dcs event stream. these change the order of the window stack
        /// to keep jafdtc always on top or allow it to be lowered into the background.
        /// </summary>
        private void ProcessWindowStackCommand(TelemDataRx.TelemData data)
        {
            bool isUpdateWindowLayer = false;
            if (!TogglePressed && (data.CmdToggle == "1"))
            {
                IsJAFDTCPinnedToTop = !IsJAFDTCPinnedToTop;
                TogglePressed = true;
                isUpdateWindowLayer = true;
            }
            else if (TogglePressed && (data.CmdToggle == "0"))
            {
                TogglePressed = false;
            }
            else if (!IsJAFDTCPinnedToTop && (data.CmdShow == "1"))
            {
                IsJAFDTCPinnedToTop = true;
                isUpdateWindowLayer = true;
            }
            else if (IsJAFDTCPinnedToTop && (data.CmdHide == "1"))
            {
                IsJAFDTCPinnedToTop = false;
                isUpdateWindowLayer = true;
            }
            if (isUpdateWindowLayer)
            {
                Window?.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    if (IsJAFDTCPinnedToTop)
                    {
                        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(Window);
                        ShowWindow(hWnd, WindowShowStyle.SW_NORMAL);
                        Window.ConfigListPage.RebuildInterfaceState();
                    }
                    (Window.AppWindow.Presenter as OverlappedPresenter).IsAlwaysOnTop = IsJAFDTCPinnedToTop;
                    if (!IsJAFDTCPinnedToTop)
                    {
                        // TODO: reset cockpit "always on top" control to "not always on top" (eg, FLIR GAIN/LVL/AUTO in viper)?
                        Window.AppWindow.MoveInZOrderAtBottom();
                        Process[] arrProcesses = Process.GetProcessesByName("DCS");
                        if (arrProcesses.Length > 0)
                        {
                            IntPtr ipHwnd = arrProcesses[0].MainWindowHandle;
                            Thread.Sleep(100);
                            SetForegroundWindow(ipHwnd);
                        }
                    }
                });
            }
        }

        /// <summary>
        /// process the increment and decrement commands from the dcs event stream. these change the currently selected
        /// configuration and (optionally) inform the user of the new configuration.
        /// </summary>
        private void ProcessIncrDecrCommands(TelemDataRx.TelemData data)
        {
            long curTicks = DateTime.Now.Ticks;
            System.TimeSpan timeSpan = new(curTicks - IncDecPressedTimestamp);

            if (!IncPressed && (data.CmdIncr == "1"))
            {
                IncPressed = true;
            }
            else if (IncPressed && (data.CmdIncr == "0"))
            {
                IncDecPressedTimestamp = curTicks;
                IncPressed = false;
                Window?.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    Window.ConfigListPage?.PreviousConfiguration((timeSpan.TotalMilliseconds > 4000));
                });
            }

            if (!DecPressed && (data.CmdDecr == "1"))
            {
                DecPressed = true;
            }
            else if (DecPressed && (data.CmdDecr == "0"))
            {
                IncDecPressedTimestamp = curTicks;
                DecPressed = false;
                Window?.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    Window.ConfigListPage?.NextConfiguration((timeSpan.TotalMilliseconds > 4000));
                });
            }
        }

        /// <summary>
        /// handle an inbound telemetry data packet from dcs. this packet provides information on which airframe is
        /// curerently active, state of the dtc, and cockpit control state that we use to trigger dtc actions.
        /// </summary>
        private void TelemDataReceiver_DataReceived(TelemDataRx.TelemData data)
        {
            if (Window != null)
            {
                DCSActiveAirframe = (_dcsToJAFDTCTypeMap.TryGetValue(data.Model, out AirframeTypes value))
                                        ? value : AirframeTypes.None;

#if DCS_TELEM_INCLUDES_LAT_LON
                DCSLastLat = (double.TryParse(data.Lat, out double lat)) ? lat : 0.0;
                DCSLastLon = (double.TryParse(data.Lat, out double lon)) ? lon : 0.0;
#endif

                ProcessQueryResponse(data);
                ProcessMarker(data);
                ProcessUploadCommand(data);
                ProcessWindowStackCommand(data);
                ProcessIncrDecrCommands(data);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// application launched: preflight the file manager and settings, initialize the "dcs available" timer,
        /// create the main window, set it up, and activate it to get this show on the road.
        /// </summary>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // NOTE: we should not get here if an og instance of jafdtc is already running; if we are here, we are
            // NOTE: the og. reaching here as a non-og instance is prohibited by the "aborting launch" exception
            // NOTE: above that triggers during application construction after we have (1) realized we're not the
            // NOTE: og instance, and (2) handed off activations to the og instance.

            try
            {
                FileManager.Preflight();
                Settings.Preflight();

                FileManager.Log($"Command line: jafdtc.exe {CmdLnArgs.Summary}");
                if (!string.IsNullOrEmpty(CmdLnArgs.ArgValueOpen))
                    FileManager.Log($"  --open value: {CmdLnArgs.ArgValueOpen}");
                if (!string.IsNullOrEmpty(CmdLnArgs.ArgValuePack))
                    FileManager.Log($"  --pack value: {CmdLnArgs.ArgValuePack}");
                foreach (string path in CmdLnArgs.ArgPaths)
                    FileManager.Log($"  [path] value: {path}");

                // since --open has no user interaction by definition, we can take care of that argument here
                // even though we are still a ways away from having the ui up.
                //
                // for any --pack and [path] args, we will wait until MainWindow.AppContentFrame_Loaded to process
                // the args to allow additional initialization to finish up as these operations may require ui.
                //
                if (!string.IsNullOrEmpty(CmdLnArgs.ArgValueOpen))
                {
                    FileManager.Log($"Opening unmanaged config file: {CmdLnArgs.ArgValueOpen}");
                    IConfiguration config = ConfigExchangeUIHelper.ConfigSilentImportJAFDTC(CmdLnArgs.ArgValueOpen);
                    Settings.LastConfigFilenameSelection = config.Filename;
                }

                IsJAFDTCPinnedToTop = Settings.IsAlwaysOnTop;
                IsDCSUploadInFlight = false;

                TelemDataRx.Instance.TelemDataReceived += TelemDataReceiver_DataReceived;
                TelemDataRx.Instance.Start();

                WyptCaptureDataRx.Instance.Start();

                CheckDCSTimer = new DispatcherTimer();
                CheckDCSTimer.Tick += CheckDCSTimer_Tick;
                CheckDCSTimer.Interval = new System.TimeSpan(0, 0, 10);

                IsAppStartupGood = true;
            }
            catch (System.Exception ex)
            {
// TODO: what if FileManager doesn't pass preflight?
                FileManager.Log($"App:App exception: {ex.Message}\n{ex.StackTrace}");
            }

            Window = new MainWindow();
            Window.Activated += MainWindow_Activated;
            Window.Closed += MainWindow_Closed;
            Window.Activate();

            // window activation will start the process of filling in the ui and wrapping up the last mile. our
            // last touchpoint is in MainWindow.AppContentFrame_Loaded where we do the final splash activities
            // and handle final command line args before turning things over to the main ui.

            IsMainWindowBuilt = true;
        }

        /// <summary>
        /// check dcs timer ticks: update IsDCSAvailable state based on lua installation, dcs process running, and
        /// indications dcs export is running. force regeneration of dcs state.
        /// </summary>
        private void CheckDCSTimer_Tick(object sender, object args)
        {
            _ = IsDCSAvailable;                     // invoke accessor to rebuild state, return value ignored
        }

        /// <summary>
        /// window activated: when a window is activated or deactivated, start or stop (respectively) the dcs state
        /// check timer to monitor that state for the rest of the ui. force regeneration of dcs state.
        /// </summary>
        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                CheckDCSTimer?.Stop();
            }
            else
            {
                _ = IsDCSAvailable;                 // invoke accessor to rebuild state, return value ignored
                CheckDCSTimer?.Start();
            }
        }

        /// <summary>
        /// window closed: flag the app as shutting down so interested parties can take appropriate action.
        /// </summary>
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            IsAppShuttingDown = true;
        }
    }
}
