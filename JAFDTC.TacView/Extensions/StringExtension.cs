using JAFDTC.TacView.Models;
using System.Globalization;

namespace JAFDTC.TacView.Extensions
{
    public static class StringExtension
    {
        public static CategoryType ToCategory(this string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return CategoryType.Unknown;

            switch (value)
            {
                case "Air+FixedWing": return CategoryType.Air_FixedWing;
                case "Air+Rotorcraft": return CategoryType.Air_Rotorcraft;
                case "Ground+AntiAircraft": return CategoryType.Ground_AntiAircraft;
                case "Ground+Heavy+Armor+Vehicle+Tank": return CategoryType.Ground_Heavy_Armor_Vehicle_Tank;
                case "Ground+Static+Aerodrome": return CategoryType.Ground_Static_Aerodrome;
                case "Ground+Static+Building": return CategoryType.Ground_Static_Building;
                case "Ground+Vehicle": return CategoryType.Ground_Vehicle;
                case "Misc+Container": return CategoryType.Misc_Container;
                case "Misc+Decoy+Chaff": return CategoryType.Misc_Decoy_Chaff;
                case "Misc+Decoy+Flare": return CategoryType.Misc_Decoy_Flare;
                case "Misc+Shrapnel": return CategoryType.Misc_Shrapnel;
                case "Navaid+Static+Bullseye": return CategoryType.Navaid_Static_Bullseye;
                case "Projectile+Shell": return CategoryType.Projectile_Shell;
                case "Sea+Watercraft": return CategoryType.Sea_Watercraft;
                case "Sea+Watercraft+AircraftCarrier": return CategoryType.Sea_Watercraft_AircraftCarrier;
                case "Sea+Watercraft+Warship": return CategoryType.Sea_Watercraft_Warship;
                case "Weapon+Bomb": return CategoryType.Weapon_Bomb;
                case "Weapon+Missile": return CategoryType.Weapon_Missile;
                case "Weapon+Rocket": return CategoryType.Weapon_Rocket;

                default:
                    return CategoryType.Unknown;
                    //throw new ArgumentException($"Unknown unit type string: {input}");
            }

        }

        public static CoalitionType ToCoalition(this string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return CoalitionType.Unknown;

            var normalized = value.Replace('+', ' ').Trim().ToLowerInvariant();

            return normalized switch
            {
                // Allies variants
                "allies" or "ally" or "allied" or "friendly" or "friendlies" => CoalitionType.Allies,

                // Enemies variants
                "enemies" or "enemy" or "hostile" or "hostiles" or "opposing" => CoalitionType.Enemies,

                // Neutrals variants
                "neutrals" or "neutral" or "none" => CoalitionType.Neutrals,

                _ => CoalitionType.Unknown,
            };
        }

        public static UnitType ToUnit(this string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return UnitType.Unknown;

            var normalized = value
                .Replace(" ", "_")
                .Replace("-", "_");

            // If starts with digit, prefix with U_
            if (char.IsDigit(normalized[0]))
            {
                normalized = "U_" + normalized;
            }

            if (Enum.TryParse<UnitType>(normalized, true, out var result))
                return result;

            return UnitType.Unknown;
        }

        public static string ToCleanValue(this string value)
        {
            return value[(value.IndexOf('=') + 1)..];
        }

        public static string ToCleanValue(this Dictionary<string, string> dict, string keyName)
        {
            if (dict.TryGetValue(keyName, out var v))
                return v;

            return string.Empty;
        }

        public static double ToCleaDouble(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;
        }

    }
}
