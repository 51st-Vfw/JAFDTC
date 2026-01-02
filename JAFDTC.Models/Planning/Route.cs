namespace JAFDTC.Models.Planning
{
    public class Route
    {
        public required string Name { get; set; }
        public required IReadOnlyList<Navpoint> NavPoints { get; set; }
    }
}
