using JAFDTC.Core.Extensions;
using JAFDTC.Kneeboard.Models;
using Svg;
using System.Drawing.Imaging;

namespace JAFDTC.Kneeboard.Generate
{
    internal class ProcessKneeboard(GenerateCriteria _generateCriteria, string _templateFilePath) : IDisposable
    {
        private SvgDocument _svgDocument;

        public string Process()
        {
            var destinationPath = GetDestinationPath();

            _svgDocument = SvgDocument.Open(_templateFilePath);

            ProcessMisc();
            ProcessComms();
            ProcessFlights();
            ProcessSteerpoints();
            ProcessAirfields();
            ProcessMaps();

            Save(destinationPath);

            _svgDocument = null;

            return destinationPath;
        }

        private void ProcessMisc()
        {
            Assign("HEADER", _generateCriteria.Name);
            Assign("FOOTER", $"{_generateCriteria.Name}, {DateTime.Now.ToString("MM/dd/yyyy")}");
            Assign("THEATER", _generateCriteria.Theater);
            Assign("ISNIGHT", _generateCriteria.NightMode.ToString()); //eh?

            if (!string.IsNullOrWhiteSpace(_generateCriteria.PathLogo))
            {
                //todo: load into mem and assign (base64?) to svg...
            }

        }

        private void ProcessComms()
        {
            if (_generateCriteria.Comms.IsEmpty())
                return;

            foreach (var radio in _generateCriteria.Comms)
            {
                var commPrefix = $"COMM{radio.CommMode}";

                Assign(commPrefix, "NAME", radio.Name);
                Assign(commPrefix, "FREQNAME", radio.FrequencyName);

                for (var i = 0; i < 20; i++)
                {
                    var channel = (radio.Channels.HasData() && radio.Channels.Count() > i) ?
                        radio.Channels[i]
                        : new()
                        {
                            ChannelId = i + 1,
                            Frequency = 0,
                            Description = null
                        };
                    
                    var presetPrefix = $"{commPrefix}_PRESET{channel.ChannelId}";

                    Assign(presetPrefix, "ID", channel.ChannelId.ToString());
                    Assign(presetPrefix, "FREQ", channel.Frequency.ToString("{d:#.##}"));
                    Assign(presetPrefix, "DESC", channel.Description ?? "Unassigned"); //always replace text with Unassigned or blank...?
                }
            }
        }

        private void ProcessFlights()
        {
            //todo: 
        }

        private void ProcessSteerpoints()
        {
            //todo: 
        }

        private void ProcessAirfields()
        {
            //todo: 
        }

        private void ProcessMaps()
        {
            //todo: 
        }

        private string GetDestinationPath()
        {
            var baseName = Path.GetFileNameWithoutExtension(_templateFilePath);

            var safeFileName = string.Concat(_generateCriteria.Name);
            foreach (var c in Path.GetInvalidFileNameChars())
                safeFileName = safeFileName.Replace(c, '-');

            var fileName = $"{safeFileName}_{baseName}.png";
            var destinationPath = Path.Combine(_generateCriteria.PathOutput, _generateCriteria.Airframe, fileName);

            return destinationPath;
        }

        private void Save(string filePath)
        {
            using var stream = new MemoryStream();
            using var bitmap = _svgDocument.Draw();
            bitmap.Save(filePath, ImageFormat.Png);
        }

        private void Assign(string key, string value)
        {
            var match = $"[{key}]";

            var items = _svgDocument
                .Descendants()
                .OfType<SvgTextSpan>()
                .Where(p => p.Text.Contains(match, StringComparison.OrdinalIgnoreCase));

            if (items.IsEmpty())
                return;

            foreach (var item in items)
                item.Text = item.Text.Replace(match, value, StringComparison.OrdinalIgnoreCase);
        }

        private void Assign(string keyPrefix, string keySuffix, string value)
        {
            Assign($"{keyPrefix}_{keySuffix}", value);
        }

        public void Dispose()
        {
        }

    }
}
