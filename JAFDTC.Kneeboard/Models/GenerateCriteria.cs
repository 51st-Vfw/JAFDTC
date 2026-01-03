using JAFDTC.Models.Planning;

namespace JAFDTC.Kneeboard.Models
{
    public class GenerateCriteria
    {
        public required string PathTemplates { get; set; } //should come from settings for general root template folder
        public required string PathOutput { get; set; } //should come from settings for dcs saved games path
        public required string Name { get; set; } //the jaf profile name
        public required Mission Mission { get; set; } //set of what we are planning
        public bool? NightMode { get; set; } //tint option.. or just always gen both?
        //public string? PathLogo { get; set; } //from settings for squad or wing logo
    }
}