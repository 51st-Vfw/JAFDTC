using JAFDTC.Models.Core;

namespace JAFDTC.Models.Units
{
    public class UnitGroupItem
    {
        public required string UniqueID { get; set; }                       // unique identifier for group
        public required CoalitionType Coalition { get; set; }               // coalition of group
        public required UnitCategoryType Category { get; set; }             // category of units in group
        public required string Name { get; set; }                           // group name
        public required List<UnitItem> Units { get; set; }         // list of units in group
        public required List<UnitPositionItem> Route { get; set; } // list of positions along route
    }
}
