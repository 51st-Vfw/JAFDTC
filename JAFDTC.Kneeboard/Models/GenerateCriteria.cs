namespace JAFDTC.Kneeboard.Models
{
    public class GenerateCriteria
    {
        public required string PathTemplates { get; set; } //should come from settings for airframe specific templates with semantic naming
        public required string PathOutput { get; set; } //should come from settings for dcs path
        public required string Airframe { get; set; } //from jaf profile so we know what complete path to write to
        public required string Name { get; set; } //the jaf profile name for headers and cleaned filename prefix per kb

        public string Theater { get; set; } //for kb header info + date
        public bool NightMode { get; set; } //tint option.. or just always gen both?
        public string PathLogo { get; set; } //from settings for squad or wing logo
        public string[] Flights { get; set; } //from jaf profile, from global models.. flight into todo
        public string[] Comms { get; set; } //from jaf profile, from global models, also  uhf/vhf, preset num, freq, name (inclue guard).. flight specific? ie primary victors, etc?
        public string[] Steerpoints { get; set; } //from jaf profile, from global models... with theater should drive what map to load and "in-mem screenshot", ip/tgt drive zoomed in map
                //if STP is near a POI, global marker, or imported ground unit.. should have that info as well!
                //if stp is an airbase or FARP... extract to KB about airfields/landing/takeoff/alt, prefix, ground, tower, atis comms if available from theater...

        public string[] Units { get;set; } //if available from theater, import miz/cf/acmi.... drive more info on Map, threat rings, labels, etc
    }
}
