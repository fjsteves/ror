# Rendering Pipeline Guide

This document explains how the Realm of Reality rendering system works, including the isometric projection, terrain rendering, and depth sorting algorithms.

## Overview

The rendering pipeline uses:
- **3D textured quads** for terrain (BasicEffect)
- **2D SpriteBatch** for entities, statics, and UI
- **Depth sorting** using painter's algorithm

## Isometric Projection

Realm of Reality uses Ultima Online's 2:1 dimetric projection (commonly called "isometric"):

```
Tile dimensions: 44×44 pixel diamond
Tile step: 22 pixels horizontal/vertical between adjacent tiles
Z scale: 4 pixels per altitude unit

Screen X = (tileX - tileY) × 22
Screen Y = (tileX + tileY) × 22 - tileZ × 4
```

### Coordinate Systems

| System | Description |
|--------|-------------|
| World (X, Y, Z) | Tile coordinates + altitude in game units |
| Screen (X, Y) | Pixel coordinates on screen |
| Block (X, Y) | 8×8 tile blocks for map storage |
| Cell (X, Y) | Position within a block (0-7) |

### Block Indexing

Map data is stored in 8×8 tile blocks using **column-major** ordering:

```csharp
blockIndex = blockX × blockHeight + blockY  // Column-major (ClassicUO)
cellIndex = cellY × 8 + cellX               // Row-major within block
```

## Terrain Rendering

### Quad-Based Approach

Unlike flat tile games, UO terrain uses **textured 3D quads** where each corner can have a different height:

```
        NW (x, y)
           ∧
          /|\
         / | \
        /  |  \
       /   |   \
      /    |    \
     /     |     \
    /      |      \
   SW (x, y+1)────SE (x+1, y+1)
    \      |      /
     \     |     /
      \    |    /
       \   |   /
        \  |  /
         \ | /
          \|/
           V
        NE (x+1, y)
```

### Vertex Calculation

For a terrain quad at tile (x, y):

```csharp
// Get corner heights from map data
int zNW = map.GetLandTile(x, y).Z;
int zNE = map.GetLandTile(x + 1, y).Z;
int zSW = map.GetLandTile(x, y + 1).Z;
int zSE = map.GetLandTile(x + 1, y + 1).Z;

// Calculate screen positions for each corner
var screenNW = WorldToScreen(x, y, zNW);
var screenNE = WorldToScreen(x + 1, y, zNE);
var screenSW = WorldToScreen(x, y + 1, zSW);
var screenSE = WorldToScreen(x + 1, y + 1, zSE);
```

### UV Mapping

Texture coordinates use standard quad mapping (NOT diamond):

```
NW = (0, 0)    NE = (1, 0)
SW = (0, 1)    SE = (1, 1)
```

### Triangulation

Each quad is split into 2 triangles:

```csharp
indices = { 0, 2, 1, 1, 2, 3 };  // NW-SW-NE, NE-SW-SE
// or
indices = { NW, SW, NE, NE, SW, SE };
```

## Texture Selection

### TileData → Texmaps Mapping

**Critical**: The index into texmaps.mul is `TileData.TextureId`, NOT the land tile ID!

```csharp
// CORRECT:
LandTileData data = tileData.GetLandTile(landTileId);
Texture2D texture = texmaps.GetTexmap(data.TextureId);  // Use TextureId!

// WRONG:
Texture2D texture = texmaps.GetTexmap(landTileId);  // Don't use tile ID directly!
```

### Texture Fallback Chain

```
1. texmaps.mul (stretched terrain, 64×64 or 128×128)
   ↓ (if TextureId = 0 or not found)
2. art.mul (44×44 diamond land tile)
   ↓ (if not found)
3. Procedural fallback (green/gray)
```

### Quad Texture Selection

When a quad spans tiles with different textures, pick the texture from the **highest-Z corner** with a valid TextureId:

```csharp
ushort PickTextureId(TerrainQuad quad)
{
    ushort bestTexId = 0;
    sbyte bestZ = sbyte.MinValue;
    
    foreach (corner in [NW, NE, SW, SE])
    {
        var data = TileData.GetLandTile(corner.TileId);
        if (data.TextureId > 0 && corner.Z >= bestZ)
        {
            bestZ = corner.Z;
            bestTexId = data.TextureId;
        }
    }
    
    return bestTexId;
}
```

## Depth Sorting

### Render Priority Formula

```csharp
priority = (tileX + tileY) × 256 + (tileZ + 128) + layerOffset
```

Where `layerOffset`:
- 0 = Land/terrain
- 1 = Static items
- 2 = Items on ground
- 3 = Mobiles
- 4 = Effects

### SpriteBatch Layer Depth

For `SpriteSortMode.BackToFront`:

```csharp
depth = 1.0f - (priority / 10000000f);
```

Lower depth values render on top.

## File Formats

### Map Block (196 bytes)

```
Offset  Size  Description
0       4     Header (usually 0x00000000)
4       192   64 tiles × 3 bytes each
              - ushort TileId (2 bytes)
              - sbyte Z (1 byte)
```

### TileData Land Entry

**Old format (26 bytes):**
```
uint Flags        (4 bytes)
ushort TextureId  (2 bytes)  ← Maps to texmaps.mul
char[20] Name     (20 bytes)
```

**New format (30 bytes, High Seas+):**
```
ulong Flags       (8 bytes)
ushort TextureId  (2 bytes)
char[20] Name     (20 bytes)
```

### Texmaps Entry

Index file (texidx.mul): 12 bytes per entry
```
uint Offset  (4 bytes)
uint Length  (4 bytes)
uint Extra   (4 bytes)  ← 0 = 64×64, non-zero = 128×128
```

Data file (texmaps.mul): Raw ARGB1555 pixels

### ARGB1555 Color Format

```
Bit 15    = Alpha (1 = opaque, 0 = transparent)
Bits 14-10 = Red (5 bits, 0-31)
Bits 9-5   = Green (5 bits, 0-31)
Bits 4-0   = Blue (5 bits, 0-31)

Color 0x0000 is always transparent regardless of alpha bit.
```

Conversion:
```csharp
uint ConvertARGB1555(ushort color)
{
    if (color == 0) return 0;  // Transparent
    
    int r = ((color >> 10) & 0x1F) * 255 / 31;
    int g = ((color >> 5) & 0x1F) * 255 / 31;
    int b = (color & 0x1F) * 255 / 31;
    
    // MonoGame format: 0xAABBGGRR
    return 0xFF000000 | (b << 16) | (g << 8) | r;
}
```

## Camera Transform

```csharp
Matrix GetTransformMatrix()
{
    return Matrix.CreateTranslation(-Position.X, -Position.Y, 0) *
           Matrix.CreateScale(Zoom, Zoom, 1) *
           Matrix.CreateTranslation(ViewportWidth / 2, ViewportHeight / 2, 0);
}
```

## Render Loop Order

1. Clear screen
2. Set up terrain matrices (orthographic projection)
3. Collect visible terrain quads
4. Sort quads by priority
5. Render terrain quads with BasicEffect
6. Begin SpriteBatch (BackToFront, camera transform)
7. Render statics
8. Render items
9. Render mobiles
10. Render effects
11. End SpriteBatch
12. Render UI (separate SpriteBatch, no transform)

## Performance Tips

1. **Cull early**: Only process tiles within visible range
2. **Cache textures**: Use LRU cache for loaded textures
3. **Batch similar**: Sort by texture to minimize state changes
4. **Reuse buffers**: Pre-allocate vertex arrays
5. **Block caching**: Keep recently accessed map blocks in memory

## Debugging Tips

- Log TileData.TextureId values to verify mapping
- Check that texmaps.mul and texidx.mul exist
- Verify block indexing is column-major
- Ensure UV coordinates are 0-1 range (not pixel coordinates)
- Test with known locations (Britain = ~1500, 1600)
