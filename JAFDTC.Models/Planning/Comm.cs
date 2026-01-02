namespace JAFDTC.Models.Planning
{
    public class Comm
    {
        public required int CommId { get; set; }
        public required double Frequency { get; set; }
        public string? Name { get; set; }
        public string? Modulation { get; set; }
        public string? Description { get; set; }
    }
}
