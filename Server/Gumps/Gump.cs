using RealmOfReality.Shared.Gumps;

namespace RealmOfReality.Server.Gumps;

/// <summary>
/// Base class for server-side gumps with builder-pattern API.
/// Extend this class to create custom gumps for NPCs, items, spells, etc.
/// </summary>
public abstract class Gump
{
    private static uint _nextSerial = 1;
    
    /// <summary>
    /// Unique type ID for this gump type. Override in derived classes.
    /// </summary>
    public abstract uint TypeId { get; }
    
    /// <summary>
    /// Instance serial for tracking responses
    /// </summary>
    public uint Serial { get; }
    
    /// <summary>
    /// Screen X position
    /// </summary>
    public int X { get; set; }
    
    /// <summary>
    /// Screen Y position
    /// </summary>
    public int Y { get; set; }
    
    /// <summary>
    /// Whether the gump can be closed
    /// </summary>
    public bool Closable { get; set; } = true;
    
    /// <summary>
    /// Whether the gump can be dragged
    /// </summary>
    public bool Dragable { get; set; } = true;
    
    /// <summary>
    /// Whether the gump is modal (blocks other gumps)
    /// </summary>
    public bool Modal { get; set; } = false;
    
    /// <summary>
    /// Current page being built
    /// </summary>
    private int _currentPage = 0;
    
    /// <summary>
    /// All elements
    /// </summary>
    private readonly List<GumpElement> _elements = new();
    
    /// <summary>
    /// Protected access to elements for derived classes
    /// </summary>
    protected List<GumpElement> Elements => _elements;
    
    /// <summary>
    /// Text pool
    /// </summary>
    private readonly List<string> _texts = new();
    
    /// <summary>
    /// Text lookup for deduplication
    /// </summary>
    private readonly Dictionary<string, int> _textLookup = new();
    
    protected Gump(int x = 0, int y = 0)
    {
        Serial = _nextSerial++;
        X = x;
        Y = y;
    }
    
    #region Builder Methods
    
    /// <summary>
    /// Start building a new page. Page 0 elements are visible on all pages.
    /// </summary>
    public Gump AddPage(int page)
    {
        _currentPage = page;
        return this;
    }
    
    /// <summary>
    /// Add a resizable background panel
    /// </summary>
    public Gump AddBackground(int x, int y, int width, int height, int gumpId)
    {
        _elements.Add(new GumpBackground
        {
            Page = (byte)_currentPage,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            GumpId = gumpId
        });
        return this;
    }
    
    /// <summary>
    /// Add a static image
    /// </summary>
    public Gump AddImage(int x, int y, int gumpId, int hue = 0)
    {
        _elements.Add(new GumpImage
        {
            Page = (byte)_currentPage,
            X = x,
            Y = y,
            GumpId = gumpId,
            Hue = hue
        });
        return this;
    }
    
    /// <summary>
    /// Add a tiled image fill
    /// </summary>
    public Gump AddImageTiled(int x, int y, int width, int height, int gumpId)
    {
        _elements.Add(new GumpImageTiled
        {
            Page = (byte)_currentPage,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            GumpId = gumpId
        });
        return this;
    }
    
    /// <summary>
    /// Add a text label
    /// </summary>
    public Gump AddLabel(int x, int y, int hue, string text)
    {
        _elements.Add(new GumpLabel
        {
            Page = (byte)_currentPage,
            X = x,
            Y = y,
            Hue = hue,
            TextIndex = AddText(text)
        });
        return this;
    }
    
    /// <summary>
    /// Add a cropped text label
    /// </summary>
    public Gump AddLabelCropped(int x, int y, int width, int height, int hue, string text)
    {
        _elements.Add(new GumpLabelCropped
        {
            Page = (byte)_currentPage,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            Hue = hue,
            TextIndex = AddText(text)
        });
        return this;
    }
    
    /// <summary>
    /// Add HTML/rich text
    /// </summary>
    public Gump AddHtml(int x, int y, int width, int height, string html, 
        bool hasBackground = false, bool hasScrollbar = false)
    {
        _elements.Add(new GumpHtml
        {
            Page = (byte)_currentPage,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            TextIndex = AddText(html),
            HasBackground = hasBackground,
            HasScrollbar = hasScrollbar
        });
        return this;
    }
    
    /// <summary>
    /// Add a button
    /// </summary>
    public Gump AddButton(int x, int y, int normalId, int pressedId, int buttonId,
        GumpButtonType type = GumpButtonType.Reply, int param = 0)
    {
        _elements.Add(new GumpButton
        {
            Page = (byte)_currentPage,
            X = x,
            Y = y,
            NormalId = normalId,
            PressedId = pressedId,
            ButtonType = type,
            Param = param,
            ButtonId = buttonId
        });
        return this;
    }
    
    /// <summary>
    /// Add a text entry field
    /// </summary>
    public Gump AddTextEntry(int x, int y, int width, int height, int hue,
        int entryId, string initialText = "", int maxLength = 0)
    {
        _elements.Add(new GumpTextEntry
        {
            Page = (byte)_currentPage,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            Hue = hue,
            EntryId = entryId,
            InitialTextIndex = AddText(initialText),
            MaxLength = maxLength > 0 ? maxLength : 256
        });
        return this;
    }
    
    /// <summary>
    /// Add a checkbox
    /// </summary>
    public Gump AddCheckbox(int x, int y, int uncheckedId, int checkedId, 
        int switchId, bool initialState = false)
    {
        _elements.Add(new GumpCheckbox
        {
            Page = (byte)_currentPage,
            X = x,
            Y = y,
            UncheckedId = uncheckedId,
            CheckedId = checkedId,
            SwitchId = switchId,
            InitialState = initialState
        });
        return this;
    }
    
    /// <summary>
    /// Add a radio button
    /// </summary>
    public Gump AddRadio(int x, int y, int uncheckedId, int checkedId,
        int switchId, int groupId = 0, bool initialState = false)
    {
        _elements.Add(new GumpRadio
        {
            Page = (byte)_currentPage,
            X = x,
            Y = y,
            UncheckedId = uncheckedId,
            CheckedId = checkedId,
            SwitchId = switchId,
            GroupId = groupId,
            InitialState = initialState
        });
        return this;
    }
    
    /// <summary>
    /// Add a game item display
    /// </summary>
    public Gump AddItem(int x, int y, int itemId, int hue = 0)
    {
        _elements.Add(new GumpItem
        {
            Page = (byte)_currentPage,
            X = x,
            Y = y,
            ItemId = itemId,
            Hue = hue
        });
        return this;
    }
    
    /// <summary>
    /// Add a tooltip to the previous element
    /// </summary>
    public Gump AddTooltip(string text)
    {
        _elements.Add(new GumpTooltip
        {
            Page = (byte)_currentPage,
            TextIndex = AddText(text)
        });
        return this;
    }
    
    /// <summary>
    /// Add a transparent region
    /// </summary>
    public Gump AddAlphaRegion(int x, int y, int width, int height)
    {
        _elements.Add(new GumpAlphaRegion
        {
            Page = (byte)_currentPage,
            X = x,
            Y = y,
            Width = width,
            Height = height
        });
        return this;
    }
    
    /// <summary>
    /// Start a new radio button group
    /// </summary>
    public Gump AddGroup(int groupId)
    {
        _elements.Add(new GumpGroup
        {
            Page = (byte)_currentPage,
            GroupId = groupId
        });
        return this;
    }
    
    #endregion
    
    #region Helpers
    
    /// <summary>
    /// Add text to pool, deduplicating
    /// </summary>
    private int AddText(string text)
    {
        text ??= "";
        
        if (_textLookup.TryGetValue(text, out var index))
            return index;
        
        index = _texts.Count;
        _texts.Add(text);
        _textLookup[text] = index;
        return index;
    }
    
    /// <summary>
    /// Build the gump data for transmission
    /// </summary>
    public GumpData BuildData()
    {
        var flags = GumpFlags.None;
        if (Closable) flags |= GumpFlags.Closable;
        if (Dragable) flags |= GumpFlags.Dragable;
        if (Modal) flags |= GumpFlags.Modal;
        
        var data = new GumpData
        {
            GumpTypeId = TypeId,
            Serial = Serial,
            X = X,
            Y = Y,
            Flags = flags
        };
        
        data.Elements.AddRange(_elements);
        data.Texts.AddRange(_texts);
        
        return data;
    }
    
    #endregion
    
    #region Response Handling
    
    /// <summary>
    /// Called when the player responds to this gump.
    /// Override to handle button clicks, text entries, etc.
    /// </summary>
    /// <param name="player">The player who responded</param>
    /// <param name="response">The response data</param>
    public virtual void OnResponse(object player, GumpResponse response)
    {
        // Default: do nothing
        // Override in derived classes to handle responses
    }
    
    /// <summary>
    /// Called when the gump is closed without explicit button click
    /// </summary>
    public virtual void OnClose(object player)
    {
        // Default: do nothing
    }
    
    #endregion
}

/// <summary>
/// Common UO gump graphics IDs for reference
/// </summary>
public static class GumpGraphics
{
    // Resizable backgrounds (9-slice)
    public const int BackgroundParchment = 9200;
    public const int BackgroundDark = 9270;
    public const int BackgroundScroll = 5054;
    public const int BackgroundBrown = 3600;
    public const int BackgroundGrey = 2620;
    public const int BackgroundBlue = 9260;
    
    // Buttons
    public const int ButtonOkNormal = 4005;
    public const int ButtonOkPressed = 4007;
    public const int ButtonCancelNormal = 4017;
    public const int ButtonCancelPressed = 4019;
    public const int ButtonArrowRightNormal = 4005;
    public const int ButtonArrowRightPressed = 4007;
    public const int ButtonArrowLeftNormal = 4014;
    public const int ButtonArrowLeftPressed = 4016;
    
    // Checkboxes
    public const int CheckboxUnchecked = 210;
    public const int CheckboxChecked = 211;
    
    // Radio buttons
    public const int RadioUnchecked = 208;
    public const int RadioChecked = 209;
    
    // Close button
    public const int CloseButtonNormal = 5052;
    public const int CloseButtonPressed = 5053;
    
    // Scrollbar
    public const int ScrollbarBackground = 256;
    public const int ScrollbarUp = 250;
    public const int ScrollbarDown = 252;
    public const int ScrollbarThumb = 254;
}
