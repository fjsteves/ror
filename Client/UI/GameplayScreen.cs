using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RealmOfReality.Client.Engine;
using RealmOfReality.Client.Game;
using RealmOfReality.Client.Gumps;
using RealmOfReality.Shared.Core;
using RealmOfReality.Shared.Entities;
using RealmOfReality.Shared.Gumps;
using RealmOfReality.Shared.Items;
using RealmOfReality.Shared.Network;
using RealmOfReality.Shared.Skills;
using XnaGameTime = Microsoft.Xna.Framework.GameTime;
using XnaColor = Microsoft.Xna.Framework.Color;
using SharedColor = RealmOfReality.Shared.Core.Color;

#pragma warning disable CS0414 // Fields are assigned for future use

namespace RealmOfReality.Client.UI;

/// <summary>
/// Main gameplay screen - renders world and handles player input
/// </summary>
public class GameplayScreen : IScreen
{
    private readonly GameState _gameState;
    private readonly WorldRenderer _worldRenderer;
    private readonly UIRenderer _ui;
    private readonly Camera _camera;
    private readonly InputManager _input;
    private readonly AssetManager _assets;
    private readonly Assets.UOAssetManager? _uoAssets;
    
    // Gump system
    private ClientGumpManager? _gumpManager;
    private GumpRenderer? _gumpRenderer;
    
    // Sub-panels
    private PaperdollPanel? _paperdoll;
    private InventoryPanel? _inventory;
    private SkillsPanel? _skillsPanel;
    private SpellbookPanel? _spellbook;
    private SettingsPanel? _settings;
    private HelpPanel? _help;
    
    // Movement timing
    private double _lastMoveTime;
    private bool _isRunning = false;
    
    // Chat system - requires Enter to activate
    private bool _chatActive = false;
    private string _chatInput = "";
    private const int MaxChatLength = 100;
    private double _lastChatTime;
    private const double ChatRateLimit = 1.0;
    private readonly List<OverheadText> _overheadTexts = new();
    
    // Floating damage numbers
    private readonly List<FloatingDamage> _floatingDamage = new();
    
    // Particle effects
    private readonly List<Particle> _particles = new();
    
    // Hotbar
    private readonly HotbarSlot[] _hotbar = new HotbarSlot[10];
    
    // Dragging state for hotbar
    private bool _isDraggingToHotbar = false;
    private HotbarSlotType _dragType;
    private int _dragId;
    private string _dragName = "";
    private string _dragIcon = "";
    
    // Combat
    private EntityId? _targetEntityId;
    private double _lastAttackTime;
    private const double AttackInterval = 2.0;
    private bool _combatMode = false; // Tab to toggle
    private EntityId? _hoveredEntityId; // Entity currently under mouse cursor
    
    // Draggable target bars (multiple targets can have bars)
    private class TargetBar
    {
        public EntityId EntityId;
        public Vector2 Position;
        public bool IsDragging;
        public Vector2 DragOffset;
    }
    private readonly List<TargetBar> _targetBars = new();
    private TargetBar? _draggingTargetBar;
    private EntityId? _entityBeingDragged;
    private Vector2 _entityDragStartPos;
    
    // Spell casting
    private bool _isCasting = false;
    private double _castStartTime;
    private double _castDuration = 2.0;
    private int _castingSpellId;
    private string _castingSpellName = "";
    private bool _awaitingSpellTarget = false;
    private int _pendingSpellId;
    
    // Death state
    private bool _isDead = false;
    private float _deathFadeProgress = 0f;
    private bool _isGhost = false;
    
    // HUD Layout
    private Rectangle _healthBarRect;
    private Rectangle _manaBarRect;
    private Rectangle _staminaBarRect;
    private Rectangle _chatInputRect;
    private Rectangle _minimapRect;
    private Rectangle _hotbarRect;
    private Rectangle _expBarRect;
    private Rectangle _targetFrameRect;
    
    // Stat bar dragging
    private Rectangle _statBarPanel;
    private bool _isDraggingStatBar = false;
    private Vector2 _statBarDragOffset;
    private Vector2 _statBarPosition = new Vector2(20, 20);
    
    // Menu buttons
    private Rectangle _inventoryBtnRect;
    private Rectangle _equipmentBtnRect;
    private Rectangle _skillsBtnRect;
    private Rectangle _spellbookBtnRect;
    private Rectangle _settingsBtnRect;
    private Rectangle _helpBtnRect;
    
    // Admin tools (F12 to toggle)
    private bool _showAdminPanel = false;
    private int _adminSelectedNpcType = 10; // Goblin
    private string[] _npcTypes = { "Goblin", "Skeleton", "Wolf", "Dragon", "Healer", "Tree", "Rock" };
    private int[] _npcTypeIds = { 10, 20, 30, 100, 40, 200, 150 };
    
    // Command system - /command or /m command for multi-mode
    private enum AdminMode { None, Teleport, Kill, AddNpc, AddItem }
    private AdminMode _adminMode = AdminMode.None;
    private bool _adminModeMulti = false; // /m prefix enables multi-click mode
    private string _adminModeParam = ""; // Additional parameter for mode
    private bool _showAddMenu = false;
    private int _addMenuScroll = 0;
    
    // Viewport system - game renders at fixed size, anchored in window
    private readonly GraphicsDevice _graphicsDevice;
    private RenderTarget2D? _gameViewport;
    private int _viewportWidth = 1024;
    private int _viewportHeight = 768;
    private Rectangle _viewportRect; // Where the viewport is drawn on screen
    private int _windowWidth = 1280;
    private int _windowHeight = 720;
    
    // Viewport size limits
    public const int MinViewportWidth = 800;
    public const int MinViewportHeight = 600;
    public const int MaxViewportWidth = 1920;
    public const int MaxViewportHeight = 1080;
    
    // Viewport dragging (by border) and resize (by corner)
    private bool _isDraggingViewport = false;
    private bool _isDraggingViewportResize = false;
    private Vector2 _viewportDragOffset;
    private Vector2 _viewportResizeStart;
    private Vector2 _viewportPosition; // Top-left position of viewport (user-controlled)
    private bool _viewportPositionInitialized = false;
    private const int BorderDragWidth = 8; // How wide the draggable border is
    
    public GameplayScreen(GameState gameState, WorldRenderer worldRenderer, UIRenderer ui, 
        Camera camera, InputManager input, AssetManager assets, GraphicsDevice graphicsDevice, Assets.UOAssetManager? uoAssets = null)
    {
        _gameState = gameState;
        _worldRenderer = worldRenderer;
        _ui = ui;
        _camera = camera;
        _input = input;
        _assets = assets;
        _graphicsDevice = graphicsDevice;
        _uoAssets = uoAssets;
        
        // Get actual window dimensions from graphics device
        _windowWidth = graphicsDevice.Viewport.Width;
        _windowHeight = graphicsDevice.Viewport.Height;
        
        // Create viewport render target
        CreateViewportRenderTarget();
        
        // Initialize hotbar with some default spells for testing
        for (int i = 0; i < _hotbar.Length; i++)
            _hotbar[i] = new HotbarSlot();
        
        // Pre-populate some hotbar slots for testing
        _hotbar[0].SetSpell(5, "Magic Arrow", ">");
        _hotbar[1].SetSpell(18, "Fireball", "*");
        _hotbar[2].SetSpell(29, "Greater Heal", "+");
        _hotbar[3].SetSpell(42, "Energy Bolt", "!");
        
        // Subscribe to events
        _gameState.ChatReceived += OnChatReceived;
        _gameState.SystemMessageReceived += OnSystemMessage;
        _gameState.DamageDealt += OnDamageDealt;
        _gameState.EntityDied += OnEntityDied;
        _gameState.SpellCast += OnSpellCast;
        
        // Initialize gump system
        _gumpRenderer = new GumpRenderer(graphicsDevice, uoAssets);
        _gumpManager = new ClientGumpManager(_gumpRenderer);
        _gumpManager.OnGumpResponse += response => _gameState.SendGumpResponse(response);
        _gameState.GumpReceived += OnGumpReceived;
        _gameState.GumpClosed += OnGumpClosed;
    }
    
    /// <summary>
    /// Create or recreate the viewport render target
    /// </summary>
    private void CreateViewportRenderTarget()
    {
        _gameViewport?.Dispose();
        _gameViewport = new RenderTarget2D(_graphicsDevice, _viewportWidth, _viewportHeight);
        UpdateViewportPosition();
        UpdateLayout();
    }
    
    /// <summary>
    /// Update viewport position - anchors top-right initially, then uses user position
    /// </summary>
    private void UpdateViewportPosition()
    {
        if (!_viewportPositionInitialized)
        {
            // Initial position: top-right corner of window
            _viewportPosition = new Vector2(_windowWidth - _viewportWidth, 0);
            _viewportPositionInitialized = true;
        }
        
        // Clamp position to keep viewport on screen
        _viewportPosition.X = Math.Clamp(_viewportPosition.X, 0, Math.Max(0, _windowWidth - _viewportWidth));
        _viewportPosition.Y = Math.Clamp(_viewportPosition.Y, 0, Math.Max(0, _windowHeight - _viewportHeight));
        
        _viewportRect = new Rectangle(
            (int)_viewportPosition.X, 
            (int)_viewportPosition.Y, 
            _viewportWidth, 
            _viewportHeight);
        
        // Update camera viewport to match
        _camera.SetViewportSize(_viewportWidth, _viewportHeight);
    }
    
    /// <summary>
    /// Handle window resize
    /// </summary>
    public void OnWindowResize(int newWidth, int newHeight)
    {
        _windowWidth = newWidth;
        _windowHeight = newHeight;
        UpdateViewportPosition();
        UpdateLayout();
    }
    
    /// <summary>
    /// Resize the game viewport (clamped to min/max)
    /// </summary>
    public void SetViewportSize(int width, int height)
    {
        _viewportWidth = Math.Clamp(width, MinViewportWidth, MaxViewportWidth);
        _viewportHeight = Math.Clamp(height, MinViewportHeight, MaxViewportHeight);
        CreateViewportRenderTarget();
        UpdateLayout();
    }
    
    /// <summary>
    /// Convert screen coordinates to viewport coordinates
    /// Returns null if outside viewport
    /// </summary>
    public Vector2? ScreenToViewport(Vector2 screenPos)
    {
        if (!_viewportRect.Contains((int)screenPos.X, (int)screenPos.Y))
            return null;
        
        return new Vector2(
            screenPos.X - _viewportRect.X,
            screenPos.Y - _viewportRect.Y
        );
    }
    
    /// <summary>
    /// Check if screen position is inside the game viewport
    /// </summary>
    public bool IsInsideViewport(Vector2 screenPos)
    {
        return _viewportRect.Contains((int)screenPos.X, (int)screenPos.Y);
    }
    
    /// <summary>
    /// Handle viewport dragging (by border) and resize (by corner)
    /// </summary>
    private void UpdateViewportDragAndResize()
    {
        var mousePos = _input.MousePosition;
        
        // Don't start viewport drag if already dragging something else
        if (_isDraggingStatBar || _entityBeingDragged.HasValue || _draggingTargetBar != null)
            return;
        
        // Don't start viewport drag if mouse is over UI elements
        bool mouseOverStatBar = _ui.IsInside(_statBarPanel, mousePos);
        bool mouseOverTargetBar = _targetBars.Any(bar => 
            new Rectangle((int)bar.Position.X, (int)bar.Position.Y, 200, 55).Contains((int)mousePos.X, (int)mousePos.Y));
        
        if (mouseOverStatBar || mouseOverTargetBar)
            return;
        
        // Define regions
        const int resizeHandleSize = 20;
        var resizeHandle = new Rectangle(
            _viewportRect.Right - resizeHandleSize,
            _viewportRect.Bottom - resizeHandleSize,
            resizeHandleSize, resizeHandleSize);
        
        // Border regions for dragging (excluding resize corner)
        bool inTopBorder = mousePos.Y >= _viewportRect.Y && mousePos.Y < _viewportRect.Y + BorderDragWidth &&
                          mousePos.X >= _viewportRect.X && mousePos.X < _viewportRect.Right;
        bool inBottomBorder = mousePos.Y > _viewportRect.Bottom - BorderDragWidth && mousePos.Y <= _viewportRect.Bottom &&
                             mousePos.X >= _viewportRect.X && mousePos.X < _viewportRect.Right - resizeHandleSize;
        bool inLeftBorder = mousePos.X >= _viewportRect.X && mousePos.X < _viewportRect.X + BorderDragWidth &&
                           mousePos.Y >= _viewportRect.Y && mousePos.Y < _viewportRect.Bottom;
        bool inRightBorder = mousePos.X > _viewportRect.Right - BorderDragWidth && mousePos.X <= _viewportRect.Right &&
                            mousePos.Y >= _viewportRect.Y && mousePos.Y < _viewportRect.Bottom - resizeHandleSize;
        bool inBorder = inTopBorder || inBottomBorder || inLeftBorder || inRightBorder;
        
        // Start resize drag (corner takes priority)
        if (_input.IsLeftMousePressed && !_isDraggingViewport && resizeHandle.Contains((int)mousePos.X, (int)mousePos.Y))
        {
            _isDraggingViewportResize = true;
            _viewportResizeStart = mousePos;
        }
        // Start position drag (border)
        else if (_input.IsLeftMousePressed && !_isDraggingViewportResize && inBorder)
        {
            _isDraggingViewport = true;
            _viewportDragOffset = mousePos - _viewportPosition;
        }
        
        // Handle resize dragging
        if (_isDraggingViewportResize)
        {
            if (_input.IsLeftMouseDown)
            {
                var delta = mousePos - _viewportResizeStart;
                var newWidth = _viewportWidth + (int)delta.X;
                var newHeight = _viewportHeight + (int)delta.Y;
                
                // Clamp to valid range and window size
                newWidth = Math.Clamp(newWidth, MinViewportWidth, Math.Min(MaxViewportWidth, _windowWidth - (int)_viewportPosition.X));
                newHeight = Math.Clamp(newHeight, MinViewportHeight, Math.Min(MaxViewportHeight, _windowHeight - (int)_viewportPosition.Y));
                
                if (newWidth != _viewportWidth || newHeight != _viewportHeight)
                {
                    _viewportWidth = newWidth;
                    _viewportHeight = newHeight;
                    _viewportResizeStart = mousePos;
                    CreateViewportRenderTarget();
                }
            }
            else
            {
                _isDraggingViewportResize = false;
            }
        }
        
        // Handle position dragging
        if (_isDraggingViewport)
        {
            if (_input.IsLeftMouseDown)
            {
                _viewportPosition = mousePos - _viewportDragOffset;
                // Clamp to window bounds
                _viewportPosition.X = Math.Clamp(_viewportPosition.X, 0, Math.Max(0, _windowWidth - _viewportWidth));
                _viewportPosition.Y = Math.Clamp(_viewportPosition.Y, 0, Math.Max(0, _windowHeight - _viewportHeight));
                UpdateViewportPosition();
                UpdateLayout();
            }
            else
            {
                _isDraggingViewport = false;
            }
        }
    }
    
    public void Enter()
    {
        _chatActive = false;
        _chatInput = "";
        
        // Ensure viewport and camera are properly configured
        UpdateViewportPosition();
        
        // Initialize sub-panels
        _paperdoll = new PaperdollPanel(_ui, _input, _assets);
        _inventory = new InventoryPanel(_ui, _input, _assets);
        _skillsPanel = new SkillsPanel(_ui, _input);
        _spellbook = new SpellbookPanel(_ui, _input);
        _settings = new SettingsPanel(_ui, _input);
        _help = new HelpPanel(_ui, _input, _uoAssets);
        
        // Wire up drag events for hotbar
        _inventory.OnItemDragStart += OnInventoryDragStart;
        _spellbook.OnSpellDragStart += OnSpellDragStart;
        
        // Center camera on player instantly
        if (_gameState.Player != null)
        {
            _camera.Follow(_gameState.Player.Position);
            _camera.Position = _camera.TargetPosition;
        }
        
        UpdateLayout();
        
        // Add welcome message as overhead text
        if (_gameState.Player != null)
        {
            AddOverheadText(_gameState.Player.Id, "Welcome to Realm of Reality!", XnaColor.Yellow);
        }
    }
    
    public void Exit()
    {
        _chatActive = false;
        _chatInput = "";
        _paperdoll?.Hide();
        _inventory?.Hide();
        _skillsPanel?.Hide();
        _spellbook?.Hide();
        _settings?.Hide();
        _help?.Hide();
    }
    
    private void UpdateLayout()
    {
        // UI elements are positioned relative to viewport
        int vx = _viewportRect.X;
        int vy = _viewportRect.Y;
        int vw = _viewportRect.Width;
        int vh = _viewportRect.Height;
        
        // Stat bar panel is draggable (starts at top-left of viewport)
        var statX = vx + (int)_statBarPosition.X;
        var statY = vy + (int)_statBarPosition.Y;
        _statBarPanel = new Rectangle(statX, statY, 210, 70);
        _healthBarRect = new Rectangle(statX + 5, statY + 5, 200, 18);
        _manaBarRect = new Rectangle(statX + 5, statY + 27, 200, 18);
        _staminaBarRect = new Rectangle(statX + 5, statY + 49, 200, 18);
        
        // Exp bar at top of viewport
        _expBarRect = new Rectangle(vx, vy, vw, 5);
        
        // Chat at bottom-left of viewport
        _chatInputRect = new Rectangle(vx + 10, vy + vh - 40, 350, 30);
        
        // Minimap at top-right of viewport
        _minimapRect = new Rectangle(vx + vw - 170, vy + 10, 160, 160);
        
        // Target frame at top-center
        _targetFrameRect = new Rectangle(vx + (vw - 220) / 2 + 50, vy + 15, 220, 60);
        
        // Hotbar at bottom-center (can extend below viewport into black border)
        var hotbarWidth = 420;
        _hotbarRect = new Rectangle(vx + (vw - hotbarWidth) / 2, vy + vh - 55, hotbarWidth, 45);
        
        // Menu buttons at bottom-right (can be in black border area)
        var btnSize = 36;
        var btnSpacing = 40;
        var btnY = vy + vh + 5; // Just below viewport
        var btnStartX = vx + vw - 260;
        
        _inventoryBtnRect = new Rectangle(btnStartX, btnY, btnSize, btnSize);
        _equipmentBtnRect = new Rectangle(btnStartX + btnSpacing, btnY, btnSize, btnSize);
        _skillsBtnRect = new Rectangle(btnStartX + btnSpacing * 2, btnY, btnSize, btnSize);
        _spellbookBtnRect = new Rectangle(btnStartX + btnSpacing * 3, btnY, btnSize, btnSize);
        _settingsBtnRect = new Rectangle(btnStartX + btnSpacing * 4, btnY, btnSize, btnSize);
        _helpBtnRect = new Rectangle(btnStartX + btnSpacing * 5, btnY, btnSize, btnSize);
    }
    
    public void Update(XnaGameTime gameTime)
    {
        var elapsed = gameTime.TotalGameTime.TotalSeconds;
        var totalTimeMs = gameTime.TotalGameTime.TotalMilliseconds;
        
        // CRITICAL: Ensure camera viewport matches game viewport for correct click detection
        _camera.SetViewportSize(_viewportWidth, _viewportHeight);
        
        // Update gump system first (handles input)
        _gumpManager?.Update(gameTime, _input);
        
        // Handle viewport drag and resize
        UpdateViewportDragAndResize();
        
        // Update game state (handles movement timeout etc.)
        _gameState.Update(totalTimeMs);
        
        // Camera follows player and updates smoothly
        if (_gameState.Player != null)
        {
            _camera.Follow(_gameState.Player.Position);
        }
        _camera.Update(gameTime);
        
        UpdateOverheadTexts(gameTime);
        
        // Handle Enter to toggle chat mode
        if (_input.IsKeyPressed(Keys.Enter))
        {
            if (_chatActive)
            {
                if (_chatInput.Length > 0)
                    SendChatMessage(elapsed);
                _chatActive = false;
            }
            else
            {
                _chatActive = true;
                _chatInput = "";
            }
        }
        
        // Handle Escape
        if (_input.IsKeyPressed(Keys.Escape))
        {
            if (_awaitingSpellTarget)
            {
                _awaitingSpellTarget = false;
            }
            else if (_chatActive)
            {
                _chatActive = false;
                _chatInput = "";
            }
            else if (_targetEntityId.HasValue)
            {
                _targetEntityId = null;
            }
            else
            {
                CloseTopWindow();
            }
        }
        
        // Toggle combat mode with Tab
        if (_input.IsKeyPressed(Keys.Tab))
        {
            _combatMode = !_combatMode;
            if (_gameState.Player != null)
            {
                if (_combatMode)
                    AddOverheadText(_gameState.Player.Id, "War Mode", XnaColor.Red);
                else
                    AddOverheadText(_gameState.Player.Id, "Peace Mode", XnaColor.Green);
            }
        }
        
        // Update spell casting
        UpdateSpellCasting(gameTime);
        
        // Update sub-panels
        _paperdoll?.Update(gameTime);
        _inventory?.Update(gameTime);
        _skillsPanel?.Update(gameTime);
        _spellbook?.Update(gameTime);
        _settings?.Update(gameTime);
        _help?.Update(gameTime);
        
        if (_chatActive)
        {
            HandleChatInput();
        }
        else
        {
            HandleGameplayInput(gameTime);
        }
        
        HandleHotbarDrop();
        HandleMenuButtons();
        HandleStatBarDrag();
        HandleTargetBarDragging();
        UpdateCombat(gameTime);
        
        // Update camera to follow player (using isometric screen position)
        if (_gameState.Player != null)
        {
            _camera.Follow(_gameState.Player.Position);
            _camera.Position = _camera.TargetPosition; // Instant follow for tile movement
        }
    }
    
    private void HandleStatBarDrag()
    {
        var mousePos = _input.MousePosition;
        
        if (_input.IsLeftMousePressed && _ui.IsInside(_statBarPanel, mousePos))
        {
            _isDraggingStatBar = true;
            _statBarDragOffset = mousePos - new Vector2(_statBarPanel.X, _statBarPanel.Y);
        }
        
        if (_isDraggingStatBar)
        {
            if (_input.IsLeftMouseDown)
            {
                // Calculate new panel position (screen coords)
                var newPos = mousePos - _statBarDragOffset;
                // Clamp to full window (can drag outside viewport into black border)
                newPos.X = Math.Clamp(newPos.X, 0, _windowWidth - _statBarPanel.Width);
                newPos.Y = Math.Clamp(newPos.Y, 0, _windowHeight - _statBarPanel.Height);
                
                // Convert back to viewport-relative position for storage
                _statBarPosition = new Vector2(newPos.X - _viewportRect.X, newPos.Y - _viewportRect.Y);
                UpdateLayout();
            }
            else
            {
                _isDraggingStatBar = false;
            }
        }
    }
    
    private void HandleTargetBarDragging()
    {
        var mousePos = _input.MousePosition;
        const float DragThreshold = 20f; // Pixels before bar appears
        
        // Start potential drag on entity
        if (_input.IsLeftMousePressed && !_isDraggingStatBar && !_entityBeingDragged.HasValue)
        {
            var viewportPos = MouseToViewportCoords(mousePos);
            var clickWorld = _camera.ScreenToWorld(viewportPos);
            foreach (var entity in _gameState.GetAllEntities())
            {
                if (entity.Id == _gameState.PlayerEntityId) continue;
                if (entity is not Mobile) continue;
                
                var dist = entity.Position.DistanceTo(clickWorld);
                if (dist < 1.5f)
                {
                    _entityBeingDragged = entity.Id;
                    _entityDragStartPos = mousePos;
                    // Don't create bar yet - wait for drag threshold
                    break;
                }
            }
        }
        
        // Check if drag threshold reached - then create bar
        if (_entityBeingDragged.HasValue && _input.IsLeftMouseDown && _draggingTargetBar == null)
        {
            var dragDist = Vector2.Distance(mousePos, _entityDragStartPos);
            if (dragDist >= DragThreshold)
            {
                // Create the target bar now
                if (!_targetBars.Any(t => t.EntityId == _entityBeingDragged.Value))
                {
                    var newBar = new TargetBar
                    {
                        EntityId = _entityBeingDragged.Value,
                        Position = mousePos - new Vector2(100, 30),
                        IsDragging = true,
                        DragOffset = new Vector2(100, 30)
                    };
                    _targetBars.Add(newBar);
                    _draggingTargetBar = newBar;
                }
            }
        }
        
        // Update dragged target bar position
        if (_draggingTargetBar != null && _input.IsLeftMouseDown)
        {
            _draggingTargetBar.Position = mousePos - _draggingTargetBar.DragOffset;
            // Clamp to full window (can drag outside viewport into black border)
            _draggingTargetBar.Position = new Vector2(
                Math.Clamp(_draggingTargetBar.Position.X, 0, _windowWidth - 200),
                Math.Clamp(_draggingTargetBar.Position.Y, 0, _windowHeight - 60));
        }
        
        // End drag
        if (_entityBeingDragged.HasValue && !_input.IsLeftMouseDown)
        {
            if (_draggingTargetBar != null)
            {
                _draggingTargetBar.IsDragging = false;
            }
            _entityBeingDragged = null;
            _draggingTargetBar = null;
        }
        
        // Drag existing target bars (not from entity)
        if (_input.IsLeftMousePressed && !_entityBeingDragged.HasValue)
        {
            foreach (var bar in _targetBars)
            {
                var barRect = new Rectangle((int)bar.Position.X, (int)bar.Position.Y, 200, 60);
                if (_ui.IsInside(barRect, mousePos))
                {
                    bar.IsDragging = true;
                    bar.DragOffset = mousePos - bar.Position;
                    _draggingTargetBar = bar;
                    break;
                }
            }
        }
        
        if (_draggingTargetBar != null)
        {
            if (_input.IsLeftMouseDown)
            {
                _draggingTargetBar.Position = mousePos - _draggingTargetBar.DragOffset;
                _draggingTargetBar.Position.X = Math.Clamp(_draggingTargetBar.Position.X, 0, 1080);
                _draggingTargetBar.Position.Y = Math.Clamp(_draggingTargetBar.Position.Y, 0, 660);
            }
            else
            {
                _draggingTargetBar.IsDragging = false;
                _draggingTargetBar = null;
            }
        }
        
        // Right-click to close target bar
        if (_input.IsRightMousePressed)
        {
            for (int i = _targetBars.Count - 1; i >= 0; i--)
            {
                var bar = _targetBars[i];
                var barRect = new Rectangle((int)bar.Position.X, (int)bar.Position.Y, 200, 60);
                if (_ui.IsInside(barRect, mousePos))
                {
                    _targetBars.RemoveAt(i);
                    break;
                }
            }
        }
        
        // Remove bars for dead/despawned entities
        _targetBars.RemoveAll(bar => 
        {
            var entity = _gameState.GetEntity(bar.EntityId) as Mobile;
            return entity == null || entity.Health <= 0;
        });
    }
    
    private void UpdateSpellCasting(XnaGameTime gameTime)
    {
        if (!_isCasting) return;
        
        var elapsed = gameTime.TotalGameTime.TotalSeconds;
        var progress = (elapsed - _castStartTime) / _castDuration;
        
        if (progress >= 1.0)
        {
            // Cast complete - enter targeting mode
            _isCasting = false;
            _awaitingSpellTarget = true;
            _pendingSpellId = _castingSpellId;
            if (_gameState.Player != null)
                AddOverheadText(_gameState.Player.Id, "Select Target", XnaColor.Cyan);
        }
    }
    
    private void StartSpellCast(int spellId, string spellName)
    {
        if (_isCasting || _awaitingSpellTarget) return;
        
        var spellInfo = SpellDefinitions.GetSpell(spellId);
        
        _isCasting = true;
        _castStartTime = 0; // Will be set on first update
        _castingSpellId = spellId;
        _castingSpellName = spellInfo?.Name ?? spellName;
        _castDuration = spellInfo?.CastDelay ?? 1.0f;
        
        // Show casting words over head
        var words = spellInfo?.Words ?? "Rel Wis";
        if (_gameState.Player != null)
            AddOverheadText(_gameState.Player.Id, words, XnaColor.Cyan);
    }
    
    private string GetSpellWords(int spellId)
    {
        var spell = SpellDefinitions.GetSpell(spellId);
        return spell?.Words ?? "Rel Wis";
    }
    
    private float GetSpellCastTime(int spellId)
    {
        var spell = SpellDefinitions.GetSpell(spellId);
        return spell?.CastDelay ?? 1.0f;
    }
    
    private int GetSpellManaCost(int spellId)
    {
        var spell = SpellDefinitions.GetSpell(spellId);
        return spell?.ManaCost ?? 10;
    }
    
    private string GetSpellName(int spellId)
    {
        var spell = SpellDefinitions.GetSpell(spellId);
        return spell?.Name ?? "Unknown";
    }
    
    private void CastSpellOnTarget(EntityId targetId)
    {
        _awaitingSpellTarget = false;
        
        // Send spell cast to server
        _ = _gameState.CastSpellAsync((ushort)_pendingSpellId, targetId);
        
        // Visual feedback
        var target = _gameState.GetEntity(targetId);
        if (target != null)
        {
            var spellName = GetSpellName(_pendingSpellId);
            AddOverheadText(targetId, $"*{spellName}*", XnaColor.Magenta);
            
            // Spawn spell particles at target
            var targetScreen = WorldToScreen(target.Position);
            SpawnSpellParticles(_pendingSpellId, targetScreen);
        }
    }
    
    private void SpawnSpellParticles(int spellId, Vector2 position)
    {
        var spell = SpellDefinitions.GetSpell(spellId);
        var color = spell?.DamageType switch
        {
            Shared.Skills.DamageType.Fire => XnaColor.Orange,
            Shared.Skills.DamageType.Cold => XnaColor.Cyan,
            Shared.Skills.DamageType.Lightning => XnaColor.Yellow,
            Shared.Skills.DamageType.Poison => XnaColor.LimeGreen,
            _ => XnaColor.Magenta
        };
        
        var particleCount = 15 + (spell?.Circle ?? 1) * 5; // More particles for higher circle spells
        var rand = new Random();
        
        for (int i = 0; i < particleCount; i++)
        {
            var angle = rand.NextDouble() * Math.PI * 2;
            var speed = 50 + rand.NextDouble() * 100;
            var lifetime = 0.5f + (float)rand.NextDouble() * 0.5f;
            
            _particles.Add(new Particle
            {
                Position = position + new Vector2(rand.Next(-10, 10), rand.Next(-10, 10)),
                Velocity = new Vector2((float)(Math.Cos(angle) * speed), (float)(Math.Sin(angle) * speed)),
                Color = color,
                Size = 2 + rand.Next(4),
                Lifetime = lifetime,
                TimeRemaining = lifetime,
                Gravity = 50f
            });
        }
    }
    
    private void UpdateOverheadTexts(XnaGameTime gameTime)
    {
        var toRemove = new List<OverheadText>();
        foreach (var text in _overheadTexts)
        {
            text.TimeRemaining -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (text.TimeRemaining <= 0)
                toRemove.Add(text);
        }
        foreach (var text in toRemove)
            _overheadTexts.Remove(text);
    }
    
    private bool CloseTopWindow()
    {
        if (_help?.IsVisible == true) { _help.Hide(); return true; }
        if (_settings?.IsVisible == true) { _settings.Hide(); return true; }
        if (_spellbook?.IsVisible == true) { _spellbook.Hide(); return true; }
        if (_skillsPanel?.IsVisible == true) { _skillsPanel.Hide(); return true; }
        if (_inventory?.IsVisible == true) { _inventory.Hide(); return true; }
        if (_paperdoll?.IsVisible == true) { _paperdoll.Hide(); return true; }
        return false;
    }
    
    private void HandleGameplayInput(XnaGameTime gameTime)
    {
        bool mouseOverUI = IsMouseOverUI();
        bool mouseOutsideViewport = !IsInsideViewport(_input.MousePosition);
        
        // Update hovered entity (for highlighting)
        _hoveredEntityId = null;
        if (!mouseOverUI && !mouseOutsideViewport)
        {
            _hoveredEntityId = GetEntityAtMouse();
        }
        
        // UI toggles
        if (_input.IsKeyPressed(Keys.I)) _inventory?.Toggle();
        if (_input.IsKeyPressed(Keys.E) || _input.IsKeyPressed(Keys.P)) _paperdoll?.Toggle();
        if (_input.IsKeyPressed(Keys.K)) _skillsPanel?.Toggle();
        if (_input.IsKeyPressed(Keys.B)) _spellbook?.Toggle();
        if (_input.IsKeyPressed(Keys.O)) _settings?.Toggle();
        if (_input.IsKeyPressed(Keys.F1)) _help?.Toggle();
        
        // Admin panel - only for GM+ (AccessLevel >= 2)
        if (_input.IsKeyPressed(Keys.F12))
        {
            if (_gameState.Player != null && _gameState.Player.IsStaff)
            {
                _showAdminPanel = !_showAdminPanel;
            }
            else
            {
                // Non-staff get a message
                AddOverheadText(_gameState.PlayerEntityId ?? new EntityId(0), "Access denied", XnaColor.Red);
            }
        }
        
        // Camera zoom (only inside viewport and not over UI)
        if (!mouseOverUI && !mouseOutsideViewport)
        {
            var scroll = _input.ScrollWheelDelta;
            if (scroll != 0)
                _camera.Zoom = MathHelper.Clamp(_camera.Zoom + scroll * 0.001f, 0.5f, 2.0f);
            
            // Handle admin mode clicks first
            if (_adminMode != AdminMode.None && _input.IsLeftMousePressed)
            {
                HandleAdminModeClick();
            }
            else if (_input.IsLeftMousePressed)
            {
                // If awaiting spell target, cast on clicked entity or target bar
                if (_awaitingSpellTarget)
                {
                    // First check target bars
                    var barTarget = GetEntityFromTargetBarAtMouse();
                    if (barTarget.HasValue)
                    {
                        CastSpellOnTarget(barTarget.Value);
                    }
                    else
                    {
                        // Then check entities in world
                        var targetEntity = GetEntityAtMouse();
                        if (targetEntity.HasValue)
                        {
                            CastSpellOnTarget(targetEntity.Value);
                        }
                    }
                }
                else
                {
                    TryTargetEntityAtMouse();
                }
            }
        }
        
        // Handle Escape to cancel admin mode
        if (_adminMode != AdminMode.None && _input.IsKeyPressed(Keys.Escape))
        {
            _adminMode = AdminMode.None;
            _adminModeMulti = false;
            _showAddMenu = false;
            ShowSystemMessage("Mode cancelled", XnaColor.Gray);
        }
        
        // Don't allow movement while casting
        if (_isCasting)
        {
            // Skip all movement handling
        }
        else
        {
            // Hold right mouse for continuous movement toward cursor (UO style)
            // Only when inside viewport
            if (_input.IsRightMouseDown && !mouseOverUI && !mouseOutsideViewport)
            {
                UpdateHoldToMoveMovement(gameTime);
            }
            
            // Hold right mouse or shift to run
            _isRunning = _input.IsRightMouseDown || _input.IsShiftDown;
            
            // Keyboard movement (always available)
            var (kdx, kdy) = _input.GetMovementDirection();
            if (kdx != 0 || kdy != 0)
            {
                HandleKeyboardMovement(gameTime);
            }
        }
        
        // Hotbar keys (1-0)
        for (int i = 0; i < 10; i++)
        {
            var key = i == 9 ? Keys.D0 : (Keys)((int)Keys.D1 + i);
            if (_input.IsKeyPressed(key))
                UseHotbarSlot(i);
        }
    }
    
    /// <summary>
    /// Convert screen mouse position to viewport-relative position for world coordinate conversion
    /// </summary>
    private Vector2 MouseToViewportCoords(Vector2 screenPos)
    {
        return new Vector2(
            screenPos.X - _viewportRect.X,
            screenPos.Y - _viewportRect.Y
        );
    }
    
    private EntityId? GetEntityAtMouse()
    {
        var mousePos = _input.MousePosition;
        // Convert to viewport-relative coords for world conversion
        var viewportPos = MouseToViewportCoords(mousePos);
        var clickWorld = _camera.ScreenToWorld(viewportPos);
        
        foreach (var entity in _gameState.GetAllEntities())
        {
            if (entity.Id == _gameState.PlayerEntityId) continue;
            if (entity is not Mobile) continue;
            
            var dist = entity.Position.DistanceTo(clickWorld);
            if (dist < 1.5f)
            {
                return entity.Id;
            }
        }
        return null;
    }
    
    private EntityId? GetEntityFromTargetBarAtMouse()
    {
        var mousePos = _input.MousePosition;
        foreach (var bar in _targetBars)
        {
            var barRect = new Rectangle((int)bar.Position.X, (int)bar.Position.Y, 200, 60);
            if (_ui.IsInside(barRect, mousePos))
            {
                return bar.EntityId;
            }
        }
        return null;
    }
    
    // Movement timing - 60% faster than before
    private const double WalkInterval = 0.10;  // Walking speed (was 0.25)
    private const double RunInterval = 0.06;   // Running speed (was 0.15)
    
    private void UpdateHoldToMoveMovement(XnaGameTime gameTime)
    {
        if (_gameState.Player == null) return;
        
        var elapsed = gameTime.TotalGameTime.TotalSeconds;
        var interval = RunInterval; // Always run when holding
        if (elapsed - _lastMoveTime < interval) return;
        
        // Get screen direction from center to mouse cursor
        var mousePos = _input.MousePosition;
        var viewportPos = MouseToViewportCoords(mousePos);
        var screenCenter = new Vector2(_viewportWidth / 2f, _viewportHeight / 2f);
        var screenDir = viewportPos - screenCenter;
        
        // Only move if cursor is far enough from center
        if (screenDir.LengthSquared() < 900) return; // 30 pixel deadzone
        
        // Convert screen direction to world direction using isometric transform
        // In isometric: screenX = (worldX - worldY) * 22, screenY = (worldX + worldY) * 22
        // Inverse: worldX = (screenX + screenY) / 44, worldY = (screenY - screenX) / 44
        // For direction vectors, we just need the ratio, so we can simplify:
        float worldDirX = screenDir.X + screenDir.Y;  // Proportional to world X direction
        float worldDirY = screenDir.Y - screenDir.X;  // Proportional to world Y direction
        
        // Get the 8-directional movement from world direction
        var direction = DirectionExtensions.FromVector(worldDirX, worldDirY);
        var (dx, dy) = direction.GetOffset();
        
        if (dx != 0 || dy != 0)
        {
            _lastMoveTime = elapsed;
            _ = _gameState.MoveAsync(direction, true);
        }
    }
    
    private void HandleKeyboardMovement(XnaGameTime gameTime)
    {
        if (_gameState.Player == null) return;
        
        var elapsed = gameTime.TotalGameTime.TotalSeconds;
        var interval = _isRunning ? RunInterval : WalkInterval;
        if (elapsed - _lastMoveTime < interval) return;
        
        var (dx, dy) = _input.GetMovementDirection();
        if (dx != 0 || dy != 0)
        {
            _lastMoveTime = elapsed;
            _ = _gameState.MoveAsync(DirectionHelper.FromOffset(dx, dy), _isRunning);
        }
    }
    
    private void TryTargetEntityAtMouse()
    {
        // Find entity under mouse cursor
        var mousePos = _input.MousePosition;
        var viewportPos = MouseToViewportCoords(mousePos);
        var clickWorld = _camera.ScreenToWorld(viewportPos);
        
        Entity? closest = null;
        float closestDist = float.MaxValue;
        
        foreach (var entity in _gameState.GetAllEntities())
        {
            if (entity.Id == _gameState.PlayerEntityId) continue;
            if (entity is not Mobile) continue;
            
            // Check distance from click to entity
            var dist = entity.Position.DistanceTo(clickWorld);
            if (dist < 1.5f && dist < closestDist) // Within 1.5 tiles of click
            {
                closest = entity;
                closestDist = dist;
            }
        }
        
        if (closest != null)
        {
            // Check if ghost clicking on healer or ankh for resurrection
            if (_isGhost && closest is NpcEntity npc)
            {
                if (npc.Name == "Healer" || npc.Name == "Ankh")
                {
                    // Resurrect!
                    Resurrect();
                    AddOverheadText(npc.Id, "You have been resurrected!", XnaColor.LightGreen);
                    return;
                }
            }
            
            _targetEntityId = closest.Id;
        }
    }
    
    private void Resurrect()
    {
        _isGhost = false;
        _isDead = false;
        _deathFadeProgress = 0f;
        
        // Request resurrection from server
        _ = _gameState.ResurrectAsync();
        
        if (_gameState.Player != null)
        {
            // Restore some health locally (server will sync actual values)
            _gameState.Player.Health = _gameState.Player.MaxHealth / 2;
            AddOverheadText(_gameState.Player.Id, "Resurrected!", XnaColor.LightGreen);
        }
    }
    
    private void CycleTarget()
    {
        var entities = _gameState.GetAllEntities()
            .Where(e => e.Id != _gameState.PlayerEntityId && e is Mobile)
            .ToList();
        
        if (entities.Count == 0) { _targetEntityId = null; return; }
        
        if (!_targetEntityId.HasValue)
            _targetEntityId = entities[0].Id;
        else
        {
            var idx = entities.FindIndex(e => e.Id == _targetEntityId);
            _targetEntityId = entities[(idx + 1) % entities.Count].Id;
        }
    }
    
    private void UpdateCombat(XnaGameTime gameTime)
    {
        if (!_targetEntityId.HasValue || _gameState.Player == null) return;
        
        var target = _gameState.GetEntity(_targetEntityId.Value) as Mobile;
        if (target == null || target.Health <= 0) { _targetEntityId = null; return; }
        
        var distance = _gameState.Player.Position.DistanceTo(target.Position);
        if (distance <= 2.0f)
        {
            var elapsed = gameTime.TotalGameTime.TotalSeconds;
            if (elapsed - _lastAttackTime >= AttackInterval)
            {
                _lastAttackTime = elapsed;
                // Send real attack packet
                _ = _gameState.AttackAsync(_targetEntityId.Value);
            }
        }
    }
    
    private void OnDamageDealt(EntityId attackerId, EntityId targetId, int damage, bool isCritical)
    {
        // Add floating damage number
        var entity = _gameState.GetEntity(targetId);
        if (entity != null)
        {
            var screenPos = WorldToScreen(entity.Position);
            var color = isCritical ? XnaColor.Yellow : XnaColor.Red;
            _floatingDamage.Add(new FloatingDamage
            {
                Position = new Vector2(screenPos.X, screenPos.Y - 40),
                Text = isCritical ? $"-{damage}!" : $"-{damage}",
                Color = color,
                TimeRemaining = 2.0f,
                VelocityY = -50f // Float upward
            });
        }
        
        // Check if player died
        if (targetId == _gameState.PlayerEntityId && _gameState.Player != null && _gameState.Player.Health <= 0)
        {
            OnPlayerDeath();
        }
    }
    
    private void OnPlayerDeath()
    {
        _isDead = true;
        _deathFadeProgress = 0f;
        _isGhost = true;
        _combatMode = false;
        _targetEntityId = null;
    }
    
    private void OnEntityDied(EntityId entityId, EntityId killerId)
    {
        // Add death text
        var entity = _gameState.GetEntity(entityId);
        if (entity != null)
        {
            var screenPos = WorldToScreen(entity.Position);
            _floatingDamage.Add(new FloatingDamage
            {
                Position = new Vector2(screenPos.X, screenPos.Y - 50),
                Text = "DEAD",
                Color = XnaColor.DarkRed,
                TimeRemaining = 3.0f,
                VelocityY = -20f
            });
        }
        
        if (entityId == _targetEntityId)
        {
            _targetEntityId = null;
        }
        
        // Check if it's the player
        if (entityId == _gameState.PlayerEntityId)
        {
            OnPlayerDeath();
        }
    }
    
    private void OnSpellCast(EntityId casterId, ushort spellId, int damage, int heal)
    {
        var spellName = GetSpellName(spellId);
        AddOverheadText(casterId, spellName + "!", XnaColor.Cyan);
        
        // Heal shown as green floating number
        if (heal > 0)
        {
            var entity = _gameState.GetEntity(casterId);
            if (entity != null)
            {
                var screenPos = WorldToScreen(entity.Position);
                _floatingDamage.Add(new FloatingDamage
                {
                    Position = new Vector2(screenPos.X, screenPos.Y - 40),
                    Text = $"+{heal}",
                    Color = XnaColor.LightGreen,
                    TimeRemaining = 2.0f,
                    VelocityY = -40f
                });
            }
        }
    }
    
    private bool IsMouseOverUI()
    {
        var mousePos = _input.MousePosition;
        
        // Check server-side gumps first
        if (_gumpManager?.IsMouseOverGump((int)mousePos.X, (int)mousePos.Y) == true) return true;
        
        if (_paperdoll?.IsMouseOver == true) return true;
        if (_inventory?.IsMouseOver == true) return true;
        if (_skillsPanel?.IsMouseOver == true) return true;
        if (_spellbook?.IsMouseOver == true) return true;
        if (_settings?.IsMouseOver == true) return true;
        if (_help?.IsMouseOver == true) return true;
        if (_chatActive && _ui.IsInside(_chatInputRect, mousePos)) return true;
        if (_ui.IsInside(_minimapRect, mousePos)) return true;
        if (_ui.IsInside(_hotbarRect, mousePos)) return true;
        return false;
    }
    
    private void HandleHotbarDrop()
    {
        if (!_isDraggingToHotbar) return;
        
        if (!_input.IsLeftMouseDown)
        {
            var mousePos = _input.MousePosition;
            if (_ui.IsInside(_hotbarRect, mousePos))
            {
                var slotX = (int)(mousePos.X - _hotbarRect.X - 10) / 42;
                if (slotX >= 0 && slotX < 10)
                {
                    switch (_dragType)
                    {
                        case HotbarSlotType.Item: _hotbar[slotX].SetItem(_dragId, _dragName, _dragIcon); break;
                        case HotbarSlotType.Spell: _hotbar[slotX].SetSpell(_dragId, _dragName, _dragIcon); break;
                        case HotbarSlotType.Skill: _hotbar[slotX].SetSkill(_dragId, _dragName, _dragIcon); break;
                    }
                }
            }
            _isDraggingToHotbar = false;
        }
    }
    
    private void HandleMenuButtons()
    {
        if (!_input.IsLeftMousePressed) return;
        var mousePos = _input.MousePosition;
        
        if (_ui.IsInside(_inventoryBtnRect, mousePos)) _inventory?.Toggle();
        if (_ui.IsInside(_equipmentBtnRect, mousePos)) _paperdoll?.Toggle();
        if (_ui.IsInside(_skillsBtnRect, mousePos)) _skillsPanel?.Toggle();
        if (_ui.IsInside(_spellbookBtnRect, mousePos)) _spellbook?.Toggle();
        if (_ui.IsInside(_settingsBtnRect, mousePos)) _settings?.Toggle();
        if (_ui.IsInside(_helpBtnRect, mousePos)) _help?.Toggle();
    }
    
    private void UseHotbarSlot(int slot)
    {
        var s = _hotbar[slot];
        if (s.IsEmpty) return;
        
        if (s.Type == HotbarSlotType.Spell)
            CastSpell(s);
        else if (_gameState.Player != null)
            AddOverheadText(_gameState.Player.Id, $"Using {s.Name}!", XnaColor.Yellow);
    }
    
    private void CastSpell(HotbarSlot spell)
    {
        if (_gameState.Player == null) return;
        if (_isCasting || _awaitingSpellTarget) return;
        
        var spellInfo = SpellDefinitions.GetSpell(spell.Id);
        var manaCost = spellInfo?.ManaCost ?? 10;
        
        if (_gameState.Player.Mana < manaCost)
        {
            AddOverheadText(_gameState.Player.Id, "Not enough mana!", XnaColor.Blue);
            return;
        }
        
        // Check spell target type
        if (spellInfo != null)
        {
            // Self-target spells cast immediately
            if (spellInfo.TargetType == SpellTarget.Self)
            {
                _ = _gameState.CastSpellAsync((ushort)spell.Id, _gameState.Player.Id);
                AddOverheadText(_gameState.Player.Id, spellInfo.Words, XnaColor.Cyan);
                return;
            }
            
            // Friendly spells on self (like Heal, Greater Heal)
            if (spellInfo.TargetType == SpellTarget.Friendly && spellInfo.Effect == SpellEffect.Heal)
            {
                _ = _gameState.CastSpellAsync((ushort)spell.Id, _gameState.Player.Id);
                AddOverheadText(_gameState.Player.Id, spellInfo.Words, XnaColor.Cyan);
                return;
            }
        }
        
        // Offensive/targeted spells need casting time and target selection
        StartSpellCast(spell.Id, spell.Name);
    }
    
    private void OnInventoryDragStart(int slot, Item item)
    {
        _isDraggingToHotbar = true;
        _dragType = HotbarSlotType.Item;
        _dragId = slot;
        _dragName = item.Name;
        _dragIcon = "#";
    }
    
    private void OnSpellDragStart(ushort spellId)
    {
        _isDraggingToHotbar = true;
        _dragType = HotbarSlotType.Spell;
        _dragId = spellId;
        _dragName = GetSpellName(spellId);
        _dragIcon = GetSpellIcon(spellId);
    }
    
    private string GetSpellIcon(int id) => id switch { 5 => ">", 18 => "*", 29 => "+", 42 => "!", _ => "o" };
    
    private void HandleChatInput()
    {
        foreach (var key in _input.GetPressedKeys())
        {
            if (!_input.IsKeyPressed(key)) continue;
            if (key == Keys.Back && _chatInput.Length > 0) _chatInput = _chatInput[..^1];
            else if (_chatInput.Length < MaxChatLength)
            {
                if (key >= Keys.A && key <= Keys.Z)
                    _chatInput += _input.IsShiftDown ? (char)('A' + key - Keys.A) : (char)('a' + key - Keys.A);
                else if (key >= Keys.D0 && key <= Keys.D9)
                    _chatInput += (char)('0' + key - Keys.D0);
                else if (key == Keys.Space) _chatInput += ' ';
                else if (key == Keys.OemPeriod) _chatInput += '.';
                else if (key == Keys.OemComma) _chatInput += ',';
                else if (key == Keys.OemQuestion) _chatInput += _input.IsShiftDown ? '?' : '/';
            }
        }
    }
    
    private void SendChatMessage(double currentTime)
    {
        if (string.IsNullOrWhiteSpace(_chatInput) || currentTime - _lastChatTime < ChatRateLimit) return;
        
        var msg = _chatInput.Trim();
        _chatInput = "";
        _lastChatTime = currentTime;
        
        // Check for commands
        if (msg.StartsWith("/"))
        {
            ProcessCommand(msg);
            return;
        }
        
        var channel = ChatChannel.Local;
        if (msg.StartsWith("/g ")) { channel = ChatChannel.Global; msg = msg[3..]; }
        else if (msg.StartsWith("/p ")) { channel = ChatChannel.Party; msg = msg[3..]; }
        
        _ = _gameState.SendChatAsync(channel, msg);
        
        if (channel == ChatChannel.Local && _gameState.Player != null)
            AddOverheadText(_gameState.Player.Id, msg, XnaColor.White);
    }
    
    /// <summary>
    /// Process admin commands. /command for single use, /m command for multi-click mode
    /// </summary>
    private void ProcessCommand(string input)
    {
        var parts = input.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;
        
        var cmd = parts[0];
        bool isMulti = cmd == "/m";
        
        if (isMulti && parts.Length < 2)
        {
            ShowSystemMessage("Usage: /m <command> - enables multi-click mode", XnaColor.Yellow);
            return;
        }
        
        var actualCmd = isMulti ? parts[1] : cmd.TrimStart('/');
        var args = isMulti ? parts.Skip(2).ToArray() : parts.Skip(1).ToArray();
        
        switch (actualCmd)
        {
            case "teleport":
            case "tp":
                HandleTeleportCommand(args, isMulti);
                break;
                
            case "kill":
                HandleKillCommand(args, isMulti);
                break;
                
            case "add":
                HandleAddCommand(args, isMulti);
                break;
                
            case "spawn":
                HandleSpawnCommand(args);
                break;
                
            case "cancel":
            case "c":
                _adminMode = AdminMode.None;
                _adminModeMulti = false;
                _showAddMenu = false;
                ShowSystemMessage("Command mode cancelled", XnaColor.Gray);
                break;
                
            case "help":
            case "?":
                ShowCommandHelp();
                break;
                
            default:
                // Forward unknown commands to server
                _ = _gameState.SendChatAsync(ChatChannel.Local, input);
                break;
        }
    }
    
    private void HandleTeleportCommand(string[] args, bool isMulti)
    {
        if (args.Length >= 2 && int.TryParse(args[0], out int x) && int.TryParse(args[1], out int y))
        {
            // Immediate teleport to coordinates
            TeleportPlayer(x, y);
            if (!isMulti) return;
        }
        
        // Enable teleport mode
        _adminMode = AdminMode.Teleport;
        _adminModeMulti = isMulti;
        ShowSystemMessage(isMulti ? "TELEPORT MODE: Click to teleport (click or /cancel to exit)" : 
            "Click destination to teleport", XnaColor.Cyan);
    }
    
    private void HandleKillCommand(string[] args, bool isMulti)
    {
        _adminMode = AdminMode.Kill;
        _adminModeMulti = isMulti;
        ShowSystemMessage(isMulti ? "KILL MODE: Click entities to kill (/cancel to exit)" : 
            "Click an entity to kill", XnaColor.Red);
    }
    
    private void HandleAddCommand(string[] args, bool isMulti)
    {
        if (args.Length > 0)
        {
            // Direct add with type specified
            _adminModeParam = args[0];
            _adminMode = AdminMode.AddNpc;
            _adminModeMulti = isMulti;
            ShowSystemMessage($"ADD MODE [{_adminModeParam}]: Click to place (/cancel to exit)", XnaColor.Green);
        }
        else
        {
            // Show add menu
            _showAddMenu = true;
            _adminMode = AdminMode.AddNpc;
            _adminModeMulti = isMulti;
            ShowSystemMessage("Select entity type from menu, then click to place", XnaColor.Green);
        }
    }
    
    private void HandleSpawnCommand(string[] args)
    {
        if (args.Length < 1)
        {
            ShowSystemMessage("Usage: /spawn <type> [count] - spawn NPCs at your location", XnaColor.Yellow);
            return;
        }
        
        var typeName = args[0].ToLower();
        int count = args.Length > 1 && int.TryParse(args[1], out int c) ? Math.Min(c, 10) : 1;
        
        // Find matching NPC type
        int typeId = -1;
        for (int i = 0; i < _npcTypes.Length; i++)
        {
            if (_npcTypes[i].ToLower().StartsWith(typeName))
            {
                typeId = _npcTypeIds[i];
                typeName = _npcTypes[i];
                break;
            }
        }
        
        if (typeId == -1)
        {
            ShowSystemMessage($"Unknown type: {args[0]}. Types: {string.Join(", ", _npcTypes)}", XnaColor.Red);
            return;
        }
        
        // Spawn at player location
        if (_gameState.Player != null)
        {
            for (int i = 0; i < count; i++)
            {
                var offset = new WorldPosition(i % 3 - 1, i / 3 - 1, 0);
                var pos = _gameState.Player.Position + offset;
                _ = _gameState.AdminSpawnNpcAsync(typeName, typeId, pos);
            }
            ShowSystemMessage($"Spawned {count}x {typeName}", XnaColor.Green);
        }
    }
    
    private void ShowCommandHelp()
    {
        ShowSystemMessage("=== Admin Commands ===", XnaColor.Yellow);
        ShowSystemMessage("/teleport [x y] or /tp - teleport mode", XnaColor.White);
        ShowSystemMessage("/kill - kill mode (click to kill)", XnaColor.White);
        ShowSystemMessage("/add [type] - add entity mode", XnaColor.White);
        ShowSystemMessage("/spawn <type> [count] - spawn at location", XnaColor.White);
        ShowSystemMessage("/m <cmd> - multi-click mode", XnaColor.White);
        ShowSystemMessage("/cancel or /c - exit current mode", XnaColor.White);
    }
    
    private void TeleportPlayer(float x, float y)
    {
        if (_gameState.Player != null)
        {
            _gameState.Player.Position = new WorldPosition(x, y, 0);
            _camera.Follow(_gameState.Player.Position);
            _camera.Position = _camera.TargetPosition;
            ShowSystemMessage($"Teleported to ({x:F0}, {y:F0})", XnaColor.Cyan);
        }
    }
    
    private void ShowSystemMessage(string message, XnaColor color)
    {
        if (_gameState.Player != null)
            AddOverheadText(_gameState.Player.Id, message, color);
    }
    
    /// <summary>
    /// Handle clicks when in an admin mode (teleport, kill, add)
    /// </summary>
    private void HandleAdminModeClick()
    {
        var mousePos = _input.MousePosition;
        var viewportPos = MouseToViewportCoords(mousePos);
        var worldPos = _camera.ScreenToWorld(viewportPos);
        
        switch (_adminMode)
        {
            case AdminMode.Teleport:
                TeleportPlayer(worldPos.X, worldPos.Y);
                if (!_adminModeMulti)
                {
                    _adminMode = AdminMode.None;
                }
                break;
                
            case AdminMode.Kill:
                var targetEntity = GetEntityAtMouse();
                if (targetEntity.HasValue)
                {
                    // Send kill command to server
                    _ = _gameState.AdminKillAsync(targetEntity.Value);
                    ShowSystemMessage("Killed entity", XnaColor.Red);
                }
                if (!_adminModeMulti)
                {
                    _adminMode = AdminMode.None;
                }
                break;
                
            case AdminMode.AddNpc:
                // Spawn NPC at click location
                int typeId = _adminSelectedNpcType;
                string typeName = _npcTypes[Array.IndexOf(_npcTypeIds, typeId)];
                
                // If param specified, try to match it
                if (!string.IsNullOrEmpty(_adminModeParam))
                {
                    for (int i = 0; i < _npcTypes.Length; i++)
                    {
                        if (_npcTypes[i].ToLower().StartsWith(_adminModeParam.ToLower()))
                        {
                            typeId = _npcTypeIds[i];
                            typeName = _npcTypes[i];
                            break;
                        }
                    }
                }
                
                _ = _gameState.AdminSpawnNpcAsync(typeName, typeId, worldPos);
                ShowSystemMessage($"Spawned {typeName}", XnaColor.Green);
                
                if (!_adminModeMulti)
                {
                    _adminMode = AdminMode.None;
                    _showAddMenu = false;
                }
                break;
        }
    }
    
    private void OnChatReceived(ChatChannel channel, string sender, string message)
    {
        if (channel == ChatChannel.Local)
        {
            foreach (var e in _gameState.GetAllEntities())
            {
                if (e.Name == sender && e.Id != _gameState.PlayerEntityId)
                {
                    AddOverheadText(e.Id, message, XnaColor.White);
                    break;
                }
            }
        }
    }
    
    private void OnSystemMessage(string message, SharedColor color)
    {
        if (_gameState.Player != null)
            AddOverheadText(_gameState.Player.Id, message, new XnaColor(color.R, color.G, color.B, color.A));
    }
    
    private void OnGumpReceived(GumpData gumpData)
    {
        _gumpManager?.OpenGump(gumpData);
    }
    
    private void OnGumpClosed(uint typeId, uint serial)
    {
        _gumpManager?.CloseGump(typeId, serial, sendResponse: false);
    }
    
    private void AddOverheadText(EntityId entityId, string text, XnaColor color)
    {
        _overheadTexts.Add(new OverheadText
        {
            EntityId = entityId,
            Text = text,
            Color = color,
            TimeRemaining = 4.0f,
            YOffset = _overheadTexts.Count(t => t.EntityId == entityId) * 18
        });
    }
    
    public void Draw(XnaGameTime gameTime)
    {
        // STEP 1: Render game world to viewport render target
        if (_gameViewport != null)
        {
            _graphicsDevice.SetRenderTarget(_gameViewport);
            _graphicsDevice.Clear(XnaColor.Black);
            
            // CRITICAL: Ensure camera viewport matches render target size for correct centering
            _camera.SetViewportSize(_viewportWidth, _viewportHeight);
            
            // Set entity highlight for hover effect
            _worldRenderer.HighlightedEntityId = _hoveredEntityId;
            if (_hoveredEntityId.HasValue)
            {
                var entity = _gameState.GetEntity(_hoveredEntityId.Value);
                if (entity is NpcEntity npc)
                {
                    _worldRenderer.HighlightColor = GetNotorietyColor(npc);
                }
                else
                {
                    _worldRenderer.HighlightColor = XnaColor.Gray;
                }
            }
            
            // Apply ghost/death visual effect
            if (_isGhost)
            {
                // Ghost mode - world is greyed out (handled by WorldRenderer tint)
            }
            
            _worldRenderer.Draw(_gameState, gameTime);
            
            // Reset render target to back buffer
            _graphicsDevice.SetRenderTarget(null);
        }
        
        // STEP 2: Clear screen with black (for border area)
        _graphicsDevice.Clear(XnaColor.Black);
        
        // STEP 3: Draw viewport to screen (centered)
        _ui.Begin();
        
        if (_gameViewport != null)
        {
            _ui.DrawTexture(_gameViewport, _viewportRect);
        }
        
        // Draw viewport border frame
        DrawViewportBorder();
        
        // Draw viewport resize handle (bottom-right corner of viewport)
        DrawViewportResizeHandle();
        
        // Draw entity highlights in combat mode (offset to viewport)
        DrawEntityHighlightsUI();
        
        // STEP 4: Draw UI elements (can be anywhere on screen)
        DrawExperienceBar();
        DrawPlayerStats();
        DrawTargetBars();
        DrawMinimap();
        DrawOverheadTexts();
        DrawFloatingDamage(gameTime);
        DrawParticles(gameTime);
        DrawHotbar();
        DrawMenuButtons();
        DrawCombatModeIndicator();
        DrawCastingBar(gameTime);
        DrawSpellTargetCursor();
        
        if (_chatActive)
            DrawChatInput();
        else
            _ui.DrawText("Press ENTER to chat", new Vector2(_viewportRect.X + 12, _viewportRect.Bottom - 35), new XnaColor(100, 100, 100), 1f);
        
        _paperdoll?.Draw(gameTime);
        _inventory?.Draw(gameTime);
        _skillsPanel?.Draw(gameTime);
        _spellbook?.Draw(gameTime);
        _settings?.Draw(gameTime);
        _help?.Draw(gameTime);
        
        // Draw admin panel
        if (_showAdminPanel)
        {
            DrawAdminPanel();
        }
        
        // Draw admin mode indicator and add menu
        if (_adminMode != AdminMode.None)
        {
            DrawAdminModeIndicator();
        }
        if (_showAddMenu)
        {
            DrawAddMenu();
        }
        
        // Draw death overlay on top of everything
        if (_isDead)
        {
            DrawDeathScreen(gameTime);
        }
        
        // Draw ghost indicator
        if (_isGhost && !_isDead)
        {
            DrawGhostIndicator();
        }
        
        // Draw viewport size/position indicator when dragging or resizing
        if (_isDraggingViewportResize)
        {
            var sizeText = $"{_viewportWidth}x{_viewportHeight}";
            _ui.DrawText(sizeText, new Vector2(_viewportRect.Right - 80, _viewportRect.Bottom + 5), XnaColor.White, 1.2f);
        }
        else if (_isDraggingViewport)
        {
            var posText = $"Position: {(int)_viewportPosition.X}, {(int)_viewportPosition.Y}";
            _ui.DrawText(posText, new Vector2(_viewportRect.X, _viewportRect.Y - 20), XnaColor.Yellow, 1.2f);
        }
        
        // Draw gumps (before cursor, after other UI)
        _gumpManager?.Draw(_ui.SpriteBatch);
        
        // Draw custom cursor last (on top of everything)
        DrawCustomCursor();
        
        _ui.End();
    }
    
    /// <summary>
    /// Draw the custom UO-style cursor
    /// </summary>
    private void DrawCustomCursor()
    {
        var mousePos = _input.MousePosition;
        
        // Determine which cursor to use
        Texture2D cursor;
        XnaColor tint = XnaColor.White;
        bool centerCursor = false;
        
        if (_awaitingSpellTarget)
        {
            // Spell targeting cursor
            cursor = _assets.GetCursor(false, true, false);
            tint = XnaColor.Cyan;
            centerCursor = true;
        }
        else if (_adminMode != AdminMode.None)
        {
            // Admin mode - use target cursor with mode-specific color
            cursor = _assets.GetCursor(false, true, _adminMode == AdminMode.Kill);
            tint = _adminMode switch
            {
                AdminMode.Teleport => XnaColor.Cyan,
                AdminMode.Kill => XnaColor.Red,
                AdminMode.AddNpc => XnaColor.LightGreen,
                _ => XnaColor.Yellow
            };
            centerCursor = true;
        }
        else
        {
            // Check if mouse is inside viewport for directional cursor
            bool insideViewport = IsInsideViewport(mousePos);
            
            if (insideViewport && _gameState.Player != null)
            {
                // Calculate direction from player to mouse cursor
                var playerScreenPos = WorldToScreen(_gameState.Player.Position);
                var toMouse = mousePos - playerScreenPos;
                
                // Only use directional cursor if far enough from player
                if (toMouse.LengthSquared() > 400) // 20 pixels squared
                {
                    float angle = MathF.Atan2(toMouse.Y, toMouse.X);
                    int direction = AssetManager.AngleToDirection(angle);
                    
                    cursor = _assets.GetDirectionalCursor(direction, _combatMode);
                    centerCursor = true; // Directional cursors should be centered
                    
                    if (_combatMode)
                    {
                        tint = new XnaColor(255, 150, 150); // Slight red tint in war mode
                    }
                }
                else
                {
                    // Too close to player, use normal cursor
                    cursor = _assets.GetCursor(_combatMode, false);
                    if (_combatMode) tint = new XnaColor(255, 150, 150);
                }
            }
            else
            {
                // Outside viewport or no player, use normal cursor
                cursor = _assets.GetCursor(_combatMode, false);
                if (_combatMode) tint = new XnaColor(255, 150, 150);
            }
        }
        
        // Draw cursor
        var cursorPos = mousePos;
        
        // Center the cursor if needed
        if (centerCursor)
        {
            cursorPos -= new Vector2(cursor.Width / 2, cursor.Height / 2);
        }
        
        _ui.DrawTexture(cursor, new Rectangle((int)cursorPos.X, (int)cursorPos.Y, cursor.Width, cursor.Height), tint);
    }
    
    /// <summary>
    /// Draw resize handle at bottom-right of viewport
    /// </summary>
    private void DrawViewportResizeHandle()
    {
        const int handleSize = 16;
        var handleRect = new Rectangle(
            _viewportRect.Right - handleSize,
            _viewportRect.Bottom - handleSize,
            handleSize, handleSize);
        
        // Draw diagonal lines to indicate resize handle
        var handleColor = _isDraggingViewportResize ? XnaColor.Yellow : new XnaColor(150, 120, 80);
        for (int i = 0; i < 3; i++)
        {
            int offset = 4 + i * 4;
            _ui.DrawLine(
                new Vector2(handleRect.Right - offset, handleRect.Bottom - 2),
                new Vector2(handleRect.Right - 2, handleRect.Bottom - offset),
                handleColor, 2);
        }
    }
    
    /// <summary>
    /// Draw an ornate border around the viewport (similar to classic UO)
    /// </summary>
    private void DrawViewportBorder()
    {
        const int BorderWidth = 4;
        
        // Highlight border when dragging
        XnaColor darkBorder, midBorder, lightBorder, innerShadow;
        if (_isDraggingViewport)
        {
            darkBorder = new XnaColor(80, 70, 40);
            midBorder = new XnaColor(140, 120, 60);
            lightBorder = new XnaColor(200, 180, 100);
            innerShadow = new XnaColor(60, 50, 30);
        }
        else
        {
            darkBorder = new XnaColor(40, 30, 20);
            midBorder = new XnaColor(80, 60, 40);
            lightBorder = new XnaColor(120, 100, 70);
            innerShadow = new XnaColor(20, 15, 10);
        }
        
        int x = _viewportRect.X;
        int y = _viewportRect.Y;
        int w = _viewportRect.Width;
        int h = _viewportRect.Height;
        
        // Outer dark border
        // Top
        _ui.DrawRectangle(new Rectangle(x, y, w, BorderWidth), darkBorder);
        // Bottom
        _ui.DrawRectangle(new Rectangle(x, y + h - BorderWidth, w, BorderWidth), darkBorder);
        // Left
        _ui.DrawRectangle(new Rectangle(x, y, BorderWidth, h), darkBorder);
        // Right
        _ui.DrawRectangle(new Rectangle(x + w - BorderWidth, y, BorderWidth, h), darkBorder);
        
        // Mid-tone decorative line (1 pixel inside)
        // Top
        _ui.DrawRectangle(new Rectangle(x + BorderWidth, y + BorderWidth, w - BorderWidth * 2, 2), midBorder);
        // Bottom
        _ui.DrawRectangle(new Rectangle(x + BorderWidth, y + h - BorderWidth - 2, w - BorderWidth * 2, 2), midBorder);
        // Left
        _ui.DrawRectangle(new Rectangle(x + BorderWidth, y + BorderWidth, 2, h - BorderWidth * 2), midBorder);
        // Right
        _ui.DrawRectangle(new Rectangle(x + w - BorderWidth - 2, y + BorderWidth, 2, h - BorderWidth * 2), midBorder);
        
        // Light highlight on top-left edges (3D effect)
        _ui.DrawRectangle(new Rectangle(x + BorderWidth + 2, y + BorderWidth + 2, w - BorderWidth * 2 - 4, 1), lightBorder);
        _ui.DrawRectangle(new Rectangle(x + BorderWidth + 2, y + BorderWidth + 2, 1, h - BorderWidth * 2 - 4), lightBorder);
        
        // Inner shadow on bottom-right edges (3D effect)
        _ui.DrawRectangle(new Rectangle(x + BorderWidth + 3, y + h - BorderWidth - 3, w - BorderWidth * 2 - 4, 1), innerShadow);
        _ui.DrawRectangle(new Rectangle(x + w - BorderWidth - 3, y + BorderWidth + 3, 1, h - BorderWidth * 2 - 4), innerShadow);
        
        // Corner decorations (small squares)
        var cornerSize = 8;
        // Top-left
        _ui.DrawRectangle(new Rectangle(x + 2, y + 2, cornerSize, cornerSize), lightBorder);
        _ui.DrawRectangleOutline(new Rectangle(x + 2, y + 2, cornerSize, cornerSize), darkBorder);
        // Top-right
        _ui.DrawRectangle(new Rectangle(x + w - cornerSize - 2, y + 2, cornerSize, cornerSize), lightBorder);
        _ui.DrawRectangleOutline(new Rectangle(x + w - cornerSize - 2, y + 2, cornerSize, cornerSize), darkBorder);
        // Bottom-left
        _ui.DrawRectangle(new Rectangle(x + 2, y + h - cornerSize - 2, cornerSize, cornerSize), lightBorder);
        _ui.DrawRectangleOutline(new Rectangle(x + 2, y + h - cornerSize - 2, cornerSize, cornerSize), darkBorder);
        // Bottom-right
        _ui.DrawRectangle(new Rectangle(x + w - cornerSize - 2, y + h - cornerSize - 2, cornerSize, cornerSize), lightBorder);
        _ui.DrawRectangleOutline(new Rectangle(x + w - cornerSize - 2, y + h - cornerSize - 2, cornerSize, cornerSize), darkBorder);
    }
    
    private void DrawAdminPanel()
    {
        var panelRect = new Rectangle(_windowWidth - 220, 100, 210, 320);
        _ui.DrawRectangle(panelRect, new XnaColor(30, 30, 40, 230));
        _ui.DrawRectangleOutline(panelRect, XnaColor.Gold);
        
        var y = panelRect.Y + 5;
        _ui.DrawTextCentered("ADMIN TOOLS", new Vector2(panelRect.X + panelRect.Width / 2, y), XnaColor.Gold, 1.5f);
        y += 22;
        
        // Show access level
        var accessLevel = _gameState.Player?.AccessLevel ?? Shared.Entities.AccessLevel.Player;
        var levelColor = accessLevel >= Shared.Entities.AccessLevel.Administrator ? XnaColor.Red : XnaColor.Yellow;
        _ui.DrawTextCentered($"[{accessLevel}]", new Vector2(panelRect.X + panelRect.Width / 2, y), levelColor, 1.2f);
        y += 20;
        
        _ui.DrawText("F12 to close", new Vector2(panelRect.X + 10, y), XnaColor.Gray, 1f);
        y += 25;
        
        // NPC spawn section
        _ui.DrawText("Spawn NPC:", new Vector2(panelRect.X + 10, y), XnaColor.White, 1.2f);
        y += 20;
        
        for (int i = 0; i < _npcTypes.Length; i++)
        {
            var btnRect = new Rectangle(panelRect.X + 10, y, 90, 22);
            var isSelected = _npcTypeIds[i] == _adminSelectedNpcType;
            var hover = _ui.IsInside(btnRect, _input.MousePosition);
            
            _ui.DrawRectangle(btnRect, isSelected ? new XnaColor(80, 80, 120) : (hover ? new XnaColor(60, 60, 80) : new XnaColor(40, 40, 50)));
            _ui.DrawRectangleOutline(btnRect, isSelected ? XnaColor.Yellow : XnaColor.Gray);
            _ui.DrawTextCentered(_npcTypes[i], new Vector2(btnRect.X + btnRect.Width / 2, btnRect.Y + 3), XnaColor.White, 1.1f);
            
            if (hover && _input.IsLeftMousePressed)
            {
                _adminSelectedNpcType = _npcTypeIds[i];
            }
            
            y += 26;
        }
        
        y += 10;
        
        // Spawn at cursor button
        var spawnBtn = new Rectangle(panelRect.X + 10, y, 190, 28);
        var spawnHover = _ui.IsInside(spawnBtn, _input.MousePosition);
        _ui.DrawRectangle(spawnBtn, spawnHover ? new XnaColor(60, 100, 60) : new XnaColor(40, 80, 40));
        _ui.DrawRectangleOutline(spawnBtn, XnaColor.Green);
        _ui.DrawTextCentered("Spawn at Cursor", new Vector2(spawnBtn.X + spawnBtn.Width / 2, spawnBtn.Y + 5), XnaColor.White, 1.2f);
        
        if (spawnHover && _input.IsLeftMousePressed)
        {
            SpawnNpcAtCursor();
        }
        
        y += 35;
        
        // Kill target button
        var killBtn = new Rectangle(panelRect.X + 10, y, 190, 28);
        var killHover = _ui.IsInside(killBtn, _input.MousePosition);
        _ui.DrawRectangle(killBtn, killHover ? new XnaColor(100, 60, 60) : new XnaColor(80, 40, 40));
        _ui.DrawRectangleOutline(killBtn, XnaColor.Red);
        _ui.DrawTextCentered("Kill Target", new Vector2(killBtn.X + killBtn.Width / 2, killBtn.Y + 5), XnaColor.White, 1.2f);
        
        if (killHover && _input.IsLeftMousePressed && _targetEntityId.HasValue)
        {
            _ = _gameState.AdminKillAsync(_targetEntityId.Value);
        }
        
        y += 35;
        
        // Heal self button
        var healBtn = new Rectangle(panelRect.X + 10, y, 190, 28);
        var healHover = _ui.IsInside(healBtn, _input.MousePosition);
        _ui.DrawRectangle(healBtn, healHover ? new XnaColor(60, 60, 100) : new XnaColor(40, 40, 80));
        _ui.DrawRectangleOutline(healBtn, XnaColor.Cyan);
        _ui.DrawTextCentered("Full Heal Self", new Vector2(healBtn.X + healBtn.Width / 2, healBtn.Y + 5), XnaColor.White, 1.2f);
        
        if (healHover && _input.IsLeftMousePressed)
        {
            _ = _gameState.AdminHealSelfAsync();
        }
    }
    
    private void SpawnNpcAtCursor()
    {
        var mousePos = _input.MousePosition;
        var viewportPos = MouseToViewportCoords(mousePos);
        var worldPos = _camera.ScreenToWorld(viewportPos);
        var npcName = _npcTypes[Array.IndexOf(_npcTypeIds, _adminSelectedNpcType)];
        _ = _gameState.AdminSpawnNpcAsync(npcName, _adminSelectedNpcType, worldPos);
    }
    
    private void DrawDeathScreen(XnaGameTime gameTime)
    {
        // Fade in the death screen
        _deathFadeProgress = Math.Min(_deathFadeProgress + (float)gameTime.ElapsedGameTime.TotalSeconds * 0.5f, 1f);
        
        // Black fade first, then grey overlay
        var blackAlpha = (int)(Math.Min(_deathFadeProgress * 2f, 1f) * 200);
        var greyAlpha = (int)(Math.Max(_deathFadeProgress - 0.5f, 0f) * 2f * 180);
        
        // Draw black overlay (cover entire window)
        _ui.DrawRectangle(new Rectangle(0, 0, _windowWidth, _windowHeight), new XnaColor(0, 0, 0, blackAlpha));
        
        // Draw grey overlay
        if (_deathFadeProgress > 0.5f)
        {
            _ui.DrawRectangle(new Rectangle(0, 0, _windowWidth, _windowHeight), new XnaColor(50, 50, 50, greyAlpha));
        }
        
        // Draw death text (centered in window)
        if (_deathFadeProgress > 0.7f)
        {
            var textAlpha = Math.Min((_deathFadeProgress - 0.7f) * 3.3f, 1f);
            var textColor = new XnaColor(200, 200, 200, (int)(textAlpha * 255));
            
            var centerX = _windowWidth / 2f;
            var centerY = _windowHeight / 2f;
            
            _ui.DrawTextCentered("You are dead", new Vector2(centerX, centerY - 60), textColor, 4f);
            _ui.DrawTextCentered("Find a Healer or Ankh to resurrect", new Vector2(centerX, centerY + 20), new XnaColor(150, 150, 150, (int)(textAlpha * 200)), 2f);
            
            // Click to continue as ghost
            if (_deathFadeProgress >= 1f)
            {
                var pulseAlpha = (float)(Math.Sin(gameTime.TotalGameTime.TotalSeconds * 3) * 0.3 + 0.7);
                _ui.DrawTextCentered("Click to continue as a ghost...", new Vector2(centerX, centerY + 100), 
                    new XnaColor(100, 100, 100, (int)(pulseAlpha * 200)), 1.5f);
                
                if (_input.IsLeftMousePressed)
                {
                    _isDead = false; // Close death screen, remain as ghost
                }
            }
        }
    }
    
    private void DrawGhostIndicator()
    {
        // Small ghost indicator in corner (relative to viewport)
        var x = _viewportRect.Right - 90;
        var y = _viewportRect.Y + 205;
        _ui.DrawRectangle(new Rectangle(x, y, 80, 25), new XnaColor(80, 80, 100, 180));
        _ui.DrawRectangleOutline(new Rectangle(x, y, 80, 25), new XnaColor(150, 150, 180));
        _ui.DrawTextCentered("GHOST", new Vector2(x + 40, y + 3), new XnaColor(180, 180, 220), 1.2f);
    }
    
    private void DrawFloatingDamage(XnaGameTime gameTime)
    {
        var elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // Update and draw floating damage numbers
        for (int i = _floatingDamage.Count - 1; i >= 0; i--)
        {
            var fd = _floatingDamage[i];
            
            // Update position (float upward)
            fd.Position = new Vector2(fd.Position.X, fd.Position.Y + fd.VelocityY * elapsed);
            fd.TimeRemaining -= elapsed;
            
            if (fd.TimeRemaining <= 0)
            {
                _floatingDamage.RemoveAt(i);
                continue;
            }
            
            // Calculate alpha (fade out in last second)
            var alpha = fd.TimeRemaining < 1f ? fd.TimeRemaining : 1f;
            var color = fd.Color * alpha;
            
            // Scale based on time (start big, shrink slightly)
            var scale = 1.5f + (fd.TimeRemaining > 1.5f ? (fd.TimeRemaining - 1.5f) * 0.5f : 0f);
            
            // Draw shadow
            _ui.DrawTextCentered(fd.Text, fd.Position + new Vector2(2, 2), new XnaColor(0, 0, 0, (int)(alpha * 150)), scale);
            
            // Draw text
            _ui.DrawTextCentered(fd.Text, fd.Position, color, scale);
        }
    }
    
    private void DrawParticles(XnaGameTime gameTime)
    {
        var elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            
            // Update particle
            p.Velocity = new Vector2(p.Velocity.X, p.Velocity.Y + p.Gravity * elapsed);
            p.Position = new Vector2(p.Position.X + p.Velocity.X * elapsed, p.Position.Y + p.Velocity.Y * elapsed);
            p.TimeRemaining -= elapsed;
            
            if (p.TimeRemaining <= 0)
            {
                _particles.RemoveAt(i);
                continue;
            }
            
            // Calculate alpha
            var alpha = p.TimeRemaining / p.Lifetime;
            var color = p.Color * alpha;
            
            // Draw particle as small rectangle
            var size = (int)(p.Size * alpha);
            if (size < 1) size = 1;
            var rect = new Rectangle((int)p.Position.X - size/2, (int)p.Position.Y - size/2, size, size);
            _ui.DrawRectangle(rect, color);
        }
    }
    
    private void DrawEntityHighlights()
    {
        // This is called before _ui.Begin() so we need to draw highlights differently
        // We'll draw them as part of the UI draw phase instead
    }
    
    private void DrawEntityHighlightsUI()
    {
        // Only show name above the entity under the cursor (hover highlighting)
        // The actual entity tinting is done in WorldRenderer
        if (!_hoveredEntityId.HasValue) return;
        
        var entity = _gameState.GetEntity(_hoveredEntityId.Value);
        if (entity == null || entity.Id == _gameState.PlayerEntityId) return;
        
        // Get notoriety color for the name
        XnaColor color;
        if (entity is NpcEntity npc)
        {
            color = GetNotorietyColor(npc);
        }
        else
        {
            color = XnaColor.Gray; // Default for unknown entity types
        }
        
        var screenPos = WorldToScreen(entity.Position);
        
        // Show name on hover (entity itself is tinted in WorldRenderer)
        _ui.DrawTextCentered(entity.Name, new Vector2(screenPos.X, screenPos.Y - 70), color, 1.3f);
    }
    
    private XnaColor GetNotorietyColor(NpcEntity npc)
    {
        // Red = hostile/evil, Grey = neutral/attackable, Blue = friendly
        return npc.Behavior switch
        {
            NpcBehavior.Hostile or NpcBehavior.Aggressive => XnaColor.Red,
            NpcBehavior.Defensive => new XnaColor(128, 128, 128), // Grey
            NpcBehavior.Passive => XnaColor.Blue,
            NpcBehavior.Vendor or NpcBehavior.QuestGiver => XnaColor.Green,
            NpcBehavior.Guard => XnaColor.Yellow,
            _ => new XnaColor(128, 128, 128)
        };
    }
    
    private void DrawTargetBars()
    {
        foreach (var bar in _targetBars.ToList()) // ToList to allow modification
        {
            var entity = _gameState.GetEntity(bar.EntityId) as Mobile;
            if (entity == null) continue;
            
            // Skip drawing for dead entities (but keep bar for potential resurrection tracking)
            if (entity.Health <= 0)
            {
                // Draw greyed out bar for dead entity
                var deadRect = new Rectangle((int)bar.Position.X, (int)bar.Position.Y, 200, 55);
                _ui.DrawRectangle(deadRect, new XnaColor(30, 30, 30, 180));
                _ui.DrawRectangleOutline(deadRect, XnaColor.DarkGray);
                _ui.DrawTextCentered($"{entity.Name} (Dead)", new Vector2(deadRect.X + deadRect.Width / 2, deadRect.Y + 20), XnaColor.Gray, 1.2f);
                continue;
            }
            
            var rect = new Rectangle((int)bar.Position.X, (int)bar.Position.Y, 200, 55);
            var borderColor = bar.IsDragging ? XnaColor.CornflowerBlue : AssetManager.BorderColor;
            
            // Notoriety-colored header
            var headerColor = entity is NpcEntity npc ? GetNotorietyColor(npc) : new XnaColor(60, 60, 80);
            
            _ui.DrawRectangle(rect, new XnaColor(20, 20, 30, 220));
            _ui.DrawRectangle(new Rectangle(rect.X, rect.Y, rect.Width, 20), headerColor);
            _ui.DrawRectangleOutline(rect, borderColor);
            
            // Name
            _ui.DrawTextCentered(entity.Name, new Vector2(rect.X + rect.Width / 2, rect.Y + 3), XnaColor.White, 1.2f);
            
            // Health bar - safely calculate health percentage
            var maxHealth = Math.Max(1, entity.MaxHealth); // Prevent division by zero
            var healthPercent = Math.Clamp((float)entity.Health / maxHealth, 0f, 1f);
            
            var hpBar = new Rectangle(rect.X + 5, rect.Y + 25, rect.Width - 10, 14);
            _ui.DrawProgressBar(hpBar, healthPercent, XnaColor.DarkRed);
            _ui.DrawTextCentered($"{entity.Health}/{entity.MaxHealth}", 
                new Vector2(hpBar.X + hpBar.Width / 2, hpBar.Y), XnaColor.White, 1.0f);
            
            // Level
            _ui.DrawText($"Level {entity.Level}", new Vector2(rect.X + 5, rect.Y + 40), XnaColor.Gray, 1.0f);
        }
    }
    
    private void DrawCombatModeIndicator()
    {
        if (_combatMode)
        {
            var text = "WAR";
            var pos = new Vector2(_viewportRect.Right - 60, _viewportRect.Y + 180);
            _ui.DrawRectangle(new Rectangle((int)pos.X - 5, (int)pos.Y - 2, 50, 22), new XnaColor(150, 30, 30, 200));
            _ui.DrawRectangleOutline(new Rectangle((int)pos.X - 5, (int)pos.Y - 2, 50, 22), XnaColor.Red);
            _ui.DrawText(text, pos, XnaColor.White, 1.4f);
        }
    }
    
    private void DrawAdminModeIndicator()
    {
        var modeText = _adminMode switch
        {
            AdminMode.Teleport => "TELEPORT",
            AdminMode.Kill => "KILL",
            AdminMode.AddNpc => "ADD",
            AdminMode.AddItem => "ADD ITEM",
            _ => ""
        };
        
        var modeColor = _adminMode switch
        {
            AdminMode.Teleport => XnaColor.Cyan,
            AdminMode.Kill => XnaColor.Red,
            AdminMode.AddNpc => XnaColor.LightGreen,
            AdminMode.AddItem => XnaColor.Yellow,
            _ => XnaColor.White
        };
        
        if (_adminModeMulti) modeText = $"[M] {modeText}";
        
        // Draw at top center of viewport
        var pos = new Vector2(_viewportRect.X + _viewportRect.Width / 2 - 60, _viewportRect.Y + 10);
        var rect = new Rectangle((int)pos.X - 10, (int)pos.Y - 5, 140, 30);
        
        _ui.DrawRectangle(rect, new XnaColor(30, 30, 40, 230));
        _ui.DrawRectangleOutline(rect, modeColor);
        _ui.DrawTextCentered(modeText, new Vector2(rect.X + rect.Width / 2, rect.Y + 8), modeColor, 1.4f);
        
        // Hint text
        var hint = _adminModeMulti ? "ESC or /cancel to exit" : "Click to use, ESC to cancel";
        _ui.DrawTextCentered(hint, new Vector2(rect.X + rect.Width / 2, rect.Y + 35), XnaColor.Gray, 1.0f);
    }
    
    private void DrawAddMenu()
    {
        // Draw menu of addable entities
        var menuWidth = 180;
        var menuHeight = 30 + _npcTypes.Length * 28;
        var x = _viewportRect.X + _viewportRect.Width / 2 - menuWidth / 2;
        var y = _viewportRect.Y + 70;
        
        var menuRect = new Rectangle(x, y, menuWidth, menuHeight);
        _ui.DrawRectangle(menuRect, new XnaColor(30, 30, 40, 240));
        _ui.DrawRectangleOutline(menuRect, XnaColor.Green);
        
        _ui.DrawTextCentered("Select Type", new Vector2(x + menuWidth / 2, y + 5), XnaColor.White, 1.2f);
        
        var mousePos = _input.MousePosition;
        
        for (int i = 0; i < _npcTypes.Length; i++)
        {
            var itemY = y + 28 + i * 28;
            var itemRect = new Rectangle(x + 5, itemY, menuWidth - 10, 26);
            var isHovered = itemRect.Contains((int)mousePos.X, (int)mousePos.Y);
            var isSelected = _npcTypeIds[i] == _adminSelectedNpcType;
            
            var bgColor = isSelected ? new XnaColor(60, 100, 60) : 
                          isHovered ? new XnaColor(60, 60, 80) : new XnaColor(40, 40, 50);
            
            _ui.DrawRectangle(itemRect, bgColor);
            if (isSelected) _ui.DrawRectangleOutline(itemRect, XnaColor.LightGreen);
            
            _ui.DrawText(_npcTypes[i], new Vector2(x + 15, itemY + 4), 
                isSelected ? XnaColor.LightGreen : XnaColor.White, 1.2f);
            
            if (isHovered && _input.IsLeftMousePressed)
            {
                _adminSelectedNpcType = _npcTypeIds[i];
                _adminModeParam = _npcTypes[i].ToLower();
            }
        }
    }
    
    private void DrawCastingBar(XnaGameTime gameTime)
    {
        if (!_isCasting) return;
        
        // Initialize cast start time on first frame
        if (_castStartTime == 0)
            _castStartTime = gameTime.TotalGameTime.TotalSeconds;
        
        var elapsed = gameTime.TotalGameTime.TotalSeconds;
        var progress = (float)((elapsed - _castStartTime) / _castDuration);
        progress = Math.Clamp(progress, 0, 1);
        
        var barWidth = 200;
        var barHeight = 20;
        var x = _viewportRect.X + (_viewportRect.Width - barWidth) / 2;
        var y = _viewportRect.Y + _viewportRect.Height / 2 + 50;
        
        var bgRect = new Rectangle(x - 5, y - 5, barWidth + 10, barHeight + 25);
        _ui.DrawRectangle(bgRect, new XnaColor(20, 20, 30, 220));
        _ui.DrawRectangleOutline(bgRect, XnaColor.Purple);
        
        // Spell name
        _ui.DrawTextCentered(_castingSpellName, new Vector2(x + barWidth / 2, y - 2), XnaColor.Cyan, 1.2f);
        
        // Progress bar
        var barRect = new Rectangle(x, y + 15, barWidth, barHeight);
        _ui.DrawRectangle(barRect, new XnaColor(40, 40, 60));
        _ui.DrawRectangle(new Rectangle(x, y + 15, (int)(barWidth * progress), barHeight), XnaColor.Purple);
        _ui.DrawRectangleOutline(barRect, XnaColor.Gray);
    }
    
    private void DrawSpellTargetCursor()
    {
        if (!_awaitingSpellTarget) return;
        
        var mousePos = _input.MousePosition;
        var x = (int)mousePos.X;
        var y = (int)mousePos.Y;
        
        // Draw targeting cursor sprite
        var cursor = _assets.CursorTarget;
        var cursorPos = new Vector2(x - cursor.Width / 2, y - cursor.Height / 2);
        _ui.DrawSprite(cursor, cursorPos, XnaColor.White);
        
        // Spell name hint
        var spellName = GetSpellName(_pendingSpellId);
        _ui.DrawTextCentered($"Target: {spellName}", new Vector2(x, y + 25), XnaColor.Cyan, 1.2f);
    }
    
    private void DrawOverheadTexts()
    {
        foreach (var oh in _overheadTexts)
        {
            var entity = _gameState.GetEntity(oh.EntityId);
            if (entity == null) continue;
            
            var screenPos = WorldToScreen(entity.Position);
            var textSize = _ui.MeasureText(oh.Text);
            var pos = new Vector2(screenPos.X - textSize.X / 2, screenPos.Y - 70 - oh.YOffset);
            var alpha = oh.TimeRemaining < 1f ? oh.TimeRemaining : 1f;
            
            _ui.DrawRectangle(new Rectangle((int)pos.X - 4, (int)pos.Y - 2, (int)textSize.X + 8, (int)textSize.Y + 4), new XnaColor(0, 0, 0, (int)(180 * alpha)));
            _ui.DrawText(oh.Text, pos, oh.Color * alpha, 1.4f);
        }
    }
    
    private Vector2 WorldToScreen(WorldPosition worldPos)
    {
        var playerPos = _gameState.Player?.Position ?? new WorldPosition(0, 0, 0);
        var relX = worldPos.X - playerPos.X;
        var relY = worldPos.Y - playerPos.Y;
        var isoX = (relX - relY) * 32 * _camera.Zoom;
        var isoY = (relX + relY) * 16 * _camera.Zoom - worldPos.Z * 4 * _camera.Zoom;
        // Center on viewport (add viewport offset for screen coordinates)
        var centerX = _viewportRect.X + _viewportWidth / 2f;
        var centerY = _viewportRect.Y + _viewportHeight / 2f;
        return new Vector2(centerX + (float)isoX, centerY + (float)isoY);
    }
    
    private void DrawTargetFrame()
    {
        if (!_targetEntityId.HasValue) return;
        var target = _gameState.GetEntity(_targetEntityId.Value) as Mobile;
        if (target == null) return;
        
        _ui.DrawRectangle(_targetFrameRect, new XnaColor(30, 20, 20, 220));
        _ui.DrawRectangleOutline(_targetFrameRect, XnaColor.DarkRed);
        _ui.DrawText(target.Name, new Vector2(_targetFrameRect.X + 10, _targetFrameRect.Y + 5), XnaColor.White, 1.4f);
        
        var hpBar = new Rectangle(_targetFrameRect.X + 10, _targetFrameRect.Y + 28, _targetFrameRect.Width - 20, 14);
        var maxHp = Math.Max(1, target.MaxHealth);
        var hpPercent = Math.Clamp((float)target.Health / maxHp, 0f, 1f);
        _ui.DrawProgressBar(hpBar, hpPercent, XnaColor.DarkRed);
        _ui.DrawTextCentered($"{target.Health}/{target.MaxHealth}", new Vector2(hpBar.X + hpBar.Width / 2, hpBar.Y), XnaColor.White, 1.1f);
        _ui.DrawText($"Level {target.Level}", new Vector2(_targetFrameRect.X + 10, _targetFrameRect.Y + 44), XnaColor.Gray, 1.1f);
    }
    
    private void DrawExperienceBar()
    {
        if (_gameState.Player == null) return;
        _ui.DrawRectangle(_expBarRect, new XnaColor(20, 20, 30));
        var fillWidth = (int)(_expBarRect.Width * 0.35f);
        _ui.DrawRectangle(new Rectangle(_expBarRect.X, _expBarRect.Y, fillWidth, _expBarRect.Height), new XnaColor(80, 80, 200));
    }
    
    private void DrawPlayerStats()
    {
        var p = _gameState.Player;
        if (p == null) return;
        
        // Draw draggable panel background with UO-style parchment look
        var panelColor = _isDraggingStatBar ? new XnaColor(50, 45, 35, 230) : new XnaColor(40, 35, 25, 220);
        _ui.DrawRectangle(_statBarPanel, panelColor);
        
        // Ornate border
        var borderDark = new XnaColor(60, 50, 30);
        var borderLight = new XnaColor(100, 90, 60);
        _ui.DrawRectangleOutline(_statBarPanel, borderDark, 2);
        _ui.DrawRectangle(new Rectangle(_statBarPanel.X + 2, _statBarPanel.Y + 2, _statBarPanel.Width - 4, 1), borderLight);
        _ui.DrawRectangle(new Rectangle(_statBarPanel.X + 2, _statBarPanel.Y + 2, 1, _statBarPanel.Height - 4), borderLight);
        
        // Character name at top
        _ui.DrawTextCentered(p.Name.Length > 0 ? p.Name : "Player", 
            new Vector2(_statBarPanel.X + _statBarPanel.Width / 2, _statBarPanel.Y - 12), 
            new XnaColor(220, 200, 150), 1.3f);
        
        // Health bar with label
        DrawStatBar(_healthBarRect, "Hits", p.Health, p.MaxHealth, new XnaColor(140, 30, 30), new XnaColor(200, 50, 50));
        
        // Mana bar with label
        DrawStatBar(_manaBarRect, "Mana", p.Mana, p.MaxMana, new XnaColor(30, 50, 140), new XnaColor(50, 80, 200));
        
        // Stamina bar with label
        DrawStatBar(_staminaBarRect, "Stam", p.Stamina, p.MaxStamina, new XnaColor(30, 100, 30), new XnaColor(50, 160, 50));
    }
    
    /// <summary>
    /// Draw a UO-style stat bar with gradient, label and value
    /// </summary>
    private void DrawStatBar(Rectangle rect, string label, int current, int max, XnaColor darkColor, XnaColor lightColor)
    {
        // Background
        _ui.DrawRectangle(rect, new XnaColor(20, 20, 20));
        
        // Fill
        float ratio = max > 0 ? Math.Clamp((float)current / max, 0, 1) : 0;
        int fillWidth = (int)(rect.Width * ratio);
        
        if (fillWidth > 0)
        {
            // Draw gradient-like fill (dark at bottom, light at top)
            var bottomRect = new Rectangle(rect.X, rect.Y + rect.Height / 2, fillWidth, rect.Height / 2);
            var topRect = new Rectangle(rect.X, rect.Y, fillWidth, rect.Height / 2);
            _ui.DrawRectangle(bottomRect, darkColor);
            _ui.DrawRectangle(topRect, lightColor);
        }
        
        // Border
        _ui.DrawRectangleOutline(rect, new XnaColor(60, 50, 40));
        
        // Label on left
        _ui.DrawText(label, new Vector2(rect.X + 3, rect.Y + 2), new XnaColor(180, 170, 140), 1.0f);
        
        // Value on right
        _ui.DrawText($"{current}/{max}", new Vector2(rect.Right - 50, rect.Y + 2), XnaColor.White, 1.0f);
    }
    
    private void DrawMinimap()
    {
        // UO-style minimap with parchment background
        var panelColor = new XnaColor(40, 35, 25, 230);
        _ui.DrawRectangle(_minimapRect, panelColor);
        
        // Ornate border
        var borderDark = new XnaColor(60, 50, 30);
        var borderLight = new XnaColor(100, 90, 60);
        _ui.DrawRectangleOutline(_minimapRect, borderDark, 2);
        _ui.DrawRectangle(new Rectangle(_minimapRect.X + 2, _minimapRect.Y + 2, _minimapRect.Width - 4, 1), borderLight);
        _ui.DrawRectangle(new Rectangle(_minimapRect.X + 2, _minimapRect.Y + 2, 1, _minimapRect.Height - 4), borderLight);
        
        // Title
        _ui.DrawTextCentered("MAP", new Vector2(_minimapRect.X + _minimapRect.Width / 2, _minimapRect.Y + 8), 
            new XnaColor(180, 170, 140), 1.3f);
        
        // Separator line
        _ui.DrawRectangle(new Rectangle(_minimapRect.X + 10, _minimapRect.Y + 25, _minimapRect.Width - 20, 1), borderDark);
        
        // Map area (placeholder - green terrain)
        var mapArea = new Rectangle(_minimapRect.X + 10, _minimapRect.Y + 30, _minimapRect.Width - 20, _minimapRect.Height - 40);
        _ui.DrawRectangle(mapArea, new XnaColor(30, 50, 30));
        _ui.DrawRectangleOutline(mapArea, borderDark);
        
        // Player marker (white dot in center)
        var centerX = mapArea.X + mapArea.Width / 2;
        var centerY = mapArea.Y + mapArea.Height / 2;
        _ui.DrawRectangle(new Rectangle(centerX - 3, centerY - 3, 6, 6), XnaColor.White);
        _ui.DrawRectangleOutline(new Rectangle(centerX - 3, centerY - 3, 6, 6), XnaColor.Black);
        
        // Target marker if we have one
        if (_targetEntityId.HasValue)
        {
            var target = _gameState.GetEntity(_targetEntityId.Value);
            if (target != null && _gameState.Player != null)
            {
                // Calculate relative position (clamped to map bounds)
                var dx = target.Position.X - _gameState.Player.Position.X;
                var dy = target.Position.Y - _gameState.Player.Position.Y;
                var maxRange = 20f;
                dx = Math.Clamp(dx, -maxRange, maxRange) / maxRange;
                dy = Math.Clamp(dy, -maxRange, maxRange) / maxRange;
                
                var targetX = centerX + (int)(dx * mapArea.Width / 2 * 0.8f);
                var targetY = centerY + (int)(dy * mapArea.Height / 2 * 0.8f);
                
                _ui.DrawRectangle(new Rectangle(targetX - 2, targetY - 2, 4, 4), XnaColor.Red);
            }
        }
        
        // Compass directions
        _ui.DrawText("N", new Vector2(centerX - 4, mapArea.Y + 2), new XnaColor(150, 140, 110), 0.9f);
        _ui.DrawText("S", new Vector2(centerX - 4, mapArea.Bottom - 12), new XnaColor(150, 140, 110), 0.9f);
        _ui.DrawText("W", new Vector2(mapArea.X + 2, centerY - 5), new XnaColor(150, 140, 110), 0.9f);
        _ui.DrawText("E", new Vector2(mapArea.Right - 10, centerY - 5), new XnaColor(150, 140, 110), 0.9f);
    }
    
    private void DrawHotbar()
    {
        var mousePos = _input.MousePosition;
        
        // UO-style parchment background
        var panelColor = new XnaColor(40, 35, 25, 240);
        _ui.DrawRectangle(_hotbarRect, panelColor);
        
        // Ornate border
        var borderDark = new XnaColor(60, 50, 30);
        var borderLight = new XnaColor(100, 90, 60);
        _ui.DrawRectangleOutline(_hotbarRect, borderDark, 2);
        _ui.DrawRectangle(new Rectangle(_hotbarRect.X + 2, _hotbarRect.Y + 2, _hotbarRect.Width - 4, 1), borderLight);
        _ui.DrawRectangle(new Rectangle(_hotbarRect.X + 2, _hotbarRect.Y + 2, 1, _hotbarRect.Height - 4), borderLight);
        
        for (int i = 0; i < 10; i++)
        {
            var rect = new Rectangle(_hotbarRect.X + 10 + i * 42, _hotbarRect.Y + 4, 38, 38);
            var hover = _ui.IsInside(rect, mousePos);
            
            // Slot background with 3D effect
            _ui.DrawRectangle(rect, new XnaColor(30, 25, 20));
            if (hover)
            {
                _ui.DrawRectangle(new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2), new XnaColor(50, 45, 35));
            }
            
            // Inner shadow (top-left light, bottom-right dark)
            _ui.DrawRectangle(new Rectangle(rect.X, rect.Y, rect.Width, 1), new XnaColor(20, 15, 10));
            _ui.DrawRectangle(new Rectangle(rect.X, rect.Y, 1, rect.Height), new XnaColor(20, 15, 10));
            _ui.DrawRectangle(new Rectangle(rect.X + 1, rect.Bottom - 1, rect.Width - 1, 1), borderLight);
            _ui.DrawRectangle(new Rectangle(rect.Right - 1, rect.Y + 1, 1, rect.Height - 1), borderLight);
            
            var slot = _hotbar[i];
            if (!slot.IsEmpty)
            {
                // Try to draw spell icon texture
                var spellIcon = _assets.GetSpellIcon(slot.Id);
                if (spellIcon != null && slot.Type == HotbarSlotType.Spell)
                {
                    _ui.DrawTexture(spellIcon, new Rectangle(rect.X + 3, rect.Y + 3, 32, 32));
                }
                else
                {
                    // Fallback to text icon
                    var col = slot.Type == HotbarSlotType.Spell ? XnaColor.Cyan : (slot.Type == HotbarSlotType.Skill ? XnaColor.LightGreen : XnaColor.White);
                    _ui.DrawTextCentered(slot.Icon, new Vector2(rect.X + 19, rect.Y + 10), col, 2f);
                }
                
                if (hover)
                {
                    var col = slot.Type == HotbarSlotType.Spell ? XnaColor.Cyan : (slot.Type == HotbarSlotType.Skill ? XnaColor.LightGreen : XnaColor.White);
                    var tip = new Rectangle((int)mousePos.X + 10, (int)mousePos.Y - 30, slot.Name.Length * 8 + 16, 24);
                    _ui.DrawRectangle(tip, new XnaColor(20, 20, 30, 240));
                    _ui.DrawText(slot.Name, new Vector2(tip.X + 8, tip.Y + 4), col, 1.3f);
                }
            }
            _ui.DrawText(i == 9 ? "0" : (i + 1).ToString(), new Vector2(rect.X + 2, rect.Y + 2), new XnaColor(150, 150, 150), 1f);
        }
    }
    
    private void DrawMenuButtons()
    {
        var mp = _input.MousePosition;
        DrawBtn(_inventoryBtnRect, "I", "Inventory", mp);
        DrawBtn(_equipmentBtnRect, "E", "Equipment", mp);
        DrawBtn(_skillsBtnRect, "K", "Skills", mp);
        DrawBtn(_spellbookBtnRect, "B", "Spellbook", mp);
        DrawBtn(_settingsBtnRect, "O", "Settings", mp);
        DrawBtn(_helpBtnRect, "?", "Help (F1)", mp);
    }
    
    private void DrawBtn(Rectangle r, string icon, string tip, Vector2 mp)
    {
        var h = _ui.IsInside(r, mp);
        _ui.DrawRectangle(r, h ? new XnaColor(60, 60, 80) : new XnaColor(40, 40, 50, 200));
        _ui.DrawRectangleOutline(r, h ? XnaColor.CornflowerBlue : AssetManager.BorderColor);
        _ui.DrawTextCentered(icon, new Vector2(r.X + r.Width / 2, r.Y + 8), XnaColor.White, 2f);
        if (h)
        {
            var tr = new Rectangle((int)mp.X + 10, (int)mp.Y - 25, tip.Length * 8 + 10, 20);
            _ui.DrawRectangle(tr, new XnaColor(20, 20, 30, 240));
            _ui.DrawText(tip, new Vector2(tr.X + 5, tr.Y + 3), XnaColor.White, 1.2f);
        }
    }
    
    private void DrawChatInput()
    {
        _ui.DrawRectangle(_chatInputRect, new XnaColor(40, 40, 50, 230));
        _ui.DrawRectangleOutline(_chatInputRect, XnaColor.CornflowerBlue);
        var txt = _chatInput.Length > 38 ? _chatInput[^38..] : _chatInput;
        _ui.DrawText(txt + "_", new Vector2(_chatInputRect.X + 8, _chatInputRect.Y + 7), XnaColor.White, 1.3f);
        _ui.DrawText($"{_chatInput.Length}/{MaxChatLength}", new Vector2(_chatInputRect.Right - 55, _chatInputRect.Y + 10), XnaColor.Gray, 1f);
    }
}

public class OverheadText
{
    public EntityId EntityId { get; set; }
    public string Text { get; set; } = "";
    public XnaColor Color { get; set; }
    public float TimeRemaining { get; set; }
    public float YOffset { get; set; }
}

public class FloatingDamage
{
    public Vector2 Position { get; set; }
    public string Text { get; set; } = "";
    public XnaColor Color { get; set; }
    public float TimeRemaining { get; set; }
    public float VelocityY { get; set; }
}

public enum HotbarSlotType { Empty, Item, Spell, Skill }

public class HotbarSlot
{
    public HotbarSlotType Type { get; set; } = HotbarSlotType.Empty;
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public bool IsEmpty => Type == HotbarSlotType.Empty;
    
    public void SetItem(int id, string name, string icon = "#") { Type = HotbarSlotType.Item; Id = id; Name = name; Icon = icon; }
    public void SetSpell(int id, string name, string icon = "o") { Type = HotbarSlotType.Spell; Id = id; Name = name; Icon = icon; }
    public void SetSkill(int id, string name, string icon = "~") { Type = HotbarSlotType.Skill; Id = id; Name = name; Icon = icon; }
    public void Clear() { Type = HotbarSlotType.Empty; Id = 0; Name = ""; Icon = ""; }
}

public class Particle
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public XnaColor Color { get; set; }
    public int Size { get; set; }
    public float Lifetime { get; set; }
    public float TimeRemaining { get; set; }
    public float Gravity { get; set; }
}

public static class DirectionHelper
{
    public static Direction FromOffset(int dx, int dy) => DirectionExtensions.FromOffset(dx, dy);
}
