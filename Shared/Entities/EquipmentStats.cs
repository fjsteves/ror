using RealmOfReality.Shared.Items;

namespace RealmOfReality.Shared.Entities;

/// <summary>
/// Calculates total stat bonuses from equipped items
/// </summary>
public class EquipmentStats
{
    // Offensive stats
    public int MinDamage { get; set; }
    public int MaxDamage { get; set; }
    public float AttackSpeed { get; set; } = 1.0f;
    public float DPS => (MinDamage + MaxDamage) / 2f * AttackSpeed;
    
    // Defensive stats
    public int TotalArmor { get; set; }
    public int TotalMagicResist { get; set; }
    public float DamageReduction => CalculateDamageReduction(TotalArmor);
    
    // Primary stat bonuses
    public int BonusStrength { get; set; }
    public int BonusDexterity { get; set; }
    public int BonusIntelligence { get; set; }
    
    // Secondary stat bonuses
    public int BonusHealth { get; set; }
    public int BonusMana { get; set; }
    public int BonusStamina { get; set; }
    
    // Derived bonuses from primary stats
    public int StrengthDamageBonus => BonusStrength / 5;
    public int DexterityHitBonus => BonusDexterity / 10;
    public int IntelligenceManaBonus => BonusIntelligence * 5;
    
    /// <summary>
    /// Calculates damage reduction percentage from armor
    /// Uses UO-style formula: reduction = armor / (armor + 50)
    /// </summary>
    private static float CalculateDamageReduction(int armor)
    {
        if (armor <= 0) return 0;
        return 100f * armor / (armor + 50f);
    }
    
    /// <summary>
    /// Calculate equipment stats from an equipment set
    /// </summary>
    public static EquipmentStats Calculate(Equipment equipment)
    {
        var stats = new EquipmentStats();
        
        foreach (var (slot, item) in equipment.GetAllEquipped())
        {
            if (item?.Definition == null) continue;
            var def = item.Definition;
            
            // Skip hair and such
            if (slot == Layer.Invalid || slot == Layer.Hair || slot == Layer.FacialHair) continue;
            
            // Weapon damage (from main weapon only)
            if (slot == Layer.OneHanded || slot == Layer.TwoHanded)
            {
                stats.MinDamage = def.MinDamage;
                stats.MaxDamage = def.MaxDamage;
                stats.AttackSpeed = def.AttackSpeed > 0 ? def.AttackSpeed : 1.0f;
            }
            
            // Accumulate armor
            stats.TotalArmor += def.Armor;
            stats.TotalMagicResist += def.MagicResist;
            
            // Accumulate stat bonuses
            stats.BonusStrength += def.BonusStrength;
            stats.BonusDexterity += def.BonusDexterity;
            stats.BonusIntelligence += def.BonusIntelligence;
            stats.BonusHealth += def.BonusHealth;
            stats.BonusMana += def.BonusMana;
            stats.BonusStamina += def.BonusStamina;
        }
        
        return stats;
    }
    
    /// <summary>
    /// Get a summary string of all bonuses
    /// </summary>
    public string GetSummary()
    {
        var lines = new List<string>();
        
        if (MinDamage > 0 || MaxDamage > 0)
            lines.Add($"Damage: {MinDamage}-{MaxDamage} ({DPS:F1} DPS)");
        
        if (TotalArmor > 0)
            lines.Add($"Armor: {TotalArmor} ({DamageReduction:F1}% reduction)");
        
        if (TotalMagicResist > 0)
            lines.Add($"Magic Resist: {TotalMagicResist}");
        
        if (BonusStrength > 0)
            lines.Add($"+{BonusStrength} Strength");
        if (BonusDexterity > 0)
            lines.Add($"+{BonusDexterity} Dexterity");
        if (BonusIntelligence > 0)
            lines.Add($"+{BonusIntelligence} Intelligence");
        
        if (BonusHealth > 0)
            lines.Add($"+{BonusHealth} Health");
        if (BonusMana > 0)
            lines.Add($"+{BonusMana} Mana");
        if (BonusStamina > 0)
            lines.Add($"+{BonusStamina} Stamina");
        
        return string.Join("\n", lines);
    }
}

/// <summary>
/// Extension methods for equipment stat calculation
/// </summary>
public static class EquipmentExtensions
{
    /// <summary>
    /// Get all equipped items
    /// </summary>
    public static IEnumerable<(EquipmentSlot Slot, Item? Item)> GetAllEquipped(this Equipment equipment)
    {
        // Iterate through all slots
        yield return (EquipmentSlot.Head, equipment[EquipmentSlot.Head]);
        yield return (EquipmentSlot.Neck, equipment[EquipmentSlot.Neck]);
        yield return (EquipmentSlot.Chest, equipment[EquipmentSlot.Chest]);
        yield return (EquipmentSlot.Back, equipment[EquipmentSlot.Back]);
        yield return (EquipmentSlot.Arms, equipment[EquipmentSlot.Arms]);
        yield return (EquipmentSlot.Hands, equipment[EquipmentSlot.Hands]);
        yield return (EquipmentSlot.Waist, equipment[EquipmentSlot.Waist]);
        yield return (EquipmentSlot.Legs, equipment[EquipmentSlot.Legs]);
        yield return (EquipmentSlot.Feet, equipment[EquipmentSlot.Feet]);
        yield return (EquipmentSlot.Ring1, equipment[EquipmentSlot.Ring1]);
        yield return (EquipmentSlot.Ring2, equipment[EquipmentSlot.Ring2]);
        yield return (EquipmentSlot.MainHand, equipment[EquipmentSlot.MainHand]);
        yield return (EquipmentSlot.OffHand, equipment[EquipmentSlot.OffHand]);
        yield return (EquipmentSlot.Ammo, equipment[EquipmentSlot.Ammo]);
    }
    
    /// <summary>
    /// Check if an item can be equipped in a given slot
    /// </summary>
    public static bool CanEquipInSlot(this ItemDefinition def, EquipmentSlot targetSlot)
    {
        // Check if item is equipable
        if ((def.Flags & ItemFlags.Equipable) == 0 && def.Layer == Layer.Invalid)
            return false;
        
        // Get the item's natural slot
        var itemSlot = def.Slot;
        
        // Direct match
        if (itemSlot == targetSlot) return true;
        
        // Rings can go in either ring slot
        if (itemSlot == EquipmentSlot.Ring1 && targetSlot == EquipmentSlot.Ring2) return true;
        if (itemSlot == EquipmentSlot.Ring2 && targetSlot == EquipmentSlot.Ring1) return true;
        
        return false;
    }
    
    /// <summary>
    /// Check if player meets requirements to equip an item
    /// </summary>
    public static EquipCheckResult CheckEquipRequirements(this ItemDefinition def, int playerLevel, int strength, int dexterity, int intelligence)
    {
        if (def.RequiredLevel > playerLevel)
            return EquipCheckResult.Fail($"Requires level {def.RequiredLevel}");
        
        if (def.RequiredStrength > strength)
            return EquipCheckResult.Fail($"Requires {def.RequiredStrength} Strength");
        
        if (def.RequiredDexterity > dexterity)
            return EquipCheckResult.Fail($"Requires {def.RequiredDexterity} Dexterity");
        
        if (def.RequiredIntelligence > intelligence)
            return EquipCheckResult.Fail($"Requires {def.RequiredIntelligence} Intelligence");
        
        return EquipCheckResult.Success();
    }
}

public struct EquipCheckResult
{
    public bool CanEquip;
    public string? Reason;
    
    public static EquipCheckResult Success() => new() { CanEquip = true };
    public static EquipCheckResult Fail(string reason) => new() { CanEquip = false, Reason = reason };
}
