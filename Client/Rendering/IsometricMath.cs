// ==========================================================================
// IsometricMath.cs - Isometric coordinate transformation utilities
// ==========================================================================
// Ultima Online uses a dimetric (2:1) isometric projection where:
// - Tiles are 44x44 pixel diamonds
// - Moving +1 in world X: screen moves right 22, down 22
// - Moving +1 in world Y: screen moves left 22, down 22
// - Moving +1 in world Z: screen moves up 4 pixels
// ==========================================================================

using Microsoft.Xna.Framework;

namespace RealmOfReality.Client.Rendering;

/// <summary>
/// Isometric coordinate transformation utilities.
/// All formulas are derived from ClassicUO's implementation.
/// </summary>
public static class IsometricMath
{
    // ========================================================================
    // WORLD TO SCREEN CONVERSION
    // ========================================================================
    
    /// <summary>
    /// Convert world tile coordinates to screen pixel coordinates.
    /// This is the core isometric projection formula.
    /// </summary>
    /// <param name="worldX">World X coordinate (tile units)</param>
    /// <param name="worldY">World Y coordinate (tile units)</param>
    /// <param name="worldZ">World Z coordinate (altitude units)</param>
    /// <returns>Screen position in pixels</returns>
    public static Vector2 WorldToScreen(float worldX, float worldY, float worldZ)
    {
        // UO isometric projection:
        // screenX = (worldX - worldY) * TILE_STEP
        // screenY = (worldX + worldY) * TILE_STEP - worldZ * Z_SCALE
        float screenX = (worldX - worldY) * UOConstants.TILE_STEP;
        float screenY = (worldX + worldY) * UOConstants.TILE_STEP - worldZ * UOConstants.Z_SCALE;
        return new Vector2(screenX, screenY);
    }
    
    /// <summary>
    /// Convert world tile coordinates to screen pixel coordinates.
    /// Overload for integer coordinates.
    /// </summary>
    public static (int X, int Y) WorldToScreen(int worldX, int worldY, int worldZ)
    {
        int screenX = (worldX - worldY) * UOConstants.TILE_STEP;
        int screenY = (worldX + worldY) * UOConstants.TILE_STEP - worldZ * UOConstants.Z_SCALE;
        return (screenX, screenY);
    }
    
    /// <summary>
    /// Convert a Vector3 world position to screen coordinates.
    /// </summary>
    public static Vector2 WorldToScreen(Vector3 world)
    {
        return WorldToScreen(world.X, world.Y, world.Z);
    }
    
    // ========================================================================
    // SCREEN TO WORLD CONVERSION
    // ========================================================================
    
    /// <summary>
    /// Convert screen pixel coordinates to world tile coordinates.
    /// Assumes Z = 0 (ground level).
    /// </summary>
    /// <param name="screenX">Screen X in pixels</param>
    /// <param name="screenY">Screen Y in pixels</param>
    /// <returns>World position (X, Y, Z=0)</returns>
    public static Vector3 ScreenToWorld(float screenX, float screenY)
    {
        return ScreenToWorld(screenX, screenY, 0);
    }
    
    /// <summary>
    /// Convert screen pixel coordinates to world tile coordinates.
    /// Uses assumed Z for altitude compensation.
    /// </summary>
    /// <param name="screenX">Screen X in pixels</param>
    /// <param name="screenY">Screen Y in pixels</param>
    /// <param name="assumedZ">Assumed altitude for accurate conversion</param>
    /// <returns>World position (X, Y, Z)</returns>
    public static Vector3 ScreenToWorld(float screenX, float screenY, float assumedZ)
    {
        // Compensate for Z offset
        screenY += assumedZ * UOConstants.Z_SCALE;
        
        // Reverse isometric projection:
        // From: screenX = (X - Y) * 22, screenY = (X + Y) * 22
        // Solve: X = (screenX + screenY) / 44, Y = (screenY - screenX) / 44
        float worldX = (screenX + screenY) / (2.0f * UOConstants.TILE_STEP);
        float worldY = (screenY - screenX) / (2.0f * UOConstants.TILE_STEP);
        
        return new Vector3(worldX, worldY, assumedZ);
    }
    
    /// <summary>
    /// Convert screen position to tile coordinates (integer).
    /// </summary>
    public static (int X, int Y) ScreenToTile(int screenX, int screenY, int assumedZ = 0)
    {
        var world = ScreenToWorld(screenX, screenY, assumedZ);
        return ((int)MathF.Floor(world.X), (int)MathF.Floor(world.Y));
    }
    
    // ========================================================================
    // DEPTH SORTING
    // ========================================================================
    
    /// <summary>
    /// Calculate render priority for depth sorting (painter's algorithm).
    /// Higher values should be rendered later (on top).
    /// </summary>
    /// <param name="worldX">World X coordinate</param>
    /// <param name="worldY">World Y coordinate</param>
    /// <param name="worldZ">World Z coordinate</param>
    /// <param name="layerOffset">Additional offset for object type (0=land, 1=static, 2=mobile)</param>
    /// <returns>Priority value for sorting</returns>
    public static int CalculateRenderPriority(int worldX, int worldY, int worldZ, int layerOffset = 0)
    {
        // Primary sort: isometric depth (X + Y) - farther tiles have higher values
        // Secondary sort: altitude (Z) - higher tiles render on top
        // Tertiary sort: layer offset - mobiles on top of statics on top of land
        return (worldX + worldY) * 256 + worldZ * 4 + layerOffset;
    }
    
    /// <summary>
    /// Calculate SpriteBatch depth value (0.0 = front, 1.0 = back).
    /// For use with SpriteSortMode.BackToFront.
    /// </summary>
    public static float CalculateSpriteDepth(int worldX, int worldY, int worldZ, int layerOffset = 0)
    {
        // Invert so larger priority = smaller depth (rendered on top)
        int priority = CalculateRenderPriority(worldX, worldY, worldZ, layerOffset);
        
        // Normalize to 0.0-1.0 range
        // Max reasonable priority: (8000 + 8000) * 256 + 200 * 4 + 10 ≈ 4,000,000
        return 1.0f - (priority / 4_000_000.0f);
    }
    
    // ========================================================================
    // TILE BOUNDS CHECKING
    // ========================================================================
    
    /// <summary>
    /// Check if a point is inside a tile's diamond shape.
    /// Used for mouse hit testing.
    /// </summary>
    /// <param name="pointX">Point X in screen coordinates</param>
    /// <param name="pointY">Point Y in screen coordinates</param>
    /// <param name="tileCenterX">Tile center X in screen coordinates</param>
    /// <param name="tileCenterY">Tile center Y in screen coordinates</param>
    /// <returns>True if point is inside the diamond</returns>
    public static bool IsPointInTileDiamond(int pointX, int pointY, int tileCenterX, int tileCenterY)
    {
        // Manhattan distance test for diamond
        int dx = Math.Abs(pointX - tileCenterX);
        int dy = Math.Abs(pointY - tileCenterY);
        
        // Point is inside if: dx/halfWidth + dy/halfHeight <= 1
        // For 44x44 diamond: halfWidth = 22, halfHeight = 22
        return dx + dy <= UOConstants.TILE_STEP;
    }
    
    // ========================================================================
    // CAMERA/VIEWPORT HELPERS
    // ========================================================================
    
    /// <summary>
    /// Calculate the screen position for a camera centered on a world position.
    /// </summary>
    public static Vector2 GetCameraScreenPosition(float worldX, float worldY, float worldZ)
    {
        return WorldToScreen(worldX, worldY, worldZ);
    }
    
    /// <summary>
    /// Get the visible tile range for a viewport.
    /// </summary>
    /// <param name="cameraWorldX">Camera center world X</param>
    /// <param name="cameraWorldY">Camera center world Y</param>
    /// <param name="viewportWidth">Viewport width in pixels</param>
    /// <param name="viewportHeight">Viewport height in pixels</param>
    /// <param name="zoom">Camera zoom level (1.0 = normal)</param>
    /// <returns>Tile range (minX, minY, maxX, maxY)</returns>
    public static (int minX, int minY, int maxX, int maxY) GetVisibleTileRange(
        int cameraWorldX, int cameraWorldY, int viewportWidth, int viewportHeight, float zoom = 1.0f)
    {
        // Calculate how many tiles fit in the viewport
        // Account for isometric diamond shape needing more tiles
        int tilesWide = (int)(viewportWidth / (UOConstants.TILE_STEP * zoom)) + 4;
        int tilesHigh = (int)(viewportHeight / (UOConstants.TILE_STEP * zoom)) + 4;
        
        // The visible range is a diamond shape in world coordinates
        // We approximate with a square for simplicity
        int range = Math.Max(tilesWide, tilesHigh) / 2 + 2;
        
        return (
            cameraWorldX - range,
            cameraWorldY - range,
            cameraWorldX + range,
            cameraWorldY + range
        );
    }
    
    // ========================================================================
    // BLOCK INDEXING (ClassicUO column-major format)
    // ========================================================================
    
    /// <summary>
    /// Calculate block coordinates from world tile coordinates.
    /// </summary>
    public static (int blockX, int blockY) WorldToBlock(int worldX, int worldY)
    {
        return (worldX >> 3, worldY >> 3); // worldX / 8, worldY / 8
    }
    
    /// <summary>
    /// Calculate cell coordinates within a block.
    /// </summary>
    public static (int cellX, int cellY) WorldToCell(int worldX, int worldY)
    {
        return (worldX & 7, worldY & 7); // worldX % 8, worldY % 8
    }
    
    /// <summary>
    /// Calculate block index for file access.
    /// Uses COLUMN-MAJOR ordering per ClassicUO: blockX * blockHeight + blockY
    /// </summary>
    /// <param name="blockX">Block X coordinate</param>
    /// <param name="blockY">Block Y coordinate</param>
    /// <param name="blockHeight">Map height in blocks</param>
    /// <returns>Block index for file seek</returns>
    public static int CalculateBlockIndex(int blockX, int blockY, int blockHeight)
    {
        return blockX * blockHeight + blockY;
    }
    
    /// <summary>
    /// Calculate cell index within a block.
    /// Uses ROW-MAJOR ordering: cellY * 8 + cellX
    /// </summary>
    public static int CalculateCellIndex(int cellX, int cellY)
    {
        return (cellY << 3) + cellX; // cellY * 8 + cellX
    }
}
