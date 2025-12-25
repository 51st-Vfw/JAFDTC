using JAFDTC.Kneeboard.Models;
using Svg;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Text;

namespace JAFDTC.Kneeboard.Generate
{
    internal abstract class GenerateBase : IGenerateKB
    {
        public abstract string Process(GenerateCriteria generateCriteria, string templateFilePath);

        public string GetDestinationPath(GenerateCriteria generateCriteria, string templateFilePath)
        {
            var baseName = Path.GetFileNameWithoutExtension(templateFilePath);
            var fileName = $"{generateCriteria.Name}_{baseName}.png";
            var destinationPath = Path.Combine(generateCriteria.PathOutput, generateCriteria.Airframe, fileName);

            return destinationPath;
        }

        public void Save(SvgDocument svgDocument, string filePath)
        {
            using var stream = new MemoryStream();
            var bitmap = svgDocument.Draw();
            bitmap.Save(filePath, ImageFormat.Png);
        }

        public virtual void Dispose()
        {
        }
    }
}
