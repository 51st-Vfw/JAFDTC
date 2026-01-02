namespace JAFDTC.Models.Planning
{
    public class Flight
    {
        public required string Name { get; set; }        
        public required string Aircraft { get; set; }

        public required IReadOnlyList<Pilot> Pilots { get; set; }
        public IReadOnlyList<Comm>? Comms { get; set; }
        public IReadOnlyList<Route>? Routes { get; set; }

    }
}
