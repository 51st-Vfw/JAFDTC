using JAFDTC.Core.Extensions;
using Svg;
using System.Drawing.Imaging;

namespace JAFDTC.Kneeboard.Generate
{
    internal class Builder(IReadOnlyDictionary<string, string> _data, string _templateFilePath, string _destinationFilePath) : IDisposable
    {
        private SvgDocument _svgDocument;
        private List<SvgTextSpan> _textItems;
        private List<SvgImage> _imageItems;
        
        public void Build()
        {
            LoadKB();
            Assign();
            Save();
        }

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

        private void Assign()
        {
            foreach (var item in _data)
            {
                AssignText(item.Key, item.Value);
                AssignImage(item.Key, item.Value);
            }
        }

        private void Save()
        {
            using var stream = new MemoryStream();
            using var bitmap = _svgDocument.Draw();
            bitmap.Save(_destinationFilePath, ImageFormat.Png);
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

        private static string ToMatch(string value)
        {
            return $"[{value}]";
        }

        public void Dispose()
        {
            _textItems?.Clear();
            _imageItems?.Clear();
            _svgDocument = null;
        }
    }
}