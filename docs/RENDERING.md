# Rendering System Deep Dive

This document provides exhaustive technical details about the rendering pipeline in Realm of Reality.

## Table of Contents

1. [Rendering Overview](#rendering-overview)
2. [Terrain Quad System](#terrain-quad-system)
3. [Texture Loading Pipeline](#texture-loading-pipeline)
4. [Depth Sorting Algorithm](#depth-sorting-algorithm)
5. [Entity Rendering](#entity-rendering)
6. [Common Bugs and Fixes](#common-bugs-and-fixes)

---

## Rendering Overview

### Frame Render Order

```
1. Clear background
2. Render terrain (3D quads via BasicEffect)
3. Begin SpriteBatch (with camera transform)
4. Render statics (sorted)
5. Render entities (sorted)
6. Render effects
7. End SpriteBatch
8. Render UI (separate SpriteBatch, no transform)
```

### Why Terrain Uses 3D Quads (Not SpriteBatch)

SpriteBatch is optimized for axis-aligned, same-Z sprites. Terrain requires:
- Per-vertex Z heights (slopes)
- Perspective-correct texture mapping
- Proper occlusion on hills

```
SpriteBatch Terrain (WRONG):        3D Quad Terrain (CORRECT):
                                   
┌─────────────┐                    ┌─────────────┐
│ ████████████│ All same Z         │╲   ██████  │ Varied Z
│ ████████████│ Flat appearance    │ ╲  ██████  │ Natural slopes
│ ████████████│ Z-fighting         │  ╲ ██████  │ Correct depth
└─────────────┘                    └────────────┘
```

---

## Terrain Quad System

### Quad Structure

Each terrain cell is defined by 4 corner tiles:

```
World Layout:                    Screen Layout:

(x,y)───────(x+1,y)                    NW ◆
  │           │                       ╱    ╲
  │   Cell    │        →         SW ◆        ◆ NE
  │           │                       ╲    ╱
(x,y+1)─────(x+1,y+1)                   ◆ SE

Corner assignments:
- NW (Northwest/Top):    GetLandTile(x, y)
- NE (Northeast/Right):  GetLandTile(x+1, y)
- SW (Southwest/Left):   GetLandTile(x, y+1)
- SE (Southeast/Bottom): GetLandTile(x+1, y+1)
```

### Vertex Construction

```csharp
// Calculate screen positions for each corner
var screenNW = WorldToScreen(x, y, nw.Z);
var screenNE = WorldToScreen(x + 1, y, ne.Z);
var screenSW = WorldToScreen(x, y + 1, sw.Z);
var screenSE = WorldToScreen(x + 1, y + 1, se.Z);

// Apply camera transform
var transform = _camera.GetTransformMatrix();
var drawNW = Vector2.Transform(screenNW, transform);
var drawNE = Vector2.Transform(screenNE, transform);
var drawSW = Vector2.Transform(screenSW, transform);
var drawSE = Vector2.Transform(screenSE, transform);

// Build vertices with UV coordinates
// UV is a standard quad mapping (NOT diamond)
_quadVertices[0] = new VertexPositionTexture(
    new Vector3(drawNW, 0), new Vector2(0, 0));  // NW → UV(0,0)
_quadVertices[1] = new VertexPositionTexture(
    new Vector3(drawNE, 0), new Vector2(1, 0));  // NE → UV(1,0)
_quadVertices[2] = new VertexPositionTexture(
    new Vector3(drawSW, 0), new Vector2(0, 1));  // SW → UV(0,1)
_quadVertices[3] = new VertexPositionTexture(
    new Vector3(drawSE, 0), new Vector2(1, 1));  // SE → UV(1,1)
```

### Index Buffer (Triangle Winding)

```csharp
// Two triangles form the quad:
// Triangle 1: NW → SW → NE (indices 0, 2, 1)
// Triangle 2: NE → SW → SE (indices 1, 2, 3)
short[] indices = { 0, 2, 1, 1, 2, 3 };
```

Visual:
```
    0 (NW) ────── 1 (NE)
       │╲          │
       │  ╲        │
       │    ╲      │
       │      ╲    │
       │        ╲  │
    2 (SW) ────── 3 (SE)

Triangle 1: 0-2-1 (NW-SW-NE)
Triangle 2: 1-2-3 (NE-SW-SE)
```

### Render Range and Loop Bounds

```csharp
private const int RENDER_RANGE = 24;  // Tiles from center

// CRITICAL: Loop bounds must be Width-1 and Height-1
// because we access (x+1, y+1) for the SE corner
for (int dy = -RENDER_RANGE; dy <= RENDER_RANGE; dy++)
{
    for (int dx = -RENDER_RANGE; dx <= RENDER_RANGE; dx++)
    {
        int x = centerX + dx;
        int y = centerY + dy;
        
        // CORRECT: < Width-1, not <= Width-1 or < Width
        if (x < 0 || y < 0 || x >= _map.Width - 1 || y >= _map.Height - 1)
            continue;
        
        // Safe to access (x+1, y+1) now
    }
}
```

---

## Texture Loading Pipeline

### The TextureId Lookup Chain

```
┌─────────────────────────────────────────────────────────────────┐
│                    TEXTURE SELECTION PIPELINE                    │
└─────────────────────────────────────────────────────────────────┘

Step 1: Get corner tile IDs
┌─────────────────────────────────────────────────────────────────┐
│  NW.TileId = 3     NE.TileId = 3                                │
│  SW.TileId = 3     SE.TileId = 168                              │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
Step 2: Look up TextureId from TileData (pick highest Z corner)
┌─────────────────────────────────────────────────────────────────┐
│  foreach corner:                                                │
│    if corner.TileId == 0: skip                                  │
│    textureId = TileData.GetLandTile(corner.TileId).TextureId    │
│    if textureId > 0 && corner.Z >= bestZ:                       │
│      bestTexId = textureId                                      │
│      bestZ = corner.Z                                           │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
Step 3: Load texture from Texmaps
┌─────────────────────────────────────────────────────────────────┐
│  if bestTexId > 0:                                              │
│    texture = Texmaps.GetTexmap(bestTexId)                       │
│    if texture != null: return texture                           │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
Step 4: Fallback to Art tile
┌─────────────────────────────────────────────────────────────────┐
│  artId = first non-zero TileId from corners                     │
│  art = Art.GetLandTile(artId)                                   │
│  if art?.IsValid: return art.Texture                            │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
Step 5: Final fallback
┌─────────────────────────────────────────────────────────────────┐
│  return _fallbackTexture  (64×64 green noise)                   │
└─────────────────────────────────────────────────────────────────┘
```

### CRITICAL: TileId vs TextureId

```
╔═══════════════════════════════════════════════════════════════════╗
║                        CRITICAL WARNING                           ║
╠═══════════════════════════════════════════════════════════════════╣
║                                                                   ║
║   NEVER pass TileId directly to TexmapLoader.GetTexmap()!         ║
║                                                                   ║
║   ✗ WRONG:  Texmaps.GetTexmap(landTile.TileId)                   ║
║   ✓ RIGHT:  Texmaps.GetTexmap(TileData.GetLandTile(tileId)       ║
║                                           .TextureId)             ║
║                                                                   ║
║   TileId = Art index (0-16383)                                   ║
║   TextureId = Texmap index (from tiledata.mul)                   ║
║                                                                   ║
║   They are DIFFERENT values for the same tile!                   ║
║                                                                   ║
╚═══════════════════════════════════════════════════════════════════╝
```

### Example Values

```
Land Tile "grass" (common):
  TileId = 3
  TileData.GetLandTile(3):
    TextureId = 32
    Flags = Surface
    Name = "grass"
  
  Texmap lookup: Texmaps.GetTexmap(32) → 128×128 grass texture

Land Tile "water" (special):
  TileId = 168
  TileData.GetLandTile(168):
    TextureId = 0    ← Note: zero!
    Flags = Wet
    Name = "water"
  
  TextureId=0 → Use Art.GetLandTile(168) instead (44×44 diamond)
```

---

## Depth Sorting Algorithm

### Priority Calculation

```csharp
// Base priority from isometric position
int priority = (tileX + tileY) * 256;

// Add Z component (shifted to prevent negatives)
priority += (tileZ + 128);

// Add layer offset to prevent Z-fighting
priority += layerOffset;

// Layer offsets:
const int LAND_OFFSET = 0;
const int STATIC_OFFSET = 1;
const int ITEM_OFFSET = 2;
const int MOBILE_OFFSET = 3;
const int EFFECT_OFFSET = 4;
```

### Visual Explanation

```
Isometric depth ordering (back to front):

          ┌───┐ Priority: 0
         ╱   ╲
        ┌─────┐ Priority: 256
       ╱       ╲
      ┌─────────┐ Priority: 512
     ╱           ╲
    ┌─────────────┐ Priority: 768
   ╱               ╲
  ┌─────────────────┐ Priority: 1024 (front)

Objects further from camera (northwest) have lower priority.
Objects closer to camera (southeast) have higher priority.
Render order: Sort by priority ascending (back to front).
```

### SpriteBatch Depth

SpriteBatch uses inverted depth (0 = front, 1 = back):

```csharp
// Convert priority to SpriteBatch depth
float depth = 1.0f - (priority / 10000000f);

// Example:
// Priority 500 → depth 0.99995 (back)
// Priority 5000 → depth 0.9995 (middle)
// Priority 50000 → depth 0.995 (front)
```

---

## Entity Rendering

### Mobile Draw Position

```csharp
// Get terrain height at entity position
int terrainZ = _map.GetLandTile(entityX, entityY).Z;

// Total Z = terrain + entity offset
int totalZ = terrainZ + entityZ;

// Convert to screen
Vector2 screenPos = WorldToScreen(entityX, entityY, totalZ);

// Sprite anchor: bottom-center
// Draw position = screen position, offset by sprite dimensions
Vector2 drawPos = new Vector2(
    screenPos.X - spriteWidth / 2,    // Center horizontally
    screenPos.Y + TILE_STEP - spriteHeight  // Bottom at tile center
);
```

### Animation Frame Selection

```csharp
// Get body type
int bodyId = mobile is PlayerEntity player 
    ? player.BodyType 
    : mobile.TypeId;

// Get action
AnimAction action = mobile.IsMoving 
    ? (mobile.IsRunning ? AnimAction.Run : AnimAction.Walk)
    : AnimAction.Stand;

// Get direction (UO uses 5 stored directions, mirror others)
AnimDirection direction = (AnimDirection)((int)mobile.Facing % 8);

// Get animation
var anim = _animations.GetAnimation(bodyId, action, direction);

// Get current frame
int frameIndex = animState.FrameIndex % anim.Frames.Length;
var frame = anim.Frames[frameIndex];
```

### Direction Mirroring

```csharp
// UO stores only 5 directions (S, SW, W, NW, N)
// Other 3 are mirrored versions

Direction visual → Direction stored, SpriteEffects
───────────────────────────────────────────────────
North     → North,     None
Northeast → Northwest, FlipHorizontally
East      → West,      FlipHorizontally
Southeast → Southwest, FlipHorizontally
South     → South,     None
Southwest → Southwest, None
West      → West,      None
Northwest → Northwest, None
```

---

## Common Bugs and Fixes

### Bug 1: Black/Missing Terrain

**Symptom:** Large black areas where terrain should be.

**Causes and Fixes:**

1. **TileData not loaded**
   ```csharp
   // Check in WorldRenderer.Render():
   if (_uoAssets?.TileData?.IsLoaded != true)
       Console.WriteLine("WARNING: TileData not loaded!");
   ```

2. **Wrong TextureId lookup**
   ```csharp
   // WRONG:
   var tex = Texmaps.GetTexmap(landTile.TileId);
   
   // CORRECT:
   var textureId = TileData.GetLandTile(landTile.TileId).TextureId;
   var tex = Texmaps.GetTexmap(textureId);
   ```

3. **All corners are void**
   ```csharp
   // Skip void quads but log them
   if (nw.TileId == 0 && ne.TileId == 0 && sw.TileId == 0 && se.TileId == 0)
   {
       voidCount++;
       continue;
   }
   ```

### Bug 2: Terrain Gaps/Seams

**Symptom:** Thin lines between terrain tiles.

**Causes and Fixes:**

1. **Off-by-one in loop bounds**
   ```csharp
   // WRONG: Goes too far, crashes or wraps
   if (x >= _map.Width || y >= _map.Height)
   
   // CORRECT: Leave room for +1 access
   if (x >= _map.Width - 1 || y >= _map.Height - 1)
   ```

2. **Floating point precision**
   ```csharp
   // Use integers for position calculations
   int screenX = (tileX - tileY) * TILE_STEP;  // Not float
   ```

### Bug 3: Z-Fighting (Flickering)

**Symptom:** Objects flicker when overlapping.

**Causes and Fixes:**

1. **Same priority for different layers**
   ```csharp
   // Add layer offsets
   int landPriority = (x + y) * 256 + z + 0;    // +0 for land
   int staticPriority = (x + y) * 256 + z + 1;  // +1 for statics
   int mobilePriority = (x + y) * 256 + z + 3;  // +3 for mobiles
   ```

2. **Not sorting terrain quads**
   ```csharp
   // Must sort quads before rendering
   quads.Sort((a, b) => a.Priority.CompareTo(b.Priority));
   ```

### Bug 4: Sprites at Wrong Position

**Symptom:** Entities float above or sink into terrain.

**Causes and Fixes:**

1. **Not accounting for terrain Z**
   ```csharp
   // WRONG: Ignores terrain height
   int z = (int)entity.Position.Z;
   
   // CORRECT: Add terrain height
   int terrainZ = _map.GetLandTile(x, y).Z;
   int z = terrainZ + (int)entity.Position.Z;
   ```

2. **Wrong anchor point**
   ```csharp
   // Static items: bottom-center
   drawY = screenY - spriteHeight + TILE_STEP;
   
   // Land tiles: top-left (already positioned)
   // No offset needed for land tiles
   ```

### Bug 5: Camera Transform Issues

**Symptom:** World scrolls wrong or doesn't follow player.

**Causes and Fixes:**

1. **Double transform**
   ```csharp
   // WRONG: Transform applied twice
   var screen = WorldToScreen(x, y, z);
   var transformed = Vector2.Transform(screen, camera);
   _spriteBatch.Draw(tex, transformed);  // SpriteBatch also transforms!
   
   // CORRECT: Transform in SpriteBatch.Begin() only
   _spriteBatch.Begin(..., camera.GetTransformMatrix());
   _spriteBatch.Draw(tex, screen);  // Raw screen position
   ```

2. **Terrain uses manual transform (correct)**
   ```csharp
   // For BasicEffect, we transform manually
   var transformed = Vector2.Transform(screenPos, camera);
   _quadVertices[i] = new VertexPositionTexture(
       new Vector3(transformed, 0), uv);
   ```

---

## Debug Utilities

### Enable Debug Logging

Call `WorldRenderer.ResetDebugLogging()` to rerun the one-time diagnostics
after loading or reloading maps. The world renderer now clears its missing
asset caches and debug guard when a map loads so you see fresh warnings on
new data.

```csharp
// Add to WorldRenderer.Render():
if (!_debugLogged)
{
    Console.WriteLine($"TileData: {_uoAssets?.TileData?.IsLoaded}");
    Console.WriteLine($"Texmaps: {_uoAssets?.Texmaps?.IsLoaded}");
    Console.WriteLine($"Art: {_uoAssets?.Art?.IsLoaded}");
    Console.WriteLine($"Map: {_map?.IsLoaded}");
    
    // Sample some tiles
    for (int i = 0; i < 5; i++)
    {
        var tile = _map.GetLandTile(centerX + i, centerY);
        var data = _uoAssets.TileData.GetLandTile(tile.TileId);
        Console.WriteLine($"Tile({centerX + i},{centerY}): " +
            $"TileId={tile.TileId}, Z={tile.Z}, TexId={data.TextureId}");
    }
    
    _debugLogged = true;
}
```

### Render Wireframe

```csharp
// Show quad edges for debugging
private void DrawWireframe(TerrainQuad quad)
{
    // Create line primitive batch
    var lineVertices = new VertexPositionColor[]
    {
        new(new Vector3(drawNW, 0), Color.Red),
        new(new Vector3(drawNE, 0), Color.Red),
        new(new Vector3(drawNE, 0), Color.Red),
        new(new Vector3(drawSE, 0), Color.Red),
        new(new Vector3(drawSE, 0), Color.Red),
        new(new Vector3(drawSW, 0), Color.Red),
        new(new Vector3(drawSW, 0), Color.Red),
        new(new Vector3(drawNW, 0), Color.Red),
    };
    
    _graphics.DrawUserPrimitives(PrimitiveType.LineList, lineVertices, 0, 4);
}
```

---

*This document should be updated as the rendering system evolves.*
