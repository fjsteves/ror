// SPDX-License-Identifier: BSD-2-Clause
// Minimal implementation for RealmOfReality

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ClassicUO.IO
{
    /// <summary>
    /// Memory-mapped file reader for efficient binary file access.
    /// </summary>
    public unsafe class FileReader : IDisposable
    {
        private MemoryMappedFile? _mmf;
        private MemoryMappedViewAccessor? _accessor;
        private byte* _ptr;
        private long _position;
        private readonly long _length;
        private bool _disposed;

        public long Position
        {
            get => _position;
            set => _position = Math.Clamp(value, 0, _length);
        }

        public long Length => _length;

        public byte* PositionAddress => _ptr + _position;
        public byte* StartAddress => _ptr;

        public FileReader(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _length = 0;
                return;
            }

            var fileInfo = new FileInfo(filePath);
            _length = fileInfo.Length;

            if (_length == 0)
                return;

            _mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            _accessor = _mmf.CreateViewAccessor(0, _length, MemoryMappedFileAccess.Read);
            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _ptr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Seek(long offset, SeekOrigin origin = SeekOrigin.Begin)
        {
            _position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _position + offset,
                SeekOrigin.End => _length + offset,
                _ => _position
            };
            _position = Math.Clamp(_position, 0, _length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Skip(int count) => _position += count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadUInt8()
        {
            if (_position >= _length) return 0;
            return _ptr[_position++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadInt8()
        {
            if (_position >= _length) return 0;
            return (sbyte)_ptr[_position++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            if (_position + 2 > _length) return 0;
            var val = *(ushort*)(_ptr + _position);
            _position += 2;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16()
        {
            if (_position + 2 > _length) return 0;
            var val = *(short*)(_ptr + _position);
            _position += 2;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            if (_position + 4 > _length) return 0;
            var val = *(uint*)(_ptr + _position);
            _position += 4;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            if (_position + 4 > _length) return 0;
            var val = *(int*)(_ptr + _position);
            _position += 4;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64()
        {
            if (_position + 8 > _length) return 0;
            var val = *(ulong*)(_ptr + _position);
            _position += 8;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64()
        {
            if (_position + 8 > _length) return 0;
            var val = *(long*)(_ptr + _position);
            _position += 8;
            return val;
        }

        public int Read(Span<byte> buffer)
        {
            int toRead = (int)Math.Min(buffer.Length, _length - _position);
            if (toRead <= 0) return 0;

            new ReadOnlySpan<byte>(_ptr + _position, toRead).CopyTo(buffer);
            _position += toRead;
            return toRead;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return Read(buffer.AsSpan(offset, count));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>() where T : unmanaged
        {
            int size = sizeof(T);
            if (_position + size > _length)
                return default;

            T val = *(T*)(_ptr + _position);
            _position += size;
            return val;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_accessor != null)
            {
                _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                _accessor.Dispose();
            }
            _mmf?.Dispose();
        }
    }
}
