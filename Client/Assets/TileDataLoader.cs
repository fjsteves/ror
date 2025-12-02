namespace RealmOfReality.Client.Assets;

/// <summary>
/// Loads tile metadata from tiledata.mul - contains properties for all land and static tiles.
/// 
/// File Format:
/// - Land tiles: 512 groups × (4-byte header + 32 tiles)
/// - Static tiles: N groups × (4-byte header + 32 tiles)
/// 
/// Version Detection:
/// - Old format (pre-High Seas): 26-byte land tiles, 37-byte static tiles
/// - New format (High Seas+): 30-byte land tiles, 41-byte static tiles
/// - Detection: file size > 3,188,736 bytes indicates new format
/// 
/// Critical Field:
/// - LandTileData.TextureId maps to texmaps.mul for stretched terrain textures
/// 
/// Reference: ClassicUO TileDataLoader.cs
/// </summary>
public sealed class TileDataLoader : IDisposable
{
    // ═══════════════════════════════════════════════════════════════════
    // CONSTANTS
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>Total number of land tile entries (0x0000-0x3FFF)</summary>
    public const int LAND_TILE_COUNT = 0x4000; // 16,384
    
    /// <summary>Number of land tile groups (512 groups × 32 tiles = 16,384)</summary>
    private const int LAND_GROUP_COUNT = 512;
    
    /// <summary>Tiles per group</summary>
    private const int TILES_PER_GROUP = 32;
    
    /// <summary>Group header size in bytes</summary>
    private const int GROUP_HEADER_SIZE = 4;
    
    /// <summary>File size threshold for new (High Seas+) format</summary>
    private const long NEW_FORMAT_THRESHOLD = 3_188_736;
    
    // ═══════════════════════════════════════════════════════════════════
    // DATA STORAGE
    // ═══════════════════════════════════════════════════════════════════
    
    private LandTileData[] _landData = Array.Empty<LandTileData>();
    private StaticTileData[] _staticData = Array.Empty<StaticTileData>();
    
    // ═══════════════════════════════════════════════════════════════════
    // PUBLIC PROPERTIES
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>Whether tiledata.mul was successfully loaded</summary>
    public bool IsLoaded { get; private set; }
    
    /// <summary>Number of land tiles loaded</summary>
    public int LandCount => _landData.Length;
    
    /// <summary>Number of static tiles loaded</summary>
    public int StaticCount => _staticData.Length;
    
    /// <summary>Alias for StaticCount (compatibility)</summary>
    public int StaticItemCount => _staticData.Length;
    
    /// <summary>Whether the file uses the new (High Seas+) format</summary>
    public bool IsNewFormat { get; private set; }
    
    // ═══════════════════════════════════════════════════════════════════
    // LOADING
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Load tiledata.mul from the specified path.
    /// </summary>
    /// <param name="path">Full path to tiledata.mul</param>
    /// <returns>True if loaded successfully</returns>
    public bool Load(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"[TileData] File not found: {path}");
            return false;
        }
        
        try
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream);
            
            long fileSize = stream.Length;
            Console.WriteLine($"[TileData] Loading tiledata.mul ({fileSize:N0} bytes)");
            
            // Detect format version
            IsNewFormat = fileSize > NEW_FORMAT_THRESHOLD;
            int landTileSize = IsNewFormat ? 30 : 26;
            int staticTileSize = IsNewFormat ? 41 : 37;
            
            Console.WriteLine($"[TileData] Format: {(IsNewFormat ? "High Seas+" : "Classic")}");
            Console.WriteLine($"[TileData] Land tile size: {landTileSize} bytes, Static tile size: {staticTileSize} bytes");
            
            // ─────────────────────────────────────────────────────────────
            // Load land tiles (512 groups × 32 tiles = 16,384 total)
            // ─────────────────────────────────────────────────────────────
            _landData = new LandTileData[LAND_TILE_COUNT];
            
            for (int group = 0; group < LAND_GROUP_COUNT; group++)
            {
                // Skip 4-byte group header
                reader.ReadInt32();
                
                for (int i = 0; i < TILES_PER_GROUP; i++)
                {
                    int index = group * TILES_PER_GROUP + i;
                    if (index >= LAND_TILE_COUNT) break;
                    
                    _landData[index] = ReadLandTile(reader, IsNewFormat);
                }
            }
            
            // ─────────────────────────────────────────────────────────────
            // Load static tiles (remaining file data)
            // ─────────────────────────────────────────────────────────────
            long staticDataStart = stream.Position;
            long staticDataBytes = fileSize - staticDataStart;
            
            int bytesPerGroup = GROUP_HEADER_SIZE + (TILES_PER_GROUP * staticTileSize);
            int staticGroupCount = (int)(staticDataBytes / bytesPerGroup);
            int staticTileCount = staticGroupCount * TILES_PER_GROUP;
            
            Console.WriteLine($"[TileData] Static data: {staticDataBytes:N0} bytes, {staticGroupCount} groups, {staticTileCount} tiles");
            
            _staticData = new StaticTileData[staticTileCount];
            
            for (int group = 0; group < staticGroupCount; group++)
            {
                // Skip 4-byte group header
                reader.ReadInt32();
                
                for (int i = 0; i < TILES_PER_GROUP; i++)
                {
                    int index = group * TILES_PER_GROUP + i;
                    if (index >= staticTileCount) break;
                    
                    _staticData[index] = ReadStaticTile(reader, IsNewFormat);
                }
            }
            
            IsLoaded = true;
            Console.WriteLine($"[TileData] Loaded {_landData.Length:N0} land tiles, {_staticData.Length:N0} static tiles");
            
            // Debug: Show some sample TextureIds
            LogSampleTextureIds();
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TileData] Load failed: {ex.Message}");
            Console.WriteLine($"[TileData] Stack: {ex.StackTrace}");
            return false;
        }
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // DATA ACCESS
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Get land tile data by tile ID.
    /// Returns empty struct if ID is out of range.
    /// </summary>
    /// <param name="tileId">Land tile ID (0-16383)</param>
    public LandTileData GetLandTile(int tileId)
    {
        if (tileId < 0 || tileId >= _landData.Length)
            return default;
        
        return _landData[tileId];
    }
    
    /// <summary>
    /// Get static tile data by item ID.
    /// Returns empty struct if ID is out of range.
    /// </summary>
    /// <param name="itemId">Static item ID (0+)</param>
    public StaticTileData GetStaticTile(int itemId)
    {
        if (itemId < 0 || itemId >= _staticData.Length)
            return default;
        
        return _staticData[itemId];
    }
    
    /// <summary>Alias for GetStaticTile (compatibility)</summary>
    public StaticTileData GetStaticItem(int itemId) => GetStaticTile(itemId);
    
    /// <summary>
    /// Check if a land tile has a valid texture mapping.
    /// Tiles with TextureId=0 should use flat art instead of stretched texmaps.
    /// </summary>
    public bool HasTexture(int tileId)
    {
        if (tileId < 0 || tileId >= _landData.Length)
            return false;
        
        return _landData[tileId].TextureId > 0;
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ═══════════════════════════════════════════════════════════════════
    
    private static LandTileData ReadLandTile(BinaryReader reader, bool newFormat)
    {
        var data = new LandTileData();
        
        // Flags: 4 bytes (old) or 8 bytes (new)
        if (newFormat)
            data.Flags = reader.ReadUInt64();
        else
            data.Flags = reader.ReadUInt32();
        
        // TextureId: 2 bytes - maps to texmaps.mul index
        data.TextureId = reader.ReadUInt16();
        
        // Name: 20 bytes null-terminated ASCII
        data.Name = ReadFixedString(reader, 20);
        
        return data;
    }
    
    private static StaticTileData ReadStaticTile(BinaryReader reader, bool newFormat)
    {
        var data = new StaticTileData();
        
        // Flags: 4 bytes (old) or 8 bytes (new)
        if (newFormat)
            data.Flags = reader.ReadUInt64();
        else
            data.Flags = reader.ReadUInt32();
        
        // Properties
        data.Weight = reader.ReadByte();       // 1 byte
        data.Layer = reader.ReadByte();        // 1 byte (equipment layer)
        data.Count = reader.ReadInt32();       // 4 bytes (stack count)
        data.AnimId = reader.ReadUInt16();     // 2 bytes (animation body ID)
        data.Hue = reader.ReadUInt16();        // 2 bytes (default hue)
        data.LightIndex = reader.ReadUInt16(); // 2 bytes (light source index)
        data.Height = reader.ReadByte();       // 1 byte (Z height for stacking)
        data.Name = ReadFixedString(reader, 20); // 20 bytes
        
        return data;
    }
    
    private static string ReadFixedString(BinaryReader reader, int length)
    {
        var bytes = reader.ReadBytes(length);
        int end = Array.IndexOf(bytes, (byte)0);
        if (end < 0) end = length;
        return System.Text.Encoding.ASCII.GetString(bytes, 0, end);
    }
    
    private void LogSampleTextureIds()
    {
        // Count statistics
        int withTextureId = 0;
        int withoutTextureId = 0;
        
        for (int i = 0; i < _landData.Length; i++)
        {
            if (_landData[i].TextureId > 0)
                withTextureId++;
            else
                withoutTextureId++;
        }
        
        Console.WriteLine($"[TileData] TextureId statistics: {withTextureId} with TextureId, {withoutTextureId} without (use art fallback)");
        
        // Log first 10 land tiles with valid TextureIds
        Console.WriteLine("[TileData] Sample land tiles with TextureId:");
        int found = 0;
        for (int i = 0; i < Math.Min(1000, _landData.Length) && found < 10; i++)
        {
            if (_landData[i].TextureId > 0)
            {
                Console.WriteLine($"  LandTile[{i}] TextureId={_landData[i].TextureId}, Name=\"{_landData[i].Name}\"");
                found++;
            }
        }
        
        if (found == 0)
        {
            Console.WriteLine("[TileData] WARNING: No land tiles with TextureId found in first 1000 entries!");
            Console.WriteLine("[TileData] This may indicate a tiledata.mul format issue or custom client data.");
        }
    }
    
    public void Dispose()
    {
        _landData = Array.Empty<LandTileData>();
        _staticData = Array.Empty<StaticTileData>();
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// DATA STRUCTURES
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Land tile metadata from tiledata.mul.
/// Land tiles are the terrain layer (grass, dirt, water, etc.)
/// </summary>
public struct LandTileData
{
    /// <summary>Tile property flags (see TileFlag enum)</summary>
    public ulong Flags;
    
    /// <summary>
    /// Texture index for stretched terrain rendering.
    /// Maps to texmaps.mul - use this value (NOT the tile ID) to look up textures.
    /// Value of 0 means no stretched texture; use flat 44×44 art instead.
    /// </summary>
    public ushort TextureId;
    
    /// <summary>Tile name (e.g., "grass", "water", "dirt")</summary>
    public string Name;

    /// <summary>
    /// Whether this land tile references a stretched terrain texture.
    /// Matches ClassicUO behavior: valid texmap indices are greater than 0
    /// and below the maximum texmap count.
    /// </summary>
    public readonly bool HasTexmap => TextureId > 0 && TextureId < Client.Rendering.UOConstants.MAX_TEXMAP_INDEX;
    
    // ─────────────────────────────────────────────────────────────────────
    // Flag Properties
    // ─────────────────────────────────────────────────────────────────────
    
    public readonly bool IsWet => (Flags & (ulong)TileFlag.Wet) != 0;
    public readonly bool IsImpassable => (Flags & (ulong)TileFlag.Impassable) != 0;
    public readonly bool IsDamaging => (Flags & (ulong)TileFlag.Damaging) != 0;
}

/// <summary>
/// Static item metadata from tiledata.mul.
/// Static items are fixed world objects (walls, trees, furniture, etc.)
/// </summary>
public struct StaticTileData
{
    /// <summary>Item property flags (see TileFlag enum)</summary>
    public ulong Flags;
    
    /// <summary>Item weight (255 = immovable)</summary>
    public byte Weight;
    
    /// <summary>Equipment layer when worn (0 = not wearable)</summary>
    public byte Layer;
    
    /// <summary>Default stack count</summary>
    public int Count;
    
    /// <summary>Animation body ID for animated items</summary>
    public ushort AnimId;
    
    /// <summary>Default hue (color)</summary>
    public ushort Hue;
    
    /// <summary>Light source index (0 = no light)</summary>
    public ushort LightIndex;
    
    /// <summary>Z-height for stacking objects on top</summary>
    public byte Height;
    
    /// <summary>Item name</summary>
    public string Name;
    
    // ─────────────────────────────────────────────────────────────────────
    // Flag Properties
    // ─────────────────────────────────────────────────────────────────────
    
    public readonly bool IsBackground => (Flags & (ulong)TileFlag.Background) != 0;
    public readonly bool IsWeapon => (Flags & (ulong)TileFlag.Weapon) != 0;
    public readonly bool IsTransparent => (Flags & (ulong)TileFlag.Transparent) != 0;
    public readonly bool IsTranslucent => (Flags & (ulong)TileFlag.Translucent) != 0;
    public readonly bool IsWall => (Flags & (ulong)TileFlag.Wall) != 0;
    public readonly bool IsDamaging => (Flags & (ulong)TileFlag.Damaging) != 0;
    public readonly bool IsImpassable => (Flags & (ulong)TileFlag.Impassable) != 0;
    public readonly bool IsWet => (Flags & (ulong)TileFlag.Wet) != 0;
    public readonly bool IsSurface => (Flags & (ulong)TileFlag.Surface) != 0;
    public readonly bool IsBridge => (Flags & (ulong)TileFlag.Bridge) != 0;
    public readonly bool IsStackable => (Flags & (ulong)TileFlag.Generic) != 0;
    public readonly bool IsWindow => (Flags & (ulong)TileFlag.Window) != 0;
    public readonly bool IsNoShoot => (Flags & (ulong)TileFlag.NoShoot) != 0;
    public readonly bool IsFoliage => (Flags & (ulong)TileFlag.Foliage) != 0;
    public readonly bool IsPartialHue => (Flags & (ulong)TileFlag.PartialHue) != 0;
    public readonly bool IsMap => (Flags & (ulong)TileFlag.Map) != 0;
    public readonly bool IsContainer => (Flags & (ulong)TileFlag.Container) != 0;
    public readonly bool IsWearable => (Flags & (ulong)TileFlag.Wearable) != 0;
    public readonly bool IsLightSource => (Flags & (ulong)TileFlag.LightSource) != 0;
    public readonly bool IsAnimated => (Flags & (ulong)TileFlag.Animation) != 0;
    public readonly bool IsNoDiagonal => (Flags & (ulong)TileFlag.NoDiagonal) != 0;
    public readonly bool IsArmor => (Flags & (ulong)TileFlag.Armor) != 0;
    public readonly bool IsRoof => (Flags & (ulong)TileFlag.Roof) != 0;
    public readonly bool IsDoor => (Flags & (ulong)TileFlag.Door) != 0;
    public readonly bool IsStairBack => (Flags & (ulong)TileFlag.StairBack) != 0;
    public readonly bool IsStairRight => (Flags & (ulong)TileFlag.StairRight) != 0;
}

/// <summary>
/// Tile property flags from tiledata.mul.
/// These control rendering, collision, and game mechanics.
/// </summary>
[Flags]
public enum TileFlag : ulong
{
    None = 0,
    
    // Basic properties
    Background = 0x00000001,    // Draw behind other objects
    Weapon = 0x00000002,        // Is a weapon
    Transparent = 0x00000004,   // Fully transparent (invisible)
    Translucent = 0x00000008,   // Semi-transparent
    Wall = 0x00000010,          // Is a wall
    Damaging = 0x00000020,      // Causes damage when walked on
    Impassable = 0x00000040,    // Cannot walk through
    Wet = 0x00000080,           // Is water
    
    // Unknown/reserved
    Unknown1 = 0x00000100,
    
    // Surface properties
    Surface = 0x00000200,       // Can place items on top
    Bridge = 0x00000400,        // Is a bridge surface
    Generic = 0x00000800,       // Stackable item
    Window = 0x00001000,        // Is a window
    NoShoot = 0x00002000,       // Arrows/projectiles blocked
    ArticleA = 0x00004000,      // Uses "a" article
    ArticleAn = 0x00008000,     // Uses "an" article
    
    // Visual properties
    Internal = 0x00010000,      // Internal use only
    Foliage = 0x00020000,       // Is foliage (grass, leaves)
    PartialHue = 0x00040000,    // Only part is hued
    Unknown2 = 0x00080000,
    Map = 0x00100000,           // Is a map item
    Container = 0x00200000,     // Can hold items
    Wearable = 0x00400000,      // Can be equipped
    LightSource = 0x00800000,   // Emits light
    
    // Animation and special
    Animation = 0x01000000,     // Has animation
    NoDiagonal = 0x02000000,    // Can't move diagonally past
    Unknown3 = 0x04000000,
    Armor = 0x08000000,         // Is armor
    Roof = 0x10000000,          // Is a roof tile
    Door = 0x20000000,          // Is a door
    StairBack = 0x40000000,     // Stair facing back
    StairRight = 0x80000000,    // Stair facing right
    
    // High Seas+ extended flags (bits 32-63)
    AlphaBlend = 0x100000000,
    UseNewArt = 0x200000000,
    ArtUsed = 0x400000000,
    NoShadow = 0x1000000000,
    PixelBleed = 0x2000000000,
    PlayAnimOnce = 0x4000000000,
    MultiMovable = 0x10000000000
}
