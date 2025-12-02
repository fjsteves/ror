using System.Collections.Concurrent;
using RealmOfReality.Shared.Core;
using RealmOfReality.Shared.Items;
using RealmOfReality.Shared.Serialization;

namespace RealmOfReality.Shared.Entities;

/// <summary>
/// Base class for all game entities (players, NPCs, items, etc.)
/// </summary>
public abstract class Entity
{
    public EntityId Id { get; init; }
    public ushort TypeId { get; set; }
    public WorldPosition Position { get; set; }
    public Direction Facing { get; set; } = Direction.South;
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Map/region this entity is in
    /// </summary>
    public ushort MapId { get; set; }
    
    /// <summary>
    /// Display name
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// Visual appearance hue/tint
    /// </summary>
    public ushort Hue { get; set; }
    
    /// <summary>
    /// Get the tile this entity occupies
    /// </summary>
    public TilePosition Tile => Position.ToTile();
    
    /// <summary>
    /// Entity flags for status effects, visibility, etc.
    /// </summary>
    public EntityFlags Flags { get; set; }
    
    /// <summary>
    /// Called each server tick
    /// </summary>
    public virtual void Update(GameTime time) { }
    
    /// <summary>
    /// Serialize entity state for network transmission
    /// </summary>
    public virtual void Serialize(PacketWriter writer)
    {
        writer.WriteEntityId(Id);
        writer.WriteUInt16(TypeId);
        writer.WriteWorldPosition(Position);
        writer.WriteDirection(Facing);
        writer.WriteString(Name);
        writer.WriteUInt16(Hue);
        writer.WriteUInt16((ushort)Flags);
    }
    
    /// <summary>
    /// Distance to another entity
    /// </summary>
    public float DistanceTo(Entity other) => Position.DistanceTo(other.Position);
    
    /// <summary>
    /// Check if another entity is within range
    /// </summary>
    public bool InRange(Entity other, float range) => DistanceTo(other) <= range;
}

/// <summary>
/// Entity status flags
/// </summary>
[Flags]
public enum EntityFlags : ushort
{
    None = 0,
    Hidden = 1 << 0,      // Invisible to others
    Invulnerable = 1 << 1, // Cannot be damaged
    Frozen = 1 << 2,       // Cannot move
    Silenced = 1 << 3,     // Cannot use skills
    Dead = 1 << 4,         // Dead/incapacitated
    InCombat = 1 << 5,     // Currently in combat
    AFK = 1 << 6,          // Away from keyboard
    Mounted = 1 << 7,      // On a mount
    Swimming = 1 << 8,     // In water
    Flying = 1 << 9,       // Airborne
    WarMode = 1 << 10,     // War mode (hostile targeting)
}

/// <summary>
/// Entity types for the game
/// </summary>
public enum EntityType : ushort
{
    Unknown = 0,
    
    // Players (1-99)
    PlayerMale = 1,
    PlayerFemale = 2,
    
    // Humanoid NPCs (100-499)
    NpcVendor = 100,
    NpcGuard = 101,
    NpcQuestGiver = 102,
    NpcBanker = 103,
    
    // Monsters (500-999)
    MonsterRat = 500,
    MonsterWolf = 501,
    MonsterSkeleton = 502,
    MonsterZombie = 503,
    MonsterOrc = 504,
    MonsterTroll = 505,
    MonsterDragon = 506,
    
    // Animals (1000-1499)
    AnimalChicken = 1000,
    AnimalPig = 1001,
    AnimalCow = 1002,
    AnimalHorse = 1003,
    AnimalDeer = 1004,
    
    // Items on ground (2000-2999)
    ItemDropped = 2000,
    ItemCorpse = 2001,
    
    // Static objects (3000+)
    ObjectContainer = 3000,
    ObjectDoor = 3001,
    ObjectSign = 3002,
}

/// <summary>
/// Mobile entity - anything that can move and has stats
/// </summary>
public abstract class Mobile : Entity
{
    // Base stats
    public int Strength { get; set; } = 10;
    public int Dexterity { get; set; } = 10;
    public int Intelligence { get; set; } = 10;
    
    // Current/max vital stats
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public int Mana { get; set; } = 100;
    public int MaxMana { get; set; } = 100;
    public int Stamina { get; set; } = 100;
    public int MaxStamina { get; set; } = 100;
    
    // Stat locks (for training)
    public StatLock StrLock { get; set; } = StatLock.Up;
    public StatLock DexLock { get; set; } = StatLock.Up;
    public StatLock IntLock { get; set; } = StatLock.Up;
    
    // Character properties
    public bool IsFemale { get; set; }
    public int StatsCap { get; set; } = 225;  // Total stat cap
    
    // Resistances
    public int PhysicalResistance { get; set; }
    public int FireResistance { get; set; }
    public int ColdResistance { get; set; }
    public int PoisonResistance { get; set; }
    public int EnergyResistance { get; set; }
    
    // Combat stats
    public int DamageMin { get; set; } = 1;
    public int DamageMax { get; set; } = 5;
    public int Luck { get; set; }
    
    // Inventory/Weight
    public int Weight { get; set; }
    public int WeightMax { get; set; } = 400;
    
    // Followers (pets/summons)
    public int Followers { get; set; }
    public int FollowersMax { get; set; } = 5;
    
    // Combat
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
    
    // Movement
    public float MoveSpeed { get; set; } = 1.0f; // Tiles per second (walking)
    public float RunSpeedMultiplier { get; set; } = 2.0f;
    public bool IsRunning { get; set; }
    public bool IsMoving { get; set; }
    
    // Current movement target
    protected WorldPosition? MoveTarget { get; set; }
    
    public bool IsDead => Flags.HasFlag(EntityFlags.Dead);
    public bool CanMove => !IsDead && !Flags.HasFlag(EntityFlags.Frozen);
    public bool CanAct => !IsDead && !Flags.HasFlag(EntityFlags.Silenced);
    
    /// <summary>
    /// Calculate actual move speed
    /// </summary>
    public float CurrentMoveSpeed => IsRunning ? MoveSpeed * RunSpeedMultiplier : MoveSpeed;
    
    /// <summary>
    /// Start moving toward a target position
    /// </summary>
    public virtual void MoveTo(WorldPosition target)
    {
        if (!CanMove) return;
        MoveTarget = target;
        IsMoving = true;
        
        // Calculate facing direction
        var dx = target.X - Position.X;
        var dy = target.Y - Position.Y;
        Facing = DirectionExtensions.FromVector(dx, dy);
    }
    
    /// <summary>
    /// Stop movement
    /// </summary>
    public virtual void StopMoving()
    {
        MoveTarget = null;
        IsMoving = false;
    }
    
    /// <summary>
    /// Update movement (call from Update)
    /// </summary>
    protected virtual void UpdateMovement(GameTime time)
    {
        if (!IsMoving || MoveTarget == null) return;
        
        var target = MoveTarget.Value;
        var distance = Position.DistanceTo(target);
        var moveDistance = (float)(CurrentMoveSpeed * time.DeltaTime);
        
        if (distance <= moveDistance)
        {
            // Arrived at target
            Position = target;
            StopMoving();
        }
        else
        {
            // Move toward target
            var t = moveDistance / distance;
            Position = WorldPosition.Lerp(Position, target, t);
        }
    }
    
    /// <summary>
    /// Take damage
    /// </summary>
    public virtual int TakeDamage(int amount, Entity? source = null)
    {
        if (IsDead || Flags.HasFlag(EntityFlags.Invulnerable))
            return 0;
        
        var actualDamage = Math.Min(amount, Health);
        Health -= actualDamage;
        
        if (Health <= 0)
        {
            Health = 0;
            Die(source);
        }
        
        return actualDamage;
    }
    
    /// <summary>
    /// Heal
    /// </summary>
    public virtual int Heal(int amount)
    {
        if (IsDead) return 0;
        
        var actualHeal = Math.Min(amount, MaxHealth - Health);
        Health += actualHeal;
        return actualHeal;
    }
    
    /// <summary>
    /// Die
    /// </summary>
    protected virtual void Die(Entity? killer)
    {
        Flags |= EntityFlags.Dead;
        StopMoving();
    }
    
    /// <summary>
    /// Respawn
    /// </summary>
    public virtual void Respawn(WorldPosition position)
    {
        Position = position;
        Health = MaxHealth;
        Mana = MaxMana;
        Stamina = MaxStamina;
        Flags &= ~EntityFlags.Dead;
    }
    
    public override void Update(GameTime time)
    {
        base.Update(time);
        UpdateMovement(time);
    }
    
    public override void Serialize(PacketWriter writer)
    {
        base.Serialize(writer);
        writer.WriteInt16((short)Health);
        writer.WriteInt16((short)MaxHealth);
        writer.WriteByte((byte)Level);
    }
}

/// <summary>
/// Player character entity
/// </summary>
public class PlayerEntity : Mobile
{
    public AccountId AccountId { get; init; }
    public CharacterId CharacterId { get; init; }
    
    // Session info
    public DateTime LoginTime { get; set; }
    public bool IsOnline { get; set; }
    
    // Access level (permissions)
    public AccessLevel AccessLevel { get; set; } = AccessLevel.Player;
    
    // Character customization
    public byte Gender { get; set; }
    public ushort BodyType { get; set; } = 400;  // Default to male human body
    public ushort SkinHue { get; set; } = 1002;  // Default skin tone
    public ushort HairStyle { get; set; }
    public ushort HairHue { get; set; }
    public ushort BeardStyle { get; set; }
    public ushort BeardHue { get; set; }
    
    // Currency
    public long Gold { get; set; }
    
    // Equipment and Inventory
    public Items.Equipment Equipment { get; } = new();
    public Items.Inventory Inventory { get; } = new(40);
    
    // For client-side prediction
    public byte LastMoveSequence { get; set; }
    
    /// <summary>
    /// Staff members (GM+) are invincible
    /// </summary>
    public bool IsStaff => AccessLevel >= AccessLevel.GameMaster;
    
    /// <summary>
    /// Staff members wear red robes (hue 33 = red in UO)
    /// </summary>
    public ushort StaffRobeHue => 33;
    
    public PlayerEntity()
    {
        TypeId = (ushort)EntityType.PlayerMale;
    }
    
    /// <summary>
    /// Override TakeDamage - staff members are invulnerable
    /// </summary>
    public override int TakeDamage(int amount, Entity? source = null)
    {
        if (IsStaff)
            return 0; // Invincible
        return base.TakeDamage(amount, source);
    }
    
    public override void Serialize(PacketWriter writer)
    {
        base.Serialize(writer);
        writer.WriteByte(Gender);
        writer.WriteUInt16(SkinHue);
        writer.WriteUInt16(HairStyle);
        writer.WriteUInt16(HairHue);
        writer.WriteByte((byte)AccessLevel);
    }
}

/// <summary>
/// Player access levels (like RunUO)
/// </summary>
public enum AccessLevel : byte
{
    Player = 0,
    Counselor = 1,
    GameMaster = 2,
    Seer = 3,
    Administrator = 4,
    Developer = 5,
    Owner = 6
}

/// <summary>
/// NPC entity (monsters, vendors, etc.)
/// </summary>
public class NpcEntity : Mobile
{
    /// <summary>
    /// AI behavior type
    /// </summary>
    public NpcBehavior Behavior { get; set; } = NpcBehavior.Passive;
    
    /// <summary>
    /// Spawn point for respawning
    /// </summary>
    public WorldPosition SpawnPoint { get; set; }
    
    /// <summary>
    /// Time until respawn (when dead)
    /// </summary>
    public TimeSpan RespawnDelay { get; set; } = TimeSpan.FromSeconds(60);
    
    /// <summary>
    /// Wander radius from spawn point
    /// </summary>
    public float WanderRadius { get; set; } = 5f;
    
    /// <summary>
    /// Aggro radius for hostile NPCs
    /// </summary>
    public float AggroRadius { get; set; } = 8f;
    
    /// <summary>
    /// Current target (for combat)
    /// </summary>
    public EntityId? TargetId { get; set; }
}

/// <summary>
/// NPC behavior types
/// </summary>
public enum NpcBehavior
{
    Passive,      // Won't attack
    Hostile,      // Attacks on sight (alias for Aggressive)
    Aggressive,   // Attacks on sight
    Defensive,    // Attacks when attacked
    Vendor,       // Shop NPC
    QuestGiver,   // Quest NPC
    Guard,        // Protects area
}

/// <summary>
/// Thread-safe entity collection
/// </summary>
public class EntityManager
{
    private readonly ConcurrentDictionary<EntityId, Entity> _entities = new();
    private readonly IdGenerator _idGenerator;
    
    public EntityManager(ushort serverId)
    {
        _idGenerator = new IdGenerator(serverId);
    }
    
    public EntityId GenerateId() => _idGenerator.Generate();
    
    public void Add(Entity entity)
    {
        _entities[entity.Id] = entity;
    }
    
    public bool Remove(EntityId id)
    {
        return _entities.TryRemove(id, out _);
    }
    
    public Entity? Get(EntityId id)
    {
        _entities.TryGetValue(id, out var entity);
        return entity;
    }
    
    public T? Get<T>(EntityId id) where T : Entity
    {
        return Get(id) as T;
    }
    
    public bool TryGet<T>(EntityId id, out T? entity) where T : Entity
    {
        entity = Get<T>(id);
        return entity != null;
    }
    
    public IEnumerable<Entity> GetAll() => _entities.Values;
    
    public IEnumerable<T> GetAll<T>() where T : Entity => _entities.Values.OfType<T>();
    
    public IEnumerable<Entity> GetInRange(WorldPosition center, float range)
    {
        return _entities.Values.Where(e => e.Position.DistanceTo(center) <= range);
    }
    
    public IEnumerable<Entity> GetOnMap(ushort mapId)
    {
        return _entities.Values.Where(e => e.MapId == mapId);
    }
    
    public int Count => _entities.Count;
    
    public void Clear() => _entities.Clear();
    
    public void UpdateAll(GameTime time)
    {
        foreach (var entity in _entities.Values.Where(e => e.IsActive))
        {
            entity.Update(time);
        }
    }
}

/// <summary>
/// Stat lock status for training control
/// </summary>
public enum StatLock : byte
{
    Up = 0,      // Stat can increase
    Down = 1,    // Stat can decrease
    Locked = 2   // Stat is locked
}
