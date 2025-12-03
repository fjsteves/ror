// SPDX-License-Identifier: BSD-2-Clause
// Minimal implementation for RealmOfReality

using System;
using System.Collections.Generic;
using System.IO;

namespace ClassicUO.IO
{
    /// <summary>
    /// Reader for UO .def definition files.
    /// </summary>
    public class DefReader : IDisposable
    {
        private readonly StreamReader _reader;
        private string[]? _parts;
        private int _partIndex;
        private readonly int _minParts;

        public int PartsCount => _parts?.Length ?? 0;

        public DefReader(string path, int minParts = 2)
        {
            _reader = new StreamReader(File.OpenRead(path));
            _minParts = minParts;
        }

        public bool Next()
        {
            while (!_reader.EndOfStream)
            {
                string? line = _reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Remove comments
                int commentIndex = line.IndexOf('#');
                if (commentIndex >= 0)
                    line = line.Substring(0, commentIndex);

                line = line.Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Split by whitespace and braces
                _parts = SplitLine(line);
                _partIndex = 0;

                if (_parts.Length >= _minParts)
                    return true;
            }

            return false;
        }

        private static string[] SplitLine(string line)
        {
            var parts = new List<string>();
            int start = -1;
            bool inBraces = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '{')
                {
                    inBraces = true;
                    if (start >= 0)
                    {
                        parts.Add(line.Substring(start, i - start).Trim());
                        start = -1;
                    }
                    start = i;
                }
                else if (c == '}')
                {
                    if (start >= 0)
                    {
                        parts.Add(line.Substring(start, i - start + 1).Trim());
                        start = -1;
                    }
                    inBraces = false;
                }
                else if (!inBraces && (c == ' ' || c == '\t'))
                {
                    if (start >= 0)
                    {
                        parts.Add(line.Substring(start, i - start).Trim());
                        start = -1;
                    }
                }
                else if (start < 0)
                {
                    start = i;
                }
            }

            if (start >= 0)
            {
                parts.Add(line.Substring(start).Trim());
            }

            return parts.ToArray();
        }

        public int ReadInt()
        {
            if (_parts == null || _partIndex >= _parts.Length)
                return 0;

            string part = _parts[_partIndex++];

            // Remove braces if present
            if (part.StartsWith("{") && part.EndsWith("}"))
                part = part.Substring(1, part.Length - 2);

            if (int.TryParse(part, out int val))
                return val;

            // Try hex
            if (part.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(part.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out val))
                    return val;
            }

            return 0;
        }

        public int ReadGroupInt()
        {
            var group = ReadGroup();
            return group != null && group.Length > 0 ? group[0] : 0;
        }

        public int[]? ReadGroup()
        {
            if (_parts == null || _partIndex >= _parts.Length)
                return null;

            string part = _parts[_partIndex++];

            // Check if it's a group in braces
            if (part.StartsWith("{") && part.EndsWith("}"))
            {
                part = part.Substring(1, part.Length - 2).Trim();
            }

            // Split by commas
            var items = part.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<int>();

            foreach (var item in items)
            {
                var trimmed = item.Trim();
                if (int.TryParse(trimmed, out int val))
                {
                    result.Add(val);
                }
                else if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    if (int.TryParse(trimmed.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out val))
                        result.Add(val);
                }
            }

            return result.Count > 0 ? result.ToArray() : null;
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
    }
}
