using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;
using RealmOfReality.Shared.Core;

namespace RealmOfReality.Shared.Serialization;

/// <summary>
/// High-performance binary writer for network packets and world data
/// Uses pooled buffers for zero-allocation writing
/// </summary>
public sealed class PacketWriter : IDisposable
{
    private byte[] _buffer;
    private int _position;
    private bool _disposed;
    
    public PacketWriter(int initialCapacity = 256)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
        _position = 0;
    }
    
    public int Length => _position;
    public int Capacity => _buffer.Length;
    
    /// <summary>
    /// Get the written data as a span
    /// </summary>
    public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _position);
    
    /// <summary>
    /// Get a copy of the written data
    /// </summary>
    public byte[] ToArray()
    {
        var result = new byte[_position];
        Buffer.BlockCopy(_buffer, 0, result, 0, _position);
        return result;
    }
    
    /// <summary>
    /// Reset the writer for reuse
    /// </summary>
    public void Reset() => _position = 0;
    
    private void EnsureCapacity(int additionalBytes)
    {
        var required = _position + additionalBytes;
        if (required <= _buffer.Length) return;
        
        var newSize = Math.Max(_buffer.Length * 2, required);
        var newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _position);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value)
    {
        EnsureCapacity(1);
        _buffer[_position++] = value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSByte(sbyte value) => WriteByte((byte)value);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBool(bool value) => WriteByte(value ? (byte)1 : (byte)0);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt16(short value)
    {
        EnsureCapacity(2);
        BinaryPrimitives.WriteInt16LittleEndian(_buffer.AsSpan(_position), value);
        _position += 2;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt16(ushort value)
    {
        EnsureCapacity(2);
        BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(_position), value);
        _position += 2;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt32(int value)
    {
        EnsureCapacity(4);
        BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(_position), value);
        _position += 4;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt32(uint value)
    {
        EnsureCapacity(4);
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(_position), value);
        _position += 4;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt64(long value)
    {
        EnsureCapacity(8);
        BinaryPrimitives.WriteInt64LittleEndian(_buffer.AsSpan(_position), value);
        _position += 8;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt64(ulong value)
    {
        EnsureCapacity(8);
        BinaryPrimitives.WriteUInt64LittleEndian(_buffer.AsSpan(_position), value);
        _position += 8;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSingle(float value)
    {
        EnsureCapacity(4);
        BinaryPrimitives.WriteSingleLittleEndian(_buffer.AsSpan(_position), value);
        _position += 4;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteDouble(double value)
    {
        EnsureCapacity(8);
        BinaryPrimitives.WriteDoubleLittleEndian(_buffer.AsSpan(_position), value);
        _position += 8;
    }
    
    /// <summary>
    /// Write a length-prefixed UTF-8 string (max 65535 bytes)
    /// </summary>
    public void WriteString(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            WriteUInt16(0);
            return;
        }
        
        var byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount > ushort.MaxValue)
            throw new ArgumentException($"String too long: {byteCount} bytes (max {ushort.MaxValue})");
        
        WriteUInt16((ushort)byteCount);
        EnsureCapacity(byteCount);
        Encoding.UTF8.GetBytes(value, _buffer.AsSpan(_position));
        _position += byteCount;
    }
    
    /// <summary>
    /// Write raw bytes
    /// </summary>
    public void WriteBytes(ReadOnlySpan<byte> data)
    {
        EnsureCapacity(data.Length);
        data.CopyTo(_buffer.AsSpan(_position));
        _position += data.Length;
    }
    
    /// <summary>
    /// Write a length-prefixed byte array
    /// </summary>
    public void WriteByteArray(ReadOnlySpan<byte> data)
    {
        WriteInt32(data.Length);
        WriteBytes(data);
    }
    
    // Game-specific types
    public void WriteEntityId(EntityId id) => WriteUInt64(id.Value);
    public void WriteAccountId(AccountId id) => WriteUInt32(id.Value);
    public void WriteCharacterId(CharacterId id) => WriteUInt32(id.Value);
    
    public void WriteTilePosition(TilePosition pos)
    {
        WriteInt32(pos.X);
        WriteInt32(pos.Y);
    }
    
    public void WriteWorldPosition(WorldPosition pos)
    {
        WriteSingle(pos.X);
        WriteSingle(pos.Y);
        WriteSingle(pos.Z);
    }
    
    public void WriteDirection(Direction dir) => WriteByte((byte)dir);
    
    public void WriteColor(Color color)
    {
        WriteByte(color.R);
        WriteByte(color.G);
        WriteByte(color.B);
        WriteByte(color.A);
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        ArrayPool<byte>.Shared.Return(_buffer);
        _disposed = true;
    }
}

/// <summary>
/// High-performance binary reader for network packets and world data
/// </summary>
public ref struct PacketReader
{
    private readonly ReadOnlySpan<byte> _data;
    private int _position;
    
    public PacketReader(ReadOnlySpan<byte> data)
    {
        _data = data;
        _position = 0;
    }
    
    public int Position => _position;
    public int Remaining => _data.Length - _position;
    public bool EndOfData => _position >= _data.Length;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte()
    {
        if (_position >= _data.Length)
            throw new EndOfStreamException();
        return _data[_position++];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadSByte() => (sbyte)ReadByte();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool() => ReadByte() != 0;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadInt16()
    {
        var value = BinaryPrimitives.ReadInt16LittleEndian(_data.Slice(_position));
        _position += 2;
        return value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUInt16()
    {
        var value = BinaryPrimitives.ReadUInt16LittleEndian(_data.Slice(_position));
        _position += 2;
        return value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt32()
    {
        var value = BinaryPrimitives.ReadInt32LittleEndian(_data.Slice(_position));
        _position += 4;
        return value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt32()
    {
        var value = BinaryPrimitives.ReadUInt32LittleEndian(_data.Slice(_position));
        _position += 4;
        return value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadInt64()
    {
        var value = BinaryPrimitives.ReadInt64LittleEndian(_data.Slice(_position));
        _position += 8;
        return value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadUInt64()
    {
        var value = BinaryPrimitives.ReadUInt64LittleEndian(_data.Slice(_position));
        _position += 8;
        return value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadSingle()
    {
        var value = BinaryPrimitives.ReadSingleLittleEndian(_data.Slice(_position));
        _position += 4;
        return value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDouble()
    {
        var value = BinaryPrimitives.ReadDoubleLittleEndian(_data.Slice(_position));
        _position += 8;
        return value;
    }
    
    public string ReadString()
    {
        var length = ReadUInt16();
        if (length == 0) return string.Empty;
        
        var value = Encoding.UTF8.GetString(_data.Slice(_position, length));
        _position += length;
        return value;
    }
    
    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        var data = _data.Slice(_position, count);
        _position += count;
        return data;
    }
    
    public byte[] ReadByteArray()
    {
        var length = ReadInt32();
        return ReadBytes(length).ToArray();
    }
    
    public void Skip(int count) => _position += count;
    
    // Game-specific types
    public EntityId ReadEntityId() => new(ReadUInt64());
    public AccountId ReadAccountId() => new(ReadUInt32());
    public CharacterId ReadCharacterId() => new(ReadUInt32());
    
    public TilePosition ReadTilePosition() => new(ReadInt32(), ReadInt32());
    
    public WorldPosition ReadWorldPosition() => new(ReadSingle(), ReadSingle(), ReadSingle());
    
    public Direction ReadDirection() => (Direction)ReadByte();
    
    public Color ReadColor() => new(ReadByte(), ReadByte(), ReadByte(), ReadByte());
}

/// <summary>
/// Interface for types that can serialize themselves
/// </summary>
public interface ISerializable
{
    void Serialize(PacketWriter writer);
}

/// <summary>
/// Interface for types that can deserialize themselves
/// </summary>
public interface IDeserializable<T>
{
    static abstract T Deserialize(ref PacketReader reader);
}
