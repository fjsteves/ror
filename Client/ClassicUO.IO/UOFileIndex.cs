// SPDX-License-Identifier: BSD-2-Clause
// Minimal implementation for RealmOfReality

using System;
using System.Runtime.InteropServices;

namespace ClassicUO.IO
{
    /// <summary>
    /// Verdata patch index structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct UOFileIndex5D
    {
        public uint FileID;
        public uint BlockID;
        public uint Position;
        public uint Length;
        public uint GumpData;
    }

    public struct UOFileIndex : IEquatable<UOFileIndex>
    {
        public UOFile? File;
        public long Offset;
        public int Length;
        public int DecompressedLength;
        public CompressionType CompressionFlag;

        // For gumps and other sized entries
        public short Width;
        public short Height;
        public ushort Hue;

        public static readonly UOFileIndex Invalid = new UOFileIndex { Offset = -1, Length = -1 };

        public UOFileIndex(UOFile? file, long offset, int length, int decompressedLength)
        {
            File = file;
            Offset = offset;
            Length = length;
            DecompressedLength = decompressedLength;
            CompressionFlag = CompressionType.None;
            Width = 0;
            Height = 0;
            Hue = 0;
        }

        public UOFileIndex(UOFile? file, long offset, int length, int decompressedLength,
            CompressionType compression, short width = 0, short height = 0)
        {
            File = file;
            Offset = offset;
            Length = length;
            DecompressedLength = decompressedLength;
            CompressionFlag = compression;
            Width = width;
            Height = height;
            Hue = 0;
        }

        public bool Equals(UOFileIndex other)
        {
            return Offset == other.Offset && Length == other.Length;
        }

        public override bool Equals(object? obj)
        {
            return obj is UOFileIndex other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Offset, Length);
        }
    }
}
