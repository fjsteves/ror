using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RealmOfReality.Client.Engine;

namespace RealmOfReality.Client.UI;

/// <summary>
/// Settings panel for game options
/// </summary>
public class SettingsPanel
{
    private readonly UIRenderer _ui;
    private readonly InputManager _input;
    
    private Rectangle _panelRect;
    private bool _isVisible = false;
    private bool _isDragging = false;
    private Vector2 _dragOffset;
    
    // Settings values
    private float _musicVolume = 0.7f;
    private float _soundVolume = 0.8f;
    private bool _showHealthBars = true;
    private bool _showNames = true;
    private bool _showDamageNumbers = true;
    private bool _autoLoot = false;
    private bool _alwaysRun = false;
    private int _chatFontSize = 1; // 0=small, 1=medium, 2=large
    
    public SettingsPanel(UIRenderer ui, InputManager input)
    {
        _ui = ui;
        _input = input;
        _panelRect = new Rectangle(400, 150, 320, 400);
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
        
        // Handle setting interactions
        HandleSettingClicks(mousePos);
    }
    
    private void HandleSettingClicks(Vector2 mousePos)
    {
        if (!_input.IsLeftMousePressed) return;
        
        var y = _panelRect.Y + 50;
        var rowHeight = 35;
        
        // Music volume slider
        var musicSliderRect = new Rectangle(_panelRect.X + 140, y, 150, 20);
        if (_ui.IsInside(musicSliderRect, mousePos))
        {
            _musicVolume = Math.Clamp((mousePos.X - musicSliderRect.X) / musicSliderRect.Width, 0, 1);
        }
        y += rowHeight;
        
        // Sound volume slider
        var soundSliderRect = new Rectangle(_panelRect.X + 140, y, 150, 20);
        if (_ui.IsInside(soundSliderRect, mousePos))
        {
            _soundVolume = Math.Clamp((mousePos.X - soundSliderRect.X) / soundSliderRect.Width, 0, 1);
        }
        y += rowHeight + 20;
        
        // Checkboxes
        var checkX = _panelRect.X + 20;
        var checkSize = 20;
        
        // Show health bars
        var healthBarCheck = new Rectangle(checkX, y, checkSize, checkSize);
        if (_ui.IsInside(healthBarCheck, mousePos)) _showHealthBars = !_showHealthBars;
        y += rowHeight;
        
        // Show names
        var namesCheck = new Rectangle(checkX, y, checkSize, checkSize);
        if (_ui.IsInside(namesCheck, mousePos)) _showNames = !_showNames;
        y += rowHeight;
        
        // Show damage numbers
        var damageCheck = new Rectangle(checkX, y, checkSize, checkSize);
        if (_ui.IsInside(damageCheck, mousePos)) _showDamageNumbers = !_showDamageNumbers;
        y += rowHeight;
        
        // Auto loot
        var lootCheck = new Rectangle(checkX, y, checkSize, checkSize);
        if (_ui.IsInside(lootCheck, mousePos)) _autoLoot = !_autoLoot;
        y += rowHeight;
        
        // Always run
        var runCheck = new Rectangle(checkX, y, checkSize, checkSize);
        if (_ui.IsInside(runCheck, mousePos)) _alwaysRun = !_alwaysRun;
        y += rowHeight + 20;
        
        // Chat font size buttons
        for (int i = 0; i < 3; i++)
        {
            var btnRect = new Rectangle(_panelRect.X + 140 + i * 50, y, 45, 25);
            if (_ui.IsInside(btnRect, mousePos)) _chatFontSize = i;
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
        _ui.DrawText("SETTINGS", new Vector2(_panelRect.X + 10, _panelRect.Y + 5), Color.White, 1.5f);
        
        // Close button
        var closeBtn = new Rectangle(_panelRect.Right - 25, _panelRect.Y + 3, 20, 20);
        var closeHover = _ui.IsInside(closeBtn, mousePos);
        _ui.DrawRectangle(closeBtn, closeHover ? new Color(150, 50, 50) : new Color(100, 50, 50));
        _ui.DrawTextCentered("X", new Vector2(closeBtn.X + 10, closeBtn.Y + 3), Color.White, 1.5f);
        
        var y = _panelRect.Y + 50;
        var rowHeight = 35;
        
        // Audio section
        _ui.DrawText("AUDIO", new Vector2(_panelRect.X + 20, y - 18), Color.Gray, 1.2f);
        
        // Music volume
        _ui.DrawText("Music:", new Vector2(_panelRect.X + 20, y), Color.White, 1.3f);
        DrawSlider(_panelRect.X + 140, y, 150, _musicVolume, mousePos);
        y += rowHeight;
        
        // Sound volume
        _ui.DrawText("Sound:", new Vector2(_panelRect.X + 20, y), Color.White, 1.3f);
        DrawSlider(_panelRect.X + 140, y, 150, _soundVolume, mousePos);
        y += rowHeight + 20;
        
        // Display section
        _ui.DrawText("DISPLAY", new Vector2(_panelRect.X + 20, y - 18), Color.Gray, 1.2f);
        
        // Show health bars
        DrawCheckbox(_panelRect.X + 20, y, "Show Health Bars", _showHealthBars, mousePos);
        y += rowHeight;
        
        // Show names
        DrawCheckbox(_panelRect.X + 20, y, "Show Names", _showNames, mousePos);
        y += rowHeight;
        
        // Show damage numbers
        DrawCheckbox(_panelRect.X + 20, y, "Show Damage Numbers", _showDamageNumbers, mousePos);
        y += rowHeight + 20;
        
        // Gameplay section
        _ui.DrawText("GAMEPLAY", new Vector2(_panelRect.X + 20, y - 18), Color.Gray, 1.2f);
        
        // Auto loot
        DrawCheckbox(_panelRect.X + 20, y, "Auto Loot", _autoLoot, mousePos);
        y += rowHeight;
        
        // Always run
        DrawCheckbox(_panelRect.X + 20, y, "Always Run", _alwaysRun, mousePos);
        y += rowHeight + 20;
        
        // Chat font size
        _ui.DrawText("Chat Size:", new Vector2(_panelRect.X + 20, y), Color.White, 1.3f);
        
        var sizes = new[] { "S", "M", "L" };
        for (int i = 0; i < 3; i++)
        {
            var btnRect = new Rectangle(_panelRect.X + 140 + i * 50, y, 45, 25);
            var isSelected = _chatFontSize == i;
            var isHovered = _ui.IsInside(btnRect, mousePos);
            
            _ui.DrawRectangle(btnRect, isSelected ? new Color(70, 70, 100) : (isHovered ? new Color(55, 55, 75) : new Color(45, 45, 60)));
            _ui.DrawRectangleOutline(btnRect, isSelected ? Color.CornflowerBlue : AssetManager.BorderColor);
            _ui.DrawTextCentered(sizes[i], new Vector2(btnRect.X + 22, btnRect.Y + 5), isSelected ? Color.White : Color.Gray, 1.3f);
        }
    }
    
    private void DrawSlider(int x, int y, int width, float value, Vector2 mousePos)
    {
        var sliderRect = new Rectangle(x, y, width, 20);
        var isHovered = _ui.IsInside(sliderRect, mousePos);
        
        // Track
        _ui.DrawRectangle(sliderRect, new Color(30, 30, 40));
        
        // Fill
        var fillWidth = (int)(width * value);
        _ui.DrawRectangle(new Rectangle(x, y, fillWidth, 20), new Color(60, 100, 150));
        
        // Border
        _ui.DrawRectangleOutline(sliderRect, isHovered ? Color.CornflowerBlue : AssetManager.BorderColor);
        
        // Value text
        _ui.DrawTextCentered($"{(int)(value * 100)}%", new Vector2(x + width / 2, y + 3), Color.White, 1.2f);
    }
    
    private void DrawCheckbox(int x, int y, string label, bool value, Vector2 mousePos)
    {
        var checkRect = new Rectangle(x, y, 20, 20);
        var isHovered = _ui.IsInside(checkRect, mousePos);
        
        // Box
        _ui.DrawRectangle(checkRect, new Color(30, 30, 40));
        _ui.DrawRectangleOutline(checkRect, isHovered ? Color.CornflowerBlue : AssetManager.BorderColor);
        
        // Check mark
        if (value)
        {
            _ui.DrawTextCentered("✓", new Vector2(x + 10, y + 2), Color.LightGreen, 1.5f);
        }
        
        // Label
        _ui.DrawText(label, new Vector2(x + 30, y + 2), Color.White, 1.3f);
    }
}
