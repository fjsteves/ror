// ==========================================================================
// TexmapLoader.cs - texmaps.mul / texidx.mul parser
// ==========================================================================
// Loads stretched terrain textures (64x64 or 128x128).
// These textures are used when terrain has different heights at corners.
// ==========================================================================

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RealmOfReality.Client.Rendering.Loaders;

/// <summary>
/// Loads stretched terrain textures from texmaps.mul.
/// </summary>
public sealed class TexmapLoader : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly string _dataPath;
    
    private FileStream? _texmapsStream;
    private BinaryReader? _texmapsReader;
    private FileStream? _texidxStream;
    private BinaryReader? _texidxReader;
    
    private readonly Dictionary<int, Texture2D> _cache = new();
    private const int MAX_CACHE_SIZE = 512;
    
    public bool IsLoaded { get; private set; }
    
    public TexmapLoader(GraphicsDevice graphicsDevice, string dataPath)
    {
        _graphicsDevice = graphicsDevice;
        _dataPath = dataPath;
    }
    
    /// <summary>
    /// Load texmaps.mul and texidx.mul.
    /// </summary>
    public bool Load()
    {
        string texmapsPath = Path.Combine(_dataPath, "texmaps.mul");
        string texidxPath = Path.Combine(_dataPath, "texidx.mul");
        
        if (!File.Exists(texmapsPath) || !File.Exists(texidxPath))
        {
            Console.WriteLine("[TexmapLoader] Files not found");
            return false;
        }
        
        try
        {
            _texmapsStream = File.OpenRead(texmapsPath);
            _texmapsReader = new BinaryReader(_texmapsStream);
            _texidxStream = File.OpenRead(texidxPath);
            _texidxReader = new BinaryReader(_texidxStream);
            
            int entryCount = (int)(_texidxStream.Length / UOConstants.INDEX_ENTRY_BYTES);
            
            IsLoaded = true;
            Console.WriteLine($"[TexmapLoader] Loaded: {entryCount} entries, data={_texmapsStream.Length:N0} bytes");
            
            // Test load a few texmaps
            int loaded = 0;
            for (int i = 1; i < Math.Min(100, entryCount) && loaded < 3; i++)
            {
                var tex = GetTexmap(i);
                if (tex != null)
                {
                    Console.WriteLine($"[TexmapLoader] Texmap[{i}]: {tex.Width}x{tex.Height}");
                    loaded++;
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TexmapLoader] Load error: {ex.Message}");
            Dispose();
            return false;
        }
    }
    
    /// <summary>
    /// Get a texmap texture by TextureId (from TileData).
    /// </summary>
    /// <param name="textureId">TextureId from LandTileData (NOT land tile ID!)</param>
    /// <returns>Texture or null if not found</returns>
    public Texture2D? GetTexmap(int textureId)
    {
        if (!IsLoaded || _texidxReader == null || _texmapsReader == null)
            return null;
        
        if (textureId <= 0 || textureId >= UOConstants.MAX_TEXMAP_INDEX)
            return null;
        
        // Check cache
        if (_cache.TryGetValue(textureId, out var cached))
            return cached;
        
        try
        {
            // Read index entry
            long idxOffset = (long)textureId * UOConstants.INDEX_ENTRY_BYTES;
            if (idxOffset + 12 > _texidxStream!.Length)
                return null;
            
            _texidxStream.Seek(idxOffset, SeekOrigin.Begin);
            int lookup = _texidxReader.ReadInt32();
            int length = _texidxReader.ReadInt32();
            int extra = _texidxReader.ReadInt32();
            
            // Check for invalid entry
            if (lookup == -1 || lookup == unchecked((int)0xFFFFFFFF) || length <= 0)
                return null;
            
            if (lookup >= _texmapsStream!.Length)
                return null;
            
            // Determine texture size
            // extra == 0 means 64x64, otherwise 128x128
            // Also can infer from data length
            int size;
            if (extra == 0 || length <= UOConstants.TEXMAP_BYTES_SMALL)
                size = UOConstants.TEXMAP_SIZE_SMALL;
            else
                size = UOConstants.TEXMAP_SIZE_LARGE;
            
            int expectedBytes = size * size * 2;
            if (length < expectedBytes)
            {
                // Try to infer from actual length
                int inferredSize = (int)Math.Sqrt(length / 2);
                if (inferredSize == 64 || inferredSize == 128)
                    size = inferredSize;
                else
                    return null;
            }
            
            // Read pixel data
            _texmapsStream.Seek(lookup, SeekOrigin.Begin);
            var pixels = new Color[size * size];
            
            for (int i = 0; i < pixels.Length; i++)
            {
                ushort color16 = _texmapsReader.ReadUInt16();
                pixels[i] = ColorConversion.Argb1555ToColor(color16);
            }
            
            // Create texture
            var texture = new Texture2D(_graphicsDevice, size, size);
            texture.SetData(pixels);
            
            // Cache it
            CacheTexture(textureId, texture);
            
            return texture;
        }
        catch
        {
            return null;
        }
    }
    
    private void CacheTexture(int id, Texture2D texture)
    {
        if (_cache.Count >= MAX_CACHE_SIZE)
        {
            // Remove oldest entries
            var toRemove = _cache.Take(MAX_CACHE_SIZE / 2).ToList();
            foreach (var kvp in toRemove)
            {
                kvp.Value?.Dispose();
                _cache.Remove(kvp.Key);
            }
        }
        _cache[id] = texture;
    }
    
    /// <summary>
    /// Clear texture cache.
    /// </summary>
    public void ClearCache()
    {
        foreach (var tex in _cache.Values)
            tex?.Dispose();
        _cache.Clear();
    }
    
    public void Dispose()
    {
        ClearCache();
        
        _texmapsReader?.Dispose();
        _texmapsStream?.Dispose();
        _texidxReader?.Dispose();
        _texidxStream?.Dispose();
        
        _texmapsReader = null;
        _texmapsStream = null;
        _texidxReader = null;
        _texidxStream = null;
        
        IsLoaded = false;
    }
}
