using JAFDTC.Core.Extensions;
using JAFDTC.Kneeboard.Models;
using Svg;
using System.Drawing.Imaging;

namespace JAFDTC.Kneeboard.Generate
{
    internal class ProcessKneeboard(GenerateCriteria _generateCriteria, string _templateFilePath) : IDisposable
    {
        private SvgDocument _svgDocument;
        private List<SvgTextSpan> _elements;

        public string Process()
        {
            LoadKB();

            ProcessMisc();
            ProcessComms();
            ProcessFlights();
            ProcessSteerpoints();
            ProcessAirfields();
            ProcessThreats();
            ProcessMaps();

            return Save();
        }

        private void ProcessMisc()
        {
            Assign("HEADER", _generateCriteria.Name);
            Assign("FOOTER", $"{_generateCriteria.Name}, by {_generateCriteria.Owner.ToString()} @ {DateTime.Now.ToString("MM/dd/yyyy")}");
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
            /*
             * for general flight
             * dl page for
             *      CY-2 for COWBOY-2
             * radio page for 
             *      comm1,2 PRESET
             * misc page for 
             *      TACAN  (may not since it could be set per AC)
             *      JOKER
             *      LASE (may not since it could be set per AC)
             * 
             * we can ignore aircraft for now..
             * 
             * for each Pilot Slot
             * dl page for:
             *      name.. ie CY-2 = Cowboy 2
             *          Entry adds to name.. Cowboy 2-1
             *      pilot name
             *      tndl (stn)
             *  ..5-8 ignore flight name?
             *  
             *  radio page 
             *      look at presets?
             *      TACAN
             *          if your name is set to lead.. then everyone else gets recip.. else set primary to reciprocal
             *      JOKER (or just flight level)
             */
        }

        private void ProcessSteerpoints()
        {
            /*
             * STP page for
             *      stp num
             *      stp name if avail
             *      lat/lon (or just noise...)
             *      alt
             *      speed
             *      TOS/TOT
             * 
             * from Map POI DB and/or Import Miz/CF/ACMI
             *      if stp is at (or near??) then upgrade any missing data (like name, alt, etc)
             *      maybe if STP in WEZ or know threats..list them out in Notes ?
             * 
             */
        }

        private void ProcessAirfields()
        {
            /*
             * for any STPs that are linked or "at/near" airfield (farp tbd)
             *      also if first/last stp...?
             * 
             * from POI DB
             *      all info we have
             *      ?? tower comms, alt, runway(s), icls, etc?
             *      Enemy Threats: Aircraft, AAA, SAM, SHORAD, other?
             * 
             */
        }

        private void ProcessThreats()
        {
            /*
             * from POI import of miz/cf/acmi
             * distinct list of air/ground threats
             * 
             * Aircraft
             *      name, amount, rwr symbol, MAR?
             *      Mig-29, 12x
             *      Su-27, 3x
             *      AN-50, 1x
             *      
             * SAM
             *      name, rwr symbol, WEZ, ceiling, IR/EO/RD
             *      EWR, 3x
             *      SA-2 10x
             *      SA-5 2x
             *      SA-10 3x
             *      MIM-111 4x
             * 
             * SHORAD
             *      name, rwr, WEZ, ceiling, IR/EO/RD
             *      SA-8 x23
             *      SA-11 2x
             * 
             * AAA
             *      name, rwr, WEZ, ceiling, EO/RD
             *      Gepard 2x
             *      ZSU-23 80x
             * 
             * 
             * Locations 
             *      just airfields, farps?  cities?
             *      Aleppo: SA-10, SA-11, SA-15, Mig-29
             *      Damascus: EWR, SA-8, Su-27
             * 
             */
        }

        private void ProcessMaps()
        {
            /*
             * long term...
             * gen an image from "map widget"
             * 
             * theater map of total STP + buffer distance
             * 
             * stp map
             *      numbers/doghouses info
             * 
             * ip/target area zoomed in map + buffer distance
             * 
             * include POIs and Threats if imported from miz/cf/acmi
             *      airfield names, farp names
             *      wez rings
             *      enemy aircraft fields (ie mig29 aleppo)
             * 
             */
        }


        private void LoadKB()
        {
            _svgDocument = SvgDocument.Open(_templateFilePath);

            _elements = _svgDocument
                .Descendants()
                .OfType<SvgTextSpan>()
                .ToList();
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

        private string Save()
        {
            var filePath = GetDestinationPath();

            using var stream = new MemoryStream();
            using var bitmap = _svgDocument.Draw();
            bitmap.Save(filePath, ImageFormat.Png);

            return filePath;
        }

        private void Assign(string key, string value)
        {
            var match = $"[{key}]";

            var items = _elements
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
            _elements?.Clear();
            _svgDocument = null;
        }

    }
}
