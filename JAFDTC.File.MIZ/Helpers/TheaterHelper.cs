using JAFDTC.File.MIZ.Models;

namespace JAFDTC.File.MIZ.Helpers
{
    public static class TheaterHelper
    {
        // internal theater inforamtion dictionary mapping a DCS theater (from the theater file embedded in the .miz)
        // onto a corresponding TheaterInfo instance. x/z values in the terrain info are pre-computed based on grid
        // inforamtion generated from dcs.
        //
        private static readonly Dictionary<string, TheaterInfo> _theaterInfo = new()
        {
            ["Afghanistan"] = new TheaterInfo(3759656.73273285, 300149.98159237, 41, false),
            ["Caucasus"] = new TheaterInfo(4998114.6775109, 99517.01067793, 36, false),
            ["Falklands"] = new TheaterInfo(4184583.3670, -147639.8755, 21, true),
            ["Germany"] = new TheaterInfo(6061632.75443205, -35427.57613432, 34, false),
            ["Iraq"] = new TheaterInfo(3680056.7536012, -72289.97515422, 38, false),
            ["Kola"] = new TheaterInfo(7543624.54780873, 62701.93823161, 34, false),
            ["MarianaIslands"] = new TheaterInfo(1491839.88704271, -238417.99059562, 55, false),
            ["Nevada"] = new TheaterInfo(4410027.78012357, 193996.80821451, 11, false),
            ["PersianGulf"] = new TheaterInfo(2894932.78443276, -75755.99875273, 40, false),
            ["SinaiMap"] = new TheaterInfo(3325312.76592359, -169221.99957107, 36, false),
            ["Syria"] = new TheaterInfo(3879865.72585971, -282800.99275397, 37, false),
        };

        public static TheaterInfo? GetTheaterInfo(string theater)
        {
            if (_theaterInfo.TryGetValue(theater, out TheaterInfo? value))
                return value;

            return null;
        }

    }
}
