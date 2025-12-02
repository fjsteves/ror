namespace RealmOfReality.Shared.Core;

/// <summary>
/// Isometric coordinate utilities for 2:1 dimetric projection (Ultima Online style).
/// 
/// Coordinate System:
/// - World coordinates: (X, Y, Z) in tile/altitude units
/// - Screen coordinates: (X, Y) in pixels
/// 
/// Core Formulas (ClassicUO-compatible):
///   screenX = (tileX - tileY) * 22
///   screenY = (tileX + tileY) * 22 - tileZ * 4
/// 
/// Tile Dimensions:
/// - Land tiles are 44×44 pixel diamonds
/// - Adjacent tiles are offset by 22 pixels in each direction
/// - Each Z-unit of altitude raises objects by 4 pixels
/// 
/// This is the Shared library version; Client uses Client.Rendering.IsometricMath
/// for MonoGame-specific types (Vector2, Matrix, etc.)
/// </summary>
public static class IsometricHelper
{
    // ═══════════════════════════════════════════════════════════════════
    // CONSTANTS - Matches ClassicUO exactly
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>Full width/height of a land tile diamond (44 pixels)</summary>
    public const int TileWidth = 44;
    
    /// <summary>Full width/height of a land tile diamond (44 pixels)</summary>
    public const int TileHeight = 44;
    
    /// <summary>Horizontal distance between adjacent tiles (22 pixels)</summary>
    public const int TileStepX = 22;
    
    /// <summary>Vertical distance between adjacent tiles (22 pixels)</summary>
    public const int TileStepY = 22;
    
    /// <summary>Pixels per Z-level altitude (4 pixels)</summary>
    public const int TileElevationStep = 4;
    
    /// <summary>
    /// Convert world coordinates to isometric screen coordinates
    /// </summary>
    public static ScreenPosition WorldToScreen(WorldPosition world, ScreenPosition cameraOffset)
    {
        // UO isometric projection:
        // Moving +1 in X: screen moves right 22, down 22
        // Moving +1 in Y: screen moves left 22, down 22
        var screenX = (int)((world.X - world.Y) * TileStepX);
        var screenY = (int)((world.X + world.Y) * TileStepY);
        
        // Apply Z elevation (objects at higher Z appear higher on screen)
        screenY -= (int)(world.Z * TileElevationStep);
        
        // Apply camera offset
        return new ScreenPosition(
            screenX - cameraOffset.X,
            screenY - cameraOffset.Y
        );
    }
    
    /// <summary>
    /// Convert tile coordinates to screen coordinates
    /// </summary>
    public static ScreenPosition TileToScreen(TilePosition tile, float elevation, ScreenPosition cameraOffset)
    {
        return WorldToScreen(new WorldPosition(tile.X, tile.Y, elevation), cameraOffset);
    }
    
    /// <summary>
    /// Convert screen coordinates back to world coordinates (at Z=0)
    /// </summary>
    public static WorldPosition ScreenToWorld(ScreenPosition screen, ScreenPosition cameraOffset)
    {
        // Reverse the camera offset
        var screenX = screen.X + cameraOffset.X;
        var screenY = screen.Y + cameraOffset.Y;
        
        // Reverse isometric projection
        // screenX = (X - Y) * 22, screenY = (X + Y) * 22
        // Solving: X = (screenX/22 + screenY/22) / 2, Y = (screenY/22 - screenX/22) / 2
        var worldX = (float)(screenX + screenY) / (2.0f * TileStepX);
        var worldY = (float)(screenY - screenX) / (2.0f * TileStepY);
        
        return new WorldPosition(worldX, worldY, 0);
    }
    
    /// <summary>
    /// Convert screen coordinates to tile position
    /// </summary>
    public static TilePosition ScreenToTile(ScreenPosition screen, ScreenPosition cameraOffset)
    {
        return ScreenToWorld(screen, cameraOffset).ToTile();
    }
    
    /// <summary>
    /// Get the screen bounds for a tile (diamond shape corners)
    /// </summary>
    public static (ScreenPosition topLeft, ScreenPosition bottomRight) GetTileScreenBounds(TilePosition tile, ScreenPosition cameraOffset)
    {
        var center = TileToScreen(tile, 0, cameraOffset);
        return (
            new ScreenPosition(center.X - TileWidth / 2, center.Y - TileHeight / 2),
            new ScreenPosition(center.X + TileWidth / 2, center.Y + TileHeight / 2)
        );
    }
    
    /// <summary>
    /// Calculate render order for depth sorting (painter's algorithm).
    /// Higher values should be rendered later (on top).
    /// 
    /// Formula: (X + Y) * 256 + Z + typeOffset
    /// </summary>
    public static int GetRenderDepth(WorldPosition pos)
    {
        // Objects further south and east render on top
        // Z-level also affects depth (higher Z = rendered on top)
        return (int)((pos.X + pos.Y) * 256 + (pos.Z + 128));
    }
    
    /// <summary>
    /// Get all tiles visible in a screen rectangle
    /// </summary>
    public static IEnumerable<TilePosition> GetVisibleTiles(
        ScreenPosition screenTopLeft, 
        ScreenPosition screenBottomRight,
        ScreenPosition cameraOffset)
    {
        // Convert screen corners to world space
        var worldTopLeft = ScreenToWorld(screenTopLeft, cameraOffset);
        var worldBottomRight = ScreenToWorld(screenBottomRight, cameraOffset);
        
        // Expand bounds to account for tall objects and isometric diamond shape
        var minX = (int)Math.Floor(worldTopLeft.X) - 2;
        var maxX = (int)Math.Ceiling(worldBottomRight.X) + 2;
        var minY = (int)Math.Floor(worldTopLeft.Y) - 2;
        var maxY = (int)Math.Ceiling(worldBottomRight.Y) + 2;
        
        // Generate tiles in render order (back to front)
        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                yield return new TilePosition(x, y);
            }
        }
    }
    
    /// <summary>
    /// Check if a screen point is inside the diamond-shaped tile
    /// </summary>
    public static bool IsPointInTileDiamond(ScreenPosition point, ScreenPosition tileCenter)
    {
        // Diamond test using Manhattan distance from center
        // For a 44x44 diamond: half-width = 22, half-height = 22
        var dx = Math.Abs(point.X - tileCenter.X);
        var dy = Math.Abs(point.Y - tileCenter.Y);
        
        // Point is inside if: dx/halfWidth + dy/halfHeight <= 1
        return (float)dx / (TileWidth / 2) + (float)dy / (TileHeight / 2) <= 1.0f;
    }
}
