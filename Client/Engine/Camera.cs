using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RealmOfReality.Shared.Core;
using XnaGameTime = Microsoft.Xna.Framework.GameTime;

namespace RealmOfReality.Client.Engine;

/// <summary>
/// Camera for managing the game viewport in isometric space.
/// 
/// ═══════════════════════════════════════════════════════════════════════════
/// COORDINATE SYSTEM
/// ═══════════════════════════════════════════════════════════════════════════
/// 
/// The camera operates in screen-space coordinates (post-isometric transform):
///   - Position: Center point of the viewport in isometric screen coordinates
///   - This is NOT world tile coordinates!
/// 
/// Transform Matrix (for SpriteBatch):
///   1. Translate by -Position (center world at origin)
///   2. Scale by Zoom factor
///   3. Translate to viewport center (center on screen)
/// 
/// ═══════════════════════════════════════════════════════════════════════════
/// USAGE
/// ═══════════════════════════════════════════════════════════════════════════
/// 
/// // In Update():
/// camera.Follow(player.GetWorldPosition());
/// camera.Update(gameTime);
/// 
/// // In Draw():
/// spriteBatch.Begin(transformMatrix: camera.GetTransformMatrix());
/// 
/// // For terrain (manual transform):
/// var screenPos = WorldToScreen(x, y, z);
/// var drawPos = Vector2.Transform(screenPos, camera.GetTransformMatrix());
/// 
/// </summary>
public class Camera
{
    // ═══════════════════════════════════════════════════════════════════
    // CONSTANTS
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>Minimum zoom level (zoomed out)</summary>
    public const float MIN_ZOOM = 0.25f;
    
    /// <summary>Maximum zoom level (zoomed in)</summary>
    public const float MAX_ZOOM = 4.0f;
    
    /// <summary>Default zoom level</summary>
    public const float DEFAULT_ZOOM = 1.0f;
    
    // ═══════════════════════════════════════════════════════════════════
    // PROPERTIES
    // ═══════════════════════════════════════════════════════════════════
    
    public Viewport Viewport { get; set; }
    
    /// <summary>
    /// Current camera position in isometric screen coordinates.
    /// This is where the viewport is currently centered.
    /// </summary>
    public Vector2 Position { get; set; }
    
    /// <summary>
    /// Target position for smooth follow.
    /// Camera lerps from Position toward TargetPosition each frame.
    /// </summary>
    public Vector2 TargetPosition { get; set; }
    
    private float _zoom = DEFAULT_ZOOM;
    
    /// <summary>
    /// Zoom level (1.0 = normal, 2.0 = 2x zoom in, 0.5 = 2x zoom out).
    /// Clamped to MIN_ZOOM..MAX_ZOOM range.
    /// </summary>
    public float Zoom
    {
        get => _zoom;
        set => _zoom = MathHelper.Clamp(value, MIN_ZOOM, MAX_ZOOM);
    }
    
    /// <summary>
    /// Smoothing factor for camera movement.
    /// 0 = instant snap to target
    /// 0.1 = smooth follow (default)
    /// 0.9 = very slow follow
    /// </summary>
    public float Smoothing { get; set; } = 0.1f;
    
    /// <summary>
    /// Camera bounds in screen coordinates.
    /// If set, camera won't go outside these bounds.
    /// </summary>
    public Rectangle? Bounds { get; set; }
    
    public Camera(Viewport viewport)
    {
        Viewport = viewport;
        Position = Vector2.Zero;
        TargetPosition = Vector2.Zero;
    }
    
    /// <summary>
    /// Update camera position with smooth follow
    /// </summary>
    public void Update(XnaGameTime gameTime)
    {
        // Smooth interpolation towards target
        Position = Vector2.Lerp(Position, TargetPosition, 1f - Smoothing);
        
        // Clamp to bounds if set
        if (Bounds.HasValue)
        {
            var halfWidth = Viewport.Width / (2f * Zoom);
            var halfHeight = Viewport.Height / (2f * Zoom);
            
            Position = new Vector2(
                MathHelper.Clamp(Position.X, Bounds.Value.Left + halfWidth, Bounds.Value.Right - halfWidth),
                MathHelper.Clamp(Position.Y, Bounds.Value.Top + halfHeight, Bounds.Value.Bottom - halfHeight)
            );
        }
    }
    
    /// <summary>
    /// Follow a world position
    /// </summary>
    public void Follow(WorldPosition worldPos)
    {
        // Convert world position to screen-space center point
        var screenPos = IsometricHelper.WorldToScreen(worldPos, new ScreenPosition(0, 0));
        TargetPosition = new Vector2(screenPos.X, screenPos.Y);
    }
    
    /// <summary>
    /// Get the transformation matrix for SpriteBatch
    /// </summary>
    public Matrix GetTransformMatrix()
    {
        return Matrix.CreateTranslation(-Position.X, -Position.Y, 0) *
               Matrix.CreateScale(Zoom, Zoom, 1) *
               Matrix.CreateTranslation(Viewport.Width / 2f, Viewport.Height / 2f, 0);
    }
    
    /// <summary>
    /// Convert screen coordinates to world coordinates
    /// </summary>
    public WorldPosition ScreenToWorld(Vector2 screenPos)
    {
        // Reverse the transform
        var worldScreenPos = Vector2.Transform(screenPos, Matrix.Invert(GetTransformMatrix()));
        
        return IsometricHelper.ScreenToWorld(
            new ScreenPosition((int)worldScreenPos.X, (int)worldScreenPos.Y),
            new ScreenPosition(0, 0)
        );
    }
    
    /// <summary>
    /// Convert world coordinates to screen coordinates
    /// </summary>
    public Vector2 WorldToScreen(WorldPosition worldPos)
    {
        var isoScreen = IsometricHelper.WorldToScreen(worldPos, new ScreenPosition(0, 0));
        return Vector2.Transform(new Vector2(isoScreen.X, isoScreen.Y), GetTransformMatrix());
    }
    
    /// <summary>
    /// Get the visible world area in tile coordinates
    /// </summary>
    public Rectangle GetVisibleArea()
    {
        // Calculate the center tile based on camera position
        // The camera position is in isometric screen coordinates
        // We need to reverse-transform to get world coordinates
        
        // Simple approach: figure out what world tile is at the center of screen
        var centerWorld = ScreenToWorld(new Vector2(Viewport.Width / 2f, Viewport.Height / 2f));
        
        // Return a rectangle centered on that position with padding for screen size
        var tilesWide = (int)(Viewport.Width / (32 * Zoom)) + 10;
        var tilesHigh = (int)(Viewport.Height / (16 * Zoom)) + 10;
        
        return new Rectangle(
            (int)centerWorld.X - tilesWide,
            (int)centerWorld.Y - tilesHigh,
            tilesWide * 2,
            tilesHigh * 2
        );
    }
    
    /// <summary>
    /// Get the camera offset for IsometricHelper functions
    /// </summary>
    public ScreenPosition GetCameraOffset()
    {
        return new ScreenPosition((int)Position.X, (int)Position.Y);
    }
    
    /// <summary>
    /// Set the viewport size (used when game viewport is resized)
    /// </summary>
    public void SetViewportSize(int width, int height)
    {
        Viewport = new Viewport(0, 0, width, height);
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // ZOOM CONTROLS
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Zoom in by a step amount (default 0.1).
    /// </summary>
    public void ZoomIn(float step = 0.1f)
    {
        Zoom += step;
    }
    
    /// <summary>
    /// Zoom out by a step amount (default 0.1).
    /// </summary>
    public void ZoomOut(float step = 0.1f)
    {
        Zoom -= step;
    }
    
    /// <summary>
    /// Reset zoom to default level.
    /// </summary>
    public void ResetZoom()
    {
        Zoom = DEFAULT_ZOOM;
    }
    
    /// <summary>
    /// Snap camera to target immediately (no smooth follow).
    /// </summary>
    public void SnapToTarget()
    {
        Position = TargetPosition;
    }
}
