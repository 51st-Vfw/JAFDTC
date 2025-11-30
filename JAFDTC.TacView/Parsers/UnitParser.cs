using JAFDTC.TacView.Extensions;
using JAFDTC.TacView.Models;

namespace JAFDTC.TacView.Parsers
{
    public static class UnitParser
    {
        private static readonly HashSet<string> _IgnoreCategoryTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Weapon+",
            "Projectile+",
            "Misc+",
        };

        public static UnitItem? Parse(string value)
        {
            //104,T=4.4097307|5.3318376|1840.03|-3.8|1.6|321|112075.78|-383873.53|318.9,Type=Air+FixedWing,Color=Blue,Coalition=Enemies,Name=F-16C_50,Pilot=51st | Slinger,Group=Jedi-1,Country=us

            var fieldDict = GetKeyValuePairs(value);

            var cat = fieldDict.ToCleanValue("Type");
            if (_IgnoreCategoryTypes.Any(cat.Contains)) //we never care about these.. so skip em...
                return default;

            var result = new UnitItem
            {
                Id = fieldDict.Keys.First(),
                Position = PositionParser.Parse(fieldDict.ToCleanValue("T")), //to be overwritten at final Marker point...
                Category = cat.ToCategory(),
                Coalition = fieldDict.ToCleanValue("Coalition").ToCoalition(),
                Color = fieldDict.ToCleanValue("Color").ToColor(),
                Unit = fieldDict.ToCleanValue("Name").ToUnit(),
                GroupName = fieldDict.ToCleanValue("Group"),
                UnitName = fieldDict.ToCleanValue("Pilot"),
                IsAlive = true, //default to alive; deletions will set to false below

                //for testing....
                DebugInfo = value,
                DebugInfoDict = fieldDict,
                //DebugInfo2 = fields[2].ToCleanValue()
                //DebugInfo2 = fieldDict.ToCleanValue("Name"),
            };

            if (result.Category == CategoryType.Navaid_Static_Bullseye)
            {
                result.GroupName = result.UnitName = $"Bullseye_{result.Coalition}";
                result.Unit = UnitType.BULLSEYE;
            }

            if (string.IsNullOrWhiteSpace(result.UnitName))
                result.UnitName = $"Unit {result.Unit} ({result.Id})"; //or something?

            if (string.IsNullOrWhiteSpace(result.GroupName))
                result.GroupName = $"Group {result.UnitName}"; //or something?

            return result;
        }

        public static Dictionary<string, string> GetKeyValuePairs(string value)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); //fields can SWAP ORDER!
            foreach (var pair in value.Split(','))
            {
                var kv = pair.Split('=');
                result[kv[0]] = kv.Length == 2 ? kv[1] : kv[0];
            }
            return result;
        }
    }
}