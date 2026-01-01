using System;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Models.Planning
{
    public class Comm
    {
        public required int CommId { get; set; }
        public required double Frequency { get; set; }
        public string? Description { get; set; }
    }
}
