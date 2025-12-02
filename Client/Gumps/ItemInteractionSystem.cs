using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RealmOfReality.Shared.Items;
using ItemRarity = RealmOfReality.Shared.Items.ItemRarity;

#pragma warning disable CS0067 // Events are declared for future use
#pragma warning disable CS0414 // Fields are assigned for future use

namespace RealmOfReality.Client.Gumps;

/// <summary>
/// Manages item dragging, tooltips, and visual feedback for item interactions
/// </summary>
public class ItemInteractionSystem
{
    private readonly GumpRenderer _renderer;
    private SpriteFont? _font;
    private Texture2D? _pixelTexture;
    
    // Dragged item state
    private DraggedItem? _draggedItem;
    private int _dragOffsetX, _dragOffsetY;
    private float _dragStartTime;
    private const float DragDelay = 0.15f; // Delay before drag starts
    
    // Tooltip state
    private TooltipInfo? _currentTooltip;
    private float _tooltipShowDelay = 0.5f;
    private float _tooltipTimer;
    private ulong _hoveredItemSerial;
    
    // Visual feedback
    private DropFeedback? _dropFeedback;
    private float _feedbackTimer;
    private const float FeedbackDuration = 0.5f;
    
    // Events
    public event Action<ulong, int, int>? OnItemDropped;      // Serial, X, Y (screen coords)
    public event Action<ulong, byte>? OnItemEquipped;          // Serial, Layer
    public event Action<ulong, ulong>? OnItemDroppedOnItem;    // DraggedSerial, TargetSerial
    public event Action<ulong>? OnItemDoubleClicked;           // Serial
    
    public bool IsDragging => _draggedItem != null;
    public DraggedItem? DraggedItem => _draggedItem;
    
    public ItemInteractionSystem(GumpRenderer renderer)
    {
        _renderer = renderer;
    }
    
    public void Initialize(GraphicsDevice device, SpriteFont font)
    {
        _font = font;
        _pixelTexture = new Texture2D(device, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }
    
    /// <summary>
    /// Start dragging an item
    /// </summary>
    public void StartDrag(ulong serial, int itemId, int hue, string name, int mouseX, int mouseY, ItemDefinition? definition = null)
    {
        _draggedItem = new DraggedItem
        {
            Serial = serial,
            ItemId = itemId,
            Hue = hue,
            Name = name,
            Definition = definition,
            StartX = mouseX,
            StartY = mouseY
        };
        _dragOffsetX = 22; // Center on cursor
        _dragOffsetY = 22;
        _dragStartTime = 0;
    }
    
    /// <summary>
    /// Cancel current drag
    /// </summary>
    public void CancelDrag()
    {
        _draggedItem = null;
    }
    
    /// <summary>
    /// Complete a drag at the given position
    /// </summary>
    public void CompleteDrag(int mouseX, int mouseY, DropTarget? target)
    {
        if (_draggedItem == null) return;
        
        var item = _draggedItem;
        _draggedItem = null;
        
        if (target != null)
        {
            switch (target.Type)
            {
                case DropTargetType.EquipmentSlot:
                    OnItemEquipped?.Invoke(item.Serial, target.Layer);
                    ShowFeedback(mouseX, mouseY, true, $"Equipped to {GetLayerName(target.Layer)}");
                    break;
                    
                case DropTargetType.Container:
                    OnItemDroppedOnItem?.Invoke(item.Serial, target.TargetSerial);
                    break;
                    
                case DropTargetType.Ground:
                    OnItemDropped?.Invoke(item.Serial, mouseX, mouseY);
                    break;
            }
        }
        else
        {
            // Dropped on nothing - return to original position or drop on ground
            OnItemDropped?.Invoke(item.Serial, mouseX, mouseY);
        }
    }
    
    /// <summary>
    /// Show a tooltip for an item
    /// </summary>
    public void ShowTooltip(ulong serial, int itemId, string name, ItemDefinition? definition, int mouseX, int mouseY)
    {
        if (_hoveredItemSerial == serial && _currentTooltip != null)
        {
            // Already showing tooltip for this item
            _currentTooltip.X = mouseX + 20;
            _currentTooltip.Y = mouseY + 20;
            return;
        }
        
        _hoveredItemSerial = serial;
        _tooltipTimer = 0;
        
        // Build tooltip content
        var lines = new List<TooltipLine>
        {
            new TooltipLine { Text = name, Color = GetRarityColor(definition?.Rarity ?? ItemRarity.Common), Bold = true }
        };
        
        if (definition != null)
        {
            // Item type
            if (definition.Layer != Layer.Invalid)
            {
                lines.Add(new TooltipLine { Text = GetLayerName((byte)definition.Layer), Color = Color.LightGray });
            }
            
            // Weight
            if (definition.Weight > 0)
            {
                lines.Add(new TooltipLine { Text = $"Weight: {definition.Weight:F1} stones", Color = Color.Gray });
            }
            
            // Damage for weapons
            if (definition.MinDamage > 0 || definition.MaxDamage > 0)
            {
                lines.Add(new TooltipLine { Text = $"Damage: {definition.MinDamage}-{definition.MaxDamage}", Color = Color.LightCoral });
            }
            
            // Armor
            if (definition.Armor > 0)
            {
                lines.Add(new TooltipLine { Text = $"Armor: {definition.Armor}", Color = Color.LightBlue });
            }
            
            // Stat bonuses
            AddStatBonusLines(lines, definition);
            
            // Requirements
            if (definition.RequiredStrength > 0)
            {
                lines.Add(new TooltipLine { Text = $"Strength Required: {definition.RequiredStrength}", Color = Color.Orange });
            }
            if (definition.RequiredDexterity > 0)
            {
                lines.Add(new TooltipLine { Text = $"Dexterity Required: {definition.RequiredDexterity}", Color = Color.Orange });
            }
            if (definition.RequiredIntelligence > 0)
            {
                lines.Add(new TooltipLine { Text = $"Intelligence Required: {definition.RequiredIntelligence}", Color = Color.Orange });
            }
            
            // Special properties
            if (definition.Flags.HasFlag(ItemFlags.Blessed))
            {
                lines.Add(new TooltipLine { Text = "Blessed", Color = Color.Gold });
            }
            if (definition.Flags.HasFlag(ItemFlags.Magic))
            {
                lines.Add(new TooltipLine { Text = "Magical", Color = Color.Cyan });
            }
            if (definition.Flags.HasFlag(ItemFlags.Cursed))
            {
                lines.Add(new TooltipLine { Text = "Cursed", Color = Color.DarkRed });
            }
            
            // Description
            if (!string.IsNullOrEmpty(definition.Description))
            {
                lines.Add(new TooltipLine { Text = "", Color = Color.Transparent }); // Spacer
                lines.Add(new TooltipLine { Text = definition.Description, Color = Color.Gray, Italic = true });
            }
        }
        
        _currentTooltip = new TooltipInfo
        {
            Serial = serial,
            X = mouseX + 20,
            Y = mouseY + 20,
            Lines = lines
        };
    }
    
    private void AddStatBonusLines(List<TooltipLine> lines, ItemDefinition def)
    {
        var bonuses = new List<string>();
        
        if (def.BonusStrength != 0)
            bonuses.Add($"Strength {def.BonusStrength:+#;-#;0}");
        if (def.BonusDexterity != 0)
            bonuses.Add($"Dexterity {def.BonusDexterity:+#;-#;0}");
        if (def.BonusIntelligence != 0)
            bonuses.Add($"Intelligence {def.BonusIntelligence:+#;-#;0}");
        if (def.BonusHealth != 0)
            bonuses.Add($"Hit Points {def.BonusHealth:+#;-#;0}");
        if (def.BonusMana != 0)
            bonuses.Add($"Mana {def.BonusMana:+#;-#;0}");
        if (def.BonusStamina != 0)
            bonuses.Add($"Stamina {def.BonusStamina:+#;-#;0}");
        
        foreach (var bonus in bonuses)
        {
            lines.Add(new TooltipLine { Text = bonus, Color = Color.LightGreen });
        }
    }
    
    /// <summary>
    /// Hide tooltip
    /// </summary>
    public void HideTooltip()
    {
        _currentTooltip = null;
        _hoveredItemSerial = 0;
        _tooltipTimer = 0;
    }
    
    /// <summary>
    /// Show visual feedback for drop result
    /// </summary>
    public void ShowFeedback(int x, int y, bool success, string message)
    {
        _dropFeedback = new DropFeedback
        {
            X = x,
            Y = y,
            Success = success,
            Message = message
        };
        _feedbackTimer = FeedbackDuration;
    }
    
    /// <summary>
    /// Update the interaction system
    /// </summary>
    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // Update drag start time
        if (_draggedItem != null)
        {
            _dragStartTime += dt;
        }
        
        // Update tooltip timer
        if (_hoveredItemSerial != 0 && _currentTooltip == null)
        {
            _tooltipTimer += dt;
        }
        
        // Update feedback timer
        if (_dropFeedback != null)
        {
            _feedbackTimer -= dt;
            if (_feedbackTimer <= 0)
            {
                _dropFeedback = null;
            }
        }
    }
    
    /// <summary>
    /// Draw the interaction system overlays (dragged item, tooltip, feedback)
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, int mouseX, int mouseY)
    {
        // Draw dragged item
        if (_draggedItem != null && _dragStartTime > DragDelay)
        {
            int drawX = mouseX - _dragOffsetX;
            int drawY = mouseY - _dragOffsetY;
            
            // Semi-transparent item (alpha not supported, just draw normally)
            _renderer.DrawItem(spriteBatch, _draggedItem.ItemId, drawX, drawY, _draggedItem.Hue);
        }
        
        // Draw tooltip
        if (_currentTooltip != null && _font != null)
        {
            DrawTooltip(spriteBatch, _currentTooltip);
        }
        
        // Draw feedback
        if (_dropFeedback != null && _font != null)
        {
            DrawFeedback(spriteBatch, _dropFeedback);
        }
    }
    
    private void DrawTooltip(SpriteBatch spriteBatch, TooltipInfo tooltip)
    {
        if (_font == null || _pixelTexture == null) return;
        
        // Calculate tooltip size
        int maxWidth = 0;
        int totalHeight = 8; // Padding
        
        foreach (var line in tooltip.Lines)
        {
            if (string.IsNullOrEmpty(line.Text)) 
            {
                totalHeight += 8; // Spacer
                continue;
            }
            var size = _font.MeasureString(line.Text);
            maxWidth = Math.Max(maxWidth, (int)size.X);
            totalHeight += (int)size.Y + 2;
        }
        
        maxWidth += 16; // Padding
        totalHeight += 8;
        
        // Clamp position to screen
        int x = Math.Min(tooltip.X, 1920 - maxWidth - 10);
        int y = Math.Min(tooltip.Y, 1080 - totalHeight - 10);
        
        // Draw background
        DrawRoundedRect(spriteBatch, x, y, maxWidth, totalHeight, new Color(20, 20, 30, 240));
        DrawRectOutline(spriteBatch, x, y, maxWidth, totalHeight, new Color(100, 100, 120));
        
        // Draw lines
        int lineY = y + 6;
        foreach (var line in tooltip.Lines)
        {
            if (string.IsNullOrEmpty(line.Text))
            {
                lineY += 8;
                continue;
            }
            spriteBatch.DrawString(_font, line.Text, new Vector2(x + 8, lineY), line.Color);
            lineY += (int)_font.MeasureString(line.Text).Y + 2;
        }
    }
    
    private void DrawFeedback(SpriteBatch spriteBatch, DropFeedback feedback)
    {
        if (_font == null || _pixelTexture == null) return;
        
        float alpha = Math.Min(1f, _feedbackTimer * 2);
        float yOffset = (FeedbackDuration - _feedbackTimer) * 30; // Float upward
        
        var color = feedback.Success ? new Color(50, 200, 50) : new Color(200, 50, 50);
        color *= alpha;
        
        var size = _font.MeasureString(feedback.Message);
        int x = feedback.X - (int)size.X / 2;
        int y = feedback.Y - (int)yOffset;
        
        // Shadow
        spriteBatch.DrawString(_font, feedback.Message, new Vector2(x + 1, y + 1), Color.Black * alpha * 0.5f);
        // Text
        spriteBatch.DrawString(_font, feedback.Message, new Vector2(x, y), color);
    }
    
    private void DrawRoundedRect(SpriteBatch spriteBatch, int x, int y, int w, int h, Color color)
    {
        if (_pixelTexture == null) return;
        spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, w, h), color);
    }
    
    private void DrawRectOutline(SpriteBatch spriteBatch, int x, int y, int w, int h, Color color)
    {
        if (_pixelTexture == null) return;
        spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, w, 1), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(x, y + h - 1, w, 1), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(x, y, 1, h), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(x + w - 1, y, 1, h), color);
    }
    
    private Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => Color.White,
            ItemRarity.Uncommon => new Color(30, 255, 30),     // Green
            ItemRarity.Rare => new Color(80, 130, 255),        // Blue
            ItemRarity.Epic => new Color(180, 80, 255),        // Purple
            ItemRarity.Legendary => new Color(255, 165, 0),    // Orange
            ItemRarity.Artifact => new Color(255, 215, 0),     // Gold
            _ => Color.White
        };
    }
    
    private string GetLayerName(byte layer)
    {
        return ((Layer)layer) switch
        {
            Layer.OneHanded => "One-Handed Weapon",
            Layer.TwoHanded => "Two-Handed / Shield",
            Layer.Shoes => "Feet",
            Layer.Pants => "Legs",
            Layer.Shirt => "Torso",
            Layer.Helm => "Head",
            Layer.Gloves => "Hands",
            Layer.Ring => "Ring",
            Layer.Talisman => "Talisman",
            Layer.Necklace => "Neck",
            Layer.Waist => "Waist",
            Layer.InnerTorso => "Chest Armor",
            Layer.Bracelet => "Bracelet",
            Layer.MiddleTorso => "Tunic",
            Layer.Earrings => "Earrings",
            Layer.Arms => "Arms",
            Layer.Cloak => "Cloak",
            Layer.OuterTorso => "Robe",
            Layer.OuterLegs => "Skirt/Kilt",
            Layer.InnerLegs => "Leg Armor",
            _ => "Equipment"
        };
    }
}

/// <summary>
/// Information about a dragged item
/// </summary>
public class DraggedItem
{
    public ulong Serial { get; set; }
    public int ItemId { get; set; }
    public int Hue { get; set; }
    public string Name { get; set; } = "";
    public ItemDefinition? Definition { get; set; }
    public int StartX { get; set; }
    public int StartY { get; set; }
}

/// <summary>
/// Drop target information
/// </summary>
public class DropTarget
{
    public DropTargetType Type { get; set; }
    public byte Layer { get; set; }
    public ulong TargetSerial { get; set; }
    public Rectangle Bounds { get; set; }
}

public enum DropTargetType
{
    None,
    EquipmentSlot,
    Container,
    Ground
}

/// <summary>
/// Tooltip information
/// </summary>
public class TooltipInfo
{
    public ulong Serial { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public List<TooltipLine> Lines { get; set; } = new();
}

public class TooltipLine
{
    public string Text { get; set; } = "";
    public Color Color { get; set; } = Color.White;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
}

/// <summary>
/// Visual feedback for drops
/// </summary>
public class DropFeedback
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}
