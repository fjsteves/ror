using System.Runtime.InteropServices;

namespace RealmOfReality.Client.Assets;

/// <summary>
/// Index entry in .idx files (12 bytes each)
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct IndexEntry
{
    public int Lookup;  // Offset in .mul file (-1 = invalid)
    public int Length;  // Data length in bytes
    public int Extra;   // Extra data (width/height for gumps, unused for art)
    
    public bool IsValid => Lookup >= 0 && Length > 0;
}

/// <summary>
/// Base class for reading UO MUL/IDX file pairs
/// Supports both classic MUL format and LegacyMUL format (used by ClassicUO)
/// </summary>
public abstract class MulFileReader : IDisposable
{
    protected FileStream? _mulFile;
    protected IndexEntry[]? _index;
    protected readonly string _mulPath;
    protected readonly string _idxPath;
    protected readonly object _lock = new();
    protected bool _isLegacyMode = false;
    
    public int EntryCount => _index?.Length ?? 0;
    public bool IsLoaded => _mulFile != null && _index != null;
    public bool IsLegacyMode => _isLegacyMode;
    
    protected MulFileReader(string mulPath, string idxPath)
    {
        _mulPath = mulPath;
        _idxPath = idxPath;
    }
    
    /// <summary>
    /// Set legacy mode (LegacyMUL files have embedded index)
    /// </summary>
    public void SetLegacyMode(bool legacy)
    {
        _isLegacyMode = legacy;
    }
    
    /// <summary>
    /// Load the index and open the MUL file
    /// </summary>
    public virtual bool Load()
    {
        try
        {
            if (!File.Exists(_mulPath))
                return false;
            
            if (_isLegacyMode)
            {
                // LegacyMUL format - index is embedded in the MUL file
                return LoadLegacyMul();
            }
            
            if (!File.Exists(_idxPath))
                return false;
            
            // Load index file
            var idxData = File.ReadAllBytes(_idxPath);
            var entryCount = idxData.Length / 12;
            _index = new IndexEntry[entryCount];
            
            for (int i = 0; i < entryCount; i++)
            {
                _index[i] = new IndexEntry
                {
                    Lookup = BitConverter.ToInt32(idxData, i * 12),
                    Length = BitConverter.ToInt32(idxData, i * 12 + 4),
                    Extra = BitConverter.ToInt32(idxData, i * 12 + 8)
                };
            }
            
            // Open MUL file for reading
            _mulFile = new FileStream(_mulPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading {_mulPath}: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Load a LegacyMUL file which has the index embedded at the start
    /// Format: [4 bytes: entry count] [N * 12 bytes: index entries] [data...]
    /// </summary>
    protected virtual bool LoadLegacyMul()
    {
        try
        {
            _mulFile = new FileStream(_mulPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            
            // Read entry count (4 bytes)
            var header = new byte[4];
            _mulFile.Read(header, 0, 4);
            var entryCount = BitConverter.ToInt32(header, 0);
            
            // Sanity check
            if (entryCount <= 0 || entryCount > 500000)
            {
                Console.WriteLine($"LegacyMUL: Invalid entry count {entryCount}, trying sequential scan...");
                return LoadLegacyMulSequential();
            }
            
            Console.WriteLine($"LegacyMUL: Loading {entryCount} entries from {Path.GetFileName(_mulPath)}");
            
            // Read index entries
            var indexSize = entryCount * 12;
            var indexData = new byte[indexSize];
            _mulFile.Read(indexData, 0, indexSize);
            
            _index = new IndexEntry[entryCount];
            var headerSize = 4 + indexSize; // Account for header when reading
            
            for (int i = 0; i < entryCount; i++)
            {
                var lookup = BitConverter.ToInt32(indexData, i * 12);
                _index[i] = new IndexEntry
                {
                    // Lookup values are relative to start of file in LegacyMUL
                    Lookup = lookup,
                    Length = BitConverter.ToInt32(indexData, i * 12 + 4),
                    Extra = BitConverter.ToInt32(indexData, i * 12 + 8)
                };
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading LegacyMUL {_mulPath}: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Fallback: scan file sequentially if header-based loading fails
    /// </summary>
    protected virtual bool LoadLegacyMulSequential()
    {
        // Create a minimal index - subclasses can override with file-specific logic
        _index = new IndexEntry[0];
        return true;
    }
    
    /// <summary>
    /// Read raw data for an entry
    /// </summary>
    protected byte[]? ReadEntry(int index)
    {
        if (_mulFile == null || _index == null || index < 0 || index >= _index.Length)
            return null;
        
        var entry = _index[index];
        if (!entry.IsValid)
            return null;
        
        lock (_lock)
        {
            try
            {
                _mulFile.Seek(entry.Lookup, SeekOrigin.Begin);
                var data = new byte[entry.Length];
                _mulFile.Read(data, 0, entry.Length);
                return data;
            }
            catch
            {
                return null;
            }
        }
    }
    
    /// <summary>
    /// Get the index entry for an ID
    /// </summary>
    protected IndexEntry? GetIndexEntry(int index)
    {
        if (_index == null || index < 0 || index >= _index.Length)
            return null;
        return _index[index];
    }
    
    public void Dispose()
    {
        _mulFile?.Dispose();
        _mulFile = null;
        _index = null;
    }
}

/// <summary>
/// Utility methods for reading UO data types
/// </summary>
public static class UODataReader
{
    /// <summary>
    /// Convert ARGB1555 (16-bit) to ARGB8888 (32-bit)
    /// UO uses 1 bit alpha, 5 bits each for RGB
    /// </summary>
    public static uint Argb1555ToArgb8888(ushort color)
    {
        if (color == 0)
            return 0; // Transparent
        
        // Extract components (ARRRRRGGGGGBBBBB)
        uint a = (uint)((color >> 15) & 0x01);
        uint r = (uint)((color >> 10) & 0x1F);
        uint g = (uint)((color >> 5) & 0x1F);
        uint b = (uint)(color & 0x1F);
        
        // Scale to 8-bit (multiply by 255/31 ≈ 8.226)
        r = (r * 255) / 31;
        g = (g * 255) / 31;
        b = (b * 255) / 31;
        a = a == 0 ? 0u : 255u;
        
        return (a << 24) | (r << 16) | (g << 8) | b;
    }
    
    /// <summary>
    /// Convert ARGB1555 to ABGR for XNA/MonoGame Texture2D.SetData
    /// MonoGame expects: byte0=R, byte1=G, byte2=B, byte3=A (little-endian ABGR)
    /// Uses bit replication for accurate color mapping (ClassicUO method).
    /// </summary>
    public static uint Argb1555ToRgba(ushort color)
    {
        if (color == 0)
            return 0; // Transparent
        
        // ARGB1555: A(1 bit) R(5 bits) G(5 bits) B(5 bits)
        uint r5 = (uint)((color >> 10) & 0x1F);
        uint g5 = (uint)((color >> 5) & 0x1F);
        uint b5 = (uint)(color & 0x1F);
        
        // Bit replication: expand 5-bit (0-31) to 8-bit (0-255)
        // Formula: (value << 3) | (value >> 2)
        // This maps: 0->0, 15->123, 31->255 (exact)
        uint r = (r5 << 3) | (r5 >> 2);
        uint g = (g5 << 3) | (g5 >> 2);
        uint b = (b5 << 3) | (b5 >> 2);
        
        // MonoGame format: 0xAABBGGRR (ABGR in memory, little-endian)
        // Alpha = 0xFF for non-transparent pixels
        return 0xFF000000 | (b << 16) | (g << 8) | r;
    }
    
    /// <summary>
    /// Read a little-endian ushort from a byte array
    /// </summary>
    public static ushort ReadUInt16(byte[] data, int offset)
    {
        return (ushort)(data[offset] | (data[offset + 1] << 8));
    }
    
    /// <summary>
    /// Read a little-endian int from a byte array
    /// </summary>
    public static int ReadInt32(byte[] data, int offset)
    {
        return data[offset] | (data[offset + 1] << 8) | 
               (data[offset + 2] << 16) | (data[offset + 3] << 24);
    }
}
