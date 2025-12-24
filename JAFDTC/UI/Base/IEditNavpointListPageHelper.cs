// ********************************************************************************************************************
//
// IEditNavpointListHelper.cs : interface for EditNavPointListPage helper classes
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

using JAFDTC.Models;
using JAFDTC.Models.Base;
using JAFDTC.Models.POI;
using JAFDTC.UI.Controls.Map;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using static JAFDTC.Utilities.Networking.WyptCaptureDataRx;

namespace JAFDTC.UI.Base
{
    /// <summary>
    /// interface for the EditNavPointListPage ui page helper class responsible for specializing the
    /// EditNavPointListPage base behavior for a specific airframe.
    /// </summary>
    public interface IEditNavpointListPageHelper
    {
        /// <summary>
        /// return the navpoint system from a configuration.
        /// </summary>
        public INavpointSystemImport NavptSystem(IConfiguration config);

        /// <summary>
        /// return an object defining key navpoint system parameters.
        /// </summary>
        public NavpointSystemInfo SystemInfo { get; }

        /// <summary>
        /// return the type of the class for the editor interface page to use to edit a navpoint.
        /// </summary>
        public Type NavptEditorType { get; }

        /// <summary>
        /// return an object to use as the argument to the navpoint editor. this object is passed in through the
        /// Parameter of a navigation operation to a page of type NavptEditorType.
        /// </summary>
        public object NavptEditorArg(Page parentEditor, IMapControlVerbMirror verbMirror, IConfiguration config,
                                     int indexNavpt);

        /// <summary>
        /// set up the user interface for the navpoint list editor. this method is called at OnNavigatedTo. 
        /// </summary>
        public void SetupUserInterface(IConfiguration config, ListView listView);

        /// <summary>
        /// update the edit navpoints from the configuration navpoints. the update will perform a deep copy of
        /// the navpoints from the configuration.
        /// </summary>
        public void CopyConfigToEdit(IConfiguration config, ObservableCollection<INavpointInfo> edit);

        /// <summary>
        /// update the configuration navpoints from the edit navpoints. the update will perform a deep copy of
        /// the navpoints from the configuration.
        /// </summary>
        public bool CopyEditToConfig(ObservableCollection<INavpointInfo> edit, IConfiguration config);

        /// <summary>
        /// reset the navpoint system to its default state.
        /// </summary>
        public void ResetSystem(IConfiguration config);

        /// <summary>
        /// add a navigation point to the navpoint list in the configuration at the indicated position (default is
        /// end of list). this updates (but does not save) the configuration. returns index of added navpoint.
        /// </summary>
        public int AddNavpoint(IConfiguration config, int atIndex = -1);

        /// <summary>
        /// add navpoints from a list of pois to the end of the navpoint list in the configuration. this updates
        /// (but does not save) the configuration. 
        /// </summary>
        public void AddNavpointsFromPOIs(IEnumerable<PointOfInterest> pois, IConfiguration config);

        /// <summary>
        /// paste navpoints from the clipboard data. pasted navpoints can replace or append to the current navpoint
        /// list in the system.
        /// </summary>
        public bool PasteNavpoints(IConfiguration config, string cbData, bool isReplace = false);

        /// <summary>
        /// capture navpoints from dcs using the dcs ui.
        /// </summary>
        public void CaptureNavpoints(IConfiguration config, WyptCaptureData[] wypts, int startIndex);
    }
}
