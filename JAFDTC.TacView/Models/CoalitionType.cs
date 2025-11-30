namespace JAFDTC.TacView.Models
{
    /// <summary>
    /// Known ACMI "Coalition=" values.
    /// Values derived from sample ACMI: "Neutrals", "Enemies", "Allies".
    /// </summary>
    public enum CoalitionType
    {
        Unknown = 0,

        Allies,
        Enemies,
        Neutral,
        Neutrals
    }
}