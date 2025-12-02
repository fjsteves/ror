// ==========================================================================
// MapLoader.cs - UO map*.mul / statics*.mul parser
// ==========================================================================
// Loads terrain and static data from UO map files.
// Supports both MUL and UOP formats.
// Uses column-major block indexing per ClassicUO specification.
// ==========================================================================

using Microsoft.Xna.Framework.Graphics;

namespace RealmOfReality.Client.Rendering.Loaders;

/// <summary>
/// Loads map terrain and statics from UO data files.
/// </summary>
public sealed class MapLoader : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly string _dataPath;
    
    // Map dimensions
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int BlockWidth => Width >> 3;
    public int BlockHeight => Height >> 3;
    public int Facet { get; private set; }
    
    // File handles
    private FileStream? _mapStream;
    private BinaryReader? _mapReader;
    private FileStream? _staidxStream;
    private BinaryReader? _staidxReader;
    private FileStream? _staticsStream;
    private BinaryReader? _staticsReader;
    
    // UOP support
    private UopFileReader? _mapUopReader;
    private bool _useUop;
    
    // Caches
    private readonly Dictionary<int, MapBlock> _blockCache = new();
    private readonly Dictionary<int, StaticTile[]> _staticCache = new();
    private const int MAX_CACHE_SIZE = 1024;
    
    public bool IsLoaded { get; private set; }
    public bool IsUsingUop => _useUop;
    
    public MapLoader(GraphicsDevice graphicsDevice, string dataPath)
    {
        _graphicsDevice = graphicsDevice;
        _dataPath = dataPath;
    }
    
    /// <summary>
    /// Load map data for a specific facet.
    /// </summary>
    public bool Load(int facet = 0)
    {
        Facet = facet;
        
        try
        {
            // Set dimensions
            (Width, Height) = UOConstants.GetMapDimensions(facet);
            string facetName = UOConstants.GetFacetName(facet);
            
            Console.WriteLine($"[MapLoader] Loading {facetName} (facet {facet}): {Width}x{Height} tiles");
            Console.WriteLine($"[MapLoader] Block dimensions: {BlockWidth}x{BlockHeight}");
            
            // Try UOP first
            string uopPath = Path.Combine(_dataPath, $"map{facet}LegacyMUL.uop");
            if (File.Exists(uopPath))
            {
                if (TryLoadUop(uopPath, facet))
                {
                    LoadStatics(facet);
                    IsLoaded = true;
                    return true;
                }
            }
            
            // Fall back to MUL
            string mulPath = Path.Combine(_dataPath, $"map{facet}.mul");
            if (File.Exists(mulPath))
            {
                if (TryLoadMul(mulPath))
                {
                    LoadStatics(facet);
                    IsLoaded = true;
                    return true;
                }
            }
            
            Console.WriteLine($"[MapLoader] No valid map file found for facet {facet}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MapLoader] Load error: {ex.Message}");
            return false;
        }
    }
    
    private bool TryLoadUop(string path, int facet)
    {
        try
        {
            Console.WriteLine($"[MapLoader] Trying UOP: {Path.GetFileName(path)}");
            
            _mapUopReader = new UopFileReader(path, $"build/map{facet}legacymul/{{0:D8}}.dat");
            if (!_mapUopReader.Load())
            {
                _mapUopReader.Dispose();
                _mapUopReader = null;
                return false;
            }
            
            _useUop = true;
            Console.WriteLine($"[MapLoader] UOP loaded: {_mapUopReader.EntryCount} entries");
            return true;
        }
        catch
        {
            _mapUopReader?.Dispose();
            _mapUopReader = null;
            return false;
        }
    }
    
    private bool TryLoadMul(string path)
    {
        try
        {
            Console.WriteLine($"[MapLoader] Loading MUL: {Path.GetFileName(path)}");
            
            _mapStream = File.OpenRead(path);
            _mapReader = new BinaryReader(_mapStream);
            
            // Verify size
            long expectedSize = (long)BlockWidth * BlockHeight * UOConstants.BLOCK_BYTES;
            if (_mapStream.Length < expectedSize)
            {
                Console.WriteLine($"[MapLoader] WARNING: File size {_mapStream.Length:N0} < expected {expectedSize:N0}");
            }
            
            Console.WriteLine($"[MapLoader] MUL loaded: {_mapStream.Length:N0} bytes");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MapLoader] MUL error: {ex.Message}");
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
            Console.WriteLine("[MapLoader] Statics files not found (optional)");
            return;
        }
        
        try
        {
            _staidxStream = File.OpenRead(staidxPath);
            _staidxReader = new BinaryReader(_staidxStream);
            _staticsStream = File.OpenRead(staticsPath);
            _staticsReader = new BinaryReader(_staticsStream);
            
            Console.WriteLine($"[MapLoader] Statics loaded: idx={_staidxStream.Length:N0}, data={_staticsStream.Length:N0}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MapLoader] Statics error: {ex.Message}");
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
    
    // ========================================================================
    // PUBLIC ACCESSORS
    // ========================================================================
    
    /// <summary>
    /// Get land tile at world coordinates.
    /// </summary>
    public LandTile GetLandTile(int worldX, int worldY)
    {
        if (!IsLoaded)
            return new LandTile(3, 0); // Default grass
        
        if (worldX < 0 || worldX >= Width || worldY < 0 || worldY >= Height)
            return new LandTile(168, -5); // Water for out of bounds
        
        var (blockX, blockY) = IsometricMath.WorldToBlock(worldX, worldY);
        var block = GetBlock(blockX, blockY);
        
        if (block == null)
            return new LandTile(3, 0);
        
        var (cellX, cellY) = IsometricMath.WorldToCell(worldX, worldY);
        return block.GetTile(cellX, cellY);
    }
    
    /// <summary>
    /// Get static tiles at world coordinates.
    /// </summary>
    public StaticTile[] GetStaticTiles(int worldX, int worldY)
    {
        if (!IsLoaded || _staidxReader == null)
            return Array.Empty<StaticTile>();
        
        if (worldX < 0 || worldX >= Width || worldY < 0 || worldY >= Height)
            return Array.Empty<StaticTile>();
        
        var (blockX, blockY) = IsometricMath.WorldToBlock(worldX, worldY);
        var (cellX, cellY) = IsometricMath.WorldToCell(worldX, worldY);
        
        var blockStatics = GetBlockStatics(blockX, blockY);
        return blockStatics.Where(s => s.X == cellX && s.Y == cellY).ToArray();
    }
    
    /// <summary>
    /// Get all static tiles in a block.
    /// </summary>
    public StaticTile[] GetBlockStatics(int blockX, int blockY)
    {
        if (_staidxReader == null || _staticsReader == null)
            return Array.Empty<StaticTile>();
        
        int blockIndex = IsometricMath.CalculateBlockIndex(blockX, blockY, BlockHeight);
        
        if (_staticCache.TryGetValue(blockIndex, out var cached))
            return cached;
        
        try
        {
            // Read index entry
            long idxOffset = (long)blockIndex * UOConstants.INDEX_ENTRY_BYTES;
            if (idxOffset + 12 > _staidxStream!.Length)
                return CacheStatics(blockIndex, Array.Empty<StaticTile>());
            
            lock (_staidxStream)
            {
                _staidxStream.Seek(idxOffset, SeekOrigin.Begin);
                uint dataOffset = _staidxReader.ReadUInt32();
                uint dataLength = _staidxReader.ReadUInt32();
                
                if (dataOffset == 0xFFFFFFFF || dataLength == 0 || dataLength > 100000)
                    return CacheStatics(blockIndex, Array.Empty<StaticTile>());
                
                int count = (int)(dataLength / UOConstants.STATIC_TILE_BYTES);
                if (count > 1024) count = 1024;
                
                var statics = new StaticTile[count];
                
                lock (_staticsStream!)
                {
                    _staticsStream.Seek(dataOffset, SeekOrigin.Begin);
                    
                    for (int i = 0; i < count; i++)
                    {
                        ushort itemId = _staticsReader.ReadUInt16();
                        byte x = (byte)(_staticsReader.ReadByte() & 0x07);
                        byte y = (byte)(_staticsReader.ReadByte() & 0x07);
                        sbyte z = _staticsReader.ReadSByte();
                        short hue = _staticsReader.ReadInt16();
                        
                        statics[i] = new StaticTile(itemId, x, y, z, hue);
                    }
                }
                
                return CacheStatics(blockIndex, statics);
            }
        }
        catch
        {
            return CacheStatics(blockIndex, Array.Empty<StaticTile>());
        }
    }
    
    // ========================================================================
    // BLOCK LOADING
    // ========================================================================
    
    private MapBlock? GetBlock(int blockX, int blockY)
    {
        int blockIndex = IsometricMath.CalculateBlockIndex(blockX, blockY, BlockHeight);
        
        if (_blockCache.TryGetValue(blockIndex, out var cached))
            return cached;
        
        try
        {
            byte[]? blockData = null;
            
            if (_useUop && _mapUopReader != null)
            {
                blockData = ReadBlockFromUop(blockIndex);
            }
            else if (_mapReader != null)
            {
                blockData = ReadBlockFromMul(blockIndex);
            }
            
            if (blockData == null || blockData.Length < UOConstants.BLOCK_BYTES)
                return null;
            
            var block = ParseBlock(blockData);
            CacheBlock(blockIndex, block);
            return block;
        }
        catch
        {
            return null;
        }
    }
    
    private byte[]? ReadBlockFromMul(int blockIndex)
    {
        long offset = (long)blockIndex * UOConstants.BLOCK_BYTES;
        
        if (offset + UOConstants.BLOCK_BYTES > _mapStream!.Length)
            return null;
        
        lock (_mapStream)
        {
            _mapStream.Seek(offset, SeekOrigin.Begin);
            return _mapReader!.ReadBytes(UOConstants.BLOCK_BYTES);
        }
    }
    
    private byte[]? ReadBlockFromUop(int blockIndex)
    {
        // Each UOP entry contains 4096 blocks
        int entryIndex = blockIndex / UOConstants.MAP_BLOCKS_PER_UOP_ENTRY;
        int blockInEntry = blockIndex % UOConstants.MAP_BLOCKS_PER_UOP_ENTRY;
        
        var entryData = _mapUopReader!.GetData(entryIndex);
        if (entryData == null)
            return null;
        
        int offset = blockInEntry * UOConstants.BLOCK_BYTES;
        if (offset + UOConstants.BLOCK_BYTES > entryData.Length)
            return null;
        
        var blockData = new byte[UOConstants.BLOCK_BYTES];
        Array.Copy(entryData, offset, blockData, 0, UOConstants.BLOCK_BYTES);
        return blockData;
    }
    
    private static MapBlock ParseBlock(byte[] data)
    {
        var block = new MapBlock
        {
            Header = BinaryUtils.ReadUInt32(data, 0)
        };
        
        int offset = 4;
        for (int i = 0; i < 64; i++)
        {
            ushort tileId = BinaryUtils.ReadUInt16(data, offset);
            sbyte z = (sbyte)data[offset + 2];
            block.Tiles[i] = new LandTile(tileId, z);
            offset += UOConstants.LAND_TILE_BYTES;
        }
        
        return block;
    }
    
    // ========================================================================
    // CACHING
    // ========================================================================
    
    private void CacheBlock(int index, MapBlock block)
    {
        if (_blockCache.Count >= MAX_CACHE_SIZE)
        {
            var toRemove = _blockCache.Keys.Take(MAX_CACHE_SIZE / 2).ToList();
            foreach (var key in toRemove)
                _blockCache.Remove(key);
        }
        _blockCache[index] = block;
    }
    
    private StaticTile[] CacheStatics(int index, StaticTile[] statics)
    {
        if (_staticCache.Count >= MAX_CACHE_SIZE)
        {
            var toRemove = _staticCache.Keys.Take(MAX_CACHE_SIZE / 2).ToList();
            foreach (var key in toRemove)
                _staticCache.Remove(key);
        }
        _staticCache[index] = statics;
        return statics;
    }
    
    /// <summary>
    /// Clear all cached data.
    /// </summary>
    public void ClearCache()
    {
        _blockCache.Clear();
        _staticCache.Clear();
    }
    
    public void Dispose()
    {
        _mapReader?.Dispose();
        _mapStream?.Dispose();
        _staidxReader?.Dispose();
        _staidxStream?.Dispose();
        _staticsReader?.Dispose();
        _staticsStream?.Dispose();
        _mapUopReader?.Dispose();
        
        _blockCache.Clear();
        _staticCache.Clear();
        
        IsLoaded = false;
    }
}
