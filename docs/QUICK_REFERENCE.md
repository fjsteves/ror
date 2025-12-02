# Realm of Reality - Quick Reference

A developer's cheat sheet for common tasks and critical patterns.

## Isometric Formulas

```csharp
// World to Screen
screenX = (tileX - tileY) * 22;
screenY = (tileX + tileY) * 22 - tileZ * 4;

// Screen to World (assuming Z=0)
tileX = (screenX/22 + screenY/22) / 2;
tileY = (screenY/22 - screenX/22) / 2;
```

## Block/Cell Indexing

```csharp
// Map block index (COLUMN-MAJOR!)
int blockIndex = blockX * blockHeight + blockY;  // ✓ CORRECT
// NOT: blockY * blockWidth + blockX           // ✗ WRONG

// Cell within block (row-major)
int cellIndex = cellY * 8 + cellX;
```

## Texture Lookup Chain

```csharp
// ✓ CORRECT: Use TileData.TextureId
ushort textureId = TileData.GetLandTile(landTile.TileId).TextureId;
Texture2D tex = Texmaps.GetTexmap(textureId);

// ✗ WRONG: Never pass TileId directly!
Texture2D tex = Texmaps.GetTexmap(landTile.TileId);  // WRONG!
```

## Terrain Quad Corners

```
NW (x,y) ────── NE (x+1,y)
   │              │
   │    Cell      │
   │              │
SW (x,y+1) ───── SE (x+1,y+1)

Indices: { 0, 2, 1, 1, 2, 3 }  // Two triangles
UV:      NW(0,0) NE(1,0) SW(0,1) SE(1,1)
```

## Depth Sorting

```csharp
int priority = (tileX + tileY) * 256 + tileZ + layerOffset;

// Layer offsets (prevent Z-fighting):
// Land=0, Static=1, Item=2, Mobile=3, Effect=4

// SpriteBatch depth (inverted):
float depth = 1.0f - (priority / 10000000f);
```

## File Format Sizes

| Format | Size |
|--------|------|
| Map block | 196 bytes (4 header + 64×3 tiles) |
| Land tile (old) | 26 bytes |
| Land tile (new) | 30 bytes |
| Static tile (old) | 37 bytes |
| Static tile (new) | 41 bytes |
| Static entry (map) | 7 bytes |
| Index entry | 12 bytes |

## ARGB1555 Conversion

```csharp
if (color16 == 0) return Transparent;
int r = ((color16 >> 10) & 0x1F) * 255 / 31;
int g = ((color16 >> 5) & 0x1F) * 255 / 31;
int b = (color16 & 0x1F) * 255 / 31;
return new Color(r, g, b, 255);
```

## Map Dimensions

| Facet | Tiles | Blocks |
|-------|-------|--------|
| Felucca | 7168×4096 | 896×512 |
| Trammel | 7168×4096 | 896×512 |
| Ilshenar | 2304×1600 | 288×200 |
| Malas | 2560×2560 | 320×320 |
| Tokuno | 1448×1448 | 181×181 |
| Ter Mur | 1280×4096 | 160×512 |

## Animation Directions

```
Stored: N, NW, W, SW, S (5 directions)
Mirror: NE←NW, E←W, SE←SW (with FlipHorizontally)
```

## Body Type Ranges

```
0-199:    Monster  (13 actions)
200-399:  Animal   (22 actions)
400+:     Human    (35 actions)
```

## Common Tile IDs

| ID | Name |
|----|------|
| 0 | Void (no tile) |
| 3 | Grass |
| 168 | Water |
| 510+ | Mountains |

## Critical Rules

1. **NEVER** use SpriteBatch for terrain - use 3D quads
2. **NEVER** pass TileId to Texmaps - use TextureId
3. **ALWAYS** use column-major block indexing
4. **ALWAYS** check bounds: `x < Width-1` (not `<=` or `Width`)
5. **ALWAYS** sort terrain quads back-to-front

## Debug Logging Tags

```
[Map]           MapLoader
[TileData]      TileDataLoader
[Texmaps]       TexmapLoader
[Art]           ArtLoader
[WorldRenderer] Rendering
```

## Common Console Checks

```
TileData loaded:  [TileData] Loaded 16384 land tiles
TextureId valid:  [TileData] Sample: LandTile[3] TextureId=32
Texmaps loaded:   [Texmaps] Loaded: texmaps.mul
Map loaded:       [Map] Loaded from MUL format
```
