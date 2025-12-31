// ********************************************************************************************************************
//
// IMapControlVerbMirror.cs : interfaces for a map window control verb mirror
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

namespace JAFDTC.UI.Controls.Map
{
    /// <summary>
    /// defines the interface for a mirror object that is responsible for distributing calls to map "verbs" to
    /// various interested objects. this allows for coordination on edits.
    /// </summary>
    public interface IMapControlVerbMirror
    {
        /// <summary>
        /// registers a map control verb observer with the mirror.
        /// </summary>
        public void RegisterMapControlVerbObserver(IMapControlVerbHandler observer);

        /// <summary>
        /// mirror marker selected across all registered observers except for the sender.
        /// </summary>
        public void MirrorVerbMarkerSelected(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0);

        /// <summary>
        /// mirror marker opened across all registered observers except for the sender.
        /// </summary>
        public void MirrorVerbMarkerOpened(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0);

        /// <summary>
        /// mirror marker moved across all registered observers except for the sender.
        /// </summary>
        public void MirrorVerbMarkerMoved(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0);

        /// <summary>
        /// mirror marker added across all registered observers except for the sender.
        /// </summary>
        public void MirrorVerbMarkerAdded(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0);

        /// <summary>
        /// mirror marker deleted across all registered observers except for the sender.
        /// </summary>
        public void MirrorVerbMarkerDeleted(IMapControlVerbHandler sender, MapMarkerInfo info, int param = 0);
    }
}
