namespace JAFDTC.Models.Units
{
    public class UnitItem
    {
        public required string UniqueID { get; set; }                       // unique identifier for unit
                                                                            // TODO: make this an enum? need some mapping from string to enum for ui, possibly
        public required string Type { get; set; }                           // unit type
        public required string Name { get; set; }                           // unit name
        public required UnitPositionItem Position { get; set; }             // unit position
        public required bool IsAlive { get; set; }                          // t => alive
    }
}
