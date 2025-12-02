using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RealmOfReality.Client.Assets;
using RealmOfReality.Client.Engine;

#pragma warning disable CS0414 // Fields are assigned for future use

namespace RealmOfReality.Client.UI;

/// <summary>
/// Help panel showing controls and game info, plus asset browser for testing
/// </summary>
public class HelpPanel
{
    private readonly UIRenderer _ui;
    private readonly InputManager _input;
    private readonly UOAssetManager? _uoAssets;
    
    private Rectangle _panelRect;
    private bool _isVisible = false;
    private bool _isDragging = false;
    private Vector2 _dragOffset;
    private int _currentTab = 0;
    
    // Asset browser state
    private int _assetType = 0; // 0=Land, 1=Static, 2=Gump, 3=Anim
    private int _currentAssetId = 0;
    private int _animBodyId = 400;
    private int _animGroup = 4;
    private int _animDir = 4;
    private int _animFrame = 0;
    private double _animTimer = 0;
    private string _assetIdInput = "0";
    private string _bodyIdInput = "400";
    private string _groupInput = "4";
    private Animation? _currentAnim = null;
    
    private static readonly string[] Tabs = { "Controls", "Gameplay", "Combat", "Assets" };
    private static readonly string[] AssetTypes = { "Land Tiles", "Static Items", "Gumps", "Animations" };
    
    public HelpPanel(UIRenderer ui, InputManager input, UOAssetManager? uoAssets = null)
    {
        _ui = ui;
        _input = input;
        _uoAssets = uoAssets;
        _panelRect = new Rectangle(200, 50, 550, 580);
    }
    
    public void SetUOAssets(UOAssetManager? uoAssets)
    {
        // Allow setting assets after construction
        // Note: We store it but can't reassign readonly field, so we use the constructor param
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
        
        // Tab clicks
        for (int i = 0; i < Tabs.Length; i++)
        {
            var tabRect = new Rectangle(_panelRect.X + 10 + i * 133, _panelRect.Y + 30, 128, 28);
            if (_input.IsLeftMousePressed && _ui.IsInside(tabRect, mousePos))
            {
                _currentTab = i;
            }
        }
        
        // Asset browser specific updates
        if (_currentTab == 3)
        {
            UpdateAssetBrowser(gameTime);
        }
    }
    
    private void UpdateAssetBrowser(GameTime gameTime)
    {
        var mousePos = _input.MousePosition;
        var contentY = _panelRect.Y + 70;
        
        // Asset type buttons
        for (int i = 0; i < AssetTypes.Length; i++)
        {
            var btnRect = new Rectangle(_panelRect.X + 15 + i * 130, contentY, 125, 28);
            if (_input.IsLeftMousePressed && _ui.IsInside(btnRect, mousePos))
            {
                _assetType = i;
                _currentAssetId = 0;
                _assetIdInput = "0";
            }
        }
        
        // Navigation buttons
        var navY = contentY + 45;
        var prevRect = new Rectangle(_panelRect.X + 15, navY, 50, 30);
        var nextRect = new Rectangle(_panelRect.X + 75, navY, 50, 30);
        var prev10Rect = new Rectangle(_panelRect.X + 135, navY, 50, 30);
        var next10Rect = new Rectangle(_panelRect.X + 195, navY, 50, 30);
        
        if (_input.IsLeftMousePressed)
        {
            if (_ui.IsInside(prevRect, mousePos)) { _currentAssetId = Math.Max(0, _currentAssetId - 1); _assetIdInput = _currentAssetId.ToString(); }
            if (_ui.IsInside(nextRect, mousePos)) { _currentAssetId++; _assetIdInput = _currentAssetId.ToString(); }
            if (_ui.IsInside(prev10Rect, mousePos)) { _currentAssetId = Math.Max(0, _currentAssetId - 10); _assetIdInput = _currentAssetId.ToString(); }
            if (_ui.IsInside(next10Rect, mousePos)) { _currentAssetId += 10; _assetIdInput = _currentAssetId.ToString(); }
        }
        
        // Update animation frame
        if (_assetType == 3 && _currentAnim != null && _currentAnim.FrameCount > 0)
        {
            _animTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
            if (_animTimer >= 150)
            {
                _animTimer = 0;
                _animFrame = (_animFrame + 1) % _currentAnim.FrameCount;
            }
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
        _ui.DrawText("HELP & INFORMATION", new Vector2(_panelRect.X + 10, _panelRect.Y + 5), Color.White, 1.5f);
        
        // Close button
        var closeBtn = new Rectangle(_panelRect.Right - 25, _panelRect.Y + 3, 20, 20);
        var closeHover = _ui.IsInside(closeBtn, mousePos);
        _ui.DrawRectangle(closeBtn, closeHover ? new Color(150, 50, 50) : new Color(100, 50, 50));
        _ui.DrawTextCentered("X", new Vector2(closeBtn.X + 10, closeBtn.Y + 3), Color.White, 1.5f);
        
        // Tabs
        for (int i = 0; i < Tabs.Length; i++)
        {
            var tabRect = new Rectangle(_panelRect.X + 10 + i * 133, _panelRect.Y + 30, 128, 28);
            var isSelected = _currentTab == i;
            var isHovered = _ui.IsInside(tabRect, mousePos);
            
            var tabColor = isSelected ? new Color(70, 70, 100) : (isHovered ? new Color(55, 55, 75) : new Color(45, 45, 60));
            _ui.DrawRectangle(tabRect, tabColor);
            _ui.DrawRectangleOutline(tabRect, isSelected ? Color.CornflowerBlue : AssetManager.BorderColor);
            _ui.DrawTextCentered(Tabs[i], new Vector2(tabRect.X + 64, tabRect.Y + 7), isSelected ? Color.White : Color.Gray, 1.3f);
        }
        
        // Content area
        var contentRect = new Rectangle(_panelRect.X + 10, _panelRect.Y + 65, _panelRect.Width - 20, _panelRect.Height - 80);
        
        switch (_currentTab)
        {
            case 0: DrawControlsTab(contentRect); break;
            case 1: DrawGameplayTab(contentRect); break;
            case 2: DrawCombatTab(contentRect); break;
            case 3: DrawAssetsTab(contentRect, gameTime); break;
        }
    }
    
    private void DrawControlsTab(Rectangle content)
    {
        var y = content.Y + 10;
        var lineHeight = 22;
        
        DrawSection("MOVEMENT", content.X, ref y, lineHeight);
        DrawKeyValue("WASD / Arrows", "Move character", content.X, ref y, lineHeight);
        DrawKeyValue("Right-click + Hold", "Move toward cursor", content.X, ref y, lineHeight);
        DrawKeyValue("Shift + Move", "Run", content.X, ref y, lineHeight);
        DrawKeyValue("R", "Toggle always run", content.X, ref y, lineHeight);
        
        y += 10;
        DrawSection("WINDOWS", content.X, ref y, lineHeight);
        DrawKeyValue("I", "Inventory", content.X, ref y, lineHeight);
        DrawKeyValue("E / P", "Equipment (Paperdoll)", content.X, ref y, lineHeight);
        DrawKeyValue("K", "Skills", content.X, ref y, lineHeight);
        DrawKeyValue("B", "Spellbook", content.X, ref y, lineHeight);
        DrawKeyValue("O", "Settings", content.X, ref y, lineHeight);
        DrawKeyValue("F1", "Help", content.X, ref y, lineHeight);
        DrawKeyValue("ESC", "Close window / Menu", content.X, ref y, lineHeight);
        
        y += 10;
        DrawSection("INTERACTION", content.X, ref y, lineHeight);
        DrawKeyValue("Enter", "Open/send chat", content.X, ref y, lineHeight);
        DrawKeyValue("1-0", "Use hotbar slots", content.X, ref y, lineHeight);
        DrawKeyValue("Scroll wheel", "Zoom camera", content.X, ref y, lineHeight);
    }
    
    private void DrawGameplayTab(Rectangle content)
    {
        var y = content.Y + 10;
        var lineHeight = 20;
        
        DrawSection("SKILLS", content.X, ref y, lineHeight);
        DrawParagraph("Skills range from 0.0 to 100.0 and improve through use.", content.X, ref y, lineHeight, content.Width - 20);
        DrawParagraph("Total skill cap is 700.0 points across all skills.", content.X, ref y, lineHeight, content.Width - 20);
        y += 10;
        
        DrawSection("STATS", content.X, ref y, lineHeight);
        DrawParagraph("Strength: Melee damage, carrying capacity", content.X, ref y, lineHeight, content.Width - 20);
        DrawParagraph("Dexterity: Attack speed, defense", content.X, ref y, lineHeight, content.Width - 20);
        DrawParagraph("Intelligence: Mana pool, spell power", content.X, ref y, lineHeight, content.Width - 20);
        y += 10;
        
        DrawSection("DEATH", content.X, ref y, lineHeight);
        DrawParagraph("When you die, you become a ghost. Find a healer or use a resurrection spell to return to life.", content.X, ref y, lineHeight, content.Width - 20);
    }
    
    private void DrawCombatTab(Rectangle content)
    {
        var y = content.Y + 10;
        var lineHeight = 20;
        
        DrawSection("ATTACKING", content.X, ref y, lineHeight);
        DrawParagraph("Double-click enemies to attack. Combat uses your equipped weapon and combat skills.", content.X, ref y, lineHeight, content.Width - 20);
        y += 10;
        
        DrawSection("MAGIC", content.X, ref y, lineHeight);
        DrawParagraph("Spells require mana and often reagents. Higher magery skill unlocks more powerful spell circles.", content.X, ref y, lineHeight, content.Width - 20);
        y += 10;
        
        DrawSection("SPELL CIRCLES", content.X, ref y, lineHeight);
        DrawParagraph("Circle 1-2: Basic spells (0-30 Magery)", content.X, ref y, lineHeight, content.Width - 20);
        DrawParagraph("Circle 3-4: Intermediate (30-60 Magery)", content.X, ref y, lineHeight, content.Width - 20);
        DrawParagraph("Circle 5-6: Advanced (60-85 Magery)", content.X, ref y, lineHeight, content.Width - 20);
        DrawParagraph("Circle 7-8: Master (85+ Magery)", content.X, ref y, lineHeight, content.Width - 20);
    }
    
    private void DrawAssetsTab(Rectangle content, GameTime gameTime)
    {
        var mousePos = _input.MousePosition;
        var y = content.Y + 5;
        
        // Check if UO assets are loaded
        if (_uoAssets == null || !_uoAssets.IsInitialized)
        {
            _ui.DrawText("UO Assets not loaded!", new Vector2(content.X + 10, y), Color.Red, 1.4f);
            y += 30;
            _ui.DrawText("Configure UO data path in Settings", new Vector2(content.X + 10, y), Color.Yellow, 1.2f);
            y += 25;
            _ui.DrawText("(Press O or click Settings button)", new Vector2(content.X + 10, y), Color.Gray, 1.2f);
            return;
        }
        
        // Asset type selector buttons
        for (int i = 0; i < AssetTypes.Length; i++)
        {
            var btnRect = new Rectangle(content.X + 5 + i * 130, y, 125, 28);
            var isSelected = _assetType == i;
            var isHover = _ui.IsInside(btnRect, mousePos);
            
            var color = isSelected ? new Color(70, 100, 70) : (isHover ? new Color(60, 60, 80) : new Color(45, 45, 60));
            _ui.DrawRectangle(btnRect, color);
            _ui.DrawRectangleOutline(btnRect, isSelected ? Color.LightGreen : AssetManager.BorderColor);
            _ui.DrawTextCentered(AssetTypes[i], new Vector2(btnRect.X + 62, btnRect.Y + 7), Color.White, 1.1f);
        }
        y += 40;
        
        // Navigation buttons
        DrawNavButton(content.X + 5, y, 50, "<", mousePos);
        DrawNavButton(content.X + 65, y, 50, ">", mousePos);
        DrawNavButton(content.X + 125, y, 50, "-10", mousePos);
        DrawNavButton(content.X + 185, y, 50, "+10", mousePos);
        
        // Current ID display
        _ui.DrawText($"ID: {_currentAssetId}", new Vector2(content.X + 250, y + 5), Color.Yellow, 1.4f);
        y += 45;
        
        // Preview area
        var previewRect = new Rectangle(content.X + 5, y, 200, 200);
        _ui.DrawRectangle(previewRect, new Color(20, 20, 30));
        _ui.DrawRectangleOutline(previewRect, AssetManager.BorderColor);
        
        // Draw the asset
        DrawAssetPreview(previewRect);
        
        // Info area
        var infoX = previewRect.Right + 20;
        var infoY = y;
        
        DrawAssetInfo(infoX, infoY);
        
        y = previewRect.Bottom + 15;
        
        // Animation-specific controls
        if (_assetType == 3)
        {
            DrawAnimationControls(content.X + 5, y, mousePos);
        }
        
        // Quick ID jumps for common assets
        y = content.Bottom - 100;
        _ui.DrawText("Quick Jump:", new Vector2(content.X + 10, y), new Color(180, 160, 100), 1.2f);
        y += 22;
        
        var quickIds = _assetType switch
        {
            0 => new[] { ("Grass", 3), ("Water", 168), ("Sand", 24), ("Stone", 220) },
            1 => new[] { ("Sword", 3936), ("Shield", 7025), ("Potion", 3847), ("Gold", 3821) },
            2 => new[] { ("Btn", 2440), ("Scroll", 2100), ("Spell1", 2240), ("Spell8", 2280) },
            3 => new[] { ("Human", 400), ("Dragon", 12), ("Skeleton", 50), ("Wolf", 225) },
            _ => Array.Empty<(string, int)>()
        };
        
        var qx = content.X + 10;
        foreach (var (name, id) in quickIds)
        {
            var qRect = new Rectangle(qx, y, 65, 24);
            var qHover = _ui.IsInside(qRect, mousePos);
            _ui.DrawRectangle(qRect, qHover ? new Color(60, 60, 80) : new Color(45, 45, 60));
            _ui.DrawTextCentered(name, new Vector2(qRect.X + 32, qRect.Y + 5), Color.White, 1.0f);
            
            if (_input.IsLeftMousePressed && qHover)
            {
                if (_assetType == 3)
                    _animBodyId = id;
                else
                    _currentAssetId = id;
                _assetIdInput = id.ToString();
            }
            qx += 70;
        }
    }
    
    private void DrawNavButton(int x, int y, int width, string text, Vector2 mousePos)
    {
        var rect = new Rectangle(x, y, width, 30);
        var isHover = _ui.IsInside(rect, mousePos);
        _ui.DrawRectangle(rect, isHover ? new Color(60, 60, 80) : new Color(45, 45, 60));
        _ui.DrawRectangleOutline(rect, AssetManager.BorderColor);
        _ui.DrawTextCentered(text, new Vector2(rect.X + width / 2, rect.Y + 7), Color.White, 1.3f);
    }
    
    private void DrawAssetPreview(Rectangle previewRect)
    {
        if (_uoAssets == null) return;
        
        var centerX = previewRect.X + previewRect.Width / 2;
        var centerY = previewRect.Y + previewRect.Height / 2;
        
        switch (_assetType)
        {
            case 0: // Land Tiles
                var landTex = _uoAssets.GetLandTile(_currentAssetId);
                if (landTex != null)
                {
                    _ui.DrawTexture(landTex, new Rectangle(centerX - 22, centerY - 22, 44, 44));
                }
                else
                {
                    _ui.DrawTextCentered("No tile", new Vector2(centerX, centerY - 8), Color.Gray, 1.2f);
                }
                break;
                
            case 1: // Static Items
                var staticArt = _uoAssets.Art?.GetStaticItem(_currentAssetId);
                if (staticArt?.Texture != null)
                {
                    // Scale to fit while maintaining aspect ratio
                    var scale = Math.Min(180f / staticArt.Width, 180f / staticArt.Height);
                    var w = (int)(staticArt.Width * scale);
                    var h = (int)(staticArt.Height * scale);
                    _ui.DrawTexture(staticArt.Texture, new Rectangle(centerX - w / 2, centerY - h / 2, w, h));
                }
                else
                {
                    _ui.DrawTextCentered("No item", new Vector2(centerX, centerY - 8), Color.Gray, 1.2f);
                }
                break;
                
            case 2: // Gumps
                var gump = _uoAssets.Gumps?.GetGump(_currentAssetId);
                if (gump?.Texture != null)
                {
                    var scale = Math.Min(180f / gump.Width, 180f / gump.Height);
                    var w = (int)(gump.Width * scale);
                    var h = (int)(gump.Height * scale);
                    _ui.DrawTexture(gump.Texture, new Rectangle(centerX - w / 2, centerY - h / 2, w, h));
                }
                else
                {
                    _ui.DrawTextCentered("No gump", new Vector2(centerX, centerY - 8), Color.Gray, 1.2f);
                }
                break;
                
            case 3: // Animations
                // Load animation if needed
                if (_currentAnim == null || _currentAnim.BodyId != _animBodyId)
                {
                    _currentAnim = _uoAssets.GetAnimation(_animBodyId, (AnimAction)_animGroup, (AnimDirection)_animDir);
                    _animFrame = 0;
                }
                
                if (_currentAnim != null && _currentAnim.FrameCount > 0 && _animFrame < _currentAnim.FrameCount)
                {
                    var frame = _currentAnim.Frames[_animFrame];
                    if (frame?.Texture != null)
                    {
                        var scale = Math.Min(180f / frame.Width, 180f / frame.Height);
                        var w = (int)(frame.Width * scale);
                        var h = (int)(frame.Height * scale);
                        _ui.DrawTexture(frame.Texture, new Rectangle(centerX - w / 2, centerY - h / 2, w, h));
                    }
                    else
                    {
                        // Debug: show why frame isn't rendering
                        if (frame == null)
                            _ui.DrawTextCentered($"Frame[{_animFrame}] NULL", new Vector2(centerX, centerY - 8), Color.Red, 1.0f);
                        else
                            _ui.DrawTextCentered($"Frame[{_animFrame}] no tex", new Vector2(centerX, centerY - 8), Color.Orange, 1.0f);
                    }
                }
                else
                {
                    _ui.DrawTextCentered("No anim", new Vector2(centerX, centerY - 8), Color.Gray, 1.2f);
                }
                break;
        }
    }
    
    private void DrawAssetInfo(int x, int y)
    {
        if (_uoAssets == null) return;
        
        _ui.DrawText("Asset Info:", new Vector2(x, y), new Color(180, 160, 100), 1.2f);
        y += 22;
        
        switch (_assetType)
        {
            case 0:
                var landData = _uoAssets.GetLandData(_currentAssetId);
                var landTex = _uoAssets.GetLandTile(_currentAssetId);
                if (landData != null)
                {
                    _ui.DrawText($"Name: {landData.Value.Name}", new Vector2(x, y), Color.White, 1.1f); y += 18;
                    // Truncate flags to fit
                    var flagStr = landData.Value.Flags.ToString();
                    if (flagStr.Length > 30) flagStr = flagStr.Substring(0, 30) + "...";
                    _ui.DrawText($"Flags: {flagStr}", new Vector2(x, y), Color.LightGray, 1.0f); y += 18;
                }
                // Debug info for land tiles
                _ui.DrawText($"Texture: {(landTex != null ? "OK" : "NULL")}", new Vector2(x, y), landTex != null ? Color.Green : Color.Red, 1.0f); y += 18;
                _ui.DrawText($"Art loaded: {_uoAssets.Art?.IsLoaded}", new Vector2(x, y), Color.Yellow, 0.9f); y += 16;
                _ui.DrawText($"Art UOP: {_uoAssets.Art?.IsUsingUop}", new Vector2(x, y), Color.Yellow, 0.9f);
                break;
                
            case 1:
                var staticData = _uoAssets.GetItemData(_currentAssetId);
                var staticArt = _uoAssets.Art?.GetStaticItem(_currentAssetId);
                if (staticData != null)
                {
                    _ui.DrawText($"Name: {staticData.Value.Name}", new Vector2(x, y), Color.White, 1.1f); y += 18;
                    _ui.DrawText($"Weight: {staticData.Value.Weight}", new Vector2(x, y), Color.LightGray, 1.0f); y += 18;
                    _ui.DrawText($"Height: {staticData.Value.Height}", new Vector2(x, y), Color.LightGray, 1.0f); y += 18;
                }
                if (staticArt != null)
                {
                    _ui.DrawText($"Size: {staticArt.Width}x{staticArt.Height}", new Vector2(x, y), Color.LightGray, 1.0f); y += 18;
                }
                break;
                
            case 2:
                var gump = _uoAssets.Gumps?.GetGump(_currentAssetId);
                if (gump != null)
                {
                    _ui.DrawText($"Size: {gump.Width}x{gump.Height}", new Vector2(x, y), Color.White, 1.1f); y += 18;
                }
                else
                {
                    _ui.DrawText($"Gump: NULL", new Vector2(x, y), Color.Red, 1.0f); y += 18;
                    _ui.DrawText($"Gumps loaded: {_uoAssets.Gumps?.IsLoaded}", new Vector2(x, y), Color.Yellow, 0.9f); y += 16;
                    _ui.DrawText($"Gumps UOP: {_uoAssets.Gumps?.IsUsingUop}", new Vector2(x, y), Color.Yellow, 0.9f);
                }
                break;
                
            case 3:
                _ui.DrawText($"Body: {_animBodyId}", new Vector2(x, y), Color.White, 1.1f); y += 18;
                _ui.DrawText($"Group: {_animGroup}", new Vector2(x, y), Color.LightGray, 1.0f); y += 18;
                _ui.DrawText($"Dir: {_animDir}", new Vector2(x, y), Color.LightGray, 1.0f); y += 18;
                if (_currentAnim != null)
                {
                    _ui.DrawText($"Frames: {_currentAnim.FrameCount}", new Vector2(x, y), Color.LightGray, 1.0f); y += 18;
                    _ui.DrawText($"Current: {_animFrame}", new Vector2(x, y), Color.Yellow, 1.0f);
                }
                else
                {
                    _ui.DrawText($"Anim: NULL", new Vector2(x, y), Color.Red, 1.0f); y += 18;
                    _ui.DrawText($"Anims loaded: {_uoAssets.Animations?.IsLoaded}", new Vector2(x, y), Color.Yellow, 0.9f);
                }
                break;
        }
    }
    
    private void DrawAnimationControls(int x, int y, Vector2 mousePos)
    {
        _ui.DrawText("Animation Controls:", new Vector2(x, y), new Color(180, 160, 100), 1.2f);
        y += 25;
        
        // Body ID
        _ui.DrawText($"Body: {_animBodyId}", new Vector2(x, y), Color.White, 1.1f);
        DrawSmallNavButton(x + 100, y, "-", mousePos, () => { _animBodyId = Math.Max(0, _animBodyId - 1); _currentAnim = null; });
        DrawSmallNavButton(x + 130, y, "+", mousePos, () => { _animBodyId++; _currentAnim = null; });
        y += 25;
        
        // Group
        _ui.DrawText($"Group: {_animGroup}", new Vector2(x, y), Color.White, 1.1f);
        DrawSmallNavButton(x + 100, y, "-", mousePos, () => { _animGroup = Math.Max(0, _animGroup - 1); _currentAnim = null; });
        DrawSmallNavButton(x + 130, y, "+", mousePos, () => { _animGroup++; _currentAnim = null; });
        y += 25;
        
        // Direction
        _ui.DrawText($"Dir: {_animDir}", new Vector2(x, y), Color.White, 1.1f);
        DrawSmallNavButton(x + 100, y, "-", mousePos, () => { _animDir = Math.Max(0, _animDir - 1); _currentAnim = null; });
        DrawSmallNavButton(x + 130, y, "+", mousePos, () => { _animDir = Math.Min(4, _animDir + 1); _currentAnim = null; });
        
        // Common animation groups reference
        y += 35;
        _ui.DrawText("Human Groups: 0=Walk, 4=Stand, 9=Attack, 15=Cast, 20=Die", new Vector2(x, y), Color.Gray, 0.9f);
    }
    
    private void DrawSmallNavButton(int x, int y, string text, Vector2 mousePos, Action onClick)
    {
        var rect = new Rectangle(x, y - 2, 25, 22);
        var isHover = _ui.IsInside(rect, mousePos);
        _ui.DrawRectangle(rect, isHover ? new Color(60, 60, 80) : new Color(45, 45, 60));
        _ui.DrawTextCentered(text, new Vector2(rect.X + 12, rect.Y + 3), Color.White, 1.2f);
        
        if (_input.IsLeftMousePressed && isHover)
        {
            onClick();
        }
    }
    
    private void DrawSection(string title, int x, ref int y, int lineHeight)
    {
        _ui.DrawText(title, new Vector2(x, y), new Color(180, 160, 100), 1.4f);
        y += lineHeight + 5;
    }
    
    private void DrawKeyValue(string key, string value, int x, ref int y, int lineHeight)
    {
        _ui.DrawText(key, new Vector2(x + 10, y), Color.CornflowerBlue, 1.2f);
        _ui.DrawText(value, new Vector2(x + 180, y), Color.White, 1.2f);
        y += lineHeight;
    }
    
    private void DrawParagraph(string text, int x, ref int y, int lineHeight, int maxWidth)
    {
        _ui.DrawText(text, new Vector2(x + 10, y), Color.LightGray, 1.2f);
        y += lineHeight;
    }
}
