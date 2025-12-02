using RealmOfReality.Shared.Gumps;

namespace RealmOfReality.Server.Gumps;

/// <summary>
/// Simple confirmation dialog gump
/// </summary>
public class ConfirmGump : Gump
{
    public const uint GumpId = 1000;
    public override uint TypeId => GumpId;
    
    private readonly Action<bool>? _callback;
    private readonly string _title;
    private readonly string _message;
    
    public ConfirmGump(string title, string message, Action<bool>? callback = null)
        : base(200, 200)
    {
        _title = title;
        _message = message;
        _callback = callback;
        
        Build();
    }
    
    private void Build()
    {
        AddPage(0);
        
        // Background
        AddBackground(0, 0, 300, 150, GumpGraphics.BackgroundParchment);
        
        // Title
        AddLabel(20, 15, 0, _title);
        
        // Horizontal line
        AddImageTiled(20, 40, 260, 2, 2620);
        
        // Message
        AddHtml(20, 50, 260, 60, _message, false, false);
        
        // Buttons
        AddButton(60, 115, GumpGraphics.ButtonOkNormal, GumpGraphics.ButtonOkPressed, 1);
        AddLabel(95, 115, 0, "Yes");
        
        AddButton(160, 115, GumpGraphics.ButtonCancelNormal, GumpGraphics.ButtonCancelPressed, 2);
        AddLabel(195, 115, 0, "No");
    }
    
    public override void OnResponse(object player, GumpResponse response)
    {
        _callback?.Invoke(response.ButtonId == 1);
    }
}

/// <summary>
/// Text input dialog gump
/// </summary>
public class TextInputGump : Gump
{
    public const uint GumpId = 1001;
    public override uint TypeId => GumpId;
    
    private readonly Action<string?>? _callback;
    private readonly string _title;
    private readonly string _prompt;
    
    public TextInputGump(string title, string prompt, string defaultText = "", Action<string?>? callback = null)
        : base(200, 200)
    {
        _title = title;
        _prompt = prompt;
        _callback = callback;
        
        Build(defaultText);
    }
    
    private void Build(string defaultText)
    {
        AddPage(0);
        
        // Background
        AddBackground(0, 0, 350, 150, GumpGraphics.BackgroundParchment);
        
        // Title
        AddLabel(20, 15, 0, _title);
        
        // Prompt
        AddLabel(20, 50, 0, _prompt);
        
        // Text entry
        AddBackground(20, 75, 310, 25, GumpGraphics.BackgroundDark);
        AddTextEntry(25, 78, 300, 20, 0, 0, defaultText, 100);
        
        // Buttons
        AddButton(100, 115, GumpGraphics.ButtonOkNormal, GumpGraphics.ButtonOkPressed, 1);
        AddLabel(135, 115, 0, "OK");
        
        AddButton(200, 115, GumpGraphics.ButtonCancelNormal, GumpGraphics.ButtonCancelPressed, 0);
        AddLabel(235, 115, 0, "Cancel");
    }
    
    public override void OnResponse(object player, GumpResponse response)
    {
        if (response.ButtonId == 1)
        {
            var text = response.GetTextEntry(0);
            _callback?.Invoke(text);
        }
        else
        {
            _callback?.Invoke(null);
        }
    }
}

/// <summary>
/// Multi-page menu gump example
/// </summary>
public class MenuGump : Gump
{
    public const uint GumpId = 1002;
    public override uint TypeId => GumpId;
    
    private readonly string _title;
    private readonly List<(string text, int id)> _options;
    private readonly Action<int>? _callback;
    
    private const int ItemsPerPage = 8;
    
    public MenuGump(string title, List<(string text, int id)> options, Action<int>? callback = null)
        : base(200, 100)
    {
        _title = title;
        _options = options;
        _callback = callback;
        
        Build();
    }
    
    private void Build()
    {
        int pageCount = (_options.Count + ItemsPerPage - 1) / ItemsPerPage;
        
        AddPage(0);
        
        // Background (visible on all pages)
        AddBackground(0, 0, 300, 320, GumpGraphics.BackgroundParchment);
        
        // Title
        AddLabel(20, 15, 0, _title);
        AddImageTiled(20, 40, 260, 2, 2620);
        
        // Close button
        AddButton(265, 10, GumpGraphics.CloseButtonNormal, GumpGraphics.CloseButtonPressed, 0);
        
        // Build each page
        for (int page = 1; page <= pageCount; page++)
        {
            AddPage(page);
            
            int startIndex = (page - 1) * ItemsPerPage;
            int endIndex = Math.Min(startIndex + ItemsPerPage, _options.Count);
            
            int y = 55;
            for (int i = startIndex; i < endIndex; i++)
            {
                var (text, id) = _options[i];
                AddButton(20, y, GumpGraphics.ButtonArrowRightNormal, GumpGraphics.ButtonArrowRightPressed, id);
                AddLabel(55, y, 0, text);
                y += 30;
            }
            
            // Page navigation
            if (page > 1)
            {
                AddButton(20, 285, GumpGraphics.ButtonArrowLeftNormal, GumpGraphics.ButtonArrowLeftPressed, 0, GumpButtonType.Page, page - 1);
                AddLabel(55, 285, 0, "Previous");
            }
            
            if (page < pageCount)
            {
                AddButton(200, 285, GumpGraphics.ButtonArrowRightNormal, GumpGraphics.ButtonArrowRightPressed, 0, GumpButtonType.Page, page + 1);
                AddLabel(235, 285, 0, "Next");
            }
            
            // Page indicator
            AddLabel(130, 285, 0, $"Page {page}/{pageCount}");
        }
    }
    
    public override void OnResponse(object player, GumpResponse response)
    {
        if (response.ButtonId > 0)
        {
            _callback?.Invoke(response.ButtonId);
        }
    }
}

/// <summary>
/// NPC dialog gump for conversations
/// </summary>
public class DialogGump : Gump
{
    public const uint GumpId = 1003;
    public override uint TypeId => GumpId;
    
    private readonly string _npcName;
    private readonly string _dialogText;
    private readonly List<(string text, int id)> _responses;
    private readonly Action<int>? _callback;
    
    public DialogGump(string npcName, string dialogText, List<(string text, int id)> responses, Action<int>? callback = null)
        : base(100, 100)
    {
        _npcName = npcName;
        _dialogText = dialogText;
        _responses = responses;
        _callback = callback;
        
        Build();
    }
    
    private void Build()
    {
        int height = 150 + _responses.Count * 25;
        
        AddPage(0);
        
        // Background
        AddBackground(0, 0, 400, height, GumpGraphics.BackgroundParchment);
        
        // NPC portrait area (placeholder)
        AddBackground(15, 15, 70, 70, GumpGraphics.BackgroundDark);
        AddImage(25, 25, 50430); // Generic NPC portrait
        
        // NPC name
        AddLabel(100, 20, 0x35, _npcName);
        AddImageTiled(100, 45, 280, 2, 2620);
        
        // Dialog text
        AddHtml(100, 55, 285, 70, _dialogText, false, true);
        
        // Response options
        int y = 130;
        foreach (var (text, id) in _responses)
        {
            AddButton(20, y, GumpGraphics.ButtonArrowRightNormal, GumpGraphics.ButtonArrowRightPressed, id);
            AddLabel(55, y, 0, text);
            y += 25;
        }
    }
    
    public override void OnResponse(object player, GumpResponse response)
    {
        _callback?.Invoke(response.ButtonId);
    }
}

/// <summary>
/// Vendor buy/sell gump
/// </summary>
public class VendorGump : Gump
{
    public const uint GumpId = 1004;
    public override uint TypeId => GumpId;
    
    public class VendorItem
    {
        public int ItemId { get; set; }
        public string Name { get; set; } = "";
        public int Price { get; set; }
        public int Stock { get; set; }
        public int Hue { get; set; }
    }
    
    private readonly string _vendorName;
    private readonly List<VendorItem> _items;
    private readonly Action<int, int>? _buyCallback; // itemIndex, quantity
    
    public VendorGump(string vendorName, List<VendorItem> items, Action<int, int>? buyCallback = null)
        : base(50, 50)
    {
        _vendorName = vendorName;
        _items = items;
        _buyCallback = buyCallback;
        
        Build();
    }
    
    private void Build()
    {
        int height = 150 + _items.Count * 50;
        
        AddPage(0);
        
        // Background
        AddBackground(0, 0, 450, Math.Min(height, 500), GumpGraphics.BackgroundParchment);
        
        // Title
        AddLabel(20, 15, 0x35, $"{_vendorName}'s Shop");
        AddImageTiled(20, 40, 410, 2, 2620);
        
        // Close button
        AddButton(415, 10, GumpGraphics.CloseButtonNormal, GumpGraphics.CloseButtonPressed, 0);
        
        // Header row
        AddLabel(70, 50, 0, "Item");
        AddLabel(250, 50, 0, "Price");
        AddLabel(320, 50, 0, "Stock");
        AddLabel(380, 50, 0, "Buy");
        
        AddImageTiled(20, 72, 410, 2, 2620);
        
        // Item list
        int y = 80;
        for (int i = 0; i < _items.Count && y < 450; i++)
        {
            var item = _items[i];
            
            // Item graphic
            AddItem(25, y, item.ItemId, item.Hue);
            
            // Item name
            AddLabelCropped(70, y + 10, 170, 20, 0, item.Name);
            
            // Price
            AddLabel(250, y + 10, 0x44, $"{item.Price}gp");
            
            // Stock
            AddLabel(320, y + 10, item.Stock > 0 ? 0x40 : 0x20, 
                item.Stock > 0 ? item.Stock.ToString() : "Out");
            
            // Buy button
            if (item.Stock > 0)
            {
                AddButton(380, y + 5, GumpGraphics.ButtonOkNormal, GumpGraphics.ButtonOkPressed, 100 + i);
            }
            
            y += 50;
        }
    }
    
    public override void OnResponse(object player, GumpResponse response)
    {
        if (response.ButtonId >= 100)
        {
            int itemIndex = response.ButtonId - 100;
            if (itemIndex >= 0 && itemIndex < _items.Count)
            {
                _buyCallback?.Invoke(itemIndex, 1); // Default to buying 1
            }
        }
    }
}

/// <summary>
/// Settings/options gump with checkboxes and radio buttons
/// </summary>
public class SettingsGump : Gump
{
    public const uint GumpId = 1005;
    public override uint TypeId => GumpId;
    
    public class Settings
    {
        public bool SoundEnabled { get; set; } = true;
        public bool MusicEnabled { get; set; } = true;
        public bool ShowNames { get; set; } = true;
        public bool ShowHealthBars { get; set; } = true;
        public int Difficulty { get; set; } = 1; // 0=Easy, 1=Normal, 2=Hard
    }
    
    private readonly Settings _settings;
    private readonly Action<Settings>? _callback;
    
    // Switch IDs
    private const int SwitchSound = 1;
    private const int SwitchMusic = 2;
    private const int SwitchNames = 3;
    private const int SwitchHealthBars = 4;
    private const int RadioEasy = 10;
    private const int RadioNormal = 11;
    private const int RadioHard = 12;
    
    public SettingsGump(Settings? currentSettings = null, Action<Settings>? callback = null)
        : base(200, 100)
    {
        _settings = currentSettings ?? new Settings();
        _callback = callback;
        
        Build();
    }
    
    private void Build()
    {
        AddPage(0);
        
        // Background
        AddBackground(0, 0, 300, 300, GumpGraphics.BackgroundParchment);
        
        // Title
        AddLabel(110, 15, 0x35, "Settings");
        AddImageTiled(20, 40, 260, 2, 2620);
        
        // Sound settings section
        AddLabel(20, 55, 0, "Audio");
        
        AddCheckbox(30, 80, GumpGraphics.CheckboxUnchecked, GumpGraphics.CheckboxChecked, 
            SwitchSound, _settings.SoundEnabled);
        AddLabel(55, 80, 0, "Sound Effects");
        
        AddCheckbox(30, 105, GumpGraphics.CheckboxUnchecked, GumpGraphics.CheckboxChecked,
            SwitchMusic, _settings.MusicEnabled);
        AddLabel(55, 105, 0, "Background Music");
        
        // Display settings section
        AddLabel(20, 140, 0, "Display");
        
        AddCheckbox(30, 165, GumpGraphics.CheckboxUnchecked, GumpGraphics.CheckboxChecked,
            SwitchNames, _settings.ShowNames);
        AddLabel(55, 165, 0, "Show Player Names");
        
        AddCheckbox(30, 190, GumpGraphics.CheckboxUnchecked, GumpGraphics.CheckboxChecked,
            SwitchHealthBars, _settings.ShowHealthBars);
        AddLabel(55, 190, 0, "Show Health Bars");
        
        // Difficulty section with radio buttons
        AddLabel(20, 225, 0, "Difficulty");
        AddGroup(1);
        
        AddRadio(30, 245, GumpGraphics.RadioUnchecked, GumpGraphics.RadioChecked,
            RadioEasy, 1, _settings.Difficulty == 0);
        AddLabel(55, 245, 0, "Easy");
        
        AddRadio(100, 245, GumpGraphics.RadioUnchecked, GumpGraphics.RadioChecked,
            RadioNormal, 1, _settings.Difficulty == 1);
        AddLabel(125, 245, 0, "Normal");
        
        AddRadio(180, 245, GumpGraphics.RadioUnchecked, GumpGraphics.RadioChecked,
            RadioHard, 1, _settings.Difficulty == 2);
        AddLabel(205, 245, 0, "Hard");
        
        // Buttons
        AddButton(80, 270, GumpGraphics.ButtonOkNormal, GumpGraphics.ButtonOkPressed, 1);
        AddLabel(115, 270, 0, "Save");
        
        AddButton(160, 270, GumpGraphics.ButtonCancelNormal, GumpGraphics.ButtonCancelPressed, 0);
        AddLabel(195, 270, 0, "Cancel");
    }
    
    public override void OnResponse(object player, GumpResponse response)
    {
        if (response.ButtonId == 1)
        {
            // Save settings
            var settings = new Settings
            {
                SoundEnabled = response.IsSwitchOn(SwitchSound),
                MusicEnabled = response.IsSwitchOn(SwitchMusic),
                ShowNames = response.IsSwitchOn(SwitchNames),
                ShowHealthBars = response.IsSwitchOn(SwitchHealthBars),
                Difficulty = response.IsSwitchOn(RadioEasy) ? 0 :
                            response.IsSwitchOn(RadioNormal) ? 1 :
                            response.IsSwitchOn(RadioHard) ? 2 : 1
            };
            
            _callback?.Invoke(settings);
        }
    }
}

/// <summary>
/// Character stats display gump
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
