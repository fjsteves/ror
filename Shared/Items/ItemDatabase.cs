namespace RealmOfReality.Shared.Items;

/// <summary>
/// Database of all item definitions with UO-authentic graphic IDs
/// </summary>
public static class ItemDatabase
{
    private static readonly Dictionary<ushort, ItemDefinition> _items = new();
    private static ulong _nextItemId = 1;
    
    static ItemDatabase()
    {
        RegisterAllItems();
    }
    
    public static ItemDefinition? Get(ushort templateId)
    {
        return _items.GetValueOrDefault(templateId);
    }
    
    public static IEnumerable<ItemDefinition> GetAll() => _items.Values;
    
    public static Item CreateItem(ushort templateId, int amount = 1)
    {
        var def = Get(templateId);
        if (def == null)
            throw new ArgumentException($"Unknown item template: {templateId}");
        
        return new Item
        {
            Id = new ItemId(_nextItemId++),
            TemplateId = templateId,
            Amount = amount,
            Definition = def,
            Durability = 100,
            MaxDurability = 100
        };
    }
    
    private static void Register(ItemDefinition def)
    {
        _items[def.TemplateId] = def;
    }
    
    private static void RegisterAllItems()
    {
        // ========================================
        // CLOTHING - Basic starting gear
        // ========================================
        
        // Plain Shirt (fancy shirt)
        Register(new ItemDefinition
        {
            TemplateId = 1,
            Name = "Fancy Shirt",
            Description = "A comfortable cloth shirt",
            Category = ItemCategory.Armor,
            Layer = Layer.Shirt,
            SpriteId = 0x1EFD,           // Art ID for shirt in inventory
            GumpId = 0x03E3,             // Gump ID for paperdoll (male)
            GumpIdFemale = 0x03E4,       // Gump ID for paperdoll (female)
            Hue = 0,
            Weight = 1.0f,
            BuyPrice = 10,
            SellPrice = 2,
            Flags = ItemFlags.Equipable | ItemFlags.Tradeable | ItemFlags.Droppable
        });
        
        // Long Pants
        Register(new ItemDefinition
        {
            TemplateId = 2,
            Name = "Long Pants",
            Description = "A pair of cloth pants",
            Category = ItemCategory.Armor,
            Layer = Layer.Pants,
            SpriteId = 0x1539,           // Art ID for pants
            GumpId = 0x03B6,             // Gump ID for paperdoll (male)
            GumpIdFemale = 0x03B7,       // Gump ID (female)
            Hue = 0,
            Weight = 2.0f,
            BuyPrice = 15,
            SellPrice = 3,
            Flags = ItemFlags.Equipable | ItemFlags.Tradeable | ItemFlags.Droppable
        });
        
        // Sandals
        Register(new ItemDefinition
        {
            TemplateId = 3,
            Name = "Sandals",
            Description = "Simple leather sandals",
            Category = ItemCategory.Armor,
            Layer = Layer.Shoes,
            SpriteId = 0x170D,           // Art ID for sandals
            GumpId = 0x0410,             // Gump ID for paperdoll
            GumpIdFemale = 0x0411,
            Hue = 0,
            Weight = 1.0f,
            BuyPrice = 8,
            SellPrice = 2,
            Flags = ItemFlags.Equipable | ItemFlags.Tradeable | ItemFlags.Droppable
        });
        
        // ========================================
        // HAIR STYLES (not equippable items, but used for character)
        // ========================================
        
        // Long Hair
        Register(new ItemDefinition
        {
            TemplateId = 100,
            Name = "Long Hair",
            Description = "Long flowing hair",
            Category = ItemCategory.Misc,
            Layer = Layer.Hair,
            SpriteId = 0x203C,           // Hair item art
            GumpId = 0x0064,             // Hair gump ID (male long)
            GumpIdFemale = 0x0048,       // Hair gump ID (female long)  
            Hue = 0x044E,                // Default brown
            Flags = ItemFlags.None       // Not tradeable
        });
        
        // Long Beard
        Register(new ItemDefinition
        {
            TemplateId = 101,
            Name = "Long Beard",
            Description = "A distinguished long beard",
            Category = ItemCategory.Misc,
            Layer = Layer.FacialHair,
            SpriteId = 0x2041,           // Beard item art
            GumpId = 0x0061,             // Long beard gump
            GumpIdFemale = 0,            // No female version
            Hue = 0x044E,                // Default brown
            Flags = ItemFlags.None
        });
        
        // ========================================
        // CONSUMABLES
        // ========================================
        
        // Greater Healing Potion
        Register(new ItemDefinition
        {
            TemplateId = 200,
            Name = "Greater Healing Potion",
            Description = "Restores a large amount of health",
            Category = ItemCategory.Consumable,
            Layer = Layer.Invalid,
            SpriteId = 0x0F0C,           // Red potion
            Hue = 0,
            IsStackable = true,
            MaxStack = 100,
            Weight = 0.1f,
            BuyPrice = 50,
            SellPrice = 15,
            HealAmount = 50,             // Heals 50 HP
            Flags = ItemFlags.Consumable | ItemFlags.Usable | ItemFlags.Tradeable | ItemFlags.Droppable
        });
        
        // Lesser Healing Potion
        Register(new ItemDefinition
        {
            TemplateId = 201,
            Name = "Lesser Healing Potion",
            Description = "Restores a small amount of health",
            Category = ItemCategory.Consumable,
            Layer = Layer.Invalid,
            SpriteId = 0x0F0C,
            Hue = 0,
            IsStackable = true,
            MaxStack = 100,
            Weight = 0.1f,
            BuyPrice = 15,
            SellPrice = 5,
            HealAmount = 15,
            Flags = ItemFlags.Consumable | ItemFlags.Usable | ItemFlags.Tradeable | ItemFlags.Droppable
        });
        
        // Scroll of Fireball
        Register(new ItemDefinition
        {
            TemplateId = 210,
            Name = "Scroll of Fireball",
            Description = "Casts a powerful fireball at the target",
            Category = ItemCategory.Consumable,
            Layer = Layer.Invalid,
            SpriteId = 0x1F4E,           // Scroll graphic
            Hue = 0,
            IsStackable = true,
            MaxStack = 100,
            Weight = 0.1f,
            BuyPrice = 75,
            SellPrice = 25,
            SpellEffect = "Fireball",
            MinDamage = 20,
            MaxDamage = 35,
            DamageType = DamageType.Fire,
            Flags = ItemFlags.Consumable | ItemFlags.Usable | ItemFlags.Tradeable | ItemFlags.Droppable | ItemFlags.Magic
        });
        
        // Scroll of Lightning
        Register(new ItemDefinition
        {
            TemplateId = 211,
            Name = "Scroll of Lightning",
            Description = "Calls down lightning on the target",
            Category = ItemCategory.Consumable,
            Layer = Layer.Invalid,
            SpriteId = 0x1F4E,
            Hue = 0x0480,                // Blue tint
            IsStackable = true,
            MaxStack = 100,
            Weight = 0.1f,
            BuyPrice = 100,
            SellPrice = 35,
            SpellEffect = "Lightning",
            MinDamage = 25,
            MaxDamage = 45,
            DamageType = DamageType.Lightning,
            Flags = ItemFlags.Consumable | ItemFlags.Usable | ItemFlags.Tradeable | ItemFlags.Droppable | ItemFlags.Magic
        });
        
        // ========================================
        // MISC ITEMS
        // ========================================
        
        // Bandages
        Register(new ItemDefinition
        {
            TemplateId = 300,
            Name = "Bandages",
            Description = "Clean bandages for healing wounds",
            Category = ItemCategory.Consumable,
            Layer = Layer.Invalid,
            SpriteId = 0x0E21,           // Bandage graphic
            Hue = 0,
            IsStackable = true,
            MaxStack = 100,
            Weight = 0.1f,
            BuyPrice = 5,
            SellPrice = 1,
            HealAmount = 10,
            Flags = ItemFlags.Consumable | ItemFlags.Usable | ItemFlags.Tradeable | ItemFlags.Droppable
        });
        
        // Gold Coin
        Register(new ItemDefinition
        {
            TemplateId = 301,
            Name = "Gold",
            Description = "Gold coins",
            Category = ItemCategory.Misc,
            Layer = Layer.Invalid,
            SpriteId = 0x0EED,           // Gold pile
            Hue = 0,
            IsStackable = true,
            MaxStack = 60000,
            Weight = 0.02f,
            BuyPrice = 1,
            SellPrice = 1,
            Flags = ItemFlags.Tradeable | ItemFlags.Droppable
        });
        
        // Torch
        Register(new ItemDefinition
        {
            TemplateId = 302,
            Name = "Torch",
            Description = "A lit torch providing light",
            Category = ItemCategory.Tool,
            Layer = Layer.TwoHanded,
            SpriteId = 0x0F6B,           // Torch graphic
            GumpId = 0x0488,
            GumpIdFemale = 0x0488,
            Hue = 0,
            Weight = 1.0f,
            BuyPrice = 8,
            SellPrice = 2,
            Flags = ItemFlags.Equipable | ItemFlags.Tradeable | ItemFlags.Droppable
        });
        
        // ========================================
        // WEAPONS
        // ========================================
        
        // Dagger
        Register(new ItemDefinition
        {
            TemplateId = 400,
            Name = "Dagger",
            Description = "A sharp dagger",
            Category = ItemCategory.Weapon,
            Layer = Layer.OneHanded,
            SpriteId = 0x0F52,           // Dagger art
            GumpId = 0x0453,
            GumpIdFemale = 0x0453,
            Hue = 0,
            Weight = 1.0f,
            BuyPrice = 25,
            SellPrice = 8,
            MinDamage = 3,
            MaxDamage = 10,
            AttackSpeed = 1.5f,
            AttackRange = 1.0f,
            DamageType = DamageType.Physical,
            Flags = ItemFlags.Equipable | ItemFlags.Tradeable | ItemFlags.Droppable
        });
        
        // Short Sword
        Register(new ItemDefinition
        {
            TemplateId = 401,
            Name = "Short Sword",
            Description = "A standard short sword",
            Category = ItemCategory.Weapon,
            Layer = Layer.OneHanded,
            SpriteId = 0x0F5E,
            GumpId = 0x045B,
            GumpIdFemale = 0x045B,
            Hue = 0,
            Weight = 4.0f,
            BuyPrice = 50,
            SellPrice = 15,
            MinDamage = 5,
            MaxDamage = 15,
            AttackSpeed = 1.25f,
            AttackRange = 1.0f,
            DamageType = DamageType.Physical,
            Flags = ItemFlags.Equipable | ItemFlags.Tradeable | ItemFlags.Droppable
        });
        
        // ========================================
        // ARMOR
        // ========================================
        
        // Leather Cap
        Register(new ItemDefinition
        {
            TemplateId = 500,
            Name = "Leather Cap",
            Description = "A simple leather cap",
            Category = ItemCategory.Armor,
            Layer = Layer.Helm,
            SpriteId = 0x1DB9,
            GumpId = 0x03D1,
            GumpIdFemale = 0x03D1,
            Hue = 0,
            Weight = 1.0f,
            BuyPrice = 20,
            SellPrice = 5,
            Armor = 2,
            Flags = ItemFlags.Equipable | ItemFlags.Tradeable | ItemFlags.Droppable
        });
        
        // Leather Gloves
        Register(new ItemDefinition
        {
            TemplateId = 501,
            Name = "Leather Gloves",
            Description = "Leather hand protection",
            Category = ItemCategory.Armor,
            Layer = Layer.Gloves,
            SpriteId = 0x13C6,
            GumpId = 0x03CC,
            GumpIdFemale = 0x03CC,
            Hue = 0,
            Weight = 1.0f,
            BuyPrice = 15,
            SellPrice = 4,
            Armor = 1,
            Flags = ItemFlags.Equipable | ItemFlags.Tradeable | ItemFlags.Droppable
        });
        
        // Cloak
        Register(new ItemDefinition
        {
            TemplateId = 502,
            Name = "Cloak",
            Description = "A flowing cloak",
            Category = ItemCategory.Armor,
            Layer = Layer.Cloak,
            SpriteId = 0x1515,
            GumpId = 0x0409,
            GumpIdFemale = 0x040A,
            Hue = 0,
            Weight = 2.0f,
            BuyPrice = 30,
            SellPrice = 8,
            Armor = 1,
            Flags = ItemFlags.Equipable | ItemFlags.Tradeable | ItemFlags.Droppable
        });
    }
}
