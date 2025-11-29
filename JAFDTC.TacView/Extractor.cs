using JAFDTC.TacView.Extensions;
using JAFDTC.TacView.Parsers;
using JAFDTC.TacView.Models;
using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace JAFDTC.TacView
{
    public class Extractor : IExtractor
    {

        public IReadOnlyList<UnitItem> Extract(ExtractCriteria extractCriteria)
        {
            extractCriteria.Required();
            extractCriteria.FilePath.Required();

            if (!File.Exists(extractCriteria.FilePath))
                throw new FileNotFoundException("ACMI file not found", extractCriteria.FilePath);

            var rawData = ReadACMI(extractCriteria.FilePath);
            var allLines = GetRawLines(rawData);
            rawData = null;
            if (!allLines.Any())
                throw new InvalidDataException("ACMI file contains no data");

            var timeMarker = GetTimeMarker(allLines, extractCriteria.TimeSnippet);

            var result = GetUnits(allLines, timeMarker)
                .LimitCoalitions(extractCriteria.Coalitions)
                .LimitCategories(extractCriteria.Categories) //todo: parent categories such as Air Def... SAM, SHORAD, etc etc misc groupings...
                .LimitAlive(extractCriteria.IsAlive)
                .ToList();

            allLines.Clear();

            return result;
        }

        internal static string ReadACMI(string filePath)
        {
            if (filePath.EndsWith(".zip.acmi", StringComparison.OrdinalIgnoreCase))
            {
                using var archive = ZipFile.Open(filePath, ZipArchiveMode.Read); //only 1 file
                using var entry = archive.Entries.First().Open();
                using var memoryStream = new MemoryStream();
                entry.CopyTo(memoryStream);
                memoryStream.Position = 0;
                using var reader = new StreamReader(memoryStream, Encoding.UTF8);
                return reader.ReadToEnd();
            }

            return File.ReadAllText(filePath);
        }

        internal static List<string> GetRawLines(string rawData)
        {
            return rawData
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(l => l?.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList()!;
        }

        internal static double GetTimeMarker(List<string> lines, DateTimeOffset? timeSnippet)
        {
            // parse ReferenceTime (header "0,ReferenceTime=2025-02-21T11:00:01Z")
            DateTimeOffset referenceTime = DateTimeOffset.MinValue;
            var refLine = lines.FirstOrDefault(l => l.StartsWith("0,ReferenceTime=", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(refLine))
            {
                var idx = refLine.IndexOf('=');
                if (idx >= 0)
                {
                    var val = refLine.Substring(idx + 1).Trim();
                    if (!DateTimeOffset.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out referenceTime))
                    {
                        referenceTime = DateTimeOffset.MinValue;
                    }
                }
            }

            // collect all timestamp markers (numeric seconds after ReferenceTime)
            var markers = new List<double>();
            foreach (var l in lines)
            {
                if (l.StartsWith("#", StringComparison.Ordinal))
                {
                    var s = l.Substring(1).Trim();
                    if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var sec))
                        markers.Add(sec);
                }
            }

            if (!markers.Any())
                return 0;

            // choose marker to extract: if TimeSnippet provided pick nearest marker; otherwise pick last marker
            if (timeSnippet.HasValue && referenceTime != DateTimeOffset.MinValue)
            {
                var alignedTime = DateTime.Parse($"{referenceTime.ToString("MM/dd/yyyy")} {timeSnippet.Value.ToString("hh:mm:ss")}");
                var sourceTime = DateTime.Parse($"{referenceTime.ToString("MM/dd/yyyy")} {referenceTime.ToString("hh:mm:ss")}");

                var targetSeconds = (alignedTime - sourceTime).TotalSeconds;
                return markers.OrderBy(m => Math.Abs(m - targetSeconds)).First();
            }

            return markers.Max();
        }

        internal static IEnumerable<UnitItem> GetUnits(List<string> lines, double timeMarker)
        {
            var units = new Dictionary<string, UnitItem>(StringComparer.OrdinalIgnoreCase);
            double? currentMarker = null;

            foreach (var line in lines) //i HATE event streams...
            {
                if (line.StartsWith("#", StringComparison.Ordinal)) //linear log of time marker...
                {
                    if (double.TryParse(line.Substring(1).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var sec))
                        currentMarker = sec;
                    else
                        currentMarker = null; //throw error?

                    if (currentMarker > timeMarker)
                        break; //we are passed the marker we care about.. ignore anything further....
                } 
                else if (line.Contains("Color=")) //first instance of the unit... or bulls...
                {
                    var unitItem = UnitParser.Parse(line);
                    if (unitItem != null)
                        units[unitItem.Id] = unitItem; //last one wins...
                }
                else if (line.StartsWith("-", StringComparison.Ordinal)) //unit was deleted/killed
                {
                    if (units.TryGetValue(line.Substring(1).Trim(), out var u)) //ensure it exists...
                        u.IsAlive = false;
                }
                else if (line.Contains("T=")) //subset of data that is just position updates... existing units on the move...
                {
                    var id = line[..line.IndexOf(',')];
                    if (units.TryGetValue(id, out var u)) //ensure it exists...
                        u.Position = PositionParser.Parse(line[(id.Length + 1)..].ToCleanValue()) ?? u.Position; //last update wins... if its null, use the previous position...
                }
            }

            //var debugUnitType = units.Values
            //    .Select(p=> p.DebugInfo2).Distinct().OrderBy(p=>p).ToList();
            //var debugUnitType = units.Values.Where(p=>p.Unit == UnitType.Unknown).Select(p => p.DebugInfo2).Distinct().OrderBy(p => p).ToList();

            return units.Values; //everything found up to the time marker...
        }

        public void Dispose()
        {         
        }
    }
}
