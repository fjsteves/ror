using RealmOfReality.Shared.Core;
using RealmOfReality.Shared.Entities;
using RealmOfReality.Shared.Items;

namespace RealmOfReality.Server.Game;

/// <summary>
/// Handles using items (consumables, scrolls, etc.)
/// </summary>
public class ItemUseHandler
{
    private readonly ILogger _logger;
    private readonly Random _random = new();
    
    public ItemUseHandler(ILogger logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Use an item from inventory
    /// </summary>
    public async Task<ItemUseResult> UseItemAsync(PlayerEntity player, Item item, EntityId? targetId = null)
    {
        if (item.Definition == null)
            return new ItemUseResult(false, "Unknown item");
        
        var def = item.Definition;
        
        // Check if usable
        if (!def.Flags.HasFlag(ItemFlags.Usable))
            return new ItemUseResult(false, "This item cannot be used");
        
        // Handle based on category
        return def.Category switch
        {
            ItemCategory.Consumable => await UseConsumableAsync(player, item, targetId),
            _ => new ItemUseResult(false, "This item cannot be used")
        };
    }
    
    private async Task<ItemUseResult> UseConsumableAsync(PlayerEntity player, Item item, EntityId? targetId)
    {
        var def = item.Definition!;
        
        // Healing items
        if (def.HealAmount > 0)
        {
            return await UseHealingItemAsync(player, item);
        }
        
        // Mana items
        if (def.ManaAmount > 0)
        {
            return await UseManaItemAsync(player, item);
        }
        
        // Spell scrolls
        if (!string.IsNullOrEmpty(def.SpellEffect))
        {
            return await UseSpellScrollAsync(player, item, targetId);
        }
        
        return new ItemUseResult(false, "This consumable has no effect");
    }
    
    private Task<ItemUseResult> UseHealingItemAsync(PlayerEntity player, Item item)
    {
        var def = item.Definition!;
        
        if (player.Health >= player.MaxHealth)
            return Task.FromResult(new ItemUseResult(false, "You are already at full health"));
        
        var healed = player.Heal(def.HealAmount);
        ConsumeItem(player, item);
        
        _logger.LogInformation("Player {Name} used {Item}, healed {Amount} HP (now {Current}/{Max})",
            player.Name, def.Name, healed, player.Health, player.MaxHealth);
        
        return Task.FromResult(new ItemUseResult(true, $"You drink the {def.Name} and recover {healed} health.",
            new ItemUseEffect
            {
                Type = ItemUseEffectType.Heal,
                Value = healed,
                TargetId = player.Id
            }));
    }
    
    private Task<ItemUseResult> UseManaItemAsync(PlayerEntity player, Item item)
    {
        var def = item.Definition!;
        
        if (player.Mana >= player.MaxMana)
            return Task.FromResult(new ItemUseResult(false, "You are already at full mana"));
        
        var restored = Math.Min(def.ManaAmount, player.MaxMana - player.Mana);
        player.Mana += restored;
        ConsumeItem(player, item);
        
        _logger.LogInformation("Player {Name} used {Item}, restored {Amount} mana (now {Current}/{Max})",
            player.Name, def.Name, restored, player.Mana, player.MaxMana);
        
        return Task.FromResult(new ItemUseResult(true, $"You drink the {def.Name} and recover {restored} mana.",
            new ItemUseEffect
            {
                Type = ItemUseEffectType.RestoreMana,
                Value = restored,
                TargetId = player.Id
            }));
    }
    
    private Task<ItemUseResult> UseSpellScrollAsync(PlayerEntity player, Item item, EntityId? targetId)
    {
        var def = item.Definition!;
        
        // For now, we require a target for offensive scrolls
        if (def.MinDamage > 0 && targetId == null)
        {
            return Task.FromResult(new ItemUseResult(false, "Select a target first"));
        }
        
        // Calculate damage
        int damage = 0;
        if (def.MinDamage > 0)
        {
            damage = _random.Next(def.MinDamage, def.MaxDamage + 1);
            // TODO: Apply to target
        }
        
        ConsumeItem(player, item);
        
        _logger.LogInformation("Player {Name} used {Item} scroll, effect: {Effect}",
            player.Name, def.Name, def.SpellEffect);
        
        var effectType = def.SpellEffect switch
        {
            "Fireball" => ItemUseEffectType.Fireball,
            "Lightning" => ItemUseEffectType.Lightning,
            _ => ItemUseEffectType.SpellGeneric
        };
        
        return Task.FromResult(new ItemUseResult(true, $"You read the {def.Name} and cast {def.SpellEffect}!",
            new ItemUseEffect
            {
                Type = effectType,
                Value = damage,
                TargetId = targetId ?? player.Id,
                SourcePosition = player.Position
            }));
    }
    
    private void ConsumeItem(PlayerEntity player, Item item)
    {
        if (item.Amount > 1)
        {
            item.Amount--;
        }
        else
        {
            // Remove from inventory
            player.Inventory.Remove(item);
        }
    }
}

/// <summary>
/// Result of using an item
/// </summary>
public record ItemUseResult(bool Success, string Message, ItemUseEffect? Effect = null);

/// <summary>
/// Effect to play after using an item
/// </summary>
public class ItemUseEffect
{
    public ItemUseEffectType Type { get; init; }
    public int Value { get; init; }
    public EntityId? TargetId { get; init; }
    public Shared.Core.WorldPosition? SourcePosition { get; init; }
}

/// <summary>
/// Types of use effects
/// </summary>
public enum ItemUseEffectType
{
    None,
    Heal,
    RestoreMana,
    RestoreStamina,
    Fireball,
    Lightning,
    SpellGeneric,
    Buff,
    Debuff
}
