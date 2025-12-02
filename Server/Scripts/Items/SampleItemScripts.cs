// Sample Item Scripts for Realm of Reality
// These scripts demonstrate the item scripting system
// Save .cs files to Server/scripts/Items/ for auto-loading

using RealmOfReality.Server.Scripting;
using RealmOfReality.Shared.Core;
using RealmOfReality.Shared.Entities;
using RealmOfReality.Shared.Items;

namespace RealmOfReality.Scripts.Items;

/// <summary>
/// Health potion - restores health when used
/// </summary>
public class HealthPotionScript : ItemScriptBase
{
    public override string Name => "HealthPotion";
    
    public override bool OnUse(Item item, Mobile user, object? target)
    {
        // Calculate heal amount based on item properties
        var healAmount = item.Properties.GetValueOrDefault("HealAmount", 25) as int? ?? 25;
        
        var healed = user.Heal(healAmount);
        
        if (healed > 0)
        {
            Context.Logger.LogInformation("{User} used health potion, healed for {Amount}", 
                user.Name, healed);
            
            // Consume the item
            item.Amount--;
            
            // Play effects (would send to client)
            Context.PublishEvent?.Invoke("ItemUsed", new { Item = item, User = user, Effect = "Heal", Amount = healed });
            
            return true;
        }
        
        return false; // Already at full health
    }
    
    public override void OnPickup(Item item, Mobile picker)
    {
        Context.Logger.LogDebug("{User} picked up health potion", picker.Name);
    }
}

/// <summary>
/// Mana potion - restores mana when used
/// </summary>
public class ManaPotionScript : ItemScriptBase
{
    public override string Name => "ManaPotion";
    
    public override bool OnUse(Item item, Mobile user, object? target)
    {
        var restoreAmount = item.Properties.GetValueOrDefault("RestoreAmount", 25) as int? ?? 25;
        
        var oldMana = user.Mana;
        user.Mana = Math.Min(user.MaxMana, user.Mana + restoreAmount);
        var restored = user.Mana - oldMana;
        
        if (restored > 0)
        {
            item.Amount--;
            Context.PublishEvent?.Invoke("ItemUsed", new { Item = item, User = user, Effect = "RestoreMana", Amount = restored });
            return true;
        }
        
        return false;
    }
}

/// <summary>
/// Weapon with lifesteal effect
/// </summary>
public class VampiricBladeScript : ItemScriptBase
{
    public override string Name => "VampiricBlade";
    
    private const float LifestealPercent = 0.1f; // 10% lifesteal
    
    public override int OnHit(Item weapon, Mobile attacker, Mobile defender, int baseDamage)
    {
        // Apply lifesteal
        var lifesteal = (int)(baseDamage * LifestealPercent);
        if (lifesteal > 0)
        {
            attacker.Heal(lifesteal);
            Context.PublishEvent?.Invoke("Lifesteal", new { Attacker = attacker, Amount = lifesteal });
        }
        
        return baseDamage;
    }
    
    public override void OnEquip(Item item, Mobile wearer)
    {
        Context.Logger.LogInformation("{User} equipped the Vampiric Blade", wearer.Name);
        Context.PublishEvent?.Invoke("SpecialEquip", new { Item = item, User = wearer, Message = "The blade hungers..." });
    }
    
    public override void OnUnequip(Item item, Mobile wearer)
    {
        Context.PublishEvent?.Invoke("SpecialUnequip", new { Item = item, User = wearer, Message = "The blade reluctantly releases its grip." });
    }
}

/// <summary>
/// Scroll that teaches a spell
/// </summary>
public class SpellScrollScript : ItemScriptBase
{
    public override string Name => "SpellScroll";
    
    public override bool OnUse(Item item, Mobile user, object? target)
    {
        if (user is not PlayerEntity player)
            return false;
        
        var spellId = item.Properties.GetValueOrDefault("SpellId", 0) as int? ?? 0;
        if (spellId == 0)
            return false;
        
        // Check if player has enough magery skill
        var requiredSkill = item.Properties.GetValueOrDefault("RequiredSkill", 0) as int? ?? 0;
        
        // This would check the player's skills
        // For now, just consume and publish event
        item.Amount--;
        
        Context.PublishEvent?.Invoke("LearnSpell", new { Player = player, SpellId = spellId });
        Context.Logger.LogInformation("{Player} learned spell {SpellId} from scroll", player.Name, spellId);
        
        return true;
    }
}

/// <summary>
/// Cursed item that cannot be unequipped normally
/// </summary>
public class CursedAmuletScript : ItemScriptBase
{
    public override string Name => "CursedAmulet";
    
    public override void OnEquip(Item item, Mobile wearer)
    {
        Context.Logger.LogWarning("{User} equipped the Cursed Amulet!", wearer.Name);
        Context.PublishEvent?.Invoke("CurseApplied", new { Item = item, User = wearer });
        
        // Apply curse debuff
        item.Properties["IsCursed"] = true;
        
        // Stat penalty
        wearer.Intelligence -= 5;
    }
    
    public override void OnUnequip(Item item, Mobile wearer)
    {
        // This would normally be blocked by the curse
        // Only called if curse is removed first
        wearer.Intelligence += 5;
        item.Properties.Remove("IsCursed");
        Context.PublishEvent?.Invoke("CurseRemoved", new { Item = item, User = wearer });
    }
}

/// <summary>
/// Container item (bag, chest)
/// </summary>
public class ContainerScript : ItemScriptBase
{
    public override string Name => "Container";
    
    public override bool OnUse(Item item, Mobile user, object? target)
    {
        // Open container UI
        Context.PublishEvent?.Invoke("OpenContainer", new { Container = item, User = user });
        return true;
    }
    
    public override void OnDrop(Item item, Mobile dropper, WorldPosition location)
    {
        // Containers can be placed in the world
        Context.Logger.LogInformation("{User} placed container at {Location}", dropper.Name, location);
    }
}

/// <summary>
/// Key item for opening locked doors/chests
/// </summary>
public class KeyScript : ItemScriptBase
{
    public override string Name => "Key";
    
    public override bool OnUse(Item item, Mobile user, object? target)
    {
        var keyId = item.Properties.GetValueOrDefault("KeyId", 0) as int? ?? 0;
        
        if (target == null)
        {
            Context.PublishEvent?.Invoke("SystemMessage", new { User = user, Message = "Target a door or container to unlock." });
            return false;
        }
        
        // Check if target has matching lock
        // This would integrate with the door/container system
        Context.PublishEvent?.Invoke("TryUnlock", new { Key = item, User = user, Target = target, KeyId = keyId });
        
        return true;
    }
}

/// <summary>
/// Reagent item (used for spellcasting)
/// </summary>
public class ReagentScript : ItemScriptBase
{
    public override string Name => "Reagent";
    
    // Reagents are mostly passive - they're consumed when casting spells
    // No special OnUse behavior
}

/// <summary>
/// Food item that restores stamina over time
/// </summary>
public class FoodScript : ItemScriptBase
{
    public override string Name => "Food";
    
    public override bool OnUse(Item item, Mobile user, object? target)
    {
        var nutritionValue = item.Properties.GetValueOrDefault("Nutrition", 10) as int? ?? 10;
        
        // Apply eating buff
        Context.PublishEvent?.Invoke("StartEating", new { 
            User = user, 
            Item = item,
            Duration = nutritionValue * 2, // Duration in seconds
            StaminaPerTick = nutritionValue / 10
        });
        
        item.Amount--;
        
        return true;
    }
}
