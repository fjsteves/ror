using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RealmOfReality.Client.Assets;
using RealmOfReality.Client.Game;
using RealmOfReality.Shared.Core;
using RealmOfReality.Shared.Entities;
using XnaColor = Microsoft.Xna.Framework.Color;
using XnaGameTime = Microsoft.Xna.Framework.GameTime;

namespace RealmOfReality.Client.Engine;

/// <summary>
/// Renders the game world in isometric perspective using UO map data.
/// 
/// ═══════════════════════════════════════════════════════════════════════════
/// ARCHITECTURE
/// ═══════════════════════════════════════════════════════════════════════════
/// 
/// The world is rendered in layers, back-to-front:
///   1. Terrain  - 3D textured quads via BasicEffect (NOT SpriteBatch!)
///   2. Statics  - SpriteBatch with depth sorting
///   3. Entities - SpriteBatch with animations
///   4. Effects  - SpriteBatch overlays
/// 
/// ═══════════════════════════════════════════════════════════════════════════
/// TERRAIN RENDERING (CRITICAL)
/// ═══════════════════════════════════════════════════════════════════════════
/// 
/// Each terrain cell is a textured quad built from 4 corner tiles:
/// 
///   NW (x,y) ────── NE (x+1,y)      Screen orientation:
///      │              │                     NW ◆
///      │    Cell      │                   ╱    ╲
///      │              │              SW ◆        ◆ NE
///   SW (x,y+1) ───── SE (x+1,y+1)        ╲    ╱
///                                          ◆ SE
/// 
/// WHY NOT SpriteBatch?
///   - SpriteBatch renders axis-aligned rectangles
///   - Cannot handle per-vertex Z heights (slopes)
///   - Results in visual gaps and Z-fighting
/// 
/// TEXTURE LOOKUP CHAIN:
///   1. Get LandTile.TileId from map
///   2. Look up TileData.GetLandTile(TileId).TextureId  ← CRITICAL!
///   3. If TextureId > 0: use Texmaps.GetTexmap(TextureId)
///   4. If TextureId == 0: use Art.GetLandTile(TileId) as fallback
/// 
/// ⚠️ NEVER pass TileId directly to TexmapLoader! Always use TextureId!
/// 
/// ═══════════════════════════════════════════════════════════════════════════
/// ISOMETRIC FORMULAS
/// ═══════════════════════════════════════════════════════════════════════════
/// 
///   screenX = (tileX - tileY) × 22
///   screenY = (tileX + tileY) × 22 - tileZ × 4
/// 
///   Tile dimensions: 44×44 pixel diamonds
///   Z scale: 4 pixels per altitude unit
/// 
/// ═══════════════════════════════════════════════════════════════════════════
/// REFERENCE
/// ═══════════════════════════════════════════════════════════════════════════
/// 
/// ClassicUO sources:
///   - GameSceneDrawingSorting.cs (depth sorting)
///   - Land.cs (terrain quad construction)
///   - GameObject.cs (entity rendering)
/// 
/// </summary>
public sealed class WorldRenderer : IDisposable
{
    // ═══════════════════════════════════════════════════════════════════
    // CONSTANTS
    // ═══════════════════════════════════════════════════════════════════
    
    private const int TILE_SIZE = 44;     // Land tile diamond size
    private const int TILE_STEP = 22;     // Half tile (screen step)
    private const int Z_SCALE = 4;        // Pixels per Z unit
    private const int RENDER_RANGE = 24;  // Tiles from center to render
    
    // ═══════════════════════════════════════════════════════════════════
    // DEPENDENCIES
    // ═══════════════════════════════════════════════════════════════════
    
    private readonly GraphicsDevice _graphics;
    private readonly SpriteBatch _spriteBatch;
    private readonly AssetManager _assets;
    private readonly Camera _camera;
    private readonly UOAssetManager? _uoAssets;
    
    // ═══════════════════════════════════════════════════════════════════
    // TERRAIN RENDERING
    // ═══════════════════════════════════════════════════════════════════
    
    private BasicEffect _terrainEffect = null!;
    private Texture2D _fallbackTexture = null!;
    private MapLoader? _map;
    
    // Terrain vertex/index buffers (reused each frame)
    private readonly VertexPositionTexture[] _quadVertices = new VertexPositionTexture[4];
    private readonly short[] _quadIndices = { 0, 2, 1, 1, 2, 3 };  // NW-SW-NE, NE-SW-SE
    
    // ═══════════════════════════════════════════════════════════════════
    // ENTITY RENDERING
    // ═══════════════════════════════════════════════════════════════════
    
    private readonly Dictionary<ulong, AnimationState> _animStates = new();
    
    // ═══════════════════════════════════════════════════════════════════
    // PUBLIC OPTIONS
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>Whether map is loaded and ready</summary>
    public bool MapLoaded => _map?.IsLoaded ?? false;
    
    /// <summary>Show static objects (walls, trees, etc.)</summary>
    public bool ShowStatics { get; set; } = true;
    
    /// <summary>Use UO tile graphics (vs placeholders)</summary>
    public bool UseUOTiles { get; set; } = true;
    
    /// <summary>Show tile coordinate grid overlay</summary>
    public bool ShowGrid { get; set; } = false;
    
    /// <summary>Tile to highlight (for mouse hover)</summary>
    public TilePosition? HighlightedTile { get; set; }
    
    /// <summary>Entity to highlight</summary>
    public EntityId? HighlightedEntityId { get; set; }
    
    /// <summary>Color for highlighted entities</summary>
    public XnaColor HighlightColor { get; set; } = XnaColor.Yellow;
    
    // ═══════════════════════════════════════════════════════════════════
    // DEBUG OPTIONS
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>Show debug statistics overlay</summary>
    public bool ShowDebugStats { get; set; } = false;
    
    /// <summary>Show texture IDs on terrain (performance heavy)</summary>
    public bool ShowTextureIds { get; set; } = false;
    
    /// <summary>Count terrain quads skipped due to void</summary>
    public int VoidQuadCount { get; private set; }
    
    /// <summary>Count terrain quads rendered this frame</summary>
    public int RenderedQuadCount { get; private set; }
    
    /// <summary>Count statics rendered this frame</summary>
    public int RenderedStaticCount { get; private set; }
    
    /// <summary>Count entities rendered this frame</summary>
    public int RenderedEntityCount { get; private set; }
    
    /// <summary>Count texmap textures used this frame</summary>
    public int TexmapHitCount { get; private set; }

    /// <summary>Count art fallbacks used this frame</summary>
    public int ArtFallbackCount { get; private set; }

    // Debug: track logged failures so we report each missing asset once
    private readonly HashSet<ushort> _loggedTexmapFailures = new();
    private readonly HashSet<ushort> _loggedArtFailures = new();
    
    // ═══════════════════════════════════════════════════════════════════
    // CONSTRUCTOR
    // ═══════════════════════════════════════════════════════════════════
    
    public WorldRenderer(SpriteBatch spriteBatch, AssetManager assets, Camera camera, UOAssetManager? uoAssets = null)
    {
        _graphics = spriteBatch.GraphicsDevice;
        _spriteBatch = spriteBatch;
        _assets = assets;
        _camera = camera;
        _uoAssets = uoAssets;
        
        InitializeTerrainRendering();
    }
    
    private void InitializeTerrainRendering()
    {
        // BasicEffect for textured terrain quads
        _terrainEffect = new BasicEffect(_graphics)
        {
            TextureEnabled = true,
            VertexColorEnabled = false,
            LightingEnabled = false
        };
        
        // Fallback texture for tiles without texmaps
        CreateFallbackTexture();
    }
    
    private void CreateFallbackTexture()
    {
        // Create a 64×64 magenta/cyan checkerboard pattern
        // This makes missing textures VERY obvious for debugging
        _fallbackTexture = new Texture2D(_graphics, 64, 64);
        var pixels = new XnaColor[64 * 64];
        
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                // 8x8 pixel checkerboard squares
                bool isEven = ((x / 8) + (y / 8)) % 2 == 0;
                pixels[y * 64 + x] = isEven 
                    ? new XnaColor(255, 0, 255, 255)   // Magenta
                    : new XnaColor(0, 255, 255, 255);   // Cyan
            }
        }
        
        _fallbackTexture.SetData(pixels);
        Console.WriteLine("[WorldRenderer] Created fallback checkerboard texture");
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // MAP LOADING
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Load a UO map for rendering.
    /// </summary>
    /// <param name="graphicsDevice">Graphics device (unused, for compatibility)</param>
    /// <param name="uoDataPath">Path to UO data files</param>
    /// <param name="facet">Map facet (0=Felucca, 1=Trammel, etc.)</param>
    public bool LoadMap(GraphicsDevice graphicsDevice, string uoDataPath, int facet = 0)
    {
        _map = new MapLoader(_graphics, uoDataPath);

        if (_map.Load(facet))
        {
            Console.WriteLine($"[WorldRenderer] Map loaded: {_map.Width}×{_map.Height} tiles");
            ResetDebugLogging();
            return true;
        }
        
        Console.WriteLine("[WorldRenderer] Map load failed");
        _map = null;
        return false;
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // MAIN RENDER ENTRY POINTS
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Main render method (alias for Render).
    /// </summary>
    public void Draw(GameState gameState, XnaGameTime gameTime)
    {
        Render(gameState, gameTime);
    }
    
    /// <summary>
    /// Render the complete game world.
    /// </summary>
    public void Render(GameState gameState, XnaGameTime gameTime)
    {
        // Reset frame statistics
        VoidQuadCount = 0;
        RenderedQuadCount = 0;
        RenderedStaticCount = 0;
        RenderedEntityCount = 0;
        TexmapHitCount = 0;
        ArtFallbackCount = 0;
        
        // Get center position from player
        var player = gameState.Player;
        int centerX = player != null ? (int)player.Position.X : 1000;
        int centerY = player != null ? (int)player.Position.Y : 1000;
        
        // One-time debug logging
        if (!_debugLogged)
        {
            LogDebugInfo(centerX, centerY);
            _debugLogged = true;
        }
        
        // ─────────────────────────────────────────────────────────────────
        // 1. Render terrain as 3D quads
        // ─────────────────────────────────────────────────────────────────
        RenderTerrain(centerX, centerY);
        
        // ─────────────────────────────────────────────────────────────────
        // 2. Render statics and entities with SpriteBatch
        // ─────────────────────────────────────────────────────────────────
        _spriteBatch.Begin(
            SpriteSortMode.BackToFront,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            null,
            _camera.GetTransformMatrix()
        );
        
        if (ShowStatics)
            RenderStatics(centerX, centerY);
        
        RenderEntities(gameState, gameTime);
        
        if (HighlightedTile.HasValue)
            RenderTileHighlight(HighlightedTile.Value);
        
        _spriteBatch.End();
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // TERRAIN RENDERING
    // ═══════════════════════════════════════════════════════════════════
    
    private void RenderTerrain(int centerX, int centerY)
    {
        if (_map == null || !_map.IsLoaded)
            return;
        
        // Set up orthographic projection that maps directly to screen
        SetupTerrainMatrices();
        
        // Collect and sort terrain quads
        var quads = CollectTerrainQuads(centerX, centerY);
        
        // Sort back-to-front
        quads.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        
        // Render each quad
        foreach (var quad in quads)
        {
            DrawTerrainQuad(quad);
        }
    }
    
    private void SetupTerrainMatrices()
    {
        // Identity matrices - we'll transform vertices directly
        _terrainEffect.World = Matrix.Identity;
        _terrainEffect.View = Matrix.Identity;
        
        // Orthographic projection to screen coordinates
        _terrainEffect.Projection = Matrix.CreateOrthographicOffCenter(
            0, _graphics.Viewport.Width,
            _graphics.Viewport.Height, 0,
            -1, 1
        );
        
        // Disable backface culling for terrain
        _graphics.RasterizerState = RasterizerState.CullNone;
    }
    
    private List<TerrainQuad> CollectTerrainQuads(int centerX, int centerY)
    {
        var quads = new List<TerrainQuad>();
        
        for (int dy = -RENDER_RANGE; dy <= RENDER_RANGE; dy++)
        {
            for (int dx = -RENDER_RANGE; dx <= RENDER_RANGE; dx++)
            {
                int x = centerX + dx;
                int y = centerY + dy;
                
                // Need x+1 and y+1 for quad corners
                if (x < 0 || y < 0 || x >= _map!.Width - 1 || y >= _map.Height - 1)
                    continue;
                
                // Get the 4 corner tiles
                var nw = _map.GetLandTile(x, y);         // Northwest (top)
                var ne = _map.GetLandTile(x + 1, y);     // Northeast (right)
                var sw = _map.GetLandTile(x, y + 1);     // Southwest (left)
                var se = _map.GetLandTile(x + 1, y + 1); // Southeast (bottom)
                
                // Skip if all corners are void
                if (nw.IsVoid && ne.IsVoid && sw.IsVoid && se.IsVoid)
                {
                    VoidQuadCount++;
                    continue;
                }
                
                // Calculate render priority (back-to-front)
                int maxZ = Math.Max(Math.Max(nw.Z, ne.Z), Math.Max(sw.Z, se.Z));
                int priority = (x + y) * 256 + maxZ;
                
                quads.Add(new TerrainQuad
                {
                    X = x,
                    Y = y,
                    NW = nw,
                    NE = ne,
                    SW = sw,
                    SE = se,
                    Priority = priority
                });
            }
        }
        
        return quads;
    }
    
    private void DrawTerrainQuad(TerrainQuad quad)
    {
        // Get texture for this quad (texmap or art fallback) - never null
        var texture = GetQuadTexture(quad);
        
        // Track statistics
        RenderedQuadCount++;
        
        // ─────────────────────────────────────────────────────────────────
        // Calculate screen positions for each corner vertex
        // Each corner can have a different Z height for smooth terrain slopes
        // ─────────────────────────────────────────────────────────────────
        var screenNW = WorldToScreen(quad.X, quad.Y, quad.NW.Z);
        var screenNE = WorldToScreen(quad.X + 1, quad.Y, quad.NE.Z);
        var screenSW = WorldToScreen(quad.X, quad.Y + 1, quad.SW.Z);
        var screenSE = WorldToScreen(quad.X + 1, quad.Y + 1, quad.SE.Z);
        
        // ─────────────────────────────────────────────────────────────────
        // Apply camera transform manually (BasicEffect doesn't use SpriteBatch)
        // ─────────────────────────────────────────────────────────────────
        var cameraMatrix = _camera.GetTransformMatrix();
        var drawNW = Vector2.Transform(screenNW, cameraMatrix);
        var drawNE = Vector2.Transform(screenNE, cameraMatrix);
        var drawSW = Vector2.Transform(screenSW, cameraMatrix);
        var drawSE = Vector2.Transform(screenSE, cameraMatrix);
        
        // ─────────────────────────────────────────────────────────────────
        // Build vertex buffer with UV coordinates
        // UV is standard quad mapping: (0,0), (1,0), (0,1), (1,1)
        // NOT diamond mapping - the texture is stretched to fit the quad
        // ─────────────────────────────────────────────────────────────────
        _quadVertices[0] = new VertexPositionTexture(new Vector3(drawNW, 0), new Vector2(0, 0));  // NW = top
        _quadVertices[1] = new VertexPositionTexture(new Vector3(drawNE, 0), new Vector2(1, 0));  // NE = right
        _quadVertices[2] = new VertexPositionTexture(new Vector3(drawSW, 0), new Vector2(0, 1));  // SW = left
        _quadVertices[3] = new VertexPositionTexture(new Vector3(drawSE, 0), new Vector2(1, 1));  // SE = bottom
        
        // Draw the quad
        _terrainEffect.Texture = texture;
        
        foreach (var pass in _terrainEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphics.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList,
                _quadVertices,
                0,
                4,
                _quadIndices,
                0,
                2
            );
        }
    }
    
    /// <summary>
    /// Get the texture for a terrain quad.
    /// 
    /// LOOKUP CHAIN (ClassicUO-compliant):
    /// 1. Get TextureId from TileData (NOT the TileId!)
    /// 2. Try Texmaps.GetTexmap(TextureId) for stretched terrain
    /// 3. Fallback to Art.GetLandTile(TileId) for 44×44 diamond
    /// 4. Final fallback to checkerboard placeholder texture
    /// 
    /// This method ALWAYS returns a texture - never null.
    /// </summary>
    private Texture2D GetQuadTexture(TerrainQuad quad)
    {
        // Strategy: Pick the texture from the best corner based on ClassicUO algorithm
        var textureChoice = ChooseTerrainTextures(quad);
        
        // 1. Try texmap if we have a valid TextureId
        // CRITICAL: textureId comes from TileData, NOT TileId!
        if (textureChoice.TextureId > 0 && _uoAssets?.Texmaps?.IsLoaded == true)
        {
            var texmap = _uoAssets.Texmaps.GetTexmap(textureChoice.TextureId);
            if (texmap != null)
            {
                TexmapHitCount++;
                return texmap;
            }
            // Debug: texmap lookup failed for valid TextureId (only log once)
            if (ShouldLogTexmapFailure(textureChoice.TextureId))
            {
                Console.WriteLine($"[WorldRenderer] Texmap lookup failed for TextureId={textureChoice.TextureId}");
            }
        }
        
        // 2. Fallback to 44×44 art tile
        if (_uoAssets?.Art != null)
        {
            // Use same priority algorithm for TileId selection
            ushort artId = textureChoice.ArtTileId;
            
            if (artId > 0)
            {
                var art = _uoAssets.Art.GetLandTile(artId);
                // Use art if we have a texture
                if (art?.Texture != null)
                {
                    ArtFallbackCount++;
                    return art.Texture;
                }
                // Debug: art lookup failed (only log once)
                if (ShouldLogArtFailure(artId))
                {
                    Console.WriteLine($"[WorldRenderer] Art lookup failed for TileId={artId}");
                    _debugLogged = true;
                }
            }
        }
        
        // 3. Final fallback - ALWAYS return something to avoid black tiles
        ArtFallbackCount++;
        if (_fallbackTexture == null)
        {
            Console.WriteLine("[WorldRenderer] WARNING: Fallback texture is null! Creating emergency texture.");
            CreateFallbackTexture();
        }
        return _fallbackTexture!;
    }
    
    /// <summary>
    /// Choose both the texmap TextureId and the art TileId in one pass so
    /// fallback visuals stay aligned with ClassicUO's highest-corner rule.
    /// </summary>
    private TerrainTextureChoice ChooseTerrainTextures(TerrainQuad quad)
    {
        var tileData = _uoAssets?.TileData;
        bool tileDataReady = tileData?.IsLoaded == true;

        // Evaluate corners in deterministic NW → NE → SW → SE order to match
        // ClassicUO tie-breaking and avoid shimmering across adjacent quads.
        var corners = new[]
        {
            new CornerCandidate(quad.NW, tileDataReady ? tileData!.GetLandTile(quad.NW.TileId) : default, 0),
            new CornerCandidate(quad.NE, tileDataReady ? tileData!.GetLandTile(quad.NE.TileId) : default, 1),
            new CornerCandidate(quad.SW, tileDataReady ? tileData!.GetLandTile(quad.SW.TileId) : default, 2),
            new CornerCandidate(quad.SE, tileDataReady ? tileData!.GetLandTile(quad.SE.TileId) : default, 3)
        };

        ushort bestTex = 0;
        sbyte bestTexZ = sbyte.MinValue;
        int bestTexPriority = int.MaxValue;
        LandTile bestTexTile = default;

        LandTile bestArtTile = default;
        sbyte bestArtZ = sbyte.MinValue;
        int bestArtPriority = int.MaxValue;

        foreach (var corner in corners)
        {
            if (corner.Tile.TileId != 0 && (corner.Tile.Z > bestArtZ || (corner.Tile.Z == bestArtZ && corner.Priority < bestArtPriority)))
            {
                bestArtTile = corner.Tile;
                bestArtZ = corner.Tile.Z;
                bestArtPriority = corner.Priority;
            }

            if (!corner.Data.HasTexmap)
                continue;

            if (corner.Tile.Z > bestTexZ || (corner.Tile.Z == bestTexZ && corner.Priority < bestTexPriority))
            {
                bestTex = corner.Data.TextureId;
                bestTexZ = corner.Tile.Z;
                bestTexPriority = corner.Priority;
                bestTexTile = corner.Tile;
            }
        }

        ushort artTileId = 0;

        // If we selected a texmap corner, use that same corner's art for fallback
        if (bestTex > 0 && bestTexTile.TileId != 0)
        {
            artTileId = bestTexTile.TileId;
        }
        else if (bestArtTile.TileId != 0)
        {
            artTileId = bestArtTile.TileId;
        }

        return new TerrainTextureChoice(bestTex, artTileId);
    }

    private readonly struct CornerCandidate
    {
        public readonly LandTile Tile;
        public readonly LandTileData Data;
        public readonly int Priority;

        public CornerCandidate(LandTile tile, LandTileData data, int priority)
        {
            Tile = tile;
            Data = data;
            Priority = priority;
        }
    }

    private readonly struct TerrainTextureChoice
    {
        public readonly ushort TextureId;
        public readonly ushort ArtTileId;

        public TerrainTextureChoice(ushort textureId, ushort artTileId)
        {
            TextureId = textureId;
            ArtTileId = artTileId;
        }
    }

    private bool ShouldLogTexmapFailure(ushort textureId)
    {
        if (textureId == 0)
            return false;

        return _loggedTexmapFailures.Add(textureId);
    }

    private bool ShouldLogArtFailure(ushort artTileId)
    {
        if (artTileId == 0)
            return false;

        return _loggedArtFailures.Add(artTileId);
    }

    // ═══════════════════════════════════════════════════════════════════
    // STATIC RENDERING
    // ═══════════════════════════════════════════════════════════════════
    
    private void RenderStatics(int centerX, int centerY)
    {
        if (_map == null || !_map.HasStatics || _uoAssets?.Art == null)
            return;
        
        for (int dy = -RENDER_RANGE; dy <= RENDER_RANGE; dy++)
        {
            for (int dx = -RENDER_RANGE; dx <= RENDER_RANGE; dx++)
            {
                int x = centerX + dx;
                int y = centerY + dy;
                
                if (x < 0 || y < 0 || x >= _map.Width || y >= _map.Height)
                    continue;
                
                var statics = _map.GetStaticTiles(x, y);
                foreach (var staticTile in statics)
                {
                    DrawStatic(x, y, staticTile);
                }
            }
        }
    }
    
    private void DrawStatic(int tileX, int tileY, StaticTile staticTile)
    {
        var art = _uoAssets?.Art?.GetStaticItem(staticTile.ItemId);
        if (art?.Texture == null)
            return;
        
        // Track statistics
        RenderedStaticCount++;
        
        // Calculate screen position
        var screenPos = WorldToScreen(tileX, tileY, staticTile.Z);
        
        // Static items are bottom-center anchored
        var drawPos = new Vector2(
            screenPos.X - art.Width / 2,
            screenPos.Y - art.Height + TILE_STEP
        );
        
        // Calculate depth for sorting
        float depth = CalculateDepth(tileX, tileY, staticTile.Z, 1);
        
        _spriteBatch.Draw(
            art.Texture,
            drawPos,
            null,
            XnaColor.White,
            0f,
            Vector2.Zero,
            1f,
            SpriteEffects.None,
            depth
        );
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // ENTITY RENDERING
    // ═══════════════════════════════════════════════════════════════════
    
    private void RenderEntities(GameState gameState, XnaGameTime gameTime)
    {
        foreach (var entity in gameState.GetAllEntities())
        {
            // Track statistics
            RenderedEntityCount++;
            
            int ex = (int)entity.Position.X;
            int ey = (int)entity.Position.Y;
            
            // Get terrain height at entity position
            int terrainZ = 0;
            if (_map?.IsLoaded == true && ex >= 0 && ey >= 0 && ex < _map.Width && ey < _map.Height)
            {
                terrainZ = _map.GetLandTile(ex, ey).Z;
            }
            
            int ez = terrainZ + (int)entity.Position.Z;
            var screenPos = WorldToScreen(ex, ey, ez);
            float depth = CalculateDepth(ex, ey, ez, 3);
            
            if (entity is Mobile mobile)
            {
                DrawMobile(mobile, screenPos, depth, gameTime);
            }
            else
            {
                DrawEntity(entity, screenPos, depth);
            }
        }
    }
    
    private void DrawMobile(Mobile mobile, Vector2 screenPos, float depth, XnaGameTime gameTime)
    {
        Texture2D? texture = null;
        
        // Try UO animation
        if (UseUOTiles && _uoAssets?.Animations != null)
        {
            var animState = GetAnimState(mobile);
            animState.Update(gameTime, mobile.IsMoving, mobile.IsRunning);
            
            int bodyId = mobile is PlayerEntity player ? player.BodyType : mobile.TypeId;
            var action = animState.GetAction(mobile.IsMoving, mobile.IsRunning);
            var direction = (AnimDirection)((int)mobile.Facing % 8);
            
            var anim = _uoAssets.Animations.GetAnimation(bodyId, action, direction);
            if (anim != null && animState.FrameIndex < anim.Frames.Length)
            {
                var frame = anim.Frames[animState.FrameIndex];
                if (frame?.Texture != null)
                {
                    var drawPos = new Vector2(
                        screenPos.X - frame.Texture.Width / 2,
                        screenPos.Y + TILE_STEP - frame.Texture.Height
                    );
                    
                    var color = IsHighlighted(mobile) ? HighlightColor : XnaColor.White;
                    
                    _spriteBatch.Draw(frame.Texture, drawPos, null, color, 0f, Vector2.Zero, 1f, SpriteEffects.None, depth);
                    return;
                }
            }
        }
        
        // Fallback to simple sprites
        texture = mobile switch
        {
            PlayerEntity => _assets.PlayerSprite,
            NpcEntity => _assets.NpcSprite,
            _ => _assets.PlayerSprite
        };
        
        if (texture != null)
        {
            var drawPos = new Vector2(
                screenPos.X - texture.Width / 2,
                screenPos.Y + TILE_STEP - texture.Height
            );
            
            var color = IsHighlighted(mobile) ? HighlightColor : XnaColor.White;
            _spriteBatch.Draw(texture, drawPos, null, color, 0f, Vector2.Zero, 1f, SpriteEffects.None, depth);
        }
    }
    
    private void DrawEntity(Entity entity, Vector2 screenPos, float depth)
    {
        var texture = _assets.PlayerSprite;
        if (texture == null) return;
        
        var drawPos = new Vector2(
            screenPos.X - texture.Width / 2,
            screenPos.Y + TILE_STEP - texture.Height
        );
        
        _spriteBatch.Draw(texture, drawPos, null, XnaColor.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, depth);
    }
    
    private void RenderTileHighlight(TilePosition pos)
    {
        if (_assets.TileHighlight == null) return;
        
        int z = 0;
        if (_map?.IsLoaded == true && pos.X >= 0 && pos.Y >= 0 && pos.X < _map.Width && pos.Y < _map.Height)
        {
            z = _map.GetLandTile(pos.X, pos.Y).Z;
        }
        
        var screenPos = WorldToScreen(pos.X, pos.Y, z);
        var drawPos = new Vector2(screenPos.X - TILE_STEP, screenPos.Y);
        
        _spriteBatch.Draw(_assets.TileHighlight, drawPos, new XnaColor(XnaColor.Yellow, 0.5f));
    }
    
    private bool IsHighlighted(Entity entity)
    {
        return HighlightedEntityId.HasValue && entity.Id == HighlightedEntityId.Value;
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // COORDINATE CONVERSION
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Convert world tile coordinates to screen coordinates.
    /// </summary>
    private static Vector2 WorldToScreen(int tileX, int tileY, int tileZ)
    {
        int screenX = (tileX - tileY) * TILE_STEP;
        int screenY = (tileX + tileY) * TILE_STEP - tileZ * Z_SCALE;
        return new Vector2(screenX, screenY);
    }
    
    /// <summary>
    /// Convert screen coordinates to tile coordinates.
    /// </summary>
    public TilePosition ScreenToTile(int screenX, int screenY, int assumedZ = 0)
    {
        screenY += assumedZ * Z_SCALE;
        float fx = (screenX / (float)TILE_STEP + screenY / (float)TILE_STEP) / 2f;
        float fy = (screenY / (float)TILE_STEP - screenX / (float)TILE_STEP) / 2f;
        return new TilePosition((int)MathF.Floor(fx), (int)MathF.Floor(fy));
    }
    
    /// <summary>
    /// Calculate SpriteBatch layer depth (0=front, 1=back).
    /// </summary>
    private static float CalculateDepth(int tileX, int tileY, int tileZ, int typeBonus)
    {
        int priority = (tileX + tileY) * 256 + (tileZ + 128) + typeBonus;
        return 1.0f - (priority / 10000000f);
    }
    
    /// <summary>
    /// Get land tile at coordinates (for external use).
    /// </summary>
    public LandTile GetLandTile(int x, int y)
    {
        if (_map?.IsLoaded == true && x >= 0 && y >= 0 && x < _map.Width && y < _map.Height)
        {
            return _map.GetLandTile(x, y);
        }
        return new LandTile { TileId = 3, Z = 0 };
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // ANIMATION STATE
    // ═══════════════════════════════════════════════════════════════════
    
    private AnimationState GetAnimState(Entity entity)
    {
        if (!_animStates.TryGetValue(entity.Id.Value, out var state))
        {
            state = new AnimationState();
            _animStates[entity.Id.Value] = state;
        }
        return state;
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // DEBUG
    // ═══════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Get a summary of render statistics for the last frame.
    /// Useful for debugging and performance monitoring.
    /// </summary>
    public string GetRenderStats()
    {
        return $"Quads: {RenderedQuadCount} (void: {VoidQuadCount}) | " +
               $"Tex: {TexmapHitCount}/{ArtFallbackCount} | " +
               $"Statics: {RenderedStaticCount} | " +
               $"Entities: {RenderedEntityCount}";
    }

    /// <summary>
    /// Reset one-time debug guards so diagnostics rerun after loading or
    /// reloading content. Also clears missing-asset tracking so new assets
    /// can surface warnings if they still fail to load.
    /// </summary>
    public void ResetDebugLogging()
    {
        _debugLogged = false;
        _loggedTexmapFailures.Clear();
        _loggedArtFailures.Clear();
    }
    
    private void LogDebugInfo(int centerX, int centerY)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine("[WorldRenderer] Debug Info");
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
        Console.WriteLine($"[WorldRenderer] Map loaded: {MapLoaded}");
        Console.WriteLine($"[WorldRenderer] TileData loaded: {_uoAssets?.TileData?.IsLoaded ?? false}, count: {_uoAssets?.TileData?.LandCount ?? 0}");
        Console.WriteLine($"[WorldRenderer] Texmaps loaded: {_uoAssets?.Texmaps?.IsLoaded ?? false}, entries: {_uoAssets?.Texmaps?.EntryCount ?? 0}");
        Console.WriteLine($"[WorldRenderer] Art loaded: {_uoAssets?.Art?.IsLoaded ?? false}, entries: {_uoAssets?.Art?.EntryCount ?? 0}");
        Console.WriteLine($"[WorldRenderer] Fallback texture: {(_fallbackTexture != null ? "created" : "NULL!")}");
        
        if (_map != null)
        {
            Console.WriteLine($"[WorldRenderer] Center: ({centerX}, {centerY})");
            Console.WriteLine("[WorldRenderer] Sample tiles around center:");
            
            for (int i = 0; i < 5; i++)
            {
                var tile = _map.GetLandTile(centerX + i, centerY);
                Console.Write($"  Tile({centerX + i},{centerY}): TileId={tile.TileId}, Z={tile.Z}");
                
                if (_uoAssets?.TileData?.IsLoaded == true && tile.TileId > 0)
                {
                    var data = _uoAssets.TileData.GetLandTile(tile.TileId);
                    Console.Write($" → TextureId={data.TextureId}");
                    
                    // Try to show what texture would be used
                    if (data.TextureId > 0 && _uoAssets.Texmaps?.IsLoaded == true)
                    {
                        var texmap = _uoAssets.Texmaps.GetTexmap(data.TextureId);
                        Console.Write(texmap != null ? " [TEXMAP OK]" : " [TEXMAP FAIL]");
                    }
                    else if (_uoAssets.Art?.IsLoaded == true)
                    {
                        var art = _uoAssets.Art.GetLandTile(tile.TileId);
                        Console.Write(art?.Texture != null ? " [ART OK]" : " [ART FAIL]");
                    }
                }
                Console.WriteLine();
            }
        }
        Console.WriteLine("═══════════════════════════════════════════════════════════════════");
    }
    
    // ═══════════════════════════════════════════════════════════════════
    // CLEANUP
    // ═══════════════════════════════════════════════════════════════════
    
    public void Dispose()
    {
        _terrainEffect?.Dispose();
        _fallbackTexture?.Dispose();
        _map?.Dispose();
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// HELPER STRUCTURES
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Represents a terrain quad to be rendered.
/// </summary>
internal struct TerrainQuad
{
    public int X;
    public int Y;
    public LandTile NW;
    public LandTile NE;
    public LandTile SW;
    public LandTile SE;
    public int Priority;
}

/// <summary>
/// Tracks animation state for an entity.
/// </summary>
internal sealed class AnimationState
{
    public int FrameIndex { get; private set; }
    private double _frameTimer;
    private const double WalkFrameDuration = 0.15;
    private const double RunFrameDuration = 0.08;
    
    public void Update(XnaGameTime gameTime, bool isMoving, bool isRunning)
    {
        if (!isMoving)
        {
            FrameIndex = 0;
            _frameTimer = 0;
            return;
        }
        
        double frameDuration = isRunning ? RunFrameDuration : WalkFrameDuration;
        _frameTimer += gameTime.ElapsedGameTime.TotalSeconds;
        
        if (_frameTimer >= frameDuration)
        {
            _frameTimer -= frameDuration;
            FrameIndex = (FrameIndex + 1) % 8;
        }
    }
    
    public AnimAction GetAction(bool isMoving, bool isRunning)
    {
        if (!isMoving) return AnimAction.Stand;
        return isRunning ? AnimAction.Run : AnimAction.Walk;
    }
}
