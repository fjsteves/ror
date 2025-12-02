# Realm of Reality - Architecture Deep Dive

This document provides comprehensive technical details about the engine architecture,
file formats, and implementation patterns used in Realm of Reality.

## Table of Contents

1. [Rendering Pipeline](#rendering-pipeline)
2. [Asset Loading System](#asset-loading-system)
3. [UO File Format Specifications](#uo-file-format-specifications)
4. [Isometric Projection Math](#isometric-projection-math)
5. [Coordinate Systems](#coordinate-systems)
6. [Entity System](#entity-system)
7. [Network Architecture](#network-architecture)

---

## Rendering Pipeline

### Overview

The rendering pipeline follows a layered approach:

```
┌─────────────────────────────────────────────────────────────┐
│                     WorldRenderer                            │
├─────────────────────────────────────────────────────────────┤
│  1. Terrain Layer    │ 3D quads via BasicEffect              │
│  2. Statics Layer    │ SpriteBatch, depth-sorted             │
│  3. Entities Layer   │ SpriteBatch, animated sprites         │
│  4. UI Layer         │ SpriteBatch, screen-space             │
└─────────────────────────────────────────────────────────────┘
```

### Terrain Rendering (CRITICAL)

Terrain is rendered as 3D textured quads, NOT SpriteBatch sprites.
This is essential for proper height interpolation.

```csharp
// Each terrain cell is a quad with 4 corners at potentially different heights
//
//        NW ──── NE
//        │ ╲    │
//        │   ╲  │      Screen orientation
//        │     ╲│
//        SW ──── SE
//
// Quad corners:
//   NW = (x,   y  ) - Top vertex
//   NE = (x+1, y  ) - Right vertex  
//   SW = (x,   y+1) - Left vertex
//   SE = (x+1, y+1) - Bottom vertex
```

**Why NOT SpriteBatch?**
- SpriteBatch renders axis-aligned rectangles
- Cannot handle per-vertex Z heights
- Results in visual gaps and Z-fighting on sloped terrain

**Implementation:**
```csharp
// Build vertex buffer with world-to-screen transformed positions
_quadVertices[0] = new VertexPositionTexture(new Vector3(drawNW, 0), new Vector2(0, 0));  // NW
_quadVertices[1] = new VertexPositionTexture(new Vector3(drawNE, 0), new Vector2(1, 0));  // NE
_quadVertices[2] = new VertexPositionTexture(new Vector3(drawSW, 0), new Vector2(0, 1));  // SW
_quadVertices[3] = new VertexPositionTexture(new Vector3(drawSE, 0), new Vector2(1, 1));  // SE

// Index buffer: two triangles (NW-SW-NE, NE-SW-SE)
short[] indices = { 0, 2, 1, 1, 2, 3 };

// Render with BasicEffect
_terrainEffect.Texture = quadTexture;
_graphics.DrawUserIndexedPrimitives(...);
```

### Texture Selection Pipeline

```
┌─────────────────────────────────────────────────────────────┐
│               Texture Selection Flow                         │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│   LandTile.TileId  ──►  TileData.GetLandTile(TileId)        │
│                              │                               │
│                              ▼                               │
│                        TextureId                             │
│                              │                               │
│                    ┌────────┴────────┐                       │
│                    │                  │                       │
│            TextureId > 0      TextureId == 0                 │
│                    │                  │                       │
│                    ▼                  ▼                       │
│           Texmaps.GetTexmap()   Art.GetLandTile()           │
│            (64×64 or 128×128)      (44×44 diamond)           │
│                    │                  │                       │
│                    └────────┬────────┘                       │
│                             │                                │
│                             ▼                                │
│                      Final Texture                           │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

**CRITICAL**: NEVER pass `LandTile.TileId` directly to `TexmapLoader.GetTexmap()`.
The TileId is an art index; you must look up `TileData.TextureId` first.

### Depth Sorting

Objects are rendered back-to-front using priority values:

```csharp
// Priority calculation
int priority = (tileX + tileY) * 256 + (tileZ + 128);

// Layer offsets (added to prevent Z-fighting)
const int LAND_OFFSET = 0;
const int STATIC_OFFSET = 1;
const int ITEM_OFFSET = 2;
const int MOBILE_OFFSET = 3;
const int EFFECT_OFFSET = 4;

// SpriteBatch depth (inverted: 0=front, 1=back)
float depth = 1.0f - (priority / 10000000f);
```

---

## Asset Loading System

### Loader Hierarchy

```
UOAssetManager
    ├── TileDataLoader   (tiledata.mul)
    ├── MapLoader        (map*.mul, statics*.mul)
    ├── TexmapLoader     (texmaps.mul, texidx.mul)
    ├── ArtLoader        (art.mul, artidx.mul)
    ├── AnimLoader       (anim*.mul)
    ├── GumpLoader       (gumps.mul, gumpidx.mul)
    └── HuesLoader       (hues.mul)
```

### Loading Order

The loading order is critical due to dependencies:

1. **TileDataLoader** - Must load first (provides TextureId mappings)
2. **MapLoader** - Depends on nothing
3. **TexmapLoader** - Depends on TileDataLoader for TextureId validation
4. **ArtLoader** - Independent
5. **AnimLoader** - Independent
6. **GumpLoader** - Independent
7. **HuesLoader** - Used by all for color transformations

### Caching Strategy

All loaders implement LRU-style caching:

```csharp
private const int MAX_CACHE_SIZE = 256;

private void CacheEntry(int index, T value)
{
    if (_cache.Count >= MAX_CACHE_SIZE)
    {
        // Evict oldest half
        var keysToRemove = _cache.Keys.Take(MAX_CACHE_SIZE / 2).ToList();
        foreach (var key in keysToRemove)
        {
            _cache[key]?.Dispose();
            _cache.Remove(key);
        }
    }
    _cache[index] = value;
}
```

---

## UO File Format Specifications

### tiledata.mul

Contains metadata for all land and static tiles.

**Format Detection:**
```csharp
bool isNewFormat = fileLength > 3_188_736;
int landTileSize = isNewFormat ? 30 : 26;
int staticTileSize = isNewFormat ? 41 : 37;
```

**Land Tile Structure (Old Format - 26 bytes):**
```
Offset  Size  Field       Description
──────  ────  ────────    ───────────────────────────
0       4     Flags       Tile property flags (uint)
4       2     TextureId   Index into texmaps.mul (ushort) ← CRITICAL
6       20    Name        Null-terminated ASCII string
```

**Land Tile Structure (New Format - 30 bytes):**
```
Offset  Size  Field       Description
──────  ────  ────────    ───────────────────────────
0       8     Flags       Extended flags (ulong)
8       2     TextureId   Index into texmaps.mul (ushort)
10      20    Name        Null-terminated ASCII string
```

**Static Tile Structure (Old Format - 37 bytes):**
```
Offset  Size  Field       Description
──────  ────  ────────    ───────────────────────────
0       4     Flags       Item property flags
4       1     Weight      Item weight (255 = immovable)
5       1     Layer       Equipment layer
6       4     Count       Stack count
10      2     AnimId      Animation body ID
12      2     Hue         Default hue
14      2     LightIndex  Light source index
16      1     Height      Z-height for stacking
17      20    Name        Null-terminated ASCII
```

**Group Structure:**
- 512 land groups, each with 4-byte header + 32 tiles
- Total land tiles: 512 × 32 = 16,384 (0x0000-0x3FFF)

### map*.mul

Contains terrain height and tile data in 8×8 blocks.

**Block Structure (196 bytes):**
```
Offset  Size  Field       Description
──────  ────  ────────    ───────────────────────────
0       4     Header      Block header (usually 0)
4       192   Cells       64 cells × 3 bytes each
```

**Cell Structure (3 bytes):**
```
Offset  Size  Field       Description
──────  ────  ────────    ───────────────────────────
0       2     TileId      Land tile graphic ID (ushort)
2       1     Z           Altitude (sbyte, -128 to +127)
```

**Block Indexing (CRITICAL - Column-Major):**
```csharp
// CORRECT: Column-major indexing
int blockIndex = blockX * blockHeight + blockY;

// WRONG: Row-major (will corrupt map!)
// int blockIndex = blockY * blockWidth + blockX;  // DON'T DO THIS

// Cell indexing within block (Row-major)
int cellIndex = cellY * 8 + cellX;
```

**Map Dimensions:**
```
Facet       Width   Height  Tiles         Blocks
─────────   ─────   ──────  ─────────     ────────────
Felucca     896     512     7168×4096     896×512
Trammel     896     512     7168×4096     896×512
Ilshenar    288     200     2304×1600     288×200
Malas       320     320     2560×2560     320×320
Tokuno      181     181     1448×1448     181×181
Ter Mur     160     512     1280×4096     160×512
```

### texmaps.mul / texidx.mul

Contains stretched terrain textures (64×64 or 128×128).

**Index Entry (12 bytes):**
```
Offset  Size  Field       Description
──────  ────  ────────    ───────────────────────────
0       4     Offset      File offset (0xFFFFFFFF = invalid)
4       4     Length      Data length in bytes
8       4     Extra       0 = 64×64, non-zero = 128×128
```

**Pixel Format: ARGB1555**
```
Bit 15:    Alpha (1 = opaque)
Bits 14-10: Red (5 bits)
Bits 9-5:   Green (5 bits)
Bits 4-0:   Blue (5 bits)
```

**Conversion to RGBA8888:**
```csharp
public static Color ConvertARGB1555(ushort color16)
{
    if (color16 == 0) return Color.Transparent;
    
    int r = ((color16 >> 10) & 0x1F) * 255 / 31;
    int g = ((color16 >> 5) & 0x1F) * 255 / 31;
    int b = (color16 & 0x1F) * 255 / 31;
    
    return new Color(r, g, b, 255);
}
```

### art.mul / artidx.mul

Contains land tile and static item graphics.

**Land Tiles (IDs 0-16383):**
- Fixed 44×44 diamond shape
- Raw ARGB1555 pixels in diamond pattern
- Row widths: 2, 4, 6, ..., 44, ..., 6, 4, 2
- Total pixels: 968
- File size: 1936 bytes minimum

```csharp
// Diamond row pattern
for (int y = 0; y < 22; y++)
{
    int rowWidth = (y + 1) * 2;      // 2, 4, 6, ..., 44
    int startX = (44 - rowWidth) / 2;
    // Read rowWidth pixels for this row
}
for (int y = 22; y < 44; y++)
{
    int rowWidth = (44 - y) * 2;     // 42, 40, ..., 2
    int startX = (44 - rowWidth) / 2;
    // Read rowWidth pixels for this row
}
```

**Static Items (IDs 16384+):**
- Variable size with RLE compression
- Header: 4 bytes flags + 2 bytes width + 2 bytes height
- Lookup table: 2 bytes per row (offset from pixel data)
- RLE format: runs of (offset, length, pixels)

### statics*.mul / staidx*.mul

Contains world static objects (walls, trees, furniture).

**Index Entry (12 bytes):**
```
Offset  Size  Field       Description
──────  ────  ────────    ───────────────────────────
0       4     Offset      File offset in statics*.mul
4       4     Length      Data length
8       4     Extra       Unused
```

**Static Entry (7 bytes):**
```
Offset  Size  Field       Description
──────  ────  ────────    ───────────────────────────
0       2     ItemId      Static item graphic ID
2       1     X           X within block (0-7)
3       1     Y           Y within block (0-7)
4       1     Z           Altitude (sbyte)
5       2     Hue         Color hue
```

---

## Isometric Projection Math

### World to Screen Conversion

```csharp
const int TILE_SIZE = 44;   // Diamond width/height
const int TILE_STEP = 22;   // Half tile (screen step)
const int Z_SCALE = 4;      // Pixels per Z unit

public static Vector2 WorldToScreen(int tileX, int tileY, int tileZ)
{
    int screenX = (tileX - tileY) * TILE_STEP;
    int screenY = (tileX + tileY) * TILE_STEP - tileZ * Z_SCALE;
    return new Vector2(screenX, screenY);
}
```

### Screen to World Conversion

```csharp
public static (int tileX, int tileY) ScreenToWorld(int screenX, int screenY, int assumedZ = 0)
{
    // Adjust for assumed Z height
    screenY += assumedZ * Z_SCALE;
    
    // Inverse of isometric transform
    float fx = (screenX / (float)TILE_STEP + screenY / (float)TILE_STEP) / 2f;
    float fy = (screenY / (float)TILE_STEP - screenX / (float)TILE_STEP) / 2f;
    
    return ((int)MathF.Floor(fx), (int)MathF.Floor(fy));
}
```

### Visual Representation

```
World Coordinates          Screen Projection
                          
     Y                           ◆ (0,0)
     ▲                          ╱ ╲
     │                         ╱   ╲
     │                        ◆     ◆ (1,0)
     │                       ╱ ╲   ╱ ╲
     └───────► X            ◆   ◆ ◆   ◆
                           (0,1) (1,1)

Each tile is a 44×44 diamond that takes up 22px horizontal space
and 22px vertical space (before Z offset).
```

---

## Coordinate Systems

### Tile Coordinates
- Origin: Northwest corner of map
- X increases going East
- Y increases going South
- Range: (0,0) to (Width-1, Height-1)

### Block Coordinates
- 8 tiles per block in each dimension
- `blockX = tileX / 8`
- `blockY = tileY / 8`

### Cell Coordinates (within block)
- `cellX = tileX % 8` (0-7)
- `cellY = tileY % 8` (0-7)

### Screen Coordinates
- Origin: Center of viewport (after camera transform)
- Positive X: Right
- Positive Y: Down

---

## Entity System

### Entity Hierarchy

```
Entity (base)
├── Mobile (can move)
│   ├── PlayerEntity
│   └── NpcEntity
└── Item (static objects)
    ├── Equipment
    └── Container
```

### Position Representation

```csharp
public struct EntityPosition
{
    public float X;     // Tile X (can be fractional during movement)
    public float Y;     // Tile Y
    public float Z;     // Height offset from terrain
}
```

### Animation System

**Direction Mapping (UO Standard):**
```
Visual Direction    Stored Direction    Mirror From
────────────────    ────────────────    ───────────
North               N                   -
Northeast           NW                  ← mirrored
East                W                   ← mirrored
Southeast           SW                  ← mirrored
South               S                   -
Southwest           SW                  -
West                W                   -
Northwest           NW                  -
```

**Body Type Ranges:**
```
Range       Type        Action Count
─────────   ─────────   ────────────
0-199       Monster     13 actions
200-399     Animal      22 actions
400+        Human       35 actions
```

---

## Network Architecture

### Packet Structure

```
┌────────┬──────────┬───────────────┐
│ Length │  Opcode  │   Payload     │
│ 4 bytes│  2 bytes │   Variable    │
└────────┴──────────┴───────────────┘
```

### Connection Flow

```
Client                              Server
  │                                   │
  │──────── Connect ─────────────────►│
  │                                   │
  │◄──────── Accept ─────────────────│
  │                                   │
  │──── LoginRequest(user, pass) ────►│
  │                                   │
  │◄─── LoginResponse(status) ───────│
  │                                   │
  │──── CharacterSelect(id) ─────────►│
  │                                   │
  │◄─── WorldState(entities, map) ───│
  │                                   │
  │◄═══════ Game Loop ══════════════►│
```

### Key Packet Types

| Opcode | Name | Direction | Purpose |
|--------|------|-----------|---------|
| 0x01 | LoginRequest | C→S | Authentication |
| 0x02 | LoginResponse | S→C | Auth result |
| 0x10 | MoveRequest | C→S | Player movement |
| 0x11 | MoveUpdate | S→C | Entity position |
| 0x20 | ChatMessage | Both | Chat communication |
| 0x30 | EntitySpawn | S→C | New entity appears |
| 0x31 | EntityDespawn | S→C | Entity removed |

---

## Performance Considerations

### Rendering Budget

At 60 FPS, each frame has ~16.6ms. Target allocation:
- Terrain rendering: 4-6ms
- Statics rendering: 2-3ms
- Entity rendering: 2-3ms
- UI rendering: 1-2ms
- Networking: 1-2ms
- Buffer: 3-4ms

### Memory Management

- Block cache: 512 blocks × ~1KB = ~512KB
- Texture cache: 256 textures × ~64KB avg = ~16MB
- Entity pool: 1000 entities × ~256 bytes = ~256KB

### Optimization Strategies

1. **Frustum Culling**: Only render visible tiles
2. **Block Caching**: Keep recently used map blocks in memory
3. **Texture Atlasing**: Combine textures to reduce draw calls
4. **Entity Pooling**: Reuse entity instances
5. **Lazy Loading**: Load assets on-demand

---

## Debugging

### Console Log Tags

| Tag | System |
|-----|--------|
| `[Map]` | MapLoader |
| `[TileData]` | TileDataLoader |
| `[Texmaps]` | TexmapLoader |
| `[Art]` | ArtLoader |
| `[WorldRenderer]` | Rendering pipeline |
| `[Network]` | Networking |

### Common Issues

**Black terrain:**
1. TileData not loaded
2. TextureId being ignored
3. Texmap lookup failing

**Misaligned tiles:**
1. Wrong block indexing (row vs column major)
2. Incorrect cell indexing
3. Camera transform issues

**Z-fighting:**
1. Depth calculation incorrect
2. Layer offsets not applied
3. Terrain quads not sorted

---

*Last updated: December 2025*
