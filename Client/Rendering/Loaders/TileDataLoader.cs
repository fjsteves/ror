// ==========================================================================
// TileDataLoader.cs - tiledata.mul parser
// ==========================================================================
// Loads tile metadata including TextureId for texmap lookup.
// Supports both old (pre-High Seas) and new (HS+) formats.
// ==========================================================================

namespace RealmOfReality.Client.Rendering;

/// <summary>
/// Loads tile metadata from tiledata.mul.
/// Provides flags, names, and TextureId for land/static tiles.
/// </summary>
public sealed class TileDataLoader : IDisposable
{
    private LandTileData[] _landData = Array.Empty<LandTileData>();
    private StaticTileData[] _staticData = Array.Empty<StaticTileData>();
    
    public bool IsLoaded { get; private set; }
    public bool IsNewFormat { get; private set; }
    
    /// <summary>
    /// Number of land tiles loaded (should be 16384).
    /// </summary>
    public int LandTileCount => _landData.Length;
    
    /// <summary>
    /// Number of static tiles loaded.
    /// </summary>
    public int StaticTileCount => _staticData.Length;
    
    /// <summary>
    /// Load tiledata.mul from the specified path.
    /// </summary>
    public bool Load(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"[TileData] File not found: {path}");
            return false;
        }
        
        try
        {
            var fileData = File.ReadAllBytes(path);
            return LoadFromBytes(fileData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TileData] Load error: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Load tiledata from byte array.
    /// </summary>
    public bool LoadFromBytes(byte[] data)
    {
        try
        {
            long fileLength = data.Length;
            Console.WriteLine($"[TileData] Loading {fileLength:N0} bytes");
            
            // Detect format based on file size
            // Old format: 512 land groups * (4 + 32*26) + static groups * (4 + 32*37)
            // New format: 512 land groups * (4 + 32*30) + static groups * (4 + 32*41)
            IsNewFormat = fileLength > UOConstants.TILEDATA_NEW_FORMAT_THRESHOLD;
            
            int landTileSize = IsNewFormat ? UOConstants.LAND_TILE_NEW_SIZE : UOConstants.LAND_TILE_OLD_SIZE;
            int staticTileSize = IsNewFormat ? UOConstants.STATIC_TILE_NEW_SIZE : UOConstants.STATIC_TILE_OLD_SIZE;
            int landGroupSize = 4 + (UOConstants.TILEDATA_GROUP_SIZE * landTileSize);
            int staticGroupSize = 4 + (UOConstants.TILEDATA_GROUP_SIZE * staticTileSize);
            
            Console.WriteLine($"[TileData] Format: {(IsNewFormat ? "High Seas+" : "Classic")}");
            Console.WriteLine($"[TileData] Land tile size: {landTileSize}, Static tile size: {staticTileSize}");
            
            // Read land tiles (512 groups * 32 tiles = 16384)
            _landData = new LandTileData[UOConstants.LAND_TILE_COUNT];
            int offset = 0;
            
            for (int group = 0; group < UOConstants.LAND_TILE_GROUPS; group++)
            {
                offset += 4; // Skip group header
                
                for (int i = 0; i < UOConstants.TILEDATA_GROUP_SIZE; i++)
                {
                    int index = group * UOConstants.TILEDATA_GROUP_SIZE + i;
                    if (index >= UOConstants.LAND_TILE_COUNT)
                        break;
                    
                    _landData[index] = ReadLandTile(data, ref offset);
                }
            }
            
            // Calculate remaining bytes for statics
            long staticDataBytes = data.Length - offset;
            int staticGroups = (int)(staticDataBytes / staticGroupSize);
            int staticCount = staticGroups * UOConstants.TILEDATA_GROUP_SIZE;
            
            Console.WriteLine($"[TileData] Reading {staticCount:N0} static tiles from offset {offset}");
            
            // Read static tiles
            _staticData = new StaticTileData[staticCount];
            
            for (int group = 0; group < staticGroups; group++)
            {
                offset += 4; // Skip group header
                
                for (int i = 0; i < UOConstants.TILEDATA_GROUP_SIZE; i++)
                {
                    int index = group * UOConstants.TILEDATA_GROUP_SIZE + i;
                    if (index >= staticCount)
                        break;
                    
                    _staticData[index] = ReadStaticTile(data, ref offset);
                }
            }
            
            IsLoaded = true;
            Console.WriteLine($"[TileData] Loaded {_landData.Length:N0} land, {_staticData.Length:N0} static tiles");
            
            // Debug: Show some land tiles with valid TextureIds
            int shown = 0;
            for (int i = 0; i < Math.Min(1000, _landData.Length) && shown < 5; i++)
            {
                if (_landData[i].TextureId > 0)
                {
                    Console.WriteLine($"[TileData] Land[{i}]: TextureId={_landData[i].TextureId}, Name='{_landData[i].Name}'");
                    shown++;
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TileData] Parse error: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Read a single land tile entry.
    /// </summary>
    private LandTileData ReadLandTile(byte[] data, ref int offset)
    {
        TileFlags flags;
        if (IsNewFormat)
        {
            flags = (TileFlags)BinaryUtils.ReadUInt64(data, offset);
            offset += 8;
        }
        else
        {
            flags = (TileFlags)BinaryUtils.ReadUInt32(data, offset);
            offset += 4;
        }
        
        ushort textureId = BinaryUtils.ReadUInt16(data, offset);
        offset += 2;
        
        string name = BinaryUtils.ReadFixedString(data, offset, 20);
        offset += 20;
        
        return new LandTileData(flags, textureId, name);
    }
    
    /// <summary>
    /// Read a single static tile entry.
    /// </summary>
    private StaticTileData ReadStaticTile(byte[] data, ref int offset)
    {
        TileFlags flags;
        if (IsNewFormat)
        {
            flags = (TileFlags)BinaryUtils.ReadUInt64(data, offset);
            offset += 8;
        }
        else
        {
            flags = (TileFlags)BinaryUtils.ReadUInt32(data, offset);
            offset += 4;
        }
        
        byte weight = data[offset++];
        byte layer = data[offset++];
        int count = BinaryUtils.ReadInt32(data, offset); offset += 4;
        ushort animId = BinaryUtils.ReadUInt16(data, offset); offset += 2;
        ushort hue = BinaryUtils.ReadUInt16(data, offset); offset += 2;
        ushort lightIndex = BinaryUtils.ReadUInt16(data, offset); offset += 2;
        byte height = data[offset++];
        string name = BinaryUtils.ReadFixedString(data, offset, 20);
        offset += 20;
        
        return new StaticTileData(flags, weight, layer, count, animId, hue, lightIndex, height, name);
    }
    
    /// <summary>
    /// Get land tile data by tile ID.
    /// </summary>
    public LandTileData GetLandTile(int tileId)
    {
        if (tileId < 0 || tileId >= _landData.Length)
            return default;
        return _landData[tileId];
    }
    
    /// <summary>
    /// Get static tile data by item ID.
    /// </summary>
    public StaticTileData GetStaticTile(int itemId)
    {
        if (itemId < 0 || itemId >= _staticData.Length)
            return default;
        return _staticData[itemId];
    }
    
    /// <summary>
    /// Get TextureId for a land tile.
    /// Returns 0 if tile has no stretched texture.
    /// </summary>
    public ushort GetLandTextureId(int tileId)
    {
        if (tileId < 0 || tileId >= _landData.Length)
            return 0;
        return _landData[tileId].TextureId;
    }
    
    public void Dispose()
    {
        _landData = Array.Empty<LandTileData>();
        _staticData = Array.Empty<StaticTileData>();
        IsLoaded = false;
    }
}
