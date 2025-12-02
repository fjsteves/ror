using RealmOfReality.Shared.Core;
using RealmOfReality.Shared.Gumps;
using RealmOfReality.Shared.Serialization;

namespace RealmOfReality.Shared.Network;

/// <summary>
/// Packet opcodes for client-server communication
/// </summary>
public enum PacketOpcode : ushort
{
    // Connection (0x00-0x0F)
    Ping = 0x00,
    Pong = 0x01,
    Disconnect = 0x02,
    
    // Authentication (0x10-0x1F)
    LoginRequest = 0x10,
    LoginResponse = 0x11,
    LogoutRequest = 0x12,
    CreateAccountRequest = 0x13,
    CreateAccountResponse = 0x14,
    
    // Character Management (0x20-0x2F)
    CharacterListRequest = 0x20,
    CharacterList = 0x21,
    CreateCharacterRequest = 0x22,
    CreateCharacterResponse = 0x23,
    DeleteCharacterRequest = 0x24,
    DeleteCharacterResponse = 0x25,
    SelectCharacterRequest = 0x26,
    EnterWorld = 0x27,
    
    // World State (0x30-0x4F)
    WorldTime = 0x30,
    MapData = 0x31,
    EntitySpawn = 0x32,
    EntityDespawn = 0x33,
    EntityUpdate = 0x34,
    EntityMove = 0x35,
    PlayerStats = 0x36,
    AreaChange = 0x37,
    
    // Player Actions (0x50-0x6F)
    MoveRequest = 0x50,
    MoveConfirm = 0x51,
    MoveReject = 0x52,
    ActionRequest = 0x53,
    ActionResult = 0x54,
    InteractRequest = 0x55,
    InteractResult = 0x56,
    
    // Chat (0x70-0x7F)
    ChatMessage = 0x70,
    ChatBroadcast = 0x71,
    SystemMessage = 0x72,
    
    // Inventory (0x80-0x8F)
    InventoryFull = 0x80,
    InventoryUpdate = 0x81,
    ItemPickup = 0x82,
    ItemDrop = 0x83,
    ItemUse = 0x84,
    ItemEquip = 0x85,
    ItemUnequip = 0x86,
    
    // Combat (0x90-0x9F)
    AttackRequest = 0x90,
    DamageDealt = 0x91,
    DamageReceived = 0x92,
    Death = 0x93,
    Respawn = 0x94,
    ResurrectRequest = 0x95,
    
    // Skills (0xA0-0xAF)
    SkillList = 0xA0,
    SkillUse = 0xA1,
    SkillResult = 0xA2,
    SkillGain = 0xA3,
    
    // Gumps (0xB0-0xBF)
    GumpOpen = 0xB0,         // Server sends gump to client
    GumpResponse = 0xB1,     // Client responds to gump
    GumpClose = 0xB2,        // Server closes a gump
    GumpUpdate = 0xB3,       // Server updates an existing gump
    
    // Admin commands (0xF0-0xFF)
    AdminSpawnNpc = 0xF0,
    AdminKill = 0xF1,
    AdminHeal = 0xF2,
    AdminTeleport = 0xF3,
}

/// <summary>
/// Result codes for server responses
/// </summary>
public enum ResultCode : byte
{
    Success = 0,
    Failed = 1,
    InvalidCredentials = 2,
    AccountExists = 3,
    AccountNotFound = 4,
    CharacterNotFound = 5,
    CharacterNameTaken = 6,
    InvalidName = 7,
    ServerFull = 8,
    NotAuthorized = 9,
    Timeout = 10,
    InvalidState = 11,
    TooFar = 12,
    Blocked = 13,
    InCombat = 14,
    Dead = 15
}

/// <summary>
/// Base class for all network packets
/// </summary>
public abstract class Packet : ISerializable
{
    public abstract PacketOpcode Opcode { get; }
    
    public abstract void Serialize(PacketWriter writer);
    
    /// <summary>
    /// Build a complete packet with header
    /// </summary>
    public byte[] Build()
    {
        using var writer = new PacketWriter();
        // Reserve 4 bytes for length prefix (will be filled later)
        writer.WriteInt32(0);
        // Write opcode
        writer.WriteUInt16((ushort)Opcode);
        // Write packet body
        Serialize(writer);
        
        // Go back and write length
        var data = writer.ToArray();
        var length = data.Length - 4; // Exclude the length field itself
        BitConverter.TryWriteBytes(data, length);
        
        return data;
    }
}

/// <summary>
/// Packet header structure
/// </summary>
public readonly struct PacketHeader
{
    public int Length { get; }
    public PacketOpcode Opcode { get; }
    
    public PacketHeader(int length, PacketOpcode opcode)
    {
        Length = length;
        Opcode = opcode;
    }
    
    public const int Size = 6; // 4 bytes length + 2 bytes opcode
    
    public static PacketHeader Read(ref PacketReader reader)
    {
        var length = reader.ReadInt32();
        var opcode = (PacketOpcode)reader.ReadUInt16();
        return new PacketHeader(length, opcode);
    }
}

// ============ Connection Packets ============

public sealed class PingPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.Ping;
    public long ClientTime { get; init; }
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteInt64(ClientTime);
    }
    
    public static PingPacket Deserialize(ref PacketReader reader) =>
        new() { ClientTime = reader.ReadInt64() };
}

public sealed class PongPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.Pong;
    public long ClientTime { get; init; }
    public long ServerTime { get; init; }
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteInt64(ClientTime);
        writer.WriteInt64(ServerTime);
    }
    
    public static PongPacket Deserialize(ref PacketReader reader) =>
        new() { ClientTime = reader.ReadInt64(), ServerTime = reader.ReadInt64() };
}

public sealed class DisconnectPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.Disconnect;
    public string Reason { get; init; } = "";
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteString(Reason);
    }
    
    public static DisconnectPacket Deserialize(ref PacketReader reader) =>
        new() { Reason = reader.ReadString() };
}

// ============ Authentication Packets ============

public sealed class LoginRequestPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.LoginRequest;
    public string Username { get; init; } = "";
    public string PasswordHash { get; init; } = ""; // SHA256 hash, not plaintext
    public string ClientVersion { get; init; } = "";
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteString(Username);
        writer.WriteString(PasswordHash);
        writer.WriteString(ClientVersion);
    }
    
    public static LoginRequestPacket Deserialize(ref PacketReader reader) => new()
    {
        Username = reader.ReadString(),
        PasswordHash = reader.ReadString(),
        ClientVersion = reader.ReadString()
    };
}

public sealed class LoginResponsePacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.LoginResponse;
    public ResultCode Result { get; init; }
    public AccountId AccountId { get; init; }
    public string Message { get; init; } = "";
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteByte((byte)Result);
        writer.WriteAccountId(AccountId);
        writer.WriteString(Message);
    }
    
    public static LoginResponsePacket Deserialize(ref PacketReader reader) => new()
    {
        Result = (ResultCode)reader.ReadByte(),
        AccountId = reader.ReadAccountId(),
        Message = reader.ReadString()
    };
}

// ============ Character Management Packets ============

public sealed class CharacterListRequestPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.CharacterListRequest;
    public override void Serialize(PacketWriter writer) { }
    public static CharacterListRequestPacket Deserialize(ref PacketReader reader) => new();
}

/// <summary>
/// Brief character info for the character selection screen
/// </summary>
public sealed class CharacterListEntry : ISerializable
{
    public CharacterId Id { get; init; }
    public string Name { get; init; } = "";
    public byte Level { get; init; }
    public string Location { get; init; } = "";
    
    public void Serialize(PacketWriter writer)
    {
        writer.WriteCharacterId(Id);
        writer.WriteString(Name);
        writer.WriteByte(Level);
        writer.WriteString(Location);
    }
    
    public static CharacterListEntry Deserialize(ref PacketReader reader) => new()
    {
        Id = reader.ReadCharacterId(),
        Name = reader.ReadString(),
        Level = reader.ReadByte(),
        Location = reader.ReadString()
    };
}

public sealed class CharacterListPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.CharacterList;
    public List<CharacterListEntry> Characters { get; init; } = new();
    public byte MaxCharacters { get; init; } = 5;
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteByte((byte)Characters.Count);
        writer.WriteByte(MaxCharacters);
        foreach (var c in Characters)
            c.Serialize(writer);
    }
    
    public static CharacterListPacket Deserialize(ref PacketReader reader)
    {
        var count = reader.ReadByte();
        var max = reader.ReadByte();
        var chars = new List<CharacterListEntry>(count);
        for (var i = 0; i < count; i++)
            chars.Add(CharacterListEntry.Deserialize(ref reader));
        return new() { Characters = chars, MaxCharacters = max };
    }
}

public sealed class CreateCharacterRequestPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.CreateCharacterRequest;
    public string Name { get; init; } = "";
    public byte Gender { get; init; } // 0 = male, 1 = female
    public ushort BodyType { get; init; }
    public ushort SkinHue { get; init; }
    public ushort HairStyle { get; init; }
    public ushort HairHue { get; init; }
    
    // Starting stat allocation (must sum to a fixed total)
    public byte Strength { get; init; }
    public byte Dexterity { get; init; }
    public byte Intelligence { get; init; }
    
    // Starting skill selection
    public ushort StartingSkill1 { get; init; }
    public ushort StartingSkill2 { get; init; }
    public ushort StartingSkill3 { get; init; }
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteString(Name);
        writer.WriteByte(Gender);
        writer.WriteUInt16(BodyType);
        writer.WriteUInt16(SkinHue);
        writer.WriteUInt16(HairStyle);
        writer.WriteUInt16(HairHue);
        writer.WriteByte(Strength);
        writer.WriteByte(Dexterity);
        writer.WriteByte(Intelligence);
        writer.WriteUInt16(StartingSkill1);
        writer.WriteUInt16(StartingSkill2);
        writer.WriteUInt16(StartingSkill3);
    }
    
    public static CreateCharacterRequestPacket Deserialize(ref PacketReader reader) => new()
    {
        Name = reader.ReadString(),
        Gender = reader.ReadByte(),
        BodyType = reader.ReadUInt16(),
        SkinHue = reader.ReadUInt16(),
        HairStyle = reader.ReadUInt16(),
        HairHue = reader.ReadUInt16(),
        Strength = reader.ReadByte(),
        Dexterity = reader.ReadByte(),
        Intelligence = reader.ReadByte(),
        StartingSkill1 = reader.ReadUInt16(),
        StartingSkill2 = reader.ReadUInt16(),
        StartingSkill3 = reader.ReadUInt16()
    };
}

public sealed class CreateCharacterResponsePacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.CreateCharacterResponse;
    public ResultCode Result { get; init; }
    public CharacterId CharacterId { get; init; }
    public string Message { get; init; } = "";
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteByte((byte)Result);
        writer.WriteCharacterId(CharacterId);
        writer.WriteString(Message);
    }
    
    public static CreateCharacterResponsePacket Deserialize(ref PacketReader reader) => new()
    {
        Result = (ResultCode)reader.ReadByte(),
        CharacterId = reader.ReadCharacterId(),
        Message = reader.ReadString()
    };
}

public sealed class SelectCharacterRequestPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.SelectCharacterRequest;
    public CharacterId CharacterId { get; init; }
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteCharacterId(CharacterId);
    }
    
    public static SelectCharacterRequestPacket Deserialize(ref PacketReader reader) => new()
    {
        CharacterId = reader.ReadCharacterId()
    };
}

// ============ World State Packets ============

public sealed class EnterWorldPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.EnterWorld;
    public EntityId PlayerEntityId { get; init; }
    public WorldPosition Position { get; init; }
    public Direction Facing { get; init; }
    public ushort MapId { get; init; }
    public long ServerTick { get; init; }
    public byte AccessLevel { get; init; } // Player's access level (0=Player, 2=GM, 4=Admin, 6=Owner)
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteEntityId(PlayerEntityId);
        writer.WriteWorldPosition(Position);
        writer.WriteDirection(Facing);
        writer.WriteUInt16(MapId);
        writer.WriteInt64(ServerTick);
        writer.WriteByte(AccessLevel);
    }
    
    public static EnterWorldPacket Deserialize(ref PacketReader reader) => new()
    {
        PlayerEntityId = reader.ReadEntityId(),
        Position = reader.ReadWorldPosition(),
        Facing = reader.ReadDirection(),
        MapId = reader.ReadUInt16(),
        ServerTick = reader.ReadInt64(),
        AccessLevel = reader.ReadByte()
    };
}

public sealed class EntitySpawnPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.EntitySpawn;
    public EntityId EntityId { get; init; }
    public ushort EntityType { get; init; } // Type ID for graphics/behavior
    public WorldPosition Position { get; init; }
    public Direction Facing { get; init; }
    public string Name { get; init; } = "";
    public ushort BodyHue { get; init; }
    public byte Flags { get; init; } // Visibility, status effects, etc.
    public int Health { get; init; }
    public int MaxHealth { get; init; }
    public int Level { get; init; }
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteEntityId(EntityId);
        writer.WriteUInt16(EntityType);
        writer.WriteWorldPosition(Position);
        writer.WriteDirection(Facing);
        writer.WriteString(Name);
        writer.WriteUInt16(BodyHue);
        writer.WriteByte(Flags);
        writer.WriteInt32(Health);
        writer.WriteInt32(MaxHealth);
        writer.WriteInt32(Level);
    }
    
    public static EntitySpawnPacket Deserialize(ref PacketReader reader) => new()
    {
        EntityId = reader.ReadEntityId(),
        EntityType = reader.ReadUInt16(),
        Position = reader.ReadWorldPosition(),
        Facing = reader.ReadDirection(),
        Name = reader.ReadString(),
        BodyHue = reader.ReadUInt16(),
        Flags = reader.ReadByte(),
        Health = reader.ReadInt32(),
        MaxHealth = reader.ReadInt32(),
        Level = reader.ReadInt32()
    };
}

public sealed class EntityDespawnPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.EntityDespawn;
    public EntityId EntityId { get; init; }
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteEntityId(EntityId);
    }
    
    public static EntityDespawnPacket Deserialize(ref PacketReader reader) => new()
    {
        EntityId = reader.ReadEntityId()
    };
}

public sealed class EntityMovePacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.EntityMove;
    public EntityId EntityId { get; init; }
    public WorldPosition Position { get; init; }
    public Direction Facing { get; init; }
    public byte MoveType { get; init; } // 0=walk, 1=run, 2=teleport
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteEntityId(EntityId);
        writer.WriteWorldPosition(Position);
        writer.WriteDirection(Facing);
        writer.WriteByte(MoveType);
    }
    
    public static EntityMovePacket Deserialize(ref PacketReader reader) => new()
    {
        EntityId = reader.ReadEntityId(),
        Position = reader.ReadWorldPosition(),
        Facing = reader.ReadDirection(),
        MoveType = reader.ReadByte()
    };
}

// ============ Player Action Packets ============

public sealed class MoveRequestPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.MoveRequest;
    public Direction Direction { get; init; }
    public bool Running { get; init; }
    public byte SequenceNumber { get; init; } // For client-side prediction reconciliation
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteDirection(Direction);
        writer.WriteBool(Running);
        writer.WriteByte(SequenceNumber);
    }
    
    public static MoveRequestPacket Deserialize(ref PacketReader reader) => new()
    {
        Direction = reader.ReadDirection(),
        Running = reader.ReadBool(),
        SequenceNumber = reader.ReadByte()
    };
}

public sealed class MoveConfirmPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.MoveConfirm;
    public byte SequenceNumber { get; init; }
    public WorldPosition Position { get; init; }
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteByte(SequenceNumber);
        writer.WriteWorldPosition(Position);
    }
    
    public static MoveConfirmPacket Deserialize(ref PacketReader reader) => new()
    {
        SequenceNumber = reader.ReadByte(),
        Position = reader.ReadWorldPosition()
    };
}

// ============ Chat Packets ============

public enum ChatChannel : byte
{
    Local = 0,    // Nearby players
    Global = 1,   // All players
    Party = 2,    // Party members
    Guild = 3,    // Guild members
    Whisper = 4,  // Direct message
    System = 5    // System messages
}

public sealed class ChatMessagePacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.ChatMessage;
    public ChatChannel Channel { get; init; }
    public string Message { get; init; } = "";
    public string? TargetName { get; init; } // For whispers
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteByte((byte)Channel);
        writer.WriteString(Message);
        writer.WriteString(TargetName ?? "");
    }
    
    public static ChatMessagePacket Deserialize(ref PacketReader reader) => new()
    {
        Channel = (ChatChannel)reader.ReadByte(),
        Message = reader.ReadString(),
        TargetName = reader.ReadString() is var s && string.IsNullOrEmpty(s) ? null : s
    };
}

public sealed class ChatBroadcastPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.ChatBroadcast;
    public ChatChannel Channel { get; init; }
    public EntityId SenderEntityId { get; init; }
    public string SenderName { get; init; } = "";
    public string Message { get; init; } = "";
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteByte((byte)Channel);
        writer.WriteEntityId(SenderEntityId);
        writer.WriteString(SenderName);
        writer.WriteString(Message);
    }
    
    public static ChatBroadcastPacket Deserialize(ref PacketReader reader) => new()
    {
        Channel = (ChatChannel)reader.ReadByte(),
        SenderEntityId = reader.ReadEntityId(),
        SenderName = reader.ReadString(),
        Message = reader.ReadString()
    };
}

public sealed class SystemMessagePacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.SystemMessage;
    public string Message { get; init; } = "";
    public Color TextColor { get; init; } = Color.White;
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteString(Message);
        writer.WriteColor(TextColor);
    }
    
    public static SystemMessagePacket Deserialize(ref PacketReader reader) => new()
    {
        Message = reader.ReadString(),
        TextColor = reader.ReadColor()
    };
}

// ==================== COMBAT PACKETS ====================

public sealed class AttackRequestPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.AttackRequest;
    public EntityId TargetId { get; init; }
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteEntityId(TargetId);
    }
    
    public static AttackRequestPacket Deserialize(ref PacketReader reader) => new()
    {
        TargetId = reader.ReadEntityId()
    };
}

public sealed class DamageDealtPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.DamageDealt;
    public EntityId AttackerId { get; init; }
    public EntityId TargetId { get; init; }
    public int Damage { get; init; }
    public DamageType DamageType { get; init; }
    public bool IsCritical { get; init; }
    public int TargetHealth { get; init; }
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteEntityId(AttackerId);
        writer.WriteEntityId(TargetId);
        writer.WriteInt32(Damage);
        writer.WriteByte((byte)DamageType);
        writer.WriteBool(IsCritical);
        writer.WriteInt32(TargetHealth);
    }
    
    public static DamageDealtPacket Deserialize(ref PacketReader reader) => new()
    {
        AttackerId = reader.ReadEntityId(),
        TargetId = reader.ReadEntityId(),
        Damage = reader.ReadInt32(),
        DamageType = (DamageType)reader.ReadByte(),
        IsCritical = reader.ReadBool(),
        TargetHealth = reader.ReadInt32()
    };
}

public sealed class DeathPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.Death;
    public EntityId EntityId { get; init; }
    public EntityId KillerId { get; init; }
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteEntityId(EntityId);
        writer.WriteEntityId(KillerId);
    }
    
    public static DeathPacket Deserialize(ref PacketReader reader) => new()
    {
        EntityId = reader.ReadEntityId(),
        KillerId = reader.ReadEntityId()
    };
}

public sealed class CastSpellRequestPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.SkillUse;
    public ushort SpellId { get; init; }
    public EntityId? TargetId { get; init; }
    public WorldPosition? TargetPosition { get; init; }
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteUInt16(SpellId);
        writer.WriteBool(TargetId.HasValue);
        if (TargetId.HasValue)
            writer.WriteEntityId(TargetId.Value);
        writer.WriteBool(TargetPosition.HasValue);
        if (TargetPosition.HasValue)
            writer.WriteWorldPosition(TargetPosition.Value);
    }
    
    public static CastSpellRequestPacket Deserialize(ref PacketReader reader)
    {
        var spellId = reader.ReadUInt16();
        var hasTarget = reader.ReadBool();
        EntityId? targetId = hasTarget ? reader.ReadEntityId() : null;
        var hasPos = reader.ReadBool();
        WorldPosition? targetPos = hasPos ? reader.ReadWorldPosition() : null;
        return new() { SpellId = spellId, TargetId = targetId, TargetPosition = targetPos };
    }
}

public sealed class SpellEffectPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.SkillResult;
    public EntityId CasterId { get; init; }
    public ushort SpellId { get; init; }
    public EntityId? TargetId { get; init; }
    public WorldPosition TargetPosition { get; init; }
    public int Damage { get; init; }
    public int Healing { get; init; }
    public bool Success { get; init; }
    public string? FailReason { get; init; }
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteEntityId(CasterId);
        writer.WriteUInt16(SpellId);
        writer.WriteBool(TargetId.HasValue);
        if (TargetId.HasValue)
            writer.WriteEntityId(TargetId.Value);
        writer.WriteWorldPosition(TargetPosition);
        writer.WriteInt32(Damage);
        writer.WriteInt32(Healing);
        writer.WriteBool(Success);
        writer.WriteString(FailReason ?? "");
    }
    
    public static SpellEffectPacket Deserialize(ref PacketReader reader)
    {
        var caster = reader.ReadEntityId();
        var spell = reader.ReadUInt16();
        var hasTarget = reader.ReadBool();
        EntityId? target = hasTarget ? reader.ReadEntityId() : null;
        var pos = reader.ReadWorldPosition();
        var dmg = reader.ReadInt32();
        var heal = reader.ReadInt32();
        var success = reader.ReadBool();
        var reason = reader.ReadString();
        return new() { CasterId = caster, SpellId = spell, TargetId = target, TargetPosition = pos, Damage = dmg, Healing = heal, Success = success, FailReason = string.IsNullOrEmpty(reason) ? null : reason };
    }
}

/// <summary>
/// Request to resurrect (client to server)
/// </summary>
public sealed class ResurrectRequestPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.ResurrectRequest;
    
    public override void Serialize(PacketWriter writer) { }
    
    public static ResurrectRequestPacket Deserialize(ref PacketReader reader)
    {
        return new ResurrectRequestPacket();
    }
}

/// <summary>
/// Admin spawn NPC command
/// </summary>
public sealed class AdminSpawnNpcPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.AdminSpawnNpc;
    public string Name { get; init; } = "";
    public int TypeId { get; init; }
    public WorldPosition Position { get; init; }
    public int Level { get; init; }
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteString(Name);
        writer.WriteInt32(TypeId);
        writer.WriteWorldPosition(Position);
        writer.WriteInt32(Level);
    }
    
    public static AdminSpawnNpcPacket Deserialize(ref PacketReader reader)
    {
        return new AdminSpawnNpcPacket
        {
            Name = reader.ReadString(),
            TypeId = reader.ReadInt32(),
            Position = reader.ReadWorldPosition(),
            Level = reader.ReadInt32()
        };
    }
}

/// <summary>
/// Admin kill command
/// </summary>
public sealed class AdminKillPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.AdminKill;
    public EntityId TargetId { get; init; }
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteEntityId(TargetId);
    }
    
    public static AdminKillPacket Deserialize(ref PacketReader reader)
    {
        return new AdminKillPacket
        {
            TargetId = reader.ReadEntityId()
        };
    }
}

/// <summary>
/// Admin heal command
/// </summary>
public sealed class AdminHealPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.AdminHeal;
    
    public override void Serialize(PacketWriter writer) { }
    
    public static AdminHealPacket Deserialize(ref PacketReader reader)
    {
        return new AdminHealPacket();
    }
}

/// <summary>
/// Admin teleport command
/// </summary>
public sealed class AdminTeleportPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.AdminTeleport;
    public WorldPosition Position { get; init; }
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteWorldPosition(Position);
    }
    
    public static AdminTeleportPacket Deserialize(ref PacketReader reader)
    {
        return new AdminTeleportPacket
        {
            Position = reader.ReadWorldPosition()
        };
    }
}

// ============ Gump Packets ============

/// <summary>
/// Server sends a gump to the client
/// </summary>
public sealed class GumpOpenPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.GumpOpen;
    
    public Gumps.GumpData? GumpData { get; set; }
    
    public override void Serialize(PacketWriter writer)
    {
        GumpData?.Serialize(writer);
    }
    
    public static GumpOpenPacket Deserialize(ref PacketReader reader)
    {
        return new GumpOpenPacket
        {
            GumpData = Gumps.GumpData.Deserialize(ref reader)
        };
    }
}

/// <summary>
/// Client responds to a gump interaction
/// </summary>
public sealed class GumpResponsePacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.GumpResponse;
    
    public Gumps.GumpResponse? Response { get; set; }
    
    public override void Serialize(PacketWriter writer)
    {
        Response?.Serialize(writer);
    }
    
    public static GumpResponsePacket Deserialize(ref PacketReader reader)
    {
        return new GumpResponsePacket
        {
            Response = Gumps.GumpResponse.Deserialize(ref reader)
        };
    }
}

/// <summary>
/// Server closes a gump on the client
/// </summary>
public sealed class GumpClosePacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.GumpClose;
    
    public uint GumpTypeId { get; set; }
    public uint Serial { get; set; }
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteUInt32(GumpTypeId);
        writer.WriteUInt32(Serial);
    }
    
    public static GumpClosePacket Deserialize(ref PacketReader reader)
    {
        return new GumpClosePacket
        {
            GumpTypeId = reader.ReadUInt32(),
            Serial = reader.ReadUInt32()
        };
    }
}

/// <summary>
/// Client requests to use an item (double-click)
/// </summary>
public sealed class ItemUsePacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.ItemUse;
    
    public ulong ItemSerial { get; set; }
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteUInt64(ItemSerial);
    }
    
    public static ItemUsePacket Deserialize(ref PacketReader reader)
    {
        return new ItemUsePacket
        {
            ItemSerial = reader.ReadUInt64()
        };
    }
}

/// <summary>
/// Client requests to equip an item from inventory
/// </summary>
public sealed class ItemEquipPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.ItemEquip;
    
    public ulong ItemSerial { get; set; }
    public byte Layer { get; set; }  // 0 = auto-detect from item
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteUInt64(ItemSerial);
        writer.WriteByte(Layer);
    }
    
    public static ItemEquipPacket Deserialize(ref PacketReader reader)
    {
        return new ItemEquipPacket
        {
            ItemSerial = reader.ReadUInt64(),
            Layer = reader.ReadByte()
        };
    }
}

/// <summary>
/// Client requests to unequip an item
/// </summary>
public sealed class ItemUnequipPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.ItemUnequip;
    
    public ulong ItemSerial { get; set; }
    public byte Layer { get; set; }
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteUInt64(ItemSerial);
        writer.WriteByte(Layer);
    }
    
    public static ItemUnequipPacket Deserialize(ref PacketReader reader)
    {
        return new ItemUnequipPacket
        {
            ItemSerial = reader.ReadUInt64(),
            Layer = reader.ReadByte()
        };
    }
}

/// <summary>
/// Server sends full inventory data to client
/// </summary>
public sealed class InventoryFullPacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.InventoryFull;
    
    public List<InventoryItemData> Items { get; } = new();
    public List<EquipmentItemData> Equipment { get; } = new();
    
    public override void Serialize(PacketWriter writer)
    {
        // Inventory items
        writer.WriteUInt16((ushort)Items.Count);
        foreach (var item in Items)
        {
            item.Serialize(writer);
        }
        
        // Equipment
        writer.WriteByte((byte)Equipment.Count);
        foreach (var equip in Equipment)
        {
            equip.Serialize(writer);
        }
    }
    
    public static InventoryFullPacket Deserialize(ref PacketReader reader)
    {
        var packet = new InventoryFullPacket();
        
        var itemCount = reader.ReadUInt16();
        for (int i = 0; i < itemCount; i++)
        {
            packet.Items.Add(InventoryItemData.Deserialize(ref reader));
        }
        
        var equipCount = reader.ReadByte();
        for (int i = 0; i < equipCount; i++)
        {
            packet.Equipment.Add(EquipmentItemData.Deserialize(ref reader));
        }
        
        return packet;
    }
}

/// <summary>
/// Server sends inventory update (item added/removed/changed)
/// </summary>
public sealed class InventoryUpdatePacket : Packet
{
    public override PacketOpcode Opcode => PacketOpcode.InventoryUpdate;
    
    public InventoryUpdateType UpdateType { get; set; }
    public InventoryItemData? Item { get; set; }
    public int SlotIndex { get; set; }
    
    public override void Serialize(PacketWriter writer)
    {
        writer.WriteByte((byte)UpdateType);
        writer.WriteInt16((short)SlotIndex);
        if (UpdateType != InventoryUpdateType.Remove && Item != null)
        {
            Item.Serialize(writer);
        }
    }
    
    public static InventoryUpdatePacket Deserialize(ref PacketReader reader)
    {
        var packet = new InventoryUpdatePacket
        {
            UpdateType = (InventoryUpdateType)reader.ReadByte(),
            SlotIndex = reader.ReadInt16()
        };
        
        if (packet.UpdateType != InventoryUpdateType.Remove)
        {
            packet.Item = InventoryItemData.Deserialize(ref reader);
        }
        
        return packet;
    }
}

/// <summary>
/// Inventory update types
/// </summary>
public enum InventoryUpdateType : byte
{
    Add = 0,
    Remove = 1,
    Update = 2,
    Equip = 3,
    Unequip = 4
}

/// <summary>
/// Inventory item data for network transfer
/// </summary>
public class InventoryItemData : ISerializable
{
    public ulong Serial { get; set; }
    public ushort ItemId { get; set; }
    public ushort Hue { get; set; }
    public int Amount { get; set; }
    public int SlotIndex { get; set; }
    public string Name { get; set; } = "";
    
    public void Serialize(PacketWriter writer)
    {
        writer.WriteUInt64(Serial);
        writer.WriteUInt16(ItemId);
        writer.WriteUInt16(Hue);
        writer.WriteInt32(Amount);
        writer.WriteInt16((short)SlotIndex);
        writer.WriteString(Name);
    }
    
    public static InventoryItemData Deserialize(ref PacketReader reader)
    {
        return new InventoryItemData
        {
            Serial = reader.ReadUInt64(),
            ItemId = reader.ReadUInt16(),
            Hue = reader.ReadUInt16(),
            Amount = reader.ReadInt32(),
            SlotIndex = reader.ReadInt16(),
            Name = reader.ReadString()
        };
    }
}

/// <summary>
/// Equipment item data for network transfer
/// </summary>
public class EquipmentItemData : ISerializable
{
    public ulong Serial { get; set; }
    public ushort ItemId { get; set; }
    public ushort GumpId { get; set; }
    public ushort Hue { get; set; }
    public byte Layer { get; set; }
    public string Name { get; set; } = "";
    
    public void Serialize(PacketWriter writer)
    {
        writer.WriteUInt64(Serial);
        writer.WriteUInt16(ItemId);
        writer.WriteUInt16(GumpId);
        writer.WriteUInt16(Hue);
        writer.WriteByte(Layer);
        writer.WriteString(Name);
    }
    
    public static EquipmentItemData Deserialize(ref PacketReader reader)
    {
        return new EquipmentItemData
        {
            Serial = reader.ReadUInt64(),
            ItemId = reader.ReadUInt16(),
            GumpId = reader.ReadUInt16(),
            Hue = reader.ReadUInt16(),
            Layer = reader.ReadByte(),
            Name = reader.ReadString()
        };
    }
}

/// <summary>
/// Damage types for combat
/// </summary>
public enum DamageType : byte
{
    Physical = 0,
    Fire = 1,
    Cold = 2,
    Poison = 3,
    Energy = 4,
    Holy = 5,
    Shadow = 6
}
