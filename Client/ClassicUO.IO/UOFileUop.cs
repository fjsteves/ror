// SPDX-License-Identifier: BSD-2-Clause
// Minimal implementation for RealmOfReality

using System;
using System.Collections.Generic;
using System.IO;

namespace ClassicUO.IO
{
    /// <summary>
    /// Reader for UOP format files (Ultima Online patcher format).
    /// </summary>
    public class UOFileUop : UOFile
    {
        private readonly string _pattern;
        private readonly bool _hasExtra;
        private readonly Dictionary<ulong, int> _hashToIndex = new();

        public UOFileUop(string filePath, string pattern, bool hasExtra = false) : base()
        {
            _pattern = pattern;
            _hasExtra = hasExtra;
            Load(filePath);
        }

        public static ulong CreateHash(string s)
        {
            uint eax, ecx, edx, ebx, esi, edi;
            eax = ecx = edx = ebx = esi = edi = 0;
            ebx = edi = esi = (uint)s.Length + 0xDEADBEEF;

            int i = 0;

            for (i = 0; i + 12 < s.Length; i += 12)
            {
                edi = (uint)((s[i + 7] << 24) | (s[i + 6] << 16) | (s[i + 5] << 8) | s[i + 4]) + edi;
                esi = (uint)((s[i + 11] << 24) | (s[i + 10] << 16) | (s[i + 9] << 8) | s[i + 8]) + esi;
                edx = (uint)((s[i + 3] << 24) | (s[i + 2] << 16) | (s[i + 1] << 8) | s[i]) - esi;

                edx = (edx + ebx) ^ (esi >> 28) ^ (esi << 4);
                esi += edi;
                edi = (edi - edx) ^ (edx >> 26) ^ (edx << 6);
                edx += esi;
                esi = (esi - edi) ^ (edi >> 24) ^ (edi << 8);
                edi += edx;
                ebx = (edx - esi) ^ (esi >> 16) ^ (esi << 16);
                esi += edi;
                edi = (edi - ebx) ^ (ebx >> 13) ^ (ebx << 19);
                ebx += esi;
                esi = (esi - edi) ^ (edi >> 28) ^ (edi << 4);
                edi += ebx;
            }

            if (s.Length - i > 0)
            {
                switch (s.Length - i)
                {
                    case 12:
                        esi += (uint)s[i + 11] << 24;
                        goto case 11;
                    case 11:
                        esi += (uint)s[i + 10] << 16;
                        goto case 10;
                    case 10:
                        esi += (uint)s[i + 9] << 8;
                        goto case 9;
                    case 9:
                        esi += s[i + 8];
                        goto case 8;
                    case 8:
                        edi += (uint)s[i + 7] << 24;
                        goto case 7;
                    case 7:
                        edi += (uint)s[i + 6] << 16;
                        goto case 6;
                    case 6:
                        edi += (uint)s[i + 5] << 8;
                        goto case 5;
                    case 5:
                        edi += s[i + 4];
                        goto case 4;
                    case 4:
                        ebx += (uint)s[i + 3] << 24;
                        goto case 3;
                    case 3:
                        ebx += (uint)s[i + 2] << 16;
                        goto case 2;
                    case 2:
                        ebx += (uint)s[i + 1] << 8;
                        goto case 1;
                    case 1:
                        ebx += s[i];
                        break;
                }

                esi = (esi ^ edi) - ((edi >> 18) ^ (edi << 14));
                ecx = (esi ^ ebx) - ((esi >> 21) ^ (esi << 11));
                edi = (edi ^ ecx) - ((ecx >> 7) ^ (ecx << 25));
                esi = (esi ^ edi) - ((edi >> 16) ^ (edi << 16));
                edx = (esi ^ ecx) - ((esi >> 28) ^ (esi << 4));
                edi = (edi ^ edx) - ((edx >> 18) ^ (edx << 14));
                eax = (esi ^ edi) - ((edi >> 8) ^ (edi << 24));

                return ((ulong)edi << 32) | eax;
            }

            return ((ulong)esi << 32) | eax;
        }

        public override void FillEntries()
        {
            if (_reader == null || Length < 28)
                return;

            Seek(0, SeekOrigin.Begin);

            // Read header
            uint magic = ReadUInt32();
            if (magic != 0x50594D) // "MYP"
                return;

            uint version = ReadUInt32();
            uint timestamp = ReadUInt32();
            long firstBlock = ReadInt64();
            uint blockSize = ReadUInt32();
            uint fileCount = ReadUInt32();

            var entries = new List<UOFileIndex>((int)fileCount);
            _hashToIndex.Clear();

            long nextBlock = firstBlock;

            while (nextBlock != 0)
            {
                Seek(nextBlock, SeekOrigin.Begin);
                int filesInBlock = ReadInt32();
                nextBlock = ReadInt64();

                for (int i = 0; i < filesInBlock; i++)
                {
                    long offset = ReadInt64();
                    int headerLength = ReadInt32();
                    int compressedLength = ReadInt32();
                    int decompressedLength = ReadInt32();
                    ulong hash = ReadUInt64();
                    uint adler32 = ReadUInt32();
                    ushort flags = ReadUInt16();

                    if (offset == 0)
                        continue;

                    CompressionType compression = CompressionType.None;
                    if ((flags & 1) != 0)
                        compression = CompressionType.Zlib;
                    if ((flags & 2) != 0)
                        compression = CompressionType.ZlibBwt;

                    int index = entries.Count;
                    _hashToIndex[hash] = index;

                    var entry = new UOFileIndex(this, offset + headerLength, compressedLength, decompressedLength)
                    {
                        CompressionFlag = compression
                    };

                    entries.Add(entry);
                }
            }

            // Now map pattern-based hashes to indices
            // e.g., "build/artlegacymul/{0:D8}.tga"
            if (!string.IsNullOrEmpty(_pattern))
            {
                var patternEntries = new UOFileIndex[entries.Count + 0x10000]; // Leave room for land tiles
                int maxIndex = -1;

                for (int i = 0; i < 0x100000; i++)
                {
                    string name = string.Format(_pattern, i);
                    ulong hash = CreateHash(name);

                    if (_hashToIndex.TryGetValue(hash, out int entryIndex))
                    {
                        patternEntries[i] = entries[entryIndex];
                        patternEntries[i].File = this;
                        if (i > maxIndex) maxIndex = i;
                    }
                }

                // Trim to actual size
                if (maxIndex >= 0)
                {
                    Entries = new UOFileIndex[maxIndex + 1];
                    Array.Copy(patternEntries, Entries, maxIndex + 1);
                }
                else
                {
                    Entries = entries.ToArray();
                }
            }
            else
            {
                Entries = entries.ToArray();
            }
        }

        public bool TryGetUOPData(ulong hash, out UOFileIndex data)
        {
            if (_hashToIndex.TryGetValue(hash, out int index) && index < Entries.Length)
            {
                data = Entries[index];
                return true;
            }

            data = UOFileIndex.Invalid;
            return false;
        }
    }
}
