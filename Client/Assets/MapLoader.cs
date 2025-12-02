using Microsoft.Xna.Framework.Graphics;

namespace RealmOfReality.Client.Assets;

/// <summary>
/// Loads UO map terrain data from map*.mul files and static objects from statics*.mul.
/// 
/// Map File Structure:
/// - 196-byte blocks containing 8×8 tiles each
/// - Block format: 4-byte header + 64 tiles × 3 bytes (TileId:2, Z:1)
/// - Block indexing: COLUMN-MAJOR (blockX * blockHeight + blockY)
/// - Cell indexing within block: ROW-MAJOR (cellY * 8 + cellX)
/// 
/// Statics File Structure:
/// - staidx*.mul: 12-byte index entries (offset, length, extra)
/// - statics*.mul: 7-byte static tile entries (ItemId:2, X:1, Y:1, Z:1, Hue:2)
/// 
/// UOP Support:
/// - map*LegacyMUL.uop: Modern packed format containing same data
/// - Each UOP entry contains 4096 blocks
/// 
/// Reference: ClassicUO MapLoader.cs
/// </summary>
public sealed class MapLoader : IDisposable
{
    // ═══════════════════════════════════════════════════════════════════
    // CONSTANTS
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>Size of a map block in bytes (4 header + 64 tiles × 3 bytes)</summary>
    private const int BLOCK_SIZE = 196;
    
    /// <summary>Tiles per block dimension (8×8 = 64 tiles per block)</summary>
    private const int BLOCK_DIMENSION = 8;
    
    /// <summary>Tiles per block total</summary>
    private const int TILES_PER_BLOCK = 64;
    
    /// <summary>Bytes per static tile entry</summary>
    private const int STATIC_TILE_SIZE = 7;
    
    /// <summary>Bytes per statics index entry</summary>
    private const int STATIC_INDEX_SIZE = 12;
    
    /// <summary>Blocks per UOP entry</summary>
    private const int BLOCKS_PER_UOP_ENTRY = 4096;
    
    /// <summary>Invalid statics index marker</summary>
    private const uint INVALID_OFFSET = 0xFFFFFFFF;
    
    // ═══════════════════════════════════════════════════════════════════
    // STATE
    // ═══════════════════════════════════════════════════════════════════
    
    private readonly GraphicsDevice _graphics;
    private readonly string _dataPath;
    
    // MUL file handles
    private FileStream? _mapStream;
    private BinaryReader? _mapReader;
    private FileStream? _staidxStream;
    private BinaryReader? _staidxReader;
    private FileStream? _staticsStream;
    private BinaryReader? _staticsReader;
    
    // UOP support
    private UopFileReader? _mapUop;
    private bool _useUop;
    
    // Caching
    private readonly Dictionary<int, MapBlock> _blockCache = new();
    private readonly Dictionary<int, StaticTile[]> _staticCache = new();
    private const int MAX_CACHE_SIZE = 512;
    
    // ═══════════════════════════════════════════════════════════════════
    // PUBLIC PROPERTIES
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>Whether map data was loaded successfully</summary>
    public bool IsLoaded { get; private set; }
    
    /// <summary>Map width in tiles</summary>
    public int Width { get; private set; } = 7168;
    
    /// <summary>Map height in tiles</summary>
    public int Height { get; private set; } = 4096;
    
    /// <summary>Map width in blocks (Width / 8)</summary>
    public int BlockWidth => Width >> 3;
    
    /// <summary>Map height in blocks (Height / 8)</summary>
    public int BlockHeight => Height >> 3;
    
    /// <summary>Current facet/map index</summary>
    public int Facet { get; private set; }
    
    /// <summary>Whether statics data is available</summary>
    public bool HasStatics => _staidxReader != null && _staticsReader != null;
    
    // ═══════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════
    
    public MapLoader(GraphicsDevice graphics, string dataPath)
    {
        _graphics = graphics;
        _dataPath = dataPath;
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // LOADING
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Load a map facet.
    /// </summary>
    /// <param name="facet">Facet index (0=Felucca, 1=Trammel, 2=Ilshenar, etc.)</param>
    /// <returns>True if loaded successfully</returns>
    public bool Load(int facet = 0)
    {
        try
        {
            Facet = facet;
            SetDimensions(facet);
            
            Console.WriteLine($"[Map] Loading facet {facet}: {Width}×{Height} tiles ({BlockWidth}×{BlockHeight} blocks)");
            
            // Try UOP format first (modern clients)
            if (TryLoadUop(facet))
            {
                Console.WriteLine($"[Map] Loaded from UOP format");
            }
            // Fall back to MUL format
            else if (TryLoadMul(facet))
            {
                Console.WriteLine($"[Map] Loaded from MUL format");
            }
            else
            {
                Console.WriteLine($"[Map] ERROR: No map data found for facet {facet}");
                return false;
            }
            
            // Load statics (optional but recommended)
            LoadStatics(facet);
            
            IsLoaded = true;
            Console.WriteLine($"[Map] Load complete. Statics: {(HasStatics ? "Available" : "Not found")}");
            
            // Debug: log some sample tiles
            LogSampleTiles();
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Map] Load failed: {ex.Message}");
            Console.WriteLine($"[Map] Stack: {ex.StackTrace}");
            return false;
        }
    }
    
    private void SetDimensions(int facet)
    {
        // Standard UO map dimensions
        (Width, Height) = facet switch
        {
            0 => (7168, 4096),  // Felucca
            1 => (7168, 4096),  // Trammel
            2 => (2304, 1600),  // Ilshenar
            3 => (2560, 2048),  // Malas
            4 => (1448, 1448),  // Tokuno
            5 => (1280, 4096),  // Ter Mur
            _ => (7168, 4096)   // Default
        };
    }
    
    private bool TryLoadUop(int facet)
    {
        string uopPath = Path.Combine(_dataPath, $"map{facet}LegacyMUL.uop");
        
        if (!File.Exists(uopPath))
            return false;
        
        try
        {
            _mapUop = new UopFileReader(uopPath, $"build/map{facet}legacymul/{{0:D8}}.dat");
            if (_mapUop.Load())
            {
                _useUop = true;
                Console.WriteLine($"[Map] UOP: {_mapUop.EntryCount} entries");
                return true;
            }
            
            _mapUop?.Dispose();
            _mapUop = null;
            return false;
        }
        catch
        {
            _mapUop?.Dispose();
            _mapUop = null;
            return false;
        }
    }
    
    private bool TryLoadMul(int facet)
    {
        string mulPath = Path.Combine(_dataPath, $"map{facet}.mul");
        
        if (!File.Exists(mulPath))
            return false;
        
        try
        {
            _mapStream = File.OpenRead(mulPath);
            _mapReader = new BinaryReader(_mapStream);
            
            // Verify file size
            long expectedSize = (long)BlockWidth * BlockHeight * BLOCK_SIZE;
            if (_mapStream.Length < expectedSize)
            {
                Console.WriteLine($"[Map] WARNING: File smaller than expected ({_mapStream.Length:N0} < {expectedSize:N0})");
            }
            
            Console.WriteLine($"[Map] MUL: {_mapStream.Length:N0} bytes");
            return true;
        }
        catch
        {
            _mapReader?.Dispose();
            _mapStream?.Dispose();
            _mapReader = null;
            _mapStream = null;
            return false;
        }
    }
    
    private void LoadStatics(int facet)
    {
        string staidxPath = Path.Combine(_dataPath, $"staidx{facet}.mul");
        string staticsPath = Path.Combine(_dataPath, $"statics{facet}.mul");
        
        if (!File.Exists(staidxPath) || !File.Exists(staticsPath))
        {
            Console.WriteLine($"[Map] Statics files not found (optional)");
            return;
        }
        
        try
        {
            _staidxStream = File.OpenRead(staidxPath);
            _staidxReader = new BinaryReader(_staidxStream);
            _staticsStream = File.OpenRead(staticsPath);
            _staticsReader = new BinaryReader(_staticsStream);
            
            Console.WriteLine($"[Map] Statics: idx={_staidxStream.Length:N0}, data={_staticsStream.Length:N0}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Map] Failed to load statics: {ex.Message}");
            _staidxReader?.Dispose();
            _staidxStream?.Dispose();
            _staticsReader?.Dispose();
            _staticsStream?.Dispose();
            _staidxReader = null;
            _staidxStream = null;
            _staticsReader = null;
            _staticsStream = null;
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // LAND TILE ACCESS
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Get the land tile at world coordinates.
    /// </summary>
    /// <param name="x">World X coordinate</param>
    /// <param name="y">World Y coordinate</param>
    /// <returns>Land tile data</returns>
    public LandTile GetLandTile(int x, int y)
    {
        if (!IsLoaded)
            return new LandTile { TileId = 3, Z = 0 }; // Default grass
        
        // Bounds check
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return new LandTile { TileId = 168, Z = -5 }; // Water for out of bounds
        
        // Get block coordinates
        int blockX = x >> 3;  // x / 8
        int blockY = y >> 3;  // y / 8
        
        // Get the block
        var block = GetBlock(blockX, blockY);
        if (block == null)
            return new LandTile { TileId = 3, Z = 0 };
        
        // Get cell within block (row-major: cellY * 8 + cellX)
        int cellX = x & 7;  // x % 8
        int cellY = y & 7;  // y % 8
        int cellIndex = (cellY << 3) + cellX;
        
        return block.Tiles[cellIndex];
    }
    
    /// <summary>
    /// Get land tiles for a rectangular area.
    /// </summary>
    public LandTile[,] GetLandTileArea(int x, int y, int width, int height)
    {
        var tiles = new LandTile[width, height];
        
        for (int dy = 0; dy < height; dy++)
        {
            for (int dx = 0; dx < width; dx++)
            {
                tiles[dx, dy] = GetLandTile(x + dx, y + dy);
            }
        }
        
        return tiles;
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // STATIC TILE ACCESS
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Get all static tiles at world coordinates.
    /// </summary>
    /// <param name="x">World X coordinate</param>
    /// <param name="y">World Y coordinate</param>
    /// <returns>Array of static tiles (empty if none)</returns>
    public StaticTile[] GetStaticTiles(int x, int y)
    {
        if (!HasStatics)
            return Array.Empty<StaticTile>();
        
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return Array.Empty<StaticTile>();
        
        // Get block coordinates
        int blockX = x >> 3;
        int blockY = y >> 3;
        int cellX = x & 7;
        int cellY = y & 7;
        
        // Get all statics for the block
        var blockStatics = GetBlockStatics(blockX, blockY);
        
        // Filter to just the cell we want
        return blockStatics.Where(s => s.X == cellX && s.Y == cellY).ToArray();
    }
    
    /// <summary>
    /// Get all static tiles within a block.
    /// </summary>
    public StaticTile[] GetBlockStatics(int blockX, int blockY)
    {
        if (!HasStatics)
            return Array.Empty<StaticTile>();
        
        // Calculate block index (column-major)
        int blockIndex = blockX * BlockHeight + blockY;
        
        // Check cache
        if (_staticCache.TryGetValue(blockIndex, out var cached))
            return cached;
        
        // Load from file
        var statics = LoadBlockStatics(blockIndex);
        CacheStatics(blockIndex, statics);
        
        return statics;
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // PRIVATE BLOCK LOADING
    // ═══════════════════════════════════════════════════════════════════
    
    private MapBlock? GetBlock(int blockX, int blockY)
    {
        // Calculate block index (COLUMN-MAJOR per ClassicUO)
        int blockIndex = blockX * BlockHeight + blockY;
        
        // Check cache
        if (_blockCache.TryGetValue(blockIndex, out var cached))
            return cached;
        
        // Load from file
        byte[]? data = _useUop ? ReadBlockFromUop(blockIndex) : ReadBlockFromMul(blockIndex);
        
        if (data == null || data.Length < BLOCK_SIZE)
            return null;
        
        // Parse block
        var block = ParseBlock(data);
        CacheBlock(blockIndex, block);
        
        return block;
    }
    
    private byte[]? ReadBlockFromMul(int blockIndex)
    {
        if (_mapReader == null || _mapStream == null)
            return null;
        
        long offset = (long)blockIndex * BLOCK_SIZE;
        if (offset + BLOCK_SIZE > _mapStream.Length)
            return null;
        
        lock (_mapStream)
        {
            _mapStream.Seek(offset, SeekOrigin.Begin);
            return _mapReader.ReadBytes(BLOCK_SIZE);
        }
    }
    
    private byte[]? ReadBlockFromUop(int blockIndex)
    {
        if (_mapUop == null)
            return null;
        
        // Each UOP entry contains BLOCKS_PER_UOP_ENTRY blocks
        int entryIndex = blockIndex / BLOCKS_PER_UOP_ENTRY;
        int blockInEntry = blockIndex % BLOCKS_PER_UOP_ENTRY;
        
        var entryData = _mapUop.GetData(entryIndex);
        if (entryData == null)
            return null;
        
        int offset = blockInEntry * BLOCK_SIZE;
        if (offset + BLOCK_SIZE > entryData.Length)
            return null;
        
        var blockData = new byte[BLOCK_SIZE];
        Array.Copy(entryData, offset, blockData, 0, BLOCK_SIZE);
        return blockData;
    }
    
    private static MapBlock ParseBlock(byte[] data)
    {
        var block = new MapBlock
        {
            Header = BitConverter.ToUInt32(data, 0),
            Tiles = new LandTile[TILES_PER_BLOCK]
        };
        
        for (int i = 0; i < TILES_PER_BLOCK; i++)
        {
            int offset = 4 + (i * 3);
            block.Tiles[i] = new LandTile
            {
                TileId = BitConverter.ToUInt16(data, offset),
                Z = (sbyte)data[offset + 2]
            };
        }
        
        return block;
    }
    
    private StaticTile[] LoadBlockStatics(int blockIndex)
    {
        if (_staidxReader == null || _staticsReader == null)
            return Array.Empty<StaticTile>();
        
        try
        {
            // Read index entry
            long idxOffset = (long)blockIndex * STATIC_INDEX_SIZE;
            if (idxOffset + STATIC_INDEX_SIZE > _staidxStream!.Length)
                return Array.Empty<StaticTile>();
            
            uint dataOffset, dataLength;
            lock (_staidxStream)
            {
                _staidxStream.Seek(idxOffset, SeekOrigin.Begin);
                dataOffset = _staidxReader.ReadUInt32();
                dataLength = _staidxReader.ReadUInt32();
                // uint extra = _staidxReader.ReadUInt32(); // unused
            }
            
            // Validate
            if (dataOffset == INVALID_OFFSET || dataLength == 0 || dataLength > 100000)
                return Array.Empty<StaticTile>();
            
            // Read static tiles
            int count = (int)(dataLength / STATIC_TILE_SIZE);
            if (count > 1024) count = 1024; // Safety limit
            
            var statics = new StaticTile[count];
            
            lock (_staticsStream!)
            {
                _staticsStream.Seek(dataOffset, SeekOrigin.Begin);
                
                for (int i = 0; i < count; i++)
                {
                    statics[i] = new StaticTile
                    {
                        ItemId = _staticsReader.ReadUInt16(),
                        X = (byte)(_staticsReader.ReadByte() & 0x07),
                        Y = (byte)(_staticsReader.ReadByte() & 0x07),
                        Z = _staticsReader.ReadSByte(),
                        Hue = _staticsReader.ReadInt16()
                    };
                }
            }
            
            return statics;
        }
        catch
        {
            return Array.Empty<StaticTile>();
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // CACHING
    // ═══════════════════════════════════════════════════════════════════
    
    private void CacheBlock(int index, MapBlock block)
    {
        if (_blockCache.Count >= MAX_CACHE_SIZE)
        {
            // Simple eviction: clear half
            var keys = _blockCache.Keys.Take(MAX_CACHE_SIZE / 2).ToList();
            foreach (var k in keys)
                _blockCache.Remove(k);
        }
        _blockCache[index] = block;
    }
    
    private void CacheStatics(int index, StaticTile[] statics)
    {
        if (_staticCache.Count >= MAX_CACHE_SIZE)
        {
            var keys = _staticCache.Keys.Take(MAX_CACHE_SIZE / 2).ToList();
            foreach (var k in keys)
                _staticCache.Remove(k);
        }
        _staticCache[index] = statics;
    }
    
    /// <summary>
    /// Clear all cached data.
    /// </summary>
    public void ClearCache()
    {
        _blockCache.Clear();
        _staticCache.Clear();
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // DEBUG
    // ═══════════════════════════════════════════════════════════════════
    
    private void LogSampleTiles()
    {
        // Log some sample tiles from center of map
        int centerX = Width / 2;
        int centerY = Height / 2;
        
        Console.WriteLine($"[Map] Sample tiles around ({centerX}, {centerY}):");
        for (int i = 0; i < 5; i++)
        {
            var tile = GetLandTile(centerX + i, centerY);
            Console.WriteLine($"[Map]   ({centerX + i},{centerY}): TileId={tile.TileId}, Z={tile.Z}");
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // CLEANUP
    // ═══════════════════════════════════════════════════════════════════
    
    public void Dispose()
    {
        _mapReader?.Dispose();
        _mapStream?.Dispose();
        _staidxReader?.Dispose();
        _staidxStream?.Dispose();
        _staticsReader?.Dispose();
        _staticsStream?.Dispose();
        _mapUop?.Dispose();
        
        _blockCache.Clear();
        _staticCache.Clear();
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// DATA STRUCTURES
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// A map block containing 8×8 land tiles.
/// </summary>
public sealed class MapBlock
{
    /// <summary>Block header (usually 0)</summary>
    public uint Header;
    
    /// <summary>64 land tiles in row-major order (cellY * 8 + cellX)</summary>
    public LandTile[] Tiles = new LandTile[64];
}

/// <summary>
/// A single land tile from the map.
/// </summary>
public struct LandTile
{
    /// <summary>
    /// Land tile graphic ID (0-16383).
    /// Use with TileDataLoader.GetLandTile() to get TextureId for texmaps.
    /// </summary>
    public ushort TileId;
    
    /// <summary>Altitude (-128 to +127)</summary>
    public sbyte Z;
    
    /// <summary>Check if this is a void tile (no terrain)</summary>
    public readonly bool IsVoid => TileId == 0;
    
    public override readonly string ToString() => $"LandTile({TileId}, Z={Z})";
}

/// <summary>
/// A static object on the map (wall, tree, furniture, etc.)
/// </summary>
public struct StaticTile
{
    /// <summary>Static item graphic ID</summary>
    public ushort ItemId;
    
    /// <summary>X position within block (0-7)</summary>
    public byte X;
    
    /// <summary>Y position within block (0-7)</summary>
    public byte Y;
    
    /// <summary>Altitude (-128 to +127)</summary>
    public sbyte Z;
    
    /// <summary>Color hue (0 = default)</summary>
    public short Hue;
    
    public override readonly string ToString() => $"StaticTile({ItemId}, {X},{Y}, Z={Z}, Hue={Hue})";
}
