using RealmOfReality.Shared.Gumps;
using RealmOfReality.Shared.Network;

namespace RealmOfReality.Server.Gumps;

/// <summary>
/// Manages active gumps for all players
/// </summary>
public class GumpManager
{
    private readonly Dictionary<uint, Gump> _activeGumps = new();
    private readonly Dictionary<ulong, HashSet<uint>> _playerGumps = new(); // playerId -> gump serials
    
    private readonly object _lock = new();
    
    /// <summary>
    /// Register a gump as active for a player
    /// </summary>
    public void RegisterGump(ulong playerId, Gump gump)
    {
        lock (_lock)
        {
            _activeGumps[gump.Serial] = gump;
            
            if (!_playerGumps.TryGetValue(playerId, out var gumps))
            {
                gumps = new HashSet<uint>();
                _playerGumps[playerId] = gumps;
            }
            gumps.Add(gump.Serial);
        }
    }
    
    /// <summary>
    /// Get a gump by serial
    /// </summary>
    public Gump? GetGump(uint serial)
    {
        lock (_lock)
        {
            return _activeGumps.TryGetValue(serial, out var gump) ? gump : null;
        }
    }
    
    /// <summary>
    /// Remove a gump
    /// </summary>
    public Gump? RemoveGump(uint serial)
    {
        lock (_lock)
        {
            if (_activeGumps.TryGetValue(serial, out var gump))
            {
                _activeGumps.Remove(serial);
                
                // Remove from player tracking
                foreach (var playerGumps in _playerGumps.Values)
                {
                    playerGumps.Remove(serial);
                }
                
                return gump;
            }
            return null;
        }
    }
    
    /// <summary>
    /// Get all gumps for a player
    /// </summary>
    public IEnumerable<Gump> GetPlayerGumps(ulong playerId)
    {
        lock (_lock)
        {
            if (!_playerGumps.TryGetValue(playerId, out var serials))
                yield break;
            
            foreach (var serial in serials)
            {
                if (_activeGumps.TryGetValue(serial, out var gump))
                    yield return gump;
            }
        }
    }
    
    /// <summary>
    /// Close all gumps for a player
    /// </summary>
    public void CloseAllPlayerGumps(ulong playerId)
    {
        lock (_lock)
        {
            if (_playerGumps.TryGetValue(playerId, out var serials))
            {
                foreach (var serial in serials.ToList())
                {
                    _activeGumps.Remove(serial);
                }
                _playerGumps.Remove(playerId);
            }
        }
    }
    
    /// <summary>
    /// Close a specific gump type for a player
    /// </summary>
    public Gump? CloseGumpOfType(ulong playerId, uint gumpTypeId)
    {
        lock (_lock)
        {
            if (!_playerGumps.TryGetValue(playerId, out var serials))
                return null;
            
            foreach (var serial in serials.ToList())
            {
                if (_activeGumps.TryGetValue(serial, out var gump) && gump.TypeId == gumpTypeId)
                {
                    _activeGumps.Remove(serial);
                    serials.Remove(serial);
                    return gump;
                }
            }
            return null;
        }
    }
    
    /// <summary>
    /// Check if player has a gump of specific type open
    /// </summary>
    public bool HasGumpOfType(ulong playerId, uint gumpTypeId)
    {
        lock (_lock)
        {
            if (!_playerGumps.TryGetValue(playerId, out var serials))
                return false;
            
            foreach (var serial in serials)
            {
                if (_activeGumps.TryGetValue(serial, out var gump) && gump.TypeId == gumpTypeId)
                    return true;
            }
            return false;
        }
    }
    
    /// <summary>
    /// Handle a gump response from client
    /// </summary>
    public void HandleResponse(ulong playerId, GumpResponse response, object player)
    {
        Gump? gump;
        
        lock (_lock)
        {
            gump = RemoveGump(response.Serial);
        }
        
        if (gump == null)
            return;
        
        if (response.ButtonId == 0)
        {
            // Closed without action
            gump.OnClose(player);
        }
        else
        {
            // Button clicked
            gump.OnResponse(player, response);
        }
    }
}

/// <summary>
/// Extension methods for sending gumps to players
/// </summary>
public static class GumpExtensions
{
    private static GumpManager? _manager;
    
    /// <summary>
    /// Initialize the gump system with a manager
    /// </summary>
    public static void Initialize(GumpManager manager)
    {
        _manager = manager;
    }
    
    /// <summary>
    /// Get the global gump manager
    /// </summary>
    public static GumpManager Manager => _manager ?? throw new InvalidOperationException("GumpManager not initialized");
}
