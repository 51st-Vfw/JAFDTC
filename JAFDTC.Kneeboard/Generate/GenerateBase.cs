using JAFDTC.Core.Extensions;
using JAFDTC.Kneeboard.Models;
using Svg;
using System.Drawing.Imaging;

namespace JAFDTC.Kneeboard.Generate
{
    internal abstract class GenerateBase : IGenerateKB
    {
        public abstract string Process(GenerateCriteria generateCriteria, string templateFilePath);

        public string GetDestinationPath(GenerateCriteria generateCriteria, string templateFilePath)
        {
            var baseName = Path.GetFileNameWithoutExtension(templateFilePath);

            var safeFileName = string.Concat(generateCriteria.Name);
            foreach (var c in Path.GetInvalidFileNameChars())
                safeFileName = safeFileName.Replace(c, '-');

            var fileName = $"{safeFileName}_{baseName}.png";
            var destinationPath = Path.Combine(generateCriteria.PathOutput, generateCriteria.Airframe, fileName);

            return destinationPath;
        }

        public void Save(SvgDocument svgDocument, string filePath)
        {
            using var stream = new MemoryStream();
            using var bitmap = svgDocument.Draw();
            bitmap.Save(filePath, ImageFormat.Png);
        }

        public void Assign(SvgDocument svg, string key, string value)
        {
            var match = $"[{key}]";

            var items = svg
                .Descendants()
                .OfType<SvgTextSpan>()
                .Where(p => string.Equals(p.Text, match, StringComparison.OrdinalIgnoreCase));

            if (items.IsEmpty())
                return;

            foreach (var item in items)
                item.Text = item.Text.Replace(item.Text, value, StringComparison.OrdinalIgnoreCase);
        }

        public void Assign(SvgDocument svg, string keyPrefix, string keySuffix, string value)
        {
            Assign(svg, $"{keyPrefix}_{keySuffix}", value);
        }

        public virtual void Dispose()
        {
        }
    }
}
