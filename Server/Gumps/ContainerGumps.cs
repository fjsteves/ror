using RealmOfReality.Shared.Gumps;
using RealmOfReality.Shared.Entities;
using RealmOfReality.Shared.Items;

namespace RealmOfReality.Server.Gumps;

/// <summary>
/// UO-authentic backpack/container gump
/// Displays items in a container with drag-and-drop support
/// </summary>
public class BackpackGump : Gump
{
    public const uint GumpId = 0x003c; // Backpack container type ID (60)
    public override uint TypeId => GumpId;
    
    // Container gump graphics (from UO assets)
    public static class ContainerGumps
    {
        public const int Backpack = 0x003c;         // 60 - Standard backpack
        public const int Bag = 0x003d;              // 61 - Small bag
        public const int Basket = 0x003e;           // 62 - Basket
        public const int WoodenBox = 0x0042;        // 66 - Wooden box
        public const int MetalChest = 0x0048;       // 72 - Metal chest
        public const int WoodenChest = 0x003f;      // 63 - Wooden chest  
        public const int Crate = 0x0044;            // 68 - Crate
        public const int Barrel = 0x003a;           // 58 - Barrel
        public const int Pouch = 0x003b;            // 59 - Pouch
        public const int BankBox = 0x004a;          // 74 - Bank box
        public const int Corpse = 0x0009;           // 9 - Corpse container
        public const int GiftBox = 0x102a;          // 4138 - Gift box
        public const int Armoire = 0x0051;          // 81 - Armoire
        public const int Cabinet = 0x0052;          // 82 - Cabinet
        public const int Drawer = 0x0053;           // 83 - Drawer
        public const int OrnateChest = 0x0049;      // 73 - Ornate chest
        public const int SecureTradeWindow = 0x0866; // 2150 - Secure trade
        public const int Spellbook = 0x08ac;        // 2220 - Spellbook
    }
    
    // Container dimensions and item area bounds
    public static class ContainerSizes
    {
        // (Width, Height, ItemAreaX, ItemAreaY, ItemAreaWidth, ItemAreaHeight)
        public static readonly ContainerBounds Backpack = new(176, 156, 44, 65, 118, 75);
        public static readonly ContainerBounds Bag = new(128, 128, 29, 34, 93, 77);
        public static readonly ContainerBounds Pouch = new(138, 146, 18, 51, 105, 82);
        public static readonly ContainerBounds WoodenBox = new(136, 120, 16, 51, 105, 56);
        public static readonly ContainerBounds MetalChest = new(176, 195, 18, 105, 144, 75);
        public static readonly ContainerBounds WoodenChest = new(176, 176, 18, 105, 144, 58);
        public static readonly ContainerBounds BankBox = new(270, 212, 35, 47, 200, 152);
        public static readonly ContainerBounds Corpse = new(232, 227, 27, 56, 180, 150);
        public static readonly ContainerBounds Crate = new(150, 150, 20, 70, 110, 65);
    }
    
    public struct ContainerBounds
    {
        public int Width;
        public int Height;
        public int ItemAreaX;
        public int ItemAreaY;
        public int ItemAreaWidth;
        public int ItemAreaHeight;
        
        public ContainerBounds(int w, int h, int ix, int iy, int iw, int ih)
        {
            Width = w; Height = h;
            ItemAreaX = ix; ItemAreaY = iy;
            ItemAreaWidth = iw; ItemAreaHeight = ih;
        }
    }
    
    // Button IDs for item interaction (1000 + inventory slot)
    public const int ItemButtonBase = 1000;
    
    private readonly ulong _containerSerial;
    private readonly int _gumpGraphic;
    private readonly ContainerBounds _bounds;
    private readonly List<ContainerItem> _items;
    
    /// <summary>
    /// Create a backpack gump for a container with items
    /// </summary>
    public BackpackGump(ulong containerSerial, int gumpGraphic = ContainerGumps.Backpack, List<ContainerItem>? items = null)
        : base(100, 100)
    {
        _containerSerial = containerSerial;
        _gumpGraphic = gumpGraphic;
        _items = items ?? new List<ContainerItem>();
        _bounds = GetContainerBounds(gumpGraphic);
        
        Build();
    }
    
    /// <summary>
    /// Create a backpack gump from player inventory
    /// </summary>
    public static BackpackGump FromPlayerInventory(PlayerEntity player)
    {
        var items = new List<ContainerItem>();
        var bounds = ContainerSizes.Backpack;
        
        // Grid layout for items in backpack
        int cols = 4;
        int itemSpacingX = 44;
        int itemSpacingY = 44;
        int startX = bounds.ItemAreaX;
        int startY = bounds.ItemAreaY;
        
        int index = 0;
        foreach (var (slot, item) in player.Inventory.GetAllItems())
        {
            int col = index % cols;
            int row = index / cols;
            
            items.Add(new ContainerItem
            {
                Serial = item.Id.Value,
                ItemGraphic = item.Definition?.SpriteId ?? 0,
                X = startX + col * itemSpacingX,
                Y = startY + row * itemSpacingY,
                Amount = item.Amount,
                Hue = item.Definition?.Hue ?? 0,
                Name = item.Name,
                InventorySlot = slot
            });
            
            index++;
        }
        
        return new BackpackGump(player.Id.Value, ContainerGumps.Backpack, items);
    }
    
    private static ContainerBounds GetContainerBounds(int gumpId) => gumpId switch
    {
        ContainerGumps.Backpack => ContainerSizes.Backpack,
        ContainerGumps.Bag => ContainerSizes.Bag,
        ContainerGumps.Pouch => ContainerSizes.Pouch,
        ContainerGumps.WoodenBox => ContainerSizes.WoodenBox,
        ContainerGumps.MetalChest => ContainerSizes.MetalChest,
        ContainerGumps.WoodenChest => ContainerSizes.WoodenChest,
        ContainerGumps.BankBox => ContainerSizes.BankBox,
        ContainerGumps.Corpse => ContainerSizes.Corpse,
        ContainerGumps.Crate => ContainerSizes.Crate,
        _ => ContainerSizes.Backpack
    };
    
    private void Build()
    {
        AddPage(0);
        
        // Container background image
        AddImage(0, 0, _gumpGraphic);
        
        // Render items with click areas for interaction
        foreach (var item in _items)
        {
            // Item graphic
            AddItem(item.X, item.Y, item.ItemGraphic, item.Hue);
            
            // Show amount for stacked items
            if (item.Amount > 1)
            {
                AddLabel(item.X, item.Y + 15, 0x0035, item.Amount.ToString());
            }
            
            // Note: In real UO, items in containers are clickable via their graphic bounds
            // Our AddItem creates a GumpItemControl which handles the click area
        }
    }
    
    public override void OnResponse(object player, GumpResponse response)
    {
        // Item interaction
        if (response.ButtonId >= ItemButtonBase)
        {
            int slot = response.ButtonId - ItemButtonBase;
            // Handle item use - this would be processed by PacketHandler
        }
    }
}

/// <summary>
/// Represents an item in a container for display purposes
/// </summary>
public class ContainerItem
{
    public ulong Serial { get; set; }
    public int ItemGraphic { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Amount { get; set; } = 1;
    public int Hue { get; set; } = 0;
    public string Name { get; set; } = "";
    public int InventorySlot { get; set; } = -1;
}

/// <summary>
/// Bank box gump
/// </summary>
public class BankBoxGump : BackpackGump
{
    public new const uint GumpId = 0x004a;
    public override uint TypeId => GumpId;
    
    public BankBoxGump(ulong containerSerial, List<ContainerItem>? items = null)
        : base(containerSerial, ContainerGumps.BankBox, items)
    {
    }
}

/// <summary>
/// Corpse container gump
/// </summary>  
public class CorpseGump : BackpackGump
{
    public new const uint GumpId = 0x0009;
    public override uint TypeId => GumpId;
    
    public CorpseGump(ulong containerSerial, List<ContainerItem>? items = null)
        : base(containerSerial, ContainerGumps.Corpse, items)
    {
    }
}

/// <summary>
/// Secure trade window gump
/// </summary>
public class SecureTradeGump : Gump
{
    public const uint GumpId = 0x0866;
    public override uint TypeId => GumpId;
    
    private const int GumpBackground = 0x0866;
    
    public const int ButtonAccept = 1;
    public const int ButtonCancel = 2;
    
    private readonly ulong _tradePartnerSerial;
    private readonly string _partnerName;
    private readonly List<ContainerItem> _myItems;
    private readonly List<ContainerItem> _theirItems;
    
    public SecureTradeGump(ulong partnerSerial, string partnerName, 
        List<ContainerItem>? myItems = null, List<ContainerItem>? theirItems = null)
        : base(150, 150)
    {
        _tradePartnerSerial = partnerSerial;
        _partnerName = partnerName;
        _myItems = myItems ?? new List<ContainerItem>();
        _theirItems = theirItems ?? new List<ContainerItem>();
        
        Build();
    }
    
    private void Build()
    {
        AddPage(0);
        AddImage(0, 0, GumpBackground);
        AddLabel(50, 4, 0, _partnerName);
        
        foreach (var item in _myItems)
            AddItem(item.X + 10, item.Y + 40, item.ItemGraphic, item.Hue);
        
        foreach (var item in _theirItems)
            AddItem(item.X + 120, item.Y + 40, item.ItemGraphic, item.Hue);
    }
    
    public override void OnResponse(object player, GumpResponse response)
    {
    }
}
