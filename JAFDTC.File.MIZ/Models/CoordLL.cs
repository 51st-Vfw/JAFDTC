namespace JAFDTC.File.MIZ.Models
{
    /// <summary>
    /// latitude/longitude surface coordinate (does not contain altitude/elevation information). coordiantes
    /// are in decimal degrees or radians depending on api.
    /// </summary>
    internal class CoordLL
    {
        public double Lat;          // northing
        public double Lon;          // easting
    }
}
