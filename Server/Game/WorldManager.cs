using RealmOfReality.Server.Config;
using RealmOfReality.Server.Data;
using RealmOfReality.Server.Gumps;
using RealmOfReality.Server.Network;
using RealmOfReality.Shared.Core;
using RealmOfReality.Shared.Entities;
using RealmOfReality.Shared.Network;
using RealmOfReality.Shared.Scripts;
using RealmOfReality.Shared.World;

namespace RealmOfReality.Server.Game;

/// <summary>
/// Manages the game world state
/// </summary>
public class WorldManager
{
    private readonly ILogger _logger;
    private readonly ServerConfig _config;
    private readonly EntityManager _entities;
    private readonly Dictionary<ushort, GameMap> _maps = new();
    private readonly Dictionary<EntityId, ClientConnection> _playerConnections = new();
    private readonly WorldClock _worldClock = new();
    
    public GameTime GameTime { get; } = new();
    public WorldClock WorldClock => _worldClock;
    
    public WorldManager(ILogger logger, ServerConfig config)
    {
        _logger = logger;
        _config = config;
        _entities = new EntityManager(_config.ServerId);
    }
    
    /// <summary>
    /// Initialize the world
    /// </summary>
    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing world...");
        
        // Load or create default map
        var mapPath = Path.Combine(_config.Paths.WorldDirectory, "map_1.ror");
        Directory.CreateDirectory(_config.Paths.WorldDirectory);
        
        GameMap map;
        if (File.Exists(mapPath))
        {
            map = await GameMap.LoadAsync(mapPath);
            _logger.LogInformation("Loaded map: {Name} ({Width}x{Height})", map.Name, map.Width, map.Height);
        }
        else
        {
            map = CreateDefaultMap();
            await map.SaveAsync(mapPath);
            _logger.LogInformation("Created default map: {Name}", map.Name);
        }
        
        _maps[map.MapId] = map;
        
        // Start game time
        GameTime.Start();
        
        _logger.LogInformation("World initialized with {MapCount} maps", _maps.Count);
    }
    
    /// <summary>
    /// Create a simple default map for testing
    /// </summary>
    private GameMap CreateDefaultMap()
    {
        var map = new GameMap(1, 256, 256) { Name = "Starter Island" };
        
        // Fill with grass
        for (var y = 0; y < 256; y++)
        {
            for (var x = 0; x < 256; x++)
            {
                var tile = Tile.Grass;
                
                // Add some variety with dirt patches
                if ((x + y) % 17 == 0)
                    tile.GroundId = 4; // Dirt patches
                
                // Some trees (as impassable statics)
                if (x > 20 && x < 230 && y > 20 && y < 230 && (x * y) % 47 == 0)
                {
                    tile.StaticId = 100; // Tree
                    tile.StaticHeight = 4;
                    tile.Flags = TileFlags.Impassable;
                }
                
                map.SetTile(new TilePosition(x, y), tile);
            }
        }
        
        // Clear spawn area
        for (var y = 45; y <= 55; y++)
        {
            for (var x = 45; x <= 55; x++)
            {
                var tile = Tile.Grass;
                tile.Flags = TileFlags.SafeZone;
                map.SetTile(new TilePosition(x, y), tile);
            }
        }
        
        return map;
    }
    
    /// <summary>
    /// Update world state (called each tick)
    /// </summary>
    public void Update()
    {
        GameTime.Update();
        _worldClock.Update(GameTime.TickCount);
        _entities.UpdateAll(GameTime);
        
        // Update NPC AI every 10 ticks (0.5 seconds)
        if (GameTime.TickCount % 10 == 0)
        {
            UpdateNpcAI();
        }
    }
    
    private void UpdateNpcAI()
    {
        var npcs = _entities.GetAll<NpcEntity>().ToList();
        var players = _entities.GetAll<PlayerEntity>().Where(p => p.IsOnline).ToList();
        
        foreach (var npc in npcs)
        {
            if (npc.Health <= 0) continue;
            if (npc.Behavior != NpcBehavior.Hostile && npc.Behavior != NpcBehavior.Aggressive) continue;
            
            // Check for aggro
            if (!npc.TargetId.HasValue)
            {
                // Find nearest player in aggro range
                PlayerEntity? nearestPlayer = null;
                float nearestDist = float.MaxValue;
                
                foreach (var player in players)
                {
                    if (player.MapId != npc.MapId) continue;
                    var dist = player.Position.DistanceTo(npc.Position);
                    if (dist <= npc.AggroRadius && dist < nearestDist)
                    {
                        nearestPlayer = player;
                        nearestDist = dist;
                    }
                }
                
                if (nearestPlayer != null)
                {
                    npc.TargetId = nearestPlayer.Id;
                    _logger.LogDebug("{NpcName} aggro on {PlayerName}", npc.Name, nearestPlayer.Name);
                }
            }
            else
            {
                // Has a target - chase or attack
                if (!_entities.TryGet<PlayerEntity>(npc.TargetId.Value, out var target) || 
                    target == null || target.Health <= 0 || !target.IsOnline)
                {
                    // Target lost
                    npc.TargetId = null;
                    continue;
                }
                
                var distToTarget = target.Position.DistanceTo(npc.Position);
                var distToSpawn = npc.SpawnPoint.DistanceTo(npc.Position);
                
                // Leash distance - return to spawn if too far
                var leashDistance = npc.AggroRadius * 3;
                if (distToSpawn > leashDistance)
                {
                    npc.TargetId = null;
                    // Move back toward spawn
                    MoveNpcToward(npc, npc.SpawnPoint);
                    continue;
                }
                
                if (distToTarget > 1.5f)
                {
                    // Chase - move toward target
                    MoveNpcToward(npc, target.Position);
                }
                else
                {
                    // In range - attack!
                    _ = NpcAttackAsync(npc, target);
                }
            }
        }
    }
    
    private void MoveNpcToward(NpcEntity npc, WorldPosition target)
    {
        var dx = target.X - npc.Position.X;
        var dy = target.Y - npc.Position.Y;
        
        // Normalize and move one tile
        int moveX = dx > 0.5f ? 1 : (dx < -0.5f ? -1 : 0);
        int moveY = dy > 0.5f ? 1 : (dy < -0.5f ? -1 : 0);
        
        if (moveX != 0 || moveY != 0)
        {
            var newPos = new WorldPosition(
                npc.Position.X + moveX,
                npc.Position.Y + moveY,
                npc.Position.Z
            );
            
            npc.Position = newPos;
            npc.Facing = DirectionExtensions.FromOffset(moveX, moveY);
            
            // Broadcast movement
            _ = BroadcastEntityMoveAsync(npc);
        }
    }
    
    private async Task NpcAttackAsync(NpcEntity npc, PlayerEntity target)
    {
        // Simple damage calculation
        var baseDamage = 5 + npc.Level * 2;
        var damage = baseDamage + Random.Shared.Next(-3, 4);
        var isCritical = Random.Shared.Next(100) < 5; // 5% crit
        if (isCritical) damage = (int)(damage * 1.5f);
        
        target.Health = Math.Max(0, target.Health - damage);
        
        // Broadcast damage
        var packet = new DamageDealtPacket
        {
            AttackerId = npc.Id,
            TargetId = target.Id,
            Damage = damage,
            DamageType = DamageType.Physical,
            IsCritical = isCritical,
            TargetHealth = target.Health
        };
        
        await BroadcastToNearbyAsync(npc.Position, packet, _config.GameRules.VisibilityRange);
        
        if (target.Health <= 0)
        {
            // Player died
            await BroadcastToNearbyAsync(target.Position, 
                new DeathPacket { EntityId = target.Id, KillerId = npc.Id },
                _config.GameRules.VisibilityRange);
        }
    }
    
    /// <summary>
    /// Spawn a player into the world
    /// </summary>
    public PlayerEntity SpawnPlayer(CharacterData characterData, ClientConnection connection, AccessLevel accessLevel = AccessLevel.Player)
    {
        var entityId = _entities.GenerateId();
        var player = characterData.ToEntity(entityId);
        player.IsOnline = true;
        player.LoginTime = DateTime.UtcNow;
        player.AccessLevel = accessLevel;
        
        // Staff members (GM+) are invulnerable and wear red robes
        if (player.IsStaff)
        {
            player.Flags |= EntityFlags.Invulnerable;
            player.Hue = 33; // Red hue for staff robes
        }
        
        _entities.Add(player);
        _playerConnections[entityId] = connection;
        
        var staffTag = accessLevel >= AccessLevel.GameMaster ? $" [{accessLevel}]" : "";
        _logger.LogInformation("Spawned player {Name}{StaffTag} (Entity: {EntityId}) at ({X}, {Y})", 
            player.Name, staffTag, entityId, player.Position.X, player.Position.Y);
        
        return player;
    }
    
    /// <summary>
    /// Remove a player from the world
    /// </summary>
    public void DespawnPlayer(EntityId entityId)
    {
        if (_entities.TryGet<PlayerEntity>(entityId, out var player))
        {
            player!.IsOnline = false;
            _entities.Remove(entityId);
            _playerConnections.Remove(entityId);
            
            _logger.LogInformation("Despawned player {Name}", player.Name);
        }
    }
    
    /// <summary>
    /// Get a player entity by ID
    /// </summary>
    public PlayerEntity? GetPlayer(EntityId entityId) => _entities.Get<PlayerEntity>(entityId);
    
    /// <summary>
    /// Get all players on a map
    /// </summary>
    public IEnumerable<PlayerEntity> GetPlayersOnMap(ushort mapId) =>
        _entities.GetAll<PlayerEntity>().Where(p => p.MapId == mapId && p.IsOnline);
    
    /// <summary>
    /// Get all entities in range of a position
    /// </summary>
    public IEnumerable<Entity> GetEntitiesInRange(WorldPosition center, float range) =>
        _entities.GetInRange(center, range);
    
    /// <summary>
    /// Get the connection for a player
    /// </summary>
    public ClientConnection? GetPlayerConnection(EntityId entityId)
    {
        _playerConnections.TryGetValue(entityId, out var connection);
        return connection;
    }
    
    /// <summary>
    /// Get a map by ID
    /// </summary>
    public GameMap? GetMap(ushort mapId)
    {
        _maps.TryGetValue(mapId, out var map);
        return map;
    }
    
    /// <summary>
    /// Try to move a player
    /// </summary>
    public bool TryMovePlayer(PlayerEntity player, Direction direction, bool running)
    {
        var map = GetMap(player.MapId);
        if (map == null) return false;
        
        if (!player.CanMove) return false;
        
        var (dx, dy) = direction.GetOffset();
        var speed = running ? player.MoveSpeed * player.RunSpeedMultiplier : player.MoveSpeed;
        
        var newPos = new WorldPosition(
            player.Position.X + dx * speed * 0.05f, // 50ms tick
            player.Position.Y + dy * speed * 0.05f,
            player.Position.Z
        );
        
        // TODO: Proper collision detection requires loading UO tiledata.mul
        // For now, allow all movement (server doesn't have UO map data)
        // var newTile = newPos.ToTile();
        // if (!map.IsPassable(newTile))
        //     return false;
        
        player.Position = newPos;
        player.Facing = direction;
        player.IsRunning = running;
        
        return true;
    }
    
    /// <summary>
    /// Broadcast entity spawn to nearby players
    /// </summary>
    public async Task BroadcastEntitySpawnAsync(Entity entity)
    {
        var health = entity is Mobile m ? m.Health : 100;
        var maxHealth = entity is Mobile m2 ? m2.MaxHealth : 100;
        var level = entity is Mobile m3 ? m3.Level : 1;
        
        var packet = new EntitySpawnPacket
        {
            EntityId = entity.Id,
            EntityType = entity.TypeId,
            Position = entity.Position,
            Facing = entity.Facing,
            Name = entity.Name,
            BodyHue = entity.Hue,
            Flags = (byte)entity.Flags,
            Health = health,
            MaxHealth = maxHealth,
            Level = level
        };
        
        foreach (var player in GetPlayersOnMap(entity.MapId))
        {
            if (player.Id == entity.Id) continue;
            if (player.Position.DistanceTo(entity.Position) > _config.GameRules.VisibilityRange) continue;
            
            var conn = GetPlayerConnection(player.Id);
            if (conn != null)
            {
                await conn.SendAsync(packet);
            }
        }
    }
    
    /// <summary>
    /// Broadcast entity despawn to nearby players
    /// </summary>
    public async Task BroadcastEntityDespawnAsync(EntityId entityId, ushort mapId, WorldPosition lastPosition)
    {
        var packet = new EntityDespawnPacket { EntityId = entityId };
        
        foreach (var player in GetPlayersOnMap(mapId))
        {
            if (player.Id == entityId) continue;
            if (player.Position.DistanceTo(lastPosition) > _config.GameRules.VisibilityRange) continue;
            
            var conn = GetPlayerConnection(player.Id);
            if (conn != null)
            {
                await conn.SendAsync(packet);
            }
        }
    }
    
    /// <summary>
    /// Broadcast entity movement to nearby players
    /// </summary>
    public async Task BroadcastEntityMoveAsync(Entity entity, byte moveType = 0)
    {
        var packet = new EntityMovePacket
        {
            EntityId = entity.Id,
            Position = entity.Position,
            Facing = entity.Facing,
            MoveType = moveType
        };
        
        foreach (var player in GetPlayersOnMap(entity.MapId))
        {
            if (player.Id == entity.Id) continue;
            if (player.Position.DistanceTo(entity.Position) > _config.GameRules.VisibilityRange) continue;
            
            var conn = GetPlayerConnection(player.Id);
            if (conn != null)
            {
                await conn.SendAsync(packet);
            }
        }
    }
    
    /// <summary>
    /// Broadcast chat message
    /// </summary>
    public async Task BroadcastChatAsync(PlayerEntity sender, ChatChannel channel, string message)
    {
        var packet = new ChatBroadcastPacket
        {
            Channel = channel,
            SenderEntityId = sender.Id,
            SenderName = sender.Name,
            Message = message
        };
        
        IEnumerable<PlayerEntity> recipients;
        
        switch (channel)
        {
            case ChatChannel.Local:
                recipients = GetPlayersOnMap(sender.MapId)
                    .Where(p => p.Position.DistanceTo(sender.Position) <= _config.GameRules.LocalChatRange);
                break;
            case ChatChannel.Global:
                recipients = _entities.GetAll<PlayerEntity>().Where(p => p.IsOnline);
                break;
            default:
                recipients = Enumerable.Empty<PlayerEntity>();
                break;
        }
        
        foreach (var player in recipients)
        {
            var conn = GetPlayerConnection(player.Id);
            if (conn != null)
            {
                await conn.SendAsync(packet);
            }
        }
    }
    
    /// <summary>
    /// Send system message to a player
    /// </summary>
    public async Task SendSystemMessageAsync(ClientConnection connection, string message, Color? color = null)
    {
        var packet = new SystemMessagePacket
        {
            Message = message,
            TextColor = color ?? Color.White
        };
        await connection.SendAsync(packet);
    }
    
    /// <summary>
    /// Save the world state
    /// </summary>
    public async Task SaveAsync()
    {
        _logger.LogInformation("Saving world...");
        
        foreach (var map in _maps.Values)
        {
            var path = Path.Combine(_config.Paths.WorldDirectory, $"map_{map.MapId}.ror");
            await map.SaveAsync(path);
        }
        
        _logger.LogInformation("World saved");
    }
    
    public int OnlinePlayerCount => _playerConnections.Count;
    public int EntityCount => _entities.Count;
    
    /// <summary>
    /// Spawn an NPC into the world
    /// </summary>
    public NpcEntity SpawnNpc(string name, ushort typeId, WorldPosition position, ushort mapId = 1, int level = 1)
    {
        var entityId = _entities.GenerateId();
        var npc = new NpcEntity
        {
            Id = entityId,
            Name = name,
            TypeId = typeId,
            Position = position,
            SpawnPoint = position,
            MapId = mapId,
            Level = level,
            Health = 50 + level * 20,
            MaxHealth = 50 + level * 20,
            Mana = 20 + level * 10,
            MaxMana = 20 + level * 10,
            Stamina = 50 + level * 10,
            MaxStamina = 50 + level * 10,
            Behavior = NpcBehavior.Hostile,
            AggroRadius = 6f + level * 0.5f, // Higher level = larger aggro
            WanderRadius = 3f
        };
        
        _entities.Add(npc);
        _logger.LogInformation("Spawned NPC {Name} (Level {Level}) at ({X}, {Y})", name, level, position.X, position.Y);
        
        return npc;
    }
    
    /// <summary>
    /// Spawn a dragon for combat testing
    /// </summary>
    public NpcEntity SpawnDragon(WorldPosition position, int level = 10)
    {
        var dragon = SpawnNpc("Ancient Dragon", 100, position, 1, level);
        dragon.Health = 500 + level * 50;
        dragon.MaxHealth = dragon.Health;
        dragon.Mana = 200 + level * 20;
        dragon.MaxMana = dragon.Mana;
        dragon.Behavior = NpcBehavior.Hostile;
        dragon.AggroRadius = 12f; // Dragons have larger aggro range
        return dragon;
    }
    
    /// <summary>
    /// Spawn test creatures around spawn point
    /// </summary>
    public void SpawnTestCreatures()
    {
        // ========== TOWN OF HAVEN ==========
        // Town center at (50, 50)
        
        // Spawn an Ankh at spawn point (resurrection)
        var ankh = SpawnNpc("Ankh of Resurrection", 50, new WorldPosition(50, 50, 0), 1, 1);
        ankh.Behavior = NpcBehavior.Passive;
        ankh.AggroRadius = 0;
        
        // Healer (resurrection and healing services)
        var healer = SpawnNpc("Healer", 40, new WorldPosition(52, 48, 0), 1, 10);
        healer.Behavior = NpcBehavior.Vendor;
        healer.AggroRadius = 0;
        
        // Town Vendors - placed around town square
        var blacksmith = SpawnNpc("Blacksmith", 200, new WorldPosition(45, 45, 0), 1, 1);
        blacksmith.Behavior = NpcBehavior.Vendor;
        blacksmith.AggroRadius = 0;
        
        var provisioner = SpawnNpc("Provisioner", 201, new WorldPosition(55, 45, 0), 1, 1);
        provisioner.Behavior = NpcBehavior.Vendor;
        provisioner.AggroRadius = 0;
        
        var mage = SpawnNpc("Mage Vendor", 202, new WorldPosition(45, 55, 0), 1, 1);
        mage.Behavior = NpcBehavior.Vendor;
        mage.AggroRadius = 0;
        
        var innkeeper = SpawnNpc("Innkeeper", 204, new WorldPosition(55, 55, 0), 1, 1);
        innkeeper.Behavior = NpcBehavior.Vendor;
        innkeeper.AggroRadius = 0;
        
        var tailor = SpawnNpc("Tailor", 205, new WorldPosition(48, 42, 0), 1, 1);
        tailor.Behavior = NpcBehavior.Vendor;
        tailor.AggroRadius = 0;
        
        var jeweler = SpawnNpc("Jeweler", 206, new WorldPosition(52, 42, 0), 1, 1);
        jeweler.Behavior = NpcBehavior.Vendor;
        jeweler.AggroRadius = 0;
        
        var banker = SpawnNpc("Banker", 207, new WorldPosition(50, 42, 0), 1, 1);
        banker.Behavior = NpcBehavior.Vendor;
        banker.AggroRadius = 0;
        
        _logger.LogInformation("Spawned Town of Haven at (50, 50)");
        
        // ========== MONSTERS OUTSIDE TOWN ==========
        // Safe zone is roughly 40-60 on both axes
        
        // Goblin Camp (East, level 1-5)
        SpawnNpc("Goblin", 10, new WorldPosition(70, 45, 0), 1, 2);
        SpawnNpc("Goblin", 10, new WorldPosition(72, 48, 0), 1, 3);
        SpawnNpc("Goblin", 10, new WorldPosition(68, 50, 0), 1, 2);
        SpawnNpc("Goblin Chieftain", 10, new WorldPosition(75, 47, 0), 1, 5);
        
        // Undead Graveyard (North, level 3-7)
        SpawnNpc("Skeleton", 20, new WorldPosition(45, 70, 0), 1, 4);
        SpawnNpc("Skeleton", 20, new WorldPosition(50, 72, 0), 1, 5);
        SpawnNpc("Skeleton", 20, new WorldPosition(55, 70, 0), 1, 4);
        SpawnNpc("Skeleton Warrior", 20, new WorldPosition(50, 75, 0), 1, 7);
        
        // Wolf Den (West, level 2-5)
        SpawnNpc("Grey Wolf", 30, new WorldPosition(30, 45, 0), 1, 3);
        SpawnNpc("Grey Wolf", 30, new WorldPosition(28, 50, 0), 1, 4);
        SpawnNpc("Dire Wolf", 30, new WorldPosition(25, 48, 0), 1, 5);
        
        // Orc Camp (South, level 5-10)
        SpawnNpc("Orc", 60, new WorldPosition(45, 30, 0), 1, 6);
        SpawnNpc("Orc", 60, new WorldPosition(50, 28, 0), 1, 7);
        SpawnNpc("Orc Captain", 60, new WorldPosition(48, 25, 0), 1, 10);
        
        // Dragon's Lair (Far Northeast, level 15-25)
        SpawnDragon(new WorldPosition(85, 85, 0), 15);
        
        _logger.LogInformation("Spawned monsters in the wilderness");
    }
    
    /// <summary>
    /// Get all NPCs
    /// </summary>
    public IEnumerable<NpcEntity> GetAllNpcs() => _entities.GetAll<NpcEntity>();
    
    /// <summary>
    /// Get NPC by ID
    /// </summary>
    public NpcEntity? GetNpc(EntityId entityId) => _entities.Get<NpcEntity>(entityId);
    
    /// <summary>
    /// Get any entity by ID
    /// </summary>
    public Entity? GetEntity(EntityId entityId) => _entities.Get(entityId);
    
    /// <summary>
    /// Broadcast a packet to all players near a position
    /// </summary>
    public async Task BroadcastToNearbyAsync(WorldPosition center, Packet packet, float range = 20f)
    {
        var nearbyPlayers = GetEntitiesInRange(center, range).OfType<PlayerEntity>();
        
        foreach (var player in nearbyPlayers)
        {
            var conn = GetPlayerConnection(player.Id);
            if (conn != null)
            {
                await conn.SendAsync(packet);
            }
        }
    }
    
    // Corpse loot storage
    private readonly Dictionary<EntityId, List<LootItem>> _corpseLoot = new();
    
    /// <summary>
    /// Spawn a corpse for a dead NPC with loot
    /// </summary>
    public async Task SpawnCorpseAsync(NpcEntity npc)
    {
        // Create corpse entity
        var corpseId = _entities.GenerateId();
        var corpse = new NpcEntity
        {
            Id = corpseId,
            Name = $"{npc.Name}'s Corpse",
            Position = npc.Position,
            TypeId = 999, // Corpse type
            Level = npc.Level,
            Health = 0,
            MaxHealth = 1,
            Behavior = NpcBehavior.Passive,
            SpawnPoint = npc.Position
        };
        
        // Generate loot using RunUO-style loot packs
        var definition = CreatureScripts.GetDefinition(npc.TypeId);
        var loot = new List<LootItem>();
        
        if (definition != null && definition.LootPacks.Length > 0)
        {
            loot = definition.GenerateLoot(npc.Level);
        }
        else
        {
            // Fallback loot based on level
            var fallbackPack = npc.Level switch
            {
                < 5 => LootPack.Poor,
                < 10 => LootPack.Meager,
                < 15 => LootPack.Average,
                < 20 => LootPack.Rich,
                _ => LootPack.FilthyRich
            };
            loot = fallbackPack.Generate(npc.Level);
        }
        
        // Store loot for this corpse
        _corpseLoot[corpseId] = loot;
        
        _entities.Add(corpse);
        
        // Broadcast corpse spawn with loot count
        var spawnPacket = new EntitySpawnPacket
        {
            EntityId = corpse.Id,
            EntityType = corpse.TypeId,
            Name = corpse.Name,
            Position = corpse.Position,
            Level = loot.Count, // Use level field to store item count for now
            Health = 0,
            MaxHealth = 1,
            BodyHue = 1 // Grey hue for corpse
        };
        await BroadcastToNearbyAsync(corpse.Position, spawnPacket);
        
        // Schedule corpse decay (5 minutes)
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(5));
            await DespawnCorpseAsync(corpseId);
        });
        
        _logger.LogInformation("Spawned corpse for {NpcName} with {LootCount} items", npc.Name, loot.Count);
    }
    
    /// <summary>
    /// Get loot from a corpse
    /// </summary>
    public List<LootItem>? GetCorpseLoot(EntityId corpseId)
    {
        return _corpseLoot.TryGetValue(corpseId, out var loot) ? loot : null;
    }
    
    /// <summary>
    /// Take an item from a corpse
    /// </summary>
    public LootItem? TakeFromCorpse(EntityId corpseId, int index)
    {
        if (!_corpseLoot.TryGetValue(corpseId, out var loot) || index < 0 || index >= loot.Count)
            return null;
        
        var item = loot[index];
        loot.RemoveAt(index);
        
        // If corpse is empty, mark for faster decay
        if (loot.Count == 0)
        {
            _corpseLoot.Remove(corpseId);
        }
        
        return item;
    }
    
    /// <summary>
    /// Take all items from a corpse
    /// </summary>
    public List<LootItem> TakeAllFromCorpse(EntityId corpseId)
    {
        if (!_corpseLoot.TryGetValue(corpseId, out var loot))
            return new List<LootItem>();
        
        _corpseLoot.Remove(corpseId);
        return loot;
    }
    
    /// <summary>
    /// Despawn an NPC and schedule respawn
    /// </summary>
    public async Task DespawnNpcAsync(NpcEntity npc)
    {
        var position = npc.Position;
        var spawnPoint = npc.SpawnPoint;
        var typeId = npc.TypeId;
        var name = npc.Name;
        var level = npc.Level;
        
        // Remove the entity
        _entities.Remove(npc.Id);
        
        // Broadcast despawn
        var despawnPacket = new EntityDespawnPacket { EntityId = npc.Id };
        await BroadcastToNearbyAsync(position, despawnPacket);
        
        // Schedule respawn (30 seconds)
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            
            // Respawn at original spawn point
            var newNpc = SpawnNpc(name, typeId, spawnPoint, 1, level);
            _logger.LogDebug("Respawned {NpcName} at {Position}", name, spawnPoint);
        });
    }
    
    /// <summary>
    /// Despawn a corpse
    /// </summary>
    public async Task DespawnCorpseAsync(EntityId corpseId)
    {
        var corpse = _entities.Get(corpseId);
        if (corpse == null) return;
        
        _entities.Remove(corpseId);
        
        var despawnPacket = new EntityDespawnPacket { EntityId = corpseId };
        await BroadcastToNearbyAsync(corpse.Position, despawnPacket);
    }
    
    // ============ Gump System ============
    
    private readonly GumpManager _gumpManager = new();
    
    /// <summary>
    /// Get the gump manager
    /// </summary>
    public GumpManager GumpManager => _gumpManager;
    
    /// <summary>
    /// Send a gump to a player
    /// </summary>
    public async Task SendGumpAsync(PlayerEntity player, Gump gump)
    {
        var connection = GetPlayerConnection(player.Id);
        if (connection == null) return;
        
        _gumpManager.RegisterGump(player.Id.Value, gump);
        
        var packet = new GumpOpenPacket { GumpData = gump.BuildData() };
        await connection.SendAsync(packet);
    }
    
    /// <summary>
    /// Send a gump to a player by connection
    /// </summary>
    public async Task SendGumpAsync(ClientConnection connection, Gump gump, EntityId playerId)
    {
        _gumpManager.RegisterGump(playerId.Value, gump);
        
        var packet = new GumpOpenPacket { GumpData = gump.BuildData() };
        await connection.SendAsync(packet);
    }
    
    /// <summary>
    /// Close a specific gump for a player
    /// </summary>
    public async Task CloseGumpAsync(PlayerEntity player, uint gumpTypeId, uint serial)
    {
        var connection = GetPlayerConnection(player.Id);
        if (connection == null) return;
        
        _gumpManager.CloseGumpOfType(player.Id.Value, gumpTypeId);
        
        var packet = new GumpClosePacket { GumpTypeId = gumpTypeId, Serial = serial };
        await connection.SendAsync(packet);
    }
    
    /// <summary>
    /// Open the player's status gump
    /// </summary>
    public async Task OpenStatusGumpAsync(PlayerEntity player)
    {
        var gump = new StatusGump(player);
        await SendGumpAsync(player, gump);
    }
    
    /// <summary>
    /// Open the player's paperdoll
    /// </summary>
    public async Task OpenPaperdollAsync(PlayerEntity player, PlayerEntity? target = null)
    {
        target ??= player;
        bool isSelf = player.Id == target.Id;
        var gump = new PaperdollGump(target, isSelf);
        await SendGumpAsync(player, gump);
    }
}
