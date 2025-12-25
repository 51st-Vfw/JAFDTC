using JAFDTC.Kneeboard.Models;
using Svg;

namespace JAFDTC.Kneeboard.Generate
{
    internal class GenerateAirfields : GenerateBase
    {
        public override string Process(GenerateCriteria generateCriteria, string templateFilePath)
        {
            var destinationPath = base.GetDestinationPath(generateCriteria, templateFilePath);

            var svg = SvgDocument.Open(templateFilePath);

            //todo: find/replace text/etc etc

            //var textElement = svg.Descendants()
            //                     .First(t => t.ID == "Header")
            //                     .Children[0] as SvgTextSpan;

            //textElement.Text = "Rage";


            base.Save(svg, destinationPath);

            return destinationPath;
        }
    }
}
