using JAFDTC.Kneeboard.Models;
using Svg;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace JAFDTC.Kneeboard.Generate
{
    internal class GenerateComms : GenerateBase
    {
        public override string Process(GenerateCriteria generateCriteria, string templateFilePath)
        {
            var destinationPath = base.GetDestinationPath(generateCriteria, templateFilePath);

            var svgDocument = SvgDocument.Open(templateFilePath);

            //todo: find/replace text/etc etc

            base.Save(svgDocument, destinationPath);

            return destinationPath;
        }
    }
}
