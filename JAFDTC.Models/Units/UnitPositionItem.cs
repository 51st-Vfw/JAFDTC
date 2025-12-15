using System.Text.Json.Serialization;

namespace JAFDTC.Models.Units
{
    public class UnitPositionItem
    {
        public string Name { get; set; }                                    // position name (optional)
        public required double Latitude { get; set; }                       // decimal degrees
        public required double Longitude { get; set; }                      // decimal degrees
        public double Altitude { get; set; }                                // feet
        public double TimeOn { get; set; }                                  // s in day (local), < 0 => no time on

        [JsonIgnore]
        public string TimeOnAsHMS
            => (TimeOn >= 0.0) ? string.Format("{0:00}:{1:00}:{2:00}",
                                               (((int)TimeOn % 86400) / 3600),
                                               (((int)TimeOn % 86400) / 60) % 60,
                                               (((int)TimeOn % 86400) % 60))
                               : string.Empty;
    }
}
