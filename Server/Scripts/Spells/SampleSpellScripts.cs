// Sample Spell Scripts for Realm of Reality
// These scripts demonstrate the spell scripting system
// Save .cs files to Server/scripts/Spells/ for auto-loading

using RealmOfReality.Server.Scripting;
using RealmOfReality.Shared.Core;
using RealmOfReality.Shared.Entities;
using RealmOfReality.Shared.Skills;

namespace RealmOfReality.Scripts.Spells;

/// <summary>
/// Magic Arrow - basic damage spell (1st circle)
/// </summary>
public class MagicArrowScript : SpellScriptBase
{
    public override string Name => "MagicArrow";
    
    private const int ManaCost = 4;
    private const int MinDamage = 3;
    private const int MaxDamage = 10;
    private const float Range = 12f;
    
    public override bool CanCast(Mobile caster, object? target, out string? failReason)
    {
        failReason = null;
        
        if (caster.Mana < ManaCost)
        {
            failReason = "You do not have enough mana.";
            return false;
        }
        
        if (target is not Mobile targetMobile)
        {
            failReason = "You must target a creature.";
            return false;
        }
        
        if (caster.DistanceTo(targetMobile) > Range)
        {
            failReason = "That target is too far away.";
            return false;
        }
        
        return true;
    }
    
    public override bool OnCast(Mobile caster, object? target, WorldPosition? targetLocation)
    {
        if (target is not Mobile targetMobile)
            return false;
        
        // Consume mana
        caster.Mana -= ManaCost;
        
        // Calculate damage
        var rand = new Random();
        var damage = rand.Next(MinDamage, MaxDamage + 1);
        
        // Add eval int bonus
        // damage += GetEvalIntBonus(caster);
        
        // Apply damage
        var actualDamage = targetMobile.TakeDamage(damage, caster);
        
        // Visual effect
        Context.PublishEvent?.Invoke("SpellEffect", new {
            Spell = Name,
            Caster = caster,
            Target = targetMobile,
            Effect = "MagicArrow",
            Damage = actualDamage
        });
        
        Context.Logger.LogDebug("{Caster} cast Magic Arrow on {Target} for {Damage} damage",
            caster.Name, targetMobile.Name, actualDamage);
        
        return true;
    }
}

/// <summary>
/// Greater Heal - powerful healing spell (4th circle)
/// </summary>
public class GreaterHealScript : SpellScriptBase
{
    public override string Name => "GreaterHeal";
    
    private const int ManaCost = 11;
    private const int BaseHeal = 50;
    private const float Range = 10f;
    
    public override bool CanCast(Mobile caster, object? target, out string? failReason)
    {
        failReason = null;
        
        if (caster.Mana < ManaCost)
        {
            failReason = "You do not have enough mana.";
            return false;
        }
        
        var targetMobile = target as Mobile ?? caster;
        
        if (targetMobile.Health >= targetMobile.MaxHealth)
        {
            failReason = "That target is not injured.";
            return false;
        }
        
        if (caster.DistanceTo(targetMobile) > Range)
        {
            failReason = "That target is too far away.";
            return false;
        }
        
        return true;
    }
    
    public override bool OnCast(Mobile caster, object? target, WorldPosition? targetLocation)
    {
        var targetMobile = target as Mobile ?? caster;
        
        caster.Mana -= ManaCost;
        
        // Calculate heal (scales with Intelligence)
        var healAmount = BaseHeal + caster.Intelligence / 2;
        var healed = targetMobile.Heal(healAmount);
        
        Context.PublishEvent?.Invoke("SpellEffect", new {
            Spell = Name,
            Caster = caster,
            Target = targetMobile,
            Effect = "HealGlow",
            Amount = healed
        });
        
        Context.Logger.LogDebug("{Caster} cast Greater Heal on {Target} for {Amount} HP",
            caster.Name, targetMobile.Name, healed);
        
        return true;
    }
}

/// <summary>
/// Fireball - area damage spell (3rd circle)
/// </summary>
public class FireballScript : SpellScriptBase
{
    public override string Name => "Fireball";
    
    private const int ManaCost = 9;
    private const int MinDamage = 8;
    private const int MaxDamage = 24;
    private const float Range = 12f;
    private const float SplashRadius = 2f;
    
    public override bool CanCast(Mobile caster, object? target, out string? failReason)
    {
        failReason = null;
        
        if (caster.Mana < ManaCost)
        {
            failReason = "You do not have enough mana.";
            return false;
        }
        
        if (target is not Mobile targetMobile)
        {
            failReason = "You must target a creature.";
            return false;
        }
        
        if (caster.DistanceTo(targetMobile) > Range)
        {
            failReason = "That target is too far away.";
            return false;
        }
        
        return true;
    }
    
    public override bool OnCast(Mobile caster, object? target, WorldPosition? targetLocation)
    {
        if (target is not Mobile primaryTarget)
            return false;
        
        caster.Mana -= ManaCost;
        
        var rand = new Random();
        var baseDamage = rand.Next(MinDamage, MaxDamage + 1);
        
        // Hit primary target
        var primaryDamage = primaryTarget.TakeDamage(baseDamage, caster);
        
        // Splash damage to nearby enemies
        var splashTargets = new List<(Mobile Target, int Damage)>();
        
        // Would get nearby entities from context
        // foreach (var nearby in Context.Entities.GetInRange(primaryTarget.Position, SplashRadius))
        // {
        //     if (nearby is Mobile mobile && mobile != caster && mobile != primaryTarget)
        //     {
        //         var splashDamage = mobile.TakeDamage(baseDamage / 2, caster);
        //         splashTargets.Add((mobile, splashDamage));
        //     }
        // }
        
        Context.PublishEvent?.Invoke("SpellEffect", new {
            Spell = Name,
            Caster = caster,
            Target = primaryTarget,
            Effect = "Fireball",
            Damage = primaryDamage,
            SplashTargets = splashTargets
        });
        
        return true;
    }
}

/// <summary>
/// Invisibility - hide from sight (6th circle)
/// </summary>
public class InvisibilityScript : SpellScriptBase
{
    public override string Name => "Invisibility";
    
    private const int ManaCost = 20;
    private const float BaseDuration = 120f; // 2 minutes
    
    public override bool CanCast(Mobile caster, object? target, out string? failReason)
    {
        failReason = null;
        
        if (caster.Mana < ManaCost)
        {
            failReason = "You do not have enough mana.";
            return false;
        }
        
        var targetMobile = target as Mobile ?? caster;
        
        if (targetMobile.Flags.HasFlag(EntityFlags.Hidden))
        {
            failReason = "That target is already invisible.";
            return false;
        }
        
        return true;
    }
    
    public override bool OnCast(Mobile caster, object? target, WorldPosition? targetLocation)
    {
        var targetMobile = target as Mobile ?? caster;
        
        caster.Mana -= ManaCost;
        
        // Apply invisibility
        targetMobile.Flags |= EntityFlags.Hidden;
        
        // Calculate duration based on magery skill
        var duration = BaseDuration; // + GetMageryBonus(caster);
        
        Context.PublishEvent?.Invoke("ApplyBuff", new {
            Target = targetMobile,
            Buff = "Invisibility",
            Duration = duration,
            OnExpire = "RemoveInvisibility"
        });
        
        Context.PublishEvent?.Invoke("SpellEffect", new {
            Spell = Name,
            Caster = caster,
            Target = targetMobile,
            Effect = "Vanish"
        });
        
        return true;
    }
    
    public override void OnExpire(Mobile caster, Mobile target)
    {
        target.Flags &= ~EntityFlags.Hidden;
        
        Context.PublishEvent?.Invoke("SystemMessage", new {
            User = target,
            Message = "You are no longer invisible."
        });
    }
}

/// <summary>
/// Summon Creature - summon a pet (5th circle)
/// </summary>
public class SummonCreatureScript : SpellScriptBase
{
    public override string Name => "SummonCreature";
    
    private const int ManaCost = 16;
    private const float Duration = 300f; // 5 minutes
    
    public override bool CanCast(Mobile caster, object? target, out string? failReason)
    {
        failReason = null;
        
        if (caster.Mana < ManaCost)
        {
            failReason = "You do not have enough mana.";
            return false;
        }
        
        // Check follower slots
        // Would check caster's current followers here
        
        return true;
    }
    
    public override bool OnCast(Mobile caster, object? target, WorldPosition? targetLocation)
    {
        var summonLocation = targetLocation ?? caster.Position;
        
        caster.Mana -= ManaCost;
        
        // Determine creature type based on magery skill
        var creatureType = DetermineCreatureType(500); // Would use actual skill
        
        Context.PublishEvent?.Invoke("SummonCreature", new {
            Caster = caster,
            CreatureType = creatureType,
            Location = summonLocation,
            Duration = Duration
        });
        
        Context.PublishEvent?.Invoke("SpellEffect", new {
            Spell = Name,
            Caster = caster,
            Location = summonLocation,
            Effect = "Summon"
        });
        
        return true;
    }
    
    private ushort DetermineCreatureType(int magerySkill)
    {
        var rand = new Random();
        
        if (magerySkill >= 900)
            return (ushort)EntityType.MonsterDragon; // Wyrm
        if (magerySkill >= 700)
            return (ushort)EntityType.MonsterTroll;  // Earth elemental
        if (magerySkill >= 500)
            return (ushort)EntityType.MonsterOrc;    // Daemon
        if (magerySkill >= 300)
            return (ushort)EntityType.MonsterWolf;   // Fire elemental
        
        return (ushort)EntityType.MonsterRat; // Basic summon
    }
}

/// <summary>
/// Teleport - instant travel (3rd circle)
/// </summary>
public class TeleportScript : SpellScriptBase
{
    public override string Name => "Teleport";
    
    private const int ManaCost = 9;
    private const float MaxRange = 15f;
    
    public override bool CanCast(Mobile caster, object? target, out string? failReason)
    {
        failReason = null;
        
        if (caster.Mana < ManaCost)
        {
            failReason = "You do not have enough mana.";
            return false;
        }
        
        // Need a ground target location
        // Would validate the target location is valid
        
        return true;
    }
    
    public override bool OnCast(Mobile caster, object? target, WorldPosition? targetLocation)
    {
        if (targetLocation == null)
            return false;
        
        var dest = targetLocation.Value;
        
        // Check range
        if (caster.Position.DistanceTo(dest) > MaxRange)
        {
            Context.PublishEvent?.Invoke("SystemMessage", new {
                User = caster,
                Message = "That location is too far away."
            });
            return false;
        }
        
        // Check if destination is valid (not blocked, etc)
        // Would validate with world manager here
        
        caster.Mana -= ManaCost;
        
        var oldPos = caster.Position;
        caster.Position = dest;
        
        Context.PublishEvent?.Invoke("SpellEffect", new {
            Spell = Name,
            Caster = caster,
            SourceLocation = oldPos,
            DestLocation = dest,
            Effect = "Teleport"
        });
        
        return true;
    }
}

/// <summary>
/// Poison - damage over time (3rd circle)
/// </summary>
public class PoisonScript : SpellScriptBase
{
    public override string Name => "Poison";
    
    private const int ManaCost = 9;
    private const float Range = 10f;
    
    public override bool CanCast(Mobile caster, object? target, out string? failReason)
    {
        failReason = null;
        
        if (caster.Mana < ManaCost)
        {
            failReason = "You do not have enough mana.";
            return false;
        }
        
        if (target is not Mobile targetMobile)
        {
            failReason = "You must target a creature.";
            return false;
        }
        
        if (caster.DistanceTo(targetMobile) > Range)
        {
            failReason = "That target is too far away.";
            return false;
        }
        
        return true;
    }
    
    public override bool OnCast(Mobile caster, object? target, WorldPosition? targetLocation)
    {
        if (target is not Mobile targetMobile)
            return false;
        
        caster.Mana -= ManaCost;
        
        // Determine poison level based on magery and poisoning skill
        var poisonLevel = DeterminePoisonLevel(500); // Would use actual skills
        
        Context.PublishEvent?.Invoke("ApplyPoison", new {
            Target = targetMobile,
            Source = caster,
            Level = poisonLevel,
            Duration = 30f // 30 seconds
        });
        
        Context.PublishEvent?.Invoke("SpellEffect", new {
            Spell = Name,
            Caster = caster,
            Target = targetMobile,
            Effect = "Poison"
        });
        
        return true;
    }
    
    private int DeterminePoisonLevel(int skillValue)
    {
        if (skillValue >= 800) return 4; // Lethal
        if (skillValue >= 600) return 3; // Deadly
        if (skillValue >= 400) return 2; // Greater
        if (skillValue >= 200) return 1; // Regular
        return 0; // Lesser
    }
}

/// <summary>
/// Energy Bolt - high damage single target (4th circle)
/// </summary>
public class EnergyBoltScript : SpellScriptBase
{
    public override string Name => "EnergyBolt";
    
    private const int ManaCost = 11;
    private const int MinDamage = 15;
    private const int MaxDamage = 40;
    private const float Range = 12f;
    
    public override bool CanCast(Mobile caster, object? target, out string? failReason)
    {
        failReason = null;
        
        if (caster.Mana < ManaCost)
        {
            failReason = "You do not have enough mana.";
            return false;
        }
        
        if (target is not Mobile targetMobile)
        {
            failReason = "You must target a creature.";
            return false;
        }
        
        if (caster.DistanceTo(targetMobile) > Range)
        {
            failReason = "That target is too far away.";
            return false;
        }
        
        return true;
    }
    
    public override bool OnCast(Mobile caster, object? target, WorldPosition? targetLocation)
    {
        if (target is not Mobile targetMobile)
            return false;
        
        caster.Mana -= ManaCost;
        
        var rand = new Random();
        var damage = rand.Next(MinDamage, MaxDamage + 1);
        
        // Add eval int bonus
        damage += caster.Intelligence / 10;
        
        var actualDamage = targetMobile.TakeDamage(damage, caster);
        
        Context.PublishEvent?.Invoke("SpellEffect", new {
            Spell = Name,
            Caster = caster,
            Target = targetMobile,
            Effect = "EnergyBolt",
            Damage = actualDamage,
            DamageType = DamageType.Lightning
        });
        
        return true;
    }
}
