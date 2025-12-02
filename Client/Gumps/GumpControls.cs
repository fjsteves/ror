using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RealmOfReality.Shared.Gumps;
using RealmOfReality.Shared.Items;

namespace RealmOfReality.Client.Gumps;

/// <summary>
/// Resizable background using 9-slice rendering - passes drag events to parent window
/// </summary>
public class GumpBackgroundControl : GumpControl
{
    public int GumpId { get; set; }
    
    public GumpBackgroundControl()
    {
        AcceptsMouseInput = true;
    }
    
    public override void Draw(SpriteBatch spriteBatch, GumpRenderer renderer)
    {
        renderer.DrawResizableGump(spriteBatch, GumpId, ScreenX, ScreenY, Width, Height);
        DrawChildren(spriteBatch, renderer);
    }
    
    // Pass drag events to parent window
    public override void OnMouseDown(int localX, int localY, MouseButton button)
    {
        var window = FindParentWindow();
        if (window != null && button == MouseButton.Left)
        {
            int windowLocalX = ScreenX - window.ScreenX + localX;
            int windowLocalY = ScreenY - window.ScreenY + localY;
            window.OnMouseDown(windowLocalX, windowLocalY, button);
        }
    }
    
    public override void OnMouseUp(int localX, int localY, MouseButton button)
    {
        var window = FindParentWindow();
        if (window != null)
        {
            int windowLocalX = ScreenX - window.ScreenX + localX;
            int windowLocalY = ScreenY - window.ScreenY + localY;
            window.OnMouseUp(windowLocalX, windowLocalY, button);
        }
    }
    
    public override void OnMouseDrag(int deltaX, int deltaY)
    {
        var window = FindParentWindow();
        window?.OnMouseDrag(deltaX, deltaY);
    }
    
    private GumpWindow? FindParentWindow()
    {
        var current = Parent;
        while (current != null)
        {
            if (current is GumpWindow window)
                return window;
            current = current.Parent;
        }
        return null;
    }
}

/// <summary>
/// Static image display - passes drag events to parent window
/// </summary>
public class GumpImageControl : GumpControl
{
    public int GumpId { get; set; }
    public int Hue { get; set; }
    
    private bool _sizeInitialized;
    
    public GumpImageControl()
    {
        // Images accept input but pass through to window for dragging
        AcceptsMouseInput = true;
    }
    
    public override void Draw(SpriteBatch spriteBatch, GumpRenderer renderer)
    {
        var texture = renderer.GetGumpTexture(GumpId);
        if (texture != null)
        {
            // Update size from texture if not already set
            if (!_sizeInitialized && texture.Width > 0 && texture.Height > 0)
            {
                Width = texture.Width;
                Height = texture.Height;
                _sizeInitialized = true;
            }
        }
        
        renderer.DrawGump(spriteBatch, GumpId, ScreenX, ScreenY, Hue);
        DrawChildren(spriteBatch, renderer);
    }
    
    // Pass drag events to parent window
    public override void OnMouseDown(int localX, int localY, MouseButton button)
    {
        // Find parent window and pass the event
        var window = FindParentWindow();
        if (window != null && button == MouseButton.Left)
        {
            // Convert to window-relative coordinates
            int windowLocalX = ScreenX - window.ScreenX + localX;
            int windowLocalY = ScreenY - window.ScreenY + localY;
            window.OnMouseDown(windowLocalX, windowLocalY, button);
        }
    }
    
    public override void OnMouseUp(int localX, int localY, MouseButton button)
    {
        var window = FindParentWindow();
        if (window != null)
        {
            int windowLocalX = ScreenX - window.ScreenX + localX;
            int windowLocalY = ScreenY - window.ScreenY + localY;
            window.OnMouseUp(windowLocalX, windowLocalY, button);
        }
    }
    
    public override void OnMouseDrag(int deltaX, int deltaY)
    {
        var window = FindParentWindow();
        window?.OnMouseDrag(deltaX, deltaY);
    }
    
    private GumpWindow? FindParentWindow()
    {
        var current = Parent;
        while (current != null)
        {
            if (current is GumpWindow window)
                return window;
            current = current.Parent;
        }
        return null;
    }
}

/// <summary>
/// Tiled image fill
/// </summary>
public class GumpImageTiledControl : GumpControl
{
    public int GumpId { get; set; }
    
    public override void Draw(SpriteBatch spriteBatch, GumpRenderer renderer)
    {
        renderer.DrawGumpTiled(spriteBatch, GumpId, ScreenX, ScreenY, Width, Height);
        DrawChildren(spriteBatch, renderer);
    }
}

/// <summary>
/// Text label
/// </summary>
public class GumpLabelControl : GumpControl
{
    public string Text { get; set; } = "";
    public int Hue { get; set; }
    public bool Cropped { get; set; }
    
    public override void Draw(SpriteBatch spriteBatch, GumpRenderer renderer)
    {
        if (Cropped)
        {
            renderer.DrawTextCropped(spriteBatch, Text, ScreenX, ScreenY, Width, Height, Hue);
        }
        else
        {
            renderer.DrawText(spriteBatch, Text, ScreenX, ScreenY, Hue);
        }
        DrawChildren(spriteBatch, renderer);
    }
}

/// <summary>
/// HTML/rich text with optional scrolling
/// </summary>
public class GumpHtmlControl : GumpControl
{
    public string Html { get; set; } = "";
    public bool HasBackground { get; set; }
    public bool HasScrollbar { get; set; }
    
    private int _scrollOffset = 0;
    private int _contentHeight = 0;
    
    public override void Draw(SpriteBatch spriteBatch, GumpRenderer renderer)
    {
        if (HasBackground)
        {
            renderer.DrawResizableGump(spriteBatch, 2620, ScreenX, ScreenY, Width, Height);
        }
        
        // Simple HTML rendering (strip tags for now, full implementation would parse HTML)
        var plainText = StripHtmlTags(Html);
        _contentHeight = renderer.DrawTextWrapped(spriteBatch, plainText, ScreenX + 5, ScreenY + 5 - _scrollOffset, 
            Width - (HasScrollbar ? 20 : 10), Height - 10, 0);
        
        if (HasScrollbar && _contentHeight > Height)
        {
            renderer.DrawScrollbar(spriteBatch, ScreenX + Width - 15, ScreenY, 15, Height, 
                _scrollOffset, _contentHeight);
        }
        
        DrawChildren(spriteBatch, renderer);
    }
    
    public override void OnMouseScroll(int delta)
    {
        if (HasScrollbar && _contentHeight > Height)
        {
            _scrollOffset -= delta / 4;
            _scrollOffset = Math.Clamp(_scrollOffset, 0, Math.Max(0, _contentHeight - Height + 20));
        }
    }
    
    private static string StripHtmlTags(string html)
    {
        // Simple tag stripping - a full implementation would parse and render HTML
        var result = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", "");
        result = result.Replace("&nbsp;", " ");
        result = result.Replace("&lt;", "<");
        result = result.Replace("&gt;", ">");
        result = result.Replace("&amp;", "&");
        return result;
    }
}

/// <summary>
/// Clickable button
/// </summary>
public class GumpButtonControl : GumpControl
{
    public int NormalId { get; set; }
    public int PressedId { get; set; }
    public GumpButtonType ButtonType { get; set; }
    public int Param { get; set; }
    public int ButtonId { get; set; }
    
    public bool IsPressed { get; private set; }
    public bool IsHovered { get; private set; }
    
    private bool _sizeInitialized;
    
    private GumpWindow? GumpWindow => Parent as GumpWindow ?? Parent?.Parent as GumpWindow;
    
    public override void Draw(SpriteBatch spriteBatch, GumpRenderer renderer)
    {
        // Invisible button (NormalId = 0) - don't draw anything, just acts as clickable area
        if (NormalId == 0 && PressedId == 0)
        {
            // Optionally draw debug outline in debug mode
            // renderer.DrawRectangleOutline(spriteBatch, ScreenX, ScreenY, Width, Height, new Color(255, 0, 0, 64), 1);
            return;
        }
        
        int gumpId = IsPressed ? PressedId : NormalId;
        
        // Try to get texture to determine actual size
        var texture = renderer.GetGumpTexture(gumpId);
        if (texture != null)
        {
            // Update size from texture if not already set
            if (!_sizeInitialized && texture.Width > 0 && texture.Height > 0)
            {
                Width = texture.Width;
                Height = texture.Height;
                _sizeInitialized = true;
            }
            
            var color = IsHovered && !IsPressed ? new Color(255, 255, 255, 230) : Color.White;
            spriteBatch.Draw(texture, new Vector2(ScreenX, ScreenY), color);
        }
        else
        {
            // Draw fallback button with visible colors
            var bgColor = IsPressed ? new Color(80, 70, 60) : (IsHovered ? new Color(100, 90, 75) : new Color(70, 60, 50));
            var borderColor = IsPressed ? new Color(50, 45, 35) : new Color(110, 100, 80);
            int w = Width > 0 ? Width : 30;
            int h = Height > 0 ? Height : 25;
            
            renderer.DrawRectangle(spriteBatch, ScreenX, ScreenY, w, h, bgColor);
            renderer.DrawRectangleOutline(spriteBatch, ScreenX, ScreenY, w, h, borderColor, 1);
            
            // Draw a small indicator (arrow or dot)
            renderer.DrawRectangle(spriteBatch, ScreenX + w/2 - 3, ScreenY + h/2 - 3, 6, 6, new Color(200, 180, 140));
        }
        DrawChildren(spriteBatch, renderer);
    }
    
    public override void OnMouseEnter()
    {
        IsHovered = true;
    }
    
    public override void OnMouseLeave()
    {
        IsHovered = false;
        IsPressed = false;
    }
    
    public override void OnMouseDown(int localX, int localY, MouseButton button)
    {
        if (button == MouseButton.Left)
        {
            IsPressed = true;
        }
    }
    
    public override void OnMouseUp(int localX, int localY, MouseButton button)
    {
        if (button == MouseButton.Left && IsPressed)
        {
            IsPressed = false;
            GumpWindow?.OnButtonClick(ButtonType, ButtonId, Param);
        }
    }
}

/// <summary>
/// Editable text input
/// </summary>
public class GumpTextEntryControl : GumpControl
{
    public int EntryId { get; set; }
    public string Text { get; set; } = "";
    public int MaxLength { get; set; } = 256;
    public int Hue { get; set; }
    
    private int _cursorPosition = 0;
    private float _cursorBlink = 0;
    
    private GumpWindow? GumpWindow => Parent as GumpWindow ?? Parent?.Parent as GumpWindow;
    
    public GumpTextEntryControl()
    {
        AcceptsKeyboardInput = true;
    }
    
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (IsFocused)
        {
            _cursorBlink += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_cursorBlink > 1f) _cursorBlink = 0;
        }
    }
    
    public override void Draw(SpriteBatch spriteBatch, GumpRenderer renderer)
    {
        // Draw background
        renderer.DrawResizableGump(spriteBatch, 9270, ScreenX, ScreenY, Width, Height);
        
        // Draw text
        renderer.DrawTextCropped(spriteBatch, Text, ScreenX + 3, ScreenY + 3, Width - 6, Height - 6, Hue);
        
        // Draw cursor if focused
        if (IsFocused && _cursorBlink < 0.5f)
        {
            var cursorX = ScreenX + 3 + renderer.MeasureText(Text.Substring(0, _cursorPosition));
            renderer.DrawRectangle(spriteBatch, cursorX, ScreenY + 3, 2, Height - 6, Color.White);
        }
        
        DrawChildren(spriteBatch, renderer);
    }
    
    public override void OnMouseClick(int localX, int localY, MouseButton button)
    {
        if (button == MouseButton.Left)
        {
            IsFocused = true;
            _cursorPosition = Text.Length; // Simple: cursor at end
        }
    }
    
    public override void OnKeyDown(Keys key)
    {
        if (!IsFocused) return;
        
        switch (key)
        {
            case Keys.Back:
                if (_cursorPosition > 0)
                {
                    Text = Text.Remove(_cursorPosition - 1, 1);
                    _cursorPosition--;
                    UpdateGumpWindow();
                }
                break;
                
            case Keys.Delete:
                if (_cursorPosition < Text.Length)
                {
                    Text = Text.Remove(_cursorPosition, 1);
                    UpdateGumpWindow();
                }
                break;
                
            case Keys.Left:
                if (_cursorPosition > 0) _cursorPosition--;
                break;
                
            case Keys.Right:
                if (_cursorPosition < Text.Length) _cursorPosition++;
                break;
                
            case Keys.Home:
                _cursorPosition = 0;
                break;
                
            case Keys.End:
                _cursorPosition = Text.Length;
                break;
        }
    }
    
    public override void OnTextInput(char character)
    {
        if (!IsFocused) return;
        if (Text.Length >= MaxLength) return;
        if (char.IsControl(character)) return;
        
        Text = Text.Insert(_cursorPosition, character.ToString());
        _cursorPosition++;
        UpdateGumpWindow();
    }
    
    private void UpdateGumpWindow()
    {
        GumpWindow?.SetTextEntry(EntryId, Text);
    }
}

/// <summary>
/// Checkbox toggle
/// </summary>
public class GumpCheckboxControl : GumpControl
{
    public int UncheckedId { get; set; }
    public int CheckedId { get; set; }
    public int SwitchId { get; set; }
    public bool IsChecked { get; set; }
    
    private GumpWindow? GumpWindow => Parent as GumpWindow ?? Parent?.Parent as GumpWindow;
    
    public override void Draw(SpriteBatch spriteBatch, GumpRenderer renderer)
    {
        int gumpId = IsChecked ? CheckedId : UncheckedId;
        if (!renderer.DrawGumpIfExists(spriteBatch, gumpId, ScreenX, ScreenY, 0))
        {
            // Fallback checkbox
            int size = 16;
            renderer.DrawRectangle(spriteBatch, ScreenX, ScreenY, size, size, new Color(50, 45, 40));
            renderer.DrawRectangleOutline(spriteBatch, ScreenX, ScreenY, size, size, new Color(100, 90, 75), 1);
            
            if (IsChecked)
            {
                // Draw checkmark
                renderer.DrawRectangle(spriteBatch, ScreenX + 3, ScreenY + 3, size - 6, size - 6, new Color(180, 160, 100));
            }
        }
        DrawChildren(spriteBatch, renderer);
    }
    
    public override void OnMouseClick(int localX, int localY, MouseButton button)
    {
        if (button == MouseButton.Left)
        {
            IsChecked = !IsChecked;
            GumpWindow?.SetSwitch(SwitchId, IsChecked);
        }
    }
}

/// <summary>
/// Radio button (mutually exclusive in group)
/// </summary>
public class GumpRadioControl : GumpControl
{
    public int UncheckedId { get; set; }
    public int CheckedId { get; set; }
    public int SwitchId { get; set; }
    public int GroupId { get; set; }
    public bool IsSelected { get; set; }
    
    private GumpWindow? GumpWindow => Parent as GumpWindow ?? Parent?.Parent as GumpWindow;
    
    public override void Draw(SpriteBatch spriteBatch, GumpRenderer renderer)
    {
        int gumpId = IsSelected ? CheckedId : UncheckedId;
        if (!renderer.DrawGumpIfExists(spriteBatch, gumpId, ScreenX, ScreenY, 0))
        {
            // Fallback radio button (circle-ish)
            int size = 16;
            renderer.DrawRectangle(spriteBatch, ScreenX, ScreenY, size, size, new Color(50, 45, 40));
            renderer.DrawRectangleOutline(spriteBatch, ScreenX, ScreenY, size, size, new Color(100, 90, 75), 1);
            
            if (IsSelected)
            {
                // Draw filled center (radio selected)
                renderer.DrawRectangle(spriteBatch, ScreenX + 4, ScreenY + 4, size - 8, size - 8, new Color(200, 180, 100));
            }
        }
        DrawChildren(spriteBatch, renderer);
    }
    
    public override void OnMouseClick(int localX, int localY, MouseButton button)
    {
        if (button == MouseButton.Left && !IsSelected)
        {
            // Deselect other radios in same group
            if (Parent != null)
            {
                foreach (var sibling in Parent.Children)
                {
                    if (sibling is GumpRadioControl radio && radio.GroupId == GroupId && radio != this)
                    {
                        if (radio.IsSelected)
                        {
                            radio.IsSelected = false;
                            GumpWindow?.SetSwitch(radio.SwitchId, false);
                        }
                    }
                }
            }
            
            IsSelected = true;
            GumpWindow?.SetSwitch(SwitchId, true);
        }
    }
}

/// <summary>
/// Game item display (clickable/draggable)
/// </summary>
public class GumpItemControl : GumpControl
{
    public int ItemId { get; set; }
    public int Hue { get; set; }
    public ulong Serial { get; set; }
    public int InventorySlot { get; set; } = -1;
    
    private bool _sizeInitialized;
    
    /// <summary>
    /// Event when item is double-clicked (use)
    /// </summary>
    public event Action<ulong>? OnItemUse;
    
    /// <summary>
    /// Event when item is picked up (drag)
    /// </summary>
    public event Action<ulong>? OnItemPickup;
    
    public GumpItemControl()
    {
        AcceptsMouseInput = true;
        Width = 44;  // Default item size
        Height = 44;
    }
    
    public override void Draw(SpriteBatch spriteBatch, GumpRenderer renderer)
    {
        // Get the actual item art to determine size
        if (!_sizeInitialized)
        {
            _sizeInitialized = true;
        }
        
        renderer.DrawItem(spriteBatch, ItemId, ScreenX, ScreenY, Hue);
        DrawChildren(spriteBatch, renderer);
    }
    
    public override void OnMouseClick(int localX, int localY, MouseButton button)
    {
        if (button == MouseButton.Left && Serial != 0)
        {
            // Single click - select/pickup item
            OnItemPickup?.Invoke(Serial);
        }
    }
    
    public override void OnMouseDoubleClick(int localX, int localY, MouseButton button)
    {
        if (button == MouseButton.Left && Serial != 0)
        {
            // Double click - use item
            OnItemUse?.Invoke(Serial);
        }
    }
}

/// <summary>
/// Transparent alpha region
/// </summary>
public class GumpAlphaRegionControl : GumpControl
{
    public override void Draw(SpriteBatch spriteBatch, GumpRenderer renderer)
    {
        renderer.DrawRectangle(spriteBatch, ScreenX, ScreenY, Width, Height, new Color(0, 0, 0, 128));
        DrawChildren(spriteBatch, renderer);
    }
}

/// <summary>
/// Paperdoll body control - renders character body and equipment layers
/// </summary>
public class GumpPaperdollBodyControl : GumpControl
{
    public ushort BodyId { get; set; }
    public byte Gender { get; set; }
    public ushort SkinHue { get; set; }
    public ushort HairStyle { get; set; }
    public ushort HairHue { get; set; }
    public ushort BeardStyle { get; set; }
    public ushort BeardHue { get; set; }
    
    /// <summary>
    /// Equipment layers (layer, serial, itemId, gumpId, hue)
    /// </summary>
    public List<(byte Layer, ulong Serial, ushort ItemId, ushort GumpId, ushort Hue)> Layers { get; } = new();
    
    /// <summary>
    /// Event when an equipment layer is clicked (for unequip)
    /// </summary>
    public event Action<byte, ulong>? OnLayerClicked;
    
    // UO Constants - matching ClassicUO exactly
    private const ushort MALE_BODY_GUMP = 0x000C;    // 12
    private const ushort FEMALE_BODY_GUMP = 0x000D;  // 13
    private const int MALE_GUMP_OFFSET = 50000;
    private const int FEMALE_GUMP_OFFSET = 60000;
    
    // Layer render order (back to front) - from ClassicUO PaperDollInteractable.cs
    private static readonly Layer[] LayerOrder = new Layer[]
    {
        Layer.Cloak,      // 0x14 - drawn first (behind)
        Layer.Shirt,      // 0x05
        Layer.Pants,      // 0x04
        Layer.Shoes,      // 0x03
        Layer.InnerLegs,  // 0x18
        Layer.Arms,       // 0x13
        Layer.InnerTorso, // 0x0D
        Layer.MiddleTorso,// 0x11
        Layer.Ring,       // 0x08
        Layer.Bracelet,   // 0x0E
        Layer.Face,       // 0x0F
        Layer.Gloves,     // 0x07
        Layer.OuterLegs,  // 0x17
        Layer.OuterTorso, // 0x16
        Layer.Waist,      // 0x0C
        Layer.Necklace,   // 0x0A
        Layer.Hair,       // 0x0B
        Layer.FacialHair, // 0x10
        Layer.Earrings,   // 0x12
        Layer.Helm,       // 0x06
        Layer.OneHanded,  // 0x01
        Layer.TwoHanded,  // 0x02
        Layer.Talisman    // 0x09 - drawn last (on top)
    };
    
    // Cache for pixel texture used in fallback rendering
    private static Texture2D? _pixelTexture;
    
    public GumpPaperdollBodyControl()
    {
        AcceptsMouseInput = true;
        Width = 140;
        Height = 230;
    }
    
    public override void Draw(SpriteBatch spriteBatch, GumpRenderer renderer)
    {
        EnsurePixelTexture(spriteBatch.GraphicsDevice);
        
        bool isFemale = Gender == 1;
        ushort bodyGump = isFemale ? FEMALE_BODY_GUMP : MALE_BODY_GUMP;
        int gumpOffset = isFemale ? FEMALE_GUMP_OFFSET : MALE_GUMP_OFFSET;
        
        // Try to render UO body gump first
        var bodyTexture = renderer.GetGumpTexture(bodyGump);
        if (bodyTexture != null)
        {
            // Apply skin hue to body
            var skinColor = GetHueColor(SkinHue);
            spriteBatch.Draw(bodyTexture, new Vector2(ScreenX, ScreenY), skinColor);
            
            // Render equipment layers in order using UO gumps
            RenderEquipmentLayersWithGumps(spriteBatch, renderer, gumpOffset);
        }
        else
        {
            // Fallback: draw stylized character
            DrawStylizedCharacter(spriteBatch, isFemale);
        }
        
        DrawChildren(spriteBatch, renderer);
    }
    
    private void RenderEquipmentLayersWithGumps(SpriteBatch spriteBatch, GumpRenderer renderer, int gumpOffset)
    {
        foreach (var layer in LayerOrder)
        {
            byte layerByte = (byte)layer;
            var equipped = Layers.FirstOrDefault(l => l.Layer == layerByte);
            
            if (equipped.GumpId != 0 || equipped.ItemId != 0)
            {
                // Calculate paperdoll gump ID: AnimID + offset
                // The GumpId sent from server should already be calculated, but verify
                int gumpId = equipped.GumpId != 0 ? equipped.GumpId : (equipped.ItemId + gumpOffset);
                
                var texture = renderer.GetGumpTexture(gumpId);
                if (texture != null)
                {
                    var color = GetHueColor(equipped.Hue);
                    spriteBatch.Draw(texture, new Vector2(ScreenX, ScreenY), color);
                }
            }
        }
        
        // Hair and beard are special - use their style + offset
        if (HairStyle > 0)
        {
            int hairGumpId = HairStyle + gumpOffset;
            var hairTexture = renderer.GetGumpTexture(hairGumpId);
            if (hairTexture != null)
            {
                spriteBatch.Draw(hairTexture, new Vector2(ScreenX, ScreenY), GetHueColor(HairHue));
            }
        }
        
        if (BeardStyle > 0 && Gender == 0) // Male only
        {
            int beardGumpId = BeardStyle + gumpOffset;
            var beardTexture = renderer.GetGumpTexture(beardGumpId);
            if (beardTexture != null)
            {
                spriteBatch.Draw(beardTexture, new Vector2(ScreenX, ScreenY), GetHueColor(BeardHue));
            }
        }
    }
    
    private void DrawStylizedCharacter(SpriteBatch spriteBatch, bool isFemale)
    {
        // Nice looking stylized character - positioned to fit paperdoll frame
        int cx = ScreenX + 70;  // Center X
        int baseY = ScreenY + 15;
        
        var skin = GetSkinColor(SkinHue);
        var skinDark = DarkenColor(skin, 0.8f);
        var skinLight = LightenColor(skin, 1.1f);
        
        // Get equipment colors
        var shirtColor = GetEquipmentColor(Layer.Shirt, isFemale ? new Color(180, 80, 100) : new Color(80, 100, 160));
        var pantsColor = GetEquipmentColor(Layer.Pants, new Color(70, 60, 90));
        var shoesColor = GetEquipmentColor(Layer.Shoes, new Color(90, 60, 40));
        var cloakColor = GetEquipmentColor(Layer.Cloak, Color.Transparent);
        var helmColor = GetEquipmentColor(Layer.Helm, Color.Transparent);
        var glovesColor = GetEquipmentColor(Layer.Gloves, Color.Transparent);
        
        // === CLOAK (behind everything) ===
        if (cloakColor != Color.Transparent)
        {
            DrawEllipse(spriteBatch, cx, baseY + 60, 50, 120, DarkenColor(cloakColor, 0.7f));
        }
        
        // === BODY ===
        // Torso
        DrawEllipse(spriteBatch, cx, baseY + 75, 32, 50, shirtColor);
        DrawEllipse(spriteBatch, cx, baseY + 72, 28, 45, LightenColor(shirtColor, 1.1f)); // Highlight
        
        // Waist/belt area
        var waistColor = GetEquipmentColor(Layer.Waist, pantsColor);
        DrawEllipse(spriteBatch, cx, baseY + 115, 26, 12, waistColor);
        
        // === LEGS ===
        // Left leg
        DrawRoundedRect(spriteBatch, cx - 18, baseY + 120, 14, 55, pantsColor);
        DrawRoundedRect(spriteBatch, cx - 16, baseY + 122, 10, 50, LightenColor(pantsColor, 1.1f));
        
        // Right leg  
        DrawRoundedRect(spriteBatch, cx + 4, baseY + 120, 14, 55, pantsColor);
        DrawRoundedRect(spriteBatch, cx + 6, baseY + 122, 10, 50, LightenColor(pantsColor, 1.1f));
        
        // === FEET ===
        DrawEllipse(spriteBatch, cx - 11, baseY + 178, 12, 8, shoesColor);
        DrawEllipse(spriteBatch, cx + 11, baseY + 178, 12, 8, shoesColor);
        
        // === ARMS ===
        var armColor = glovesColor != Color.Transparent ? glovesColor : skin;
        
        // Left arm
        DrawRoundedRect(spriteBatch, cx - 42, baseY + 50, 12, 45, shirtColor);
        DrawEllipse(spriteBatch, cx - 36, baseY + 95, 8, 12, armColor); // Hand
        
        // Right arm
        DrawRoundedRect(spriteBatch, cx + 30, baseY + 50, 12, 45, shirtColor);
        DrawEllipse(spriteBatch, cx + 36, baseY + 95, 8, 12, armColor); // Hand
        
        // === NECK ===
        DrawEllipse(spriteBatch, cx, baseY + 32, 10, 12, skin);
        
        // === HEAD ===
        // Base head shape
        DrawEllipse(spriteBatch, cx, baseY + 15, 22, 26, skin);
        DrawEllipse(spriteBatch, cx - 3, baseY + 12, 18, 22, skinLight); // Highlight
        
        // === HAIR ===
        if (HairStyle > 0)
        {
            var hairColor = GetHairColor(HairHue);
            // Different hair styles
            int style = HairStyle % 10;
            switch (style)
            {
                case 1: // Short
                    DrawEllipse(spriteBatch, cx, baseY + 5, 24, 18, hairColor);
                    break;
                case 2: // Medium
                    DrawEllipse(spriteBatch, cx, baseY + 3, 26, 22, hairColor);
                    DrawEllipse(spriteBatch, cx - 20, baseY + 18, 8, 20, hairColor);
                    DrawEllipse(spriteBatch, cx + 20, baseY + 18, 8, 20, hairColor);
                    break;
                case 3: // Long
                    DrawEllipse(spriteBatch, cx, baseY + 2, 28, 24, hairColor);
                    DrawRoundedRect(spriteBatch, cx - 26, baseY + 12, 12, 50, hairColor);
                    DrawRoundedRect(spriteBatch, cx + 14, baseY + 12, 12, 50, hairColor);
                    break;
                default: // Default short
                    DrawEllipse(spriteBatch, cx, baseY + 4, 25, 20, hairColor);
                    break;
            }
        }
        
        // === FACIAL HAIR (Beard) ===
        if (BeardStyle > 0 && !isFemale)
        {
            var beardColor = GetHairColor(BeardHue);
            int style = BeardStyle % 5;
            switch (style)
            {
                case 1: // Goatee
                    DrawEllipse(spriteBatch, cx, baseY + 35, 8, 10, beardColor);
                    break;
                case 2: // Full beard
                    DrawEllipse(spriteBatch, cx, baseY + 32, 18, 18, beardColor);
                    break;
                case 3: // Mustache
                    DrawRoundedRect(spriteBatch, cx - 10, baseY + 24, 20, 6, beardColor);
                    break;
                default:
                    DrawEllipse(spriteBatch, cx, baseY + 33, 14, 12, beardColor);
                    break;
            }
        }
        
        // === HELMET (on top) ===
        if (helmColor != Color.Transparent)
        {
            DrawEllipse(spriteBatch, cx, baseY + 5, 28, 24, helmColor);
            DrawEllipse(spriteBatch, cx, baseY + 2, 24, 20, LightenColor(helmColor, 1.2f));
        }
        
        // === FACIAL FEATURES ===
        var eyeColor = new Color(40, 40, 40);
        // Eyes
        FillRect(spriteBatch, cx - 8, baseY + 12, 4, 3, eyeColor);
        FillRect(spriteBatch, cx + 4, baseY + 12, 4, 3, eyeColor);
        
        // === WEAPONS ===
        DrawWeapons(spriteBatch, cx, baseY);
    }
    
    private void DrawWeapons(SpriteBatch spriteBatch, int cx, int baseY)
    {
        var oneHand = Layers.FirstOrDefault(l => l.Layer == (byte)Layer.OneHanded);
        var twoHand = Layers.FirstOrDefault(l => l.Layer == (byte)Layer.TwoHanded);
        
        // Right hand weapon (one-handed)
        if (oneHand.ItemId != 0)
        {
            var weaponColor = GetHueColor(oneHand.Hue);
            if (weaponColor == Color.White) weaponColor = new Color(180, 180, 180);
            
            // Draw sword-like shape
            DrawRoundedRect(spriteBatch, cx + 40, baseY + 40, 6, 60, weaponColor);
            DrawRoundedRect(spriteBatch, cx + 35, baseY + 88, 16, 6, DarkenColor(weaponColor, 0.7f)); // Guard
            DrawRoundedRect(spriteBatch, cx + 38, baseY + 94, 10, 15, new Color(80, 50, 30)); // Handle
        }
        
        // Left hand / shield (two-handed or shield)
        if (twoHand.ItemId != 0)
        {
            var shieldColor = GetHueColor(twoHand.Hue);
            if (shieldColor == Color.White) shieldColor = new Color(160, 140, 100);
            
            // Draw shield-like shape
            DrawEllipse(spriteBatch, cx - 48, baseY + 75, 18, 28, shieldColor);
            DrawEllipse(spriteBatch, cx - 48, baseY + 73, 14, 22, LightenColor(shieldColor, 1.2f));
        }
    }
    
    private Color GetEquipmentColor(Layer layer, Color defaultColor)
    {
        var equipped = Layers.FirstOrDefault(l => l.Layer == (byte)layer);
        if (equipped.ItemId != 0 || equipped.GumpId != 0)
        {
            var hueColor = GetHueColor(equipped.Hue);
            return hueColor == Color.White ? defaultColor : hueColor;
        }
        return defaultColor;
    }
    
    private Color GetSkinColor(ushort hue)
    {
        if (hue == 0) return new Color(225, 185, 150); // Default caucasian
        
        // Common UO skin hues
        return hue switch
        {
            1002 => new Color(225, 185, 150), // Light
            1003 => new Color(200, 160, 130), // Medium
            1004 => new Color(170, 130, 100), // Tan
            1005 => new Color(140, 100, 70),  // Dark
            1006 => new Color(100, 70, 50),   // Very dark
            _ => new Color(225, 185, 150)
        };
    }
    
    private Color GetHairColor(ushort hue)
    {
        if (hue == 0) return new Color(60, 40, 20); // Default brown
        
        return hue switch
        {
            1102 => new Color(60, 40, 20),    // Brown
            1103 => new Color(30, 20, 10),    // Black
            1104 => new Color(180, 140, 60),  // Blonde
            1105 => new Color(140, 60, 30),   // Red
            1106 => new Color(120, 120, 120), // Gray
            1107 => new Color(200, 200, 200), // White
            _ => GetHueColor(hue)
        };
    }
    
    private Color GetHueColor(int hue)
    {
        if (hue == 0) return Color.White;
        
        // Map common UO hues to colors
        return hue switch
        {
            33 => new Color(200, 50, 50),        // Red
            38 => new Color(220, 80, 80),        // Light red
            53 => new Color(50, 100, 200),       // Blue
            68 => new Color(50, 150, 50),        // Green
            1153 => Color.White,                  // Bright white
            1157 => new Color(218, 165, 32),     // Gold
            1150 => new Color(180, 180, 180),    // Light gray
            1109 => new Color(100, 100, 100),    // Gray
            1175 => new Color(50, 50, 50),       // Dark
            1102 => new Color(139, 90, 43),      // Brown
            1152 => new Color(60, 120, 200),     // Blue
            1161 => new Color(60, 160, 60),      // Green
            1154 => new Color(140, 60, 140),     // Purple
            1260 => new Color(200, 120, 60),     // Orange
            _ => HueToColor(hue)
        };
    }
    
    private Color HueToColor(int hue)
    {
        // Convert UO hue index to RGB
        // UO hues are organized in groups of 8 shades
        int baseHue = (hue - 1) / 8;
        int shade = (hue - 1) % 8;
        float brightness = 1.0f - (shade * 0.1f);
        
        // Approximate hue wheel position
        float h = (baseHue % 180) / 180f;
        return HsvToRgb(h, 0.7f, brightness);
    }
    
    private Color HsvToRgb(float h, float s, float v)
    {
        int i = (int)(h * 6);
        float f = h * 6 - i;
        float p = v * (1 - s);
        float q = v * (1 - f * s);
        float t = v * (1 - (1 - f) * s);
        
        float r, g, b;
        switch (i % 6)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            default: r = v; g = p; b = q; break;
        }
        
        return new Color(r, g, b);
    }
    
    private Color DarkenColor(Color c, float factor)
    {
        return new Color((int)(c.R * factor), (int)(c.G * factor), (int)(c.B * factor), c.A);
    }
    
    private Color LightenColor(Color c, float factor)
    {
        return new Color(
            Math.Min(255, (int)(c.R * factor)),
            Math.Min(255, (int)(c.G * factor)),
            Math.Min(255, (int)(c.B * factor)),
            c.A);
    }
    
    // === Drawing primitives ===
    
    private void EnsurePixelTexture(GraphicsDevice device)
    {
        if (_pixelTexture == null || _pixelTexture.IsDisposed)
        {
            _pixelTexture = new Texture2D(device, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
        }
    }
    
    private void FillRect(SpriteBatch sb, int x, int y, int w, int h, Color color)
    {
        sb.Draw(_pixelTexture!, new Rectangle(x, y, w, h), color);
    }
    
    private void DrawRoundedRect(SpriteBatch sb, int x, int y, int w, int h, Color color)
    {
        // Simple rounded rect approximation with ellipse caps
        int capHeight = Math.Min(h / 4, w / 2);
        
        // Main body
        FillRect(sb, x, y + capHeight, w, h - capHeight * 2, color);
        
        // Top cap
        DrawEllipse(sb, x + w/2, y + capHeight, w/2, capHeight, color);
        
        // Bottom cap
        DrawEllipse(sb, x + w/2, y + h - capHeight, w/2, capHeight, color);
    }
    
    private void DrawEllipse(SpriteBatch sb, int cx, int cy, int rx, int ry, Color color)
    {
        if (color == Color.Transparent) return;
        
        // Draw ellipse using horizontal lines (scanline fill)
        for (int dy = -ry; dy <= ry; dy++)
        {
            // Calculate x extent at this y using ellipse equation
            float yRatio = (float)dy / ry;
            float xExtent = rx * (float)Math.Sqrt(1 - yRatio * yRatio);
            int x1 = (int)(cx - xExtent);
            int x2 = (int)(cx + xExtent);
            int width = x2 - x1;
            if (width > 0)
            {
                FillRect(sb, x1, cy + dy, width, 1, color);
            }
        }
    }
    
    /// <summary>
    /// Get the equipment drop zone at a local position
    /// </summary>
    public Layer? GetDropZoneAt(int localX, int localY)
    {
        // Calculate zones relative to character center
        int cx = 70;  // Center X relative to control
        int baseY = 15;
        
        // Check each zone from front to back
        // Head zone
        if (IsInZone(localX, localY, cx - 25, baseY, 50, 35))
            return Layer.Helm;
        
        // Neck zone
        if (IsInZone(localX, localY, cx - 15, baseY + 32, 30, 20))
            return Layer.Necklace;
        
        // Torso zone (covers multiple layers)
        if (IsInZone(localX, localY, cx - 35, baseY + 50, 70, 70))
            return Layer.InnerTorso;  // Default to chest armor
        
        // Arms zone (left)
        if (IsInZone(localX, localY, cx - 55, baseY + 50, 20, 60))
            return Layer.Arms;
        
        // Arms zone (right)  
        if (IsInZone(localX, localY, cx + 35, baseY + 50, 20, 60))
            return Layer.Arms;
        
        // Hands zone (left)
        if (IsInZone(localX, localY, cx - 55, baseY + 90, 20, 25))
            return Layer.Gloves;
        
        // Hands zone (right) - also weapon
        if (IsInZone(localX, localY, cx + 35, baseY + 90, 25, 35))
            return Layer.OneHanded;
        
        // Waist zone
        if (IsInZone(localX, localY, cx - 30, baseY + 115, 60, 15))
            return Layer.Waist;
        
        // Legs zone
        if (IsInZone(localX, localY, cx - 25, baseY + 120, 50, 60))
            return Layer.Pants;
        
        // Feet zone
        if (IsInZone(localX, localY, cx - 25, baseY + 175, 50, 20))
            return Layer.Shoes;
        
        // Back/Cloak zone (large area behind character)
        if (IsInZone(localX, localY, cx - 50, baseY + 40, 100, 100))
            return Layer.Cloak;
        
        return null;
    }
    
    private bool IsInZone(int x, int y, int zoneX, int zoneY, int zoneW, int zoneH)
    {
        return x >= zoneX && x < zoneX + zoneW && y >= zoneY && y < zoneY + zoneH;
    }
    
    /// <summary>
    /// Get all drop zones with their bounds
    /// </summary>
    public IEnumerable<(Layer Layer, Rectangle Bounds)> GetDropZones()
    {
        int cx = ScreenX + 70;
        int baseY = ScreenY + 15;
        
        yield return (Layer.Helm, new Rectangle(cx - 25, baseY, 50, 35));
        yield return (Layer.Necklace, new Rectangle(cx - 15, baseY + 32, 30, 20));
        yield return (Layer.InnerTorso, new Rectangle(cx - 35, baseY + 50, 70, 70));
        yield return (Layer.Arms, new Rectangle(cx - 55, baseY + 50, 20, 60));
        yield return (Layer.Gloves, new Rectangle(cx - 55, baseY + 90, 20, 25));
        yield return (Layer.OneHanded, new Rectangle(cx + 35, baseY + 90, 25, 35));
        yield return (Layer.Waist, new Rectangle(cx - 30, baseY + 115, 60, 15));
        yield return (Layer.Pants, new Rectangle(cx - 25, baseY + 120, 50, 60));
        yield return (Layer.Shoes, new Rectangle(cx - 25, baseY + 175, 50, 20));
    }
    
    /// <summary>
    /// Draw drop zone highlights (when dragging an item)
    /// </summary>
    public void DrawDropZoneHighlight(SpriteBatch spriteBatch, Layer? validLayer, int mouseX, int mouseY)
    {
        if (!AcceptsMouseInput) return;
        
        var dropZone = GetDropZoneAt(mouseX - ScreenX, mouseY - ScreenY);
        if (dropZone == null) return;
        
        // Highlight the zone
        foreach (var (layer, bounds) in GetDropZones())
        {
            if (layer == dropZone)
            {
                bool isValid = validLayer == null || validLayer == layer;
                var color = isValid ? new Color(0, 255, 0, 80) : new Color(255, 0, 0, 80);
                FillRect(spriteBatch, bounds.X, bounds.Y, bounds.Width, bounds.Height, color);
                break;
            }
        }
    }
    
    public override void OnMouseDoubleClick(int localX, int localY, MouseButton button)
    {
        if (button != MouseButton.Left) return;
        
        // Check if clicking on an equipment layer to unequip
        foreach (var layer in Layers)
        {
            if (layer.Serial != 0)
            {
                OnLayerClicked?.Invoke(layer.Layer, layer.Serial);
                return;
            }
        }
    }
}
