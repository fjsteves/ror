// SPDX-License-Identifier: BSD-2-Clause
// Minimal implementation for RealmOfReality

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ClassicUO.IO
{
    /// <summary>
    /// Reader for MUL+IDX format files.
    /// </summary>
    public class UOFileMul : UOFile
    {
        private FileReader? _idxReader;

        public FileReader? IdxFile => _idxReader;

        public UOFileMul(string mulPath) : base()
        {
            Load(mulPath);
        }

        public UOFileMul(string mulPath, string idxPath) : base()
        {
            Load(mulPath);

            if (File.Exists(idxPath))
            {
                _idxReader = new FileReader(idxPath);
            }
        }

        public override void FillEntries()
        {
            if (_idxReader == null || _idxReader.Length == 0)
            {
                // No index file - treat as raw data
                return;
            }

            int entryCount = (int)(_idxReader.Length / 12);
            Entries = new UOFileIndex[entryCount];

            _idxReader.Seek(0, SeekOrigin.Begin);

            for (int i = 0; i < entryCount; i++)
            {
                int offset = _idxReader.ReadInt32();
                int length = _idxReader.ReadInt32();
                int extra = _idxReader.ReadInt32();

                Entries[i] = new UOFileIndex(this, offset, length, 0)
                {
                    // Extra field interpretation depends on file type
                    // For gumps: width << 16 | height
                    // For sounds: index
                    Width = (short)(extra >> 16),
                    Height = (short)(extra & 0xFFFF)
                };
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _idxReader?.Dispose();
            _idxReader = null;
        }
    }
}
