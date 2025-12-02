using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RealmOfReality.Client.Engine;

namespace RealmOfReality.Client.UI;

/// <summary>
/// Spellbook panel showing known spells
/// </summary>
public class SpellbookPanel
{
    private readonly UIRenderer _ui;
    private readonly InputManager _input;
    
    private Rectangle _panelRect;
    private bool _isVisible = false;
    private bool _isDragging = false;
    private Vector2 _dragOffset;
    private int _currentCircle = 1;
    
    // Spell dragging
    private int _draggedSpell = -1;
    private Vector2 _dragPosition;
    
    // Events
    public event Action<ushort>? OnSpellDragStart;
    public event Action<ushort>? OnSpellCast;
    
    // Sample spells
    private readonly List<SpellDisplay>[] _spellsByCircle = new List<SpellDisplay>[8];
    
    public SpellbookPanel(UIRenderer ui, InputManager input)
    {
        _ui = ui;
        _input = input;
        _panelRect = new Rectangle(350, 120, 320, 380);
        
        InitializeSampleSpells();
    }
    
    private void InitializeSampleSpells()
    {
        for (int i = 0; i < 8; i++)
            _spellsByCircle[i] = new List<SpellDisplay>();
        
        // Circle 1
        _spellsByCircle[0].Add(new SpellDisplay(1, "Clumsy", "Vas Corp Por", 4));
        _spellsByCircle[0].Add(new SpellDisplay(2, "Create Food", "In Mani Ylem", 4));
        _spellsByCircle[0].Add(new SpellDisplay(3, "Feeblemind", "Rel Wis", 4));
        _spellsByCircle[0].Add(new SpellDisplay(4, "Heal", "In Mani", 4));
        _spellsByCircle[0].Add(new SpellDisplay(5, "Magic Arrow", "In Por Ylem", 4));
        _spellsByCircle[0].Add(new SpellDisplay(6, "Night Sight", "In Lor", 4));
        _spellsByCircle[0].Add(new SpellDisplay(7, "Reactive Armor", "Flam Sanct", 4));
        _spellsByCircle[0].Add(new SpellDisplay(8, "Weaken", "Des Mani", 4));
        
        // Circle 2
        _spellsByCircle[1].Add(new SpellDisplay(9, "Agility", "Ex Uus", 6));
        _spellsByCircle[1].Add(new SpellDisplay(10, "Cunning", "Uus Wis", 6));
        _spellsByCircle[1].Add(new SpellDisplay(11, "Cure", "An Nox", 6));
        _spellsByCircle[1].Add(new SpellDisplay(12, "Harm", "An Mani", 6));
        _spellsByCircle[1].Add(new SpellDisplay(13, "Magic Trap", "In Jux", 6));
        _spellsByCircle[1].Add(new SpellDisplay(14, "Magic Untrap", "An Jux", 6));
        _spellsByCircle[1].Add(new SpellDisplay(15, "Protection", "Uus Sanct", 6));
        _spellsByCircle[1].Add(new SpellDisplay(16, "Strength", "Uus Mani", 6));
        
        // Circle 3
        _spellsByCircle[2].Add(new SpellDisplay(17, "Bless", "Rel Sanct", 9));
        _spellsByCircle[2].Add(new SpellDisplay(18, "Fireball", "Vas Flam", 9));
        _spellsByCircle[2].Add(new SpellDisplay(19, "Magic Lock", "An Por", 9));
        _spellsByCircle[2].Add(new SpellDisplay(20, "Poison", "In Nox", 9));
        _spellsByCircle[2].Add(new SpellDisplay(21, "Telekinesis", "Ort Por Ylem", 9));
        _spellsByCircle[2].Add(new SpellDisplay(22, "Teleport", "Rel Por", 9));
        _spellsByCircle[2].Add(new SpellDisplay(23, "Unlock", "Ex Por", 9));
        _spellsByCircle[2].Add(new SpellDisplay(24, "Wall of Stone", "In Sanct Ylem", 9));
        
        // Circle 4
        _spellsByCircle[3].Add(new SpellDisplay(25, "Arch Cure", "Vas An Nox", 11));
        _spellsByCircle[3].Add(new SpellDisplay(26, "Arch Protection", "Vas Uus Sanct", 11));
        _spellsByCircle[3].Add(new SpellDisplay(27, "Curse", "Des Sanct", 11));
        _spellsByCircle[3].Add(new SpellDisplay(28, "Fire Field", "In Flam Grav", 11));
        _spellsByCircle[3].Add(new SpellDisplay(29, "Greater Heal", "In Vas Mani", 11));
        _spellsByCircle[3].Add(new SpellDisplay(30, "Lightning", "Por Ort Grav", 11));
        _spellsByCircle[3].Add(new SpellDisplay(31, "Mana Drain", "Ort Rel", 11));
        _spellsByCircle[3].Add(new SpellDisplay(32, "Recall", "Kal Ort Por", 11));
        
        // Add some higher circle spells
        _spellsByCircle[4].Add(new SpellDisplay(33, "Blade Spirits", "In Jux Hur Ylem", 14));
        _spellsByCircle[4].Add(new SpellDisplay(34, "Dispel Field", "An Grav", 14));
        _spellsByCircle[4].Add(new SpellDisplay(35, "Incognito", "Kal In Ex", 14));
        _spellsByCircle[4].Add(new SpellDisplay(36, "Magic Reflection", "In Jux Sanct", 14));
        
        _spellsByCircle[5].Add(new SpellDisplay(41, "Dispel", "An Ort", 20));
        _spellsByCircle[5].Add(new SpellDisplay(42, "Energy Bolt", "Corp Por", 20));
        _spellsByCircle[5].Add(new SpellDisplay(43, "Explosion", "Vas Ort Flam", 20));
        _spellsByCircle[5].Add(new SpellDisplay(44, "Invisibility", "An Lor Xen", 20));
        
        _spellsByCircle[6].Add(new SpellDisplay(49, "Chain Lightning", "Vas Ort Grav", 40));
        _spellsByCircle[6].Add(new SpellDisplay(50, "Energy Field", "In Sanct Grav", 40));
        _spellsByCircle[6].Add(new SpellDisplay(51, "Flamestrike", "Kal Vas Flam", 40));
        
        _spellsByCircle[7].Add(new SpellDisplay(57, "Earthquake", "In Vas Por", 50));
        _spellsByCircle[7].Add(new SpellDisplay(58, "Energy Vortex", "Vas Corp Por", 50));
        _spellsByCircle[7].Add(new SpellDisplay(59, "Resurrection", "An Corp", 50));
        _spellsByCircle[7].Add(new SpellDisplay(60, "Summon Air Elemental", "Kal Vas Xen Hur", 50));
    }
    
    public void Show() => _isVisible = true;
    public void Hide() { _isVisible = false; _draggedSpell = -1; }
    public void Toggle() { if (_isVisible) Hide(); else Show(); }
    public bool IsVisible => _isVisible;
    public bool IsMouseOver => _isVisible && _ui.IsInside(_panelRect, _input.MousePosition);
    
    public void Update(GameTime gameTime)
    {
        if (!_isVisible) return;
        
        var mousePos = _input.MousePosition;
        
        // Title bar dragging
        var titleBar = new Rectangle(_panelRect.X, _panelRect.Y, _panelRect.Width, 25);
        
        if (_input.IsLeftMousePressed && _ui.IsInside(titleBar, mousePos) && _draggedSpell < 0)
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
        
        // Circle tabs
        for (int i = 0; i < 8; i++)
        {
            var tabRect = new Rectangle(_panelRect.X + 10 + i * 37, _panelRect.Y + 30, 35, 25);
            if (_input.IsLeftMousePressed && _ui.IsInside(tabRect, mousePos))
            {
                _currentCircle = i + 1;
            }
        }
        
        // Spell interaction
        var spells = _spellsByCircle[_currentCircle - 1];
        var hoveredSpell = GetSpellAt(mousePos, spells);
        
        if (_input.IsLeftMousePressed && hoveredSpell >= 0)
        {
            _draggedSpell = hoveredSpell;
            _dragPosition = mousePos;
            OnSpellDragStart?.Invoke((ushort)spells[hoveredSpell].Id);
        }
        
        if (_draggedSpell >= 0)
        {
            if (_input.IsLeftMouseDown)
            {
                _dragPosition = mousePos;
            }
            else
            {
                _draggedSpell = -1;
            }
        }
        
        // Right-click to cast
        if (_input.IsRightMousePressed && hoveredSpell >= 0)
        {
            OnSpellCast?.Invoke((ushort)spells[hoveredSpell].Id);
        }
    }
    
    private int GetSpellAt(Vector2 mousePos, List<SpellDisplay> spells)
    {
        var startY = _panelRect.Y + 60;
        var rowHeight = 36;
        
        for (int i = 0; i < spells.Count; i++)
        {
            var rowRect = new Rectangle(_panelRect.X + 10, startY + i * rowHeight, _panelRect.Width - 20, rowHeight - 2);
            if (_ui.IsInside(rowRect, mousePos))
            {
                return i;
            }
        }
        
        return -1;
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
        _ui.DrawText("SPELLBOOK", new Vector2(_panelRect.X + 10, _panelRect.Y + 5), Color.White, 1.5f);
        
        // Close button
        var closeBtn = new Rectangle(_panelRect.Right - 25, _panelRect.Y + 3, 20, 20);
        var closeHover = _ui.IsInside(closeBtn, mousePos);
        _ui.DrawRectangle(closeBtn, closeHover ? new Color(150, 50, 50) : new Color(100, 50, 50));
        _ui.DrawTextCentered("X", new Vector2(closeBtn.X + 10, closeBtn.Y + 3), Color.White, 1.5f);
        
        // Circle tabs
        for (int i = 0; i < 8; i++)
        {
            var tabRect = new Rectangle(_panelRect.X + 10 + i * 37, _panelRect.Y + 30, 35, 25);
            var isCurrentCircle = i + 1 == _currentCircle;
            var tabHover = _ui.IsInside(tabRect, mousePos);
            
            var tabColor = isCurrentCircle ? new Color(70, 70, 100) : (tabHover ? new Color(55, 55, 75) : new Color(45, 45, 60));
            _ui.DrawRectangle(tabRect, tabColor);
            _ui.DrawRectangleOutline(tabRect, isCurrentCircle ? Color.CornflowerBlue : AssetManager.BorderColor);
            _ui.DrawTextCentered((i + 1).ToString(), new Vector2(tabRect.X + 17, tabRect.Y + 5), 
                isCurrentCircle ? Color.White : Color.Gray, 1.4f);
        }
        
        // Spells
        var spells = _spellsByCircle[_currentCircle - 1];
        var startY = _panelRect.Y + 60;
        var rowHeight = 36;
        
        for (int i = 0; i < spells.Count; i++)
        {
            if (_draggedSpell == i) continue;
            
            var spell = spells[i];
            var rowRect = new Rectangle(_panelRect.X + 10, startY + i * rowHeight, _panelRect.Width - 20, rowHeight - 2);
            DrawSpellRow(spell, rowRect, mousePos);
        }
        
        // Draw dragged spell
        if (_draggedSpell >= 0 && _draggedSpell < spells.Count)
        {
            var spell = spells[_draggedSpell];
            var dragRect = new Rectangle((int)_dragPosition.X - 20, (int)_dragPosition.Y - 15, 40, 30);
            
            _ui.DrawRectangle(dragRect, new Color(60, 80, 120, 200));
            _ui.DrawRectangleOutline(dragRect, Color.Yellow);
            _ui.DrawTextCentered("✦", new Vector2(dragRect.X + 20, dragRect.Y + 5), Color.Cyan, 2f);
        }
        
        // Tooltip
        var hoveredSpell = GetSpellAt(mousePos, spells);
        if (hoveredSpell >= 0 && _draggedSpell < 0)
        {
            DrawSpellTooltip(spells[hoveredSpell], mousePos);
        }
    }
    
    private void DrawSpellRow(SpellDisplay spell, Rectangle rect, Vector2 mousePos)
    {
        var isHovered = _ui.IsInside(rect, mousePos);
        
        if (isHovered)
        {
            _ui.DrawRectangle(rect, new Color(50, 60, 80));
        }
        
        // Spell icon
        _ui.DrawTextCentered("✦", new Vector2(rect.X + 15, rect.Y + 8), Color.Cyan, 1.8f);
        
        // Spell name
        _ui.DrawText(spell.Name, new Vector2(rect.X + 35, rect.Y + 3), Color.White, 1.3f);
        
        // Mana cost
        _ui.DrawText($"{spell.ManaCost} MP", new Vector2(rect.Right - 50, rect.Y + 3), Color.CornflowerBlue, 1.1f);
        
        // Words of power
        _ui.DrawText(spell.Words, new Vector2(rect.X + 35, rect.Y + 18), Color.Gray, 1f);
    }
    
    private void DrawSpellTooltip(SpellDisplay spell, Vector2 mousePos)
    {
        var tooltipWidth = 200;
        var tooltipHeight = 90;
        var tooltipX = (int)mousePos.X + 15;
        var tooltipY = (int)mousePos.Y + 15;
        
        if (tooltipX + tooltipWidth > 1280) tooltipX = (int)mousePos.X - tooltipWidth - 15;
        if (tooltipY + tooltipHeight > 720) tooltipY = 720 - tooltipHeight;
        
        var tooltipRect = new Rectangle(tooltipX, tooltipY, tooltipWidth, tooltipHeight);
        
        _ui.DrawRectangle(tooltipRect, new Color(30, 30, 40, 245));
        _ui.DrawRectangleOutline(tooltipRect, AssetManager.BorderColor);
        
        var y = tooltipY + 8;
        _ui.DrawText(spell.Name, new Vector2(tooltipX + 8, y), Color.Cyan, 1.5f);
        y += 18;
        
        _ui.DrawText($"Circle {_currentCircle}", new Vector2(tooltipX + 8, y), Color.Gray, 1.2f);
        y += 15;
        
        _ui.DrawText($"Mana Cost: {spell.ManaCost}", new Vector2(tooltipX + 8, y), Color.CornflowerBlue, 1.2f);
        y += 15;
        
        _ui.DrawText("Right-click to cast", new Vector2(tooltipX + 8, y), Color.Gray, 1f);
        _ui.DrawText("Drag to hotbar", new Vector2(tooltipX + 8, y + 12), Color.Gray, 1f);
    }
    
    private class SpellDisplay
    {
        public int Id { get; }
        public string Name { get; }
        public string Words { get; }
        public int ManaCost { get; }
        
        public SpellDisplay(int id, string name, string words, int manaCost)
        {
            Id = id;
            Name = name;
            Words = words;
            ManaCost = manaCost;
        }
    }
}
