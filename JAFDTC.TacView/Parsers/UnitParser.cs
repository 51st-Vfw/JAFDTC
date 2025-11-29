using JAFDTC.TacView.Extensions;
using JAFDTC.TacView.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            var fieldDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); //fields can SWAP ORDER!
            foreach (var pair in value.Split(','))
            {
                var kv = pair.Split('=');
                fieldDict[kv[0]] = kv.Length == 2 ? kv[1] : string.Empty;
            }

            var cat = fieldDict.ToCleanValue("Type");
            if (_IgnoreCategoryTypes.Any(cat.Contains)) //we never care about these.. so skip em...
                return default;

            var result = new UnitItem
            {
                Id = fieldDict.Keys.First(),
                Position = PositionParser.Parse(fieldDict.ToCleanValue("T")), //to be overwritten at final Marker point...
                Category = cat.ToCategory(),
                Coalition = fieldDict.ToCleanValue("Coalition").ToCoalition(),
                Unit = fieldDict.ToCleanValue("Name").ToUnit(),
                GroupName = fieldDict.ToCleanValue("Group"),
                UnitName = fieldDict.ToCleanValue("Pilot"),
                IsAlive = true, //default to alive; deletions will set to false below

                //for testing....
                DebugInfo = value,
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
                result.GroupName = $"Group {result.UnitName} ({result.Id})"; //or something?

            return result;
        }
    }
}