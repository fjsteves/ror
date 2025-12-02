using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RealmOfReality.Client.Assets;

/// <summary>
/// Loads gump (UI element) textures from UO's gumpart.mul/gumpidx.mul or gumpartLegacyMUL.uop
/// 
/// Gumps use a row-based RLE format:
/// - First N*4 bytes are row offsets (in DWORDs from data start)
/// - Each row is a series of (color16, length16) pairs
/// </summary>
public class GumpLoader : IDisposable
{
    private readonly GraphicsDevice _graphics;
    private readonly Dictionary<int, UOArtTexture> _cache = new();
    private readonly string _mulPath;
    private readonly string _idxPath;
    
    // File sources - MUL format
    private FileStream? _mulFile;
    private IndexEntry[]? _mulIndex;
    
    // File sources - UOP format  
    private UopFileReader? _uopReader;
    
    private bool _useMul = false;
    private bool _useUop = false;
    
    public int EntryCount => _mulIndex?.Length ?? _uopReader?.EntryCount ?? 0;
    public bool IsLoaded => _useMul || _useUop;
    public bool IsUsingUop => _useUop;
    
    public GumpLoader(GraphicsDevice graphics, string mulPath, string idxPath)
    {
        _graphics = graphics;
        _mulPath = mulPath;
        _idxPath = idxPath;
    }
    
    /// <summary>
    /// Load gumps from MUL+IDX or UOP
    /// </summary>
    public bool Load()
    {
        // Try UOP format first (modern clients)
        if (TryLoadUop())
        {
            return true;
        }
        
        // Try MUL+IDX (classic clients)
        if (TryLoadMul())
        {
            return true;
        }
        
        DebugLog.Write($"GumpLoader: Failed to load from either MUL or UOP");
        return false;
    }
    
    private bool TryLoadMul()
    {
        DebugLog.Write($"GumpLoader.TryLoadMul:");
        DebugLog.Write($"  MUL path: {_mulPath} (exists: {File.Exists(_mulPath)})");
        DebugLog.Write($"  IDX path: {_idxPath} (exists: {File.Exists(_idxPath)})");
        
        if (!File.Exists(_mulPath) || !File.Exists(_idxPath))
            return false;
            
        if (_idxPath.EndsWith(".uop", StringComparison.OrdinalIgnoreCase))
            return false;
            
        try
        {
            DebugLog.Write($"GumpLoader: Loading MUL format from {_mulPath}");
            
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
            
            _mulFile = new FileStream(_mulPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _useMul = true;
            
            DebugLog.Write($"GumpLoader: Loaded {entryCount} entries from MUL");
            return true;
        }
        catch (Exception ex)
        {
            DebugLog.Write($"GumpLoader: MUL load error: {ex.Message}");
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
            uopPath = Path.Combine(dir, "gumpartLegacyMUL.uop");
        }
        
        if (!File.Exists(uopPath))
            return false;
            
        try
        {
            DebugLog.Write($"GumpLoader: Loading UOP format from {uopPath}");
            
            // UOP gumps use pattern: build/gumpartlegacymul/{0:D8}.tga (same as art)
            _uopReader = new UopFileReader(uopPath, "build/gumpartlegacymul/{0:D8}.tga");
            if (_uopReader.Load())
            {
                _useUop = true;
                DebugLog.Write($"GumpLoader: Loaded {_uopReader.EntryCount} entries from UOP");
                return true;
            }
            _uopReader = null;
            return false;
        }
        catch (Exception ex)
        {
            DebugLog.Write($"GumpLoader: UOP load error: {ex.Message}");
            _uopReader?.Dispose();
            _uopReader = null;
            return false;
        }
    }
    
    /// <summary>
    /// Get a gump texture by ID
    /// </summary>
    public UOArtTexture? GetGump(int gumpId)
    {
        if (gumpId < 0)
            return null;
        
        if (_cache.TryGetValue(gumpId, out var cached))
            return cached;
        
        var texture = LoadGump(gumpId);
        if (texture != null)
            _cache[gumpId] = texture;
        
        return texture;
    }
    
    /// <summary>
    /// Load a gump texture - handles both MUL and UOP formats
    /// </summary>
    private UOArtTexture? LoadGump(int gumpId)
    {
        byte[]? data = null;
        int width = 0, height = 0;
        
        if (_useMul && _mulFile != null && _mulIndex != null)
        {
            if (gumpId < 0 || gumpId >= _mulIndex.Length)
                return null;
                
            var entry = _mulIndex[gumpId];
            if (entry.Lookup < 0 || entry.Length <= 0)
                return null;
            
            // Get dimensions from Extra field: (width << 16) | height
            width = (entry.Extra >> 16) & 0xFFFF;
            height = entry.Extra & 0xFFFF;
            
            if (width <= 0 || height <= 0 || width > 4096 || height > 4096)
                return null;
            
            try
            {
                lock (_mulFile)
                {
                    _mulFile.Seek(entry.Lookup, SeekOrigin.Begin);
                    data = new byte[entry.Length];
                    _mulFile.Read(data, 0, entry.Length);
                }
            }
            catch
            {
                return null;
            }
        }
        else if (_useUop && _uopReader != null)
        {
            data = _uopReader.GetData(gumpId);
            if (data == null || data.Length < 8)
            {
                if (gumpId < 50)
                    DebugLog.Write($"GumpLoader: No data for gump {gumpId}");
                return null;
            }
            
            // UOP gump data: first 8 bytes are width (4 bytes) and height (4 bytes)
            // Some implementations use 2 bytes each, let's try both
            width = BitConverter.ToInt32(data, 0);
            height = BitConverter.ToInt32(data, 4);
            
            // Check if dimensions make sense
            if (width <= 0 || height <= 0 || width > 4096 || height > 4096)
            {
                // Try 2-byte version
                width = BitConverter.ToUInt16(data, 0);
                height = BitConverter.ToUInt16(data, 2);
                
                if (width <= 0 || height <= 0 || width > 4096 || height > 4096)
                {
                    if (gumpId < 50)
                        DebugLog.Write($"GumpLoader: Invalid dimensions for gump {gumpId}: raw bytes = {data[0]:X2} {data[1]:X2} {data[2]:X2} {data[3]:X2}");
                    return null;
                }
                
                // 4-byte header (2 shorts)
                var newData = new byte[data.Length - 4];
                Array.Copy(data, 4, newData, 0, newData.Length);
                data = newData;
            }
            else
            {
                // 8-byte header (2 ints)
                var newData = new byte[data.Length - 8];
                Array.Copy(data, 8, newData, 0, newData.Length);
                data = newData;
            }
            
            if (gumpId < 50)
                DebugLog.Write($"GumpLoader: Gump {gumpId} dimensions: {width}x{height}, data length: {data.Length}");
        }
        
        if (data == null || data.Length < 4)
            return null;
        
        return DecodeGumpRLE(gumpId, data, width, height);
    }
    
    /// <summary>
    /// Decode gump RLE data
    /// Format: Row lookup table followed by RLE-encoded rows
    /// Each row: (color16, runLength16) pairs until row is filled
    /// </summary>
    private UOArtTexture? DecodeGumpRLE(int gumpId, byte[] data, int width, int height)
    {
        try
        {
            var pixels = new uint[width * height];
            
            // Row lookup table: height entries, each 4 bytes (offset in DWORDs)
            int lookupTableSize = height * 4;
            if (data.Length < lookupTableSize)
            {
                DebugLog.Write($"GumpLoader: Gump {gumpId} data too small for lookup table");
                return null;
            }
            
            for (int y = 0; y < height; y++)
            {
                // Row offset is in DWORDs, multiply by 4 for byte offset
                int rowOffset = BitConverter.ToInt32(data, y * 4) * 4;
                
                if (rowOffset < 0 || rowOffset >= data.Length)
                    continue;
                
                int x = 0;
                int pos = rowOffset;
                
                while (x < width && pos + 3 < data.Length)
                {
                    // Read color (16-bit ARGB1555) and run length (16-bit)
                    ushort color16 = BitConverter.ToUInt16(data, pos);
                    ushort runLength = BitConverter.ToUInt16(data, pos + 2);
                    pos += 4;
                    
                    if (runLength == 0)
                        break;
                    
                    // Convert ARGB1555 to RGBA8888
                    uint rgba = ConvertArgb1555ToRgba(color16);
                    
                    // Fill pixels
                    for (int i = 0; i < runLength && x < width; i++)
                    {
                        pixels[y * width + x] = rgba;
                        x++;
                    }
                }
            }
            
            // Count visible pixels for debug
            int visible = 0;
            foreach (var p in pixels)
                if ((p >> 24) > 0) visible++;
            
            if (gumpId < 20 || (gumpId >= 2100 && gumpId <= 2110))
                DebugLog.Write($"GumpLoader: Gump {gumpId} decoded: {width}x{height}, {visible} visible pixels");
            
            var texture = new Texture2D(_graphics, width, height);
            texture.SetData(pixels);
            
            return new UOArtTexture
            {
                Texture = texture,
                Width = width,
                Height = height
            };
        }
        catch (Exception ex)
        {
            DebugLog.Write($"GumpLoader: Error decoding gump {gumpId}: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Convert ARGB1555 to RGBA8888 (MonoGame format)
    /// 
    /// UO gumps use a simple transparency model:
    /// - Color value 0x0000 = fully transparent
    /// - Any other color = fully opaque (alpha bit is ignored in most cases)
    /// 
    /// Format: A(1) R(5) G(5) B(5)
    /// </summary>
    private static uint ConvertArgb1555ToRgba(ushort color)
    {
        // In UO, color 0 is the transparent color key
        if (color == 0)
            return 0; // Fully transparent
        
        // Extract RGB components (ignore alpha bit - treat all non-zero as opaque)
        int r = (color >> 10) & 0x1F;
        int g = (color >> 5) & 0x1F;
        int b = color & 0x1F;
        
        // Expand 5-bit to 8-bit (multiply by 8 and add upper bits for full range)
        byte r8 = (byte)((r << 3) | (r >> 2));
        byte g8 = (byte)((g << 3) | (g >> 2));
        byte b8 = (byte)((b << 3) | (b >> 2));
        
        // All non-transparent pixels are fully opaque
        byte a8 = 255;
        
        // MonoGame expects RGBA (R in lowest byte)
        return (uint)(r8 | (g8 << 8) | (b8 << 16) | (a8 << 24));
    }
    
    /// <summary>
    /// Preload commonly used gumps into cache
    /// </summary>
    public void PreloadCommonGumps()
    {
        // Common UI elements
        int[] commonGumps = {
            0, 1, 2, 5, 10,           // Background elements
            2100, 2101, 2102, 2103,   // Paperdoll slots
            2104, 2105, 2106, 2107,
            5000, 5001, 5002,         // Buttons
            10460, 10461, 10462,      // Scroll elements
        };
        
        foreach (var id in commonGumps)
        {
            GetGump(id); // Load into cache
        }
        
        DebugLog.Write($"GumpLoader: Preloaded {_cache.Count} common gumps");
    }
    
    public void ClearCache()
    {
        foreach (var tex in _cache.Values)
            tex.Texture?.Dispose();
        _cache.Clear();
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
