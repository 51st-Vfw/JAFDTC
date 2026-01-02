namespace JAFDTC.Models.Planning
{
    public class Threat
    {
        public required string Name { get; set; }
        public required string Type { get; set; }
        public required double Latitude { get; set; }
        public required double Longitude { get; set; }
        public required int Altitude { get; set; }
        public double? WEZ { get; set; }
    }
}
