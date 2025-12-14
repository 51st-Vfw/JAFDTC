// TODO: this all likely needs to live elsewhere. parking it here for now...

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JAFDTC.Models.CoreApp
{
    public class ExtractCriteria
    {
        public required string FilePath { get; set; }
        public string Theater { get; set; }
        public CoalitionType[]? Coalitions { get; set; }
        public UnitCategoryType[]? UnitCategories { get; set; }
// TODO: move this to an enum?
        public string[]? UnitTypes { get; set; }
        public bool? IsAlive { get; set; }
    }

    public interface IExtractor : IDisposable
    {
        IReadOnlyList<UnitGroupItem> Extract(ExtractCriteria extractCriteria);
    }

    public static class FilterExtension
    {
        public static IEnumerable<UnitGroupItem> LimitCoalitions(this IEnumerable<UnitGroupItem> values, CoalitionType[] coalitions)
        {
            if (coalitions == null || coalitions.Length == 0)
                return values;

            return values.Where(u => coalitions.Contains(u.Coalition));
        }

        public static IEnumerable<UnitGroupItem> LimitUnitCategories(this IEnumerable<UnitGroupItem> values, UnitCategoryType[] unitCategories)
        {
            if (unitCategories == null || unitCategories.Length == 0)
                return values;

            return values.Where(u => unitCategories.Contains(u.Category));
        }

        public static IEnumerable<UnitGroupItem> LimitGroupsWithUnits(this IEnumerable<UnitGroupItem> values)
        {
            return values.Where(u => (u.Units.Count > 0));
        }

        public static IEnumerable<UnitItem> LimitUnitTypes(this IEnumerable<UnitItem> values, string[] unitTypes)
        {
            if (unitTypes == null || unitTypes.Length == 0)
                return values;

            return values.Where(u => unitTypes.Contains(u.Type));
        }

        public static IEnumerable<UnitItem> LimitAlive(this IEnumerable<UnitItem> values, bool? isAlive)
        {
            if (!isAlive.HasValue)
                return values;

            return values.Where(u => u.IsAlive == isAlive.Value);
        }
    }

    public enum CoalitionType
    {
        UNKNOWN = -1,
        BLUE = 0,
        RED = 1,
        NEUTRAL = 2
    }

    public enum UnitCategoryType
    {
        AIRCRAFT = 0,
        HELICOPTER = 1,
        GROUND = 2,
        NAVAL = 3
    }

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
                                               ((int)TimeOn / 3600), ((int)TimeOn / 60) % 60, ((int)TimeOn % 60))
                               : string.Empty;
    }

    public class UnitItem
    {
        public required string UniqueID { get; set; }                       // unique identifier for unit
// TODO: make this an enum? need some mapping from string to enum for ui, possibly
        public required string Type { get; set; }                           // unit type
        public required string Name { get; set; }                           // unit name
        public required UnitPositionItem Position { get; set; }             // unit position
        public required bool IsAlive { get; set; }                          // t => alive
    }

    public class UnitGroupItem
    {
        public required string UniqueID { get; set; }                       // unique identifier for group
        public required CoalitionType Coalition { get; set; }               // coalition of group
        public required UnitCategoryType Category { get; set; }             // category of units in group
        public required string Name { get; set; }                           // group name
        public required IReadOnlyList<UnitItem> Units { get; set; }         // list of units in group
        public required IReadOnlyList<UnitPositionItem> Route { get; set; } // list of positions along route
    }
}
