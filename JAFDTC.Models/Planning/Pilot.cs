using System;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Models.Planning
{
    public class Pilot
    {
        public required string Name { get; set; }
        public required bool IsLead { get; set; }
        public string? STN { get; set; }
        public string? Board { get; set; }
        public int? Tacan { get; set; }
        public char? TacanBand { get; set; }
        public int? Joker { get; set; }
        public int? LaseCode { get; set; }
    }
}
