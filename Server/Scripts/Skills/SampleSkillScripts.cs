// Sample Skill Scripts for Realm of Reality
// These scripts demonstrate the skill scripting system
// Save .cs files to Server/scripts/Skills/ for auto-loading

using RealmOfReality.Server.Scripting;
using RealmOfReality.Shared.Core;
using RealmOfReality.Shared.Entities;
using RealmOfReality.Shared.Skills;

namespace RealmOfReality.Scripts.Skills;

/// <summary>
/// Healing skill - bandage wounds
/// </summary>
public class HealingScript : SkillScriptBase
{
    public override string Name => "Healing";
    
    private const float BaseCastTime = 5.0f; // 5 seconds
    private const int BandageItemId = 0x0E21; // Bandage item template
    
    public override bool CanUse(Mobile user, object? target, out string? failReason)
    {
        failReason = null;
        
        // Need a target
        if (target is not Mobile targetMobile)
        {
            failReason = "You must target a creature to heal.";
            return false;
        }
        
        // Target must be injured
        if (targetMobile.Health >= targetMobile.MaxHealth)
        {
            failReason = "That target is not injured.";
            return false;
        }
        
        // Must be in range
        if (user.DistanceTo(targetMobile) > 2f)
        {
            failReason = "You are too far away.";
            return false;
        }
        
        // Check for bandages in inventory
        // Would check player inventory here
        
        return true;
    }
    
    public override bool OnUse(Mobile user, object? target)
    {
        if (target is not Mobile targetMobile)
            return false;
        
        // Start healing action
        Context.PublishEvent?.Invoke("StartSkillAction", new {
            User = user,
            Skill = Name,
            Target = target,
            Duration = BaseCastTime,
            OnComplete = "CompleteHealing"
        });
        
        return true;
    }
    
    public void CompleteHealing(Mobile user, Mobile target, int skillValue)
    {
        // Calculate heal amount based on skill
        var minHeal = skillValue / 20; // 0-50 at 0-1000 skill
        var maxHeal = skillValue / 10; // 0-100 at 0-1000 skill
        var healAmount = new Random().Next(minHeal, maxHeal + 1);
        
        // Add anatomy bonus
        // var anatomyBonus = GetAnatomyBonus(user);
        
        var healed = target.Heal(healAmount);
        
        Context.Logger.LogInformation("{Healer} healed {Target} for {Amount} HP", 
            user.Name, target.Name, healed);
        
        Context.PublishEvent?.Invoke("HealComplete", new {
            Healer = user,
            Target = target,
            Amount = healed
        });
    }
    
    public override float GetDifficulty(Mobile user, object? target)
    {
        if (target is not Mobile targetMobile)
            return 1.0f;
        
        // Difficulty based on damage amount
        var damagePercent = 1.0f - (float)targetMobile.Health / targetMobile.MaxHealth;
        return 0.5f + damagePercent * 0.5f;
    }
}

/// <summary>
/// Mining skill - extract ore from rocks
/// </summary>
public class MiningScript : SkillScriptBase
{
    public override string Name => "Mining";
    
    public override bool CanUse(Mobile user, object? target, out string? failReason)
    {
        failReason = null;
        
        // Need a pickaxe equipped or in backpack
        // Would check equipment/inventory here
        
        // Target must be a mineable rock
        // Would check target type here
        
        return true;
    }
    
    public override bool OnUse(Mobile user, object? target)
    {
        // Start mining action
        Context.PublishEvent?.Invoke("StartSkillAction", new {
            User = user,
            Skill = Name,
            Target = target,
            Duration = 2.0f,
            OnComplete = "CompleteMining",
            Animation = "Mining"
        });
        
        return true;
    }
    
    public void CompleteMining(Mobile user, object rockTarget, int skillValue)
    {
        // Skill check to see if we get ore
        var chance = skillValue / 1000f; // 0-100% based on skill
        var rand = new Random();
        
        if (rand.NextDouble() < chance)
        {
            // Determine ore type based on skill and location
            var oreType = DetermineOreType(skillValue);
            var amount = rand.Next(1, 3);
            
            Context.PublishEvent?.Invoke("GiveItem", new {
                User = user,
                ItemTemplateId = oreType,
                Amount = amount
            });
            
            Context.Logger.LogInformation("{Miner} mined {Amount} ore (type {Type})", 
                user.Name, amount, oreType);
        }
        else
        {
            Context.PublishEvent?.Invoke("SystemMessage", new {
                User = user,
                Message = "You fail to extract any ore."
            });
        }
    }
    
    private ushort DetermineOreType(int skillValue)
    {
        var rand = new Random();
        
        // Higher skill = chance at rarer ores
        if (skillValue >= 900 && rand.NextDouble() < 0.1)
            return 0x1BF5; // Valorite ore
        if (skillValue >= 800 && rand.NextDouble() < 0.15)
            return 0x1BF4; // Verite ore
        if (skillValue >= 700 && rand.NextDouble() < 0.2)
            return 0x1BF3; // Agapite ore
        if (skillValue >= 600 && rand.NextDouble() < 0.25)
            return 0x1BF2; // Gold ore
        if (skillValue >= 500 && rand.NextDouble() < 0.3)
            return 0x1BF1; // Bronze ore
        if (skillValue >= 400 && rand.NextDouble() < 0.35)
            return 0x1BF0; // Copper ore
        if (skillValue >= 300 && rand.NextDouble() < 0.4)
            return 0x1BEF; // Dull copper ore
        if (skillValue >= 200 && rand.NextDouble() < 0.5)
            return 0x1BEE; // Shadow iron ore
        
        return 0x19B9; // Iron ore (default)
    }
}

/// <summary>
/// Stealth skill - become hidden while moving
/// </summary>
public class StealthScript : SkillScriptBase
{
    public override string Name => "Stealth";
    
    private const int StepsPerCheck = 10; // Skill check every 10 steps
    
    public override bool CanUse(Mobile user, object? target, out string? failReason)
    {
        failReason = null;
        
        // Must already be hidden
        if (!user.Flags.HasFlag(EntityFlags.Hidden))
        {
            failReason = "You must hide first.";
            return false;
        }
        
        // Cannot be in combat
        if (user.Flags.HasFlag(EntityFlags.InCombat))
        {
            failReason = "You cannot stealth while in combat.";
            return false;
        }
        
        // Armor check (heavy armor = harder to stealth)
        // Would check equipment here
        
        return true;
    }
    
    public override bool OnUse(Mobile user, object? target)
    {
        // Enter stealth mode
        user.Flags |= EntityFlags.Hidden;
        
        Context.PublishEvent?.Invoke("EnterStealth", new {
            User = user,
            StepsRemaining = CalculateMaxSteps(500) // Would use actual skill value
        });
        
        Context.Logger.LogInformation("{User} entered stealth mode", user.Name);
        
        return true;
    }
    
    private int CalculateMaxSteps(int skillValue)
    {
        // Max steps based on skill
        return skillValue / 10; // 0-100 steps at 0-1000 skill
    }
    
    public override float GetDifficulty(Mobile user, object? target)
    {
        // Difficulty based on nearby enemies and light level
        // Would calculate based on game state
        return 0.5f;
    }
}

/// <summary>
/// Blacksmithy skill - craft metal items
/// </summary>
public class BlacksmithyScript : SkillScriptBase
{
    public override string Name => "Blacksmithy";
    
    public override bool OnUse(Mobile user, object? target)
    {
        // Open crafting menu
        Context.PublishEvent?.Invoke("OpenCraftingMenu", new {
            User = user,
            Skill = Name,
            Categories = new[] { "Weapons", "Armor", "Shields", "Tools" }
        });
        
        return true;
    }
    
    public bool TryCraft(Mobile user, int skillValue, int recipeId)
    {
        // Get recipe requirements
        // var recipe = GetRecipe(recipeId);
        
        // Check materials
        // Check skill requirement
        // Skill check for quality/success
        
        var rand = new Random();
        var successChance = skillValue / 1000f;
        
        if (rand.NextDouble() < successChance)
        {
            // Create item
            Context.PublishEvent?.Invoke("CraftSuccess", new {
                User = user,
                RecipeId = recipeId,
                Quality = CalculateQuality(skillValue)
            });
            
            return true;
        }
        else
        {
            Context.PublishEvent?.Invoke("CraftFail", new {
                User = user,
                RecipeId = recipeId,
                Message = "You fail to craft the item, wasting some materials."
            });
            
            return false;
        }
    }
    
    private string CalculateQuality(int skillValue)
    {
        var rand = new Random();
        var roll = rand.NextDouble() * 100 + skillValue / 10;
        
        if (roll >= 150) return "Exceptional";
        if (roll >= 120) return "Superior";
        if (roll >= 100) return "Good";
        return "Normal";
    }
}

/// <summary>
/// Magery skill - base spellcasting
/// </summary>
public class MageryScript : SkillScriptBase
{
    public override string Name => "Magery";
    
    public override bool OnUse(Mobile user, object? target)
    {
        // Magery is passive - it's used when casting spells
        // This could open a spellbook
        Context.PublishEvent?.Invoke("OpenSpellbook", new {
            User = user
        });
        
        return true;
    }
    
    /// <summary>
    /// Calculate spell success chance
    /// </summary>
    public float GetSpellSuccessChance(int magerySkill, int spellCircle)
    {
        // Similar to UO formula
        var minSkill = (spellCircle - 1) * 100;
        var maxSkill = spellCircle * 100;
        
        if (magerySkill < minSkill)
            return 0f;
        
        if (magerySkill >= maxSkill)
            return 1f;
        
        return (float)(magerySkill - minSkill) / (maxSkill - minSkill);
    }
    
    public override void OnGain(Mobile user, int newValue)
    {
        // Check for spell unlocks at skill thresholds
        var circle = newValue / 100;
        
        if (newValue % 100 == 0 && circle > 0)
        {
            Context.PublishEvent?.Invoke("SystemMessage", new {
                User = user,
                Message = $"Your magery skill has increased. You can now cast Circle {circle} spells!"
            });
        }
    }
}

/// <summary>
/// Animal Taming skill
/// </summary>
public class AnimalTamingScript : SkillScriptBase
{
    public override string Name => "AnimalTaming";
    
    public override bool CanUse(Mobile user, object? target, out string? failReason)
    {
        failReason = null;
        
        if (target is not NpcEntity npc)
        {
            failReason = "You cannot tame that.";
            return false;
        }
        
        // Check if creature is tameable
        // Would check NPC properties here
        
        // Check distance
        if (user.DistanceTo(npc) > 3f)
        {
            failReason = "You are too far away.";
            return false;
        }
        
        return true;
    }
    
    public override bool OnUse(Mobile user, object? target)
    {
        if (target is not NpcEntity npc)
            return false;
        
        Context.PublishEvent?.Invoke("StartTaming", new {
            User = user,
            Target = npc,
            Duration = 10f
        });
        
        Context.Logger.LogInformation("{User} is attempting to tame {Creature}", 
            user.Name, npc.Name);
        
        return true;
    }
    
    public override float GetDifficulty(Mobile user, object? target)
    {
        if (target is not NpcEntity npc)
            return 1f;
        
        // Higher level creatures are harder to tame
        return 0.3f + (npc.Level / 100f) * 0.7f;
    }
}
