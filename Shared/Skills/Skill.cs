using RealmOfReality.Shared.Core;
using RealmOfReality.Shared.Serialization;

namespace RealmOfReality.Shared.Skills;

/// <summary>
/// Skill identifier
/// </summary>
public readonly struct SkillId : IEquatable<SkillId>
{
    public readonly ushort Value;
    
    public SkillId(ushort value) => Value = value;
    
    public static SkillId Empty => new(0);
    public bool IsEmpty => Value == 0;
    
    public bool Equals(SkillId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is SkillId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"Skill:{Value}";
    
    public static bool operator ==(SkillId a, SkillId b) => a.Value == b.Value;
    public static bool operator !=(SkillId a, SkillId b) => a.Value != b.Value;
    
    public static implicit operator SkillId(ushort value) => new(value);
    public static implicit operator ushort(SkillId id) => id.Value;
}

/// <summary>
/// Skill categories
/// </summary>
public enum SkillCategory : byte
{
    Combat = 0,       // Fighting skills
    Magic = 1,        // Spellcasting
    Crafting = 2,     // Item creation
    Gathering = 3,    // Resource collection
    Social = 4,       // Trading, persuasion
    Survival = 5,     // Tracking, camping
    Misc = 6,         // Other skills
}

/// <summary>
/// Skill definition (template)
/// </summary>
public class SkillDefinition
{
    public SkillId Id { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public SkillCategory Category { get; init; }
    public ushort IconId { get; init; }
    
    // Skill cap (0-1000 typically, like UO)
    public int MaxValue { get; init; } = 1000;
    
    // Primary stat for this skill
    public StatType PrimaryStat { get; init; }
    public StatType? SecondaryStat { get; init; }
    
    // Gain difficulty (higher = harder to gain)
    public float GainDifficulty { get; init; } = 1.0f;
    
    // Script for custom behavior
    public string? ScriptName { get; init; }
    
    // Starting value for new characters
    public int DefaultValue { get; init; }
    
    // Is this a combat skill?
    public bool IsCombatSkill { get; init; }
    
    // Prerequisites
    public List<(SkillId Skill, int MinValue)> Prerequisites { get; init; } = new();
}

/// <summary>
/// Stat types
/// </summary>
public enum StatType : byte
{
    Strength,
    Dexterity,
    Intelligence,
}

/// <summary>
/// Player's skill value
/// </summary>
public class SkillValue
{
    public SkillId SkillId { get; init; }
    public int Value { get; set; }      // 0-1000 (like UO, so 100.0 = 1000)
    public int Cap { get; set; }        // Individual skill cap
    public bool IsLocked { get; set; }  // Won't gain/lose
    public bool IsRaising { get; set; } // Actively trying to gain
    
    public float RealValue => Value / 10f; // Display as 0.0 - 100.0
    
    public SkillValue(SkillId skillId, int value = 0, int cap = 1000)
    {
        SkillId = skillId;
        Value = value;
        Cap = cap;
    }
    
    public void Serialize(PacketWriter writer)
    {
        writer.WriteUInt16(SkillId.Value);
        writer.WriteInt16((short)Value);
        writer.WriteInt16((short)Cap);
        writer.WriteByte((byte)((IsLocked ? 1 : 0) | (IsRaising ? 2 : 0)));
    }
    
    public static SkillValue Deserialize(ref PacketReader reader)
    {
        var id = new SkillId(reader.ReadUInt16());
        var value = reader.ReadInt16();
        var cap = reader.ReadInt16();
        var flags = reader.ReadByte();
        
        return new SkillValue(id, value, cap)
        {
            IsLocked = (flags & 1) != 0,
            IsRaising = (flags & 2) != 0
        };
    }
}

/// <summary>
/// Player's complete skill set
/// </summary>
public class SkillSet
{
    private readonly Dictionary<SkillId, SkillValue> _skills = new();
    
    /// <summary>
    /// Total skill cap (sum of all skills)
    /// </summary>
    public int TotalCap { get; set; } = 7000; // 700.0 total like UO
    
    public SkillValue this[SkillId id]
    {
        get
        {
            if (!_skills.TryGetValue(id, out var skill))
            {
                skill = new SkillValue(id);
                _skills[id] = skill;
            }
            return skill;
        }
    }
    
    public int GetValue(SkillId id) => this[id].Value;
    
    public float GetRealValue(SkillId id) => this[id].RealValue;
    
    public bool TryGain(SkillId id, int amount, out int newValue)
    {
        var skill = this[id];
        newValue = skill.Value;
        
        if (skill.IsLocked || skill.Value >= skill.Cap)
            return false;
        
        // Check total cap
        var totalUsed = _skills.Values.Sum(s => s.Value);
        if (totalUsed >= TotalCap)
        {
            // Try to lower another skill that's set to lower
            // For now, just fail
            return false;
        }
        
        var maxGain = Math.Min(amount, skill.Cap - skill.Value);
        maxGain = Math.Min(maxGain, TotalCap - totalUsed);
        
        if (maxGain <= 0)
            return false;
        
        skill.Value += maxGain;
        newValue = skill.Value;
        return true;
    }
    
    public void SetValue(SkillId id, int value)
    {
        this[id].Value = Math.Clamp(value, 0, this[id].Cap);
    }
    
    public int TotalUsed => _skills.Values.Sum(s => s.Value);
    
    public IEnumerable<SkillValue> GetAll() => _skills.Values;
    
    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt16((short)_skills.Count);
        writer.WriteInt16((short)TotalCap);
        
        foreach (var skill in _skills.Values)
        {
            skill.Serialize(writer);
        }
    }
}

/// <summary>
/// Spell/ability definition
/// </summary>
public class SpellDefinition
{
    public ushort Id { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public ushort IconId { get; init; }
    
    // Requirements
    public int RequiredLevel { get; init; }
    public SkillId? RequiredSkill { get; init; }
    public int RequiredSkillValue { get; init; }
    
    // Costs
    public int ManaCost { get; init; }
    public int StaminaCost { get; init; }
    public int HealthCost { get; init; }
    
    // Reagent costs (item template IDs and amounts)
    public List<(ushort ItemTemplateId, int Amount)> ReagentCosts { get; init; } = new();
    
    // Timing
    public float CastTime { get; init; }      // Seconds to cast
    public float Cooldown { get; init; }       // Seconds between uses
    public float Duration { get; init; }       // Effect duration (0 = instant)
    
    // Targeting
    public SpellTargetType TargetType { get; init; }
    public float Range { get; init; } = 10f;
    public float AreaRadius { get; init; }    // For AoE spells
    
    // Effects
    public int MinDamage { get; init; }
    public int MaxDamage { get; init; }
    public int HealAmount { get; init; }
    public DamageType DamageType { get; init; }
    
    // Script for custom behavior
    public string? ScriptName { get; init; }
    
    // Spell school
    public SpellSchool School { get; init; }
    
    // Words of power (for display)
    public string? WordsOfPower { get; init; }
}

/// <summary>
/// Spell targeting types
/// </summary>
public enum SpellTargetType : byte
{
    Self = 0,           // Cast on self
    SingleTarget = 1,   // Target one entity
    Ground = 2,         // Target a location
    Cone = 3,           // Cone in front
    Area = 4,           // Circle around target
    Line = 5,           // Line from caster
    Chain = 6,          // Jumps between targets
}

/// <summary>
/// Schools of magic
/// </summary>
public enum SpellSchool : byte
{
    None = 0,
    Magery = 1,         // General magic
    Necromancy = 2,     // Death magic
    Chivalry = 3,       // Paladin abilities
    Bushido = 4,        // Samurai abilities
    Ninjitsu = 5,       // Ninja abilities
    Spellweaving = 6,   // Elven magic
    Mysticism = 7,      // Gargoyle magic
}

/// <summary>
/// Damage type flags (from Items, re-exported for convenience)
/// </summary>
[Flags]
public enum DamageType : byte
{
    Physical = 1 << 0,
    Fire = 1 << 1,
    Cold = 1 << 2,
    Lightning = 1 << 3,
    Poison = 1 << 4,
    Holy = 1 << 5,
    Shadow = 1 << 6,
    Arcane = 1 << 7,
}

/// <summary>
/// Known spell for a character
/// </summary>
public class KnownSpell
{
    public ushort SpellId { get; init; }
    public DateTime LearnedAt { get; set; }
    public int TimesCast { get; set; }
    public DateTime LastCast { get; set; }
    
    // Cooldown tracking
    public DateTime CooldownEnds { get; set; }
    public bool IsOnCooldown => DateTime.UtcNow < CooldownEnds;
    public TimeSpan RemainingCooldown => IsOnCooldown ? CooldownEnds - DateTime.UtcNow : TimeSpan.Zero;
}

/// <summary>
/// Spellbook for a character
/// </summary>
public class Spellbook
{
    private readonly Dictionary<ushort, KnownSpell> _spells = new();
    
    public bool Knows(ushort spellId) => _spells.ContainsKey(spellId);
    
    public bool TryLearn(ushort spellId)
    {
        if (_spells.ContainsKey(spellId))
            return false;
        
        _spells[spellId] = new KnownSpell
        {
            SpellId = spellId,
            LearnedAt = DateTime.UtcNow
        };
        return true;
    }
    
    public KnownSpell? Get(ushort spellId) => _spells.GetValueOrDefault(spellId);
    
    public IEnumerable<KnownSpell> GetAll() => _spells.Values;
    
    public void StartCooldown(ushort spellId, float cooldownSeconds)
    {
        if (_spells.TryGetValue(spellId, out var spell))
        {
            spell.CooldownEnds = DateTime.UtcNow.AddSeconds(cooldownSeconds);
            spell.LastCast = DateTime.UtcNow;
            spell.TimesCast++;
        }
    }
    
    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt16((short)_spells.Count);
        
        foreach (var spell in _spells.Values)
        {
            writer.WriteUInt16(spell.SpellId);
        }
    }
}

/// <summary>
/// Default skill IDs
/// </summary>
public static class SkillIds
{
    // Combat Skills
    public static readonly SkillId Swordsmanship = 1;
    public static readonly SkillId MaceFighting = 2;
    public static readonly SkillId Fencing = 3;
    public static readonly SkillId Archery = 4;
    public static readonly SkillId Wrestling = 5;
    public static readonly SkillId Parrying = 6;
    public static readonly SkillId Tactics = 7;
    public static readonly SkillId Anatomy = 8;
    public static readonly SkillId Healing = 9;
    
    // Magic Skills
    public static readonly SkillId Magery = 10;
    public static readonly SkillId EvalInt = 11;
    public static readonly SkillId Meditation = 12;
    public static readonly SkillId MagicResist = 13;
    public static readonly SkillId Necromancy = 14;
    public static readonly SkillId SpiritSpeak = 15;
    
    // Crafting Skills
    public static readonly SkillId Blacksmithy = 20;
    public static readonly SkillId Tailoring = 21;
    public static readonly SkillId Tinkering = 22;
    public static readonly SkillId Carpentry = 23;
    public static readonly SkillId Alchemy = 24;
    public static readonly SkillId Inscription = 25;
    public static readonly SkillId Cooking = 26;
    
    // Gathering Skills
    public static readonly SkillId Mining = 30;
    public static readonly SkillId Lumberjacking = 31;
    public static readonly SkillId Fishing = 32;
    public static readonly SkillId Herding = 33;
    
    // Misc Skills
    public static readonly SkillId Stealth = 40;
    public static readonly SkillId Hiding = 41;
    public static readonly SkillId Snooping = 42;
    public static readonly SkillId Stealing = 43;
    public static readonly SkillId Lockpicking = 44;
    public static readonly SkillId DetectHidden = 45;
    public static readonly SkillId Tracking = 46;
    public static readonly SkillId AnimalTaming = 47;
    public static readonly SkillId Veterinary = 48;
    public static readonly SkillId Musicianship = 49;
    public static readonly SkillId Provocation = 50;
    public static readonly SkillId Peacemaking = 51;
}
