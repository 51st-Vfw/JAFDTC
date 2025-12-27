using System;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Models.Radios
{
    public class Channel
    {
        public required int ChannelId { get; set; }
        public required double Frequency { get; set; }
        public string? Description { get; set; }

    }
}
