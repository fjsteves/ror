using System.Text.Json.Serialization;

namespace RealmOfReality.Server.Config;

/// <summary>
/// Server configuration
/// </summary>
public class ServerConfig
{
    /// <summary>
    /// Unique server ID (for distributed ID generation)
    /// </summary>
    public ushort ServerId { get; set; } = 1;
    
    /// <summary>
    /// Server name displayed to players
    /// </summary>
    public string ServerName { get; set; } = "Realm of Reality";
    
    /// <summary>
    /// Network configuration
    /// </summary>
    public NetworkConfig Network { get; set; } = new();
    
    /// <summary>
    /// Game rules configuration
    /// </summary>
    public GameRulesConfig GameRules { get; set; } = new();
    
    /// <summary>
    /// Data paths
    /// </summary>
    public PathsConfig Paths { get; set; } = new();
    
    /// <summary>
    /// Load configuration from file or create default
    /// </summary>
    public static ServerConfig LoadOrCreate(string path)
    {
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            return System.Text.Json.JsonSerializer.Deserialize<ServerConfig>(json) ?? new ServerConfig();
        }
        
        var config = new ServerConfig();
        var json2 = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        File.WriteAllText(path, json2);
        return config;
    }
}

public class NetworkConfig
{
    /// <summary>
    /// IP address to bind to (0.0.0.0 for all interfaces)
    /// </summary>
    public string BindAddress { get; set; } = "0.0.0.0";
    
    /// <summary>
    /// Port to listen on
    /// </summary>
    public int Port { get; set; } = 7775;
    
    /// <summary>
    /// Maximum concurrent connections
    /// </summary>
    public int MaxConnections { get; set; } = 1000;
    
    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Ping interval in seconds
    /// </summary>
    public int PingIntervalSeconds { get; set; } = 10;
}

public class GameRulesConfig
{
    /// <summary>
    /// Maximum characters per account
    /// </summary>
    public int MaxCharactersPerAccount { get; set; } = 5;
    
    /// <summary>
    /// Starting map for new characters
    /// </summary>
    public ushort StartingMapId { get; set; } = 1;
    
    /// <summary>
    /// Starting position for new characters
    /// </summary>
    public float StartingX { get; set; } = 1495;
    public float StartingY { get; set; } = 1629;
    
    /// <summary>
    /// Base stat points for new characters
    /// </summary>
    public int StartingStatTotal { get; set; } = 80;
    
    /// <summary>
    /// Chat range for local chat (in tiles)
    /// </summary>
    public float LocalChatRange { get; set; } = 15;
    
    /// <summary>
    /// Entity visibility range
    /// </summary>
    public float VisibilityRange { get; set; } = 18;
    
    /// <summary>
    /// Allow PvP combat
    /// </summary>
    public bool PvPEnabled { get; set; } = true;
}

public class PathsConfig
{
    public string DataDirectory { get; set; } = "data";
    public string WorldDirectory { get; set; } = "data/world";
    public string AccountsFile { get; set; } = "data/accounts.json";
    public string CharactersDirectory { get; set; } = "data/characters";
    public string LogDirectory { get; set; } = "logs";
}
