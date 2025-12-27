using System;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Models.Radios
{
    public class Radio
    {
        public required string Name { get; set; } //arc-210 etc
        public required int CommMode {  get; set; } //comm 1, 2, 3, 4 etc
        public required double FrequencyMin {  get; set; }
        public required double FrequencyMax {  get; set; }
        public required string FrequencyName {  get; set; } //uhf, vhf, intercom, etc

        public int? Preset { get; set; }
        public bool? MonitorGuard { get; set; } //false off, true on, null not supported
        public List<Channel> Channels { get; set; }

    }
}
