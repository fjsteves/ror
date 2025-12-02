# Ultima Online File Format Reference

This document details the binary formats of UO data files used by Realm of Reality.

## Table of Contents

- [Map Files](#map-files)
- [TileData](#tiledata)
- [Texmaps](#texmaps)
- [Art](#art)
- [Statics](#statics)
- [Animations](#animations)
- [Gumps](#gumps)
- [Hues](#hues)
- [UOP Format](#uop-format)

---

## Map Files

### map*.mul

Contains terrain data in 8×8 tile blocks.

**Block Structure (196 bytes):**
```
Offset  Size  Type     Description
0       4     uint32   Header (usually 0x00000000)
4       192   Cell[64] 64 land tiles (8×8 grid)
```

**Cell Structure (3 bytes):**
```
Offset  Size  Type    Description
0       2     uint16  TileId (land graphic 0-16383)
2       1     int8    Z altitude (-128 to +127)
```

**Block Indexing (COLUMN-MAJOR):**
```csharp
blockIndex = blockX * blockHeight + blockY
```

**Cell Indexing within Block (ROW-MAJOR):**
```csharp
cellIndex = cellY * 8 + cellX
```

**Map Dimensions:**
| Facet | Width (tiles) | Height (tiles) | Blocks |
|-------|--------------|----------------|--------|
| Felucca (0) | 7168 | 4096 | 896 × 512 |
| Trammel (1) | 7168 | 4096 | 896 × 512 |
| Ilshenar (2) | 2304 | 1600 | 288 × 200 |
| Malas (3) | 2560 | 2048 | 320 × 256 |
| Tokuno (4) | 1448 | 1448 | 181 × 181 |
| Ter Mur (5) | 1280 | 4096 | 160 × 512 |

---

## TileData

### tiledata.mul

Contains metadata for all land and static tiles.

**Format Detection:**
```csharp
bool isNewFormat = fileSize > 3188736;
```

**File Structure:**
```
- 512 Land Tile Groups
  - Each: 4-byte header + 32 land tiles
- N Static Tile Groups
  - Each: 4-byte header + 32 static tiles
```

**Land Tile (Old Format - 26 bytes):**
```
Offset  Size  Type      Description
0       4     uint32    Flags (TileFlag enum)
4       2     uint16    TextureId (texmaps.mul index)
6       20    char[20]  Name (null-terminated)
```

**Land Tile (New Format - 30 bytes):**
```
Offset  Size  Type      Description
0       8     uint64    Flags (extended TileFlag)
8       2     uint16    TextureId
10      20    char[20]  Name
```

**Static Tile (Old Format - 37 bytes):**
```
Offset  Size  Type      Description
0       4     uint32    Flags
4       1     uint8     Weight
5       1     uint8     Layer (equipment slot)
6       4     int32     Count (stack size)
10      2     uint16    AnimId
12      2     uint16    Hue
14      2     uint16    LightIndex
16      1     uint8     Height (Z stacking)
17      20    char[20]  Name
```

**Static Tile (New Format - 41 bytes):**
```
Same as old but Flags is 8 bytes instead of 4.
```

**Important TileFlags:**
```csharp
Background  = 0x00000001  // Draw behind other objects
Impassable  = 0x00000040  // Cannot walk through
Wet         = 0x00000080  // Is water
Surface     = 0x00000200  // Can place items on
Foliage     = 0x00020000  // Is foliage/grass
Roof        = 0x10000000  // Is roof tile
```

---

## Texmaps

### texidx.mul

Index file with 12-byte entries pointing to texmaps.mul.

**Index Entry (12 bytes):**
```
Offset  Size  Type    Description
0       4     uint32  Offset in texmaps.mul (0xFFFFFFFF = invalid)
4       4     uint32  Length in bytes
8       4     uint32  Extra (0 = 64×64, non-zero = 128×128)
```

### texmaps.mul

Raw ARGB1555 pixel data.

**Texture Sizes:**
- 64×64: 8,192 bytes (64 × 64 × 2)
- 128×128: 32,768 bytes (128 × 128 × 2)

**Size Detection:**
```csharp
int size = (extra == 0 && length == 8192) ? 64 : 128;
```

**CRITICAL: The index used for texmaps lookup is `TileData.TextureId`, NOT the land tile ID!**

---

## Art

### artidx.mul

Index file with 12-byte entries.

### art.mul

Contains land tiles (0-16383) and static items (16384+).

**Land Tile (44×44 diamond):**
- Raw ARGB1555 pixels in diamond pattern
- Variable row widths: 2, 4, 6, ..., 44, ..., 6, 4, 2
- Total pixels: 968 (≈1936 bytes)

**Row Widths:**
```
Row 0:  2 pixels   (center 2)
Row 1:  4 pixels   (center 4)
...
Row 21: 44 pixels  (full width)
Row 22: 42 pixels
...
Row 43: 2 pixels
```

**Static Item (Variable size, RLE encoded):**
```
Offset  Size        Type      Description
0       4           uint32    Flags (unused)
4       2           uint16    Width
6       2           uint16    Height
8       height×2    uint16[]  Row lookup table
...     variable    RLE data  Per-row pixel data
```

**RLE Run Format:**
```
Offset  Size  Type    Description
0       2     uint16  X offset from last run
2       2     uint16  Run length (0,0 = end of row)
4       N×2   ARGB1555 Pixel data (N = run length)
```

---

## Statics

### staidx*.mul

Index file with 12-byte entries per block.

**Index Entry:**
```
Offset  Size  Type    Description
0       4     uint32  Offset in statics*.mul
4       4     uint32  Length in bytes
8       4     uint32  Extra (unused)
```

### statics*.mul

Static objects per block.

**Static Tile (7 bytes):**
```
Offset  Size  Type    Description
0       2     uint16  ItemId (static graphic ID)
2       1     uint8   X within block (0-7)
3       1     uint8   Y within block (0-7)
4       1     int8    Z altitude
5       2     int16   Hue (0 = default)
```

---

## Animations

### anim.mul + anim.idx

Character and creature animations.

**Body Types:**
- 0-199: Monsters
- 200-399: Animals
- 400+: Humans

**Actions per Body Type:**
| Type | Walk | Run | Stand | Attack | Death | Total |
|------|------|-----|-------|--------|-------|-------|
| Monster | 4 | - | 1 | 1 | 2 | 13 |
| Animal | 4 | 4 | 1 | 1 | 2 | 22 |
| Human | 4 | 4 | 1 | 6 | 2 | 35 |

**Stored Directions:**
Only 5 directions stored: S, SW, W, NW, N

**Mirrored Directions:**
- NE → mirrors NW
- E → mirrors W
- SE → mirrors SW

---

## Gumps

### gumpidx.mul + gumparts.mul

UI graphics.

**Index Entry (12 bytes):**
```
Offset  Size  Type    Description
0       4     uint32  Offset
4       4     uint32  Length
8       4     uint32  Extra (width << 16 | height)
```

**Gump Data:**
Run-length encoded similar to static items, but simpler format.

---

## Hues

### hues.mul

Color palettes for item/character tinting.

**Hue Entry (88 bytes):**
```
Offset  Size  Type        Description
0       64    uint16[32]  Color table (32 colors)
64      4     uint16      Table start
68      4     uint16      Table end
72      20    char[20]    Hue name
```

---

## UOP Format

Modern packed format used by newer clients.

### Header (24 bytes)
```
Offset  Size  Type    Description
0       4     uint32  Magic ("MYP\0" = 0x50594D)
4       4     uint32  Version
8       4     uint32  Misc signature (0xFD23EC43)
12      8     int64   First table offset
20      4     uint32  Table capacity
24      4     uint32  File count
```

### File Table Entry (34 bytes)
```
Offset  Size  Type    Description
0       8     int64   Data offset
8       4     int32   Header length
12      4     int32   Compressed length
16      4     int32   Decompressed length
20      8     uint64  Filename hash
28      4     uint32  Checksum
32      2     int16   Flags (1 = compressed)
```

### Hash Function (Jenkins/Adler32 variant)
```csharp
// Pattern format: "build/artlegacymul/{0:D8}.tga"
// Index 0 → "build/artlegacymul/00000000.tga"
ulong hash = HashFileName(string.Format(pattern, index));
```

### Decompression
- Uses zlib deflate
- Skip first 2 bytes (zlib header) before decompressing

---

## ARGB1555 Color Format

16-bit color used throughout UO files.

```
Bit Layout: ARRRRRGGGGGBBBBB
- Bit 15: Alpha (1=opaque, 0=transparent)
- Bits 14-10: Red (5 bits, 0-31)
- Bits 9-5: Green (5 bits, 0-31)
- Bits 4-0: Blue (5 bits, 0-31)

Special: Color value 0x0000 is always transparent.
```

**Conversion to 32-bit RGBA:**
```csharp
uint ConvertARGB1555(ushort color)
{
    if (color == 0) return 0;  // Transparent
    
    int r = ((color >> 10) & 0x1F) * 255 / 31;
    int g = ((color >> 5) & 0x1F) * 255 / 31;
    int b = (color & 0x1F) * 255 / 31;
    
    // MonoGame format: AABBGGRR (little-endian)
    return 0xFF000000 | ((uint)b << 16) | ((uint)g << 8) | (uint)r;
}
```

---

## References

- [ClassicUO](https://github.com/ClassicUO/ClassicUO) - Primary implementation reference
- [Ultima SDK](https://github.com/Ultima-Lokan/) - Format documentation
- [POL Server](https://github.com/polserver/polern) - Server-side reference
