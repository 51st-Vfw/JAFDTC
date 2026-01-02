using Svg;
using System.Drawing.Imaging;

namespace JAFDTC.Kneeboard.Generate
{
    internal class Builder(IReadOnlyDictionary<string, string> _data, string _templateFilePath, string _destinationFilePath) : IDisposable
    {
        private SvgDocument _svgDocument;
        private List<SvgTextSpan> _textItems;
        private List<SvgImage> _imageItems;

        private const char KeyStart = '[';
        private const char KeyEnd = ']';

        private static readonly char[] _KeyDelim = [KeyStart, KeyEnd];

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
            foreach (var item in _textItems.Where(p => p?.Text != null && p.Text.Contains(KeyStart) && p.Text.Contains(KeyEnd)))
            {
                var matches = item.Text.Split(_KeyDelim, StringSplitOptions.RemoveEmptyEntries);
                foreach(var match in matches)
                    if (_data.TryGetValue(match, out var value))
                        item.Text = item.Text.Replace(ToMatch(match), value, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var item in _imageItems.Where(p => p?.Href != null && p.Href.Contains(KeyStart) && p.Href.Contains(KeyEnd)))
            {
                var matches = item.Href.Split(_KeyDelim, StringSplitOptions.RemoveEmptyEntries);
                foreach (var match in matches)
                    if (_data.TryGetValue(match, out var value))
                        item.Href = item.Href.Replace(ToMatch(match), value, StringComparison.OrdinalIgnoreCase);
            }
        }

        private void Save()
        {
            using var stream = new MemoryStream();
            using var bitmap = _svgDocument.Draw();
            bitmap.Save(_destinationFilePath, ImageFormat.Png);
        }

        private static string ToMatch(string value)
        {
            return $"{KeyStart}{value}{KeyEnd}";
        }

        public void Dispose()
        {
            _textItems?.Clear();
            _imageItems?.Clear();
            _svgDocument = null;
        }
    }
}