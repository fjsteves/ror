// SPDX-License-Identifier: BSD-2-Clause
// Minimal implementation for RealmOfReality

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ClassicUO.IO
{
    /// <summary>
    /// Stack-allocated binary data reader for efficient parsing.
    /// </summary>
    public unsafe ref struct StackDataReader
    {
        private readonly ReadOnlySpan<byte> _buffer;
        private int _position;

        public StackDataReader(ReadOnlySpan<byte> buffer)
        {
            _buffer = buffer;
            _position = 0;
        }

        public StackDataReader(byte[] buffer) : this(buffer.AsSpan()) { }

        public int Position
        {
            get => _position;
            set => _position = Math.Clamp(value, 0, _buffer.Length);
        }

        public int Length => _buffer.Length;
        public int Remaining => _buffer.Length - _position;
        public ReadOnlySpan<byte> Buffer => _buffer;

        public byte* PositionAddress
        {
            get
            {
                fixed (byte* ptr = _buffer)
                    return ptr + _position;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Seek(int position) => _position = Math.Clamp(position, 0, _buffer.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Skip(int count) => _position = Math.Clamp(_position + count, 0, _buffer.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadUInt8()
        {
            if (_position >= _buffer.Length) return 0;
            return _buffer[_position++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadInt8()
        {
            if (_position >= _buffer.Length) return 0;
            return (sbyte)_buffer[_position++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16LE()
        {
            if (_position + 2 > _buffer.Length) return 0;
            var val = MemoryMarshal.Read<ushort>(_buffer.Slice(_position, 2));
            _position += 2;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16LE()
        {
            if (_position + 2 > _buffer.Length) return 0;
            var val = MemoryMarshal.Read<short>(_buffer.Slice(_position, 2));
            _position += 2;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32LE()
        {
            if (_position + 4 > _buffer.Length) return 0;
            var val = MemoryMarshal.Read<uint>(_buffer.Slice(_position, 4));
            _position += 4;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32LE()
        {
            if (_position + 4 > _buffer.Length) return 0;
            var val = MemoryMarshal.Read<int>(_buffer.Slice(_position, 4));
            _position += 4;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadUInt64LE()
        {
            if (_position + 8 > _buffer.Length) return 0;
            var val = MemoryMarshal.Read<ulong>(_buffer.Slice(_position, 8));
            _position += 8;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadInt64LE()
        {
            if (_position + 8 > _buffer.Length) return 0;
            var val = MemoryMarshal.Read<long>(_buffer.Slice(_position, 8));
            _position += 8;
            return val;
        }

        // Big-endian versions for network data
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32BE()
        {
            if (_position + 4 > _buffer.Length) return 0;
            var val = MemoryMarshal.Read<uint>(_buffer.Slice(_position, 4));
            _position += 4;
            return BitConverter.IsLittleEndian
                ? ((val >> 24) | ((val >> 8) & 0xFF00) | ((val << 8) & 0xFF0000) | (val << 24))
                : val;
        }

        public int Read(Span<byte> buffer)
        {
            int toRead = Math.Min(buffer.Length, _buffer.Length - _position);
            if (toRead <= 0) return 0;
            _buffer.Slice(_position, toRead).CopyTo(buffer);
            _position += toRead;
            return toRead;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>() where T : unmanaged
        {
            int size = Unsafe.SizeOf<T>();
            if (_position + size > _buffer.Length)
                return default;

            T val = MemoryMarshal.Read<T>(_buffer.Slice(_position, size));
            _position += size;
            return val;
        }
    }
}
