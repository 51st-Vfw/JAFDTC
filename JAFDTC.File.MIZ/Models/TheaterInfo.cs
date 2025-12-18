namespace JAFDTC.File.MIZ.Models
{
    /// <summary>
    /// theater information including computed offsets to transform between X/Z and Lat/Lon coordinates. 
    /// </summary>
    public class TheaterInfo
    {
        public double Dx;           // northing delta for dcs x to utm northing
        public double Dz;           // easting delta for dcs z to utm easting
        public int Zone;
        public bool IsSouthHemi;

        public TheaterInfo(double dx, double dz, int zone, bool isSouthHemi)
        {
            Dx = dx;
            Dz = dz;
            Zone = zone;
            IsSouthHemi = isSouthHemi;
        }
    }
}
