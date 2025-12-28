using System;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Models.Pilots
{
    public class Pilot
    {
        public required string Name { get; set; }
        public required string TNDL { get; set; }
        public required bool IsTDOA { get; set; }
        public required bool IsLead { get; set; }

        //public required string Rank { get; set; }
        //public required string Squadron { get; set; }
        //public required string Wing { get; set; }

        public string? Callsign { get; set; }
        public int? Tacan { get; set; }
        public char? TacanBand { get; set; }
        public int? Joker { get; set; }
        public int? LaseCode { get; set; }
    }
}
