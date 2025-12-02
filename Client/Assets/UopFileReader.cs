using System.IO.Compression;

namespace RealmOfReality.Client.Assets;

/// <summary>
/// Reader for UOP (Ultima Online Package) files
/// UOP files contain MUL data packed with compression and hash-based indexing
/// Used for artLegacyMUL.uop, gumpartLegacyMUL.uop, etc.
/// </summary>
public class UopFileReader : IDisposable
{
    private FileStream? _file;
    private readonly string _filePath;
    private readonly Dictionary<ulong, UopFileEntry> _entries = new();
    private readonly string _filePattern;
    
    public bool IsLoaded => _file != null && _entries.Count > 0;
    public int EntryCount => _entries.Count;
    
    /// <summary>
    /// Create a UOP reader
    /// </summary>
    /// <param name="filePath">Path to .uop file</param>
    /// <param name="filePattern">Pattern for hash lookup, e.g. "build/artlegacymul/{0:D8}.tga"</param>
    public UopFileReader(string filePath, string filePattern)
    {
        _filePath = filePath;
        _filePattern = filePattern;
    }
    
    /// <summary>
    /// Load the UOP file and parse its index
    /// </summary>
    public bool Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return false;
            
            _file = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(_file, System.Text.Encoding.UTF8, leaveOpen: true);
            
            // Read header
            var magic = reader.ReadUInt32(); // "MYP\0" = 0x50594D
            if (magic != 0x50594D)
            {
                Console.WriteLine($"UOP: Invalid magic number: 0x{magic:X8}");
                return false;
            }
            
            var version = reader.ReadUInt32();
            var misc = reader.ReadUInt32(); // Usually 0xFD23EC43
            var tableOffset = reader.ReadInt64();
            var tableCapacity = reader.ReadUInt32();
            var fileCount = reader.ReadUInt32();
            
            Console.WriteLine($"UOP: Version={version}, Files={fileCount}, TableOffset={tableOffset}");
            
            // Read file tables
            _file.Seek(tableOffset, SeekOrigin.Begin);
            
            var totalRead = 0;
            while (tableOffset != 0 && totalRead < fileCount + 100)
            {
                var tableCount = reader.ReadUInt32();
                var nextTable = reader.ReadInt64();
                
                for (int i = 0; i < tableCount && totalRead < fileCount + 100; i++)
                {
                    var entry = new UopFileEntry
                    {
                        Offset = reader.ReadInt64(),
                        HeaderLength = reader.ReadInt32(),
                        CompressedLength = reader.ReadInt32(),
                        DecompressedLength = reader.ReadInt32(),
                        Hash = reader.ReadUInt64(),
                        Checksum = reader.ReadUInt32(),
                        Flags = reader.ReadInt16()
                    };
                    
                    if (entry.Offset != 0 && entry.DecompressedLength > 0)
                    {
                        _entries[entry.Hash] = entry;
                        totalRead++;
                    }
                }
                
                if (nextTable == 0)
                    break;
                    
                _file.Seek(nextTable, SeekOrigin.Begin);
            }
            
            DebugLog.Write($"UOP: Loaded {_entries.Count} entries from {Path.GetFileName(_filePath)}");
            
            // Debug: Test which indices exist (pattern-specific)
            int[] testIndices;
            if (_filePattern.Contains("gumpart"))
            {
                // Gump-specific test indices
                testIndices = new[] { 0, 1, 2, 5, 10, 100, 2000, 2100, 5000 };
            }
            else
            {
                // Art file indices (land=0-16383, static=16384+)
                testIndices = new[] { 0, 1, 2, 3, 5, 100, 1000, 16384, 16385, 16514 };
            }
            DebugLog.Write($"UOP: Testing indices with pattern '{_filePattern}':");
            foreach (var i in testIndices)
            {
                var testFile = string.Format(_filePattern, i);
                var testHash = HashFileName(testFile);
                var found = _entries.ContainsKey(testHash);
                DebugLog.Write($"  Index {i,6} -> '{testFile}' -> Hash {testHash:X16} -> Found: {found}");
            }
            
            return _entries.Count > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UOP: Error loading {_filePath}: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Get data for a specific index using the file pattern
    /// </summary>
    public byte[]? GetData(int index)
    {
        var filename = string.Format(_filePattern, index);
        var hash = HashFileName(filename);
        
        if (!_entries.ContainsKey(hash))
        {
            // Only log first few misses to avoid spam
            if (index < 20 || index == 16384 || index == 16514)
                DebugLog.Write($"UOP.GetData: Index {index} NOT FOUND (file: '{filename}')");
            return null;
        }
        
        if (index < 20 || index == 16384 || index == 16514)
            DebugLog.Write($"UOP.GetData: Index {index} FOUND (file: '{filename}')");
        return GetDataByHash(hash);
    }
    
    /// <summary>
    /// Get data by hash
    /// </summary>
    public byte[]? GetDataByHash(ulong hash)
    {
        if (_file == null || !_entries.TryGetValue(hash, out var entry))
            return null;
        
        try
        {
            lock (_file)
            {
                _file.Seek(entry.Offset + entry.HeaderLength, SeekOrigin.Begin);
                var data = new byte[entry.CompressedLength];
                _file.Read(data, 0, entry.CompressedLength);
                
                // Decompress if needed
                if (entry.Flags == 1 && entry.CompressedLength != entry.DecompressedLength)
                {
                    return Decompress(data, entry.DecompressedLength);
                }
                
                return data;
            }
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Check if an index exists
    /// </summary>
    public bool HasEntry(int index)
    {
        var hash = HashFileName(string.Format(_filePattern, index));
        return _entries.ContainsKey(hash);
    }
    
    /// <summary>
    /// Decompress zlib data
    /// </summary>
    private static byte[]? Decompress(byte[] data, int decompressedLength)
    {
        try
        {
            // Skip first 2 bytes (zlib header)
            using var input = new MemoryStream(data, 2, data.Length - 2);
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            
            var output = new byte[decompressedLength];
            var totalRead = 0;
            
            while (totalRead < decompressedLength)
            {
                var read = deflate.Read(output, totalRead, decompressedLength - totalRead);
                if (read == 0) break;
                totalRead += read;
            }
            
            return output;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UOP: Decompress error: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Hash a filename for UOP lookup (Adler32-like hash)
    /// </summary>
    public static ulong HashFileName(string filename)
    {
        uint eax, ecx, edx, ebx, esi, edi;
        
        eax = ecx = edx = ebx = esi = edi = 0;
        ebx = edi = esi = (uint)filename.Length + 0xDEADBEEF;
        
        int i = 0;
        
        for (i = 0; i + 12 < filename.Length; i += 12)
        {
            edi = (uint)((filename[i + 7] << 24) | (filename[i + 6] << 16) | (filename[i + 5] << 8) | filename[i + 4]) + edi;
            esi = (uint)((filename[i + 11] << 24) | (filename[i + 10] << 16) | (filename[i + 9] << 8) | filename[i + 8]) + esi;
            edx = (uint)((filename[i + 3] << 24) | (filename[i + 2] << 16) | (filename[i + 1] << 8) | filename[i]) - esi;
            
            edx = (edx + ebx) ^ (esi >> 28) ^ (esi << 4);
            esi += edi;
            edi = (edi - edx) ^ (edx >> 26) ^ (edx << 6);
            edx += esi;
            esi = (esi - edi) ^ (edi >> 24) ^ (edi << 8);
            edi += edx;
            ebx = (edx - esi) ^ (esi >> 16) ^ (esi << 16);
            esi += edi;
            edi = (edi - ebx) ^ (ebx >> 13) ^ (ebx << 19);
            ebx += esi;
            esi = (esi - edi) ^ (edi >> 28) ^ (edi << 4);
            edi += ebx;
        }
        
        if (filename.Length - i > 0)
        {
            switch (filename.Length - i)
            {
                case 12:
                    esi += (uint)filename[i + 11] << 24;
                    goto case 11;
                case 11:
                    esi += (uint)filename[i + 10] << 16;
                    goto case 10;
                case 10:
                    esi += (uint)filename[i + 9] << 8;
                    goto case 9;
                case 9:
                    esi += filename[i + 8];
                    goto case 8;
                case 8:
                    edi += (uint)filename[i + 7] << 24;
                    goto case 7;
                case 7:
                    edi += (uint)filename[i + 6] << 16;
                    goto case 6;
                case 6:
                    edi += (uint)filename[i + 5] << 8;
                    goto case 5;
                case 5:
                    edi += filename[i + 4];
                    goto case 4;
                case 4:
                    ebx += (uint)filename[i + 3] << 24;
                    goto case 3;
                case 3:
                    ebx += (uint)filename[i + 2] << 16;
                    goto case 2;
                case 2:
                    ebx += (uint)filename[i + 1] << 8;
                    goto case 1;
                case 1:
                    ebx += filename[i];
                    break;
            }
            
            esi = (esi ^ edi) - ((edi >> 18) ^ (edi << 14));
            ecx = (esi ^ ebx) - ((esi >> 21) ^ (esi << 11));
            edi = (edi ^ ecx) - ((ecx >> 7) ^ (ecx << 25));
            esi = (esi ^ edi) - ((edi >> 16) ^ (edi << 16));
            edx = (esi ^ ecx) - ((esi >> 28) ^ (esi << 4));
            edi = (edi ^ edx) - ((edx >> 18) ^ (edx << 14));
            eax = (esi ^ edi) - ((edi >> 8) ^ (edi << 24));
            
            return ((ulong)edi << 32) | eax;
        }
        
        return ((ulong)esi << 32) | eax;
    }
    
    public void Dispose()
    {
        _file?.Dispose();
        _file = null;
        _entries.Clear();
    }
}

/// <summary>
/// Entry in a UOP file table
/// </summary>
public struct UopFileEntry
{
    public long Offset;
    public int HeaderLength;
    public int CompressedLength;
    public int DecompressedLength;
    public ulong Hash;
    public uint Checksum;
    public short Flags;
}
