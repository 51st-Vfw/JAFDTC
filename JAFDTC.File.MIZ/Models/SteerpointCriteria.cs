using JAFDTC.Models.Planning;

namespace JAFDTC.File.MIZ.Models
{
    public class SteerpointCriteria
    {
        public required Mission Mission { get; set; }
        public required string PathOutput { get; set; }
        public required string Name { get; set; } //jafdtc name of the current item...
    }
}
