namespace JAFDTC.TacView.Models
{
    public class UnitItem
    {
        public required string Id { get; set; } 
        public required PositionItem Position { get; set; }
        public required CoalitionType Coalition { get; set; }
        public required CategoryType Category { get; set; }
        public required ColorType Color { get; set; }
        public required UnitType Unit { get; set; }
        public required string GroupName { get; set; }
        public required string UnitName { get; set; }
        public required bool IsAlive { get; set; }
        public string? DebugInfo { get; set; }
        public string? DebugInfo2 { get; set; }
    }
}
