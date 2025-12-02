using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RealmOfReality.Client.Assets;

/// <summary>
/// Loads stretched terrain textures from texmaps.mul/texidx.mul.
/// 
/// These textures are used for terrain with varying corner heights (stretched tiles).
/// When all 4 corners have the same Z, the flat 44×44 art tile is used instead.
/// 
/// File Format (ClassicUO-compliant):
/// - texidx.mul: Index file with 12-byte entries (offset:4, length:4, extra:4)
/// - texmaps.mul: Raw ARGB1555 pixel data
/// 
/// Texture Sizes:
/// - 64×64 pixels (8,192 bytes) - standard
/// - 128×128 pixels (32,768 bytes) - high resolution
/// 
/// CRITICAL: The index into texmaps.mul is TileData.TextureId, NOT the land tile ID!
/// 
/// Reference: ClassicUO TexmapsLoader.cs
/// </summary>
public sealed class TexmapLoader : IDisposable
{
    // ═══════════════════════════════════════════════════════════════════
    // CONSTANTS
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>Size of each index entry in texidx.mul</summary>
    private const int INDEX_ENTRY_SIZE = 12;
    
    /// <summary>64×64 texture byte count (64 * 64 * 2 bytes per pixel)</summary>
    private const int SIZE_64_BYTES = 64 * 64 * 2;  // 8,192
    
    /// <summary>128×128 texture byte count (128 * 128 * 2 bytes per pixel)</summary>
    private const int SIZE_128_BYTES = 128 * 128 * 2;  // 32,768
    
    /// <summary>Invalid/empty index marker</summary>
    private const uint INVALID_OFFSET = 0xFFFFFFFF;
    
    // ═══════════════════════════════════════════════════════════════════
    // STATE
    // ═══════════════════════════════════════════════════════════════════
    
    private readonly GraphicsDevice _graphics;
    private readonly string _dataPath;
    
    private FileStream? _texmapsStream;
    private BinaryReader? _texmapsReader;
    private FileStream? _texidxStream;
    private BinaryReader? _texidxReader;
    
    // Texture cache (TextureId -> Texture2D)
    private readonly Dictionary<int, Texture2D?> _cache = new();
    private const int MAX_CACHE_SIZE = 512;
    
    // Track statistics
    private int _loadAttempts;
    private int _loadSuccesses;
    private int _cacheHits;
    
    // ═══════════════════════════════════════════════════════════════════
    // PUBLIC PROPERTIES
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>Whether texmaps files were loaded successfully</summary>
    public bool IsLoaded { get; private set; }
    
    /// <summary>Total number of index entries available</summary>
    public int EntryCount { get; private set; }
    
    /// <summary>Statistics: total load attempts</summary>
    public int LoadAttempts => _loadAttempts;
    
    /// <summary>Statistics: successful loads</summary>
    public int LoadSuccesses => _loadSuccesses;
    
    /// <summary>Statistics: cache hits</summary>
    public int CacheHits => _cacheHits;
    
    // ═══════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════
    
    public TexmapLoader(GraphicsDevice graphics, string dataPath)
    {
        _graphics = graphics;
        _dataPath = dataPath;
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // LOADING
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Load texmaps.mul and texidx.mul from the data path.
    /// </summary>
    /// <returns>True if loaded successfully</returns>
    public bool Load()
    {
        try
        {
            string texmapsPath = Path.Combine(_dataPath, "texmaps.mul");
            string texidxPath = Path.Combine(_dataPath, "texidx.mul");
            
            if (!File.Exists(texmapsPath))
            {
                Console.WriteLine($"[Texmaps] texmaps.mul not found: {texmapsPath}");
                return false;
            }
            
            if (!File.Exists(texidxPath))
            {
                Console.WriteLine($"[Texmaps] texidx.mul not found: {texidxPath}");
                return false;
            }
            
            // Open files for reading (keep open for on-demand loading)
            _texmapsStream = new FileStream(texmapsPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _texmapsReader = new BinaryReader(_texmapsStream);
            _texidxStream = new FileStream(texidxPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _texidxReader = new BinaryReader(_texidxStream);
            
            // Calculate entry count from index file size
            EntryCount = (int)(_texidxStream.Length / INDEX_ENTRY_SIZE);
            
            IsLoaded = true;
            Console.WriteLine($"[Texmaps] Loaded: texmaps.mul ({_texmapsStream.Length:N0} bytes), texidx.mul ({_texidxStream.Length:N0} bytes)");
            Console.WriteLine($"[Texmaps] {EntryCount:N0} index entries available");
            
            // Log some sample textures to verify data
            LogSampleTextures();
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Texmaps] Load failed: {ex.Message}");
            Cleanup();
            return false;
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // TEXTURE ACCESS
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Get a texmap texture by TextureId (from TileData).
    /// 
    /// IMPORTANT: Pass TileData.TextureId here, NOT the land tile ID!
    /// </summary>
    /// <param name="textureId">TextureId from TileData.GetLandTile()</param>
    /// <returns>Texture2D or null if not found/invalid</returns>
    public Texture2D? GetTexmap(int textureId)
    {
        _loadAttempts++;
        
        if (!IsLoaded || textureId < 0 || textureId >= EntryCount)
            return null;
        
        // Check cache first
        if (_cache.TryGetValue(textureId, out var cached))
        {
            _cacheHits++;
            return cached;
        }
        
        // Load from file
        var texture = LoadTexture(textureId);
        
        if (texture != null)
            _loadSuccesses++;
        
        // Cache result (even if null to avoid repeated failed lookups)
        CacheTexture(textureId, texture);
        
        return texture;
    }
    
    /// <summary>
    /// Check if a texture exists and is valid without loading it.
    /// </summary>
    public bool HasTexture(int textureId)
    {
        if (!IsLoaded || textureId < 0 || textureId >= EntryCount)
            return false;
        
        // Check cache first
        if (_cache.TryGetValue(textureId, out var cached))
            return cached != null;
        
        // Quick check without full load
        var (offset, length, _) = ReadIndexEntry(textureId);
        return offset != INVALID_OFFSET && offset != 0xFFFFFFFE && length > 0;
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // PRIVATE LOADING
    // ═══════════════════════════════════════════════════════════════════
    
    private Texture2D? LoadTexture(int textureId)
    {
        if (_texidxReader == null || _texmapsReader == null || _texmapsStream == null)
            return null;
        
        try
        {
            // Read index entry
            var (offset, length, extra) = ReadIndexEntry(textureId);
            
            // Validate - ClassicUO checks for 0xFFFFFFFF and 0xFFFFFFFE as invalid
            if (offset == INVALID_OFFSET || offset == 0xFFFFFFFE || length == 0)
                return null;
            
            // Check offset is within file bounds
            if (offset >= _texmapsStream.Length)
            {
                Console.WriteLine($"[Texmaps] TextureId {textureId}: offset {offset} exceeds file size {_texmapsStream.Length}");
                return null;
            }
            
            // Determine texture size from data length
            int size = DetermineTextureSize(length, extra);
            if (size == 0)
            {
                Console.WriteLine($"[Texmaps] TextureId {textureId}: cannot determine size from length={length}, extra={extra}");
                return null;
            }
            
            int expectedBytes = size * size * 2;
            
            // Read pixel data with thread safety
            lock (_texmapsStream)
            {
                _texmapsStream.Seek(offset, SeekOrigin.Begin);
                
                // Read raw bytes first
                int bytesToRead = Math.Min((int)length, expectedBytes);
                var rawData = _texmapsReader.ReadBytes(bytesToRead);
                
                if (rawData.Length < expectedBytes)
                {
                    // Pad with zeros if data is short
                    var paddedData = new byte[expectedBytes];
                    Array.Copy(rawData, paddedData, rawData.Length);
                    rawData = paddedData;
                }
                
                // Convert to colors
                var pixels = new Color[size * size];
                for (int i = 0; i < pixels.Length; i++)
                {
                    int byteOffset = i * 2;
                    if (byteOffset + 1 < rawData.Length)
                    {
                        ushort color16 = (ushort)(rawData[byteOffset] | (rawData[byteOffset + 1] << 8));
                        pixels[i] = ConvertARGB1555(color16);
                    }
                    else
                    {
                        pixels[i] = Color.Magenta; // Debug: show missing data
                    }
                }
                
                // Create texture
                var texture = new Texture2D(_graphics, size, size);
                texture.SetData(pixels);
                
                return texture;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Texmaps] Failed to load texture {textureId}: {ex.Message}");
            return null;
        }
    }
    
    private (uint offset, uint length, uint extra) ReadIndexEntry(int index)
    {
        if (_texidxStream == null || _texidxReader == null)
            return (INVALID_OFFSET, 0, 0);
        
        long position = (long)index * INDEX_ENTRY_SIZE;
        if (position + INDEX_ENTRY_SIZE > _texidxStream.Length)
            return (INVALID_OFFSET, 0, 0);
        
        lock (_texidxStream)
        {
            _texidxStream.Seek(position, SeekOrigin.Begin);
            uint offset = _texidxReader.ReadUInt32();
            uint length = _texidxReader.ReadUInt32();
            uint extra = _texidxReader.ReadUInt32();
            return (offset, length, extra);
        }
    }
    
    /// <summary>
    /// Determine texture size from index entry.
    /// ClassicUO approach: Use data length to determine size.
    /// 64×64 = 8,192 bytes (64*64*2)
    /// 128×128 = 32,768 bytes (128*128*2)
    /// </summary>
    private static int DetermineTextureSize(uint length, uint extra)
    {
        // Primary method: Use length-based detection (most reliable)
        if (length >= SIZE_128_BYTES)
            return 128;
        if (length >= SIZE_64_BYTES)
            return 64;
        
        // Secondary method: Use extra field as hint
        // Some files use extra=0 for 64x64, extra!=0 for 128x128
        if (extra != 0 && length > 0)
            return 128;
        if (extra == 0 && length > 0)
            return 64;
        
        // Fallback: assume 64x64 if we have any data
        if (length > 0)
            return 64;
        
        return 0;
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // COLOR CONVERSION
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Convert ARGB1555 (UO format) to RGBA8888 (XNA/MonoGame format).
    /// Uses ClassicUO's bit replication method for accurate color mapping.
    /// 
    /// ARGB1555 layout:
    /// - Bit 15: Alpha (1 = opaque, 0 = transparent)
    /// - Bits 14-10: Red (5 bits, 0-31)
    /// - Bits 9-5: Green (5 bits, 0-31)
    /// - Bits 4-0: Blue (5 bits, 0-31)
    /// 
    /// The bit replication method copies the top bits to fill the bottom 3 bits,
    /// ensuring 5-bit value 31 maps to 8-bit value 255 (not 248).
    /// </summary>
    private static Color ConvertARGB1555(ushort color16)
    {
        // Color 0 is always transparent in UO
        if (color16 == 0)
            return Color.Transparent;
        
        // Extract 5-bit components (ignore alpha bit for color calculation)
        int r5 = (color16 >> 10) & 0x1F;
        int g5 = (color16 >> 5) & 0x1F;
        int b5 = color16 & 0x1F;
        
        // Bit replication: expand 5-bit (0-31) to 8-bit (0-255)
        // Formula: (value << 3) | (value >> 2)
        // This maps: 0->0, 15->123, 31->255 (exact)
        int r = (r5 << 3) | (r5 >> 2);
        int g = (g5 << 3) | (g5 >> 2);
        int b = (b5 << 3) | (b5 >> 2);
        
        return new Color(r, g, b, 255);
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // CACHING
    // ═══════════════════════════════════════════════════════════════════
    
    private void CacheTexture(int textureId, Texture2D? texture)
    {
        // Evict old entries if cache is full
        if (_cache.Count >= MAX_CACHE_SIZE)
        {
            // Remove oldest entries (simple FIFO eviction)
            var keysToRemove = _cache.Keys.Take(MAX_CACHE_SIZE / 4).ToList();
            foreach (var key in keysToRemove)
            {
                if (_cache.TryGetValue(key, out var oldTexture))
                {
                    oldTexture?.Dispose();
                    _cache.Remove(key);
                }
            }
        }
        
        _cache[textureId] = texture;
    }
    
    /// <summary>
    /// Clear all cached textures to free memory.
    /// </summary>
    public void ClearCache()
    {
        foreach (var texture in _cache.Values)
        {
            texture?.Dispose();
        }
        _cache.Clear();
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // DEBUG
    // ═══════════════════════════════════════════════════════════════════
    
    private void LogSampleTextures()
    {
        // Log info about first few valid textures
        Console.WriteLine("[Texmaps] Sampling first valid texture entries:");
        int found = 0;
        for (int i = 0; i < Math.Min(200, EntryCount) && found < 10; i++)
        {
            var (offset, length, extra) = ReadIndexEntry(i);
            if (offset != INVALID_OFFSET && offset != 0xFFFFFFFE && length > 0)
            {
                int size = DetermineTextureSize(length, extra);
                Console.WriteLine($"  TextureId[{i}]: offset={offset}, length={length}, extra={extra} → {size}×{size}");
                found++;
            }
        }
        
        if (found == 0)
        {
            Console.WriteLine("  WARNING: No valid texture entries found in first 200 indices!");
        }
    }
    
    /// <summary>
    /// Get debug statistics string
    /// </summary>
    public string GetStats()
    {
        return $"Texmaps: {_loadAttempts} attempts, {_loadSuccesses} successes, {_cacheHits} cache hits, {_cache.Count} cached";
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // CLEANUP
    // ═══════════════════════════════════════════════════════════════════
    
    private void Cleanup()
    {
        _texidxReader?.Dispose();
        _texidxStream?.Dispose();
        _texmapsReader?.Dispose();
        _texmapsStream?.Dispose();
        
        _texidxReader = null;
        _texidxStream = null;
        _texmapsReader = null;
        _texmapsStream = null;
    }
    
    public void Dispose()
    {
        ClearCache();
        Cleanup();
    }
}
