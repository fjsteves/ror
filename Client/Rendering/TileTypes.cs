// ==========================================================================
// TileTypes.cs - Core tile data structures
// ==========================================================================
// These structures match the UO file formats exactly.
// ==========================================================================

namespace RealmOfReality.Client.Rendering;

/// <summary>
/// Land tile data from map*.mul files.
/// Each tile is 3 bytes: 2-byte TileId + 1-byte Z.
/// </summary>
public readonly struct LandTile
{
    /// <summary>
    /// Tile graphic ID (0-16383).
    /// </summary>
    public readonly ushort TileId;
    
    /// <summary>
    /// Altitude (-128 to +127).
    /// </summary>
    public readonly sbyte Z;
    
    public LandTile(ushort tileId, sbyte z)
    {
        TileId = tileId;
        Z = z;
    }
    
    /// <summary>
    /// Check if this is a void tile (TileId 0).
    /// </summary>
    public bool IsVoid => TileId == 0;
    
    public override string ToString() => $"Land[{TileId}, Z={Z}]";
}

/// <summary>
/// Static tile data from statics*.mul files.
/// Each static is 7 bytes.
/// </summary>
public readonly struct StaticTile
{
    /// <summary>
    /// Static item ID (art index offset by 0x4000).
    /// </summary>
    public readonly ushort ItemId;
    
    /// <summary>
    /// X position within block (0-7).
    /// </summary>
    public readonly byte X;
    
    /// <summary>
    /// Y position within block (0-7).
    /// </summary>
    public readonly byte Y;
    
    /// <summary>
    /// Altitude (-128 to +127).
    /// </summary>
    public readonly sbyte Z;
    
    /// <summary>
    /// Color hue (0 = default color).
    /// </summary>
    public readonly short Hue;
    
    public StaticTile(ushort itemId, byte x, byte y, sbyte z, short hue)
    {
        ItemId = itemId;
        X = x;
        Y = y;
        Z = z;
        Hue = hue;
    }
    
    public override string ToString() => $"Static[{ItemId} @ ({X},{Y}) Z={Z} Hue={Hue}]";
}

/// <summary>
/// Land tile metadata from tiledata.mul.
/// </summary>
public readonly struct LandTileData
{
    /// <summary>
    /// Tile flags (see TileFlags enum).
    /// </summary>
    public readonly TileFlags Flags;
    
    /// <summary>
    /// Index into texmaps.mul for stretched terrain texture.
    /// 0 = no texmap (use land art tile instead).
    /// </summary>
    public readonly ushort TextureId;
    
    /// <summary>
    /// Tile name (up to 20 characters).
    /// </summary>
    public readonly string Name;
    
    public LandTileData(TileFlags flags, ushort textureId, string name)
    {
        Flags = flags;
        TextureId = textureId;
        Name = name ?? "";
    }
    
    /// <summary>
    /// Check if tile is water/wet.
    /// </summary>
    public bool IsWet => Flags.HasFlag(TileFlags.Wet);
    
    /// <summary>
    /// Check if tile blocks movement.
    /// </summary>
    public bool IsImpassable => Flags.HasFlag(TileFlags.Impassable);
    
    /// <summary>
    /// Check if this tile has a stretched terrain texture.
    /// </summary>
    public bool HasTexmap => TextureId > 0 && TextureId < UOConstants.MAX_TEXMAP_INDEX;
    
    public override string ToString() => $"LandData[{Name}, TexId={TextureId}, Flags={Flags:X}]";
}

/// <summary>
/// Static tile metadata from tiledata.mul.
/// </summary>
public readonly struct StaticTileData
{
    /// <summary>
    /// Tile flags (see TileFlags enum).
    /// </summary>
    public readonly TileFlags Flags;
    
    /// <summary>
    /// Weight for encumbrance/movement.
    /// </summary>
    public readonly byte Weight;
    
    /// <summary>
    /// Equipment layer (if wearable).
    /// </summary>
    public readonly byte Layer;
    
    /// <summary>
    /// Stack count or container capacity.
    /// </summary>
    public readonly int Count;
    
    /// <summary>
    /// Animation body ID (if animated).
    /// </summary>
    public readonly ushort AnimId;
    
    /// <summary>
    /// Default hue.
    /// </summary>
    public readonly ushort Hue;
    
    /// <summary>
    /// Light source index (if light source).
    /// </summary>
    public readonly ushort LightIndex;
    
    /// <summary>
    /// Height for stacking/collision.
    /// </summary>
    public readonly byte Height;
    
    /// <summary>
    /// Item name (up to 20 characters).
    /// </summary>
    public readonly string Name;
    
    public StaticTileData(
        TileFlags flags, byte weight, byte layer, int count,
        ushort animId, ushort hue, ushort lightIndex, byte height, string name)
    {
        Flags = flags;
        Weight = weight;
        Layer = layer;
        Count = count;
        AnimId = animId;
        Hue = hue;
        LightIndex = lightIndex;
        Height = height;
        Name = name ?? "";
    }
    
    // Flag accessors
    public bool IsBackground => Flags.HasFlag(TileFlags.Background);
    public bool IsWeapon => Flags.HasFlag(TileFlags.Weapon);
    public bool IsTransparent => Flags.HasFlag(TileFlags.Transparent);
    public bool IsTranslucent => Flags.HasFlag(TileFlags.Translucent);
    public bool IsWall => Flags.HasFlag(TileFlags.Wall);
    public bool IsDamaging => Flags.HasFlag(TileFlags.Damaging);
    public bool IsImpassable => Flags.HasFlag(TileFlags.Impassable);
    public bool IsSurface => Flags.HasFlag(TileFlags.Surface);
    public bool IsBridge => Flags.HasFlag(TileFlags.Bridge);
    public bool IsRoof => Flags.HasFlag(TileFlags.Roof);
    public bool IsDoor => Flags.HasFlag(TileFlags.Door);
    public bool IsFoliage => Flags.HasFlag(TileFlags.Foliage);
    public bool IsContainer => Flags.HasFlag(TileFlags.Container);
    public bool IsWearable => Flags.HasFlag(TileFlags.Wearable);
    public bool IsLightSource => Flags.HasFlag(TileFlags.LightSource);
    public bool IsAnimated => Flags.HasFlag(TileFlags.Animation);
    public bool HasPartialHue => Flags.HasFlag(TileFlags.PartialHue);
    
    public override string ToString() => $"StaticData[{Name}, H={Height}, Flags={Flags:X}]";
}

/// <summary>
/// Map block containing 8x8 land tiles.
/// </summary>
public class MapBlock
{
    /// <summary>
    /// Block header (usually 0).
    /// </summary>
    public uint Header;
    
    /// <summary>
    /// 64 land tiles (8x8 grid, row-major order).
    /// </summary>
    public LandTile[] Tiles = new LandTile[64];
    
    /// <summary>
    /// Get tile at cell position within block.
    /// </summary>
    public LandTile GetTile(int cellX, int cellY)
    {
        int index = (cellY << 3) + cellX; // cellY * 8 + cellX
        return (index >= 0 && index < 64) ? Tiles[index] : default;
    }
}

/// <summary>
/// Index entry for MUL files (artidx.mul, gumpidx.mul, etc.).
/// </summary>
public readonly struct IndexEntry
{
    /// <summary>
    /// File offset (0xFFFFFFFF = invalid).
    /// </summary>
    public readonly int Lookup;
    
    /// <summary>
    /// Data length in bytes.
    /// </summary>
    public readonly int Length;
    
    /// <summary>
    /// Extra data (usage varies by file type).
    /// </summary>
    public readonly int Extra;
    
    public IndexEntry(int lookup, int length, int extra)
    {
        Lookup = lookup;
        Length = length;
        Extra = extra;
    }
    
    /// <summary>
    /// Check if this entry is valid (has data).
    /// </summary>
    public bool IsValid => Lookup != -1 && Lookup != unchecked((int)0xFFFFFFFF) && Length > 0;
    
    public override string ToString() => $"Index[{Lookup:X8}, Len={Length}, Extra={Extra}]";
}

/// <summary>
/// Terrain vertex for 3D terrain rendering.
/// </summary>
public struct TerrainVertex
{
    /// <summary>
    /// World position (X, Y in tile units, Z in altitude units).
    /// </summary>
    public float WorldX, WorldY, WorldZ;
    
    /// <summary>
    /// Screen position after isometric projection.
    /// </summary>
    public float ScreenX, ScreenY;
    
    /// <summary>
    /// Texture coordinates (0-1).
    /// </summary>
    public float U, V;
    
    public TerrainVertex(float worldX, float worldY, float worldZ, float u, float v)
    {
        WorldX = worldX;
        WorldY = worldY;
        WorldZ = worldZ;
        U = u;
        V = v;
        
        // Calculate screen position
        var screen = IsometricMath.WorldToScreen(worldX, worldY, worldZ);
        ScreenX = screen.X;
        ScreenY = screen.Y;
    }
}
