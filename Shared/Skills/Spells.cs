namespace RealmOfReality.Shared.Skills;

/// <summary>
/// UO-style spell definitions based on official Ultima Online spells
/// </summary>
public static class SpellDefinitions
{
    public static readonly SpellInfo[] AllSpells = 
    {
        // First Circle - Mana 4, Delay 0.5s
        new(1, "Clumsy", "Uus Jux", 1, 4, 0.5f, SpellTarget.Enemy, SpellEffect.StatDebuff),
        new(2, "Create Food", "In Mani Ylem", 1, 4, 0.5f, SpellTarget.Self, SpellEffect.Utility),
        new(3, "Feeblemind", "Rel Wis", 1, 4, 0.5f, SpellTarget.Enemy, SpellEffect.StatDebuff),
        new(4, "Heal", "In Mani", 1, 4, 0.5f, SpellTarget.Friendly, SpellEffect.Heal),
        new(5, "Magic Arrow", "In Por Ylem", 1, 4, 0.5f, SpellTarget.Enemy, SpellEffect.Damage, DamageType.Fire, 14, 18),
        new(6, "Night Sight", "In Lor", 1, 4, 0.5f, SpellTarget.Friendly, SpellEffect.Buff),
        new(7, "Reactive Armor", "Flam Sanct", 1, 4, 0.5f, SpellTarget.Self, SpellEffect.Buff),
        new(8, "Weaken", "Des Mani", 1, 4, 0.5f, SpellTarget.Enemy, SpellEffect.StatDebuff),
        
        // Second Circle - Mana 6, Delay 0.75s
        new(9, "Agility", "Ex Uus", 2, 6, 0.75f, SpellTarget.Friendly, SpellEffect.StatBuff),
        new(10, "Cunning", "Uus Wis", 2, 6, 0.75f, SpellTarget.Friendly, SpellEffect.StatBuff),
        new(11, "Cure", "An Nox", 2, 6, 0.75f, SpellTarget.Friendly, SpellEffect.Cure),
        new(12, "Harm", "An Mani", 2, 6, 0.75f, SpellTarget.Enemy, SpellEffect.Damage, DamageType.Cold, 23, 29),
        new(13, "Magic Trap", "In Jux", 2, 6, 0.75f, SpellTarget.Object, SpellEffect.Utility),
        new(14, "Remove Trap", "An Jux", 2, 6, 0.75f, SpellTarget.Object, SpellEffect.Utility),
        new(15, "Protection", "Uus Sanct", 2, 6, 0.75f, SpellTarget.Self, SpellEffect.Buff),
        new(16, "Strength", "Uus Mani", 2, 6, 0.75f, SpellTarget.Friendly, SpellEffect.StatBuff),
        
        // Third Circle - Mana 9, Delay 1.0s
        new(17, "Bless", "Rel Sanct", 3, 9, 1.0f, SpellTarget.Friendly, SpellEffect.StatBuff),
        new(18, "Fireball", "Vas Flam", 3, 9, 1.0f, SpellTarget.Enemy, SpellEffect.Damage, DamageType.Fire, 26, 31),
        new(19, "Magic Lock", "An Por", 3, 9, 1.0f, SpellTarget.Object, SpellEffect.Utility),
        new(20, "Poison", "In Nox", 3, 9, 1.0f, SpellTarget.Enemy, SpellEffect.Poison),
        new(21, "Telekinesis", "Ort Por Ylem", 3, 9, 1.0f, SpellTarget.Object, SpellEffect.Utility),
        new(22, "Teleport", "Rel Por", 3, 9, 1.0f, SpellTarget.Location, SpellEffect.Teleport),
        new(23, "Unlock", "Ex Por", 3, 9, 1.0f, SpellTarget.Object, SpellEffect.Utility),
        new(24, "Wall of Stone", "In Sanct Ylem", 3, 9, 1.0f, SpellTarget.Location, SpellEffect.Field),
        
        // Fourth Circle - Mana 11, Delay 1.25s
        new(25, "Arch Cure", "Vas An Nox", 4, 11, 1.0f, SpellTarget.Friendly, SpellEffect.Cure), // Cast speed of 3rd circle
        new(26, "Arch Protection", "Vas Uus Sanct", 4, 11, 1.25f, SpellTarget.Self, SpellEffect.AreaBuff),
        new(27, "Curse", "Des Sanct", 4, 11, 1.25f, SpellTarget.Enemy, SpellEffect.StatDebuff),
        new(28, "Fire Field", "In Flam Grav", 4, 11, 1.25f, SpellTarget.Location, SpellEffect.Field, DamageType.Fire, 2, 2),
        new(29, "Greater Heal", "In Vas Mani", 4, 11, 1.25f, SpellTarget.Friendly, SpellEffect.Heal),
        new(30, "Lightning", "Por Ort Grav", 4, 11, 1.25f, SpellTarget.Enemy, SpellEffect.Damage, DamageType.Lightning, 30, 34),
        new(31, "Mana Drain", "Ort Rel", 4, 11, 1.25f, SpellTarget.Enemy, SpellEffect.ManaDrain),
        new(32, "Recall", "Kal Ort Por", 4, 11, 1.25f, SpellTarget.Object, SpellEffect.Teleport),
        
        // Fifth Circle - Mana 14, Delay 1.5s
        new(33, "Blade Spirits", "In Jux Hur Ylem", 5, 18, 1.5f, SpellTarget.Location, SpellEffect.Summon),
        new(34, "Dispel Field", "An Grav", 5, 14, 1.5f, SpellTarget.Location, SpellEffect.Dispel),
        new(35, "Incognito", "Kal In Ex", 5, 14, 1.5f, SpellTarget.Self, SpellEffect.Transform),
        new(36, "Magic Reflection", "In Jux Sanct", 5, 14, 1.5f, SpellTarget.Self, SpellEffect.Buff),
        new(37, "Mind Blast", "Por Corp Wis", 5, 14, 1.5f, SpellTarget.Enemy, SpellEffect.Damage, DamageType.Cold, 40, 42),
        new(38, "Paralyze", "An Ex Por", 5, 14, 1.5f, SpellTarget.Enemy, SpellEffect.Paralyze),
        new(39, "Poison Field", "In Nox Grav", 5, 14, 1.5f, SpellTarget.Location, SpellEffect.Field),
        new(40, "Summon Creature", "Kal Xen", 5, 18, 1.5f, SpellTarget.Location, SpellEffect.Summon),
        
        // Sixth Circle - Mana 20, Delay 1.75s
        new(41, "Dispel", "An Ort", 6, 20, 1.75f, SpellTarget.Enemy, SpellEffect.Dispel),
        new(42, "Energy Bolt", "Corp Por", 6, 20, 1.75f, SpellTarget.Enemy, SpellEffect.Damage, DamageType.Lightning, 51, 56),
        new(43, "Explosion", "Vas Ort Flam", 6, 20, 1.75f, SpellTarget.Enemy, SpellEffect.Damage, DamageType.Fire, 51, 56),
        new(44, "Invisibility", "An Lor Xen", 6, 20, 1.75f, SpellTarget.Friendly, SpellEffect.Buff),
        new(45, "Mark", "Kal Por Ylem", 6, 20, 1.75f, SpellTarget.Object, SpellEffect.Utility),
        new(46, "Mass Curse", "Vas Des Sanct", 6, 20, 1.75f, SpellTarget.Location, SpellEffect.AreaDebuff),
        new(47, "Paralyze Field", "In Ex Grav", 6, 20, 1.75f, SpellTarget.Location, SpellEffect.Field),
        new(48, "Reveal", "Wis Quas", 6, 20, 1.75f, SpellTarget.Location, SpellEffect.Reveal),
        
        // Seventh Circle - Mana 40, Delay 2.0s
        new(49, "Chain Lightning", "Vas Ort Grav", 7, 40, 2.0f, SpellTarget.Enemy, SpellEffect.AreaDamage, DamageType.Lightning, 64, 69),
        new(50, "Energy Field", "In Sanct Grav", 7, 40, 2.0f, SpellTarget.Location, SpellEffect.Field),
        new(51, "Flamestrike", "Kal Vas Flam", 7, 40, 2.0f, SpellTarget.Enemy, SpellEffect.Damage, DamageType.Fire, 64, 69),
        new(52, "Gate Travel", "Vas Rel Por", 7, 40, 2.0f, SpellTarget.Object, SpellEffect.Teleport),
        new(53, "Mana Vampire", "Ort Sanct", 7, 40, 2.0f, SpellTarget.Enemy, SpellEffect.ManaDrain),
        new(54, "Mass Dispel", "Vas An Ort", 7, 40, 2.0f, SpellTarget.Location, SpellEffect.Dispel),
        new(55, "Meteor Swarm", "Kal Des Flam Ylem", 7, 40, 2.0f, SpellTarget.Enemy, SpellEffect.AreaDamage, DamageType.Fire, 64, 69),
        new(56, "Polymorph", "Vas Ylem Rel", 7, 40, 2.0f, SpellTarget.Self, SpellEffect.Transform),
        
        // Eighth Circle - Mana 50, Delay 2.25s
        new(57, "Earthquake", "In Vas Por", 8, 50, 2.25f, SpellTarget.Self, SpellEffect.AreaDamage, DamageType.Physical, 10, 100),
        new(58, "Energy Vortex", "Vas Corp Por", 8, 50, 2.25f, SpellTarget.Location, SpellEffect.Summon),
        new(59, "Resurrection", "An Corp", 8, 50, 2.25f, SpellTarget.Friendly, SpellEffect.Resurrect),
        new(60, "Summon Air Elemental", "Kal Vas Xen Hur", 8, 50, 2.25f, SpellTarget.Location, SpellEffect.Summon),
        new(61, "Summon Daemon", "Kal Vas Xen Corp", 8, 50, 2.25f, SpellTarget.Location, SpellEffect.Summon),
        new(62, "Summon Earth Elemental", "Kal Vas Xen Ylem", 8, 50, 2.25f, SpellTarget.Location, SpellEffect.Summon),
        new(63, "Summon Fire Elemental", "Kal Vas Xen Flam", 8, 50, 2.25f, SpellTarget.Location, SpellEffect.Summon),
        new(64, "Summon Water Elemental", "Kal Vas Xen An Flam", 8, 50, 2.25f, SpellTarget.Location, SpellEffect.Summon),
    };
    
    public static SpellInfo? GetSpell(int spellId)
    {
        if (spellId < 1 || spellId > AllSpells.Length) return null;
        return AllSpells[spellId - 1];
    }
}

public record SpellInfo(
    int Id,
    string Name,
    string Words,
    int Circle,
    int ManaCost,
    float CastDelay,
    SpellTarget TargetType,
    SpellEffect Effect,
    DamageType DamageType = 0, // 0 = no damage type
    int MinDamage = 0,
    int MaxDamage = 0
);

public enum SpellTarget
{
    Self,
    Friendly,
    Enemy,
    Location,
    Object
}

public enum SpellEffect
{
    Damage,
    AreaDamage,
    Heal,
    Buff,
    StatBuff,
    AreaBuff,
    StatDebuff,
    AreaDebuff,
    Cure,
    Poison,
    Paralyze,
    Teleport,
    Field,
    Summon,
    Dispel,
    Transform,
    Reveal,
    ManaDrain,
    Resurrect,
    Utility
}

// Note: Uses DamageType from Skill.cs
