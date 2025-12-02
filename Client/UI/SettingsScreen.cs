using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RealmOfReality.Client.Assets;
using RealmOfReality.Client.Engine;

namespace RealmOfReality.Client.UI;

/// <summary>
/// Settings screen shown before login for configuring UO data path and other options
/// </summary>
public class SettingsScreen : IScreen
{
    private readonly UIRenderer _ui;
    private readonly InputManager _input;
    private readonly ClientSettings _settings;
    private readonly Action _onClose;
    private readonly Action<bool>? _onUOPathChanged;
    
    // UI State
    private string _uoPath = "";
    private bool _useUOGraphics = true;
    private bool _useUOTiles = true;
    private bool _useUOAnimations = true;
    private string _statusMessage = "";
    private Color _statusColor = Color.White;
    private int _focusedField = 0;
    private int _currentTab = 0;
    
    // File validation results
    private List<string> _presentFiles = new();
    private bool _pathValid = false;
    
    // UI Layout
    private Rectangle _panelRect;
    private Rectangle _pathRect;
    private Rectangle _browseButtonRect;
    private Rectangle _detectButtonRect;
    private Rectangle _saveButtonRect;
    private Rectangle _cancelButtonRect;
    
    private static readonly string[] Tabs = { "UO Assets", "Graphics", "Gameplay" };
    
    public SettingsScreen(UIRenderer ui, InputManager input, ClientSettings settings, 
        Action onClose, Action<bool>? onUOPathChanged = null)
    {
        _ui = ui;
        _input = input;
        _settings = settings;
        _onClose = onClose;
        _onUOPathChanged = onUOPathChanged;
    }
    
    public void Enter()
    {
        // Load current settings
        _uoPath = _settings.UODataPath;
        _useUOGraphics = _settings.UseUOGraphics;
        _useUOTiles = _settings.UseUOTiles;
        _useUOAnimations = _settings.UseUOAnimations;
        
        UpdateLayout();
        ValidatePath();
    }
    
    public void Exit() { }
    
    private void UpdateLayout()
    {
        var panelWidth = 600;
        var panelHeight = 500;
        var screenWidth = 1280;
        var screenHeight = 720;
        
        _panelRect = new Rectangle(
            (screenWidth - panelWidth) / 2,
            (screenHeight - panelHeight) / 2,
            panelWidth,
            panelHeight
        );
        
        var fieldWidth = panelWidth - 60;
        var startY = _panelRect.Y + 100;
        
        _pathRect = new Rectangle(_panelRect.X + 30, startY, fieldWidth - 80, 36);
        _browseButtonRect = new Rectangle(_pathRect.Right + 10, startY, 60, 36);
        _detectButtonRect = new Rectangle(_panelRect.X + 30, startY + 50, 150, 32);
        
        _saveButtonRect = new Rectangle(_panelRect.X + panelWidth / 2 - 130, _panelRect.Bottom - 60, 120, 40);
        _cancelButtonRect = new Rectangle(_panelRect.X + panelWidth / 2 + 10, _panelRect.Bottom - 60, 120, 40);
    }
    
    private void ValidatePath()
    {
        _settings.UODataPath = _uoPath;
        _pathValid = _settings.ValidateUOPath();
        _presentFiles = _settings.GetPresentFiles();
        
        if (_pathValid)
        {
            _statusMessage = $"Valid UO data path - {_presentFiles.Count} files found";
            _statusColor = Color.LightGreen;
        }
        else if (Directory.Exists(_uoPath))
        {
            _statusMessage = "Path exists but required files (art.mul, artidx.mul) not found";
            _statusColor = Color.Yellow;
        }
        else
        {
            _statusMessage = "Path does not exist";
            _statusColor = Color.Red;
        }
    }
    
    public void Update(GameTime gameTime)
    {
        var mousePos = _input.MousePosition;
        var kb = Keyboard.GetState();
        
        // Handle text input for path
        if (_focusedField == 0)
        {
            foreach (var key in kb.GetPressedKeys())
            {
                if (_input.IsKeyPressed(key))
                {
                    if (key == Keys.Back && _uoPath.Length > 0)
                    {
                        _uoPath = _uoPath[..^1];
                        ValidatePath();
                    }
                    else if (key == Keys.Delete)
                    {
                        _uoPath = "";
                        ValidatePath();
                    }
                    else if (key == Keys.V && (kb.IsKeyDown(Keys.LeftControl) || kb.IsKeyDown(Keys.RightControl)))
                    {
                        // Paste not easily available in MonoGame, skip for now
                    }
                    else
                    {
                        var c = KeyToChar(key, kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift));
                        if (c.HasValue && _uoPath.Length < 256)
                        {
                            _uoPath += c.Value;
                            ValidatePath();
                        }
                    }
                }
            }
        }
        
        // Tab handling
        if (_input.IsKeyPressed(Keys.Tab))
        {
            // Could cycle through fields
        }
        
        // Escape to cancel
        if (_input.IsKeyPressed(Keys.Escape))
        {
            _onClose();
            return;
        }
        
        // Mouse clicks
        if (_input.IsLeftMousePressed)
        {
            // Path field focus
            if (_ui.IsInside(_pathRect, mousePos))
            {
                _focusedField = 0;
            }
            
            // Clear button
            if (_ui.IsInside(_browseButtonRect, mousePos))
            {
                _uoPath = "";
                _focusedField = 0;
                ValidatePath();
            }
            
            // Auto-detect button
            if (_ui.IsInside(_detectButtonRect, mousePos))
            {
                if (_settings.AutoDetectUOPath())
                {
                    _uoPath = _settings.UODataPath;
                    ValidatePath();
                    _statusMessage = $"Found UO at: {_uoPath}";
                }
                else
                {
                    _statusMessage = "Could not find UO. Type path manually (e.g. C:\\test\\gamedata)";
                    _statusColor = Color.Yellow;
                }
            }
            
            // Tab clicks
            for (int i = 0; i < Tabs.Length; i++)
            {
                var tabRect = new Rectangle(_panelRect.X + 10 + i * 195, _panelRect.Y + 30, 190, 28);
                if (_ui.IsInside(tabRect, mousePos))
                {
                    _currentTab = i;
                }
            }
            
            // Checkboxes (in UO Assets tab)
            if (_currentTab == 0)
            {
                var cbY = _panelRect.Y + 200;
                if (_ui.IsInside(new Rectangle(_panelRect.X + 30, cbY, 250, 25), mousePos))
                    _useUOGraphics = !_useUOGraphics;
                if (_ui.IsInside(new Rectangle(_panelRect.X + 30, cbY + 35, 250, 25), mousePos))
                    _useUOTiles = !_useUOTiles;
                if (_ui.IsInside(new Rectangle(_panelRect.X + 30, cbY + 70, 250, 25), mousePos))
                    _useUOAnimations = !_useUOAnimations;
            }
            
            // Save button
            if (_ui.IsInside(_saveButtonRect, mousePos))
            {
                SaveSettings();
                _onClose();
                return;
            }
            
            // Cancel button
            if (_ui.IsInside(_cancelButtonRect, mousePos))
            {
                _onClose();
                return;
            }
        }
    }
    
    private void SaveSettings()
    {
        Console.WriteLine($"SettingsScreen: Saving settings...");
        Console.WriteLine($"  UODataPath: {_uoPath}");
        Console.WriteLine($"  UseUOGraphics: {_useUOGraphics}");
        Console.WriteLine($"  PathValid: {_pathValid}");
        
        _settings.UODataPath = _uoPath;
        _settings.UseUOGraphics = _useUOGraphics;
        _settings.UseUOTiles = _useUOTiles;
        _settings.UseUOAnimations = _useUOAnimations;
        _settings.Save();
        
        // Always notify about the change so assets can be reloaded
        Console.WriteLine($"  Invoking callback with pathValid={_pathValid && _useUOGraphics}");
        _onUOPathChanged?.Invoke(_pathValid && _useUOGraphics);
    }
    
    public void Draw(GameTime gameTime)
    {
        _ui.Begin();
        
        var mousePos = _input.MousePosition;
        
        // Dim background
        _ui.DrawRectangle(new Rectangle(0, 0, 1280, 720), new Color(0, 0, 0, 180));
        
        // Panel
        _ui.DrawPanel(_panelRect);
        
        // Title
        var titleBar = new Rectangle(_panelRect.X, _panelRect.Y, _panelRect.Width, 28);
        _ui.DrawRectangle(titleBar, new Color(50, 50, 70));
        _ui.DrawText("CLIENT SETTINGS", new Vector2(_panelRect.X + 15, _panelRect.Y + 5), Color.White, 1.6f);
        
        // Tabs
        for (int i = 0; i < Tabs.Length; i++)
        {
            var tabRect = new Rectangle(_panelRect.X + 10 + i * 195, _panelRect.Y + 30, 190, 28);
            var isSelected = _currentTab == i;
            var isHovered = _ui.IsInside(tabRect, mousePos);
            
            var tabColor = isSelected ? new Color(70, 70, 100) : (isHovered ? new Color(55, 55, 75) : new Color(45, 45, 60));
            _ui.DrawRectangle(tabRect, tabColor);
            _ui.DrawRectangleOutline(tabRect, isSelected ? Color.CornflowerBlue : AssetManager.BorderColor);
            _ui.DrawTextCentered(Tabs[i], new Vector2(tabRect.X + 95, tabRect.Y + 7), isSelected ? Color.White : Color.Gray, 1.3f);
        }
        
        // Tab content
        switch (_currentTab)
        {
            case 0: DrawUOAssetsTab(mousePos); break;
            case 1: DrawGraphicsTab(mousePos); break;
            case 2: DrawGameplayTab(mousePos); break;
        }
        
        // Bottom buttons
        DrawButton(_saveButtonRect, "Save", mousePos, Color.DarkGreen);
        DrawButton(_cancelButtonRect, "Cancel", mousePos, Color.DarkRed);
        
        _ui.End();
    }
    
    private void DrawUOAssetsTab(Vector2 mousePos)
    {
        var contentY = _panelRect.Y + 70;
        
        // Path section
        _ui.DrawText("Ultima Online Data Path:", new Vector2(_panelRect.X + 30, contentY), Color.White, 1.3f);
        contentY += 28;
        
        // Path text field
        var pathFocused = _focusedField == 0;
        _ui.DrawRectangle(_pathRect, pathFocused ? new Color(50, 50, 60) : new Color(35, 35, 45));
        _ui.DrawRectangleOutline(_pathRect, pathFocused ? Color.CornflowerBlue : AssetManager.BorderColor);
        
        var displayPath = _uoPath.Length > 45 ? "..." + _uoPath[^42..] : _uoPath;
        if (string.IsNullOrEmpty(_uoPath))
            displayPath = pathFocused ? "_" : "(click here to type path)";
        else if (pathFocused)
            displayPath += "_";
        
        var pathColor = string.IsNullOrEmpty(_uoPath) && !pathFocused ? Color.Gray : Color.White;
        _ui.DrawText(displayPath, new Vector2(_pathRect.X + 8, _pathRect.Y + 9), pathColor, 1.2f);
        
        // Clear button (replaces browse)
        DrawButton(_browseButtonRect, "CLR", mousePos, new Color(80, 60, 60));
        
        // Auto-detect button
        contentY = _pathRect.Bottom + 15;
        _detectButtonRect = new Rectangle(_panelRect.X + 30, contentY, 150, 32);
        DrawButton(_detectButtonRect, "Auto-Detect", mousePos, new Color(60, 80, 60));
        
        // Status message
        contentY += 45;
        _ui.DrawText(_statusMessage, new Vector2(_panelRect.X + 30, contentY), _statusColor, 1.2f);
        
        // Checkboxes
        contentY += 35;
        DrawCheckbox(_panelRect.X + 30, contentY, "Use UO Graphics (requires valid path)", _useUOGraphics, mousePos);
        contentY += 35;
        DrawCheckbox(_panelRect.X + 30, contentY, "Use UO Land Tiles", _useUOTiles, mousePos);
        contentY += 35;
        DrawCheckbox(_panelRect.X + 30, contentY, "Use UO Character Animations", _useUOAnimations, mousePos);
        
        // Found files list
        contentY += 45;
        _ui.DrawText("Detected Files:", new Vector2(_panelRect.X + 30, contentY), new Color(180, 160, 100), 1.3f);
        contentY += 25;
        
        if (_presentFiles.Count > 0)
        {
            var fileText = string.Join(", ", _presentFiles.Take(8));
            if (_presentFiles.Count > 8)
                fileText += $" (+{_presentFiles.Count - 8} more)";
            _ui.DrawText(fileText, new Vector2(_panelRect.X + 30, contentY), Color.LightGreen, 1.1f);
        }
        else
        {
            _ui.DrawText("No UO files found at specified path", new Vector2(_panelRect.X + 30, contentY), Color.Gray, 1.1f);
        }
    }
    
    private void DrawGraphicsTab(Vector2 mousePos)
    {
        var contentY = _panelRect.Y + 80;
        
        _ui.DrawText("Resolution: 1280 x 720 (not configurable yet)", new Vector2(_panelRect.X + 30, contentY), Color.Gray, 1.2f);
        contentY += 35;
        
        _ui.DrawText("Fullscreen: Off (not configurable yet)", new Vector2(_panelRect.X + 30, contentY), Color.Gray, 1.2f);
        contentY += 35;
        
        _ui.DrawText("VSync: On", new Vector2(_panelRect.X + 30, contentY), Color.Gray, 1.2f);
        contentY += 50;
        
        _ui.DrawText("Graphics settings will be expanded in future updates.", new Vector2(_panelRect.X + 30, contentY), Color.Yellow, 1.2f);
    }
    
    private void DrawGameplayTab(Vector2 mousePos)
    {
        var contentY = _panelRect.Y + 80;
        
        DrawCheckbox(_panelRect.X + 30, contentY, "Always Run", _settings.AlwaysRun, mousePos);
        contentY += 35;
        DrawCheckbox(_panelRect.X + 30, contentY, "Show Health Bars", _settings.ShowHealthBars, mousePos);
        contentY += 35;
        DrawCheckbox(_panelRect.X + 30, contentY, "Show Names", _settings.ShowNames, mousePos);
        contentY += 35;
        DrawCheckbox(_panelRect.X + 30, contentY, "Show Grid", _settings.ShowGrid, mousePos);
        contentY += 35;
        DrawCheckbox(_panelRect.X + 30, contentY, "Show FPS", _settings.ShowFPS, mousePos);
    }
    
    private void DrawCheckbox(int x, int y, string label, bool isChecked, Vector2 mousePos)
    {
        var boxRect = new Rectangle(x, y, 20, 20);
        var isHover = _ui.IsInside(new Rectangle(x, y, 300, 25), mousePos);
        
        _ui.DrawRectangle(boxRect, isHover ? new Color(60, 60, 80) : new Color(45, 45, 60));
        _ui.DrawRectangleOutline(boxRect, AssetManager.BorderColor);
        
        if (isChecked)
        {
            _ui.DrawTextCentered("X", new Vector2(x + 10, y + 2), Color.LightGreen, 1.5f);
        }
        
        _ui.DrawText(label, new Vector2(x + 30, y + 2), isHover ? Color.White : Color.LightGray, 1.2f);
    }
    
    private void DrawButton(Rectangle rect, string text, Vector2 mousePos, Color baseColor)
    {
        var isHover = _ui.IsInside(rect, mousePos);
        var color = isHover ? new Color(baseColor.R + 30, baseColor.G + 30, baseColor.B + 30) : baseColor;
        
        _ui.DrawRectangle(rect, color);
        _ui.DrawRectangleOutline(rect, isHover ? Color.White : AssetManager.BorderColor);
        _ui.DrawTextCentered(text, new Vector2(rect.X + rect.Width / 2, rect.Y + rect.Height / 2 - 8), Color.White, 1.4f);
    }
    
    private char? KeyToChar(Keys key, bool shift)
    {
        if (key >= Keys.A && key <= Keys.Z)
        {
            var c = (char)('a' + (key - Keys.A));
            return shift ? char.ToUpper(c) : c;
        }
        if (key >= Keys.D0 && key <= Keys.D9 && !shift)
            return (char)('0' + (key - Keys.D0));
        
        return key switch
        {
            Keys.Space => ' ',
            Keys.OemPeriod => '.',
            Keys.OemMinus => shift ? '_' : '-',
            Keys.OemBackslash => '\\',
            Keys.OemPipe => '\\',
            Keys.OemQuestion => '/',
            Keys.OemSemicolon => shift ? ':' : ';',
            _ => null
        };
    }
}
