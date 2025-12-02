using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using RealmOfReality.Shared.Core;
using RealmOfReality.Shared.Entities;
using RealmOfReality.Shared.Items;

namespace RealmOfReality.Server.Data;

/// <summary>
/// World state snapshot for persistence
/// </summary>
public class WorldSnapshot
{
    public DateTime SavedAt { get; init; }
    public long ServerTick { get; init; }
    public string Version { get; init; } = "1.0";
    
    // Entities
    public List<NpcSaveData> Npcs { get; init; } = new();
    public List<ItemSaveData> GroundItems { get; init; } = new();
    public List<ContainerSaveData> Containers { get; init; } = new();
    
    // Dynamic world state
    public Dictionary<string, object?> WorldProperties { get; init; } = new();
    
    // Spawner states
    public List<SpawnerState> Spawners { get; init; } = new();
}

/// <summary>
/// NPC save data
/// </summary>
public class NpcSaveData
{
    public ulong EntityId { get; init; }
    public ushort TypeId { get; init; }
    public string Name { get; init; } = "";
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
    public ushort MapId { get; init; }
    public byte Facing { get; init; }
    public ushort Hue { get; init; }
    
    // Stats
    public int Health { get; init; }
    public int MaxHealth { get; init; }
    public int Level { get; init; }
    
    // Behavior
    public byte Behavior { get; init; }
    public float SpawnX { get; init; }
    public float SpawnY { get; init; }
    public float SpawnZ { get; init; }
    
    // Custom properties
    public Dictionary<string, object?> Properties { get; init; } = new();
    
    // Script reference
    public string? ScriptName { get; init; }
}

/// <summary>
/// Item save data
/// </summary>
public class ItemSaveData
{
    public ulong ItemId { get; init; }
    public ushort TemplateId { get; init; }
    public int Amount { get; init; }
    public int Durability { get; init; }
    public int MaxDurability { get; init; }
    
    // Location (for ground items)
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
    public ushort MapId { get; init; }
    
    // Custom properties
    public Dictionary<string, object?> Properties { get; init; } = new();
}

/// <summary>
/// Container save data
/// </summary>
public class ContainerSaveData
{
    public ulong ContainerId { get; init; }
    public ushort TypeId { get; init; }
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
    public ushort MapId { get; init; }
    public bool IsLocked { get; init; }
    public int KeyId { get; init; }
    
    public List<ItemSaveData> Items { get; init; } = new();
}

/// <summary>
/// Spawner state for persistence
/// </summary>
public class SpawnerState
{
    public string SpawnerId { get; init; } = "";
    public DateTime LastSpawn { get; init; }
    public int CurrentCount { get; init; }
    public List<ulong> SpawnedEntityIds { get; init; } = new();
}

/// <summary>
/// World persistence manager - handles save/load of world state
/// </summary>
public class WorldPersistence
{
    private readonly ILogger _logger;
    private readonly string _savePath;
    private readonly EntityManager _entities;
    
    // Auto-save settings
    private Timer? _autoSaveTimer;
    private TimeSpan _autoSaveInterval = TimeSpan.FromMinutes(5);
    private bool _isDirty = false;
    
    // Backup settings
    private int _maxBackups = 10;
    private string _backupPath;
    
    public WorldPersistence(ILogger logger, string savePath, EntityManager entities)
    {
        _logger = logger;
        _savePath = savePath;
        _entities = entities;
        _backupPath = Path.Combine(savePath, "backups");
        
        Directory.CreateDirectory(_savePath);
        Directory.CreateDirectory(_backupPath);
    }
    
    /// <summary>
    /// Start auto-save timer
    /// </summary>
    public void StartAutoSave(TimeSpan? interval = null)
    {
        if (interval.HasValue)
            _autoSaveInterval = interval.Value;
        
        _autoSaveTimer?.Dispose();
        _autoSaveTimer = new Timer(
            _ => AutoSave(),
            null,
            _autoSaveInterval,
            _autoSaveInterval
        );
        
        _logger.LogInformation("Auto-save enabled every {Interval} minutes", _autoSaveInterval.TotalMinutes);
    }
    
    /// <summary>
    /// Stop auto-save
    /// </summary>
    public void StopAutoSave()
    {
        _autoSaveTimer?.Dispose();
        _autoSaveTimer = null;
    }
    
    /// <summary>
    /// Mark world as dirty (needs saving)
    /// </summary>
    public void MarkDirty()
    {
        _isDirty = true;
    }
    
    private void AutoSave()
    {
        if (!_isDirty) return;
        
        try
        {
            _logger.LogInformation("Auto-saving world...");
            SaveWorld(0); // Would pass actual server tick
            _isDirty = false;
            _logger.LogInformation("Auto-save complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-save failed");
        }
    }
    
    /// <summary>
    /// Save the world state
    /// </summary>
    public void SaveWorld(long serverTick)
    {
        var snapshot = CreateSnapshot(serverTick);
        
        var fileName = Path.Combine(_savePath, "world.json");
        var tempFile = fileName + ".tmp";
        
        try
        {
            // Write to temp file first
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(snapshot, options);
            File.WriteAllText(tempFile, json);
            
            // Create backup of existing file
            if (File.Exists(fileName))
            {
                CreateBackup(fileName);
            }
            
            // Atomic rename
            File.Move(tempFile, fileName, overwrite: true);
            
            _logger.LogInformation("World saved: {0} NPCs, {1} items", 
                snapshot.Npcs.Count, snapshot.GroundItems.Count);
        }
        catch
        {
            // Clean up temp file on failure
            if (File.Exists(tempFile))
                File.Delete(tempFile);
            throw;
        }
    }
    
    /// <summary>
    /// Save world in compressed format
    /// </summary>
    public void SaveWorldCompressed(long serverTick)
    {
        var snapshot = CreateSnapshot(serverTick);
        
        var fileName = Path.Combine(_savePath, "world.json.gz");
        var tempFile = fileName + ".tmp";
        
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = false };
            var json = JsonSerializer.Serialize(snapshot, options);
            
            using (var fileStream = new FileStream(tempFile, FileMode.Create))
            using (var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal))
            using (var writer = new StreamWriter(gzipStream))
            {
                writer.Write(json);
            }
            
            if (File.Exists(fileName))
            {
                CreateBackup(fileName);
            }
            
            File.Move(tempFile, fileName, overwrite: true);
            
            _logger.LogInformation("World saved (compressed): {0} bytes", 
                new FileInfo(fileName).Length);
        }
        catch
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
            throw;
        }
    }
    
    /// <summary>
    /// Load world state
    /// </summary>
    public WorldSnapshot? LoadWorld()
    {
        var fileName = Path.Combine(_savePath, "world.json");
        var compressedFile = Path.Combine(_savePath, "world.json.gz");
        
        // Try compressed first
        if (File.Exists(compressedFile))
        {
            try
            {
                using var fileStream = new FileStream(compressedFile, FileMode.Open);
                using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
                using var reader = new StreamReader(gzipStream);
                
                var json = reader.ReadToEnd();
                var snapshot = JsonSerializer.Deserialize<WorldSnapshot>(json);
                
                _logger.LogInformation("Loaded world from compressed file: {0}", snapshot?.SavedAt);
                return snapshot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load compressed world file");
            }
        }
        
        // Try uncompressed
        if (File.Exists(fileName))
        {
            try
            {
                var json = File.ReadAllText(fileName);
                var snapshot = JsonSerializer.Deserialize<WorldSnapshot>(json);
                
                _logger.LogInformation("Loaded world: {0}", snapshot?.SavedAt);
                return snapshot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load world file");
            }
        }
        
        _logger.LogInformation("No world save found, starting fresh");
        return null;
    }
    
    /// <summary>
    /// Create a snapshot of current world state
    /// </summary>
    private WorldSnapshot CreateSnapshot(long serverTick)
    {
        var snapshot = new WorldSnapshot
        {
            SavedAt = DateTime.UtcNow,
            ServerTick = serverTick,
            Version = "1.0"
        };
        
        // Save NPCs
        foreach (var npc in _entities.GetAll<NpcEntity>())
        {
            snapshot.Npcs.Add(new NpcSaveData
            {
                EntityId = npc.Id.Value,
                TypeId = npc.TypeId,
                Name = npc.Name,
                X = npc.Position.X,
                Y = npc.Position.Y,
                Z = npc.Position.Z,
                MapId = npc.MapId,
                Facing = (byte)npc.Facing,
                Hue = npc.Hue,
                Health = npc.Health,
                MaxHealth = npc.MaxHealth,
                Level = npc.Level,
                Behavior = (byte)npc.Behavior,
                SpawnX = npc.SpawnPoint.X,
                SpawnY = npc.SpawnPoint.Y,
                SpawnZ = npc.SpawnPoint.Z
            });
        }
        
        // Note: Ground items and containers would be saved similarly
        // This requires tracking them in a collection
        
        return snapshot;
    }
    
    /// <summary>
    /// Restore world state from snapshot
    /// </summary>
    public void RestoreWorld(WorldSnapshot snapshot)
    {
        _logger.LogInformation("Restoring world from save dated {0}", snapshot.SavedAt);
        
        // Clear existing entities (except players)
        var toRemove = _entities.GetAll()
            .Where(e => e is not PlayerEntity)
            .Select(e => e.Id)
            .ToList();
        
        foreach (var id in toRemove)
        {
            _entities.Remove(id);
        }
        
        // Restore NPCs
        foreach (var npcData in snapshot.Npcs)
        {
            var npc = new NpcEntity
            {
                Id = new EntityId(npcData.EntityId),
                TypeId = npcData.TypeId,
                Name = npcData.Name,
                Position = new WorldPosition(npcData.X, npcData.Y, npcData.Z),
                MapId = npcData.MapId,
                Facing = (Direction)npcData.Facing,
                Hue = npcData.Hue,
                Health = npcData.Health,
                MaxHealth = npcData.MaxHealth,
                Level = npcData.Level,
                Behavior = (NpcBehavior)npcData.Behavior,
                SpawnPoint = new WorldPosition(npcData.SpawnX, npcData.SpawnY, npcData.SpawnZ)
            };
            
            _entities.Add(npc);
        }
        
        _logger.LogInformation("Restored {0} NPCs", snapshot.Npcs.Count);
    }
    
    /// <summary>
    /// Create backup of a file
    /// </summary>
    private void CreateBackup(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupFile = Path.Combine(_backupPath, $"{fileName}.{timestamp}.bak");
        
        File.Copy(filePath, backupFile, overwrite: true);
        
        // Clean old backups
        var backups = Directory.GetFiles(_backupPath, $"{fileName}.*.bak")
            .OrderByDescending(f => f)
            .Skip(_maxBackups)
            .ToList();
        
        foreach (var oldBackup in backups)
        {
            try
            {
                File.Delete(oldBackup);
            }
            catch
            {
                // Ignore deletion failures
            }
        }
    }
    
    /// <summary>
    /// List available backups
    /// </summary>
    public IEnumerable<(string FileName, DateTime Date, long Size)> ListBackups()
    {
        if (!Directory.Exists(_backupPath))
            yield break;
        
        foreach (var file in Directory.GetFiles(_backupPath, "*.bak"))
        {
            var info = new FileInfo(file);
            yield return (info.Name, info.CreationTimeUtc, info.Length);
        }
    }
    
    /// <summary>
    /// Restore from a specific backup
    /// </summary>
    public WorldSnapshot? RestoreBackup(string backupFileName)
    {
        var backupPath = Path.Combine(_backupPath, backupFileName);
        
        if (!File.Exists(backupPath))
        {
            _logger.LogError("Backup file not found: {0}", backupFileName);
            return null;
        }
        
        try
        {
            var json = File.ReadAllText(backupPath);
            return JsonSerializer.Deserialize<WorldSnapshot>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore backup: {0}", backupFileName);
            return null;
        }
    }
    
    public void Dispose()
    {
        _autoSaveTimer?.Dispose();
    }
}

/// <summary>
/// Extension to save player-specific data
/// </summary>
public static class PlayerPersistenceExtensions
{
    /// <summary>
    /// Save player equipment to character data
    /// </summary>
    public static Dictionary<string, object?> SaveEquipment(this Equipment equipment)
    {
        var data = new Dictionary<string, object?>();
        
        foreach (var (slot, item) in equipment.GetAllEquipped())
        {
            data[$"slot_{(int)slot}"] = new Dictionary<string, object?>
            {
                ["template"] = item.TemplateId,
                ["amount"] = item.Amount,
                ["durability"] = item.Durability,
                ["maxDurability"] = item.MaxDurability,
                ["properties"] = item.Properties
            };
        }
        
        return data;
    }
    
    /// <summary>
    /// Save player inventory to character data
    /// </summary>
    public static List<Dictionary<string, object?>> SaveInventory(this Inventory inventory)
    {
        var items = new List<Dictionary<string, object?>>();
        
        foreach (var (slot, item) in inventory.GetAllItems())
        {
            items.Add(new Dictionary<string, object?>
            {
                ["slot"] = slot,
                ["template"] = item.TemplateId,
                ["amount"] = item.Amount,
                ["durability"] = item.Durability,
                ["maxDurability"] = item.MaxDurability,
                ["properties"] = item.Properties
            });
        }
        
        return items;
    }
}
