using RealmOfReality.Shared.Items;

namespace RealmOfReality.Shared.Scripts;

/// <summary>
/// RunUO-style loot pack system for generating monster drops
/// </summary>
public class LootPack
{
    public LootPackEntry[] Entries { get; }
    
    public LootPack(LootPackEntry[] entries)
    {
        Entries = entries;
    }
    
    /// <summary>
    /// Generate loot from this pack into a container
    /// </summary>
    public List<LootItem> Generate(int creatureLevel, int luck = 0)
    {
        var items = new List<LootItem>();
        var rand = Random.Shared;
        
        foreach (var entry in Entries)
        {
            // Check if this entry drops
            var chance = entry.Chance;
            if (luck > 0)
                chance += luck * 0.001; // 0.1% per luck point
            
            if (rand.NextDouble() * 100 < chance)
            {
                var item = entry.Generate(creatureLevel, rand);
                if (item != null)
                    items.Add(item);
            }
        }
        
        return items;
    }
    
    // ============ Standard Loot Packs (RunUO-style) ============
    
    public static readonly LootPack Poor = new(new[]
    {
        new LootPackEntry(LootType.Gold, 100.0, 5, 15),
    });
    
    public static readonly LootPack Meager = new(new[]
    {
        new LootPackEntry(LootType.Gold, 100.0, 15, 50),
        new LootPackEntry(LootType.Reagent, 20.0, 1, 3),
    });
    
    public static readonly LootPack Average = new(new[]
    {
        new LootPackEntry(LootType.Gold, 100.0, 50, 150),
        new LootPackEntry(LootType.Reagent, 30.0, 1, 5),
        new LootPackEntry(LootType.Potion, 15.0, 1, 1),
        new LootPackEntry(LootType.MagicItem, 5.0, 1, 1),
    });
    
    public static readonly LootPack Rich = new(new[]
    {
        new LootPackEntry(LootType.Gold, 100.0, 150, 400),
        new LootPackEntry(LootType.Reagent, 40.0, 2, 6),
        new LootPackEntry(LootType.Potion, 25.0, 1, 2),
        new LootPackEntry(LootType.MagicItem, 15.0, 1, 1),
        new LootPackEntry(LootType.Gem, 20.0, 1, 3),
    });
    
    public static readonly LootPack FilthyRich = new(new[]
    {
        new LootPackEntry(LootType.Gold, 100.0, 400, 800),
        new LootPackEntry(LootType.Reagent, 50.0, 3, 8),
        new LootPackEntry(LootType.Potion, 35.0, 1, 3),
        new LootPackEntry(LootType.MagicItem, 30.0, 1, 2),
        new LootPackEntry(LootType.Gem, 40.0, 2, 5),
        new LootPackEntry(LootType.Scroll, 20.0, 1, 2),
    });
    
    public static readonly LootPack UltraRich = new(new[]
    {
        new LootPackEntry(LootType.Gold, 100.0, 800, 1500),
        new LootPackEntry(LootType.Reagent, 60.0, 5, 10),
        new LootPackEntry(LootType.Potion, 50.0, 2, 4),
        new LootPackEntry(LootType.MagicItem, 50.0, 2, 3),
        new LootPackEntry(LootType.Gem, 60.0, 3, 8),
        new LootPackEntry(LootType.Scroll, 40.0, 2, 4),
        new LootPackEntry(LootType.RareItem, 10.0, 1, 1),
    });
    
    public static readonly LootPack SuperBoss = new(new[]
    {
        new LootPackEntry(LootType.Gold, 100.0, 1500, 3000),
        new LootPackEntry(LootType.Reagent, 80.0, 8, 15),
        new LootPackEntry(LootType.Potion, 70.0, 3, 6),
        new LootPackEntry(LootType.MagicItem, 80.0, 3, 5),
        new LootPackEntry(LootType.Gem, 80.0, 5, 12),
        new LootPackEntry(LootType.Scroll, 60.0, 3, 6),
        new LootPackEntry(LootType.RareItem, 30.0, 1, 2),
        new LootPackEntry(LootType.Artifact, 5.0, 1, 1),
    });
    
    // Specialty packs
    public static readonly LootPack Gems = new(new[]
    {
        new LootPackEntry(LootType.Gem, 100.0, 1, 5),
    });
    
    public static readonly LootPack Potions = new(new[]
    {
        new LootPackEntry(LootType.Potion, 100.0, 1, 3),
    });
    
    public static readonly LootPack MedScrolls = new(new[]
    {
        new LootPackEntry(LootType.Scroll, 100.0, 1, 3, 3, 5), // Circles 3-5
    });
    
    public static readonly LootPack HighScrolls = new(new[]
    {
        new LootPackEntry(LootType.Scroll, 100.0, 1, 2, 6, 8), // Circles 6-8
    });
}

/// <summary>
/// Single entry in a loot pack
/// </summary>
public class LootPackEntry
{
    public LootType Type { get; }
    public double Chance { get; }      // Percent chance (0-100)
    public int MinQuantity { get; }
    public int MaxQuantity { get; }
    public int MinLevel { get; }       // For scrolls/items
    public int MaxLevel { get; }
    
    public LootPackEntry(LootType type, double chance, int minQty, int maxQty, int minLevel = 1, int maxLevel = 8)
    {
        Type = type;
        Chance = chance;
        MinQuantity = minQty;
        MaxQuantity = maxQty;
        MinLevel = minLevel;
        MaxLevel = maxLevel;
    }
    
    public LootItem? Generate(int creatureLevel, Random rand)
    {
        var quantity = rand.Next(MinQuantity, MaxQuantity + 1);
        if (quantity <= 0) return null;
        
        return Type switch
        {
            LootType.Gold => new LootItem(LootType.Gold, "Gold", quantity, 0),
            LootType.Reagent => GenerateReagent(quantity, rand),
            LootType.Potion => GeneratePotion(quantity, rand),
            LootType.Gem => GenerateGem(quantity, rand),
            LootType.Scroll => GenerateScroll(rand),
            LootType.MagicItem => GenerateMagicItem(creatureLevel, rand),
            LootType.RareItem => GenerateRareItem(rand),
            LootType.Artifact => GenerateArtifact(rand),
            _ => null
        };
    }
    
    private static LootItem GenerateReagent(int qty, Random rand)
    {
        var reagents = new[] { "Black Pearl", "Blood Moss", "Garlic", "Ginseng", 
            "Mandrake Root", "Nightshade", "Spider Silk", "Sulfurous Ash" };
        var name = reagents[rand.Next(reagents.Length)];
        return new LootItem(LootType.Reagent, name, qty, 0x0F7A + rand.Next(8));
    }
    
    private static LootItem GeneratePotion(int qty, Random rand)
    {
        var potions = new[] { 
            ("Lesser Heal Potion", 0x0F0C, 1),
            ("Heal Potion", 0x0F0C, 2),
            ("Greater Heal Potion", 0x0F0C, 3),
            ("Lesser Cure Potion", 0x0F07, 1),
            ("Cure Potion", 0x0F07, 2),
            ("Mana Potion", 0x0F09, 2),
            ("Greater Mana Potion", 0x0F09, 3),
            ("Refresh Potion", 0x0F0B, 1),
            ("Strength Potion", 0x0F09, 2),
            ("Agility Potion", 0x0F08, 2),
        };
        var (name, itemId, _) = potions[rand.Next(potions.Length)];
        return new LootItem(LootType.Potion, name, qty, itemId);
    }
    
    private static LootItem GenerateGem(int qty, Random rand)
    {
        var gems = new[] { "Amber", "Amethyst", "Citrine", "Diamond", "Emerald", 
            "Ruby", "Sapphire", "Star Sapphire", "Tourmaline" };
        var name = gems[rand.Next(gems.Length)];
        return new LootItem(LootType.Gem, name, qty, 0x0F10 + rand.Next(gems.Length));
    }
    
    private LootItem GenerateScroll(Random rand)
    {
        var circle = rand.Next(MinLevel, MaxLevel + 1);
        return new LootItem(LootType.Scroll, $"Scroll (Circle {circle})", 1, 0x1F2D + circle);
    }
    
    private static LootItem GenerateMagicItem(int creatureLevel, Random rand)
    {
        var types = new[] { "Sword", "Axe", "Mace", "Bow", "Shield", 
            "Helm", "Chest Armor", "Leggings", "Boots", "Gloves", "Ring", "Amulet" };
        var prefixes = new[] { "", "Fine ", "Durable ", "Substantial ", "Massive ", 
            "Fortified ", "Hardened ", "Indestructible " };
        var suffixes = new[] { "", " of Defense", " of Guarding", " of Protection",
            " of Hardening", " of Fortification", " of Invulnerability" };
        
        var type = types[rand.Next(types.Length)];
        var prefix = rand.NextDouble() < 0.3 + creatureLevel * 0.02 ? prefixes[rand.Next(prefixes.Length)] : "";
        var suffix = rand.NextDouble() < 0.2 + creatureLevel * 0.02 ? suffixes[rand.Next(suffixes.Length)] : "";
        
        return new LootItem(LootType.MagicItem, $"{prefix}{type}{suffix}", 1, 0);
    }
    
    private static LootItem GenerateRareItem(Random rand)
    {
        var rares = new[] { "Blessed Runebook", "Bag of Sending", "Powder of Translocation",
            "Arcane Gem", "Crystal Ball", "Enchanted Sextant" };
        return new LootItem(LootType.RareItem, rares[rand.Next(rares.Length)], 1, 0);
    }
    
    private static LootItem GenerateArtifact(Random rand)
    {
        var artifacts = new[] { "The Berserker's Maul", "Blade of the Righteous", 
            "Bone Crusher", "Breath of the Dead", "Frostbringer", "Legbone of the Lich",
            "Staff of Power", "Voice of the Fallen King", "Zyronic Claw" };
        return new LootItem(LootType.Artifact, artifacts[rand.Next(artifacts.Length)], 1, 0);
    }
}

public enum LootType
{
    Gold,
    Reagent,
    Potion,
    Gem,
    Scroll,
    MagicItem,
    RareItem,
    Artifact
}

/// <summary>
/// Represents a generated loot item
/// </summary>
public class LootItem
{
    public LootType Type { get; }
    public string Name { get; }
    public int Quantity { get; }
    public int ItemId { get; }
    
    public LootItem(LootType type, string name, int quantity, int itemId)
    {
        Type = type;
        Name = name;
        Quantity = quantity;
        ItemId = itemId;
    }
}
