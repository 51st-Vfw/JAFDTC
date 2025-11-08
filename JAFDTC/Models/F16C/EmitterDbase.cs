// ********************************************************************************************************************
//
// EmitterDbase.cs -- emitter dabase
//
// Copyright(C) 2021-2023 the-paid-actor & others
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

using JAFDTC.Utilities;
using System;
using System.Collections.Generic;

namespace JAFDTC.Models.F16C
{
    /// <summary>
    /// database of information on emitters for the f-16c viper. this database is a singleton and includes names,
    /// hts threat classes, rwr information, and alic table codes.
    /// </summary>
    internal class EmitterDbase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private static readonly Lazy<EmitterDbase> lazy = new(() => new EmitterDbase());
        public static EmitterDbase Instance { get => lazy.Value; }

        private Dictionary<int, List<Emitter>> Dbase { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        private EmitterDbase()
        {
            List<Emitter> emitters = FileManager.LoadEmitters();

            Dbase = [ ];
            foreach (Emitter emitter in emitters)
            {
                if (!Dbase.TryGetValue(emitter.ALICCode, out List<Emitter> value))
                {
                    value = [ ];
                    Dbase[emitter.ALICCode] = value;
                }
                value.Add(emitter);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return a list of emitters matching an alic code (-1 implies all emitters).
        /// </summary>
        public List<Emitter> Find(int alicCode = -1)
        {
            List<Emitter> list;
            if (alicCode < 0)
            {
                list = [ ];
                foreach (List<Emitter> emitterList in Dbase.Values)
                    list = [.. list, .. emitterList ];
            }
            else
            {
                list = Dbase.TryGetValue(alicCode, out List<Emitter> value) ? value : [ ];
            }
            return list;
        }
    }
}
