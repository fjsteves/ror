using System.Net;
using RealmOfReality.Server.Config;
using RealmOfReality.Server.Data;
using RealmOfReality.Server.Game;
using RealmOfReality.Server.Network;
using RealmOfReality.Shared.Core;

namespace RealmOfReality.Server;

class Program
{
    private static GameServer _server = null!;
    private static WorldManager _world = null!;
    private static DataStore _dataStore = null!;
    private static PacketHandler _packetHandler = null!;
    private static CancellationTokenSource _cts = new();
    private static ILogger _logger = null!;
    
    static async Task Main(string[] args)
    {
        Console.WriteLine(@"
╔══════════════════════════════════════════════════════════════╗
║                    REALM OF REALITY                          ║
║                   Game Server v0.1.0                         ║
╚══════════════════════════════════════════════════════════════╝
");
        
        // Load configuration
        var configPath = args.Length > 0 ? args[0] : "config/server.json";
        var config = ServerConfig.LoadOrCreate(configPath);
        
        // Create logger
        _logger = new ConsoleLogger("Server");
        
        // Create services
        _dataStore = new DataStore(config.Paths.AccountsFile, config.Paths.CharactersDirectory);
        _world = new WorldManager(new ConsoleLogger("World"), config);
        _server = new GameServer(
            IPAddress.Parse(config.Network.BindAddress),
            config.Network.Port,
            new ConsoleLogger("Network"));
        _packetHandler = new PacketHandler(
            new ConsoleLogger("Packets"),
            config,
            _dataStore,
            _world,
            _server);
        
        try
        {
            // Initialize
            await InitializeAsync(config);
            
            // Start server
            _server.Start();
            
            // Handle Ctrl+C
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                _cts.Cancel();
            };
            
            // Main game loop
            await GameLoopAsync();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error");
        }
        finally
        {
            await ShutdownAsync();
        }
    }
    
    private static async Task InitializeAsync(ServerConfig config)
    {
        _logger.LogInformation("Initializing server...");
        
        // Create directories
        Directory.CreateDirectory(config.Paths.DataDirectory);
        Directory.CreateDirectory(config.Paths.WorldDirectory);
        Directory.CreateDirectory(config.Paths.CharactersDirectory);
        Directory.CreateDirectory(config.Paths.LogDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(config.Paths.AccountsFile) ?? "data");
        
        // Load data
        await _dataStore.LoadAsync();
        
        // Initialize world
        await _world.InitializeAsync();
        
        // Wire up events
        _server.ClientConnected += OnClientConnected;
        _server.ClientDisconnected += OnClientDisconnected;
        _server.PacketReceived += OnPacketReceived;
        
        // Create test account if none exist
        if (_dataStore.GetAccountByUsername("admin") == null)
        {
            var hash = DataStore.HashPassword("admin");
            var account = _dataStore.CreateAccount("admin", hash, "admin@example.com");
            if (account != null)
            {
                account.AccessLevel = Shared.Entities.AccessLevel.Owner;
                _logger.LogInformation("Created admin account: admin / admin (Owner)");
            }
        }
        else
        {
            // Ensure existing admin account has Owner access
            var account = _dataStore.GetAccountByUsername("admin");
            if (account != null && account.AccessLevel < Shared.Entities.AccessLevel.Owner)
            {
                account.AccessLevel = Shared.Entities.AccessLevel.Owner;
                _logger.LogInformation("Updated admin account to Owner access level");
            }
        }
        
        await _dataStore.SaveAsync();
        
        // Spawn test creatures
        _world.SpawnTestCreatures();
        
        _logger.LogInformation("Server initialized");
    }
    
    private static void OnClientConnected(ClientConnection client)
    {
        _logger.LogInformation("Client connected: {0} from {1}", 
            client.ConnectionId, client.RemoteEndPoint);
    }
    
    private static async void OnClientDisconnected(ClientConnection client)
    {
        try
        {
            await _packetHandler.HandleDisconnectAsync(client);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling disconnect for client {0}", client.ConnectionId);
        }
    }
    
    private static async void OnPacketReceived(ClientConnection client, Shared.Network.Packet packet)
    {
        try
        {
            await _packetHandler.HandlePacketAsync(client, packet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling packet {0} from client {1}", 
                packet.Opcode, client.ConnectionId);
        }
    }
    
    private static async Task GameLoopAsync()
    {
        var gameTime = _world.GameTime;
        var saveInterval = TimeSpan.FromMinutes(5);
        var lastSave = DateTime.UtcNow;
        var statusInterval = TimeSpan.FromSeconds(30);
        var lastStatus = DateTime.UtcNow;
        
        _logger.LogInformation("Game loop started ({0} ticks/sec)", GameTime.TicksPerSecond);
        
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                // Wait for next tick
                var waitMs = gameTime.MillisecondsUntilNextTick();
                if (waitMs > 0)
                {
                    await Task.Delay(waitMs, _cts.Token);
                }
                
                // Process tick
                if (gameTime.ShouldTick())
                {
                    gameTime.Tick();
                    _world.Update();
                }
                
                // Periodic save
                if (DateTime.UtcNow - lastSave > saveInterval)
                {
                    lastSave = DateTime.UtcNow;
                    await _dataStore.SaveAsync();
                    await _world.SaveAsync();
                }
                
                // Status log
                if (DateTime.UtcNow - lastStatus > statusInterval)
                {
                    lastStatus = DateTime.UtcNow;
                    _logger.LogInformation(
                        "Status: {0} players, {1} entities, {2} connections, Tick {3}",
                        _world.OnlinePlayerCount,
                        _world.EntityCount,
                        _server.ConnectionCount,
                        gameTime.TickCount);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in game loop");
            }
        }
    }
    
    private static async Task ShutdownAsync()
    {
        _logger.LogInformation("Shutting down...");
        
        // Stop accepting new connections
        _server.Stop();
        
        // Save data
        await _dataStore.SaveAsync();
        await _world.SaveAsync();
        
        // Dispose
        _server.Dispose();
        
        _logger.LogInformation("Server shutdown complete");
    }
}
