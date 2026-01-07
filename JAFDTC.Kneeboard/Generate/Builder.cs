// ********************************************************************************************************************
//
// Builder.cs -- .svg builder for kneeboard generator
//
// Copyright(C) 2026 rage
//
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU General
// Public License as published by the Free Software Foundation, either version 3 of the License, or (at your
// option) any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
// for more details.
//
// You should have received a copy of the GNU General Public License along with this program.  If not, see
// <https://www.gnu.org/licenses/>.
//
// ********************************************************************************************************************

using Svg;
using System.Drawing.Imaging;

namespace JAFDTC.Kneeboard.Generate
{
    internal class Builder(IReadOnlyDictionary<string, string> _data, string _templateFilePath, string _destinationFilePath) : IDisposable
    {
        // TODO: consider dumping key start/end markers
        private const char KeyStart = '[';
        private const char KeyEnd = ']';

        private static readonly char[] _KeyDelim = [KeyStart, KeyEnd];

        private SvgDocument? _svgDocument;
        private List<SvgTextSpan> _textItems;
        private List<SvgImage> _imageItems;
        private List<SvgRectangle> _rectItems;
        private bool _changed;

        public void Build(bool isNightMode, bool isSVGMode)
        {
            LoadKB();
            Assign();
            TintKneeboard(isNightMode);
            Save(isSVGMode);
        }

        private void LoadKB()
        {
            _svgDocument = SvgDocument.Open(_templateFilePath);

            _textItems = [.. _svgDocument.Descendants().OfType<SvgTextSpan>() ];
            _imageItems = [.. _svgDocument.Descendants().OfType<SvgImage>() ];
            _rectItems = [.. _svgDocument.Descendants().OfType<SvgRectangle>() ];
        }

        private void Assign()
        {
            // TODO: consider regex here...
            foreach (var item in _textItems.Where(p => p?.Text != null && p.Text.Contains(KeyStart) && p.Text.Contains(KeyEnd)))
            {
                var matches = item.Text.Split(_KeyDelim, StringSplitOptions.RemoveEmptyEntries);

                foreach(var match in matches)
                    if (_data.TryGetValue(match, out var value))
                    {
                        _changed = true;
                        item.Text = item.Text.Replace(ToMatch(match), value, StringComparison.OrdinalIgnoreCase);
                    }
                    else 
                        item.Text = item.Text.Replace(ToMatch(match), string.Empty, StringComparison.OrdinalIgnoreCase); //or default parsed data...
            }

#if POSSIBLY_IMPLEMENT_LATER
            foreach (var item in _imageItems.Where(p => p?.Href != null && p.Href.Contains(KeyStart) && p.Href.Contains(KeyEnd)))
            {
                var matches = item.Href.Split(_KeyDelim, StringSplitOptions.RemoveEmptyEntries);
                foreach (var match in matches)
                    if (_data.TryGetValue(match, out var value))
                    {
                        _changed = true;
                        item.Href = item.Href.Replace(ToMatch(match), value, StringComparison.OrdinalIgnoreCase);
                    }
                    else
                        item.Href = item.Href.Replace(ToMatch(match), string.Empty, StringComparison.OrdinalIgnoreCase); //or default parsed data...
            }
#endif
        }

        private void TintKneeboard(bool isNight)
        {
            foreach (var item in _rectItems.Where(p => p.ID.Contains(Models.Keys.NIGHT_TINT)))
                item.Opacity = (isNight) ? item.Opacity : (float) 0.0;
        }

        private void Save(bool isSVGMode)
        {
            if (isSVGMode)
                _destinationFilePath = Path.ChangeExtension(_destinationFilePath, ".svg");

            if (File.Exists(_destinationFilePath))
                File.Delete(_destinationFilePath);

            if (_changed && !isSVGMode && (_svgDocument != null))
            {
                using var stream = new MemoryStream();
                using var bitmap = _svgDocument.Draw();
                bitmap.Save(_destinationFilePath, ImageFormat.Png);
            }
            else if (_changed && (_svgDocument != null))
            {
                _svgDocument.Write(_destinationFilePath);
            }
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