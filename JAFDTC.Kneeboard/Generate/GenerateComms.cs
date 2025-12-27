using JAFDTC.Core.Extensions;
using JAFDTC.Kneeboard.Models;
using JAFDTC.Models.Radios;
using Svg;

namespace JAFDTC.Kneeboard.Generate
{
    internal class GenerateComms : GenerateBase
    {
        public override string Process(GenerateCriteria generateCriteria, string templateFilePath)
        {
            var destinationPath = base.GetDestinationPath(generateCriteria, templateFilePath);

            var svg = SvgDocument.Open(templateFilePath);

            //todo: find/replace text/etc etc

            Assign(svg, "HEADER", generateCriteria.Name);
            Assign(svg, "FOOTER", $"{generateCriteria.Name}, {DateTime.Now.ToString("MM/dd/yyyy")}");

            //headerLogo


            foreach (var radio in generateCriteria.Comms)
            {
                var commPrefix = $"COMM{radio.CommMode}";

                Assign(svg, commPrefix, "NAME", radio.Name);
                Assign(svg, commPrefix, "FREQNAME", radio.FrequencyName);

                for (var i = 0; i < 20; i++)
                {
                    Channel channel = null;
                    if (radio.Channels.HasData() && radio.Channels.Count() > i)
                        channel = radio.Channels[i];
                    else
                        channel = new()
                        {
                            ChannelId = i + 1,
                            Frequency = 0,
                            Description = null
                        };

                    //always replace text with Unassigned or blank...
                    var presetPrefix = $"{commPrefix}_PRESET{channel.ChannelId}";

                    Assign(svg, presetPrefix, "FREQ", channel.Frequency.ToString("{d:#.##}"));
                    Assign(svg, presetPrefix, "DESC", channel.Description ?? "Unassigned");

                }
            }



            base.Save(svg, destinationPath);

            return destinationPath;
        }


    }
}
