using RealmOfReality.Shared.Serialization;

namespace RealmOfReality.Shared.Network;

/// <summary>
/// Factory for deserializing packets based on opcode
/// </summary>
public static class PacketFactory
{
    /// <summary>
    /// Deserialize a packet from raw data
    /// </summary>
    public static Packet? Deserialize(ReadOnlySpan<byte> data)
    {
        var reader = new PacketReader(data);
        var header = PacketHeader.Read(ref reader);
        
        return DeserializeByOpcode(header.Opcode, ref reader);
    }
    
    /// <summary>
    /// Deserialize from a reader (assumes header already read)
    /// </summary>
    public static Packet? DeserializeByOpcode(PacketOpcode opcode, ref PacketReader reader)
    {
        return opcode switch
        {
            // Connection
            PacketOpcode.Ping => PingPacket.Deserialize(ref reader),
            PacketOpcode.Pong => PongPacket.Deserialize(ref reader),
            PacketOpcode.Disconnect => DisconnectPacket.Deserialize(ref reader),
            
            // Authentication
            PacketOpcode.LoginRequest => LoginRequestPacket.Deserialize(ref reader),
            PacketOpcode.LoginResponse => LoginResponsePacket.Deserialize(ref reader),
            
            // Character Management
            PacketOpcode.CharacterListRequest => CharacterListRequestPacket.Deserialize(ref reader),
            PacketOpcode.CharacterList => CharacterListPacket.Deserialize(ref reader),
            PacketOpcode.CreateCharacterRequest => CreateCharacterRequestPacket.Deserialize(ref reader),
            PacketOpcode.CreateCharacterResponse => CreateCharacterResponsePacket.Deserialize(ref reader),
            PacketOpcode.SelectCharacterRequest => SelectCharacterRequestPacket.Deserialize(ref reader),
            PacketOpcode.EnterWorld => EnterWorldPacket.Deserialize(ref reader),
            
            // World State
            PacketOpcode.EntitySpawn => EntitySpawnPacket.Deserialize(ref reader),
            PacketOpcode.EntityDespawn => EntityDespawnPacket.Deserialize(ref reader),
            PacketOpcode.EntityMove => EntityMovePacket.Deserialize(ref reader),
            
            // Player Actions
            PacketOpcode.MoveRequest => MoveRequestPacket.Deserialize(ref reader),
            PacketOpcode.MoveConfirm => MoveConfirmPacket.Deserialize(ref reader),
            
            // Chat
            PacketOpcode.ChatMessage => ChatMessagePacket.Deserialize(ref reader),
            PacketOpcode.ChatBroadcast => ChatBroadcastPacket.Deserialize(ref reader),
            PacketOpcode.SystemMessage => SystemMessagePacket.Deserialize(ref reader),
            
            // Combat
            PacketOpcode.AttackRequest => AttackRequestPacket.Deserialize(ref reader),
            PacketOpcode.DamageDealt => DamageDealtPacket.Deserialize(ref reader),
            PacketOpcode.Death => DeathPacket.Deserialize(ref reader),
            PacketOpcode.ResurrectRequest => ResurrectRequestPacket.Deserialize(ref reader),
            
            // Skills/Spells
            PacketOpcode.SkillUse => CastSpellRequestPacket.Deserialize(ref reader),
            PacketOpcode.SkillResult => SpellEffectPacket.Deserialize(ref reader),
            
            // Admin
            PacketOpcode.AdminSpawnNpc => AdminSpawnNpcPacket.Deserialize(ref reader),
            PacketOpcode.AdminKill => AdminKillPacket.Deserialize(ref reader),
            PacketOpcode.AdminHeal => AdminHealPacket.Deserialize(ref reader),
            PacketOpcode.AdminTeleport => AdminTeleportPacket.Deserialize(ref reader),
            
            // Gumps
            PacketOpcode.GumpOpen => GumpOpenPacket.Deserialize(ref reader),
            PacketOpcode.GumpResponse => GumpResponsePacket.Deserialize(ref reader),
            PacketOpcode.GumpClose => GumpClosePacket.Deserialize(ref reader),
            
            _ => null // Unknown opcode
        };
    }
    
    /// <summary>
    /// Check if an opcode is registered
    /// </summary>
    public static bool IsKnownOpcode(PacketOpcode opcode) => opcode switch
    {
        PacketOpcode.Ping or PacketOpcode.Pong or PacketOpcode.Disconnect or
        PacketOpcode.LoginRequest or PacketOpcode.LoginResponse or
        PacketOpcode.CharacterListRequest or PacketOpcode.CharacterList or
        PacketOpcode.CreateCharacterRequest or PacketOpcode.CreateCharacterResponse or
        PacketOpcode.SelectCharacterRequest or PacketOpcode.EnterWorld or
        PacketOpcode.EntitySpawn or PacketOpcode.EntityDespawn or PacketOpcode.EntityMove or
        PacketOpcode.MoveRequest or PacketOpcode.MoveConfirm or
        PacketOpcode.ChatMessage or PacketOpcode.ChatBroadcast or PacketOpcode.SystemMessage or
        PacketOpcode.AttackRequest or PacketOpcode.DamageDealt or PacketOpcode.Death or
        PacketOpcode.SkillUse or PacketOpcode.SkillResult or
        PacketOpcode.AdminSpawnNpc or PacketOpcode.AdminKill or PacketOpcode.AdminHeal or PacketOpcode.AdminTeleport or
        PacketOpcode.GumpOpen or PacketOpcode.GumpResponse or PacketOpcode.GumpClose
            => true,
        _ => false
    };
}

/// <summary>
/// Packet handler delegate
/// </summary>
public delegate void PacketHandler<T>(T packet) where T : Packet;

/// <summary>
/// Packet dispatcher for routing packets to handlers
/// </summary>
public sealed class PacketDispatcher
{
    private readonly Dictionary<PacketOpcode, Action<Packet>> _handlers = new();
    
    /// <summary>
    /// Register a handler for a packet type
    /// </summary>
    public void Register<T>(PacketHandler<T> handler) where T : Packet, new()
    {
        // Get opcode from a dummy instance
        var dummy = new T();
        _handlers[dummy.Opcode] = p => handler((T)p);
    }
    
    /// <summary>
    /// Register a handler for a specific opcode
    /// </summary>
    public void Register(PacketOpcode opcode, Action<Packet> handler)
    {
        _handlers[opcode] = handler;
    }
    
    /// <summary>
    /// Dispatch a packet to its handler
    /// </summary>
    public bool Dispatch(Packet packet)
    {
        if (_handlers.TryGetValue(packet.Opcode, out var handler))
        {
            handler(packet);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Check if a handler is registered for an opcode
    /// </summary>
    public bool HasHandler(PacketOpcode opcode) => _handlers.ContainsKey(opcode);
}
