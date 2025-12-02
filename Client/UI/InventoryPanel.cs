using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RealmOfReality.Client.Engine;
using RealmOfReality.Shared.Items;

namespace RealmOfReality.Client.UI;

/// <summary>
/// Inventory panel showing player's items
/// Implements IDropTarget for drag-and-drop from paperdoll
/// </summary>
public class InventoryPanel : IDropTarget
{
    private readonly UIRenderer _ui;
    private readonly InputManager _input;
    private readonly AssetManager _assets;
    
    private Rectangle _panelRect;
    private bool _isVisible = false;
    private bool _isDragging = false;
    private Vector2 _dragOffset;
    
    // Inventory data
    public Inventory? Inventory { get; set; }
    public Equipment? Equipment { get; set; } // For comparison tooltips
    
    // Dragging
    private int _draggedSlot = -1;
    private Vector2 _dragPosition;
    private int _hoveredSlot = -1;
    
    // Grid settings
    private const int Columns = 8;
    private const int Rows = 5;
    private const int SlotSize = 40;
    private const int SlotPadding = 4;
    
    // Events
    public event Action<int, Item>? OnItemDragStart;
    public event Action<int>? OnItemUse;
    public event Action<int>? OnItemDrop;
    public event Action<int, int>? OnItemSwap;
    public event Action<int, EquipmentSlot>? OnEquipFromInventory;
    
    // Drag-drop manager reference
    private DragDropManager? _dragDropManager;
    private TooltipManager? _tooltipManager;
    
    public InventoryPanel(UIRenderer ui, InputManager input, AssetManager assets)
    {
        _ui = ui;
        _input = input;
        _assets = assets;
        
        var width = Columns * (SlotSize + SlotPadding) + SlotPadding + 20;
        var height = Rows * (SlotSize + SlotPadding) + SlotPadding + 60;
        _panelRect = new Rectangle(100, 150, width, height);
    }
    
    public void SetDragDropManager(DragDropManager manager)
    {
        _dragDropManager = manager;
        manager.RegisterDropTarget(this);
    }
    
    public void SetTooltipManager(TooltipManager manager)
    {
        _tooltipManager = manager;
    }
    
    public void Show() => _isVisible = true;
    public void Hide() { _isVisible = false; _draggedSlot = -1; }
    public void Toggle() { if (_isVisible) Hide(); else Show(); }
    public bool IsVisible => _isVisible;
    public bool IsMouseOver => _isVisible && _ui.IsInside(_panelRect, _input.MousePosition);
    
    #region IDropTarget Implementation
    
    public Rectangle GetDropBounds() => _panelRect;
    
    public bool CanAcceptDrop(Item? item)
    {
        // Inventory can accept any item
        return item != null;
    }
    
    public DropResult HandleDrop(DragSource source, int sourceSlot, EquipmentSlot sourceEquipSlot, Item? item, Vector2 mousePos)
    {
        if (item == null) return DropResult.Fail("No item to drop");
        if (Inventory == null) return DropResult.Fail("No inventory");
        
        var targetSlot = GetSlotAt(mousePos);
        
        if (source == DragSource.Equipment)
        {
            // Unequipping - find first empty slot or use hovered slot
            if (targetSlot >= 0 && targetSlot < Inventory.Capacity)
            {
                if (Inventory[targetSlot] == null)
                {
                    // Place in specific slot
                    return DropResult.Ok();
                }
            }
            
            // Find first empty slot
            for (int i = 0; i < Inventory.Capacity; i++)
            {
                if (Inventory[i] == null)
                {
                    return DropResult.Ok();
                }
            }
            
            return DropResult.Fail("Inventory is full!");
        }
        
        return DropResult.Ok();
    }
    
    #endregion
    
    public void Update(GameTime gameTime)
    {
        if (!_isVisible) return;
        
        var mousePos = _input.MousePosition;
        _hoveredSlot = GetSlotAt(mousePos);
        
        // Update tooltip
        if (_tooltipManager != null && _hoveredSlot >= 0 && _draggedSlot < 0 && Inventory != null)
        {
            var item = Inventory[_hoveredSlot];
            _tooltipManager.SetItemTooltip(item, Equipment);
        }
        else if (_tooltipManager != null && !IsMouseOver)
        {
            _tooltipManager.SetTooltip(null);
        }
        
        // Title bar dragging
        var titleBar = new Rectangle(_panelRect.X, _panelRect.Y, _panelRect.Width, 25);
        
        if (_input.IsLeftMousePressed && _ui.IsInside(titleBar, mousePos) && _draggedSlot < 0)
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
                _panelRect.X = Math.Clamp(_panelRect.X, 0, 1280 - _panelRect.Width);
                _panelRect.Y = Math.Clamp(_panelRect.Y, 0, 720 - _panelRect.Height);
            }
            else
            {
                _isDragging = false;
            }
        }
        
        // Close button
        var closeBtn = new Rectangle(_panelRect.Right - 25, _panelRect.Y + 3, 20, 20);
        if (_input.IsLeftMousePressed && _ui.IsInside(closeBtn, mousePos))
        {
            Hide();
            return;
        }
        
        // Skip item interaction if drag-drop manager is handling a drag
        if (_dragDropManager?.IsDragging == true) return;
        
        // Item interaction
        if (_input.IsLeftMousePressed && _hoveredSlot >= 0 && Inventory != null)
        {
            var item = Inventory[_hoveredSlot];
            if (item != null)
            {
                if (_dragDropManager != null)
                {
                    _dragDropManager.StartDragFromInventory(_hoveredSlot, item, mousePos);
                }
                else
                {
                    _draggedSlot = _hoveredSlot;
                    _dragPosition = mousePos;
                    OnItemDragStart?.Invoke(_hoveredSlot, item);
                }
            }
        }
        
        // Legacy drag handling (when no DragDropManager)
        if (_draggedSlot >= 0 && _dragDropManager == null)
        {
            if (_input.IsLeftMouseDown)
            {
                _dragPosition = mousePos;
            }
            else
            {
                // Drop the item
                if (!IsMouseOver)
                {
                    OnItemDrop?.Invoke(_draggedSlot);
                }
                else if (_hoveredSlot >= 0 && _hoveredSlot != _draggedSlot)
                {
                    // Swap items in inventory
                    SwapItems(_draggedSlot, _hoveredSlot);
                    OnItemSwap?.Invoke(_draggedSlot, _hoveredSlot);
                }
                _draggedSlot = -1;
            }
        }
        
        // Right-click to use/equip
        if (_input.IsRightMousePressed && _hoveredSlot >= 0 && Inventory != null)
        {
            var item = Inventory[_hoveredSlot];
            if (item != null)
            {
                // Check if it's equippable
                if (item.Definition != null && item.Slot != EquipmentSlot.None)
                {
                    OnEquipFromInventory?.Invoke(_hoveredSlot, item.Slot);
                }
                else
                {
                    OnItemUse?.Invoke(_hoveredSlot);
                }
            }
        }
    }
    
    private int GetSlotAt(Vector2 mousePos)
    {
        var gridX = _panelRect.X + 10;
        var gridY = _panelRect.Y + 35;
        
        var relX = (int)mousePos.X - gridX;
        var relY = (int)mousePos.Y - gridY;
        
        if (relX < 0 || relY < 0) return -1;
        
        var col = relX / (SlotSize + SlotPadding);
        var row = relY / (SlotSize + SlotPadding);
        
        if (col >= Columns || row >= Rows) return -1;
        
        // Check if actually inside slot (not padding)
        var slotX = col * (SlotSize + SlotPadding);
        var slotY = row * (SlotSize + SlotPadding);
        
        if (relX - slotX > SlotSize || relY - slotY > SlotSize) return -1;
        
        return row * Columns + col;
    }
    
    private void SwapItems(int slot1, int slot2)
    {
        if (Inventory == null) return;
        
        var item1 = Inventory[slot1];
        var item2 = Inventory[slot2];
        
        Inventory[slot1] = item2;
        Inventory[slot2] = item1;
    }
    
    public void Draw(GameTime gameTime)
    {
        if (!_isVisible) return;
        
        var mousePos = _input.MousePosition;
        
        // Panel background
        _ui.DrawPanel(_panelRect);
        
        // Title bar
        var titleBar = new Rectangle(_panelRect.X, _panelRect.Y, _panelRect.Width, 25);
        _ui.DrawRectangle(titleBar, new Color(50, 50, 70));
        _ui.DrawText("INVENTORY", new Vector2(_panelRect.X + 10, _panelRect.Y + 5), Color.White, 1.5f);
        
        // Close button
        var closeBtn = new Rectangle(_panelRect.Right - 25, _panelRect.Y + 3, 20, 20);
        var closeHover = _ui.IsInside(closeBtn, mousePos);
        _ui.DrawRectangle(closeBtn, closeHover ? new Color(150, 50, 50) : new Color(100, 50, 50));
        _ui.DrawTextCentered("X", new Vector2(closeBtn.X + 10, closeBtn.Y + 3), Color.White, 1.5f);
        
        // Gold display
        var gold = 12500; // Would come from player data
        _ui.DrawText($"Gold: {gold:N0}", new Vector2(_panelRect.X + 10, _panelRect.Bottom - 22), Color.Gold, 1.3f);
        
        // Draw slots
        var gridX = _panelRect.X + 10;
        var gridY = _panelRect.Y + 35;
        
        for (int row = 0; row < Rows; row++)
        {
            for (int col = 0; col < Columns; col++)
            {
                var slot = row * Columns + col;
                if (_draggedSlot == slot && _dragDropManager == null) continue;
                
                var slotRect = new Rectangle(
                    gridX + col * (SlotSize + SlotPadding),
                    gridY + row * (SlotSize + SlotPadding),
                    SlotSize, SlotSize
                );
                
                DrawSlot(slotRect, slot, mousePos);
            }
        }
        
        // Draw dragged item (legacy mode only)
        if (_draggedSlot >= 0 && _dragDropManager == null && Inventory != null)
        {
            var item = Inventory[_draggedSlot];
            if (item != null)
            {
                var dragRect = new Rectangle(
                    (int)_dragPosition.X - SlotSize / 2,
                    (int)_dragPosition.Y - SlotSize / 2,
                    SlotSize, SlotSize
                );
                
                _ui.DrawRectangle(dragRect, new Color(80, 80, 120, 200));
                _ui.DrawRectangleOutline(dragRect, Color.Yellow);
                _ui.DrawTextCentered("▪", new Vector2(dragRect.X + SlotSize / 2, dragRect.Y + 12), Color.White, 2f);
            }
        }
        
        // Drop highlight when something is being dragged over
        if (_dragDropManager?.IsDragging == true && IsMouseOver && _hoveredSlot >= 0)
        {
            var slotRect = GetSlotRect(_hoveredSlot);
            _ui.DrawRectangleOutline(slotRect, Color.LightGreen, 2);
        }
    }
    
    private Rectangle GetSlotRect(int slot)
    {
        int row = slot / Columns;
        int col = slot % Columns;
        var gridX = _panelRect.X + 10;
        var gridY = _panelRect.Y + 35;
        
        return new Rectangle(
            gridX + col * (SlotSize + SlotPadding),
            gridY + row * (SlotSize + SlotPadding),
            SlotSize, SlotSize
        );
    }
    
    private void DrawSlot(Rectangle rect, int slot, Vector2 mousePos)
    {
        var isHovered = _hoveredSlot == slot;
        var bgColor = isHovered ? new Color(60, 60, 80) : new Color(40, 40, 50);
        var borderColor = isHovered ? Color.CornflowerBlue : AssetManager.BorderColor;
        
        _ui.DrawRectangle(rect, bgColor);
        _ui.DrawRectangleOutline(rect, borderColor);
        
        // Draw item
        if (Inventory != null)
        {
            var item = Inventory[slot];
            if (item != null)
            {
                var rarityColor = GetRarityColor(item.Definition?.Rarity ?? ItemRarity.Common);
                _ui.DrawTextCentered("▪", new Vector2(rect.X + rect.Width / 2, rect.Y + 12), rarityColor, 2f);
                
                // Stack count
                if (item.Amount > 1)
                {
                    _ui.DrawText(item.Amount.ToString(), new Vector2(rect.Right - 15, rect.Bottom - 15), Color.White, 1f);
                }
                
                // Durability indicator for low durability
                if (item.MaxDurability > 0)
                {
                    var durPercent = 100f * item.Durability / item.MaxDurability;
                    if (durPercent < 25)
                    {
                        _ui.DrawRectangle(new Rectangle(rect.X, rect.Bottom - 3, (int)(rect.Width * durPercent / 100), 3), 
                            new Color(255, 100, 50));
                    }
                }
            }
        }
    }
    
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
