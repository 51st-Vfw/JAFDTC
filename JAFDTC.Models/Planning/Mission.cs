using System;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Models.Planning
{
    public class Mission
    {
        public required string Name { get; set; }
        public required string Theater { get; set; }
        public required IReadOnlyList<Package> Packages { get; set; }
        public IReadOnlyList<Threat>? Threats { get; set; }
    }
}
