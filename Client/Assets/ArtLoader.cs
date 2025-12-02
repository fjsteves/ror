using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RealmOfReality.Client.Assets;

/// <summary>
/// Represents a loaded UO art tile with texture and metadata
/// </summary>
public class UOArtTexture
{
    public Texture2D? Texture { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int OffsetX { get; set; }  // For proper positioning
    public int OffsetY { get; set; }
    
    /// <summary>Whether this is a land tile (44x44 diamond shape)</summary>
    public bool IsLandTile { get; set; } = false;
    
    /// <summary>
    /// Check if texture is valid for rendering.
    /// Land tiles are always valid if they have a texture (diamond shape has transparent corners by design).
    /// Static items check for placeholder patterns.
    /// </summary>
    public bool IsValid => Texture != null && (IsLandTile || !IsPlaceholder);
    
    // Flag set during loading if texture appears to be a placeholder
    public bool IsPlaceholder { get; set; } = false;
    
    /// <summary>
    /// Check if the texture looks like a placeholder (mostly black/magenta).
    /// NOTE: For land tiles, transparent pixels are expected (diamond shape) so we only check non-transparent pixels.
    /// </summary>
    public bool CheckIfPlaceholder()
    {
        if (Texture == null) return true;
        
        try
        {
            var pixels = new Color[Texture.Width * Texture.Height];
            Texture.GetData(pixels);
            
            int transparent = 0;
            int black = 0;
            int magenta = 0;
            int coloredPixels = 0;
            
            foreach (var pixel in pixels)
            {
                if (pixel.A == 0)
                {
                    transparent++;
                }
                else
                {
                    coloredPixels++;
                    if (pixel.R < 20 && pixel.G < 20 && pixel.B < 20) black++;
                    else if (pixel.R > 200 && pixel.G < 50 && pixel.B > 200) magenta++;
                }
            }
            
            // For land tiles: if there are NO colored pixels at all, it's a placeholder
            if (IsLandTile)
            {
                // Land tiles should have at least some colored pixels in the diamond
                // If less than 5% of non-transparent pixels are actual colors, it's placeholder
                return coloredPixels < 50; // 44x44 diamond has ~968 pixels, need at least 50
            }
            
            // For static items: if more than 95% of non-transparent pixels are black/magenta, it's placeholder
            if (coloredPixels == 0) return true;
            float badRatio = (black + magenta) / (float)coloredPixels;
            return badRatio > 0.95f;
        }
        catch
        {
            return false; // Assume valid if we can't check
        }
    }
}

/// <summary>
/// Loads art assets from UO's art.mul/artidx.mul files OR artLegacyMUL.uop
/// 
/// Art.mul contains two types of graphics:
/// - Land tiles (IDs 0-16383): 44x44 fixed-size isometric tiles, raw ARGB1555
/// - Static items (IDs 16384+): Variable size with run-length encoding
/// </summary>
public class ArtLoader : IDisposable
{
    private readonly GraphicsDevice _graphics;
    private readonly Dictionary<int, UOArtTexture> _landCache = new();
    private readonly Dictionary<int, UOArtTexture> _staticCache = new();
    
    // File sources - MUL format
    private FileStream? _mulFile;
    private IndexEntry[]? _mulIndex;
    
    // File sources - UOP format
    private UopFileReader? _uopReader;
    
    // Land tiles are 44x44 isometric diamonds
    public const int LandTileWidth = 44;
    public const int LandTileHeight = 44;
    
    // First static item ID (land tiles are 0-16383)
    public const int StaticOffset = 0x4000; // 16384
    
    private readonly string _mulPath;
    private readonly string _idxPath;
    private bool _useMul = false;
    private bool _useUop = false;
    
    public int EntryCount => _mulIndex?.Length ?? _uopReader?.EntryCount ?? 0;
    public bool IsLoaded => _useMul || _useUop;
    public bool IsUsingUop => _useUop;
    
    public ArtLoader(GraphicsDevice graphics, string mulPath, string idxPath)
    {
        _graphics = graphics;
        _mulPath = mulPath;
        _idxPath = idxPath;
    }
    
    /// <summary>
    /// Load art from MUL+IDX or UOP
    /// </summary>
    public bool Load()
    {
        // Try MUL+IDX first
        if (TryLoadMul())
        {
            return true;
        }
        
        // Try UOP format - look for artLegacyMUL.uop
        if (TryLoadUop())
        {
            return true;
        }
        
        Console.WriteLine($"ArtLoader: Failed to load from either MUL or UOP");
        return false;
    }
    
    private bool TryLoadMul()
    {
        // Check for classic MUL+IDX
        if (!File.Exists(_mulPath) || !File.Exists(_idxPath))
            return false;
            
        // Make sure IDX isn't actually a UOP
        if (_idxPath.EndsWith(".uop", StringComparison.OrdinalIgnoreCase))
            return false;
            
        try
        {
            Console.WriteLine($"ArtLoader: Loading MUL format from {_mulPath}");
            
            // Load index file
            var idxData = File.ReadAllBytes(_idxPath);
            var entryCount = idxData.Length / 12;
            _mulIndex = new IndexEntry[entryCount];
            
            for (int i = 0; i < entryCount; i++)
            {
                _mulIndex[i] = new IndexEntry
                {
                    Lookup = BitConverter.ToInt32(idxData, i * 12),
                    Length = BitConverter.ToInt32(idxData, i * 12 + 4),
                    Extra = BitConverter.ToInt32(idxData, i * 12 + 8)
                };
            }
            
            // Open MUL file for reading
            _mulFile = new FileStream(_mulPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _useMul = true;
            
            Console.WriteLine($"ArtLoader: Loaded {entryCount} entries from MUL");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ArtLoader: MUL load error: {ex.Message}");
            _mulIndex = null;
            _mulFile?.Dispose();
            _mulFile = null;
            return false;
        }
    }
    
    private bool TryLoadUop()
    {
        var uopPath = _mulPath;
        if (!uopPath.EndsWith(".uop", StringComparison.OrdinalIgnoreCase))
        {
            var dir = Path.GetDirectoryName(_mulPath) ?? ".";
            uopPath = Path.Combine(dir, "artLegacyMUL.uop");
        }
        
        if (!File.Exists(uopPath))
            return false;
            
        try
        {
            Console.WriteLine($"ArtLoader: Loading UOP format from {uopPath}");
            _uopReader = new UopFileReader(uopPath, "build/artlegacymul/{0:D8}.tga");
            if (_uopReader.Load())
            {
                _useUop = true;
                Console.WriteLine($"ArtLoader: Loaded {_uopReader.EntryCount} entries from UOP");
                return true;
            }
            _uopReader = null;
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ArtLoader: UOP load error: {ex.Message}");
            _uopReader?.Dispose();
            _uopReader = null;
            return false;
        }
    }
    
    /// <summary>
    /// Get a land tile texture (IDs 0-16383)
    /// </summary>
    public UOArtTexture? GetLandTile(int tileId)
    {
        if (tileId < 0 || tileId >= StaticOffset)
            return null;
        
        // Check cache
        if (_landCache.TryGetValue(tileId, out var cached))
            return cached;
        
        // Load from file
        var texture = LoadLandTile(tileId);
        if (texture != null)
            _landCache[tileId] = texture;
        
        return texture;
    }
    
    /// <summary>
    /// Get a static item texture (IDs 0+, internally offset by 16384)
    /// </summary>
    public UOArtTexture? GetStaticItem(int itemId)
    {
        // Check cache
        if (_staticCache.TryGetValue(itemId, out var cached))
            return cached;
        
        // Load from file (add offset for internal indexing)
        var texture = LoadStaticItem(itemId + StaticOffset);
        if (texture != null)
            _staticCache[itemId] = texture;
        
        return texture;
    }
    
    /// <summary>
    /// Read raw data for an entry from either MUL or UOP
    /// </summary>
    private byte[]? ReadEntry(int index)
    {
        if (_useMul && _mulFile != null && _mulIndex != null)
        {
            if (index < 0 || index >= _mulIndex.Length)
                return null;
                
            var entry = _mulIndex[index];
            if (entry.Lookup < 0 || entry.Length <= 0)
                return null;
                
            try
            {
                lock (_mulFile)
                {
                    _mulFile.Seek(entry.Lookup, SeekOrigin.Begin);
                    var data = new byte[entry.Length];
                    _mulFile.Read(data, 0, entry.Length);
                    return data;
                }
            }
            catch
            {
                return null;
            }
        }
        
        if (_useUop && _uopReader != null)
        {
            var data = _uopReader.GetData(index);
            if (data != null && index < 10)
            {
                Console.WriteLine($"ArtLoader: UOP entry {index} returned {data.Length} bytes");
            }
            return data;
        }
        
        return null;
    }
    
    /// <summary>
    /// Load a land tile (44x44 isometric diamond)
    /// Land tiles are stored as raw ARGB1555 pixels in diamond pattern
    /// UOP format: compressed diamond data (only non-transparent pixels stored)
    /// </summary>
    private UOArtTexture? LoadLandTile(int tileId)
    {
        if (tileId < 20)
            DebugLog.Write($"ArtLoader.LoadLandTile: Requesting tile {tileId}, useUop={_useUop}, useMul={_useMul}");
        
        var data = ReadEntry(tileId);
        if (data == null)
        {
            if (tileId < 20)
                DebugLog.Write($"ArtLoader.LoadLandTile: No data returned for tile {tileId}");
            return null;
        }
        
        if (tileId < 20)
            DebugLog.Write($"ArtLoader.LoadLandTile: Got {data.Length} bytes for tile {tileId}");
        
        // Land tile diamond: 44 rows, widths are 2,4,6...44...6,4,2
        // Total pixels = 2*(2+4+6+...+42) + 44 = 2*462 + 44 = 968 pixels
        // At 2 bytes per pixel = 1936 bytes for pixel data
        // Some UOP files add a 4-byte header = 1940 or have padding
        
        int minSize = 968 * 2; // 1936 bytes minimum for diamond pixels
        int dataOffset = 0;
        
        // UOP may have 4-byte header with flags or original index
        if (_useUop && data.Length >= minSize + 4)
        {
            // Check if there's a header by looking at size match
            // 2024 bytes = 4 header + 1936 pixel data + 84 padding/extra
            // Or first 4 bytes could be flags
            dataOffset = 4;
            if (tileId < 5)
                DebugLog.Write($"  Assuming 4-byte UOP header, reading from offset {dataOffset}");
        }
        
        if (data.Length - dataOffset < minSize)
        {
            DebugLog.Write($"ArtLoader: Land tile {tileId} data too small: {data.Length} bytes (need {minSize}+)");
            return null;
        }
        
        try
        {
            // Land tiles are 44x44 with a diamond shape
            // The diamond is drawn row by row with varying widths
            var pixels = new uint[LandTileWidth * LandTileHeight];
            int readPos = dataOffset;
            
            // Top half: rows get wider (2, 4, 6, ..., 44)
            for (int y = 0; y < 22; y++)
            {
                int rowWidth = (y + 1) * 2;
                int startX = (LandTileWidth - rowWidth) / 2;
                
                for (int x = 0; x < rowWidth && readPos + 1 < data.Length; x++)
                {
                    ushort color = UODataReader.ReadUInt16(data, readPos);
                    readPos += 2;
                    
                    int pixelIndex = y * LandTileWidth + startX + x;
                    if (pixelIndex < pixels.Length)
                        pixels[pixelIndex] = UODataReader.Argb1555ToRgba(color);
                }
            }
            
            // Bottom half: rows get narrower (42, 40, ..., 2)
            // Note: (44-22)*2=44 was wrong, should start at 42
            for (int y = 22; y < LandTileHeight; y++)
            {
                int rowWidth = (LandTileHeight - y - 1) * 2;  // 42, 40, ..., 0
                if (rowWidth <= 0) break; // Last row is transparent
                int startX = (LandTileWidth - rowWidth) / 2;
                
                for (int x = 0; x < rowWidth && readPos + 1 < data.Length; x++)
                {
                    ushort color = UODataReader.ReadUInt16(data, readPos);
                    readPos += 2;
                    
                    int pixelIndex = y * LandTileWidth + startX + x;
                    if (pixelIndex < pixels.Length)
                        pixels[pixelIndex] = UODataReader.Argb1555ToRgba(color);
                }
            }
            
            if (tileId < 5)
                DebugLog.Write($"  Read {readPos - dataOffset} bytes of pixel data");
            
            // Create texture
            var texture = new Texture2D(_graphics, LandTileWidth, LandTileHeight);
            texture.SetData(pixels);
            
            var artTexture = new UOArtTexture
            {
                Texture = texture,
                Width = LandTileWidth,
                Height = LandTileHeight,
                IsLandTile = true  // Land tiles have transparent corners by design
            };
            
            // Check if this is a placeholder texture (mostly empty)
            artTexture.IsPlaceholder = artTexture.CheckIfPlaceholder();
            if (artTexture.IsPlaceholder && tileId < 20)
            {
                DebugLog.Write($"  Tile {tileId} detected as placeholder");
            }
            
            return artTexture;
        }
        catch (Exception ex)
        {
            DebugLog.Write($"Error loading land tile {tileId}: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Load a static item texture (variable size, run-length encoded)
    /// Static items use a per-row RLE format
    /// </summary>
    private UOArtTexture? LoadStaticItem(int artId)
    {
        var data = ReadEntry(artId);
        if (data == null || data.Length < 8)
            return null;
        
        try
        {
            // Static item header
            // 4 bytes: flags (unused by us)
            // 2 bytes: width
            // 2 bytes: height
            int flags = UODataReader.ReadInt32(data, 0);
            int width = UODataReader.ReadUInt16(data, 4);
            int height = UODataReader.ReadUInt16(data, 6);
            
            if (width <= 0 || height <= 0 || width > 1024 || height > 1024)
                return null;
            
            // Lookup table: 2 bytes per row, offset from start of pixel data
            int headerSize = 8;
            int lookupSize = height * 2;
            
            if (data.Length < headerSize + lookupSize)
                return null;
            
            var pixels = new uint[width * height];
            
            // Read each row using the lookup table
            for (int y = 0; y < height; y++)
            {
                // Get offset for this row (relative to end of lookup table)
                int rowOffset = UODataReader.ReadUInt16(data, headerSize + y * 2);
                int dataPos = headerSize + lookupSize + rowOffset * 2;
                
                int x = 0;
                while (x < width && dataPos + 3 < data.Length)
                {
                    // Read run header: offset (2 bytes) + length (2 bytes)
                    int runOffset = UODataReader.ReadUInt16(data, dataPos);
                    int runLength = UODataReader.ReadUInt16(data, dataPos + 2);
                    dataPos += 4;
                    
                    // Check for end of row marker
                    if (runOffset == 0 && runLength == 0)
                        break;
                    
                    // Skip to the run start position
                    x += runOffset;
                    
                    // Read the pixel run
                    for (int i = 0; i < runLength && x < width && dataPos + 1 < data.Length; i++)
                    {
                        ushort color = UODataReader.ReadUInt16(data, dataPos);
                        dataPos += 2;
                        
                        if (x >= 0 && x < width)
                        {
                            int pixelIndex = y * width + x;
                            if (pixelIndex < pixels.Length)
                                pixels[pixelIndex] = UODataReader.Argb1555ToRgba(color);
                        }
                        x++;
                    }
                }
            }
            
            // Create texture
            var texture = new Texture2D(_graphics, width, height);
            texture.SetData(pixels);
            
            return new UOArtTexture
            {
                Texture = texture,
                Width = width,
                Height = height,
                // Static items are centered at their base
                OffsetX = width / 2,
                OffsetY = height
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading static item {artId}: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Preload common tiles for better performance
    /// </summary>
    public void PreloadCommonTiles(int count = 1000)
    {
        // Preload first N land tiles
        for (int i = 0; i < Math.Min(count, StaticOffset); i++)
        {
            GetLandTile(i);
        }
        
        // Preload first N static items
        for (int i = 0; i < count; i++)
        {
            GetStaticItem(i);
        }
    }
    
    /// <summary>
    /// Clear texture cache to free memory
    /// </summary>
    public void ClearCache()
    {
        foreach (var tex in _landCache.Values)
            tex.Texture?.Dispose();
        foreach (var tex in _staticCache.Values)
            tex.Texture?.Dispose();
        
        _landCache.Clear();
        _staticCache.Clear();
    }
    
    public void Dispose()
    {
        ClearCache();
        _mulFile?.Dispose();
        _mulFile = null;
        _mulIndex = null;
        _uopReader?.Dispose();
        _uopReader = null;
    }
}
