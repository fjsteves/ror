namespace RealmOfReality.Shared.Scripts;

/// <summary>
/// RunUO-style creature definitions - determines stats, behavior, and loot
/// Each creature type has a script that defines its properties
/// </summary>
public static class CreatureScripts
{
    /// <summary>
    /// Get the creature definition by TypeId
    /// </summary>
    public static CreatureDefinition? GetDefinition(ushort typeId)
    {
        return typeId switch
        {
            10 => Goblin,
            20 => Skeleton,
            30 => Wolf,
            40 => Healer,
            50 => Ankh,
            60 => Orc,
            70 => Troll,
            80 => Lich,
            90 => Daemon,
            100 => Dragon,
            // Vendors (200+)
            200 => Blacksmith,
            201 => Provisioner,
            202 => Mage,
            203 => Healer,
            204 => Innkeeper,
            205 => Tailor,
            206 => Jeweler,
            207 => Banker,
            _ => null
        };
    }
    
    // ============ Monsters ============
    
    public static readonly CreatureDefinition Goblin = new()
    {
        Name = "Goblin",
        TypeId = 10,
        CorpseName = "a goblin corpse",
        MinLevel = 1, MaxLevel = 5,
        // Stats
        MinHits = 30, MaxHits = 50,
        MinDamage = 3, MaxDamage = 8,
        MinStr = 50, MaxStr = 70,
        MinDex = 40, MaxDex = 60,
        MinInt = 20, MaxInt = 40,
        // Combat
        AttackSpeed = 1.5f,
        AggroRadius = 6f,
        // Loot
        LootPacks = new[] { LootPack.Poor },
        Fame = 300,
        Karma = -300,
    };
    
    public static readonly CreatureDefinition Skeleton = new()
    {
        Name = "Skeleton",
        TypeId = 20,
        CorpseName = "a skeleton corpse",
        MinLevel = 3, MaxLevel = 7,
        MinHits = 40, MaxHits = 60,
        MinDamage = 5, MaxDamage = 12,
        MinStr = 60, MaxStr = 80,
        MinDex = 50, MaxDex = 70,
        MinInt = 30, MaxInt = 50,
        AttackSpeed = 1.2f,
        AggroRadius = 8f,
        LootPacks = new[] { LootPack.Meager },
        Fame = 450,
        Karma = -450,
        IsUndead = true,
    };
    
    public static readonly CreatureDefinition Wolf = new()
    {
        Name = "Grey Wolf",
        TypeId = 30,
        CorpseName = "a wolf corpse",
        MinLevel = 2, MaxLevel = 5,
        MinHits = 35, MaxHits = 55,
        MinDamage = 4, MaxDamage = 10,
        MinStr = 60, MaxStr = 80,
        MinDex = 70, MaxDex = 90,
        MinInt = 20, MaxInt = 35,
        AttackSpeed = 1.0f,
        AggroRadius = 10f,
        LootPacks = new[] { LootPack.Poor },
        Fame = 350,
        Karma = 0, // Animals are neutral
        CanBeCarved = true,
        CarveItems = new[] { ("Raw Ribs", 2, 4), ("Fur", 1, 2) },
    };
    
    public static readonly CreatureDefinition Orc = new()
    {
        Name = "Orc",
        TypeId = 60,
        CorpseName = "an orc corpse",
        MinLevel = 5, MaxLevel = 10,
        MinHits = 80, MaxHits = 120,
        MinDamage = 8, MaxDamage = 18,
        MinStr = 100, MaxStr = 130,
        MinDex = 60, MaxDex = 80,
        MinInt = 40, MaxInt = 60,
        AttackSpeed = 1.3f,
        AggroRadius = 10f,
        LootPacks = new[] { LootPack.Average },
        Fame = 1000,
        Karma = -1000,
    };
    
    public static readonly CreatureDefinition Troll = new()
    {
        Name = "Troll",
        TypeId = 70,
        CorpseName = "a troll corpse",
        MinLevel = 8, MaxLevel = 15,
        MinHits = 150, MaxHits = 200,
        MinDamage = 12, MaxDamage = 25,
        MinStr = 150, MaxStr = 200,
        MinDex = 40, MaxDex = 60,
        MinInt = 30, MaxInt = 50,
        AttackSpeed = 2.0f,
        AggroRadius = 8f,
        LootPacks = new[] { LootPack.Rich },
        Fame = 2500,
        Karma = -2500,
        Regeneration = 5, // HP per tick
    };
    
    public static readonly CreatureDefinition Lich = new()
    {
        Name = "Lich",
        TypeId = 80,
        CorpseName = "a lich corpse",
        MinLevel = 12, MaxLevel = 20,
        MinHits = 120, MaxHits = 180,
        MinDamage = 15, MaxDamage = 30,
        MinStr = 70, MaxStr = 100,
        MinDex = 60, MaxDex = 80,
        MinInt = 150, MaxInt = 200,
        AttackSpeed = 1.5f,
        AggroRadius = 12f,
        LootPacks = new[] { LootPack.Rich, LootPack.MedScrolls },
        Fame = 4000,
        Karma = -4000,
        IsUndead = true,
        CanCastSpells = true,
    };
    
    public static readonly CreatureDefinition Daemon = new()
    {
        Name = "Daemon",
        TypeId = 90,
        CorpseName = "a daemon corpse",
        MinLevel = 15, MaxLevel = 25,
        MinHits = 250, MaxHits = 350,
        MinDamage = 20, MaxDamage = 40,
        MinStr = 200, MaxStr = 250,
        MinDex = 100, MaxDex = 130,
        MinInt = 200, MaxInt = 250,
        AttackSpeed = 1.2f,
        AggroRadius = 15f,
        LootPacks = new[] { LootPack.FilthyRich, LootPack.HighScrolls },
        Fame = 8000,
        Karma = -8000,
        CanCastSpells = true,
        MagicResist = 80,
    };
    
    public static readonly CreatureDefinition Dragon = new()
    {
        Name = "Ancient Dragon",
        TypeId = 100,
        CorpseName = "a dragon corpse",
        MinLevel = 20, MaxLevel = 35,
        MinHits = 500, MaxHits = 800,
        MinDamage = 35, MaxDamage = 60,
        MinStr = 400, MaxStr = 500,
        MinDex = 80, MaxDex = 120,
        MinInt = 300, MaxInt = 400,
        AttackSpeed = 2.0f,
        AggroRadius = 20f,
        LootPacks = new[] { LootPack.SuperBoss, LootPack.Gems },
        Fame = 20000,
        Karma = -20000,
        CanCastSpells = true,
        CanFly = true,
        BreathAttack = true,
        MagicResist = 90,
    };
    
    // ============ NPCs ============
    
    public static readonly CreatureDefinition Healer = new()
    {
        Name = "Healer",
        TypeId = 40,
        CorpseName = "a healer corpse",
        IsVendor = true,
        IsInvulnerable = true,
        CanResurrect = true,
    };
    
    public static readonly CreatureDefinition Ankh = new()
    {
        Name = "Ankh of Resurrection",
        TypeId = 50,
        IsStatic = true,
        CanResurrect = true,
    };
    
    // ============ Vendors ============
    
    public static readonly CreatureDefinition Blacksmith = new()
    {
        Name = "Blacksmith",
        TypeId = 200,
        IsVendor = true,
        IsInvulnerable = true,
        VendorType = VendorType.Blacksmith,
        SellsItems = new[] { "Iron Ingot", "Pickaxe", "Hammer", "Tongs", "Dagger", "Sword", "Axe", "Mace", "Chainmail", "Platemail" },
        BuysItems = new[] { "Weapon", "Armor", "Ore", "Ingot" },
    };
    
    public static readonly CreatureDefinition Provisioner = new()
    {
        Name = "Provisioner",
        TypeId = 201,
        IsVendor = true,
        IsInvulnerable = true,
        VendorType = VendorType.Provisioner,
        SellsItems = new[] { "Backpack", "Pouch", "Bag", "Torch", "Lantern", "Bedroll", "Fishing Pole", "Dyes" },
        BuysItems = new[] { "Cloth", "Leather", "Feathers" },
    };
    
    public static readonly CreatureDefinition Mage = new()
    {
        Name = "Mage",
        TypeId = 202,
        IsVendor = true,
        IsInvulnerable = true,
        VendorType = VendorType.Mage,
        SellsItems = new[] { "Spellbook", "Recall Scroll", "Gate Travel Scroll", "Black Pearl", "Blood Moss", "Garlic", "Ginseng", "Mandrake Root", "Nightshade", "Spider Silk", "Sulfurous Ash" },
        BuysItems = new[] { "Reagent", "Scroll", "Wand" },
    };
    
    public static readonly CreatureDefinition Innkeeper = new()
    {
        Name = "Innkeeper",
        TypeId = 204,
        IsVendor = true,
        IsInvulnerable = true,
        VendorType = VendorType.Innkeeper,
        SellsItems = new[] { "Bread", "Cheese", "Apple", "Grapes", "Pitcher of Water", "Bottle of Wine", "Bottle of Ale" },
        BuysItems = new[] { "Food" },
    };
    
    public static readonly CreatureDefinition Tailor = new()
    {
        Name = "Tailor",
        TypeId = 205,
        IsVendor = true,
        IsInvulnerable = true,
        VendorType = VendorType.Tailor,
        SellsItems = new[] { "Cloth", "Scissors", "Sewing Kit", "Shirt", "Pants", "Shoes", "Hat", "Cloak", "Robe" },
        BuysItems = new[] { "Cloth", "Clothing", "Leather" },
    };
    
    public static readonly CreatureDefinition Jeweler = new()
    {
        Name = "Jeweler",
        TypeId = 206,
        IsVendor = true,
        IsInvulnerable = true,
        VendorType = VendorType.Jeweler,
        SellsItems = new[] { "Ring", "Bracelet", "Necklace", "Earrings", "Amber", "Amethyst", "Citrine", "Diamond", "Emerald", "Ruby", "Sapphire" },
        BuysItems = new[] { "Gem", "Jewelry" },
    };
    
    public static readonly CreatureDefinition Banker = new()
    {
        Name = "Banker",
        TypeId = 207,
        IsVendor = true,
        IsInvulnerable = true,
        VendorType = VendorType.Banker,
        CanAccessBank = true,
    };
}

/// <summary>
/// Definition of a creature type
/// </summary>
public class CreatureDefinition
{
    // Identity
    public string Name { get; init; } = "";
    public ushort TypeId { get; init; }
    public string CorpseName { get; init; } = "a corpse";
    
    // Level range
    public int MinLevel { get; init; } = 1;
    public int MaxLevel { get; init; } = 1;
    
    // Stats
    public int MinHits { get; init; } = 50;
    public int MaxHits { get; init; } = 100;
    public int MinDamage { get; init; } = 1;
    public int MaxDamage { get; init; } = 5;
    public int MinStr { get; init; } = 10;
    public int MaxStr { get; init; } = 25;
    public int MinDex { get; init; } = 10;
    public int MaxDex { get; init; } = 25;
    public int MinInt { get; init; } = 10;
    public int MaxInt { get; init; } = 25;
    
    // Combat
    public float AttackSpeed { get; init; } = 1.5f;
    public float AggroRadius { get; init; } = 8f;
    public int MagicResist { get; init; } = 0;
    public int Regeneration { get; init; } = 0;
    
    // Loot
    public LootPack[] LootPacks { get; init; } = Array.Empty<LootPack>();
    public int Fame { get; init; } = 0;
    public int Karma { get; init; } = 0;
    
    // Flags
    public bool IsUndead { get; init; }
    public bool CanCastSpells { get; init; }
    public bool CanFly { get; init; }
    public bool BreathAttack { get; init; }
    public bool IsVendor { get; init; }
    public bool IsInvulnerable { get; init; }
    public bool IsStatic { get; init; }
    public bool CanResurrect { get; init; }
    public bool CanBeCarved { get; init; }
    public bool CanAccessBank { get; init; }
    
    // Carve
    public (string item, int min, int max)[] CarveItems { get; init; } = Array.Empty<(string, int, int)>();
    
    // Vendor
    public VendorType VendorType { get; init; }
    public string[] SellsItems { get; init; } = Array.Empty<string>();
    public string[] BuysItems { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// Generate loot for this creature
    /// </summary>
    public List<LootItem> GenerateLoot(int actualLevel)
    {
        var allLoot = new List<LootItem>();
        foreach (var pack in LootPacks)
        {
            allLoot.AddRange(pack.Generate(actualLevel));
        }
        return allLoot;
    }
    
    /// <summary>
    /// Roll random stats within ranges
    /// </summary>
    public (int hits, int str, int dex, int intel, int dmgMin, int dmgMax) RollStats(Random rand)
    {
        return (
            rand.Next(MinHits, MaxHits + 1),
            rand.Next(MinStr, MaxStr + 1),
            rand.Next(MinDex, MaxDex + 1),
            rand.Next(MinInt, MaxInt + 1),
            MinDamage,
            MaxDamage
        );
    }
}

public enum VendorType
{
    None,
    Blacksmith,
    Provisioner,
    Mage,
    Innkeeper,
    Tailor,
    Jeweler,
    Banker,
    Healer
}
