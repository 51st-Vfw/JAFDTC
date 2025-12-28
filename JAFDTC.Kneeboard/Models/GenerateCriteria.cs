using JAFDTC.Models.Pilots;
using JAFDTC.Models.Radios;
using JAFDTC.Models.Units;

namespace JAFDTC.Kneeboard.Models
{
    public class GenerateCriteria
    {
        public required string PathTemplates { get; set; } //should come from settings for airframe specific templates with semantic naming
        public required string PathOutput { get; set; } //should come from settings for dcs path
        public required string Airframe { get; set; } //from jaf profile so we know what complete path to write to
        public required string Name { get; set; } //the jaf profile name for headers and cleaned filename prefix per kb
        public required string Owner { get; set; } //the jaf user.. ie 51_Raven.. should be the name, stn/tndl info...

        public string? Theater { get; set; } //for kb header info + date
        public bool? NightMode { get; set; } //tint option.. or just always gen both?
        public string? PathLogo { get; set; } //from settings for squad or wing logo
        public UnitGroupItem Flight { get; set; }
        public IReadOnlyList<Pilot> Pilots { get; set; } //this needs to align with Flight Unit order...
        public IReadOnlyList<Radio> Comms { get; set; } //from jaf profile, from global models, also  uhf/vhf, preset num, freq, name (inclue guard).. flight specific? ie primary victors, etc?
        public string[] POIs { get; set; } //from jaf DB, global models.. pois from DB as well as miz/cf/acmi import .. (this is maybe JUST airfields and farps?)
        public IReadOnlyList<UnitGroupItem> UnitGroups { get; set; } //if available from import miz/cf/acmi.... ground units (maybe statics?) drive more info on Map, threat rings, labels, etc 
    }
}