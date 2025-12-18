using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JAFDTC.Core.Expressions
{
    public static partial class CommonExpressions
    {
        [GeneratedRegex(@"(?i)^.+\s+(\d\d|\d):(\d\d):(\d\d)\s+(am|pm)")]
        public static partial Regex TimeRegex();

    }
}
