// ********************************************************************************************************************
//
// MapFilterSpec.cs -- map element filter specification
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

using System.Text.Json.Serialization;

namespace JAFDTC.Models.CoreApp
{
    /// <summary>
    /// captures parameters for the filters that can be applied to elements in the map window. this allows various
    /// combinations of routes and paths to be shown/hidden by the user interface.
    /// </summary>
    public sealed class MapFilterSpec
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // types & enums
        //
        // ------------------------------------------------------------------------------------------------------------

        public enum ImportFilter                                // values must match item index in ui
        {
            NONE = 0,
            OPPOSING = 1,
            ALL = 2
        };

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public ImportFilter ShowUnits { get; set; }
        public ImportFilter ShowThreatRings { get; set; }
        public bool ShowNavRoutes { get; set; }
        public bool ShowPOIDCS { get; set; }
        public bool ShowPOIUsr { get; set; }
        public bool ShowPOICamp { get; set; }
        public string ShowCampaign { get; set; }                // null => no campaigns, "" => all campaigns, else name

        // ---- constructed properties

        [JsonIgnore]
        public bool IsDefault => ((ShowUnits == ImportFilter.ALL) &&
                                  (ShowThreatRings == ImportFilter.ALL) &&
                                  (!string.IsNullOrEmpty(ShowCampaign) && ShowCampaign.Equals("*")) &&
                                  ShowNavRoutes && ShowPOIDCS && ShowPOIUsr && ShowPOICamp);

        [JsonIgnore]
        public static MapFilterSpec Default => new();

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MapFilterSpec()
            => (ShowUnits, ShowThreatRings, ShowCampaign, ShowPOIDCS, ShowPOIUsr, ShowPOICamp, ShowNavRoutes)
                = (ImportFilter.ALL, ImportFilter.ALL, "*", true, true, true, true);

        public MapFilterSpec(ImportFilter showUnits, ImportFilter showThreatRings, string showCampaign, bool showPOIDCS,
                             bool showPOIUsr, bool showPOICamp, bool showNavRoutes)
            => (ShowUnits, ShowThreatRings, ShowCampaign, ShowPOIDCS, ShowPOIUsr, ShowPOICamp, ShowNavRoutes)
                = (showUnits, showThreatRings, showCampaign, showPOIDCS, showPOIUsr, showPOICamp, showNavRoutes);
    }
}
