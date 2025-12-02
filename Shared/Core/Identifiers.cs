using System.Runtime.InteropServices;

namespace RealmOfReality.Shared.Core;

/// <summary>
/// Unique identifier for game entities
/// Combines timestamp, server ID, and sequence number for distributed ID generation
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct EntityId : IEquatable<EntityId>, IComparable<EntityId>
{
    private readonly ulong _value;
    
    public EntityId(ulong value) => _value = value;
    
    public ulong Value => _value;
    public bool IsValid => _value != 0;
    
    public static EntityId Invalid => new(0);
    
    public bool Equals(EntityId other) => _value == other._value;
    public override bool Equals(object? obj) => obj is EntityId other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();
    public int CompareTo(EntityId other) => _value.CompareTo(other._value);
    public override string ToString() => $"E:{_value:X16}";
    
    public static bool operator ==(EntityId a, EntityId b) => a._value == b._value;
    public static bool operator !=(EntityId a, EntityId b) => a._value != b._value;
    
    public static implicit operator ulong(EntityId id) => id._value;
    public static explicit operator EntityId(ulong value) => new(value);
}

/// <summary>
/// Thread-safe unique ID generator using snowflake-like algorithm
/// Format: [timestamp 41 bits][server 10 bits][sequence 13 bits]
/// </summary>
public sealed class IdGenerator
{
    private const long Epoch = 1704067200000L; // 2024-01-01 00:00:00 UTC
    private const int ServerIdBits = 10;
    private const int SequenceBits = 13;
    
    private const long MaxServerId = (1L << ServerIdBits) - 1;
    private const long MaxSequence = (1L << SequenceBits) - 1;
    
    private readonly ushort _serverId;
    private readonly object _lock = new();
    
    private long _lastTimestamp = -1;
    private long _sequence = 0;
    
    public IdGenerator(ushort serverId)
    {
        if (serverId > MaxServerId)
            throw new ArgumentException($"Server ID must be between 0 and {MaxServerId}", nameof(serverId));
        _serverId = serverId;
    }
    
    public EntityId Generate()
    {
        lock (_lock)
        {
            var timestamp = GetTimestamp();
            
            if (timestamp < _lastTimestamp)
            {
                // Clock went backwards, wait until we catch up
                timestamp = WaitForNextMillis(_lastTimestamp);
            }
            
            if (timestamp == _lastTimestamp)
            {
                _sequence = (_sequence + 1) & MaxSequence;
                if (_sequence == 0)
                {
                    // Sequence overflow, wait for next millisecond
                    timestamp = WaitForNextMillis(_lastTimestamp);
                }
            }
            else
            {
                _sequence = 0;
            }
            
            _lastTimestamp = timestamp;
            
            var id = ((timestamp - Epoch) << (ServerIdBits + SequenceBits))
                   | ((long)_serverId << SequenceBits)
                   | _sequence;
            
            return new EntityId((ulong)id);
        }
    }
    
    /// <summary>
    /// Generate multiple IDs efficiently
    /// </summary>
    public EntityId[] GenerateBatch(int count)
    {
        var ids = new EntityId[count];
        for (var i = 0; i < count; i++)
            ids[i] = Generate();
        return ids;
    }
    
    private static long GetTimestamp() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    
    private static long WaitForNextMillis(long lastTimestamp)
    {
        var timestamp = GetTimestamp();
        while (timestamp <= lastTimestamp)
        {
            Thread.SpinWait(100);
            timestamp = GetTimestamp();
        }
        return timestamp;
    }
    
    /// <summary>
    /// Extract the timestamp from an entity ID
    /// </summary>
    public static DateTimeOffset GetCreationTime(EntityId id)
    {
        var timestamp = (long)(id.Value >> (ServerIdBits + SequenceBits)) + Epoch;
        return DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
    }
    
    /// <summary>
    /// Extract the server ID from an entity ID
    /// </summary>
    public static ushort GetServerId(EntityId id)
    {
        return (ushort)((id.Value >> SequenceBits) & MaxServerId);
    }
}

/// <summary>
/// Account ID (separate from entity IDs, used for persistent player accounts)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct AccountId : IEquatable<AccountId>
{
    private readonly uint _value;
    
    public AccountId(uint value) => _value = value;
    public uint Value => _value;
    public bool IsValid => _value != 0;
    
    public static AccountId Invalid => new(0);
    
    public bool Equals(AccountId other) => _value == other._value;
    public override bool Equals(object? obj) => obj is AccountId other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();
    public override string ToString() => $"A:{_value}";
    
    public static bool operator ==(AccountId a, AccountId b) => a._value == b._value;
    public static bool operator !=(AccountId a, AccountId b) => a._value != b._value;
}

/// <summary>
/// Character ID (for player characters, linked to accounts)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct CharacterId : IEquatable<CharacterId>
{
    private readonly uint _value;
    
    public CharacterId(uint value) => _value = value;
    public uint Value => _value;
    public bool IsValid => _value != 0;
    
    public static CharacterId Invalid => new(0);
    
    public bool Equals(CharacterId other) => _value == other._value;
    public override bool Equals(object? obj) => obj is CharacterId other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();
    public override string ToString() => $"C:{_value}";
    
    public static bool operator ==(CharacterId a, CharacterId b) => a._value == b._value;
    public static bool operator !=(CharacterId a, CharacterId b) => a._value != b._value;
}
