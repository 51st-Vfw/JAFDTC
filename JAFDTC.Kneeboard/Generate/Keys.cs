namespace JAFDTC.Kneeboard.Generate
{
    public static class Keys
    {
        public const string HEADER = "HEADER";
        public const string FOOTER = "FOOTER";
        public const string THEATER = "THEATER";
        public const string NAME = "NAME";
        public const string NIGHTMODE = "NIGHTMODE";
        public const string LOGO = "LOGO";

        public const string PACKAGE_NAME = "PACKAGE_*_NAME";

        //in future should be package prefix...based
        public const string FLIGHT_NAME = "FLIGHT_*_NAME";
        public const string FLIGHT_AIRCRAFT = "FLIGHT_*_AIRCRAFT";

        //in future should be package/flight prefix...based
        public const string PILOT_NAME = "PILOT_*_NAME";
        public const string PILOT_CALLSIGN = "PILOT_*_CALLSIGN";
        public const string PILOT_DATAID = "PILOT_*_DATAID";
        public const string PILOT_SCL = "PILOT_*_SCL";

        //since we only are supporting 1 flight right now.. let all pilots, nav points, and comms tied to that first flight...

        //public const string COMM_NUM = "COMM_*_NUM"; //may want to list radio number and name...
        //public const string COMM_NAME = "COMM_*_NAME";
        public const string COMM_PREFIX = "COMM_*_";

        //COMM PREFIX + this
        public const string COMM_NUM = "PRESET_*_NUM";
        public const string COMM_FREQ = "PRESET_*_FREQ";
        public const string COMM_DESC = "PRESET_*_DESC";
        public const string COMM_MOD = "PRESET_*_MOD";

        public const string ROUTE_NUM = "ROUTE_*_NUM";
        public const string ROUTE_NAME = "ROUTE_*_NAME";
        public const string ROUTE_PREFIX = "ROUTE_*_";

        //ROUTE PREFIX + this
        public const string NAV_NUM = "NAV_*_NUM";
        public const string NAV_NAME = "NAV_*_NAME";
        public const string NAV_NOTE = "NAV_*_NOTE";
        public const string NAV_ALT = "NAV_*_ALT";
        public const string NAV_TOS = "NAV_*_TOS";
        public const string NAV_TOT = "NAV_*_TOT";
        public const string NAV_SPEED = "NAV_*_SPEED";
        public const string NAV_COORD = "NAV_*_COORD";
        public const string NAV_MGRS = "NAV_*_MGRS";


        public const string THREAT_NAME = "THREAT_*_NAME";
        public const string THREAT_TYPE = "THREAT_*_TYPE";

        public const string OWNSHIP_NAME = "OWNSHIP_NAME";
        public const string OWNSHIP_STN = "OWNSHIP_STN";
        public const string OWNSHIP_BOARD = "OWNSHIP_BOARD";
        public const string OWNSHIP_TACAN = "OWNSHIP_TACAN";
        public const string OWNSHIP_TACANBAND = "OWNSHIP_TACANBAND";
        public const string OWNSHIP_JOKER = "OWNSHIP_JOKER";
        public const string OWNSHIP_LASE = "OWNSHIP_LASE";
        public const string OWNSHIP_COMM = "OWNSHIP_COMM";

    }
}
