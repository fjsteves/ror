using RealmOfReality.Shared.Core;
using RealmOfReality.Shared.Serialization;

namespace RealmOfReality.Shared.World;

/// <summary>
/// Tile flags for terrain properties
/// </summary>
[Flags]
public enum TileFlags : byte
{
    None = 0,
    Impassable = 1 << 0,   // Cannot walk through
    Water = 1 << 1,        // Water tile (requires swimming)
    NoShoot = 1 << 2,      // Blocks projectiles
    Bridge = 1 << 3,       // Bridge over water
    Roof = 1 << 4,         // Has roof (affects rain, etc.)
    Interior = 1 << 5,     // Inside a building
    PvPZone = 1 << 6,      // PvP enabled
    SafeZone = 1 << 7,     // No combat allowed
}

/// <summary>
/// Single tile data
/// </summary>
public struct Tile
{
    /// <summary>
    /// Ground texture/graphic ID
    /// </summary>
    public ushort GroundId;
    
    /// <summary>
    /// Static item on this tile (0 = none)
    /// </summary>
    public ushort StaticId;
    
    /// <summary>
    /// Base elevation of this tile
    /// </summary>
    public sbyte Elevation;
    
    /// <summary>
    /// Height of static object (for collision)
    /// </summary>
    public byte StaticHeight;
    
    /// <summary>
    /// Tile properties
    /// </summary>
    public TileFlags Flags;
    
    /// <summary>
    /// Color tint for ground
    /// </summary>
    public ushort GroundHue;
    
    /// <summary>
    /// Color tint for static
    /// </summary>
    public ushort StaticHue;
    
    public bool IsPassable => !Flags.HasFlag(TileFlags.Impassable);
    public bool IsWater => Flags.HasFlag(TileFlags.Water);
    public bool IsBridge => Flags.HasFlag(TileFlags.Bridge);
    
    /// <summary>
    /// Get the top Z coordinate of this tile (ground + static height)
    /// </summary>
    public float TopZ => Elevation + (StaticId != 0 ? StaticHeight : 0);
    
    public static Tile Empty => new()
    {
        GroundId = 0,
        StaticId = 0,
        Elevation = 0,
        StaticHeight = 0,
        Flags = TileFlags.Impassable
    };
    
    public static Tile Grass => new()
    {
        GroundId = 1,
        Elevation = 0,
        Flags = TileFlags.None
    };
    
    public static Tile Water => new()
    {
        GroundId = 2,
        Elevation = -1,
        Flags = TileFlags.Water | TileFlags.Impassable
    };
}

/// <summary>
/// A chunk of tiles (for efficient loading/streaming)
/// </summary>
public class MapChunk
{
    public const int ChunkSize = 32; // 32x32 tiles per chunk
    
    public int ChunkX { get; }
    public int ChunkY { get; }
    
    private readonly Tile[,] _tiles = new Tile[ChunkSize, ChunkSize];
    
    public MapChunk(int chunkX, int chunkY)
    {
        ChunkX = chunkX;
        ChunkY = chunkY;
        
        // Initialize with passable grass tiles (not Empty which is impassable)
        for (var y = 0; y < ChunkSize; y++)
        for (var x = 0; x < ChunkSize; x++)
            _tiles[x, y] = Tile.Grass;
    }
    
    /// <summary>
    /// Get tile at local chunk coordinates
    /// </summary>
    public ref Tile GetTile(int localX, int localY)
    {
        return ref _tiles[localX, localY];
    }
    
    /// <summary>
    /// Set tile at local chunk coordinates
    /// </summary>
    public void SetTile(int localX, int localY, Tile tile)
    {
        _tiles[localX, localY] = tile;
    }
    
    /// <summary>
    /// Convert world tile position to chunk-local position
    /// </summary>
    public static (int localX, int localY) WorldToLocal(TilePosition worldPos)
    {
        var localX = ((worldPos.X % ChunkSize) + ChunkSize) % ChunkSize;
        var localY = ((worldPos.Y % ChunkSize) + ChunkSize) % ChunkSize;
        return (localX, localY);
    }
    
    /// <summary>
    /// Get chunk coordinates from world tile position
    /// </summary>
    public static (int chunkX, int chunkY) GetChunkCoords(TilePosition worldPos)
    {
        var chunkX = worldPos.X >= 0 ? worldPos.X / ChunkSize : (worldPos.X - ChunkSize + 1) / ChunkSize;
        var chunkY = worldPos.Y >= 0 ? worldPos.Y / ChunkSize : (worldPos.Y - ChunkSize + 1) / ChunkSize;
        return (chunkX, chunkY);
    }
    
    /// <summary>
    /// Serialize chunk to binary
    /// </summary>
    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt32(ChunkX);
        writer.WriteInt32(ChunkY);
        
        for (var y = 0; y < ChunkSize; y++)
        for (var x = 0; x < ChunkSize; x++)
        {
            var tile = _tiles[x, y];
            writer.WriteUInt16(tile.GroundId);
            writer.WriteUInt16(tile.StaticId);
            writer.WriteSByte(tile.Elevation);
            writer.WriteByte(tile.StaticHeight);
            writer.WriteByte((byte)tile.Flags);
            writer.WriteUInt16(tile.GroundHue);
            writer.WriteUInt16(tile.StaticHue);
        }
    }
    
    /// <summary>
    /// Deserialize chunk from binary
    /// </summary>
    public static MapChunk Deserialize(ref PacketReader reader)
    {
        var chunkX = reader.ReadInt32();
        var chunkY = reader.ReadInt32();
        var chunk = new MapChunk(chunkX, chunkY);
        
        for (var y = 0; y < ChunkSize; y++)
        for (var x = 0; x < ChunkSize; x++)
        {
            chunk._tiles[x, y] = new Tile
            {
                GroundId = reader.ReadUInt16(),
                StaticId = reader.ReadUInt16(),
                Elevation = reader.ReadSByte(),
                StaticHeight = reader.ReadByte(),
                Flags = (TileFlags)reader.ReadByte(),
                GroundHue = reader.ReadUInt16(),
                StaticHue = reader.ReadUInt16()
            };
        }
        
        return chunk;
    }
}

/// <summary>
/// Complete map definition
/// </summary>
public class GameMap
{
    public ushort MapId { get; }
    public string Name { get; set; } = "";
    public int Width { get; } // In tiles
    public int Height { get; }
    
    private readonly Dictionary<(int, int), MapChunk> _chunks = new();
    
    public GameMap(ushort mapId, int width, int height)
    {
        MapId = mapId;
        Width = width;
        Height = height;
    }
    
    /// <summary>
    /// Get or create a chunk
    /// </summary>
    public MapChunk GetChunk(int chunkX, int chunkY)
    {
        var key = (chunkX, chunkY);
        if (!_chunks.TryGetValue(key, out var chunk))
        {
            chunk = new MapChunk(chunkX, chunkY);
            _chunks[key] = chunk;
        }
        return chunk;
    }
    
    /// <summary>
    /// Get tile at world position
    /// </summary>
    public Tile GetTile(TilePosition pos)
    {
        var (chunkX, chunkY) = MapChunk.GetChunkCoords(pos);
        var (localX, localY) = MapChunk.WorldToLocal(pos);
        
        if (!_chunks.TryGetValue((chunkX, chunkY), out var chunk))
            return Tile.Grass; // Default to passable grass for unloaded areas
        
        return chunk.GetTile(localX, localY);
    }
    
    /// <summary>
    /// Set tile at world position
    /// </summary>
    public void SetTile(TilePosition pos, Tile tile)
    {
        var (chunkX, chunkY) = MapChunk.GetChunkCoords(pos);
        var (localX, localY) = MapChunk.WorldToLocal(pos);
        
        var chunk = GetChunk(chunkX, chunkY);
        chunk.SetTile(localX, localY, tile);
    }
    
    /// <summary>
    /// Check if a position is passable
    /// </summary>
    public bool IsPassable(TilePosition pos)
    {
        if (pos.X < 0 || pos.X >= Width || pos.Y < 0 || pos.Y >= Height)
            return false;
        
        return GetTile(pos).IsPassable;
    }
    
    /// <summary>
    /// Check if there's line of sight between two positions
    /// </summary>
    public bool HasLineOfSight(TilePosition from, TilePosition to)
    {
        // Bresenham's line algorithm
        var dx = Math.Abs(to.X - from.X);
        var dy = Math.Abs(to.Y - from.Y);
        var sx = from.X < to.X ? 1 : -1;
        var sy = from.Y < to.Y ? 1 : -1;
        var err = dx - dy;
        
        var x = from.X;
        var y = from.Y;
        
        while (x != to.X || y != to.Y)
        {
            var tile = GetTile(new TilePosition(x, y));
            if (tile.Flags.HasFlag(TileFlags.NoShoot))
                return false;
            
            var e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Get all loaded chunks
    /// </summary>
    public IEnumerable<MapChunk> GetLoadedChunks() => _chunks.Values;
    
    /// <summary>
    /// Save map to file
    /// </summary>
    public async Task SaveAsync(string path)
    {
        await using var stream = File.Create(path);
        await using var writer = new BinaryWriter(stream);
        
        // Header
        writer.Write((byte)'R');
        writer.Write((byte)'O');
        writer.Write((byte)'R');
        writer.Write((byte)'M'); // RORM = Realm of Reality Map
        writer.Write((byte)1); // Version
        
        writer.Write(MapId);
        writer.Write(Name ?? "");
        writer.Write(Width);
        writer.Write(Height);
        writer.Write(_chunks.Count);
        
        // Chunks
        foreach (var chunk in _chunks.Values)
        {
            using var packetWriter = new PacketWriter();
            chunk.Serialize(packetWriter);
            var data = packetWriter.ToArray();
            writer.Write(data.Length);
            writer.Write(data);
        }
    }
    
    /// <summary>
    /// Load map from file
    /// </summary>
    public static async Task<GameMap> LoadAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        
        // Verify header
        if (reader.ReadByte() != 'R' || reader.ReadByte() != 'O' ||
            reader.ReadByte() != 'R' || reader.ReadByte() != 'M')
            throw new InvalidDataException("Invalid map file format");
        
        var version = reader.ReadByte();
        if (version != 1)
            throw new InvalidDataException($"Unsupported map version: {version}");
        
        var mapId = reader.ReadUInt16();
        var name = reader.ReadString();
        var width = reader.ReadInt32();
        var height = reader.ReadInt32();
        var chunkCount = reader.ReadInt32();
        
        var map = new GameMap(mapId, width, height) { Name = name };
        
        for (var i = 0; i < chunkCount; i++)
        {
            var dataLength = reader.ReadInt32();
            var data = reader.ReadBytes(dataLength);
            var chunk = DeserializeChunk(data);
            map._chunks[(chunk.ChunkX, chunk.ChunkY)] = chunk;
        }
        
        return map;
    }
    
    private static MapChunk DeserializeChunk(byte[] data)
    {
        var packetReader = new PacketReader(data);
        return MapChunk.Deserialize(ref packetReader);
    }
}

/// <summary>
/// Spawn point definition
/// </summary>
public class SpawnPoint
{
    public string Id { get; set; } = "";
    public WorldPosition Position { get; set; }
    public ushort MapId { get; set; }
    public SpawnType Type { get; set; }
}

public enum SpawnType
{
    PlayerStart,
    PlayerRespawn,
    NpcSpawn,
    BossSpawn,
    Teleporter
}
