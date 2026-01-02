namespace JAFDTC.Models.Planning
{
    public class Ownship
    {
        public required string Name { get; set; }
        public string? STN { get; set; }
        public string? Board { get; set; }
        public int? Tacan { get; set; }
        public char? TacanBand { get; set; }
        public int? Joker { get; set; }
        public int? Lase { get; set; }
        public IReadOnlyDictionary<int, int>? CommPresets { get; set; }
    }
}
