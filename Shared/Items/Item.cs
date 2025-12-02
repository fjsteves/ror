using RealmOfReality.Shared.Core;
using RealmOfReality.Shared.Serialization;

namespace RealmOfReality.Shared.Items;

/// <summary>
/// Unique identifier for items
/// </summary>
public readonly struct ItemId : IEquatable<ItemId>
{
    public readonly ulong Value;
    
    public ItemId(ulong value) => Value = value;
    
    public static ItemId Empty => new(0);
    public bool IsEmpty => Value == 0;
    
    public bool Equals(ItemId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is ItemId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"Item:{Value:X16}";
    
    public static bool operator ==(ItemId a, ItemId b) => a.Value == b.Value;
    public static bool operator !=(ItemId a, ItemId b) => a.Value != b.Value;
}

/// <summary>
/// UO-style equipment layers for paperdoll (matches ClassicUO)
/// </summary>
public enum Layer : byte
{
    Invalid = 0x00,
    OneHanded = 0x01,      // One-handed weapon
    TwoHanded = 0x02,      // Two-handed weapon, shield, or misc
    Shoes = 0x03,          // Shoes, sandals, boots
    Pants = 0x04,          // Pants, skirts
    Shirt = 0x05,          // Shirts
    Helm = 0x06,           // Helm, hat
    Gloves = 0x07,         // Gloves
    Ring = 0x08,           // Ring
    Talisman = 0x09,       // Talisman
    Necklace = 0x0A,       // Necklace
    Hair = 0x0B,           // Hair
    Waist = 0x0C,          // Waist (half apron)
    InnerTorso = 0x0D,     // Inner torso (chest armor under)
    Bracelet = 0x0E,       // Bracelet
    Face = 0x0F,           // Unused (face/makeup)
    FacialHair = 0x10,     // Facial hair (beard)
    MiddleTorso = 0x11,    // Middle torso (surcoat, tunic, full apron, sash)
    Earrings = 0x12,       // Earrings
    Arms = 0x13,           // Arms (arm armor)
    Cloak = 0x14,          // Cloak
    Backpack = 0x15,       // Backpack
    OuterTorso = 0x16,     // Outer torso (robe, dress)
    OuterLegs = 0x17,      // Outer legs (skirt, kilt)
    InnerLegs = 0x18,      // Inner legs (leg armor)
    Mount = 0x19,          // Mount
    ShopBuy = 0x1A,        // Shop buy container
    ShopResale = 0x1B,     // Shop resale container
    ShopSell = 0x1C,       // Shop sell container
    Bank = 0x1D,           // Bank box
}

/// <summary>
/// Legacy EquipmentSlot - kept for compatibility, maps to Layer
/// </summary>
public enum EquipmentSlot : byte
{
    None = 0,
    Head = (byte)Layer.Helm,
    Neck = (byte)Layer.Necklace,
    Chest = (byte)Layer.InnerTorso,
    Back = (byte)Layer.Cloak,
    Arms = (byte)Layer.Arms,
    Hands = (byte)Layer.Gloves,
    Waist = (byte)Layer.Waist,
    Legs = (byte)Layer.Pants,
    Feet = (byte)Layer.Shoes,
    Ring1 = (byte)Layer.Ring,
    Ring2 = (byte)Layer.Bracelet,
    MainHand = (byte)Layer.OneHanded,
    OffHand = (byte)Layer.TwoHanded,
    TwoHand = (byte)Layer.TwoHanded,
    Ammo = (byte)Layer.Talisman,
}

/// <summary>
/// Item categories
/// </summary>
public enum ItemCategory : byte
{
    None = 0,
    Weapon = 1,
    Armor = 2,
    Accessory = 3,
    Consumable = 4,
    Material = 5,
    Quest = 6,
    Container = 7,
    Tool = 8,
    Misc = 9,
}

/// <summary>
/// Item rarity/quality
/// </summary>
public enum ItemRarity : byte
{
    Common = 0,       // White
    Uncommon = 1,     // Green
    Rare = 2,         // Blue
    Epic = 3,         // Purple
    Legendary = 4,    // Orange
    Artifact = 5,     // Gold
}

/// <summary>
/// Damage types for weapons and spells
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
/// Base item definition (template)
/// </summary>
public class ItemDefinition
{
    public ushort TemplateId { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public ItemCategory Category { get; init; }
    public ItemRarity Rarity { get; init; }
    public Layer Layer { get; init; } = Layer.Invalid;
    
    // Legacy slot accessor for compatibility
    public EquipmentSlot Slot => (EquipmentSlot)Layer;
    
    // Visual - inventory art
    public ushort SpriteId { get; init; }
    public ushort Hue { get; init; }
    
    // Visual - paperdoll gump graphics
    public ushort GumpId { get; init; }           // Male paperdoll gump
    public ushort GumpIdFemale { get; init; }     // Female paperdoll gump
    public ushort PaperdollSpriteId { get; init; } // Legacy
    
    // Stack settings
    public bool IsStackable { get; init; }
    public int MaxStack { get; init; } = 1;
    
    // Value
    public int BuyPrice { get; init; }
    public int SellPrice { get; init; }
    public float Weight { get; init; }
    
    // Requirements
    public int RequiredLevel { get; init; }
    public int RequiredStrength { get; init; }
    public int RequiredDexterity { get; init; }
    public int RequiredIntelligence { get; init; }
    
    // Combat stats (for equipment)
    public int MinDamage { get; init; }
    public int MaxDamage { get; init; }
    public DamageType DamageType { get; init; } = DamageType.Physical;
    public float AttackSpeed { get; init; } = 1.0f;
    public float AttackRange { get; init; } = 1.0f;
    
    // Defense stats
    public int Armor { get; init; }
    public int MagicResist { get; init; }
    
    // Stat bonuses
    public int BonusStrength { get; init; }
    public int BonusDexterity { get; init; }
    public int BonusIntelligence { get; init; }
    public int BonusHealth { get; init; }
    public int BonusMana { get; init; }
    public int BonusStamina { get; init; }
    
    // Consumable effects
    public int HealAmount { get; init; }          // HP restored
    public int ManaAmount { get; init; }          // Mana restored
    public string? SpellEffect { get; init; }     // Spell name for scrolls
    
    // Script reference (for custom behaviors)
    public string? ScriptName { get; init; }
    
    // Flags
    public ItemFlags Flags { get; init; }
}

/// <summary>
/// Item flags
/// </summary>
[Flags]
public enum ItemFlags : ushort
{
    None = 0,
    Soulbound = 1 << 0,       // Cannot be traded
    Unique = 1 << 1,          // Only one can be owned
    QuestItem = 1 << 2,       // Quest related
    Consumable = 1 << 3,      // Destroyed on use
    Equipable = 1 << 4,       // Can be equipped
    Usable = 1 << 5,          // Has use action
    Tradeable = 1 << 6,       // Can be traded/sold
    Droppable = 1 << 7,       // Can be dropped
    Destructible = 1 << 8,    // Can be destroyed
    TwoHanded = 1 << 9,       // Requires both hands
    Ranged = 1 << 10,         // Ranged weapon
    Magic = 1 << 11,          // Magical item
    Blessed = 1 << 12,        // Protected from loss
    Cursed = 1 << 13,         // Cannot be unequipped normally
}

/// <summary>
/// Item instance - actual item in the game
/// </summary>
public class Item
{
    public ItemId Id { get; init; }
    public ushort TemplateId { get; set; }
    public int Amount { get; set; } = 1;
    public int Durability { get; set; } = 100;
    public int MaxDurability { get; set; } = 100;
    
    // Custom properties (enchantments, etc.)
    public Dictionary<string, object> Properties { get; } = new();
    
    // Runtime reference to definition
    public ItemDefinition? Definition { get; set; }
    
    // Location
    public ItemLocation Location { get; set; }
    public int SlotIndex { get; set; } // Slot in container/equipment
    
    // For items on the ground
    public WorldPosition? WorldPosition { get; set; }
    public ushort MapId { get; set; }
    
    public bool IsEquipped => Location == ItemLocation.Equipped;
    public bool IsInInventory => Location == ItemLocation.Inventory;
    public bool IsOnGround => Location == ItemLocation.Ground;
    
    public string Name => Definition?.Name ?? "Unknown Item";
    public ItemCategory Category => Definition?.Category ?? ItemCategory.None;
    public EquipmentSlot Slot => Definition?.Slot ?? EquipmentSlot.None;
    
    public void Serialize(PacketWriter writer)
    {
        writer.WriteUInt64(Id.Value);
        writer.WriteUInt16(TemplateId);
        writer.WriteInt32(Amount);
        writer.WriteInt16((short)Durability);
        writer.WriteInt16((short)MaxDurability);
        writer.WriteByte((byte)Location);
        writer.WriteInt16((short)SlotIndex);
    }
    
    public static Item Deserialize(ref PacketReader reader)
    {
        return new Item
        {
            Id = new ItemId(reader.ReadUInt64()),
            TemplateId = reader.ReadUInt16(),
            Amount = reader.ReadInt32(),
            Durability = reader.ReadInt16(),
            MaxDurability = reader.ReadInt16(),
            Location = (ItemLocation)reader.ReadByte(),
            SlotIndex = reader.ReadInt16()
        };
    }
}

/// <summary>
/// Where an item is located
/// </summary>
public enum ItemLocation : byte
{
    None = 0,
    Inventory = 1,
    Equipped = 2,
    Bank = 3,
    Ground = 4,
    Container = 5,
    Trade = 6,
    Mail = 7,
}

/// <summary>
/// Equipment container for a character (uses UO Layer system)
/// </summary>
public class Equipment
{
    private readonly Dictionary<Layer, Item?> _layers = new();
    
    public Equipment()
    {
        // Initialize all layers to empty
        foreach (Layer layer in Enum.GetValues<Layer>())
        {
            if (layer != Layer.Invalid)
                _layers[layer] = null;
        }
    }
    
    public Item? this[Layer layer]
    {
        get => _layers.GetValueOrDefault(layer);
        set => _layers[layer] = value;
    }
    
    // Legacy EquipmentSlot accessor
    public Item? this[EquipmentSlot slot]
    {
        get => _layers.GetValueOrDefault((Layer)slot);
        set => _layers[(Layer)slot] = value;
    }
    
    public Item? GetItem(Layer layer) => _layers.GetValueOrDefault(layer);
    public Item? GetItem(EquipmentSlot slot) => _layers.GetValueOrDefault((Layer)slot);
    
    public bool TryEquip(Item item, out Item? previousItem)
    {
        previousItem = null;
        
        if (item.Definition == null || item.Definition.Layer == Layer.Invalid)
            return false;
        
        var layer = item.Definition.Layer;
        
        // Handle two-handed weapons
        if (item.Definition.Flags.HasFlag(ItemFlags.TwoHanded))
        {
            // Unequip both hand slots
            previousItem = _layers[Layer.OneHanded];
            _layers[Layer.OneHanded] = null;
            _layers[Layer.TwoHanded] = item;
            item.Location = ItemLocation.Equipped;
            item.SlotIndex = (int)layer;
            return true;
        }
        
        previousItem = _layers[layer];
        _layers[layer] = item;
        item.Location = ItemLocation.Equipped;
        item.SlotIndex = (int)layer;
        
        if (previousItem != null)
        {
            previousItem.Location = ItemLocation.Inventory;
        }
        
        return true;
    }
    
    public Item? Unequip(Layer layer)
    {
        var item = _layers[layer];
        if (item != null)
        {
            item.Location = ItemLocation.Inventory;
            _layers[layer] = null;
        }
        return item;
    }
    
    public IEnumerable<(Layer Layer, Item Item)> GetAllEquipped()
    {
        foreach (var kvp in _layers)
        {
            if (kvp.Value != null)
                yield return (kvp.Key, kvp.Value);
        }
    }
    
    /// <summary>
    /// Get items in paperdoll render order (back to front)
    /// </summary>
    public IEnumerable<(Layer Layer, Item Item)> GetLayersInRenderOrder()
    {
        // UO renders layers in this order for paperdoll
        Layer[] renderOrder = new[]
        {
            Layer.Cloak,
            Layer.Pants,
            Layer.InnerLegs,
            Layer.Shoes,
            Layer.InnerTorso,
            Layer.Shirt,
            Layer.MiddleTorso,
            Layer.Arms,
            Layer.Gloves,
            Layer.OuterLegs,
            Layer.OuterTorso,
            Layer.Hair,
            Layer.FacialHair,
            Layer.Helm,
            Layer.OneHanded,
            Layer.TwoHanded,
        };
        
        foreach (var layer in renderOrder)
        {
            var item = _layers.GetValueOrDefault(layer);
            if (item != null)
                yield return (layer, item);
        }
    }
    
    /// <summary>
    /// Calculate total stat bonuses from equipment
    /// </summary>
    public EquipmentStats CalculateStats()
    {
        var stats = new EquipmentStats();
        
        foreach (var item in _layers.Values)
        {
            if (item?.Definition == null) continue;
            var def = item.Definition;
            
            stats.Armor += def.Armor;
            stats.MagicResist += def.MagicResist;
            stats.BonusStrength += def.BonusStrength;
            stats.BonusDexterity += def.BonusDexterity;
            stats.BonusIntelligence += def.BonusIntelligence;
            stats.BonusHealth += def.BonusHealth;
            stats.BonusMana += def.BonusMana;
            stats.BonusStamina += def.BonusStamina;
        }
        
        return stats;
    }
    
    public void Serialize(PacketWriter writer)
    {
        var equipped = _layers.Where(kvp => kvp.Value != null).ToList();
        writer.WriteByte((byte)equipped.Count);
        
        foreach (var kvp in equipped)
        {
            writer.WriteByte((byte)kvp.Key);
            kvp.Value!.Serialize(writer);
        }
    }
}

/// <summary>
/// Calculated stats from equipment
/// </summary>
public struct EquipmentStats
{
    public int Armor;
    public int MagicResist;
    public int BonusStrength;
    public int BonusDexterity;
    public int BonusIntelligence;
    public int BonusHealth;
    public int BonusMana;
    public int BonusStamina;
    public int MinDamage;
    public int MaxDamage;
    public float AttackSpeed;
}

/// <summary>
/// Player inventory
/// </summary>
public class Inventory
{
    private readonly Item?[] _slots;
    public int Capacity { get; }
    
    public Inventory(int capacity = 40)
    {
        Capacity = capacity;
        _slots = new Item?[capacity];
    }
    
    public Item? this[int index]
    {
        get => index >= 0 && index < Capacity ? _slots[index] : null;
        set
        {
            if (index >= 0 && index < Capacity)
                _slots[index] = value;
        }
    }
    
    public bool TryAdd(Item item, out int slotIndex)
    {
        slotIndex = -1;
        
        // Try to stack with existing
        if (item.Definition?.IsStackable == true)
        {
            for (int i = 0; i < Capacity; i++)
            {
                var existing = _slots[i];
                if (existing != null && 
                    existing.TemplateId == item.TemplateId &&
                    existing.Amount < (existing.Definition?.MaxStack ?? 1))
                {
                    var space = (existing.Definition?.MaxStack ?? 1) - existing.Amount;
                    var toAdd = Math.Min(space, item.Amount);
                    existing.Amount += toAdd;
                    item.Amount -= toAdd;
                    
                    if (item.Amount <= 0)
                    {
                        slotIndex = i;
                        return true;
                    }
                }
            }
        }
        
        // Find empty slot
        for (int i = 0; i < Capacity; i++)
        {
            if (_slots[i] == null)
            {
                _slots[i] = item;
                item.Location = ItemLocation.Inventory;
                item.SlotIndex = i;
                slotIndex = i;
                return true;
            }
        }
        
        return false; // Inventory full
    }
    
    public Item? Remove(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= Capacity)
            return null;
        
        var item = _slots[slotIndex];
        _slots[slotIndex] = null;
        return item;
    }
    
    public bool Remove(Item item)
    {
        for (int i = 0; i < Capacity; i++)
        {
            if (_slots[i] == item)
            {
                _slots[i] = null;
                return true;
            }
        }
        return false;
    }
    
    public int Count => _slots.Count(s => s != null);
    public int FreeSlots => Capacity - Count;
    
    public IEnumerable<(int Slot, Item Item)> GetAllItems()
    {
        for (int i = 0; i < Capacity; i++)
        {
            if (_slots[i] != null)
                yield return (i, _slots[i]!);
        }
    }
    
    public void Serialize(PacketWriter writer)
    {
        var items = GetAllItems().ToList();
        writer.WriteInt16((short)items.Count);
        
        foreach (var (slot, item) in items)
        {
            writer.WriteInt16((short)slot);
            item.Serialize(writer);
        }
    }
}
