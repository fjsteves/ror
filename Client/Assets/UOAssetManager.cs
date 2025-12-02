using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RealmOfReality.Client.Assets;

/// <summary>
/// Central manager for all Ultima Online assets.
/// Provides unified access to art, animations, fonts, and metadata.
/// 
/// ═══════════════════════════════════════════════════════════════════════════
/// ASSET TYPES
/// ═══════════════════════════════════════════════════════════════════════════
/// 
/// | Property    | Files                     | Purpose                       |
/// |-------------|---------------------------|-------------------------------|
/// | Art         | art.mul/artidx.mul        | Land tiles, static items      |
/// | Gumps       | gumps.mul/gumpidx.mul     | UI graphics                   |
/// | Animations  | anim*.mul/anim*.idx       | Character/creature movement   |
/// | TileData    | tiledata.mul              | Tile metadata, TextureId      |
/// | Texmaps     | texmaps.mul/texidx.mul    | Stretched terrain textures    |
/// | Hues        | hues.mul                  | Color palettes                |
/// | Fonts       | fonts*.mul/unifont*.mul   | Text rendering                |
/// 
/// ═══════════════════════════════════════════════════════════════════════════
/// LOADING ORDER (IMPORTANT)
/// ═══════════════════════════════════════════════════════════════════════════
/// 
/// Assets are loaded in this order due to dependencies:
/// 
/// 1. Art       - No dependencies (provides base graphics)
/// 2. Gumps     - No dependencies (UI graphics)
/// 3. Animations - No dependencies (character graphics)
/// 4. TileData  - No dependencies (provides TextureId for terrain)
/// 5. Texmaps   - Uses TileData.TextureId for lookup validation
/// 6. Hues      - No dependencies (color palettes)
/// 7. Fonts     - No dependencies (text rendering)
/// 
/// ═══════════════════════════════════════════════════════════════════════════
/// USAGE
/// ═══════════════════════════════════════════════════════════════════════════
/// 
/// // Create and initialize
/// var assets = new UOAssetManager(graphicsDevice, uoDataPath);
/// if (!assets.Initialize())
/// {
///     // Check assets.LoadErrors for details
/// }
/// 
/// // Get textures
/// var landTex = assets.GetLandTile(3);        // Grass
/// var staticTex = assets.GetStaticItem(100);  // Some item
/// var gumpTex = assets.GetGump(5000);         // Some UI element
/// 
/// // Get metadata
/// var tileInfo = assets.GetLandData(3);       // Grass properties
/// var itemInfo = assets.GetItemData(100);     // Item properties
/// 
/// ═══════════════════════════════════════════════════════════════════════════
/// CRITICAL NOTES
/// ═══════════════════════════════════════════════════════════════════════════
/// 
/// For terrain rendering, use TileData.TextureId (not TileId!) to look up
/// texmaps. See ARCHITECTURE.md and RENDERING.md for details.
/// 
/// </summary>
public class UOAssetManager : IDisposable
{
    private readonly GraphicsDevice _graphics;
    private readonly UOAssetConfig _config;
    
    // Asset loaders
    public ArtLoader? Art { get; private set; }
    public GumpLoader? Gumps { get; private set; }
    public AnimLoader? Animations { get; private set; }
    public TileDataLoader? TileData { get; private set; }
    public HuesLoader? Hues { get; private set; }
    public FontsLoader? Fonts { get; private set; }
    public TexmapLoader? Texmaps { get; private set; }
    
    public bool IsInitialized { get; private set; }
    public string DataPath => _config.DataPath;
    public List<string> LoadErrors { get; } = new();
    
    public UOAssetManager(GraphicsDevice graphics, string dataPath)
    {
        _graphics = graphics;
        _config = new UOAssetConfig { DataPath = dataPath };
    }
    
    /// <summary>
    /// Initialize all asset loaders
    /// </summary>
    public bool Initialize()
    {
        LoadErrors.Clear();
        
        // Initialize debug logging
        DebugLog.Initialize();
        
        void Log(string msg)
        {
            DebugLog.Write(msg);
        }
        
        // Validate path exists
        if (!Directory.Exists(_config.DataPath))
        {
            LoadErrors.Add($"Data path does not exist: {_config.DataPath}");
            return false;
        }
        
        // Log what files we find
        Log($"Scanning UO data path: {_config.DataPath}");
        var files = Directory.GetFiles(_config.DataPath, "*.mul").Concat(
                    Directory.GetFiles(_config.DataPath, "*.uop")).Take(30);
        foreach (var f in files)
            Log($"  Found: {Path.GetFileName(f)}");
        
        // Load Art (required)
        Log("Loading Art...");
        Art = new ArtLoader(_graphics, _config.ArtMulPath, _config.ArtIdxPath);
        if (!Art.Load())
        {
            LoadErrors.Add("Failed to load art assets");
            Art = null;
        }
        else
        {
            // Diagnostic: Test loading specific indices
            Log($"Art diagnostic - Testing index lookups:");
            Log($"  Art.IsUsingUop = {Art.IsUsingUop}");
            Log($"  Art.EntryCount = {Art.EntryCount}");
            
            // Test land tiles (indices 0-16383)
            var landTest1 = Art.GetLandTile(0);
            var landTest2 = Art.GetLandTile(3);
            var landTest3 = Art.GetLandTile(100);
            Log($"  LandTile(0) = {(landTest1 != null ? "OK" : "NULL")}");
            Log($"  LandTile(3) = {(landTest2 != null ? "OK" : "NULL")}");
            Log($"  LandTile(100) = {(landTest3 != null ? "OK" : "NULL")}");
            
            // Test static items (start at index 16384 in UOP)
            var staticTest1 = Art.GetStaticItem(1);
            var staticTest2 = Art.GetStaticItem(130);
            var staticTest3 = Art.GetStaticItem(1000);
            Log($"  StaticItem(1) = {(staticTest1 != null ? "OK" : "NULL")}");
            Log($"  StaticItem(130) = {(staticTest2 != null ? "OK" : "NULL")}");
            Log($"  StaticItem(1000) = {(staticTest3 != null ? "OK" : "NULL")}");
        }
        
        // Load Gumps (optional)
        Log("Loading Gumps...");
        Log($"  GumpArtPath = {_config.GumpArtPath} (exists: {File.Exists(_config.GumpArtPath)})");
        Log($"  GumpIdxPath = {_config.GumpIdxPath} (exists: {File.Exists(_config.GumpIdxPath)})");
        Log($"  GumpLegacyUopPath = {_config.GumpLegacyUopPath} (exists: {File.Exists(_config.GumpLegacyUopPath)})");
        
        Gumps = new GumpLoader(_graphics, _config.GumpArtPath, _config.GumpIdxPath);
        if (!Gumps.Load())
        {
            LoadErrors.Add("Failed to load gump assets");
            Gumps = null;
        }
        else
        {
            // Test loading some common gumps
            Log($"Gumps diagnostic:");
            Log($"  IsUsingUop = {Gumps.IsUsingUop}");
            Log($"  EntryCount = {Gumps.EntryCount}");
            
            int[] testGumpIds = { 0, 1, 2, 5, 10, 100, 2000, 2100, 5000 };
            foreach (var id in testGumpIds)
            {
                var gump = Gumps.GetGump(id);
                Log($"  Gump({id}) = {(gump != null ? $"{gump.Width}x{gump.Height}" : "NULL")}");
            }
        }
        
        // Load Animations (tries all anim*.mul files)
        Log("Loading Animations...");
        Animations = new AnimLoader(_graphics, _config.AnimPath, _config.AnimIdxPath);
        if (!Animations.Load())
        {
            LoadErrors.Add("Failed to load animation assets");
            Animations = null;
        }
        
        // Load TileData (optional but recommended)
        if (File.Exists(_config.TileDataPath))
        {
            TileData = new TileDataLoader();
            if (!TileData.Load(_config.TileDataPath))
            {
                LoadErrors.Add("Failed to load tiledata.mul");
                TileData = null;
            }
        }
        
        // Load Texmaps (for stretched terrain)
        Log("Loading Texmaps...");
        Texmaps = new TexmapLoader(_graphics, _config.DataPath);
        if (!Texmaps.Load())
        {
            LoadErrors.Add("Failed to load texmaps (stretched terrain will use fallback)");
            Texmaps = null;
        }
        else
        {
            Log("  Texmaps loaded successfully");
        }
        
        // Load Hues (optional)
        if (File.Exists(_config.HuesPath))
        {
            Hues = new HuesLoader();
            if (!Hues.Load(_config.HuesPath))
            {
                LoadErrors.Add("Failed to load hues.mul");
                Hues = null;
            }
        }
        
        // Load Fonts (optional but recommended for text)
        Log("Loading Fonts...");
        Fonts = new FontsLoader(_graphics, _config.DataPath);
        Fonts.Load();
        if (Fonts.IsLoaded)
        {
            Log($"  Fonts loaded: {Fonts.FontCount} ASCII fonts, Unicode: {Fonts.UnicodeFontExists(0)}");
        }
        else
        {
            LoadErrors.Add("No font files found (fonts.mul or unifont*.mul)");
        }
        
        IsInitialized = Art != null; // At minimum we need art
        
        if (IsInitialized)
        {
            Log($"UO Assets loaded from: {_config.DataPath}");
            Log($"  Art: {Art?.EntryCount ?? 0} entries, UOP: {Art?.IsUsingUop}");
            Log($"  Gumps: {Gumps?.EntryCount ?? 0} entries, UOP: {Gumps?.IsUsingUop}");
            Log($"  Animations: {Animations?.EntryCount ?? 0} entries");
            Log($"  TileData: {TileData?.LandCount ?? 0} land, {TileData?.StaticItemCount ?? 0} static");
            Log($"  Hues: {Hues?.HueCount ?? 0} entries");
            Log($"  Fonts: {Fonts?.FontCount ?? 0} ASCII, Unicode: {Fonts?.UnicodeFontExists(0) ?? false}");
        }
        
        return IsInitialized;
    }
    
    // ============ Convenience Methods ============
    
    /// <summary>
    /// Get a land tile texture
    /// </summary>
    public Texture2D? GetLandTile(int tileId)
    {
        return Art?.GetLandTile(tileId)?.Texture;
    }
    
    /// <summary>
    /// Get a static item texture
    /// </summary>
    public Texture2D? GetStaticItem(int itemId)
    {
        return Art?.GetStaticItem(itemId)?.Texture;
    }
    
    /// <summary>
    /// Get a static item with full info (including offsets)
    /// </summary>
    public UOArtTexture? GetStaticItemFull(int itemId)
    {
        return Art?.GetStaticItem(itemId);
    }
    
    /// <summary>
    /// Get a gump texture
    /// </summary>
    public Texture2D? GetGump(int gumpId)
    {
        return Gumps?.GetGump(gumpId)?.Texture;
    }
    
    /// <summary>
    /// Get static item properties
    /// </summary>
    public StaticTileData? GetItemData(int itemId)
    {
        return TileData?.GetStaticItem(itemId);
    }
    
    /// <summary>
    /// Get land tile properties
    /// </summary>
    public LandTileData? GetLandData(int tileId)
    {
        return TileData?.GetLandTile(tileId);
    }
    
    /// <summary>
    /// Get a hue color palette
    /// </summary>
    public HueEntry? GetHue(int hueId)
    {
        return Hues?.GetHue(hueId);
    }
    
    /// <summary>
    /// Get an animation
    /// </summary>
    public Animation? GetAnimation(int bodyId, AnimAction action, AnimDirection direction)
    {
        return Animations?.GetAnimation(bodyId, action, direction);
    }
    
    // ============ Preloading ============
    
    /// <summary>
    /// Preload common assets for better performance
    /// </summary>
    public void PreloadCommon()
    {
        Art?.PreloadCommonTiles(500);
        Gumps?.PreloadCommonGumps();
    }
    
    // ============ Cleanup ============
    
    /// <summary>
    /// Clear all cached textures to free memory
    /// </summary>
    public void ClearCaches()
    {
        Art?.ClearCache();
        Gumps?.ClearCache();
        Animations?.ClearCache();
    }
    
    public void Dispose()
    {
        Art?.Dispose();
        Gumps?.Dispose();
        Animations?.Dispose();
        TileData?.Dispose();
        Hues?.Dispose();
        Texmaps?.Dispose();
    }
}

/// <summary>
/// Extension methods for drawing UO assets with MonoGame
/// </summary>
public static class UOAssetDrawExtensions
{
    /// <summary>
    /// Draw a land tile at isometric position
    /// </summary>
    public static void DrawLandTile(this SpriteBatch spriteBatch, UOAssetManager assets, 
        int tileId, Vector2 screenPos, Color? color = null)
    {
        var tex = assets.GetLandTile(tileId);
        if (tex != null)
        {
            // Land tiles are 44x44 diamonds, draw centered
            spriteBatch.Draw(tex, screenPos - new Vector2(22, 22), color ?? Color.White);
        }
    }
    
    /// <summary>
    /// Draw a static item at position (accounting for offsets)
    /// </summary>
    public static void DrawStaticItem(this SpriteBatch spriteBatch, UOAssetManager assets,
        int itemId, Vector2 screenPos, Color? color = null, int hue = 0)
    {
        var art = assets.Art?.GetStaticItem(itemId);
        if (art?.Texture != null)
        {
            // Static items are drawn with their base at the given position
            var drawPos = screenPos - new Vector2(art.OffsetX, art.OffsetY);
            spriteBatch.Draw(art.Texture, drawPos, color ?? Color.White);
        }
    }
    
    /// <summary>
    /// Draw a gump at position
    /// </summary>
    public static void DrawGump(this SpriteBatch spriteBatch, UOAssetManager assets,
        int gumpId, Vector2 pos, Color? color = null)
    {
        var tex = assets.GetGump(gumpId);
        if (tex != null)
        {
            spriteBatch.Draw(tex, pos, color ?? Color.White);
        }
    }
    
    /// <summary>
    /// Draw a gump stretched to a rectangle
    /// </summary>
    public static void DrawGump(this SpriteBatch spriteBatch, UOAssetManager assets,
        int gumpId, Rectangle dest, Color? color = null)
    {
        var tex = assets.GetGump(gumpId);
        if (tex != null)
        {
            spriteBatch.Draw(tex, dest, color ?? Color.White);
        }
    }
}
