using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RealmOfReality.Client.Assets;

namespace RealmOfReality.Client.Engine;

/// <summary>
/// Manages game assets - textures, fonts, sounds
/// All assets are procedurally generated (no content pipeline needed)
/// </summary>
public class AssetManager : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Dictionary<string, Texture2D> _textures = new();
    
    // UO Assets (optional - for authentic graphics)
    private Assets.UOAssetManager? _uoAssets;
    
    /// <summary>
    /// Set the UO asset manager for authentic UO graphics
    /// </summary>
    public void SetUOAssets(Assets.UOAssetManager? uoAssets)
    {
        _uoAssets = uoAssets;
        
        // Load UO cursors from art.mul
        if (_uoAssets?.Art != null)
        {
            LoadUOCursors();
        }
    }
    
    /// <summary>
    /// Load cursor graphics from UO art assets
    /// </summary>
    private void LoadUOCursors()
    {
        if (_uoAssets?.Art == null) return;
        
        // Try to load each cursor type from art.mul
        UOCursorNormal = _uoAssets.GetStaticItem(UOCursorIds.NormalCursor);
        UOCursorWar = _uoAssets.GetStaticItem(UOCursorIds.WarCursor);
        UOCursorTargetNeutral = _uoAssets.GetStaticItem(UOCursorIds.TargetNeutral);
        UOCursorTargetHarmful = _uoAssets.GetStaticItem(UOCursorIds.TargetHarmful);
        UOCursorTargetBeneficial = _uoAssets.GetStaticItem(UOCursorIds.TargetBeneficial);
        
        // Load directional cursors (8 directions)
        for (int i = 0; i < 8; i++)
        {
            UODirectionalCursors[i] = _uoAssets.GetStaticItem(UOCursorIds.DirectionalBase + i);
            UOWarDirectionalCursors[i] = _uoAssets.GetStaticItem(UOCursorIds.WarDirectionalBase + i);
        }
        
        // Log what we loaded
        var loaded = new List<string>();
        if (UOCursorNormal != null) loaded.Add("Normal");
        if (UOCursorWar != null) loaded.Add("War");
        if (UOCursorTargetNeutral != null) loaded.Add("TargetNeutral");
        if (UOCursorTargetHarmful != null) loaded.Add("TargetHarmful");
        if (UOCursorTargetBeneficial != null) loaded.Add("TargetBeneficial");
        
        int dirCount = UODirectionalCursors.Count(c => c != null);
        if (dirCount > 0) loaded.Add($"Directional({dirCount}/8)");
        
        int warDirCount = UOWarDirectionalCursors.Count(c => c != null);
        if (warDirCount > 0) loaded.Add($"WarDirectional({warDirCount}/8)");
        
        if (loaded.Count > 0)
            DebugLog.Write($"Loaded UO cursors: {string.Join(", ", loaded)}");
        else
            DebugLog.Write("No UO cursors loaded - using fallback");
    }
    
    /// <summary>
    /// Get the appropriate cursor texture for the current state
    /// </summary>
    public Texture2D GetCursor(bool warMode, bool targeting, bool harmful = false)
    {
        if (targeting)
        {
            // Use target cursor
            if (harmful && UOCursorTargetHarmful != null)
                return UOCursorTargetHarmful;
            if (UOCursorTargetNeutral != null)
                return UOCursorTargetNeutral;
            return CursorTarget;
        }
        
        if (warMode)
        {
            // War mode cursor
            if (UOCursorWar != null)
                return UOCursorWar;
            return CursorWarMode;
        }
        
        // Normal cursor
        if (UOCursorNormal != null)
            return UOCursorNormal;
        return CursorDefault;
    }
    
    /// <summary>
    /// Get directional cursor based on mouse position relative to player
    /// Direction: 0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW
    /// </summary>
    public Texture2D GetDirectionalCursor(int direction, bool warMode)
    {
        direction = Math.Clamp(direction, 0, 7);
        
        if (warMode)
        {
            // Try war mode directional cursor
            var warDir = UOWarDirectionalCursors[direction];
            if (warDir != null) return warDir;
            
            // Fall back to regular war cursor
            if (UOCursorWar != null) return UOCursorWar;
            return CursorWarMode;
        }
        
        // Try normal directional cursor
        var dir = UODirectionalCursors[direction];
        if (dir != null) return dir;
        
        // Fall back to normal cursor
        if (UOCursorNormal != null) return UOCursorNormal;
        return CursorDefault;
    }
    
    /// <summary>
    /// Calculate direction (0-7) from angle
    /// </summary>
    public static int AngleToDirection(float angleRadians)
    {
        // Convert to degrees and normalize to 0-360
        float degrees = angleRadians * 180f / MathF.PI;
        degrees = ((degrees % 360) + 360) % 360;
        
        // Each direction covers 45 degrees, with 0 degrees = East
        // Shift by 22.5 to center each direction
        // Map: E=2, SE=3, S=4, SW=5, W=6, NW=7, N=0, NE=1
        int dir = (int)((degrees + 22.5f) / 45f) % 8;
        
        // Remap from standard angle to UO direction order (N=0)
        // Standard: E=0, SE=1, S=2, SW=3, W=4, NW=5, N=6, NE=7
        // UO: N=0, NE=1, E=2, SE=3, S=4, SW=5, W=6, NW=7
        int[] remap = { 2, 3, 4, 5, 6, 7, 0, 1 };
        return remap[dir];
    }
    
    // Default generated textures
    public Texture2D Pixel { get; private set; } = null!;
    public Texture2D TileGrass { get; private set; } = null!;
    public Texture2D TileWater { get; private set; } = null!;
    public Texture2D TileStone { get; private set; } = null!;
    public Texture2D TileSand { get; private set; } = null!;
    public Texture2D TileDirt { get; private set; } = null!;
    public Texture2D TileMountain { get; private set; } = null!;
    public Texture2D TileHighlight { get; private set; } = null!;
    public Texture2D PlayerSprite { get; private set; } = null!;
    public Texture2D NpcSprite { get; private set; } = null!;
    public Texture2D DragonSprite { get; private set; } = null!;
    public Texture2D GoblinSprite { get; private set; } = null!;
    public Texture2D SkeletonSprite { get; private set; } = null!;
    public Texture2D WolfSprite { get; private set; } = null!;
    public Texture2D HealerSprite { get; private set; } = null!;
    public Texture2D AnkhSprite { get; private set; } = null!;
    public Texture2D CorpseSprite { get; private set; } = null!;
    public Texture2D TreeSprite { get; private set; } = null!;
    public Texture2D PineTreeSprite { get; private set; } = null!;
    public Texture2D BushSprite { get; private set; } = null!;
    public Texture2D FlowerSprite { get; private set; } = null!;
    public Texture2D RockSprite { get; private set; } = null!;
    public Texture2D ButtonTexture { get; private set; } = null!;
    public Texture2D PanelTexture { get; private set; } = null!;
    public Texture2D TextBoxTexture { get; private set; } = null!;
    
    // Cursors (UO-style)
    public Texture2D CursorDefault { get; private set; } = null!;
    public Texture2D CursorTarget { get; private set; } = null!;
    public Texture2D CursorWarMode { get; private set; } = null!;
    public Texture2D CursorPick { get; private set; } = null!;
    
    // UO Cursor IDs (from art.mul as static items)
    public static class UOCursorIds
    {
        public const int NormalCursor = 8298;       // Standard pointer/hand
        public const int WarCursor = 8305;          // War mode cursor
        public const int TargetNeutral = 8310;      // Target cursor - neutral
        public const int TargetHarmful = 8311;      // Target cursor - harmful (red)
        public const int TargetBeneficial = 8312;   // Target cursor - beneficial
        public const int PickUp = 8299;             // Pick up cursor
        
        // Directional movement cursors (North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest)
        // These start at different base IDs depending on UO version
        public const int DirectionalBase = 8300;    // Base for directional cursors
        // Alternative directional cursors (war mode style)
        public const int WarDirectionalBase = 8306; // War mode directional base
    }
    
    // Loaded UO cursors
    public Texture2D? UOCursorNormal { get; private set; }
    public Texture2D? UOCursorWar { get; private set; }
    public Texture2D? UOCursorTargetNeutral { get; private set; }
    public Texture2D? UOCursorTargetHarmful { get; private set; }
    public Texture2D? UOCursorTargetBeneficial { get; private set; }
    
    // Directional cursors (8 directions: N, NE, E, SE, S, SW, W, NW)
    public Texture2D?[] UODirectionalCursors { get; private set; } = new Texture2D?[8];
    public Texture2D?[] UOWarDirectionalCursors { get; private set; } = new Texture2D?[8];
    
    // Spell icons
    public Texture2D SpellMagicArrow { get; private set; } = null!;
    public Texture2D SpellFireball { get; private set; } = null!;
    public Texture2D SpellHeal { get; private set; } = null!;
    public Texture2D SpellEnergyBolt { get; private set; } = null!;
    
    public Texture2D? GetSpellIcon(int spellId)
    {
        // Try to get UO spell icon first
        if (_uoAssets?.Gumps != null)
        {
            // UO spell icons start at gump ID 2240
            var gumpId = Assets.UOSpellIcons.SmallIconBase + spellId;
            var texture = _uoAssets.GetGump(gumpId);
            if (texture != null)
                return texture;
        }
        
        // Fall back to generated icons
        return spellId switch
        {
            5 => SpellMagicArrow,
            18 => SpellFireball,
            29 => SpellHeal,
            42 => SpellEnergyBolt,
            _ => null
        };
    }
    
    // UI Colors
    public static readonly Color PanelBackground = new(40, 40, 50, 230);
    public static readonly Color ButtonNormal = new(60, 60, 80);
    public static readonly Color ButtonHover = new(80, 80, 110);
    public static readonly Color ButtonPressed = new(50, 50, 70);
    public static readonly Color TextColor = Color.White;
    public static readonly Color TextColorDim = new(180, 180, 180);
    public static readonly Color BorderColor = new(100, 100, 120);
    
    public AssetManager(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
    }
    
    public void LoadContent()
    {
        // Generate placeholder textures
        GeneratePlaceholderTextures();
    }
    
    private void GeneratePlaceholderTextures()
    {
        // 1x1 white pixel for drawing primitives
        Pixel = new Texture2D(_graphicsDevice, 1, 1);
        Pixel.SetData(new[] { Color.White });
        
        // Isometric tile dimensions (UO uses 44x44 diamond tiles)
        const int tileWidth = 44;
        const int tileHeight = 44;
        
        // Generate isometric tiles
        TileGrass = GenerateIsometricTile(tileWidth, tileHeight, new Color(60, 140, 60), new Color(50, 120, 50));
        TileWater = GenerateIsometricTile(tileWidth, tileHeight, new Color(40, 80, 180), new Color(30, 60, 150), animated: true);
        TileStone = GenerateIsometricTile(tileWidth, tileHeight, new Color(120, 120, 120), new Color(100, 100, 100));
        TileSand = GenerateIsometricTile(tileWidth, tileHeight, new Color(210, 190, 130), new Color(190, 170, 110));
        TileDirt = GenerateIsometricTile(tileWidth, tileHeight, new Color(130, 90, 50), new Color(110, 70, 40));
        TileMountain = GenerateIsometricTile(tileWidth, tileHeight, new Color(90, 80, 70), new Color(70, 60, 50));
        TileHighlight = GenerateIsometricTileOutline(tileWidth, tileHeight, Color.Yellow);
        
        // Generate sprites
        PlayerSprite = GenerateCharacterSprite(32, 48, new Color(80, 100, 200), new Color(200, 150, 100));
        NpcSprite = GenerateCharacterSprite(32, 48, new Color(150, 80, 80), new Color(180, 140, 100));
        DragonSprite = GenerateDragonSprite(64, 64);
        GoblinSprite = GenerateCharacterSprite(24, 36, new Color(60, 120, 60), new Color(100, 180, 100));
        SkeletonSprite = GenerateCharacterSprite(28, 44, new Color(200, 200, 200), new Color(230, 230, 230));
        WolfSprite = GenerateWolfSprite(36, 28);
        HealerSprite = GenerateHealerSprite(32, 52);
        AnkhSprite = GenerateAnkhSprite(28, 48);
        CorpseSprite = GenerateCorpseSprite(36, 20);
        TreeSprite = GenerateTreeSprite(48, 80);
        PineTreeSprite = GeneratePineTreeSprite(40, 90);
        BushSprite = GenerateBushSprite(28, 20);
        FlowerSprite = GenerateFlowerSprite(16, 20);
        RockSprite = GenerateRockSprite(32, 24);
        
        // Generate UI textures
        ButtonTexture = GenerateButtonTexture(200, 40);
        PanelTexture = GeneratePanelTexture(400, 300);
        TextBoxTexture = GenerateTextBoxTexture(250, 30);
        
        // Generate spell icons
        SpellMagicArrow = GenerateSpellIcon(32, 32, new Color(100, 150, 255), "arrow");
        SpellFireball = GenerateSpellIcon(32, 32, new Color(255, 100, 50), "fire");
        SpellHeal = GenerateSpellIcon(32, 32, new Color(100, 255, 100), "cross");
        SpellEnergyBolt = GenerateSpellIcon(32, 32, new Color(200, 100, 255), "bolt");
        
        // Generate cursors (UO-style)
        CursorDefault = GenerateDefaultCursor();
        CursorTarget = GenerateTargetCursor();
        CursorWarMode = GenerateWarModeCursor();
        CursorPick = GeneratePickCursor();
    }
    
    private Texture2D GenerateDefaultCursor()
    {
        // UO-style gauntlet/hand cursor
        var width = 32;
        var height = 32;
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        
        var outline = new Color(40, 40, 50);
        var metal = new Color(140, 140, 160);
        var metalLight = new Color(180, 180, 200);
        var metalDark = new Color(100, 100, 120);
        
        // Draw a pointing hand/gauntlet shape
        // Finger pointing up-left
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 10; x++)
            {
                var px = x + 2;
                var py = y + 2;
                if (py < height && px < width)
                {
                    if (y < 2 || x < 2 || x > 7 || y > 13)
                        data[py * width + px] = outline;
                    else if (x < 4)
                        data[py * width + px] = metalDark;
                    else if (x > 5)
                        data[py * width + px] = metalLight;
                    else
                        data[py * width + px] = metal;
                }
            }
        }
        
        // Wrist/gauntlet part
        for (int y = 14; y < 28; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                var px = x + 4;
                var py = y;
                if (py < height && px < width && py >= 0 && px >= 0)
                {
                    if (y == 14 || y == 27 || x == 0 || x == 15)
                        data[py * width + px] = outline;
                    else if (x < 5)
                        data[py * width + px] = metalDark;
                    else if (x > 10)
                        data[py * width + px] = metalLight;
                    else
                        data[py * width + px] = metal;
                }
            }
        }
        
        texture.SetData(data);
        return texture;
    }
    
    private Texture2D GenerateTargetCursor()
    {
        // UO-style targeting crosshair
        var size = 32;
        var texture = new Texture2D(_graphicsDevice, size, size);
        var data = new Color[size * size];
        
        var color = new Color(200, 200, 50); // Golden yellow
        var outline = new Color(60, 60, 30);
        var center = size / 2;
        
        // Draw crosshair
        for (int i = 0; i < size; i++)
        {
            // Skip center area
            if (Math.Abs(i - center) > 4)
            {
                // Horizontal line
                data[center * size + i] = color;
                if (center - 1 >= 0) data[(center - 1) * size + i] = outline;
                if (center + 1 < size) data[(center + 1) * size + i] = outline;
                
                // Vertical line
                data[i * size + center] = color;
                if (center - 1 >= 0) data[i * size + center - 1] = outline;
                if (center + 1 < size) data[i * size + center + 1] = outline;
            }
        }
        
        // Draw circle in center
        var radius = 5;
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                var dist = Math.Sqrt(x * x + y * y);
                if (dist >= radius - 1.5 && dist <= radius + 0.5)
                {
                    var px = center + x;
                    var py = center + y;
                    if (px >= 0 && px < size && py >= 0 && py < size)
                        data[py * size + px] = color;
                }
            }
        }
        
        texture.SetData(data);
        return texture;
    }
    
    private Texture2D GenerateWarModeCursor()
    {
        // Red-tinted combat cursor (sword/weapon)
        var width = 32;
        var height = 32;
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        
        var blade = new Color(180, 180, 190);
        var bladeDark = new Color(120, 120, 130);
        var hilt = new Color(139, 90, 43); // Brown
        var outline = new Color(40, 40, 50);
        
        // Draw diagonal sword
        for (int i = 0; i < 20; i++)
        {
            var x = 4 + i;
            var y = 4 + i;
            if (x < width && y < height)
            {
                data[y * width + x] = blade;
                if (x + 1 < width) data[y * width + x + 1] = bladeDark;
                if (y + 1 < height) data[(y + 1) * width + x] = outline;
            }
        }
        
        // Hilt/handle
        for (int i = 0; i < 8; i++)
        {
            var x = 20 + i / 2;
            var y = 20 + i / 2;
            if (x < width && y < height)
            {
                // Crossguard
                if (i < 4)
                {
                    data[(y - 2) * width + x + 2] = hilt;
                    data[(y + 2) * width + x - 2] = hilt;
                }
                data[y * width + x] = hilt;
            }
        }
        
        texture.SetData(data);
        return texture;
    }
    
    private Texture2D GeneratePickCursor()
    {
        // Hand for picking up items
        var width = 24;
        var height = 24;
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        
        var skin = new Color(220, 180, 140);
        var skinDark = new Color(180, 140, 100);
        var outline = new Color(60, 40, 30);
        
        // Draw open hand shape
        // Palm
        for (int y = 10; y < 22; y++)
        {
            for (int x = 4; x < 18; x++)
            {
                if (y == 10 || y == 21 || x == 4 || x == 17)
                    data[y * width + x] = outline;
                else
                    data[y * width + x] = skin;
            }
        }
        
        // Fingers (simplified)
        for (int f = 0; f < 4; f++)
        {
            var fx = 6 + f * 3;
            for (int y = 2; y < 12; y++)
            {
                if (fx < width && y < height)
                {
                    data[y * width + fx] = skin;
                    if (fx + 1 < width) data[y * width + fx + 1] = skinDark;
                }
            }
        }
        
        // Thumb
        for (int i = 0; i < 6; i++)
        {
            var x = 2 + i;
            var y = 12 + i / 2;
            if (x < width && y < height)
                data[y * width + x] = skin;
        }
        
        texture.SetData(data);
        return texture;
    }
    
    private Texture2D GenerateSpellIcon(int width, int height, Color color, string shape)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        Array.Fill(data, new Color(30, 30, 40)); // Dark background
        
        var centerX = width / 2;
        var centerY = height / 2;
        var colorDark = new Color(color.R / 2, color.G / 2, color.B / 2);
        
        switch (shape)
        {
            case "arrow":
                // Draw arrow pointing right
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Arrow shaft
                        if (y >= centerY - 3 && y <= centerY + 3 && x >= 4 && x < width - 8)
                            data[y * width + x] = color;
                        // Arrow head
                        var headDist = Math.Abs(y - centerY);
                        if (x >= width - 12 && x < width - 4 && headDist <= (width - 4 - x))
                            data[y * width + x] = color;
                    }
                }
                break;
                
            case "fire":
                // Draw flame
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var dx = Math.Abs(x - centerX);
                        var relY = (float)(height - y) / height;
                        var maxWidth = (int)(8 * relY + 4);
                        if (dx <= maxWidth && y >= 4)
                        {
                            var intensity = 1.0f - (float)dx / maxWidth;
                            var c = y < height / 2 ? new Color(255, 200, 50) : color;
                            data[y * width + x] = c;
                        }
                    }
                }
                break;
                
            case "cross":
                // Draw healing cross
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // Vertical bar
                        if (x >= centerX - 4 && x <= centerX + 4 && y >= 4 && y < height - 4)
                            data[y * width + x] = color;
                        // Horizontal bar
                        if (y >= centerY - 4 && y <= centerY + 4 && x >= 4 && x < width - 4)
                            data[y * width + x] = color;
                    }
                }
                break;
                
            case "bolt":
                // Draw lightning bolt
                for (int y = 4; y < height - 4; y++)
                {
                    var xOff = (y < centerY) ? (centerY - y) / 2 : (y - centerY) / 2;
                    var baseX = (y < centerY) ? centerX - 4 + xOff : centerX + 4 - xOff;
                    for (int w = -2; w <= 2; w++)
                    {
                        var px = baseX + w;
                        if (px >= 0 && px < width)
                            data[y * width + px] = color;
                    }
                }
                break;
        }
        
        // Add border
        for (int y = 0; y < height; y++)
        {
            data[y * width] = colorDark;
            data[y * width + width - 1] = colorDark;
        }
        for (int x = 0; x < width; x++)
        {
            data[x] = colorDark;
            data[(height - 1) * width + x] = colorDark;
        }
        
        texture.SetData(data);
        return texture;
    }
    
    private Texture2D GenerateIsometricTile(int width, int height, Color fillColor, Color shadeColor, bool animated = false)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        
        // Clear to transparent
        Array.Fill(data, Color.Transparent);
        
        var centerX = width / 2;
        var centerY = height / 2;
        
        // Draw diamond shape
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Diamond test
                float dx = Math.Abs(x - centerX) / (float)(width / 2);
                float dy = Math.Abs(y - centerY) / (float)(height / 2);
                
                if (dx + dy <= 1.0f)
                {
                    // Inside diamond - apply gradient for 3D effect
                    float shade = 1.0f - (dy * 0.5f);
                    
                    // Add some noise for texture
                    var noise = animated ? 
                        ((x + y) % 4 == 0 ? 0.1f : 0f) : 
                        ((x * 7 + y * 13) % 17) / 100f;
                    
                    var color = Color.Lerp(fillColor, shadeColor, 1f - shade + noise);
                    
                    // Edge darkening
                    if (dx + dy > 0.9f)
                        color = Color.Lerp(color, Color.Black, 0.3f);
                    
                    data[y * width + x] = color;
                }
            }
        }
        
        texture.SetData(data);
        return texture;
    }
    
    private Texture2D GenerateIsometricTileOutline(int width, int height, Color color)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        Array.Fill(data, Color.Transparent);
        
        var centerX = width / 2;
        var centerY = height / 2;
        
        // Draw diamond outline only
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = Math.Abs(x - centerX) / (float)(width / 2);
                float dy = Math.Abs(y - centerY) / (float)(height / 2);
                float dist = dx + dy;
                
                // Draw outline band
                if (dist > 0.85f && dist <= 1.0f)
                {
                    data[y * width + x] = new Color(color, 0.6f);
                }
            }
        }
        
        texture.SetData(data);
        return texture;
    }
    
    private Texture2D GenerateCharacterSprite(int width, int height, Color bodyColor, Color skinColor)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        Array.Fill(data, Color.Transparent);
        
        var centerX = width / 2;
        
        // Head (circle at top)
        var headRadius = width / 4;
        var headCenterY = headRadius + 2;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Head
                var hdx = x - centerX;
                var hdy = y - headCenterY;
                if (hdx * hdx + hdy * hdy <= headRadius * headRadius)
                {
                    data[y * width + x] = skinColor;
                }
                
                // Body (rectangle below head)
                var bodyTop = headCenterY + headRadius + 2;
                var bodyBottom = height - 8;
                var bodyWidth = width / 3;
                
                if (y >= bodyTop && y <= bodyBottom &&
                    x >= centerX - bodyWidth && x <= centerX + bodyWidth)
                {
                    data[y * width + x] = bodyColor;
                }
                
                // Legs
                if (y > bodyBottom && y < height)
                {
                    if ((x >= centerX - bodyWidth && x < centerX - 2) ||
                        (x > centerX + 2 && x <= centerX + bodyWidth))
                    {
                        data[y * width + x] = bodyColor;
                    }
                }
            }
        }
        
        // Add outline
        AddOutline(data, width, height, new Color(20, 20, 30));
        
        texture.SetData(data);
        return texture;
    }
    
    private Texture2D GenerateTreeSprite(int width, int height)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        Array.Fill(data, Color.Transparent);
        
        var centerX = width / 2;
        var trunkColor = new Color(100, 70, 40);
        var leafColor = new Color(40, 120, 40);
        var leafColorDark = new Color(30, 90, 30);
        
        // Trunk
        var trunkWidth = 6;
        var trunkTop = height / 2;
        for (int y = trunkTop; y < height; y++)
        {
            for (int x = centerX - trunkWidth / 2; x <= centerX + trunkWidth / 2; x++)
            {
                if (x >= 0 && x < width)
                    data[y * width + x] = trunkColor;
            }
        }
        
        // Foliage (layered triangles)
        DrawTriangle(data, width, centerX, 0, 40, 25, leafColor);
        DrawTriangle(data, width, centerX, 15, 34, 22, leafColorDark);
        DrawTriangle(data, width, centerX, 28, 28, 18, leafColor);
        
        AddOutline(data, width, height, new Color(20, 40, 20));
        
        texture.SetData(data);
        return texture;
    }
    
    private void DrawTriangle(Color[] data, int width, int centerX, int topY, int triWidth, int triHeight, Color color)
    {
        for (int y = 0; y < triHeight; y++)
        {
            var rowWidth = (int)(triWidth * ((float)y / triHeight));
            for (int x = centerX - rowWidth / 2; x <= centerX + rowWidth / 2; x++)
            {
                var idx = (topY + y) * width + x;
                if (idx >= 0 && idx < data.Length && x >= 0 && x < width)
                    data[idx] = color;
            }
        }
    }
    
    private Texture2D GenerateRockSprite(int width, int height)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        Array.Fill(data, Color.Transparent);
        
        var rockColor = new Color(100, 100, 110);
        var rockDark = new Color(70, 70, 80);
        
        // Irregular rock shape
        var centerX = width / 2;
        var centerY = height / 2;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var dx = (x - centerX) / (float)(width / 2);
                var dy = (y - centerY) / (float)(height / 2);
                
                // Ellipse with noise
                var noise = ((x * 13 + y * 7) % 11) / 30f;
                if (dx * dx + dy * dy * 1.5f + noise <= 1.0f)
                {
                    var shade = y < centerY ? rockColor : rockDark;
                    data[y * width + x] = shade;
                }
            }
        }
        
        AddOutline(data, width, height, new Color(40, 40, 50));
        
        texture.SetData(data);
        return texture;
    }
    
    private Texture2D GenerateDragonSprite(int width, int height)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        Array.Fill(data, Color.Transparent);
        
        var bodyColor = new Color(180, 50, 50); // Red dragon
        var bodyDark = new Color(140, 30, 30);
        var wingColor = new Color(200, 80, 80);
        var eyeColor = new Color(255, 200, 0);
        
        var centerX = width / 2;
        
        // Body (large ellipse)
        for (int y = height/3; y < height - 8; y++)
        {
            for (int x = 8; x < width - 8; x++)
            {
                var dx = (x - centerX) / (float)(width / 3);
                var dy = (y - height/2) / (float)(height / 4);
                if (dx * dx + dy * dy <= 1.0f)
                {
                    data[y * width + x] = y < height/2 ? bodyColor : bodyDark;
                }
            }
        }
        
        // Head (circle at front)
        var headX = centerX + 16;
        var headY = height / 3;
        var headR = 10;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var dx = x - headX;
                var dy = y - headY;
                if (dx * dx + dy * dy <= headR * headR)
                {
                    data[y * width + x] = bodyColor;
                }
            }
        }
        
        // Eyes
        data[(headY - 2) * width + (headX + 4)] = eyeColor;
        data[(headY + 2) * width + (headX + 4)] = eyeColor;
        
        // Wings (triangles)
        for (int y = 0; y < 20; y++)
        {
            var wingWidth = 20 - y;
            for (int x = 0; x < wingWidth; x++)
            {
                var px = centerX - 10 - x;
                var py = height/4 + y;
                if (px >= 0 && py >= 0 && py < height)
                    data[py * width + px] = wingColor;
            }
        }
        
        // Tail
        for (int i = 0; i < 15; i++)
        {
            var tx = centerX - 20 - i;
            var ty = height/2 + i/2;
            if (tx >= 0 && ty < height)
            {
                data[ty * width + tx] = bodyDark;
                if (ty + 1 < height) data[(ty + 1) * width + tx] = bodyDark;
            }
        }
        
        // Legs
        for (int y = height - 12; y < height - 2; y++)
        {
            data[y * width + (centerX - 8)] = bodyDark;
            data[y * width + (centerX + 8)] = bodyDark;
        }
        
        AddOutline(data, width, height, new Color(80, 20, 20));
        
        texture.SetData(data);
        return texture;
    }
    
    private Texture2D GenerateWolfSprite(int width, int height)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        Array.Fill(data, Color.Transparent);
        
        var furColor = new Color(100, 100, 110);
        var furDark = new Color(70, 70, 80);
        var eyeColor = new Color(200, 180, 50);
        
        var centerX = width / 2;
        var centerY = height / 2;
        
        // Body (horizontal ellipse)
        for (int y = 4; y < height - 4; y++)
        {
            for (int x = 4; x < width - 8; x++)
            {
                var dx = (x - centerX) / (float)(width / 2.5f);
                var dy = (y - centerY) / (float)(height / 3);
                if (dx * dx + dy * dy <= 1.0f)
                {
                    data[y * width + x] = y < centerY ? furColor : furDark;
                }
            }
        }
        
        // Head (circle at front)
        var headX = width - 10;
        var headY = centerY - 2;
        var headR = 6;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var dx = x - headX;
                var dy = y - headY;
                if (dx * dx + dy * dy <= headR * headR)
                {
                    data[y * width + x] = furColor;
                }
            }
        }
        
        // Eye
        data[(headY - 1) * width + (headX + 2)] = eyeColor;
        
        // Legs
        for (int y = centerY + 4; y < height; y++)
        {
            var idx1 = y * width + (centerX - 6);
            var idx2 = y * width + (centerX + 4);
            if (idx1 >= 0 && idx1 < data.Length) data[idx1] = furDark;
            if (idx2 >= 0 && idx2 < data.Length) data[idx2] = furDark;
        }
        
        // Tail
        for (int i = 0; i < 8; i++)
        {
            var tx = 4 + i;
            var ty = centerY - 2 + i / 2;
            if (ty >= 0 && ty < height && tx < width)
                data[ty * width + tx] = furDark;
        }
        
        AddOutline(data, width, height, new Color(40, 40, 50));
        
        texture.SetData(data);
        return texture;
    }
    
    private Texture2D GenerateHealerSprite(int width, int height)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        
        var robeColor = new Color(220, 220, 240);  // White robe
        var robeDark = new Color(180, 180, 200);
        var skinColor = new Color(220, 180, 140);
        var hairColor = new Color(100, 80, 60);
        var crossColor = new Color(200, 50, 50);   // Red cross
        
        var centerX = width / 2;
        
        // Head
        var headY = 6;
        var headR = 5;
        for (int y = headY - headR; y <= headY + headR; y++)
        {
            for (int x = centerX - headR; x <= centerX + headR; x++)
            {
                var dx = x - centerX;
                var dy = y - headY;
                if (dx * dx + dy * dy <= headR * headR && y >= 0 && y < height && x >= 0 && x < width)
                {
                    data[y * width + x] = skinColor;
                }
            }
        }
        
        // Hair
        for (int y = 2; y < 6; y++)
        {
            for (int x = centerX - 4; x <= centerX + 4; x++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                    data[y * width + x] = hairColor;
            }
        }
        
        // Robe (long flowing)
        for (int y = 12; y < height - 2; y++)
        {
            var robeHalfWidth = 6 + (y - 12) / 4;
            for (int x = centerX - robeHalfWidth; x <= centerX + robeHalfWidth; x++)
            {
                if (x >= 0 && x < width)
                {
                    var shade = (x < centerX - 2) ? robeDark : robeColor;
                    data[y * width + x] = shade;
                }
            }
        }
        
        // Red cross on robe
        var crossY = 24;
        for (int dy = -4; dy <= 4; dy++)
        {
            if (crossY + dy >= 0 && crossY + dy < height)
                data[(crossY + dy) * width + centerX] = crossColor;
        }
        for (int dx = -3; dx <= 3; dx++)
        {
            if (centerX + dx >= 0 && centerX + dx < width && crossY >= 0 && crossY < height)
                data[crossY * width + centerX + dx] = crossColor;
        }
        
        AddOutline(data, width, height, new Color(60, 60, 80));
        
        texture.SetData(data);
        return texture;
    }
    
    private Texture2D GenerateAnkhSprite(int width, int height)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        
        var goldColor = new Color(220, 180, 80);
        var goldLight = new Color(255, 220, 120);
        var goldDark = new Color(180, 140, 40);
        
        var centerX = width / 2;
        
        // Ankh loop (top oval)
        var loopY = 10;
        var loopRadiusX = 6;
        var loopRadiusY = 8;
        var holeRadiusX = 3;
        var holeRadiusY = 4;
        
        for (int y = loopY - loopRadiusY; y <= loopY + loopRadiusY; y++)
        {
            for (int x = centerX - loopRadiusX; x <= centerX + loopRadiusX; x++)
            {
                if (x < 0 || x >= width || y < 0 || y >= height) continue;
                
                var dx = (float)(x - centerX) / loopRadiusX;
                var dy = (float)(y - loopY) / loopRadiusY;
                var outerDist = dx * dx + dy * dy;
                
                var hdx = (float)(x - centerX) / holeRadiusX;
                var hdy = (float)(y - loopY) / holeRadiusY;
                var innerDist = hdx * hdx + hdy * hdy;
                
                if (outerDist <= 1.0f && innerDist > 1.0f)
                {
                    var shade = (x < centerX) ? goldDark : goldLight;
                    data[y * width + x] = shade;
                }
            }
        }
        
        // Vertical stem
        for (int y = loopY + loopRadiusY - 2; y < height - 2; y++)
        {
            for (int x = centerX - 2; x <= centerX + 2; x++)
            {
                if (x >= 0 && x < width)
                {
                    var shade = (x < centerX) ? goldDark : goldLight;
                    data[y * width + x] = goldColor;
                }
            }
        }
        
        // Horizontal arms
        var armY = loopY + loopRadiusY + 2;
        for (int x = centerX - 8; x <= centerX + 8; x++)
        {
            for (int y = armY - 2; y <= armY + 2; y++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    data[y * width + x] = goldColor;
                }
            }
        }
        
        AddOutline(data, width, height, new Color(100, 80, 20));
        
        texture.SetData(data);
        return texture;
    }
    
    private Texture2D GenerateCorpseSprite(int width, int height)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        
        var boneColor = new Color(200, 190, 170);
        var boneDark = new Color(150, 140, 120);
        var bloodColor = new Color(100, 20, 20);
        
        var centerX = width / 2;
        
        // Draw bones/remains lying flat
        // Ribcage
        for (int i = 0; i < 5; i++)
        {
            var y = height / 2 - 2 + i;
            for (int x = centerX - 8; x <= centerX + 8; x++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    if (i == 0 || i == 4 || x == centerX - 8 || x == centerX + 8)
                        data[y * width + x] = boneColor;
                    else if (x % 3 == 0)
                        data[y * width + x] = boneDark;
                }
            }
        }
        
        // Skull on one end
        for (int y = height / 2 - 4; y <= height / 2 + 2; y++)
        {
            for (int x = 3; x <= 10; x++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    var dx = x - 6;
                    var dy = y - (height / 2 - 1);
                    if (dx * dx + dy * dy <= 16)
                        data[y * width + x] = boneColor;
                }
            }
        }
        
        // Blood pool underneath
        for (int y = height / 2 + 3; y < height; y++)
        {
            for (int x = centerX - 10; x <= centerX + 10; x++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    var dx = x - centerX;
                    var dy = (y - (height / 2 + 5)) * 2;
                    if (dx * dx + dy * dy <= 80)
                        data[y * width + x] = bloodColor;
                }
            }
        }
        
        AddOutline(data, width, height, new Color(50, 40, 30));
        
        texture.SetData(data);
        return texture;
    }
    
    private void AddOutline(Color[] data, int width, int height, Color outlineColor)
    {
        var outline = new bool[width * height];
        
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                var idx = y * width + x;
                if (data[idx].A == 0)
                {
                    // Check neighbors
                    if (data[(y - 1) * width + x].A > 0 ||
                        data[(y + 1) * width + x].A > 0 ||
                        data[y * width + x - 1].A > 0 ||
                        data[y * width + x + 1].A > 0)
                    {
                        outline[idx] = true;
                    }
                }
            }
        }
        
        for (int i = 0; i < outline.Length; i++)
        {
            if (outline[i])
                data[i] = outlineColor;
        }
    }
    
    private Texture2D GeneratePineTreeSprite(int width, int height)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        
        var trunkColor = new Color(100, 70, 40);
        var leafColor1 = new Color(30, 90, 30);
        var leafColor2 = new Color(40, 110, 40);
        var centerX = width / 2;
        
        // Trunk
        for (int y = height - 15; y < height; y++)
        {
            for (int x = centerX - 3; x <= centerX + 3; x++)
            {
                if (x >= 0 && x < width)
                    data[y * width + x] = trunkColor;
            }
        }
        
        // Triangular tree layers
        for (int layer = 0; layer < 3; layer++)
        {
            var layerY = 10 + layer * 20;
            var layerHeight = 25;
            for (int y = 0; y < layerHeight; y++)
            {
                var halfWidth = (layerHeight - y) / 2 + 3;
                for (int x = centerX - halfWidth; x <= centerX + halfWidth; x++)
                {
                    if (x >= 0 && x < width && (layerY + y) < height - 10)
                    {
                        var color = ((x + y) % 3 == 0) ? leafColor2 : leafColor1;
                        data[(layerY + y) * width + x] = color;
                    }
                }
            }
        }
        
        AddOutline(data, width, height, new Color(20, 50, 20));
        texture.SetData(data);
        return texture;
    }
    
    private Texture2D GenerateBushSprite(int width, int height)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        
        var leafColor1 = new Color(40, 100, 40);
        var leafColor2 = new Color(50, 120, 50);
        var centerX = width / 2;
        var centerY = height / 2;
        var radiusX = width / 2 - 2;
        var radiusY = height / 2 - 2;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var dx = (x - centerX) / (float)radiusX;
                var dy = (y - centerY) / (float)radiusY;
                if (dx * dx + dy * dy <= 1.0f)
                {
                    var color = ((x + y) % 2 == 0) ? leafColor1 : leafColor2;
                    data[y * width + x] = color;
                }
            }
        }
        
        AddOutline(data, width, height, new Color(25, 60, 25));
        texture.SetData(data);
        return texture;
    }
    
    private Texture2D GenerateFlowerSprite(int width, int height)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        
        var stemColor = new Color(60, 120, 40);
        var petalColors = new[] { 
            new Color(255, 100, 100), 
            new Color(255, 200, 100), 
            new Color(200, 100, 255),
            new Color(100, 200, 255)
        };
        var petalColor = petalColors[(width * height) % petalColors.Length];
        var centerColor = new Color(255, 220, 50);
        
        var centerX = width / 2;
        
        // Stem
        for (int y = height / 2; y < height; y++)
        {
            data[y * width + centerX] = stemColor;
        }
        
        // Petals
        var flowerY = height / 3;
        for (int dy = -4; dy <= 4; dy++)
        {
            for (int dx = -4; dx <= 4; dx++)
            {
                var dist = Math.Abs(dx) + Math.Abs(dy);
                if (dist >= 2 && dist <= 4)
                {
                    var px = centerX + dx;
                    var py = flowerY + dy;
                    if (px >= 0 && px < width && py >= 0 && py < height)
                        data[py * width + px] = petalColor;
                }
            }
        }
        
        // Center
        data[flowerY * width + centerX] = centerColor;
        
        texture.SetData(data);
        return texture;
    }
    
    private Texture2D GenerateButtonTexture(int width, int height)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Border
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                {
                    data[y * width + x] = BorderColor;
                }
                // Top edge highlight
                else if (y == 1)
                {
                    data[y * width + x] = new Color(100, 100, 130);
                }
                // Bottom edge shadow
                else if (y == height - 2)
                {
                    data[y * width + x] = new Color(40, 40, 60);
                }
                else
                {
                    data[y * width + x] = ButtonNormal;
                }
            }
        }
        
        texture.SetData(data);
        return texture;
    }
    
    private Texture2D GeneratePanelTexture(int width, int height)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Border (2 pixels)
                if (x < 2 || y < 2 || x >= width - 2 || y >= height - 2)
                {
                    data[y * width + x] = BorderColor;
                }
                else
                {
                    data[y * width + x] = PanelBackground;
                }
            }
        }
        
        texture.SetData(data);
        return texture;
    }
    
    private Texture2D GenerateTextBoxTexture(int width, int height)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        
        var bgColor = new Color(30, 30, 40, 255);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                {
                    data[y * width + x] = BorderColor;
                }
                else
                {
                    data[y * width + x] = bgColor;
                }
            }
        }
        
        texture.SetData(data);
        return texture;
    }
    
    /// <summary>
    /// Get tile texture by terrain type
    /// </summary>
    public Texture2D GetTileTexture(int terrainType)
    {
        return terrainType switch
        {
            0 => TileGrass,
            1 => TileWater,
            2 => TileStone,
            3 => TileSand,
            4 => TileDirt,
            5 => TileMountain,
            _ => TileGrass
        };
    }
    
    public void Dispose()
    {
        Pixel?.Dispose();
        TileGrass?.Dispose();
        TileWater?.Dispose();
        TileStone?.Dispose();
        TileSand?.Dispose();
        TileDirt?.Dispose();
        TileHighlight?.Dispose();
        PlayerSprite?.Dispose();
        NpcSprite?.Dispose();
        TreeSprite?.Dispose();
        RockSprite?.Dispose();
        ButtonTexture?.Dispose();
        PanelTexture?.Dispose();
        TextBoxTexture?.Dispose();
        
        foreach (var texture in _textures.Values)
            texture.Dispose();
        
        _textures.Clear();
    }
}
