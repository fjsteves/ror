// ==========================================================================
// ArtLoader.cs - art.mul / artidx.mul / artLegacyMUL.uop parser
// ==========================================================================
// Loads land tiles (44x44 diamonds) and static items (RLE encoded).
// Supports both MUL and UOP formats.
// ==========================================================================

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RealmOfReality.Client.Rendering.Loaders;

/// <summary>
/// Loaded art tile with texture and metadata.
/// </summary>
public class ArtTile
{
    public Texture2D? Texture { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }
    
    public bool IsValid => Texture != null;
}

/// <summary>
/// Loads art tiles from UO data files.
/// </summary>
public sealed class ArtLoader : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly string _dataPath;
    
    // MUL format
    private FileStream? _mulStream;
    private IndexEntry[]? _mulIndex;
    
    // UOP format
    private UopFileReader? _uopReader;
    private bool _useUop;
    
    // Caches
    private readonly Dictionary<int, ArtTile> _landCache = new();
    private readonly Dictionary<int, ArtTile> _staticCache = new();
    private const int MAX_CACHE_SIZE = 2048;
    
    public bool IsLoaded { get; private set; }
    public bool IsUsingUop => _useUop;
    public int EntryCount => _mulIndex?.Length ?? _uopReader?.EntryCount ?? 0;
    
    public ArtLoader(GraphicsDevice graphicsDevice, string dataPath)
    {
        _graphicsDevice = graphicsDevice;
        _dataPath = dataPath;
    }
    
    /// <summary>
    /// Load art files.
    /// </summary>
    public bool Load()
    {
        // Try UOP first (modern clients)
        string uopPath = Path.Combine(_dataPath, "artLegacyMUL.uop");
        if (File.Exists(uopPath) && TryLoadUop(uopPath))
            return true;
        
        // Fall back to MUL
        string mulPath = Path.Combine(_dataPath, "art.mul");
        string idxPath = Path.Combine(_dataPath, "artidx.mul");
        if (File.Exists(mulPath) && File.Exists(idxPath) && TryLoadMul(mulPath, idxPath))
            return true;
        
        Console.WriteLine("[ArtLoader] No valid art files found");
        return false;
    }
    
    private bool TryLoadUop(string path)
    {
        try
        {
            Console.WriteLine($"[ArtLoader] Loading UOP: {Path.GetFileName(path)}");
            
            _uopReader = new UopFileReader(path, "build/artlegacymul/{0:D8}.tga");
            if (!_uopReader.Load())
            {
                _uopReader.Dispose();
                _uopReader = null;
                return false;
            }
            
            _useUop = true;
            IsLoaded = true;
            Console.WriteLine($"[ArtLoader] UOP loaded: {_uopReader.EntryCount} entries");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArtLoader] UOP error: {ex.Message}");
            _uopReader?.Dispose();
            _uopReader = null;
            return false;
        }
    }
    
    private bool TryLoadMul(string mulPath, string idxPath)
    {
        try
        {
            Console.WriteLine($"[ArtLoader] Loading MUL format");
            
            // Load index
            var idxData = File.ReadAllBytes(idxPath);
            int entryCount = idxData.Length / UOConstants.INDEX_ENTRY_BYTES;
            _mulIndex = new IndexEntry[entryCount];
            
            for (int i = 0; i < entryCount; i++)
            {
                int offset = i * 12;
                _mulIndex[i] = new IndexEntry(
                    BinaryUtils.ReadInt32(idxData, offset),
                    BinaryUtils.ReadInt32(idxData, offset + 4),
                    BinaryUtils.ReadInt32(idxData, offset + 8)
                );
            }
            
            // Open data file
            _mulStream = File.OpenRead(mulPath);
            
            IsLoaded = true;
            Console.WriteLine($"[ArtLoader] MUL loaded: {entryCount} entries");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ArtLoader] MUL error: {ex.Message}");
            _mulIndex = null;
            _mulStream?.Dispose();
            _mulStream = null;
            return false;
        }
    }
    
    // ========================================================================
    // LAND TILES (44x44 diamonds)
    // ========================================================================
    
    /// <summary>
    /// Get a land tile texture by tile ID (0-16383).
    /// </summary>
    public ArtTile? GetLandTile(int tileId)
    {
        if (!IsLoaded)
            return null;
        
        if (tileId < 0 || tileId >= UOConstants.STATIC_OFFSET)
            return null;
        
        if (_landCache.TryGetValue(tileId, out var cached))
            return cached;
        
        var tile = LoadLandTile(tileId);
        if (tile != null)
        {
            CacheLandTile(tileId, tile);
        }
        
        return tile;
    }
    
    private ArtTile? LoadLandTile(int tileId)
    {
        var data = ReadEntry(tileId);
        if (data == null || data.Length < 1000)
            return null;
        
        try
        {
            // UOP may have a 4-byte header
            int dataOffset = 0;
            if (_useUop && data.Length >= 1940)
            {
                dataOffset = 4;
            }
            
            // Land tiles are 44x44 diamonds stored row by row
            // Row widths: 2, 4, 6, ..., 42, 44, 42, ..., 6, 4, 2
            // Total pixels: 2*(1+2+...+21) + 2*22 = 2*231 + 44 = 506? No...
            // Actually: sum of 2,4,6,...,42,44,42,...,6,4,2 = 968 pixels
            
            var pixels = new Color[UOConstants.TILE_SIZE * UOConstants.TILE_SIZE];
            int readPos = dataOffset;
            
            // Top half (rows 0-21): width = 2, 4, 6, ..., 44
            for (int y = 0; y < 22; y++)
            {
                int width = (y + 1) * 2;
                int startX = (UOConstants.TILE_SIZE - width) / 2;
                
                for (int x = 0; x < width && readPos + 1 < data.Length; x++)
                {
                    ushort color16 = BinaryUtils.ReadUInt16(data, readPos);
                    readPos += 2;
                    
                    int pixelIndex = y * UOConstants.TILE_SIZE + startX + x;
                    if (pixelIndex < pixels.Length)
                    {
                        pixels[pixelIndex] = ColorConversion.Argb1555ToColor(color16);
                    }
                }
            }
            
            // Bottom half (rows 22-43): width = 42, 40, ..., 2
            for (int y = 22; y < UOConstants.TILE_SIZE; y++)
            {
                int width = (UOConstants.TILE_SIZE - y - 1) * 2;
                if (width <= 0)
                    break;
                
                int startX = (UOConstants.TILE_SIZE - width) / 2;
                
                for (int x = 0; x < width && readPos + 1 < data.Length; x++)
                {
                    ushort color16 = BinaryUtils.ReadUInt16(data, readPos);
                    readPos += 2;
                    
                    int pixelIndex = y * UOConstants.TILE_SIZE + startX + x;
                    if (pixelIndex < pixels.Length)
                    {
                        pixels[pixelIndex] = ColorConversion.Argb1555ToColor(color16);
                    }
                }
            }
            
            // Create texture
            var texture = new Texture2D(_graphicsDevice, UOConstants.TILE_SIZE, UOConstants.TILE_SIZE);
            texture.SetData(pixels);
            
            return new ArtTile
            {
                Texture = texture,
                Width = UOConstants.TILE_SIZE,
                Height = UOConstants.TILE_SIZE,
                OffsetX = UOConstants.TILE_STEP,
                OffsetY = UOConstants.TILE_STEP
            };
        }
        catch
        {
            return null;
        }
    }
    
    // ========================================================================
    // STATIC ITEMS (RLE encoded)
    // ========================================================================
    
    /// <summary>
    /// Get a static item texture by item ID (0+).
    /// </summary>
    public ArtTile? GetStaticItem(int itemId)
    {
        if (!IsLoaded)
            return null;
        
        if (itemId < 0)
            return null;
        
        if (_staticCache.TryGetValue(itemId, out var cached))
            return cached;
        
        // Static items start at index 0x4000 in the art file
        var tile = LoadStaticItem(itemId + UOConstants.STATIC_OFFSET);
        if (tile != null)
        {
            CacheStaticTile(itemId, tile);
        }
        
        return tile;
    }
    
    private ArtTile? LoadStaticItem(int artIndex)
    {
        var data = ReadEntry(artIndex);
        if (data == null || data.Length < 8)
            return null;
        
        try
        {
            // Static item header
            // 4 bytes: flags
            // 2 bytes: width
            // 2 bytes: height
            // Then: height * 2 bytes for row offset lookup
            // Then: RLE pixel data
            
            int flags = BinaryUtils.ReadInt32(data, 0);
            int width = BinaryUtils.ReadUInt16(data, 4);
            int height = BinaryUtils.ReadUInt16(data, 6);
            
            if (width <= 0 || height <= 0 || width > 1024 || height > 1024)
                return null;
            
            int headerSize = 8;
            int lookupSize = height * 2;
            
            if (data.Length < headerSize + lookupSize)
                return null;
            
            var pixels = new Color[width * height];
            
            // Read each row using the lookup table
            for (int y = 0; y < height; y++)
            {
                // Row offset (relative to end of lookup table)
                int rowOffset = BinaryUtils.ReadUInt16(data, headerSize + y * 2);
                int dataPos = headerSize + lookupSize + rowOffset * 2;
                
                int x = 0;
                while (x < width && dataPos + 3 < data.Length)
                {
                    // Run header: offset (2 bytes) + length (2 bytes)
                    int runOffset = BinaryUtils.ReadUInt16(data, dataPos);
                    int runLength = BinaryUtils.ReadUInt16(data, dataPos + 2);
                    dataPos += 4;
                    
                    // End of row marker
                    if (runOffset == 0 && runLength == 0)
                        break;
                    
                    // Skip transparent pixels
                    x += runOffset;
                    
                    // Read pixel run
                    for (int i = 0; i < runLength && x < width && dataPos + 1 < data.Length; i++)
                    {
                        ushort color16 = BinaryUtils.ReadUInt16(data, dataPos);
                        dataPos += 2;
                        
                        if (x >= 0 && x < width)
                        {
                            int pixelIndex = y * width + x;
                            if (pixelIndex < pixels.Length)
                            {
                                // Force opaque for non-zero colors
                                var color = ColorConversion.Argb1555ToColor(color16);
                                if (color16 != 0)
                                    color.A = 255;
                                pixels[pixelIndex] = color;
                            }
                        }
                        x++;
                    }
                }
            }
            
            // Create texture
            var texture = new Texture2D(_graphicsDevice, width, height);
            texture.SetData(pixels);
            
            return new ArtTile
            {
                Texture = texture,
                Width = width,
                Height = height,
                // Static items are anchored at bottom center
                OffsetX = width / 2,
                OffsetY = height
            };
        }
        catch
        {
            return null;
        }
    }
    
    // ========================================================================
    // FILE ACCESS
    // ========================================================================
    
    private byte[]? ReadEntry(int index)
    {
        if (_useUop && _uopReader != null)
        {
            return _uopReader.GetData(index);
        }
        else if (_mulStream != null && _mulIndex != null)
        {
            if (index < 0 || index >= _mulIndex.Length)
                return null;
            
            var entry = _mulIndex[index];
            if (!entry.IsValid)
                return null;
            
            lock (_mulStream)
            {
                _mulStream.Seek(entry.Lookup, SeekOrigin.Begin);
                var data = new byte[entry.Length];
                _mulStream.Read(data, 0, entry.Length);
                return data;
            }
        }
        
        return null;
    }
    
    // ========================================================================
    // CACHING
    // ========================================================================
    
    private void CacheLandTile(int id, ArtTile tile)
    {
        if (_landCache.Count >= MAX_CACHE_SIZE)
        {
            var toRemove = _landCache.Take(MAX_CACHE_SIZE / 2).ToList();
            foreach (var kvp in toRemove)
            {
                kvp.Value.Texture?.Dispose();
                _landCache.Remove(kvp.Key);
            }
        }
        _landCache[id] = tile;
    }
    
    private void CacheStaticTile(int id, ArtTile tile)
    {
        if (_staticCache.Count >= MAX_CACHE_SIZE)
        {
            var toRemove = _staticCache.Take(MAX_CACHE_SIZE / 2).ToList();
            foreach (var kvp in toRemove)
            {
                kvp.Value.Texture?.Dispose();
                _staticCache.Remove(kvp.Key);
            }
        }
        _staticCache[id] = tile;
    }
    
    /// <summary>
    /// Clear all cached textures.
    /// </summary>
    public void ClearCache()
    {
        foreach (var tile in _landCache.Values)
            tile.Texture?.Dispose();
        foreach (var tile in _staticCache.Values)
            tile.Texture?.Dispose();
        
        _landCache.Clear();
        _staticCache.Clear();
    }
    
    public void Dispose()
    {
        ClearCache();
        
        _mulStream?.Dispose();
        _mulStream = null;
        _mulIndex = null;
        
        _uopReader?.Dispose();
        _uopReader = null;
        
        IsLoaded = false;
    }
}
