using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RealmOfReality.Client.Assets;
using RealmOfReality.Client.Engine;
using RealmOfReality.Client.Game;
using RealmOfReality.Client.Network;
using RealmOfReality.Client.UI;

namespace RealmOfReality.Client;

public class RealmGame : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    
    // Core systems
    private GameClient _networkClient = null!;
    private GameState _gameState = null!;
    private Camera _camera = null!;
    private InputManager _input = null!;
    private AssetManager _assets = null!;
    private UOAssetManager? _uoAssets; // Optional UO asset loader
    private ClientSettings _settings = null!;
    
    // Rendering
    private WorldRenderer _worldRenderer = null!;
    private UIRenderer _uiRenderer = null!;
    
    // Screen management
    private IScreen _currentScreen = null!;
    private LoginScreen _loginScreen = null!;
    private CharacterSelectScreen _characterSelectScreen = null!;
    private CharacterCreationScreen _characterCreationScreen = null!;
    private GameplayScreen _gameplayScreen = null!;
    private SettingsScreen? _settingsScreen;
    private bool _showingSettings = false;
    
    // Debug console
    private DebugConsole? _debugConsole;
    
    // Settings
    public const int DefaultWidth = 1280;
    public const int DefaultHeight = 720;
    
    // Expose settings for other components
    public static ClientSettings? Settings { get; private set; }
    public static UOAssetManager? UOAssets { get; private set; }
    
    public RealmGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = DefaultWidth,
            PreferredBackBufferHeight = DefaultHeight,
            IsFullScreen = false,
            SynchronizeWithVerticalRetrace = true
        };
        
        IsMouseVisible = false; // Using custom UO cursor
        Window.AllowUserResizing = true;
        Window.Title = "Realm of Reality";
    }
    
    protected override void Initialize()
    {
        // Load settings first
        _settings = ClientSettings.Load();
        Settings = _settings;
        
        // Auto-detect UO path if current path invalid
        if (!_settings.ValidateUOPath())
        {
            _settings.AutoDetectUOPath();
        }
        // Initialize input
        _input = new InputManager();
        
        // Initialize camera
        _camera = new Camera(GraphicsDevice.Viewport);
        
        // Initialize networking
        _networkClient = new GameClient("127.0.0.1", 7775);
        _gameState = new GameState(_networkClient);
        
        // Subscribe to state changes for screen transitions
        _gameState.StateChanged += OnGameStateChanged;
        
        base.Initialize();
    }
    
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        // Load standard assets
        _assets = new AssetManager(GraphicsDevice);
        _assets.LoadContent();
        
        // Try to load UO assets if enabled and path exists
        TryLoadUOAssets();
        
        // Initialize renderers
        _worldRenderer = new WorldRenderer(_spriteBatch, _assets, _camera, _uoAssets);
        _uiRenderer = new UIRenderer(_spriteBatch, _assets);
        
        // Load map data
        TryLoadMap();
        
        // Create settings screen
        _settingsScreen = new SettingsScreen(_uiRenderer, _input, _settings, 
            () => { _showingSettings = false; },
            (valid) => { if (valid) ReloadUOAssets(); }
        );
        
        // Create screens
        _loginScreen = new LoginScreen(_gameState, _networkClient, _uiRenderer, _input, _assets, _settings,
            () => { _showingSettings = true; _settingsScreen?.Enter(); }); // Settings callback
        _characterSelectScreen = new CharacterSelectScreen(_gameState, _uiRenderer, _input, _assets,
            () => SetScreen(_characterCreationScreen)); // Pass create callback
        _characterCreationScreen = new CharacterCreationScreen(_gameState, _uiRenderer, _input,
            () => SetScreen(_characterSelectScreen), // Cancel callback
            () => SetScreen(_characterSelectScreen), // Create callback (returns to select)
            _assets); // Pass assets for preview
        _gameplayScreen = new GameplayScreen(_gameState, _worldRenderer, _uiRenderer, _camera, _input, _assets, GraphicsDevice, _uoAssets);
        
        // Initialize debug console
        _debugConsole = new DebugConsole(GraphicsDevice, _uiRenderer);
        RegisterDebugCommands();
        
        // Handle window resize
        Window.ClientSizeChanged += OnWindowResize;
        
        // Start at login screen
        _currentScreen = _loginScreen;
        _currentScreen.Enter();
    }
    
    private void TryLoadUOAssets()
    {
        Console.WriteLine($"TryLoadUOAssets: UseUOGraphics={_settings.UseUOGraphics}, Path={_settings.UODataPath}");
        Console.WriteLine($"TryLoadUOAssets: Path valid={_settings.ValidateUOPath()}");
        
        if (_settings.UseUOGraphics && _settings.ValidateUOPath())
        {
            Console.WriteLine($"Loading UO assets from: {_settings.UODataPath}");
            
            // List files in the directory for debugging
            try
            {
                var files = Directory.GetFiles(_settings.UODataPath);
                Console.WriteLine($"  Files found: {files.Length}");
                foreach (var f in files.Take(20))
                    Console.WriteLine($"    {Path.GetFileName(f)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error listing files: {ex.Message}");
            }
            
            _uoAssets = new UOAssetManager(GraphicsDevice, _settings.UODataPath);
            if (_uoAssets.Initialize())
            {
                Console.WriteLine("UO assets loaded successfully!");
                _uoAssets.PreloadCommon();
                _assets.SetUOAssets(_uoAssets);
                UOAssets = _uoAssets; // Update static property
                Console.WriteLine($"Static UOAssets set: {UOAssets != null}");
            }
            else
            {
                Console.WriteLine("Failed to load UO assets:");
                foreach (var error in _uoAssets.LoadErrors)
                    Console.WriteLine($"  - {error}");
                _uoAssets = null;
                UOAssets = null;
            }
        }
        else if (_settings.UseUOGraphics)
        {
            Console.WriteLine($"UO data path not found or invalid: {_settings.UODataPath}");
            Console.WriteLine("Running with generated assets only.");
            _uoAssets = null;
            UOAssets = null;
        }
        else
        {
            Console.WriteLine("UO graphics disabled in settings.");
            _uoAssets = null;
            UOAssets = null;
        }
    }
    
    private void TryLoadMap()
    {
        if (_settings.UseUOGraphics && _settings.ValidateUOPath())
        {
            Console.WriteLine("Loading UO map data...");
            if (_worldRenderer.LoadMap(GraphicsDevice, _settings.UODataPath, 0))
            {
                Console.WriteLine("Map data loaded successfully!");
            }
            else
            {
                Console.WriteLine("Failed to load map data - using procedural terrain.");
            }
        }
    }
    
    private void ReloadUOAssets()
    {
        // Dispose old assets
        _uoAssets?.Dispose();
        _uoAssets = null;
        UOAssets = null;
        
        // Try to load new assets
        TryLoadUOAssets();
        
        // Update world renderer
        _worldRenderer = new WorldRenderer(_spriteBatch, _assets, _camera, _uoAssets);
        
        // Load map data
        TryLoadMap();
        
        // Update gameplay screen reference
        _gameplayScreen = new GameplayScreen(_gameState, _worldRenderer, _uiRenderer, _camera, _input, _assets, GraphicsDevice, _uoAssets);
    }
    
    private void SetScreen(IScreen screen)
    {
        _currentScreen?.Exit();
        _currentScreen = screen;
        _currentScreen.Enter();
    }
    
    private void OnWindowResize(object? sender, EventArgs e)
    {
        var width = Window.ClientBounds.Width;
        var height = Window.ClientBounds.Height;
        
        // Notify gameplay screen of resize - it controls the camera viewport size
        _gameplayScreen?.OnWindowResize(width, height);
    }
    
    private void RegisterDebugCommands()
    {
        if (_debugConsole == null) return;
        
        // Position command
        _debugConsole.RegisterCommand("pos", args =>
        {
            var player = _gameState.Player;
            if (player != null)
            {
                DebugConsole.WriteLine($"Position: X={player.Position.X:F1} Y={player.Position.Y:F1} Z={player.Position.Z:F1}", Color.LightGreen);
            }
            else
            {
                DebugConsole.WriteError("Not in game");
            }
        }, "Show current player position");
        
        // Teleport command
        _debugConsole.RegisterCommand("tp", args =>
        {
            if (args.Length < 2)
            {
                DebugConsole.WriteLine("Usage: tp <x> <y> [z]", Color.Yellow);
                return;
            }
            if (float.TryParse(args[0], out float x) && float.TryParse(args[1], out float y))
            {
                float z = args.Length > 2 && float.TryParse(args[2], out float zVal) ? zVal : 0;
                _ = _gameState.SendTeleportRequest(x, y, z);
                DebugConsole.WriteSuccess($"Teleport request sent to ({x}, {y}, {z})");
            }
            else
            {
                DebugConsole.WriteError("Invalid coordinates");
            }
        }, "Teleport to X Y [Z] coordinates");
        
        // Grid toggle
        _debugConsole.RegisterCommand("grid", args =>
        {
            _worldRenderer.ShowGrid = !_worldRenderer.ShowGrid;
            DebugConsole.WriteLine($"Grid: {(_worldRenderer.ShowGrid ? "ON" : "OFF")}", Color.Cyan);
        }, "Toggle grid overlay");
        
        // Statics toggle
        _debugConsole.RegisterCommand("statics", args =>
        {
            _worldRenderer.ShowStatics = !_worldRenderer.ShowStatics;
            DebugConsole.WriteLine($"Statics: {(_worldRenderer.ShowStatics ? "ON" : "OFF")}", Color.Cyan);
        }, "Toggle static object rendering");
        
        // UO tiles toggle
        _debugConsole.RegisterCommand("uotiles", args =>
        {
            _worldRenderer.UseUOTiles = !_worldRenderer.UseUOTiles;
            DebugConsole.WriteLine($"UO Tiles: {(_worldRenderer.UseUOTiles ? "ON" : "OFF")}", Color.Cyan);
        }, "Toggle UO tile textures vs fallback");
        
        // Map info
        _debugConsole.RegisterCommand("mapinfo", args =>
        {
            DebugConsole.WriteLine($"Map Loaded: {_worldRenderer.MapLoaded}", Color.Cyan);
            DebugConsole.WriteLine($"UO Assets: {(_uoAssets != null ? "Available" : "Not loaded")}", Color.Cyan);
            if (_uoAssets?.Art != null)
            {
                DebugConsole.WriteLine($"Art Loader: Ready", Color.Cyan);
            }
        }, "Show map loading status");
        
        // Entity list
        _debugConsole.RegisterCommand("entities", args =>
        {
            var entities = _gameState.GetAllEntities().ToList();
            DebugConsole.WriteLine($"=== Entities ({entities.Count}) ===", Color.Cyan);
            foreach (var e in entities.Take(20))
            {
                var type = e.GetType().Name.Replace("Entity", "");
                DebugConsole.WriteLine($"  {e.Name} ({type}) @ {e.Position.X:F0},{e.Position.Y:F0}", Color.White);
            }
            if (entities.Count > 20)
                DebugConsole.WriteLine($"  ... and {entities.Count - 20} more", Color.Gray);
        }, "List nearby entities");
        
        // Camera info
        _debugConsole.RegisterCommand("camera", args =>
        {
            DebugConsole.WriteLine($"Camera Position: {_camera.Position}", Color.Cyan);
            DebugConsole.WriteLine($"Camera Zoom: {_camera.Zoom:F2}", Color.Cyan);
        }, "Show camera info");
        
        // Zoom
        _debugConsole.RegisterCommand("zoom", args =>
        {
            if (args.Length > 0 && float.TryParse(args[0], out float zoom))
            {
                _camera.Zoom = Math.Clamp(zoom, 0.25f, 4f);
                DebugConsole.WriteLine($"Zoom set to {_camera.Zoom:F2}", Color.Cyan);
            }
            else
            {
                DebugConsole.WriteLine($"Current zoom: {_camera.Zoom:F2}", Color.Cyan);
                DebugConsole.WriteLine("Usage: zoom <0.25-4.0>", Color.Yellow);
            }
        }, "Get/set camera zoom level");
        
        // FPS
        _debugConsole.RegisterCommand("fps", args =>
        {
            IsFixedTimeStep = !IsFixedTimeStep;
            DebugConsole.WriteLine($"Fixed time step: {(IsFixedTimeStep ? "ON (60 FPS)" : "OFF (unlimited)")}", Color.Cyan);
        }, "Toggle fixed time step");
        
        // Tile info at position
        _debugConsole.RegisterCommand("tileinfo", args =>
        {
            var player = _gameState.Player;
            if (player == null)
            {
                DebugConsole.WriteError("Not in game");
                return;
            }
            int x = (int)player.Position.X;
            int y = (int)player.Position.Y;
            if (args.Length >= 2)
            {
                int.TryParse(args[0], out x);
                int.TryParse(args[1], out y);
            }
            DebugConsole.WriteLine($"Tile at ({x}, {y}):", Color.Cyan);
            // The tile info would need access to MapLoader - we can expose it through WorldRenderer
            DebugConsole.WriteLine("  (Detailed info requires MapLoader access)", Color.Gray);
        }, "Show tile info at position or current location");
        
        // Reload UO assets
        _debugConsole.RegisterCommand("reload", args =>
        {
            DebugConsole.WriteLine("Reloading UO assets...", Color.Yellow);
            ReloadUOAssets();
            DebugConsole.WriteSuccess("Assets reloaded");
        }, "Reload UO asset files");
        
        // Handle unknown commands via event
        _debugConsole.OnCommand += (cmd, args) =>
        {
            DebugConsole.WriteError($"Unknown command: {cmd}");
            DebugConsole.WriteLine("Type 'commands' for a list of available commands", Color.Gray);
        };
        
        DebugConsole.WriteInfo("Debug commands registered. Press ~ to open console.");
    }
    
    protected override void UnloadContent()
    {
        _networkClient?.Dispose();
        _assets?.Dispose();
        _uoAssets?.Dispose();
        _spriteBatch?.Dispose();
    }
    
    protected override void Update(GameTime gameTime)
    {
        // Update input
        _input.Update();
        
        // Update debug console first (it may consume input)
        _debugConsole?.Update(gameTime);
        
        // Process network packets on main thread
        _gameState.ProcessPackets();
        
        // Global exit handling (skip if console is open)
        if (_debugConsole?.IsConsumingInput != true && 
            _input.IsKeyPressed(Keys.Escape) && _gameState.Phase == ClientPhase.Disconnected)
        {
            if (_showingSettings)
            {
                _showingSettings = false;
            }
            else
            {
                Exit();
                return;
            }
        }
        
        // Update camera viewport on resize
        var currentViewport = GraphicsDevice.Viewport;
        if (_camera.Viewport.Width != currentViewport.Width || _camera.Viewport.Height != currentViewport.Height)
        {
            _camera.Viewport = currentViewport;
        }
        
        // Update settings screen if showing
        if (_showingSettings && _settingsScreen != null)
        {
            _settingsScreen.Update(gameTime);
        }
        else if (_debugConsole?.IsConsumingInput != true)
        {
            // Update current screen (skip if console is consuming input)
            _currentScreen.Update(gameTime);
        }
        
        base.Update(gameTime);
    }
    
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        
        // Draw current screen
        _currentScreen.Draw(gameTime);
        
        // Draw settings overlay if showing
        if (_showingSettings && _settingsScreen != null)
        {
            _settingsScreen.Draw(gameTime);
        }
        
        // Draw debug console on top
        _debugConsole?.Draw();
        
        base.Draw(gameTime);
    }
    
    private void OnGameStateChanged()
    {
        IScreen newScreen = _gameState.Phase switch
        {
            ClientPhase.Disconnected => _loginScreen,
            ClientPhase.Login => _loginScreen,
            ClientPhase.CharacterSelect => _characterSelectScreen,
            ClientPhase.InWorld => _gameplayScreen,
            _ => _currentScreen
        };
        
        if (newScreen != _currentScreen)
        {
            _currentScreen.Exit();
            _currentScreen = newScreen;
            _currentScreen.Enter();
        }
    }
}
