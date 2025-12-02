using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RealmOfReality.Client.Engine;
using RealmOfReality.Shared.Skills;

namespace RealmOfReality.Client.UI;

/// <summary>
/// Skills panel showing player's skill levels
/// </summary>
public class SkillsPanel
{
    private readonly UIRenderer _ui;
    private readonly InputManager _input;
    
    private Rectangle _panelRect;
    private bool _isVisible = false;
    private bool _isDragging = false;
    private Vector2 _dragOffset;
    private int _scrollOffset = 0;
    
    // Skill data (would come from GameState)
    private readonly List<SkillDisplay> _skills = new();
    
    public SkillsPanel(UIRenderer ui, InputManager input)
    {
        _ui = ui;
        _input = input;
        _panelRect = new Rectangle(500, 100, 280, 450);
        
        // Initialize with sample skills
        InitializeSampleSkills();
    }
    
    private void InitializeSampleSkills()
    {
        _skills.Clear();
        _skills.Add(new SkillDisplay("Swordsmanship", "Combat", 450, 1000));
        _skills.Add(new SkillDisplay("Tactics", "Combat", 380, 1000));
        _skills.Add(new SkillDisplay("Anatomy", "Combat", 250, 1000));
        _skills.Add(new SkillDisplay("Healing", "Combat", 420, 1000));
        _skills.Add(new SkillDisplay("Parrying", "Combat", 180, 1000));
        _skills.Add(new SkillDisplay("Magery", "Magic", 520, 1000));
        _skills.Add(new SkillDisplay("Eval Intelligence", "Magic", 350, 1000));
        _skills.Add(new SkillDisplay("Meditation", "Magic", 400, 1000));
        _skills.Add(new SkillDisplay("Magic Resist", "Magic", 280, 1000));
        _skills.Add(new SkillDisplay("Blacksmithy", "Crafting", 650, 1000));
        _skills.Add(new SkillDisplay("Mining", "Gathering", 480, 1000));
        _skills.Add(new SkillDisplay("Hiding", "Misc", 320, 1000));
        _skills.Add(new SkillDisplay("Stealth", "Misc", 150, 1000));
    }
    
    public void Show() => _isVisible = true;
    public void Hide() => _isVisible = false;
    public void Toggle() { if (_isVisible) Hide(); else Show(); }
    public bool IsVisible => _isVisible;
    public bool IsMouseOver => _isVisible && _ui.IsInside(_panelRect, _input.MousePosition);
    
    public void Update(GameTime gameTime)
    {
        if (!_isVisible) return;
        
        var mousePos = _input.MousePosition;
        
        // Title bar dragging
        var titleBar = new Rectangle(_panelRect.X, _panelRect.Y, _panelRect.Width, 25);
        
        if (_input.IsLeftMousePressed && _ui.IsInside(titleBar, mousePos))
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
        
        // Scroll
        if (IsMouseOver && _input.ScrollWheelDelta != 0)
        {
            _scrollOffset -= _input.ScrollWheelDelta / 60;
            _scrollOffset = Math.Clamp(_scrollOffset, 0, Math.Max(0, _skills.Count - 10));
        }
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
        _ui.DrawText("SKILLS", new Vector2(_panelRect.X + 10, _panelRect.Y + 5), Color.White, 1.5f);
        
        // Close button
        var closeBtn = new Rectangle(_panelRect.Right - 25, _panelRect.Y + 3, 20, 20);
        var closeHover = _ui.IsInside(closeBtn, mousePos);
        _ui.DrawRectangle(closeBtn, closeHover ? new Color(150, 50, 50) : new Color(100, 50, 50));
        _ui.DrawTextCentered("X", new Vector2(closeBtn.X + 10, closeBtn.Y + 3), Color.White, 1.5f);
        
        // Total skill points
        var totalUsed = _skills.Sum(s => s.Value);
        var totalCap = 7000;
        _ui.DrawText($"Total: {totalUsed / 10f:F1} / {totalCap / 10f:F1}", 
            new Vector2(_panelRect.X + 10, _panelRect.Y + 30), Color.Gray, 1.3f);
        
        // Skills list
        var y = _panelRect.Y + 55;
        var visibleCount = Math.Min(_skills.Count - _scrollOffset, 12);
        
        for (int i = 0; i < visibleCount; i++)
        {
            var skillIndex = i + _scrollOffset;
            var skill = _skills[skillIndex];
            
            DrawSkillRow(skill, y, mousePos);
            y += 32;
        }
        
        // Scroll indicator
        if (_skills.Count > 12)
        {
            var scrollBarHeight = _panelRect.Height - 80;
            var scrollThumbHeight = scrollBarHeight * 12 / _skills.Count;
            var scrollThumbY = _panelRect.Y + 55 + (_scrollOffset * (scrollBarHeight - scrollThumbHeight) / Math.Max(1, _skills.Count - 12));
            
            var scrollBarRect = new Rectangle(_panelRect.Right - 12, _panelRect.Y + 55, 8, scrollBarHeight);
            var scrollThumbRect = new Rectangle(_panelRect.Right - 12, (int)scrollThumbY, 8, (int)scrollThumbHeight);
            
            _ui.DrawRectangle(scrollBarRect, new Color(30, 30, 40));
            _ui.DrawRectangle(scrollThumbRect, new Color(80, 80, 100));
        }
    }
    
    private void DrawSkillRow(SkillDisplay skill, int y, Vector2 mousePos)
    {
        var rowRect = new Rectangle(_panelRect.X + 10, y, _panelRect.Width - 30, 28);
        var isHovered = _ui.IsInside(rowRect, mousePos);
        
        if (isHovered)
        {
            _ui.DrawRectangle(rowRect, new Color(50, 50, 60));
        }
        
        // Skill name
        var categoryColor = skill.Category switch
        {
            "Combat" => new Color(200, 100, 100),
            "Magic" => new Color(100, 100, 200),
            "Crafting" => new Color(200, 150, 100),
            "Gathering" => new Color(100, 200, 100),
            _ => Color.Gray
        };
        
        _ui.DrawText(skill.Name, new Vector2(rowRect.X + 5, y + 2), Color.White, 1.3f);
        
        // Skill value
        var valueText = $"{skill.Value / 10f:F1}";
        _ui.DrawText(valueText, new Vector2(rowRect.Right - 50, y + 2), categoryColor, 1.3f);
        
        // Progress bar
        var barRect = new Rectangle(rowRect.X + 5, y + 18, rowRect.Width - 60, 6);
        var progress = (float)skill.Value / skill.Cap;
        
        _ui.DrawRectangle(barRect, new Color(30, 30, 40));
        _ui.DrawRectangle(new Rectangle(barRect.X, barRect.Y, (int)(barRect.Width * progress), barRect.Height), categoryColor);
    }
    
    private class SkillDisplay
    {
        public string Name { get; }
        public string Category { get; }
        public int Value { get; set; }
        public int Cap { get; }
        
        public SkillDisplay(string name, string category, int value, int cap)
        {
            Name = name;
            Category = category;
            Value = value;
            Cap = cap;
        }
    }
}
