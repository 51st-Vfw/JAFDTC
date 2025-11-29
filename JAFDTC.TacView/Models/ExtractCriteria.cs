using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JAFDTC.TacView.Models
{
    public class ExtractCriteria
    {
        public required string FilePath { get; set; }
        public DateTimeOffset? TimeSnippet { get; set; }
        public CoalitionType[]? Coalitions { get; set; }
        public CategoryType[]? Categories { get; set; }
        public bool? IsAlive { get; set; }
}
}
