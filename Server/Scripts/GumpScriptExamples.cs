using RealmOfReality.Server.Gumps;
using RealmOfReality.Shared.Gumps;

namespace RealmOfReality.Server.Scripts;

/// <summary>
/// Example scripts demonstrating gump system usage.
/// These show how NPCs, items, and other game elements can use gumps.
/// </summary>
public static class GumpScriptExamples
{
    /// <summary>
    /// Example: NPC vendor interaction
    /// When player double-clicks a vendor, open a shop gump
    /// </summary>
    public static void OnVendorDoubleClick(object player, string vendorName)
    {
        var items = new List<VendorGump.VendorItem>
        {
            new() { ItemId = 3573, Name = "Iron Sword", Price = 100, Stock = 5, Hue = 0 },
            new() { ItemId = 3574, Name = "Steel Sword", Price = 250, Stock = 3, Hue = 0 },
            new() { ItemId = 5062, Name = "Leather Armor", Price = 150, Stock = 10, Hue = 0 },
            new() { ItemId = 5135, Name = "Health Potion", Price = 50, Stock = 20, Hue = 38 },
            new() { ItemId = 5136, Name = "Mana Potion", Price = 75, Stock = 15, Hue = 88 },
        };
        
        var gump = new VendorGump(vendorName, items, (itemIndex, quantity) =>
        {
            var item = items[itemIndex];
            Console.WriteLine($"Player wants to buy {quantity}x {item.Name} for {item.Price * quantity}gp");
            
            // In real implementation:
            // - Check player has enough gold
            // - Remove gold from player
            // - Add item to player inventory
            // - Decrease vendor stock
        });
        
        // SendGump(player, gump);
    }
    
    /// <summary>
    /// Example: Quest NPC dialog
    /// Multi-step conversation with choices
    /// </summary>
    public static void OnQuestNpcTalk(object player, string npcName)
    {
        // First dialog node
        var responses = new List<(string text, int id)>
        {
            ("Tell me about the dragon.", 1),
            ("What rewards do you offer?", 2),
            ("I'll slay the dragon!", 3),
            ("Goodbye.", 0)
        };
        
        var gump = new DialogGump(npcName,
            "Brave adventurer! A terrible dragon has been terrorizing our village. " +
            "We desperately need a hero to defeat this menace. Will you help us?",
            responses,
            buttonId =>
            {
                switch (buttonId)
                {
                    case 1:
                        ShowDragonInfo(player, npcName);
                        break;
                    case 2:
                        ShowRewards(player, npcName);
                        break;
                    case 3:
                        AcceptQuest(player, npcName);
                        break;
                    default:
                        // Goodbye or closed
                        break;
                }
            });
        
        // SendGump(player, gump);
    }
    
    private static void ShowDragonInfo(object player, string npcName)
    {
        var responses = new List<(string text, int id)>
        {
            ("I understand. I'll do it!", 1),
            ("That sounds too dangerous.", 0)
        };
        
        var gump = new DialogGump(npcName,
            "The dragon is an ancient red wyrm named Flamescorch. It lives in a cave " +
            "to the north, about a day's journey from here. It breathes fire hot enough " +
            "to melt steel, so bring fire-resistant gear!",
            responses,
            buttonId =>
            {
                if (buttonId == 1)
                {
                    AcceptQuest(player, npcName);
                }
            });
        
        // SendGump(player, gump);
    }
    
    private static void ShowRewards(object player, string npcName)
    {
        var responses = new List<(string text, int id)>
        {
            ("Sounds good! I accept.", 1),
            ("I need to think about it.", 0)
        };
        
        var gump = new DialogGump(npcName,
            "For slaying the dragon, the village will reward you with:\n\n" +
            "• 10,000 gold pieces\n" +
            "• The Dragon Slayer title\n" +
            "• A piece of the dragon's hoard\n" +
            "• Our eternal gratitude!",
            responses,
            buttonId =>
            {
                if (buttonId == 1)
                {
                    AcceptQuest(player, npcName);
                }
            });
        
        // SendGump(player, gump);
    }
    
    private static void AcceptQuest(object player, string npcName)
    {
        var responses = new List<(string text, int id)>
        {
            ("I won't let you down!", 0)
        };
        
        var gump = new DialogGump(npcName,
            "Thank you, brave hero! May the gods watch over you on your quest. " +
            "Return to me when the deed is done, and I shall reward you handsomely.\n\n" +
            "<i>Quest Added: Slay the Dragon</i>",
            responses,
            _ =>
            {
                // Add quest to player's journal
                Console.WriteLine("Quest 'Slay the Dragon' added to player");
            });
        
        // SendGump(player, gump);
    }
    
    /// <summary>
    /// Example: Crafting menu
    /// Player interacts with a crafting station
    /// </summary>
    public static void OnCraftingStationUse(object player)
    {
        var recipes = new List<(string text, int id)>
        {
            ("Iron Sword (2 Iron Ingots)", 1),
            ("Steel Sword (3 Steel Ingots)", 2),
            ("Iron Armor (5 Iron Ingots)", 3),
            ("Steel Armor (7 Steel Ingots)", 4),
            ("Health Potion (1 Ginseng, 1 Garlic)", 5),
            ("Mana Potion (1 Nightshade, 1 Mandrake)", 6),
        };
        
        var gump = new MenuGump("Crafting Menu", recipes, recipeId =>
        {
            // In real implementation:
            // - Check player has required materials
            // - Check player has required skill level
            // - Remove materials
            // - Add crafted item
            // - Award crafting experience
            
            Console.WriteLine($"Player selected recipe #{recipeId}");
            
            // Show confirmation
            var confirmGump = new ConfirmGump("Craft Item",
                $"Do you want to craft this item?",
                confirmed =>
                {
                    if (confirmed)
                    {
                        Console.WriteLine("Crafting item...");
                    }
                });
            
            // SendGump(player, confirmGump);
        });
        
        // SendGump(player, gump);
    }
    
    /// <summary>
    /// Example: Player settings/options
    /// </summary>
    public static void OnSettingsCommand(object player)
    {
        // Load current settings (would come from player data)
        var currentSettings = new SettingsGump.Settings
        {
            SoundEnabled = true,
            MusicEnabled = true,
            ShowNames = true,
            ShowHealthBars = false,
            Difficulty = 1
        };
        
        var gump = new SettingsGump(currentSettings, newSettings =>
        {
            // Save settings
            Console.WriteLine($"Saving settings: Sound={newSettings.SoundEnabled}, " +
                            $"Music={newSettings.MusicEnabled}, Names={newSettings.ShowNames}, " +
                            $"HealthBars={newSettings.ShowHealthBars}, Difficulty={newSettings.Difficulty}");
        });
        
        // SendGump(player, gump);
    }
    
    /// <summary>
    /// Example: Guild management
    /// Complex multi-page gump
    /// </summary>
    public static void OnGuildStoneUse(object player)
    {
        // This would be a more complex custom gump
        // For now, use a menu to show the pattern
        var options = new List<(string text, int id)>
        {
            ("View Members", 1),
            ("View Treasury", 2),
            ("Declare War", 3),
            ("Ally Request", 4),
            ("Guild Settings", 5),
            ("Resign", 6),
        };
        
        var gump = new MenuGump("Guild of Heroes", options, optionId =>
        {
            switch (optionId)
            {
                case 1:
                    Console.WriteLine("Showing guild members...");
                    break;
                case 2:
                    Console.WriteLine("Showing treasury...");
                    break;
                case 3:
                    var warConfirm = new ConfirmGump("Declare War",
                        "Are you sure you want to declare war? This action cannot be undone!",
                        confirmed =>
                        {
                            if (confirmed)
                            {
                                Console.WriteLine("War declared!");
                            }
                        });
                    // SendGump(player, warConfirm);
                    break;
                case 6:
                    var resignConfirm = new ConfirmGump("Resign",
                        "Are you sure you want to leave the guild?",
                        confirmed =>
                        {
                            if (confirmed)
                            {
                                Console.WriteLine("Player resigned from guild");
                            }
                        });
                    // SendGump(player, resignConfirm);
                    break;
            }
        });
        
        // SendGump(player, gump);
    }
    
    /// <summary>
    /// Example: Name change (text input)
    /// </summary>
    public static void OnNameChangeRune(object player, string currentName)
    {
        var gump = new TextInputGump("Change Name",
            "Enter your new name:",
            currentName,
            newName =>
            {
                if (string.IsNullOrWhiteSpace(newName))
                {
                    Console.WriteLine("Name change cancelled");
                    return;
                }
                
                // Validate name
                if (newName.Length < 3 || newName.Length > 20)
                {
                    Console.WriteLine("Name must be 3-20 characters");
                    return;
                }
                
                // Change name
                Console.WriteLine($"Changing name from '{currentName}' to '{newName}'");
            });
        
        // SendGump(player, gump);
    }
}

/// <summary>
/// Custom gump example: Character stats display
/// Shows how to create a completely custom gump
/// </summary>
public class CharacterStatsGump : Gump
{
    public const uint GumpId = 2000;
    public override uint TypeId => GumpId;
    
    public CharacterStatsGump(string name, int str, int dex, int intel, int hp, int maxHp, int mana, int maxMana)
        : base(100, 100)
    {
        Closable = true;
        Dragable = true;
        
        AddPage(0);
        
        // Main background
        AddBackground(0, 0, 250, 280, GumpGraphics.BackgroundParchment);
        
        // Character name header
        AddLabel(20, 15, 0x35, name);
        AddImageTiled(20, 40, 210, 2, 2620);
        
        // Stats section
        AddLabel(20, 55, 0, "Attributes");
        
        AddLabel(30, 80, 0, "Strength:");
        AddLabel(150, 80, 0x40, str.ToString());
        
        AddLabel(30, 100, 0, "Dexterity:");
        AddLabel(150, 100, 0x40, dex.ToString());
        
        AddLabel(30, 120, 0, "Intelligence:");
        AddLabel(150, 120, 0x40, intel.ToString());
        
        // Health/Mana section
        AddImageTiled(20, 150, 210, 2, 2620);
        AddLabel(20, 160, 0, "Vitals");
        
        AddLabel(30, 185, 0, "Health:");
        AddLabel(150, 185, 0x20, $"{hp}/{maxHp}");
        
        AddLabel(30, 205, 0, "Mana:");
        AddLabel(150, 205, 0x5A, $"{mana}/{maxMana}");
        
        // Close button
        AddButton(100, 240, GumpGraphics.ButtonOkNormal, GumpGraphics.ButtonOkPressed, 0);
        AddLabel(130, 240, 0, "Close");
    }
}

/// <summary>
/// Custom gump example: Bank box
/// Shows item display with interaction
/// </summary>
public class BankBoxGump : Gump
{
    public const uint GumpId = 2001;
    public override uint TypeId => GumpId;
    
    public class BankItem
    {
        public int Slot { get; set; }
        public int ItemId { get; set; }
        public int Hue { get; set; }
        public string Name { get; set; } = "";
        public int Amount { get; set; }
    }
    
    private readonly List<BankItem> _items;
    private readonly Action<int>? _onWithdraw;
    
    public BankBoxGump(string playerName, List<BankItem> items, int goldAmount, Action<int>? onWithdraw = null)
        : base(50, 50)
    {
        _items = items;
        _onWithdraw = onWithdraw;
        
        Closable = true;
        Dragable = true;
        
        const int cols = 5;
        const int rows = 4;
        const int cellSize = 50;
        
        int width = cols * cellSize + 60;
        int height = rows * cellSize + 120;
        
        AddPage(0);
        
        // Background
        AddBackground(0, 0, width, height, GumpGraphics.BackgroundParchment);
        
        // Title
        AddLabel(20, 15, 0x35, $"{playerName}'s Bank");
        AddLabel(width - 120, 15, 0x44, $"{goldAmount:N0} gp");
        AddImageTiled(20, 40, width - 40, 2, 2620);
        
        // Item grid
        int startX = 25;
        int startY = 55;
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                int x = startX + col * cellSize;
                int y = startY + row * cellSize;
                int slot = row * cols + col;
                
                // Cell background
                AddAlphaRegion(x, y, cellSize - 5, cellSize - 5);
                
                // Find item in this slot
                var item = _items.FirstOrDefault(i => i.Slot == slot);
                if (item != null)
                {
                    AddItem(x + 3, y + 3, item.ItemId, item.Hue);
                    AddTooltip(item.Name);
                    
                    // Withdraw button (clicking the item)
                    AddButton(x, y, 0, 0, 100 + slot); // Invisible button overlay
                }
            }
        }
        
        // Close button
        AddButton(width / 2 - 30, height - 35, GumpGraphics.ButtonCancelNormal, GumpGraphics.ButtonCancelPressed, 0);
        AddLabel(width / 2 - 5, height - 35, 0, "Close");
    }
    
    public override void OnResponse(object player, GumpResponse response)
    {
        if (response.ButtonId >= 100)
        {
            int slot = response.ButtonId - 100;
            var item = _items.FirstOrDefault(i => i.Slot == slot);
            if (item != null)
            {
                _onWithdraw?.Invoke(slot);
            }
        }
    }
}
