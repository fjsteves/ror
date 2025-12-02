// ==========================================================================
// UOConstants.cs - Core constants for Ultima Online rendering
// ==========================================================================
// These values are derived from ClassicUO and match the original UO client.
// Do not modify unless you understand the implications for all rendering.
// ==========================================================================

namespace RealmOfReality.Client.Rendering;

/// <summary>
/// Core constants used throughout the UO rendering pipeline.
/// All values are derived from ClassicUO's implementation.
/// </summary>
public static class UOConstants
{
    // ========================================================================
    // TILE DIMENSIONS
    // ========================================================================
    
    /// <summary>
    /// Width of a land tile diamond in pixels (44x44).
    /// </summary>
    public const int TILE_SIZE = 44;
    
    /// <summary>
    /// Half-tile step - the horizontal/vertical distance between adjacent tiles.
    /// This is TILE_SIZE / 2 = 22 pixels.
    /// </summary>
    public const int TILE_STEP = 22;
    
    /// <summary>
    /// Pixels per Z-unit of altitude.
    /// Higher Z values move objects up on screen by Z * Z_SCALE pixels.
    /// </summary>
    public const int Z_SCALE = 4;
    
    // ========================================================================
    // MAP BLOCK STRUCTURE
    // ========================================================================
    
    /// <summary>
    /// Tiles per block dimension (8x8 tiles per block).
    /// </summary>
    public const int BLOCK_SIZE = 8;
    
    /// <summary>
    /// Bytes per map block (4-byte header + 64 tiles * 3 bytes each).
    /// </summary>
    public const int BLOCK_BYTES = 196;
    
    /// <summary>
    /// Bytes per land tile in map block (2-byte TileId + 1-byte Z).
    /// </summary>
    public const int LAND_TILE_BYTES = 3;
    
    /// <summary>
    /// Bytes per static tile (2-byte ItemId + 1-byte X + 1-byte Y + 1-byte Z + 2-byte Hue).
    /// </summary>
    public const int STATIC_TILE_BYTES = 7;
    
    /// <summary>
    /// Bytes per index entry (4-byte offset + 4-byte length + 4-byte extra).
    /// </summary>
    public const int INDEX_ENTRY_BYTES = 12;
    
    // ========================================================================
    // TILEDATA CONSTANTS
    // ========================================================================
    
    /// <summary>
    /// Total land tile count (0x0000 - 0x3FFF).
    /// </summary>
    public const int LAND_TILE_COUNT = 0x4000; // 16384
    
    /// <summary>
    /// First static item index (land tiles end at 0x3FFF).
    /// </summary>
    public const int STATIC_OFFSET = 0x4000; // 16384
    
    /// <summary>
    /// Maximum valid texmap index.
    /// </summary>
    public const int MAX_TEXMAP_INDEX = 0x4000; // 16384
    
    /// <summary>
    /// Tiles per tiledata group.
    /// </summary>
    public const int TILEDATA_GROUP_SIZE = 32;
    
    /// <summary>
    /// Number of land tile groups (512 * 32 = 16384 tiles).
    /// </summary>
    public const int LAND_TILE_GROUPS = 512;
    
    // ========================================================================
    // TILEDATA FORMAT SIZES
    // ========================================================================
    
    /// <summary>
    /// Bytes per land tile in old tiledata format.
    /// 4-byte flags + 2-byte TextureId + 20-byte name = 26 bytes.
    /// </summary>
    public const int LAND_TILE_OLD_SIZE = 26;
    
    /// <summary>
    /// Bytes per land tile in new (High Seas+) tiledata format.
    /// 8-byte flags + 2-byte TextureId + 20-byte name = 30 bytes.
    /// </summary>
    public const int LAND_TILE_NEW_SIZE = 30;
    
    /// <summary>
    /// Bytes per static tile in old tiledata format.
    /// 4-byte flags + 1 weight + 1 layer + 4 count + 2 animId + 2 hue + 
    /// 2 lightIndex + 1 height + 20 name = 37 bytes.
    /// </summary>
    public const int STATIC_TILE_OLD_SIZE = 37;
    
    /// <summary>
    /// Bytes per static tile in new (High Seas+) tiledata format.
    /// 8-byte flags + same fields = 41 bytes.
    /// </summary>
    public const int STATIC_TILE_NEW_SIZE = 41;
    
    /// <summary>
    /// File size threshold for detecting High Seas+ format.
    /// </summary>
    public const long TILEDATA_NEW_FORMAT_THRESHOLD = 3188736;
    
    // ========================================================================
    // TEXMAP SIZES
    // ========================================================================
    
    /// <summary>
    /// Small texmap size in pixels (64x64).
    /// </summary>
    public const int TEXMAP_SIZE_SMALL = 64;
    
    /// <summary>
    /// Large texmap size in pixels (128x128).
    /// </summary>
    public const int TEXMAP_SIZE_LARGE = 128;
    
    /// <summary>
    /// Bytes for a small texmap (64 * 64 * 2).
    /// </summary>
    public const int TEXMAP_BYTES_SMALL = 8192;
    
    /// <summary>
    /// Bytes for a large texmap (128 * 128 * 2).
    /// </summary>
    public const int TEXMAP_BYTES_LARGE = 32768;
    
    // ========================================================================
    // UOP CONSTANTS
    // ========================================================================
    
    /// <summary>
    /// Map blocks per UOP entry.
    /// </summary>
    public const int MAP_BLOCKS_PER_UOP_ENTRY = 4096;
    
    // ========================================================================
    // MAP FACET DIMENSIONS
    // ========================================================================
    
    /// <summary>
    /// Get map dimensions for a given facet.
    /// Returns (width, height) in tiles.
    /// </summary>
    public static (int Width, int Height) GetMapDimensions(int facet)
    {
        return facet switch
        {
            0 => (7168, 4096),  // Felucca
            1 => (7168, 4096),  // Trammel
            2 => (2304, 1600),  // Ilshenar
            3 => (2560, 2048),  // Malas
            4 => (1448, 1448),  // Tokuno
            5 => (1280, 4096),  // Ter Mur
            _ => (7168, 4096)   // Default to Felucca
        };
    }
    
    /// <summary>
    /// Get facet name.
    /// </summary>
    public static string GetFacetName(int facet)
    {
        return facet switch
        {
            0 => "Felucca",
            1 => "Trammel",
            2 => "Ilshenar",
            3 => "Malas",
            4 => "Tokuno",
            5 => "Ter Mur",
            _ => $"Unknown ({facet})"
        };
    }
}

/// <summary>
/// Tile data flags from tiledata.mul.
/// These match the original UO client flags.
/// </summary>
[Flags]
public enum TileFlags : ulong
{
    None = 0,
    Background = 0x00000001,
    Weapon = 0x00000002,
    Transparent = 0x00000004,
    Translucent = 0x00000008,
    Wall = 0x00000010,
    Damaging = 0x00000020,
    Impassable = 0x00000040,
    Wet = 0x00000080,
    Unknown1 = 0x00000100,
    Surface = 0x00000200,
    Bridge = 0x00000400,
    Generic = 0x00000800,
    Window = 0x00001000,
    NoShoot = 0x00002000,
    ArticleA = 0x00004000,
    ArticleAn = 0x00008000,
    Internal = 0x00010000,
    Foliage = 0x00020000,
    PartialHue = 0x00040000,
    Unknown2 = 0x00080000,
    Map = 0x00100000,
    Container = 0x00200000,
    Wearable = 0x00400000,
    LightSource = 0x00800000,
    Animation = 0x01000000,
    NoDiagonal = 0x02000000,
    Unknown3 = 0x04000000,
    Armor = 0x08000000,
    Roof = 0x10000000,
    Door = 0x20000000,
    StairBack = 0x40000000,
    StairRight = 0x80000000
}
