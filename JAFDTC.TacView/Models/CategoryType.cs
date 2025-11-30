namespace JAFDTC.TacView.Models
{
    /// <summary>
    /// Known ACMI "Type=" values mapped to a strongly-typed enum.
    /// Values were derived from the sample ACMI file and normalized from '+' -> ' '.
    /// </summary>
    public enum CategoryType
    {
        Unknown = 0,

        Air_FixedWing,
        Air_Rotorcraft,
        Ground_AntiAircraft,
        Ground_Heavy_Armor_Vehicle_Tank,
        Ground_Static_Aerodrome,
        Ground_Static_Building,
        Ground_Vehicle,
        Misc_Container,
        Misc_Decoy_Chaff,
        Misc_Decoy_Flare,
        Misc_Shrapnel,
        Navaid_Static_Bullseye,
        Projectile_Shell,
        Sea_Watercraft,
        Sea_Watercraft_AircraftCarrier,
        Sea_Watercraft_Warship,
        Shrapnel,
        Weapon_Bomb,
        Weapon_Missile,
        Weapon_Rocket

    }
}
