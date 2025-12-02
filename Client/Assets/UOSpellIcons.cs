namespace RealmOfReality.Client.Assets;

/// <summary>
/// Maps UO spell IDs to gump icon IDs
/// Based on the standard UO Magery spellbook icons
/// 
/// Spell icons in UO are stored in gumpart.mul
/// Small icons start at gump ID 2240 for first circle
/// Large icons have different IDs
/// </summary>
public static class UOSpellIcons
{
    // Base gump IDs for spell icons
    // Small spell icons (used in spellbooks and hotbars)
    public const int SmallIconBase = 2240;
    
    // Spell scroll background
    public const int ScrollBackground = 2200;
    
    // Spellbook gumps
    public const int SpellbookBackground = 2220;
    public const int SpellbookPageLeft = 2221;
    public const int SpellbookPageRight = 2222;
    
    /// <summary>
    /// Get the small icon gump ID for a spell
    /// </summary>
    public static int GetSmallIcon(int spellId)
    {
        // UO spell icons are sequential starting at 2240
        // Each circle has 8 spells
        return SmallIconBase + spellId;
    }
    
    /// <summary>
    /// Mapping of spell names to their IDs and gump icons
    /// </summary>
    public static readonly Dictionary<string, SpellIconInfo> Spells = new()
    {
        // First Circle (0-7)
        ["Clumsy"] = new(0, 2240, 0x1B58),
        ["Create Food"] = new(1, 2241, 0x1B59),
        ["Feeblemind"] = new(2, 2242, 0x1B5A),
        ["Heal"] = new(3, 2243, 0x1B5B),
        ["Magic Arrow"] = new(4, 2244, 0x1B5C),
        ["Night Sight"] = new(5, 2245, 0x1B5D),
        ["Reactive Armor"] = new(6, 2246, 0x1B5E),
        ["Weaken"] = new(7, 2247, 0x1B5F),
        
        // Second Circle (8-15)
        ["Agility"] = new(8, 2248, 0x1B60),
        ["Cunning"] = new(9, 2249, 0x1B61),
        ["Cure"] = new(10, 2250, 0x1B62),
        ["Harm"] = new(11, 2251, 0x1B63),
        ["Magic Trap"] = new(12, 2252, 0x1B64),
        ["Magic Untrap"] = new(13, 2253, 0x1B65),
        ["Protection"] = new(14, 2254, 0x1B66),
        ["Strength"] = new(15, 2255, 0x1B67),
        
        // Third Circle (16-23)
        ["Bless"] = new(16, 2256, 0x1B68),
        ["Fireball"] = new(17, 2257, 0x1B69),
        ["Magic Lock"] = new(18, 2258, 0x1B6A),
        ["Poison"] = new(19, 2259, 0x1B6B),
        ["Telekinesis"] = new(20, 2260, 0x1B6C),
        ["Teleport"] = new(21, 2261, 0x1B6D),
        ["Unlock"] = new(22, 2262, 0x1B6E),
        ["Wall of Stone"] = new(23, 2263, 0x1B6F),
        
        // Fourth Circle (24-31)
        ["Archcure"] = new(24, 2264, 0x1B70),
        ["Archprotection"] = new(25, 2265, 0x1B71),
        ["Curse"] = new(26, 2266, 0x1B72),
        ["Fire Field"] = new(27, 2267, 0x1B73),
        ["Greater Heal"] = new(28, 2268, 0x1B74),
        ["Lightning"] = new(29, 2269, 0x1B75),
        ["Mana Drain"] = new(30, 2270, 0x1B76),
        ["Recall"] = new(31, 2271, 0x1B77),
        
        // Fifth Circle (32-39)
        ["Blade Spirits"] = new(32, 2272, 0x1B78),
        ["Dispel Field"] = new(33, 2273, 0x1B79),
        ["Incognito"] = new(34, 2274, 0x1B7A),
        ["Magic Reflection"] = new(35, 2275, 0x1B7B),
        ["Mind Blast"] = new(36, 2276, 0x1B7C),
        ["Paralyze"] = new(37, 2277, 0x1B7D),
        ["Poison Field"] = new(38, 2278, 0x1B7E),
        ["Summon Creature"] = new(39, 2279, 0x1B7F),
        
        // Sixth Circle (40-47)
        ["Dispel"] = new(40, 2280, 0x1B80),
        ["Energy Bolt"] = new(41, 2281, 0x1B81),
        ["Explosion"] = new(42, 2282, 0x1B82),
        ["Invisibility"] = new(43, 2283, 0x1B83),
        ["Mark"] = new(44, 2284, 0x1B84),
        ["Mass Curse"] = new(45, 2285, 0x1B85),
        ["Paralyze Field"] = new(46, 2286, 0x1B86),
        ["Reveal"] = new(47, 2287, 0x1B87),
        
        // Seventh Circle (48-55)
        ["Chain Lightning"] = new(48, 2288, 0x1B88),
        ["Energy Field"] = new(49, 2289, 0x1B89),
        ["Flamestrike"] = new(50, 2290, 0x1B8A),
        ["Gate Travel"] = new(51, 2291, 0x1B8B),
        ["Mana Vampire"] = new(52, 2292, 0x1B8C),
        ["Mass Dispel"] = new(53, 2293, 0x1B8D),
        ["Meteor Swarm"] = new(54, 2294, 0x1B8E),
        ["Polymorph"] = new(55, 2295, 0x1B8F),
        
        // Eighth Circle (56-63)
        ["Earthquake"] = new(56, 2296, 0x1B90),
        ["Energy Vortex"] = new(57, 2297, 0x1B91),
        ["Resurrection"] = new(58, 2298, 0x1B92),
        ["Air Elemental"] = new(59, 2299, 0x1B93),
        ["Summon Daemon"] = new(60, 2300, 0x1B94),
        ["Earth Elemental"] = new(61, 2301, 0x1B95),
        ["Fire Elemental"] = new(62, 2302, 0x1B96),
        ["Water Elemental"] = new(63, 2303, 0x1B97),
    };
    
    /// <summary>
    /// Get spell icon info by spell ID
    /// </summary>
    public static SpellIconInfo? GetBySpellId(int spellId)
    {
        foreach (var spell in Spells.Values)
        {
            if (spell.SpellId == spellId)
                return spell;
        }
        return null;
    }
    
    /// <summary>
    /// Get spell icon info by name
    /// </summary>
    public static SpellIconInfo? GetByName(string name)
    {
        if (Spells.TryGetValue(name, out var info))
            return info;
        return null;
    }
    
    /// <summary>
    /// Get all spells in a circle (0-7)
    /// </summary>
    public static IEnumerable<SpellIconInfo> GetCircle(int circle)
    {
        int startId = circle * 8;
        int endId = startId + 8;
        
        foreach (var spell in Spells.Values)
        {
            if (spell.SpellId >= startId && spell.SpellId < endId)
                yield return spell;
        }
    }
}

/// <summary>
/// Information about a spell's icon
/// </summary>
public class SpellIconInfo
{
    public int SpellId { get; }
    public int SmallGumpId { get; }
    public int ScrollItemId { get; }
    public int Circle => SpellId / 8;
    public int IndexInCircle => SpellId % 8;
    
    public SpellIconInfo(int spellId, int smallGumpId, int scrollItemId)
    {
        SpellId = spellId;
        SmallGumpId = smallGumpId;
        ScrollItemId = scrollItemId;
    }
}

/// <summary>
/// Additional UI gump IDs commonly used
/// </summary>
public static class UOGumpIds
{
    // Status bar elements
    public const int StatusBarBackground = 2050;
    public const int HealthBarFull = 2053;
    public const int ManaBarFull = 2054;
    public const int StamBarFull = 2055;
    
    // Paperdoll elements
    public const int PaperdollMale = 2000;
    public const int PaperdollFemale = 2001;
    
    // Buttons
    public const int ButtonNormal = 2440;
    public const int ButtonPressed = 2441;
    public const int CheckboxUnchecked = 210;
    public const int CheckboxChecked = 211;
    
    // Scroll/container elements
    public const int ScrollTop = 2100;
    public const int ScrollMiddle = 2101;
    public const int ScrollBottom = 2102;
    
    // Backpack/container
    public const int BackpackGump = 2422;
    public const int ContainerGeneric = 2473;
    
    // Map/world elements
    public const int MapBackground = 2320;
    
    // Combat mode icons
    public const int CombatOff = 2107;
    public const int CombatOn = 2108;
    
    // Skill icons  
    public const int SkillIconBase = 2250;
    
    // Targeting cursors (these are from art.mul actually)
    public const int TargetCursorNeutral = 8310;
    public const int TargetCursorHarmful = 8311;
    public const int TargetCursorBeneficial = 8312;
    
    // Common UI elements
    public const int MinimizeButton = 2002;
    public const int CloseButton = 2003;
    
    // Hotbar slot background
    public const int HotbarSlot = 2494;
    public const int HotbarSlotActive = 2495;
}
