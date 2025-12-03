// SPDX-License-Identifier: BSD-2-Clause
// Minimal implementation for RealmOfReality

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ClassicUO.IO
{
    /// <summary>
    /// Base class for reading Ultima Online data files.
    /// </summary>
    public abstract class UOFile : IDisposable
    {
        protected FileReader? _reader;

        public long Length => _reader?.Length ?? 0;
        public long Position => _reader?.Position ?? 0;

        public UOFileIndex[] Entries { get; protected set; } = Array.Empty<UOFileIndex>();

        protected UOFile() { }

        protected void Load(string filePath)
        {
            if (File.Exists(filePath))
            {
                _reader = new FileReader(filePath);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Seek(long offset, SeekOrigin origin = SeekOrigin.Begin)
        {
            _reader?.Seek(offset, origin);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Skip(int count) => _reader?.Skip(count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadUInt8() => _reader?.ReadUInt8() ?? 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadInt8() => _reader?.ReadInt8() ?? 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16() => _reader?.ReadUInt16() ?? 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16() => _reader?.ReadInt16() ?? 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32() => _reader?.ReadUInt32() ?? 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32() => _reader?.ReadInt32() ?? 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64() => _reader?.ReadUInt64() ?? 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64() => _reader?.ReadInt64() ?? 0;

        public int Read(Span<byte> buffer) => _reader?.Read(buffer) ?? 0;
        public int Read(byte[] buffer) => _reader?.Read(buffer, 0, buffer.Length) ?? 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>() where T : unmanaged
        {
            return _reader != null ? _reader.Read<T>() : default;
        }

        public virtual void FillEntries() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref UOFileIndex GetValidRefEntry(int index)
        {
            if (index < 0 || index >= Entries.Length)
                return ref Unsafe.NullRef<UOFileIndex>();

            ref var entry = ref Entries[index];
            if (entry.Length <= 0 || entry.Offset < 0)
                return ref Unsafe.NullRef<UOFileIndex>();

            return ref entry;
        }

        public virtual void Dispose()
        {
            _reader?.Dispose();
            _reader = null;
        }
    }
}
