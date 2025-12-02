namespace RealmOfReality.Shared.Core;

/// <summary>
/// Core isometric mathematics for Ultima Online-style 2:1 dimetric projection.
/// 
/// UO Coordinate System:
/// - World coordinates: (X, Y, Z) where X/Y are tile positions, Z is altitude
/// - Screen coordinates: (screenX, screenY) in pixels
/// - Tiles are 44×44 pixel diamonds with 22-pixel spacing
/// - Z altitude: 4 pixels per unit (Z * 4 = vertical pixel offset)
/// 
/// Isometric Formulas (ClassicUO-compatible):
///   screenX = (worldX - worldY) * 22
///   screenY = (worldX + worldY) * 22 - worldZ * 4
///   
/// Inverse (screen to world at Z=0):
///   worldX = (screenX + screenY) / 44
///   worldY = (screenY - screenX) / 44
/// </summary>
public static class IsometricMath
{
    // ═══════════════════════════════════════════════════════════════════
    // CONSTANTS - Matches ClassicUO exactly
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>Full width/height of a land tile diamond (44 pixels)</summary>
    public const int TILE_SIZE = 44;
    
    /// <summary>Half tile size - the step between adjacent tiles (22 pixels)</summary>
    public const int TILE_STEP = 22;
    
    /// <summary>Pixels per Z altitude unit (4 pixels)</summary>
    public const int Z_SCALE = 4;
    
    /// <summary>Maximum valid Z altitude in UO</summary>
    public const int MAX_Z = 127;
    
    /// <summary>Minimum valid Z altitude in UO</summary>
    public const int MIN_Z = -128;
    
    // ═══════════════════════════════════════════════════════════════════
    // WORLD TO SCREEN CONVERSION
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Convert world tile coordinates to screen pixel coordinates.
    /// This is the fundamental isometric projection used throughout rendering.
    /// </summary>
    /// <param name="tileX">World X coordinate (tile units)</param>
    /// <param name="tileY">World Y coordinate (tile units)</param>
    /// <param name="tileZ">World Z altitude (height units)</param>
    /// <returns>Screen position in pixels (before camera transform)</returns>
    public static (int screenX, int screenY) WorldToScreen(int tileX, int tileY, int tileZ)
    {
        int screenX = (tileX - tileY) * TILE_STEP;
        int screenY = (tileX + tileY) * TILE_STEP - tileZ * Z_SCALE;
        return (screenX, screenY);
    }
    
    /// <summary>
    /// Convert world coordinates to screen with float precision.
    /// Used for smooth entity movement interpolation.
    /// </summary>
    public static (float screenX, float screenY) WorldToScreenF(float worldX, float worldY, float worldZ)
    {
        float screenX = (worldX - worldY) * TILE_STEP;
        float screenY = (worldX + worldY) * TILE_STEP - worldZ * Z_SCALE;
        return (screenX, screenY);
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // SCREEN TO WORLD CONVERSION
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Convert screen coordinates back to world coordinates.
    /// Note: This assumes Z=0; for accurate picking, you need to account for terrain height.
    /// </summary>
    /// <param name="screenX">Screen X in pixels</param>
    /// <param name="screenY">Screen Y in pixels</param>
    /// <param name="assumedZ">Z altitude to assume (affects Y calculation)</param>
    /// <returns>World tile coordinates</returns>
    public static (float worldX, float worldY) ScreenToWorld(int screenX, int screenY, int assumedZ = 0)
    {
        // Adjust screen Y for assumed Z height
        int adjustedScreenY = screenY + assumedZ * Z_SCALE;
        
        // Inverse of the isometric formulas
        float worldX = (screenX + adjustedScreenY) / (2.0f * TILE_STEP);
        float worldY = (adjustedScreenY - screenX) / (2.0f * TILE_STEP);
        
        return (worldX, worldY);
    }
    
    /// <summary>
    /// Convert screen coordinates to integer tile coordinates.
    /// </summary>
    public static (int tileX, int tileY) ScreenToTile(int screenX, int screenY, int assumedZ = 0)
    {
        var (worldX, worldY) = ScreenToWorld(screenX, screenY, assumedZ);
        return ((int)MathF.Floor(worldX), (int)MathF.Floor(worldY));
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // TERRAIN VERTEX CALCULATION
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Calculate the screen position of a terrain vertex (corner of a quad).
    /// Used for 3D terrain rendering with per-corner heights.
    /// </summary>
    /// <param name="cornerX">Corner world X (can be tileX or tileX+1)</param>
    /// <param name="cornerY">Corner world Y (can be tileY or tileY+1)</param>
    /// <param name="cornerZ">Z altitude at this corner</param>
    public static (int screenX, int screenY) GetTerrainVertex(int cornerX, int cornerY, int cornerZ)
    {
        return WorldToScreen(cornerX, cornerY, cornerZ);
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // DEPTH SORTING
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Calculate render priority for depth sorting (painter's algorithm).
    /// Higher values render later (on top of lower values).
    /// 
    /// ClassicUO formula: priority = (X + Y) * SCALE + Z + typeBonus
    /// </summary>
    /// <param name="tileX">World X coordinate</param>
    /// <param name="tileY">World Y coordinate</param>
    /// <param name="tileZ">World Z altitude</param>
    /// <param name="typeBonus">Additional priority for object type (0=land, 1=static, 2=item, 3=mobile)</param>
    /// <returns>Render priority value</returns>
    public static int CalculateRenderPriority(int tileX, int tileY, int tileZ, int typeBonus = 0)
    {
        // Primary: isometric depth (farther X+Y = rendered later)
        // Secondary: altitude (higher Z = rendered later)
        // Tertiary: object type (entities on top of terrain)
        return (tileX + tileY) * 256 + (tileZ + 128) + typeBonus;
    }
    
    /// <summary>
    /// Calculate SpriteBatch layer depth (0.0 = front, 1.0 = back).
    /// Used with SpriteSortMode.BackToFront.
    /// </summary>
    public static float CalculateLayerDepth(int tileX, int tileY, int tileZ, int typeBonus = 0)
    {
        // Invert for SpriteBatch (lower depth = rendered on top)
        int priority = CalculateRenderPriority(tileX, tileY, tileZ, typeBonus);
        
        // Normalize to 0-1 range (assuming max world size ~16384)
        return 1.0f - (priority / 10000000.0f);
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // VISIBILITY CULLING
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Check if a tile is potentially visible on screen.
    /// Used for culling tiles outside the viewport.
    /// </summary>
    /// <param name="tileX">Tile world X</param>
    /// <param name="tileY">Tile world Y</param>
    /// <param name="tileZ">Tile Z altitude</param>
    /// <param name="cameraX">Camera center screen X</param>
    /// <param name="cameraY">Camera center screen Y</param>
    /// <param name="viewWidth">Viewport width in pixels</param>
    /// <param name="viewHeight">Viewport height in pixels</param>
    /// <param name="margin">Extra margin in pixels for tall objects</param>
    public static bool IsTileVisible(
        int tileX, int tileY, int tileZ,
        int cameraX, int cameraY,
        int viewWidth, int viewHeight,
        int margin = 100)
    {
        var (screenX, screenY) = WorldToScreen(tileX, tileY, tileZ);
        
        // Apply camera offset (camera position is what's at screen center)
        int drawX = screenX - cameraX + viewWidth / 2;
        int drawY = screenY - cameraY + viewHeight / 2;
        
        // Check if within viewport (with margin for tall objects)
        return drawX >= -margin && drawX <= viewWidth + margin &&
               drawY >= -margin && drawY <= viewHeight + margin;
    }
    
    /// <summary>
    /// Get the range of tiles that might be visible in the viewport.
    /// Returns conservative bounds that may include some non-visible tiles.
    /// </summary>
    public static (int minX, int minY, int maxX, int maxY) GetVisibleTileRange(
        int cameraX, int cameraY,
        int viewWidth, int viewHeight,
        int margin = 5)
    {
        // Convert screen corners to world coordinates
        int halfW = viewWidth / 2;
        int halfH = viewHeight / 2;
        
        // Screen corners relative to camera
        var (topX, topY) = ScreenToWorld(-halfW, -halfH, 0);
        var (rightX, rightY) = ScreenToWorld(halfW, 0, 0);
        var (bottomX, bottomY) = ScreenToWorld(halfW, halfH, 0);
        var (leftX, leftY) = ScreenToWorld(-halfW, 0, 0);
        
        // Find bounds with margin
        int minX = (int)MathF.Floor(MathF.Min(MathF.Min(topX, leftX), MathF.Min(rightX, bottomX))) - margin;
        int maxX = (int)MathF.Ceiling(MathF.Max(MathF.Max(topX, leftX), MathF.Max(rightX, bottomX))) + margin;
        int minY = (int)MathF.Floor(MathF.Min(MathF.Min(topY, leftY), MathF.Min(rightY, bottomY))) - margin;
        int maxY = (int)MathF.Ceiling(MathF.Max(MathF.Max(topY, leftY), MathF.Max(rightY, bottomY))) + margin;
        
        return (minX, minY, maxX, maxY);
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // POINT-IN-TILE TESTING
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Check if a screen point is inside a tile's diamond shape.
    /// Used for mouse picking and hit testing.
    /// </summary>
    /// <param name="pointX">Screen X to test</param>
    /// <param name="pointY">Screen Y to test</param>
    /// <param name="tileCenterX">Tile's screen center X</param>
    /// <param name="tileCenterY">Tile's screen center Y</param>
    public static bool IsPointInTileDiamond(int pointX, int pointY, int tileCenterX, int tileCenterY)
    {
        // Diamond test using Manhattan distance
        int dx = Math.Abs(pointX - tileCenterX);
        int dy = Math.Abs(pointY - tileCenterY);
        
        // Point is inside if: dx/halfWidth + dy/halfHeight <= 1
        return (dx / (float)TILE_STEP + dy / (float)TILE_STEP) <= 1.0f;
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // DIRECTION UTILITIES
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Get the offset for moving one tile in a direction.
    /// UO uses 8 directions: N=0, NE=1, E=2, SE=3, S=4, SW=5, W=6, NW=7
    /// </summary>
    public static (int dx, int dy) GetDirectionOffset(int direction)
    {
        return (direction & 7) switch
        {
            0 => (0, -1),   // North
            1 => (1, -1),   // Northeast
            2 => (1, 0),    // East
            3 => (1, 1),    // Southeast
            4 => (0, 1),    // South
            5 => (-1, 1),   // Southwest
            6 => (-1, 0),   // West
            7 => (-1, -1),  // Northwest
            _ => (0, 0)
        };
    }
    
    /// <summary>
    /// Get the direction from one tile to another.
    /// </summary>
    public static int GetDirection(int fromX, int fromY, int toX, int toY)
    {
        int dx = Math.Sign(toX - fromX);
        int dy = Math.Sign(toY - fromY);
        
        return (dx, dy) switch
        {
            (0, -1) => 0,   // North
            (1, -1) => 1,   // Northeast
            (1, 0) => 2,    // East
            (1, 1) => 3,    // Southeast
            (0, 1) => 4,    // South
            (-1, 1) => 5,   // Southwest
            (-1, 0) => 6,   // West
            (-1, -1) => 7,  // Northwest
            _ => 0
        };
    }
}
