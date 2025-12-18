using System.Text.RegularExpressions;

namespace JAFDTC.Core.Expressions
{
    public static partial class CommonExpressions
    {
        [GeneratedRegex(@"(?i)^.+\s+(\d\d|\d):(\d\d):(\d\d)\s+(am|pm)")]
        public static partial Regex TimeRegex();

    }
}
