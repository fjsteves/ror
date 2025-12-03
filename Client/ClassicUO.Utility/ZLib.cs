// SPDX-License-Identifier: BSD-2-Clause
// Minimal implementation for RealmOfReality

using System;
using System.IO;
using System.IO.Compression;

namespace ClassicUO.Utility
{
    /// <summary>
    /// ZLib compression utilities.
    /// </summary>
    public static class ZLib
    {
        public enum ZLibError
        {
            Ok = 0,
            StreamEnd = 1,
            NeedDict = 2,
            StreamError = -2,
            DataError = -3,
            MemError = -4,
            BufError = -5
        }

        /// <summary>
        /// Decompress ZLib data.
        /// </summary>
        public static ZLibError Decompress(ReadOnlySpan<byte> source, Span<byte> dest)
        {
            try
            {
                // Skip 2-byte zlib header if present
                int offset = 0;
                if (source.Length >= 2)
                {
                    byte cmf = source[0];
                    byte flg = source[1];

                    // Check for valid zlib header
                    if ((cmf & 0x0F) == 8 && ((cmf * 256 + flg) % 31) == 0)
                    {
                        offset = 2;
                    }
                }

                using var input = new MemoryStream(source.Slice(offset).ToArray());
                using var deflate = new DeflateStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();

                deflate.CopyTo(output);
                var result = output.ToArray();

                if (result.Length <= dest.Length)
                {
                    result.AsSpan().CopyTo(dest);
                    return ZLibError.Ok;
                }

                return ZLibError.BufError;
            }
            catch
            {
                return ZLibError.DataError;
            }
        }

        /// <summary>
        /// Decompress ZLib data.
        /// </summary>
        public static ZLibError Decompress(byte[] source, byte[] dest)
        {
            return Decompress(source.AsSpan(), dest.AsSpan());
        }

        /// <summary>
        /// Compress data with ZLib.
        /// </summary>
        public static byte[] Compress(ReadOnlySpan<byte> source)
        {
            using var output = new MemoryStream();

            // Write zlib header
            output.WriteByte(0x78); // CMF
            output.WriteByte(0x9C); // FLG

            using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, true))
            {
                deflate.Write(source);
            }

            return output.ToArray();
        }
    }
}
