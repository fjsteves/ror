using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RealmOfReality.Client.Engine;
using RealmOfReality.Client.Game;
using RealmOfReality.Shared.Items;

namespace RealmOfReality.Client.UI;

/// <summary>
/// Equipment/Paperdoll panel showing equipped items
/// </summary>
public class PaperdollPanel
{
    private readonly UIRenderer _ui;
    private readonly InputManager _input;
    private readonly AssetManager _assets;
    
    // Panel bounds
    private Rectangle _panelRect;
    private bool _isVisible = false;
    private bool _isDragging = false;
    private Vector2 _dragOffset;
    
    // Equipment slot rectangles
    private readonly Dictionary<EquipmentSlot, Rectangle> _slotRects = new();
    
    // Dragged item
    private EquipmentSlot? _draggedSlot = null;
    private Vector2 _dragPosition;
    
    // Tooltip
    private EquipmentSlot? _hoveredSlot = null;
    
    // Equipment data (would come from GameState)
    public Equipment? Equipment { get; set; }
    
    // Slot layout configuration
    private static readonly (EquipmentSlot Slot, int X, int Y, int Width, int Height)[] SlotLayout = new[]
    {
        // Head area
        (EquipmentSlot.Head, 95, 20, 50, 50),
        
        // Neck
        (EquipmentSlot.Neck, 95, 80, 50, 30),
        
        // Chest
        (EquipmentSlot.Chest, 95, 120, 50, 80),
        
        // Back (cloak)
        (EquipmentSlot.Back, 155, 80, 40, 60),
        
        // Arms
        (EquipmentSlot.Arms, 35, 120, 50, 40),
        
        // Hands
        (EquipmentSlot.Hands, 35, 170, 50, 40),
        
        // Waist
        (EquipmentSlot.Waist, 95, 210, 50, 30),
        
        // Legs
        (EquipmentSlot.Legs, 95, 250, 50, 70),
        
        // Feet
        (EquipmentSlot.Feet, 95, 330, 50, 40),
        
        // Rings (left side)
        (EquipmentSlot.Ring1, 35, 220, 35, 35),
        (EquipmentSlot.Ring2, 35, 265, 35, 35),
        
        // Weapons (bottom)
        (EquipmentSlot.MainHand, 35, 320, 50, 70),
        (EquipmentSlot.OffHand, 155, 320, 50, 70),
        
        // Ammo
        (EquipmentSlot.Ammo, 155, 250, 40, 40),
    };
    
    public PaperdollPanel(UIRenderer ui, InputManager input, AssetManager assets)
    {
        _ui = ui;
        _input = input;
        _assets = assets;
        
        // Default position
        _panelRect = new Rectangle(900, 100, 240, 420);
        
        UpdateSlotPositions();
    }
    
    private void UpdateSlotPositions()
    {
        _slotRects.Clear();
        
        foreach (var slot in SlotLayout)
        {
            _slotRects[slot.Slot] = new Rectangle(
                _panelRect.X + slot.X,
                _panelRect.Y + slot.Y,
                slot.Width,
                slot.Height
            );
        }
    }
    
    public void Show()
    {
        _isVisible = true;
    }
    
    public void Hide()
    {
        _isVisible = false;
        _draggedSlot = null;
    }
    
    public void Toggle()
    {
        if (_isVisible) Hide();
        else Show();
    }
    
    public bool IsVisible => _isVisible;
    
    public bool IsMouseOver => _isVisible && _ui.IsInside(_panelRect, _input.MousePosition);
    
    public void Update(GameTime gameTime)
    {
        if (!_isVisible) return;
        
        var mousePos = _input.MousePosition;
        
        // Check for hover
        _hoveredSlot = null;
        foreach (var kvp in _slotRects)
        {
            if (_ui.IsInside(kvp.Value, mousePos))
            {
                _hoveredSlot = kvp.Key;
                break;
            }
        }
        
        // Panel dragging (by title bar)
        var titleBar = new Rectangle(_panelRect.X, _panelRect.Y, _panelRect.Width, 25);
        
        if (_input.IsLeftMousePressed && _ui.IsInside(titleBar, mousePos) && _draggedSlot == null)
        {
            _isDragging = true;
            _dragOffset = new Vector2(mousePos.X - _panelRect.X, mousePos.Y - _panelRect.Y);
        }
        
        if (_isDragging)
        {
            if (_input.IsLeftMouseDown)
            {
                _panelRect.X = (int)(mousePos.X - _dragOffset.X);
                _panelRect.Y = (int)(mousePos.Y - _dragOffset.Y);
                
                // Clamp to screen
                _panelRect.X = Math.Clamp(_panelRect.X, 0, 1280 - _panelRect.Width);
                _panelRect.Y = Math.Clamp(_panelRect.Y, 0, 720 - _panelRect.Height);
                
                UpdateSlotPositions();
            }
            else
            {
                _isDragging = false;
            }
        }
        
        // Item dragging
        if (_input.IsLeftMousePressed && _hoveredSlot.HasValue && Equipment != null)
        {
            var item = Equipment[_hoveredSlot.Value];
            if (item != null)
            {
                _draggedSlot = _hoveredSlot;
                _dragPosition = mousePos;
            }
        }
        
        if (_draggedSlot.HasValue)
        {
            if (_input.IsLeftMouseDown)
            {
                _dragPosition = mousePos;
            }
            else
            {
                // Drop the item
                if (_hoveredSlot.HasValue && _hoveredSlot != _draggedSlot)
                {
                    // Swap items
                    OnItemSwap?.Invoke(_draggedSlot.Value, _hoveredSlot.Value);
                }
                else if (!IsMouseOver)
                {
                    // Dropped outside - unequip
                    OnItemUnequip?.Invoke(_draggedSlot.Value);
                }
                
                _draggedSlot = null;
            }
        }
        
        // Right-click to unequip
        if (_input.IsRightMousePressed && _hoveredSlot.HasValue && Equipment != null)
        {
            var item = Equipment[_hoveredSlot.Value];
            if (item != null)
            {
                OnItemUnequip?.Invoke(_hoveredSlot.Value);
            }
        }
        
        // Close button
        var closeBtn = new Rectangle(_panelRect.Right - 25, _panelRect.Y + 3, 20, 20);
        if (_input.IsLeftMousePressed && _ui.IsInside(closeBtn, mousePos))
        {
            Hide();
        }
    }
    
    // Events
    public event Action<EquipmentSlot, EquipmentSlot>? OnItemSwap;
    public event Action<EquipmentSlot>? OnItemUnequip;
    
    public void Draw(GameTime gameTime)
    {
        if (!_isVisible) return;
        
        // Panel background
        _ui.DrawPanel(_panelRect);
        
        // Title bar
        var titleBar = new Rectangle(_panelRect.X, _panelRect.Y, _panelRect.Width, 25);
        _ui.DrawRectangle(titleBar, new Color(50, 50, 70));
        _ui.DrawText("EQUIPMENT", new Vector2(_panelRect.X + 10, _panelRect.Y + 5), Color.White, 1.5f);
        
        // Close button
        var closeBtn = new Rectangle(_panelRect.Right - 25, _panelRect.Y + 3, 20, 20);
        var closeHover = _ui.IsInside(closeBtn, _input.MousePosition);
        _ui.DrawRectangle(closeBtn, closeHover ? new Color(150, 50, 50) : new Color(100, 50, 50));
        _ui.DrawTextCentered("X", new Vector2(closeBtn.X + 10, closeBtn.Y + 3), Color.White, 1.5f);
        
        // Character silhouette background
        var silhouetteRect = new Rectangle(_panelRect.X + 70, _panelRect.Y + 50, 100, 300);
        _ui.DrawRectangle(silhouetteRect, new Color(30, 30, 40, 150));
        
        // Draw equipment slots
        foreach (var kvp in _slotRects)
        {
            if (_draggedSlot == kvp.Key) continue; // Don't draw dragged slot in place
            
            DrawSlot(kvp.Key, kvp.Value);
        }
        
        // Draw dragged item
        if (_draggedSlot.HasValue && Equipment != null)
        {
            var item = Equipment[_draggedSlot.Value];
            if (item != null)
            {
                var dragRect = new Rectangle(
                    (int)_dragPosition.X - 25,
                    (int)_dragPosition.Y - 25,
                    50, 50
                );
                
                _ui.DrawRectangle(dragRect, new Color(80, 80, 120, 200));
                _ui.DrawRectangleOutline(dragRect, Color.Yellow);
                
                // Item icon placeholder
                _ui.DrawTextCentered(GetSlotIcon(_draggedSlot.Value), 
                    new Vector2(dragRect.X + 25, dragRect.Y + 15), Color.White, 2f);
            }
        }
        
        // Draw tooltip
        if (_hoveredSlot.HasValue && _draggedSlot == null && Equipment != null)
        {
            DrawTooltip(_hoveredSlot.Value);
        }
    }
    
    private void DrawSlot(EquipmentSlot slot, Rectangle rect)
    {
        var isHovered = _hoveredSlot == slot;
        var bgColor = isHovered ? new Color(60, 60, 80) : new Color(40, 40, 50);
        var borderColor = isHovered ? Color.CornflowerBlue : AssetManager.BorderColor;
        
        _ui.DrawRectangle(rect, bgColor);
        _ui.DrawRectangleOutline(rect, borderColor);
        
        // Draw item or empty slot indicator
        if (Equipment != null)
        {
            var item = Equipment[slot];
            if (item != null)
            {
                // Item icon placeholder (would use actual sprites)
                _ui.DrawTextCentered(GetSlotIcon(slot), 
                    new Vector2(rect.X + rect.Width / 2, rect.Y + rect.Height / 2 - 8), 
                    GetRarityColor(item.Definition?.Rarity ?? ItemRarity.Common), 2f);
                
                // Item name (if space)
                if (rect.Height > 50)
                {
                    var name = item.Name.Length > 8 ? item.Name[..8] + ".." : item.Name;
                    _ui.DrawTextCentered(name, 
                        new Vector2(rect.X + rect.Width / 2, rect.Bottom - 15), 
                        Color.White, 1f);
                }
            }
            else
            {
                // Empty slot indicator
                _ui.DrawTextCentered(GetSlotIcon(slot), 
                    new Vector2(rect.X + rect.Width / 2, rect.Y + rect.Height / 2 - 8), 
                    new Color(60, 60, 70), 1.5f);
            }
        }
    }
    
    private void DrawTooltip(EquipmentSlot slot)
    {
        var item = Equipment?[slot];
        
        var mousePos = _input.MousePosition;
        var tooltipWidth = 200;
        var tooltipHeight = item != null ? 150 : 60;
        
        var tooltipX = (int)mousePos.X + 15;
        var tooltipY = (int)mousePos.Y + 15;
        
        // Keep on screen
        if (tooltipX + tooltipWidth > 1280) tooltipX = (int)mousePos.X - tooltipWidth - 15;
        if (tooltipY + tooltipHeight > 720) tooltipY = 720 - tooltipHeight;
        
        var tooltipRect = new Rectangle(tooltipX, tooltipY, tooltipWidth, tooltipHeight);
        
        _ui.DrawRectangle(tooltipRect, new Color(30, 30, 40, 240));
        _ui.DrawRectangleOutline(tooltipRect, AssetManager.BorderColor);
        
        if (item != null)
        {
            var y = tooltipY + 10;
            
            // Item name
            _ui.DrawText(item.Name, new Vector2(tooltipX + 10, y), 
                GetRarityColor(item.Definition?.Rarity ?? ItemRarity.Common), 1.5f);
            y += 20;
            
            // Slot type
            _ui.DrawText($"[{slot}]", new Vector2(tooltipX + 10, y), Color.Gray, 1.2f);
            y += 18;
            
            // Stats
            if (item.Definition != null)
            {
                var def = item.Definition;
                
                if (def.Armor > 0)
                {
                    _ui.DrawText($"Armor: {def.Armor}", new Vector2(tooltipX + 10, y), Color.White, 1.2f);
                    y += 15;
                }
                
                if (def.MinDamage > 0)
                {
                    _ui.DrawText($"Damage: {def.MinDamage}-{def.MaxDamage}", new Vector2(tooltipX + 10, y), Color.White, 1.2f);
                    y += 15;
                }
                
                if (def.BonusStrength > 0)
                {
                    _ui.DrawText($"+{def.BonusStrength} Strength", new Vector2(tooltipX + 10, y), Color.Green, 1.2f);
                    y += 15;
                }
                
                if (def.BonusDexterity > 0)
                {
                    _ui.DrawText($"+{def.BonusDexterity} Dexterity", new Vector2(tooltipX + 10, y), Color.Green, 1.2f);
                    y += 15;
                }
                
                if (def.BonusIntelligence > 0)
                {
                    _ui.DrawText($"+{def.BonusIntelligence} Intelligence", new Vector2(tooltipX + 10, y), Color.Green, 1.2f);
                    y += 15;
                }
            }
            
            // Durability
            _ui.DrawText($"Durability: {item.Durability}/{item.MaxDurability}", 
                new Vector2(tooltipX + 10, tooltipRect.Bottom - 20), Color.Gray, 1f);
        }
        else
        {
            _ui.DrawText(GetSlotName(slot), new Vector2(tooltipX + 10, tooltipY + 10), Color.White, 1.5f);
            _ui.DrawText("(Empty)", new Vector2(tooltipX + 10, tooltipY + 30), Color.Gray, 1.2f);
        }
    }
    
    private static string GetSlotIcon(EquipmentSlot slot) => slot switch
    {
        EquipmentSlot.Head => "⛑",
        EquipmentSlot.Neck => "◎",
        EquipmentSlot.Chest => "▣",
        EquipmentSlot.Back => "◈",
        EquipmentSlot.Arms => "◱",
        EquipmentSlot.Hands => "✋",
        EquipmentSlot.Waist => "═",
        EquipmentSlot.Legs => "▥",
        EquipmentSlot.Feet => "⊡",
        EquipmentSlot.Ring1 => "○",
        EquipmentSlot.Ring2 => "○",
        EquipmentSlot.MainHand => "⚔",
        EquipmentSlot.OffHand => "🛡",
        EquipmentSlot.Ammo => "➳",
        _ => "?"
    };
    
    private static string GetSlotName(EquipmentSlot slot) => slot switch
    {
        EquipmentSlot.Head => "Head",
        EquipmentSlot.Neck => "Neck",
        EquipmentSlot.Chest => "Chest",
        EquipmentSlot.Back => "Back",
        EquipmentSlot.Arms => "Arms",
        EquipmentSlot.Hands => "Hands",
        EquipmentSlot.Waist => "Waist",
        EquipmentSlot.Legs => "Legs",
        EquipmentSlot.Feet => "Feet",
        EquipmentSlot.Ring1 => "Ring (Left)",
        EquipmentSlot.Ring2 => "Ring (Right)",
        EquipmentSlot.MainHand => "Main Hand",
        EquipmentSlot.OffHand => "Off Hand",
        EquipmentSlot.Ammo => "Ammo",
        _ => slot.ToString()
    };
    
    private static Color GetRarityColor(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => Color.White,
        ItemRarity.Uncommon => Color.LightGreen,
        ItemRarity.Rare => Color.CornflowerBlue,
        ItemRarity.Epic => Color.MediumPurple,
        ItemRarity.Legendary => Color.Orange,
        ItemRarity.Artifact => Color.Gold,
        _ => Color.White
    };
}
