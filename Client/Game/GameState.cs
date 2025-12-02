using RealmOfReality.Client.Network;
using RealmOfReality.Shared.Core;
using RealmOfReality.Shared.Entities;
using RealmOfReality.Shared.Gumps;
using RealmOfReality.Shared.Network;

namespace RealmOfReality.Client.Game;

/// <summary>
/// Client-side game state
/// </summary>
public class GameState
{
    private readonly GameClient _client;
    
    // Connection state
    public ClientPhase Phase { get; private set; } = ClientPhase.Disconnected;
    public AccountId? AccountId { get; private set; }
    public string? AccountMessage { get; private set; }
    
    // Character selection
    public List<CharacterListEntry> Characters { get; } = new();
    public int MaxCharacters { get; private set; } = 5;
    
    // In-game state
    public EntityId? PlayerEntityId { get; private set; }
    public PlayerEntity? Player { get; private set; }
    public ushort CurrentMapId { get; private set; }
    public long ServerTick { get; private set; }
    
    // Entities
    private readonly Dictionary<EntityId, Entity> _entities = new();
    
    // Thread-safe packet queue for processing on main thread
    private readonly System.Collections.Concurrent.ConcurrentQueue<Packet> _packetQueue = new();
    
    // Movement prediction
    private byte _moveSequence;
    private readonly Queue<(byte seq, WorldPosition pos)> _pendingMoves = new();
    
    // Chat
    public event Action<ChatChannel, string, string>? ChatReceived;
    public event Action<string, Color>? SystemMessageReceived;
    
    // Combat
    public event Action<EntityId, EntityId, int, bool>? DamageDealt; // attacker, target, damage, isCritical
    public event Action<EntityId, EntityId>? EntityDied; // entity, killer
    public event Action<EntityId, ushort, int, int>? SpellCast; // caster, spellId, damage, heal
    
    // Events
    public event Action? StateChanged;
    public event Action<Entity>? EntitySpawned;
    public event Action<EntityId>? EntityDespawned;
    public event Action<Entity>? EntityMoved;
    
    public GameState(GameClient client)
    {
        _client = client;
        
        _client.Connected += OnConnected;
        _client.Disconnected += OnDisconnected;
        _client.PacketReceived += OnPacketReceived;
    }
    
    private void OnConnected()
    {
        Phase = ClientPhase.Login;
        StateChanged?.Invoke();
    }
    
    private void OnDisconnected(string reason)
    {
        Phase = ClientPhase.Disconnected;
        AccountId = null;
        Characters.Clear();
        _entities.Clear();
        Player = null;
        PlayerEntityId = null;
        StateChanged?.Invoke();
    }
    
    private void OnPacketReceived(Packet packet)
    {
        // Queue packet for processing on main thread
        _packetQueue.Enqueue(packet);
    }
    
    /// <summary>
    /// Process queued packets - call this from the main thread (Update loop)
    /// </summary>
    public void ProcessPackets()
    {
        while (_packetQueue.TryDequeue(out var packet))
        {
            ProcessPacket(packet);
        }
    }
    
    private void ProcessPacket(Packet packet)
    {
        switch (packet)
        {
            case PongPacket pong:
                HandlePong(pong);
                break;
            
            case LoginResponsePacket login:
                HandleLoginResponse(login);
                break;
            
            case CharacterListPacket charList:
                HandleCharacterList(charList);
                break;
            
            case CreateCharacterResponsePacket createResp:
                HandleCreateCharacterResponse(createResp);
                break;
            
            case EnterWorldPacket enter:
                HandleEnterWorld(enter);
                break;
            
            case EntitySpawnPacket spawn:
                HandleEntitySpawn(spawn);
                break;
            
            case EntityDespawnPacket despawn:
                HandleEntityDespawn(despawn);
                break;
            
            case EntityMovePacket move:
                HandleEntityMove(move);
                break;
            
            case MoveConfirmPacket confirm:
                HandleMoveConfirm(confirm);
                break;
            
            case ChatBroadcastPacket chat:
                HandleChatBroadcast(chat);
                break;
            
            case SystemMessagePacket system:
                HandleSystemMessage(system);
                break;
            
            case DamageDealtPacket damage:
                HandleDamageDealt(damage);
                break;
            
            case DeathPacket death:
                HandleDeath(death);
                break;
            
            case SpellEffectPacket spell:
                HandleSpellEffect(spell);
                break;
            
            case GumpOpenPacket gumpOpen:
                HandleGumpOpen(gumpOpen);
                break;
            
            case GumpClosePacket gumpClose:
                HandleGumpClose(gumpClose);
                break;
            
            case InventoryFullPacket invFull:
                HandleInventoryFull(invFull);
                break;
            
            case InventoryUpdatePacket invUpdate:
                HandleInventoryUpdate(invUpdate);
                break;
        }
    }
    
    private void HandleInventoryFull(InventoryFullPacket packet)
    {
        // Store inventory data for client-side use
        // This would be used by BackpackGump to show items
        InventoryReceived?.Invoke(packet);
    }
    
    private void HandleInventoryUpdate(InventoryUpdatePacket packet)
    {
        // Notify UI of inventory change
        InventoryUpdated?.Invoke(packet);
    }
    
    private void HandlePong(PongPacket pong)
    {
        var latency = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - pong.ClientTime;
        // Could display latency in UI
    }
    
    private void HandleLoginResponse(LoginResponsePacket response)
    {
        if (response.Result == ResultCode.Success)
        {
            AccountId = response.AccountId;
            Phase = ClientPhase.CharacterSelect;
        }
        AccountMessage = response.Message;
        StateChanged?.Invoke();
    }
    
    private void HandleCharacterList(CharacterListPacket packet)
    {
        Characters.Clear();
        Characters.AddRange(packet.Characters);
        MaxCharacters = packet.MaxCharacters;
        StateChanged?.Invoke();
    }
    
    private void HandleCreateCharacterResponse(CreateCharacterResponsePacket response)
    {
        AccountMessage = response.Message;
        StateChanged?.Invoke();
    }
    
    private void HandleEnterWorld(EnterWorldPacket enter)
    {
        PlayerEntityId = enter.PlayerEntityId;
        CurrentMapId = enter.MapId;
        ServerTick = enter.ServerTick;
        
        // Create local player entity with default stats
        Player = new PlayerEntity
        {
            Id = enter.PlayerEntityId,
            Position = enter.Position,
            Facing = enter.Facing,
            MapId = enter.MapId,
            Name = "Player",
            AccessLevel = (AccessLevel)enter.AccessLevel,
            // Default stats until server sends real values
            Health = 100,
            MaxHealth = 100,
            Mana = 50,
            MaxMana = 50,
            Stamina = 100,
            MaxStamina = 100,
            Strength = 25,
            Dexterity = 25,
            Intelligence = 25
        };
        
        // Staff members have red hue (visual indicator)
        if (Player.IsStaff)
        {
            Player.Hue = 33; // Red
        }
        
        _entities[Player.Id] = Player;
        
        Phase = ClientPhase.InWorld;
        StateChanged?.Invoke();
    }
    
    private void HandleEntitySpawn(EntitySpawnPacket spawn)
    {
        // Create appropriate entity type based on EntityType
        Entity entity = new NpcEntity
        {
            Id = spawn.EntityId,
            TypeId = spawn.EntityType,
            Position = spawn.Position,
            Facing = spawn.Facing,
            Name = spawn.Name,
            Hue = spawn.BodyHue,
            Flags = (EntityFlags)spawn.Flags,
            Health = spawn.Health > 0 ? spawn.Health : 100,
            MaxHealth = spawn.MaxHealth > 0 ? spawn.MaxHealth : 100,
            Level = spawn.Level > 0 ? spawn.Level : 1
        };
        
        _entities[spawn.EntityId] = entity;
        EntitySpawned?.Invoke(entity);
    }
    
    private void HandleEntityDespawn(EntityDespawnPacket despawn)
    {
        _entities.Remove(despawn.EntityId);
        EntityDespawned?.Invoke(despawn.EntityId);
    }
    
    private void HandleEntityMove(EntityMovePacket move)
    {
        if (_entities.TryGetValue(move.EntityId, out var entity))
        {
            // Check if position actually changed
            bool posChanged = entity.Position.X != move.Position.X || 
                              entity.Position.Y != move.Position.Y ||
                              entity.Position.Z != move.Position.Z;
            
            entity.Position = move.Position;
            entity.Facing = move.Facing;
            
            // Set IsMoving flag for animations
            if (entity is Mobile mobile && posChanged)
            {
                mobile.IsMoving = true;
            }
            
            EntityMoved?.Invoke(entity);
        }
    }
    
    private void HandleMoveConfirm(MoveConfirmPacket confirm)
    {
        // Remove confirmed moves from pending queue
        while (_pendingMoves.Count > 0 && _pendingMoves.Peek().seq <= confirm.SequenceNumber)
        {
            _pendingMoves.Dequeue();
        }
        
        // Server correction if position differs
        if (Player != null)
        {
            var serverPos = confirm.Position;
            var clientPos = Player.Position;
            
            if (serverPos.DistanceTo(clientPos) > 0.5f)
            {
                // Snap to server position (reconciliation)
                Player.Position = serverPos;
                EntityMoved?.Invoke(Player);
            }
        }
    }
    
    private void HandleChatBroadcast(ChatBroadcastPacket chat)
    {
        ChatReceived?.Invoke(chat.Channel, chat.SenderName, chat.Message);
    }
    
    private void HandleSystemMessage(SystemMessagePacket system)
    {
        SystemMessageReceived?.Invoke(system.Message, system.TextColor);
    }
    
    private void HandleDamageDealt(DamageDealtPacket damage)
    {
        // Update target health
        if (_entities.TryGetValue(damage.TargetId, out var entity) && entity is Mobile mobile)
        {
            mobile.Health = damage.TargetHealth;
        }
        
        DamageDealt?.Invoke(damage.AttackerId, damage.TargetId, damage.Damage, damage.IsCritical);
    }
    
    private void HandleDeath(DeathPacket death)
    {
        if (_entities.TryGetValue(death.EntityId, out var entity) && entity is Mobile mobile)
        {
            mobile.Health = 0;
            mobile.Flags |= EntityFlags.Dead;
        }
        
        EntityDied?.Invoke(death.EntityId, death.KillerId);
    }
    
    private void HandleSpellEffect(SpellEffectPacket spell)
    {
        // Update health if target specified
        if (spell.TargetId.HasValue && _entities.TryGetValue(spell.TargetId.Value, out var entity) && entity is Mobile mobile)
        {
            if (spell.Damage > 0)
                mobile.Health = Math.Max(0, mobile.Health - spell.Damage);
            if (spell.Healing > 0)
                mobile.Health = Math.Min(mobile.MaxHealth, mobile.Health + spell.Healing);
        }
        
        SpellCast?.Invoke(spell.CasterId, spell.SpellId, spell.Damage, spell.Healing);
    }
    
    // Client actions
    
    public async Task LoginAsync(string username, string password)
    {
        var hash = ComputePasswordHash(password);
        await _client.SendAsync(new LoginRequestPacket
        {
            Username = username,
            PasswordHash = hash,
            ClientVersion = "0.1.0"
        });
    }
    
    public async Task CreateCharacterAsync(CreateCharacterRequestPacket request)
    {
        await _client.SendAsync(request);
    }
    
    public async Task SelectCharacterAsync(CharacterId characterId)
    {
        await _client.SendAsync(new SelectCharacterRequestPacket { CharacterId = characterId });
    }
    
    public async Task MoveAsync(Direction direction, bool running)
    {
        if (Player == null) return;
        
        _moveSequence++;
        _lastMoveTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // Record movement time
        
        // Client-side prediction
        var (dx, dy) = direction.GetOffset();
        var speed = running ? Player.MoveSpeed * Player.RunSpeedMultiplier : Player.MoveSpeed;
        var predictedPos = new WorldPosition(
            Player.Position.X + dx * speed * 0.05f,
            Player.Position.Y + dy * speed * 0.05f,
            Player.Position.Z
        );
        
        Player.Position = predictedPos;
        Player.Facing = direction;
        Player.IsMoving = true;  // Set moving flag for animation
        Player.IsRunning = running;
        
        _pendingMoves.Enqueue((_moveSequence, predictedPos));
        
        await _client.SendAsync(new MoveRequestPacket
        {
            Direction = direction,
            Running = running,
            SequenceNumber = _moveSequence
        });
        
        EntityMoved?.Invoke(Player);
    }
    
    public async Task SendChatAsync(ChatChannel channel, string message, string? targetName = null)
    {
        await _client.SendAsync(new ChatMessagePacket
        {
            Channel = channel,
            Message = message,
            TargetName = targetName
        });
    }
    
    public async Task AttackAsync(EntityId targetId)
    {
        await _client.SendAsync(new AttackRequestPacket
        {
            TargetId = targetId
        });
    }
    
    public async Task CastSpellAsync(ushort spellId, EntityId? targetId = null, WorldPosition? targetPosition = null)
    {
        await _client.SendAsync(new CastSpellRequestPacket
        {
            SpellId = spellId,
            TargetId = targetId,
            TargetPosition = targetPosition
        });
    }
    
    public async Task ResurrectAsync()
    {
        // Send resurrection request to server
        await _client.SendAsync(new ResurrectRequestPacket());
    }
    
    public async Task AdminSpawnNpcAsync(string name, int typeId, WorldPosition position)
    {
        await _client.SendAsync(new AdminSpawnNpcPacket
        {
            Name = name,
            TypeId = typeId,
            Position = position,
            Level = 5
        });
    }
    
    public async Task AdminKillAsync(EntityId targetId)
    {
        await _client.SendAsync(new AdminKillPacket
        {
            TargetId = targetId
        });
    }
    
    public async Task AdminHealSelfAsync()
    {
        await _client.SendAsync(new AdminHealPacket());
    }
    
    public async Task SendTeleportRequest(float x, float y, float z)
    {
        await _client.SendAsync(new AdminTeleportPacket
        {
            Position = new WorldPosition(x, y, z)
        });
    }
    
    public async Task SendPingAsync()
    {
        await _client.SendAsync(new PingPacket
        {
            ClientTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
    }
    
    public Entity? GetEntity(EntityId id)
    {
        _entities.TryGetValue(id, out var entity);
        return entity;
    }
    
    public IEnumerable<Entity> GetAllEntities() => _entities.Values;
    
    // Movement tracking
    private double _lastMoveTimeMs = 0;
    private const double MoveTimeoutMs = 200; // Clear IsMoving after this long with no movement
    
    /// <summary>
    /// Update game state (call each frame)
    /// </summary>
    public void Update(double totalTimeMs)
    {
        // Clear IsMoving flag if no movement for a while
        if (Player != null && Player.IsMoving)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (nowMs - _lastMoveTimeMs > MoveTimeoutMs)
            {
                Player.IsMoving = false;
            }
        }
    }
    
    /// <summary>
    /// Mark that movement happened (call from MoveAsync)
    /// </summary>
    public void MarkMovement(double totalTimeMs)
    {
        _lastMoveTimeMs = totalTimeMs;
    }
    
    private static string ComputePasswordHash(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
    
    // Gump events
    public event Action<GumpData>? GumpReceived;
    public event Action<uint, uint>? GumpClosed;
    
    // Inventory events
    public event Action<InventoryFullPacket>? InventoryReceived;
    public event Action<InventoryUpdatePacket>? InventoryUpdated;
    
    private void HandleGumpOpen(GumpOpenPacket packet)
    {
        if (packet.GumpData != null)
        {
            GumpReceived?.Invoke(packet.GumpData);
        }
    }
    
    private void HandleGumpClose(GumpClosePacket packet)
    {
        GumpClosed?.Invoke(packet.GumpTypeId, packet.Serial);
    }
    
    /// <summary>
    /// Send gump response to server
    /// </summary>
    public void SendGumpResponse(GumpResponse response)
    {
        var packet = new GumpResponsePacket { Response = response };
        _ = _client.SendAsync(packet); // Fire and forget
    }
    
    /// <summary>
    /// Use an item (double-click)
    /// </summary>
    public async Task UseItemAsync(ulong itemSerial)
    {
        await _client.SendAsync(new ItemUsePacket { ItemSerial = itemSerial });
    }
    
    /// <summary>
    /// Equip an item from inventory
    /// </summary>
    public async Task EquipItemAsync(ulong itemSerial, byte layer = 0)
    {
        await _client.SendAsync(new ItemEquipPacket
        {
            ItemSerial = itemSerial,
            Layer = layer
        });
    }
    
    /// <summary>
    /// Unequip an item
    /// </summary>
    public async Task UnequipItemAsync(ulong itemSerial, byte layer)
    {
        await _client.SendAsync(new ItemUnequipPacket
        {
            ItemSerial = itemSerial,
            Layer = layer
        });
    }
}

/// <summary>
/// Client connection phase
/// </summary>
public enum ClientPhase
{
    Disconnected,
    Connecting,
    Login,
    CharacterSelect,
    EnteringWorld,
    InWorld
}
