using System;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Models.Planning
{
    public class Nav
    {
        public required double Latitude { get; set; }
        public required double Longitude { get; set; }
        public required int Altitude { get; set; }
        public string? Name { get; set; }
        public double? Speed { get; set; }
        public string? TOT { get; set; }
        public string? TOS { get; set; }
    }
}
