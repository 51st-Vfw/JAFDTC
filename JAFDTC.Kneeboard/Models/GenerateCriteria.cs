using System;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Kneeboard.Models
{
    public class GenerateCriteria
    {
        public required string PathOutput { get; set; } //should come from settings for dcs path
        public required string Airframe { get; set; } //from jaf profile so we know what complete path to write to
        public required string Theater { get; set; } //for kb header info + date
        public required string Name { get; set; } //the jaf profile name for headers and cleaned filename prefix per kb
        public required string PathLogo { get; set; } //from settings for squad or wing logo
        public required string[] Flights { get; set; } //from jaf profile, from global models.. flight into todo
        public required string[] Comms { get; set; } //from jaf profile, from global models, also  uhf/vhf, preset num, freq, name (inclue guard).. flight specific? ie primary victors, etc?
        public required string[] Steerpoints { get; set; } //from jaf profile, from global models... with theater should drive what map to load and "in-mem screenshot", ip/tgt drive zoomed in map
                //if STP is near a POI, global marker, or imported ground unit.. should have that info as well!
                //if stp is an airbase or FARP... extract to KB about airfields/landing/takeoff/alt, prefix, ground, tower, atis comms if available from theater...

        public string[] Units { get;set; } //if available from theater, import miz/cf/acmi.... drive more info on Map, threat rings, labels, etc
    }
}
