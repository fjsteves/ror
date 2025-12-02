using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RealmOfReality.Client.Engine;
using RealmOfReality.Shared.Items;

#pragma warning disable CS0067 // Events are declared for future use

namespace RealmOfReality.Client.UI;

/// <summary>
/// Manages all drag-drop operations for items across UI panels
/// </summary>
public class DragDropManager
{
    private readonly UIRenderer _ui;
    private readonly InputManager _input;
    private readonly AssetManager _assets;
    
    // Drag state
    private bool _isDragging = false;
    private DragSource _source;
    private Item? _draggedItem;
    private int _sourceSlot = -1;
    private EquipmentSlot _sourceEquipSlot;
    private Vector2 _dragPosition;
    private Vector2 _dragOffset;
    
    // Drop targets
    private readonly List<IDropTarget> _dropTargets = new();
    private IDropTarget? _hoveredTarget;
    
    // Visual feedback
    private bool _showInvalidFeedback = false;
    private string _invalidReason = "";
    private double _invalidFeedbackTime = 0;
    private const double InvalidFeedbackDuration = 1.5;
    
    // Events
    public event Action<DragSource, int, DragSource, int>? OnItemMoved;
    public event Action<int, EquipmentSlot>? OnItemEquipped;
    public event Action<EquipmentSlot, int>? OnItemUnequipped;
    public event Action<int>? OnItemDroppedToWorld;
    
    public bool IsDragging => _isDragging;
    public Item? DraggedItem => _draggedItem;
    
    public DragDropManager(UIRenderer ui, InputManager input, AssetManager assets)
    {
        _ui = ui;
        _input = input;
        _assets = assets;
    }
    
    public void RegisterDropTarget(IDropTarget target)
    {
        if (!_dropTargets.Contains(target))
            _dropTargets.Add(target);
    }
    
    public void UnregisterDropTarget(IDropTarget target)
    {
        _dropTargets.Remove(target);
    }
    
    /// <summary>
    /// Start dragging an item from inventory
    /// </summary>
    public void StartDragFromInventory(int slot, Item item, Vector2 mousePos)
    {
        _isDragging = true;
        _source = DragSource.Inventory;
        _sourceSlot = slot;
        _draggedItem = item;
        _dragPosition = mousePos;
        _dragOffset = Vector2.Zero;
    }
    
    /// <summary>
    /// Start dragging an item from equipment
    /// </summary>
    public void StartDragFromEquipment(EquipmentSlot slot, Item item, Vector2 mousePos)
    {
        _isDragging = true;
        _source = DragSource.Equipment;
        _sourceEquipSlot = slot;
        _draggedItem = item;
        _dragPosition = mousePos;
        _dragOffset = Vector2.Zero;
    }
    
    public void Update(GameTime gameTime)
    {
        // Update invalid feedback timer
        if (_showInvalidFeedback)
        {
            _invalidFeedbackTime -= gameTime.ElapsedGameTime.TotalSeconds;
            if (_invalidFeedbackTime <= 0)
            {
                _showInvalidFeedback = false;
            }
        }
        
        if (!_isDragging) return;
        
        var mousePos = _input.MousePosition;
        _dragPosition = mousePos;
        
        // Check hovered drop target
        _hoveredTarget = null;
        foreach (var target in _dropTargets)
        {
            if (target.IsVisible && target.CanAcceptDrop(_draggedItem))
            {
                var bounds = target.GetDropBounds();
                if (_ui.IsInside(bounds, mousePos))
                {
                    _hoveredTarget = target;
                    break;
                }
            }
        }
        
        // Handle drop on mouse release
        if (!_input.IsLeftMouseDown)
        {
            HandleDrop(mousePos);
            _isDragging = false;
            _draggedItem = null;
        }
    }
    
    private void HandleDrop(Vector2 mousePos)
    {
        if (_draggedItem == null) return;
        
        // Check if dropped on a valid target
        if (_hoveredTarget != null)
        {
            var result = _hoveredTarget.HandleDrop(_source, _sourceSlot, _sourceEquipSlot, _draggedItem, mousePos);
            if (!result.Success)
            {
                ShowInvalidFeedback(result.Reason);
            }
        }
        else
        {
            // Dropped outside any panel - drop to world
            if (_source == DragSource.Inventory)
            {
                OnItemDroppedToWorld?.Invoke(_sourceSlot);
            }
            else if (_source == DragSource.Equipment)
            {
                // Unequip to inventory first, then optionally drop
                OnItemUnequipped?.Invoke(_sourceEquipSlot, -1);
            }
        }
    }
    
    public void ShowInvalidFeedback(string reason)
    {
        _showInvalidFeedback = true;
        _invalidReason = reason;
        _invalidFeedbackTime = InvalidFeedbackDuration;
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        // Draw dragged item
        if (_isDragging && _draggedItem != null)
        {
            var rect = new Rectangle(
                (int)_dragPosition.X - 20,
                (int)_dragPosition.Y - 20,
                40, 40
            );
            
            // Background
            var bgColor = _hoveredTarget != null 
                ? new Color(60, 120, 60, 220) 
                : new Color(60, 60, 80, 220);
            _ui.DrawRectangle(rect, bgColor);
            
            // Border
            var borderColor = _hoveredTarget != null ? Color.LightGreen : Color.CornflowerBlue;
            _ui.DrawRectangleOutline(rect, borderColor);
            
            // Item icon/name
            var rarity = _draggedItem.Definition?.Rarity ?? ItemRarity.Common;
            var itemColor = GetRarityColor(rarity);
            _ui.DrawTextCentered("▪", new Vector2(rect.X + 20, rect.Y + 12), itemColor, 2f);
            
            // Item name below cursor
            var namePos = new Vector2(_dragPosition.X - 40, _dragPosition.Y + 25);
            _ui.DrawText(_draggedItem.Name, namePos, Color.White, 1.2f);
            
            // Stack count
            if (_draggedItem.Amount > 1)
            {
                _ui.DrawText(_draggedItem.Amount.ToString(), 
                    new Vector2(rect.Right - 12, rect.Bottom - 12), Color.White, 1f);
            }
        }
        
        // Draw invalid feedback
        if (_showInvalidFeedback)
        {
            var alpha = (float)Math.Min(1.0, _invalidFeedbackTime / 0.5);
            var feedbackColor = new Color(200, 50, 50, (int)(alpha * 255));
            
            var screenCenter = new Vector2(640, 400);
            _ui.DrawTextCentered(_invalidReason, screenCenter, feedbackColor, 1.8f);
        }
    }
    
    private static Color GetRarityColor(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => Color.White,
        ItemRarity.Uncommon => new Color(100, 200, 100),
        ItemRarity.Rare => new Color(100, 150, 255),
        ItemRarity.Epic => new Color(180, 100, 255),
        ItemRarity.Legendary => new Color(255, 180, 50),
        ItemRarity.Artifact => new Color(255, 215, 0),
        _ => Color.White
    };
    
    public void CancelDrag()
    {
        _isDragging = false;
        _draggedItem = null;
        _hoveredTarget = null;
    }
}

public enum DragSource
{
    None,
    Inventory,
    Equipment,
    Hotbar,
    World,
    Container
}

public struct DropResult
{
    public bool Success;
    public string Reason;
    
    public static DropResult Ok() => new() { Success = true };
    public static DropResult Fail(string reason) => new() { Success = false, Reason = reason };
}

public interface IDropTarget
{
    bool IsVisible { get; }
    Rectangle GetDropBounds();
    bool CanAcceptDrop(Item? item);
    DropResult HandleDrop(DragSource source, int sourceSlot, EquipmentSlot sourceEquipSlot, Item? item, Vector2 mousePos);
}
