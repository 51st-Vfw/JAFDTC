using JAFDTC.Core.Extensions;
using JAFDTC.Kneeboard.Models;
using Svg;
using System.Drawing.Imaging;

namespace JAFDTC.Kneeboard.Generate
{
    internal class KneeboardBuilder(GenerateCriteria _generateCriteria, string _templateFilePath) : IDisposable
    {
        private SvgDocument _svgDocument;
        private List<SvgTextSpan> _textItems;
        private List<SvgImage> _imageItems;

        public string Build()
        {
            LoadKB();

            BuildMisc();
            BuildComms();
            BuildFlights();
            BuildSteerpoints();
            BuildAirfields();
            BuildThreats();
            BuildMaps();

            return Save();
        }

        #region Build Sections

        private void BuildMisc()
        {
            AssignText("HEADER", _generateCriteria.Name);
            AssignText("FOOTER", $"{_generateCriteria.Name}, by {_generateCriteria.Owner.ToString()} @ {DateTime.Now.ToString("MM/dd/yyyy")}");
            AssignText("THEATER", _generateCriteria.Theater);
            AssignText("ISNIGHT", _generateCriteria.NightMode.ToString()); //eh?

            if (!string.IsNullOrWhiteSpace(_generateCriteria.PathLogo))
            {
                var b64Logo = Convert.ToBase64String(System.IO.File.ReadAllBytes(_generateCriteria.PathLogo));
                AssignImage("LOGO", b64Logo);
            }
        }

        private void BuildComms()
        {
            if (_generateCriteria.Comms.IsEmpty())
                return;

            foreach (var radio in _generateCriteria.Comms)
            {
                var prefix = $"COMM{radio.CommMode}";

                AssignText(prefix, "NAME", radio.Name);
                AssignText(prefix, "FREQNAME", radio.FrequencyName);

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

                    var presetPrefix = $"{prefix}_PRESET{channel.ChannelId}";

                    AssignText(presetPrefix, "ID", channel.ChannelId.ToString());
                    AssignText(presetPrefix, "FREQ", channel.Frequency.ToString("{d:#.##}"));
                    AssignText(presetPrefix, "DESC", Clean(channel.Description, "Unassigned")); //always replace text with Unassigned or blank...?
                }
            }
        }

        private void BuildFlights()
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

            if (_generateCriteria.Flight?.Units?.IsEmpty() ?? true)
                return;

            var flightName = string.Empty;
            var flightNum = string.Empty;
            var tacanLead = string.Empty;
            var tacanWing = string.Empty;
            var tacanBand = string.Empty;

            if (!string.IsNullOrWhiteSpace(_generateCriteria.Flight.Name))
            {
                var initialName = string.Concat(_generateCriteria.Flight.Name).ToUpper().Trim().Replace(" ", "-").Replace("_", "-");

                flightName = initialName.Split('-')[0];

                if (initialName.Contains('-'))
                    flightNum = initialName.Split('-')[1];
            }

            if (_generateCriteria.Pilots.HasData())
            {
                var currentPilot = _generateCriteria.Pilots
                    .FirstOrDefault(p => string.Equals(p.Name, _generateCriteria.Owner, StringComparison.OrdinalIgnoreCase));

                if (currentPilot != null)
                {
                    if (currentPilot.IsLead)
                    {
                        tacanLead = currentPilot.Tacan?.ToString() ?? string.Empty;
                        tacanWing = currentPilot.Tacan.HasValue ? (currentPilot.Tacan.Value + 63).ToString() : "";
                        tacanBand = currentPilot.TacanBand?.ToString().ToUpper() ?? string.Empty;
                    }
                    else
                    {
                        tacanWing = currentPilot.Tacan?.ToString() ?? string.Empty;
                        tacanLead = currentPilot.Tacan.HasValue ? (currentPilot.Tacan.Value - 63).ToString() : "";
                        tacanBand = currentPilot.TacanBand?.ToString()?.ToUpper() ?? string.Empty;
                    }

                    if (string.IsNullOrWhiteSpace(flightName) && !string.IsNullOrWhiteSpace(currentPilot.Callsign)) //if it didnt come from imported Unit with STPs.. then use DL pilot current data...
                    {
                        flightName = currentPilot.Callsign.Split(' ')[0].ToUpper(); //WF 22 -> WF

                        if (currentPilot.Callsign.Contains(' '))
                            flightNum = currentPilot.Callsign.Split(' ')[1][0].ToString(); //WF 22 -> 22 -> 2
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(flightName)) //default
                flightName = "FLIGHT";

            if (flightName.Length == 2) //jaf CY SD etc
            {
                if (flightName == "CY")
                    flightName = "COWBOY";
                else if (flightName == "LO")
                    flightName = "LOBO";
                else if (flightName == "WF")
                    flightName = "WOLF";
                else if (flightName == "UI")
                    flightName = "UZI";
                else if (flightName == "VR")
                    flightName = "VIPER";
                else if (flightName == "SD")
                    flightName = "SPRINGFIELD";
                else if (flightName == "EN")
                    flightName = "ENSIGN";
                else if (flightName == "NA")
                    flightName = "NINJA";
                else if (flightName == "CT")
                    flightName = "COLT";
            }

            flightName += $"-{flightNum}"; //finalize flight name

            //var presetVictor = "";

            AssignText("FLIGHT_NAME", flightName);
            AssignText("FLIGHT_TYPE", _generateCriteria.Airframe);
            AssignText("FLIGHT_TACAN", string.IsNullOrWhiteSpace(tacanLead) ? string.Empty : $"{tacanLead}/{tacanWing}{tacanBand}");

            for (var i = 0; i < 8; i++) //jaf only supports 8 pilot slots (technically 2 4ships but whatevs)
            {
                var prefix = $"PILOT{i + 1}";

                //these are really tied together...
                var flight = (_generateCriteria.Flight.Units.Count() > i) ?
                        _generateCriteria.Flight.Units[i]
                        : null;

                var pilot = (_generateCriteria.Pilots.Count() > i) ?
                    _generateCriteria.Pilots[i]
                    : null;

                if (pilot == null)
                {
                    AssignText(prefix, "CALLSIGN", string.Empty);
                    AssignText(prefix, "TYPE", string.Empty);
                    AssignText(prefix, "NAME", string.Empty);
                    AssignText(prefix, "TNDL", string.Empty);
                    AssignText(prefix, "TACAN", string.Empty);
                    AssignText(prefix, "TACAN_NUM", string.Empty);
                    AssignText(prefix, "TACAN_BAND", string.Empty);
                    AssignText(prefix, "JOKER", string.Empty);
                    AssignText(prefix, "LASE_CODE", string.Empty);
                }
                else
                {
                    AssignText(prefix, "CALLSIGN", $"{flightName}-{i + 1}");

                    AssignText(prefix, "TYPE", Clean(flight?.Type, _generateCriteria.Airframe));

                    AssignText(prefix, "NAME", Clean(pilot.Name, "Unassigned"));
                    AssignText(prefix, "TNDL", Clean(pilot.TNDL, "Unassigned"));

                    AssignText(prefix, "TACAN", (pilot.IsLead ? tacanLead : tacanWing) + tacanBand);
                    AssignText(prefix, "TACAN_NUM", pilot.IsLead ? tacanLead : tacanWing);
                    AssignText(prefix, "TACAN_BAND", tacanBand);

                    if (string.Equals(pilot.Name, _generateCriteria.Owner, StringComparison.OrdinalIgnoreCase))
                    {
                        AssignText(prefix, "JOKER", pilot.Joker?.ToString() ?? string.Empty);
                        AssignText(prefix, "LASE_CODE", pilot.LaseCode?.ToString() ?? string.Empty);
                    }
                    else
                    {
                        AssignText(prefix, "JOKER", string.Empty);
                        AssignText(prefix, "LASE_CODE", string.Empty);
                    }

                }
            }
        }

        private void BuildSteerpoints()
        {
            if (_generateCriteria.Flight?.Route?.IsEmpty() ?? true)
                return;

            for (var i = 0; i < 50; i++) //this could get weird...? ie what if you have 1 to 500 stps?
            {
                var prefix = $"STP{i + 1}";

                var stp = (_generateCriteria.Flight.Route.Count() > i) ?
                        _generateCriteria.Flight.Route[i]
                        : null;

                if (stp == null)
                {
                    AssignText(prefix, "NUM", (i + 1).ToString());
                    AssignText(prefix, "NAME", prefix);
                    AssignText(prefix, "DESC", "Unassigned"); //or blank?
                    AssignText(prefix, "ALT", string.Empty);
                    AssignText(prefix, "TOS", string.Empty);
                    AssignText(prefix, "COORD", string.Empty);
                    AssignText(prefix, "MGRS", string.Empty);
                }
                else
                {
                    var desc = stp.Name;
                    if (string.IsNullOrWhiteSpace(desc)
                        || string.Equals(desc, $"STP{i + 1}", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(desc, $"SP{i + 1}", StringComparison.OrdinalIgnoreCase))
                    {
                        //todo: attempt to match by location to the POI DB
                        //maybe even import from miz/cf/acmi  group/unit/static

                        /*
                         * from Map POI DB and/or Import Miz/CF/ACMI
                         *      if stp is at (or near??) then upgrade any missing data (like name, alt, etc)
                         *      maybe if STP in WEZ or know threats..list them out in Notes ?
                         * 
                         */

                        //name = "better name..."
                    }

                    AssignText(prefix, "NUM", (i + 1).ToString());
                    AssignText(prefix, "NAME", prefix);
                    AssignText(prefix, "DESC", Clean(desc, prefix));
                    AssignText(prefix, "ALT", stp.Altitude.ToString("#"));
                    AssignText(prefix, "TOS", stp.TimeOnAsHMS); //todo: should be extension method...
                    AssignText(prefix, "COORD", "normal".ToDisplay(stp.Latitude, stp.Longitude));
                    AssignText(prefix, "MGRS", 10.ToMGRS(stp.Latitude, stp.Longitude));
                }
            }
        }

        private void BuildAirfields()
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

        private void BuildThreats()
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

        private void BuildMaps()
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

        #endregion

        #region Support Methods

        private void LoadKB()
        {
            _svgDocument = SvgDocument.Open(_templateFilePath);

            _textItems = _svgDocument
                .Descendants()
                .OfType<SvgTextSpan>()
                .ToList();

            _imageItems = _svgDocument
               .Descendants()
               .OfType<SvgImage>()
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

        private void AssignText(string key, string value)
        {
            var match = ToMatch(key);

            var items = _textItems
                .Where(p => p.Text != null && p.Text.Contains(match, StringComparison.OrdinalIgnoreCase));

            if (items.IsEmpty())
                return;

            foreach (var item in items)
                item.Text = item.Text.Replace(match, value, StringComparison.OrdinalIgnoreCase);
        }

        private void AssignText(string keyPrefix, string keySuffix, string value)
        {
            AssignText($"{keyPrefix}_{keySuffix}", value);
        }

        private void AssignImage(string key, string base64Image)
        {
            var match = ToMatch(key);

            var items = _imageItems
                .Where(p => p.Href != null && p.Href.Contains(match, StringComparison.OrdinalIgnoreCase));

            if (items.IsEmpty())
                return;

            foreach (var item in items)
                item.Href = item.Href.Replace(match, base64Image, StringComparison.OrdinalIgnoreCase);
        }

        private string ToMatch(string value)
        {
            return $"[{value}]";
        }

        private string Clean(string? input, string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(input))
                return defaultValue;

            return input;
        }

        #endregion

        public void Dispose()
        {
            _textItems?.Clear();
            _imageItems?.Clear();
            _svgDocument = null;
        }
    }
}