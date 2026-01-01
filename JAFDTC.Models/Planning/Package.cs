using System;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Models.Planning
{
    public class Package
    {
        public required string Name { get; set; }

        public required IReadOnlyList<Flight> Flights { get; set; }
    }
}
