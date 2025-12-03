// SPDX-License-Identifier: BSD-2-Clause
// Minimal implementation for RealmOfReality

using System;
using System.IO;

namespace ClassicUO.Utility
{
    /// <summary>
    /// File system helper utilities.
    /// </summary>
    public static class FileSystemHelper
    {
        public static void EnsureFileExists(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Required file not found: {path}", path);
            }
        }

        public static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"Required directory not found: {path}");
            }
        }

        public static string? FindFile(string directory, string fileName, bool caseSensitive = false)
        {
            if (!Directory.Exists(directory))
                return null;

            var comparison = caseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            foreach (var file in Directory.GetFiles(directory))
            {
                if (Path.GetFileName(file).Equals(fileName, comparison))
                    return file;
            }

            return null;
        }
    }
}
