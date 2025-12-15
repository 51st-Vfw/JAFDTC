using JAFDTC.Models.Core;
using JAFDTC.Models.Units;

namespace JAFDTC.File.Models
{
    public class ExtractCriteria
    {
        public required string FilePath { get; set; }
        public string Theater { get; set; }
        public CoalitionType[]? Coalitions { get; set; }
        public UnitCategoryType[]? UnitCategories { get; set; }
        public string[]? UnitTypes { get; set; } // TODO: move this to an enum?
        public bool? IsAlive { get; set; }
        public DateTimeOffset? TimeSnippet { get; set; }
    }
}
