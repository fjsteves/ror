using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RealmOfReality.Client.Assets;

namespace RealmOfReality.Client.Gumps;

/// <summary>
/// Handles rendering of gump graphics and text using UO fonts
/// </summary>
public class GumpRenderer
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly UOAssetManager? _uoAssets;
    private readonly Texture2D _pixel;
    
    // Font settings
    private const byte DEFAULT_FONT = 1; // Unicode font 1 is common for gumps
    
    // Cache for loaded gump textures
    private readonly Dictionary<int, Texture2D?> _gumpCache = new();
    
    // Fallback textures
    private readonly Texture2D _fallbackTexture;
    
    // Fallback bitmap font (when UO fonts unavailable)
    private readonly Texture2D _fallbackFontTexture;
    private readonly Dictionary<char, Rectangle> _fallbackGlyphs;
    private const int FallbackGlyphWidth = 7;
    private const int FallbackGlyphHeight = 9;
    
    public GumpRenderer(GraphicsDevice graphicsDevice, UOAssetManager? uoAssets)
    {
        _graphicsDevice = graphicsDevice;
        _uoAssets = uoAssets;
        
        // Create pixel texture
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        
        // Create fallback texture for missing gumps
        _fallbackTexture = CreateFallbackTexture(64, 64);
        
        // Create fallback font for when UO fonts aren't available
        _fallbackGlyphs = new Dictionary<char, Rectangle>();
        _fallbackFontTexture = GenerateFallbackFont();
    }
    
    /// <summary>
    /// Check if UO fonts are available
    /// </summary>
    public bool HasUOFonts => _uoAssets?.Fonts?.IsLoaded == true;
    
    /// <summary>
    /// Get line height for text
    /// </summary>
    public int LineHeight => HasUOFonts ? _uoAssets!.Fonts!.GetLineHeight(DEFAULT_FONT) : FallbackGlyphHeight + 2;
    
    private Texture2D CreateFallbackTexture(int width, int height)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isLight = ((x / 8) + (y / 8)) % 2 == 0;
                data[y * width + x] = isLight ? new Color(60, 60, 60) : new Color(40, 40, 40);
            }
        }
        
        texture.SetData(data);
        return texture;
    }
    
    /// <summary>
    /// Get a gump texture, loading from UO assets if available
    /// </summary>
    public Texture2D? GetGumpTexture(int gumpId)
    {
        if (_gumpCache.TryGetValue(gumpId, out var cached))
            return cached;
        
        Texture2D? texture = null;
        
        if (_uoAssets?.Gumps != null)
        {
            texture = _uoAssets.GetGump(gumpId);
        }
        
        _gumpCache[gumpId] = texture;
        return texture;
    }
    
    /// <summary>
    /// Get the size of a gump texture
    /// </summary>
    public (int width, int height) GetGumpSize(int gumpId)
    {
        var texture = GetGumpTexture(gumpId);
        if (texture != null)
            return (texture.Width, texture.Height);
        return (0, 0);
    }
    
    /// <summary>
    /// Draw a gump graphic
    /// </summary>
    public void DrawGump(SpriteBatch spriteBatch, int gumpId, int x, int y, int hue = 0)
    {
        var texture = GetGumpTexture(gumpId) ?? _fallbackTexture;
        var color = hue != 0 ? GetHueColor(hue) : Color.White;
        
        spriteBatch.Draw(texture, new Vector2(x, y), color);
    }
    
    /// <summary>
    /// Draw a gump graphic if it exists, returns false if not found
    /// </summary>
    public bool DrawGumpIfExists(SpriteBatch spriteBatch, int gumpId, int x, int y, int hue = 0)
    {
        var texture = GetGumpTexture(gumpId);
        if (texture == null)
            return false;
        
        var color = hue != 0 ? GetHueColor(hue) : Color.White;
        spriteBatch.Draw(texture, new Vector2(x, y), color);
        return true;
    }
    
    /// <summary>
    /// Draw a resizable gump using 9-slice scaling
    /// </summary>
    public void DrawResizableGump(SpriteBatch spriteBatch, int gumpId, int x, int y, int width, int height)
    {
        var texture = GetGumpTexture(gumpId);
        
        if (texture == null)
        {
            // Fallback: draw a parchment-like bordered rectangle
            DrawRectangle(spriteBatch, x, y, width, height, new Color(60, 50, 40));
            DrawRectangle(spriteBatch, x + 2, y + 2, width - 4, height - 4, new Color(90, 80, 65));
            DrawRectangleOutline(spriteBatch, x, y, width, height, new Color(40, 35, 25), 2);
            DrawRectangleOutline(spriteBatch, x + 2, y + 2, width - 4, height - 4, new Color(110, 100, 80), 1);
            return;
        }
        
        // 9-slice rendering
        int borderX = Math.Min(texture.Width / 4, width / 3);
        int borderY = Math.Min(texture.Height / 4, height / 3);
        
        var srcBorderX = texture.Width / 4;
        var srcBorderY = texture.Height / 4;
        
        // Top-left corner
        spriteBatch.Draw(texture,
            new Rectangle(x, y, borderX, borderY),
            new Rectangle(0, 0, srcBorderX, srcBorderY),
            Color.White);
        
        // Top edge
        spriteBatch.Draw(texture,
            new Rectangle(x + borderX, y, width - borderX * 2, borderY),
            new Rectangle(srcBorderX, 0, texture.Width - srcBorderX * 2, srcBorderY),
            Color.White);
        
        // Top-right corner
        spriteBatch.Draw(texture,
            new Rectangle(x + width - borderX, y, borderX, borderY),
            new Rectangle(texture.Width - srcBorderX, 0, srcBorderX, srcBorderY),
            Color.White);
        
        // Left edge
        spriteBatch.Draw(texture,
            new Rectangle(x, y + borderY, borderX, height - borderY * 2),
            new Rectangle(0, srcBorderY, srcBorderX, texture.Height - srcBorderY * 2),
            Color.White);
        
        // Center
        spriteBatch.Draw(texture,
            new Rectangle(x + borderX, y + borderY, width - borderX * 2, height - borderY * 2),
            new Rectangle(srcBorderX, srcBorderY, texture.Width - srcBorderX * 2, texture.Height - srcBorderY * 2),
            Color.White);
        
        // Right edge
        spriteBatch.Draw(texture,
            new Rectangle(x + width - borderX, y + borderY, borderX, height - borderY * 2),
            new Rectangle(texture.Width - srcBorderX, srcBorderY, srcBorderX, texture.Height - srcBorderY * 2),
            Color.White);
        
        // Bottom-left corner
        spriteBatch.Draw(texture,
            new Rectangle(x, y + height - borderY, borderX, borderY),
            new Rectangle(0, texture.Height - srcBorderY, srcBorderX, srcBorderY),
            Color.White);
        
        // Bottom edge
        spriteBatch.Draw(texture,
            new Rectangle(x + borderX, y + height - borderY, width - borderX * 2, borderY),
            new Rectangle(srcBorderX, texture.Height - srcBorderY, texture.Width - srcBorderX * 2, srcBorderY),
            Color.White);
        
        // Bottom-right corner
        spriteBatch.Draw(texture,
            new Rectangle(x + width - borderX, y + height - borderY, borderX, borderY),
            new Rectangle(texture.Width - srcBorderX, texture.Height - srcBorderY, srcBorderX, srcBorderY),
            Color.White);
    }
    
    /// <summary>
    /// Draw a tiled gump graphic
    /// </summary>
    public void DrawGumpTiled(SpriteBatch spriteBatch, int gumpId, int x, int y, int width, int height)
    {
        var texture = GetGumpTexture(gumpId);
        
        if (texture == null)
        {
            DrawRectangle(spriteBatch, x, y, width, height, new Color(60, 60, 70));
            return;
        }
        
        for (int ty = 0; ty < height; ty += texture.Height)
        {
            for (int tx = 0; tx < width; tx += texture.Width)
            {
                int drawWidth = Math.Min(texture.Width, width - tx);
                int drawHeight = Math.Min(texture.Height, height - ty);
                
                spriteBatch.Draw(texture,
                    new Rectangle(x + tx, y + ty, drawWidth, drawHeight),
                    new Rectangle(0, 0, drawWidth, drawHeight),
                    Color.White);
            }
        }
    }
    
    /// <summary>
    /// Draw an item graphic
    /// </summary>
    public void DrawItem(SpriteBatch spriteBatch, int itemId, int x, int y, int hue = 0)
    {
        var texture = _uoAssets?.GetStaticItem(itemId);
        
        if (texture == null)
        {
            DrawRectangle(spriteBatch, x, y, 44, 44, new Color(80, 60, 40));
            DrawRectangleOutline(spriteBatch, x, y, 44, 44, new Color(120, 100, 80), 1);
            return;
        }
        
        var color = hue != 0 ? GetHueColor(hue) : Color.White;
        spriteBatch.Draw(texture, new Vector2(x, y), color);
    }
    
    /// <summary>
    /// Draw text using UO fonts or fallback
    /// </summary>
    public void DrawText(SpriteBatch spriteBatch, string text, int x, int y, int hue = 0)
    {
        if (string.IsNullOrEmpty(text)) return;
        
        var color = hue != 0 ? GetHueColor(hue) : Color.White;
        
        if (HasUOFonts)
        {
            _uoAssets!.Fonts!.DrawText(spriteBatch, text, x, y, color, DEFAULT_FONT);
        }
        else
        {
            DrawFallbackText(spriteBatch, text, x, y, color);
        }
    }
    
    /// <summary>
    /// Draw text cropped to bounds
    /// </summary>
    public void DrawTextCropped(SpriteBatch spriteBatch, string text, int x, int y, int width, int height, int hue = 0)
    {
        // For now, just draw normally - proper implementation would use scissor rect
        DrawText(spriteBatch, text, x, y, hue);
    }
    
    /// <summary>
    /// Draw wrapped text, returns content height
    /// </summary>
    public int DrawTextWrapped(SpriteBatch spriteBatch, string text, int x, int y, int width, int height, int hue = 0)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        
        var color = hue != 0 ? GetHueColor(hue) : Color.White;
        var lines = WrapText(text, width);
        
        int lineHeight = LineHeight;
        int currentY = y;
        
        foreach (var line in lines)
        {
            if (currentY >= y && currentY < y + height)
            {
                if (HasUOFonts)
                {
                    _uoAssets!.Fonts!.DrawText(spriteBatch, line, x, currentY, color, DEFAULT_FONT);
                }
                else
                {
                    DrawFallbackText(spriteBatch, line, x, currentY, color);
                }
            }
            currentY += lineHeight;
        }
        
        return currentY - y;
    }
    
    /// <summary>
    /// Draw a scrollbar
    /// </summary>
    public void DrawScrollbar(SpriteBatch spriteBatch, int x, int y, int width, int height, int scrollOffset, int contentHeight)
    {
        // Background
        DrawRectangle(spriteBatch, x, y, width, height, new Color(40, 40, 50));
        
        // Thumb
        if (contentHeight > height)
        {
            int thumbHeight = Math.Max(20, (height * height) / contentHeight);
            int maxScroll = contentHeight - height;
            int thumbY = (scrollOffset * (height - thumbHeight)) / maxScroll;
            
            DrawRectangle(spriteBatch, x + 2, y + thumbY, width - 4, thumbHeight, new Color(100, 100, 120));
        }
    }
    
    /// <summary>
    /// Draw a filled rectangle
    /// </summary>
    public void DrawRectangle(SpriteBatch spriteBatch, int x, int y, int width, int height, Color color)
    {
        spriteBatch.Draw(_pixel, new Rectangle(x, y, width, height), color);
    }
    
    /// <summary>
    /// Draw a rectangle outline
    /// </summary>
    public void DrawRectangleOutline(SpriteBatch spriteBatch, int x, int y, int width, int height, Color color, int thickness = 1)
    {
        // Top
        spriteBatch.Draw(_pixel, new Rectangle(x, y, width, thickness), color);
        // Bottom
        spriteBatch.Draw(_pixel, new Rectangle(x, y + height - thickness, width, thickness), color);
        // Left
        spriteBatch.Draw(_pixel, new Rectangle(x, y, thickness, height), color);
        // Right
        spriteBatch.Draw(_pixel, new Rectangle(x + width - thickness, y, thickness, height), color);
    }
    
    /// <summary>
    /// Measure text width
    /// </summary>
    public int MeasureText(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        
        if (HasUOFonts)
        {
            return _uoAssets!.Fonts!.MeasureText(text, DEFAULT_FONT);
        }
        
        return text.Length * (FallbackGlyphWidth + 1);
    }
    
    /// <summary>
    /// Wrap text to fit width
    /// </summary>
    private List<string> WrapText(string text, int maxWidth)
    {
        var lines = new List<string>();
        var words = text.Split(' ');
        var currentLine = "";
        
        foreach (var word in words)
        {
            var testLine = currentLine.Length > 0 ? currentLine + " " + word : word;
            var testWidth = MeasureText(testLine);
            
            if (testWidth > maxWidth && currentLine.Length > 0)
            {
                lines.Add(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }
        
        if (currentLine.Length > 0)
        {
            lines.Add(currentLine);
        }
        
        return lines;
    }
    
    /// <summary>
    /// Convert UO hue ID to Color
    /// </summary>
    public Color GetHueColor(int hue)
    {
        // Try to use UO hues if available
        if (_uoAssets?.Hues != null && hue > 0)
        {
            // This would normally look up the hue in the hues table
            // For now, use a simple mapping of common hues
        }
        
        // Common UO hues (approximate colors)
        return hue switch
        {
            0 => Color.White,
            1 => new Color(255, 255, 255),      // White
            33 => new Color(0, 0, 0),           // Black
            38 => new Color(128, 128, 128),     // Gray
            53 => new Color(255, 0, 0),         // Red
            63 => new Color(255, 128, 0),       // Orange
            68 => new Color(255, 255, 0),       // Yellow
            73 => new Color(0, 255, 0),         // Green
            88 => new Color(0, 255, 255),       // Cyan
            93 => new Color(0, 0, 255),         // Blue
            98 => new Color(255, 0, 255),       // Magenta
            1152 => new Color(180, 180, 180),   // Light gray (common)
            1153 => new Color(100, 100, 100),   // Dark gray (common)
            _ => Color.White
        };
    }
    
    #region Fallback Font
    
    private void DrawFallbackText(SpriteBatch spriteBatch, string text, int x, int y, Color color)
    {
        int curX = x;
        foreach (char c in text)
        {
            char drawChar = c;
            if (!_fallbackGlyphs.ContainsKey(c))
            {
                if (_fallbackGlyphs.ContainsKey(char.ToUpper(c)))
                    drawChar = char.ToUpper(c);
                else
                    drawChar = '?';
            }
            
            if (_fallbackGlyphs.TryGetValue(drawChar, out var srcRect))
            {
                spriteBatch.Draw(_fallbackFontTexture, new Vector2(curX, y), srcRect, color);
            }
            curX += FallbackGlyphWidth + 1;
        }
    }
    
    private Texture2D GenerateFallbackFont()
    {
        const string chars = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
        int charsPerRow = 16;
        int rows = (chars.Length + charsPerRow - 1) / charsPerRow;
        int texWidth = charsPerRow * FallbackGlyphWidth;
        int texHeight = rows * FallbackGlyphHeight;
        
        var texture = new Texture2D(_graphicsDevice, texWidth, texHeight);
        var data = new Color[texWidth * texHeight];
        
        var patterns = GetFontPatterns();
        
        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];
            int col = i % charsPerRow;
            int row = i / charsPerRow;
            int baseX = col * FallbackGlyphWidth;
            int baseY = row * FallbackGlyphHeight;
            
            _fallbackGlyphs[c] = new Rectangle(baseX, baseY, FallbackGlyphWidth, FallbackGlyphHeight);
            
            if (patterns.TryGetValue(c, out var pattern))
            {
                for (int py = 0; py < pattern.Length && py < FallbackGlyphHeight; py++)
                {
                    for (int px = 0; px < FallbackGlyphWidth; px++)
                    {
                        if (px < pattern[py].Length && pattern[py][px] == '#')
                        {
                            data[(baseY + py) * texWidth + baseX + px] = Color.White;
                        }
                    }
                }
            }
        }
        
        texture.SetData(data);
        return texture;
    }
    
    private Dictionary<char, string[]> GetFontPatterns()
    {
        return new Dictionary<char, string[]>
        {
            [' '] = new[] { "", "", "", "", "", "", "", "", "" },
            ['A'] = new[] { " ### ", "#   #", "#   #", "#####", "#   #", "#   #", "#   #", "", "" },
            ['B'] = new[] { "#### ", "#   #", "#### ", "#   #", "#   #", "#### ", "", "", "" },
            ['C'] = new[] { " ### ", "#   #", "#    ", "#    ", "#   #", " ### ", "", "", "" },
            ['D'] = new[] { "#### ", "#   #", "#   #", "#   #", "#   #", "#### ", "", "", "" },
            ['E'] = new[] { "#####", "#    ", "#### ", "#    ", "#    ", "#####", "", "", "" },
            ['F'] = new[] { "#####", "#    ", "#### ", "#    ", "#    ", "#    ", "", "", "" },
            ['G'] = new[] { " ### ", "#    ", "# ###", "#   #", "#   #", " ### ", "", "", "" },
            ['H'] = new[] { "#   #", "#   #", "#####", "#   #", "#   #", "#   #", "", "", "" },
            ['I'] = new[] { " ### ", "  #  ", "  #  ", "  #  ", "  #  ", " ### ", "", "", "" },
            ['J'] = new[] { "  ###", "   # ", "   # ", "#  # ", "#  # ", " ##  ", "", "", "" },
            ['K'] = new[] { "#   #", "#  # ", "###  ", "#  # ", "#   #", "#   #", "", "", "" },
            ['L'] = new[] { "#    ", "#    ", "#    ", "#    ", "#    ", "#####", "", "", "" },
            ['M'] = new[] { "#   #", "## ##", "# # #", "#   #", "#   #", "#   #", "", "", "" },
            ['N'] = new[] { "#   #", "##  #", "# # #", "#  ##", "#   #", "#   #", "", "", "" },
            ['O'] = new[] { " ### ", "#   #", "#   #", "#   #", "#   #", " ### ", "", "", "" },
            ['P'] = new[] { "#### ", "#   #", "#### ", "#    ", "#    ", "#    ", "", "", "" },
            ['Q'] = new[] { " ### ", "#   #", "#   #", "# # #", "#  # ", " ## #", "", "", "" },
            ['R'] = new[] { "#### ", "#   #", "#### ", "#  # ", "#   #", "#   #", "", "", "" },
            ['S'] = new[] { " ####", "#    ", " ### ", "    #", "    #", "#### ", "", "", "" },
            ['T'] = new[] { "#####", "  #  ", "  #  ", "  #  ", "  #  ", "  #  ", "", "", "" },
            ['U'] = new[] { "#   #", "#   #", "#   #", "#   #", "#   #", " ### ", "", "", "" },
            ['V'] = new[] { "#   #", "#   #", "#   #", " # # ", " # # ", "  #  ", "", "", "" },
            ['W'] = new[] { "#   #", "#   #", "# # #", "# # #", "## ##", "#   #", "", "", "" },
            ['X'] = new[] { "#   #", " # # ", "  #  ", " # # ", "#   #", "#   #", "", "", "" },
            ['Y'] = new[] { "#   #", " # # ", "  #  ", "  #  ", "  #  ", "  #  ", "", "", "" },
            ['Z'] = new[] { "#####", "   # ", "  #  ", " #   ", "#    ", "#####", "", "", "" },
            ['a'] = new[] { "", " ### ", "    #", " ####", "#   #", " ####", "", "", "" },
            ['b'] = new[] { "#    ", "#    ", "#### ", "#   #", "#   #", "#### ", "", "", "" },
            ['c'] = new[] { "", " ### ", "#    ", "#    ", "#    ", " ### ", "", "", "" },
            ['d'] = new[] { "    #", "    #", " ####", "#   #", "#   #", " ####", "", "", "" },
            ['e'] = new[] { "", " ### ", "#   #", "#####", "#    ", " ### ", "", "", "" },
            ['f'] = new[] { "  ## ", " #   ", "#### ", " #   ", " #   ", " #   ", "", "", "" },
            ['g'] = new[] { "", " ####", "#   #", " ####", "    #", " ### ", "", "", "" },
            ['h'] = new[] { "#    ", "#    ", "#### ", "#   #", "#   #", "#   #", "", "", "" },
            ['i'] = new[] { "  #  ", "     ", " ##  ", "  #  ", "  #  ", " ### ", "", "", "" },
            ['j'] = new[] { "   # ", "     ", "  ## ", "   # ", "   # ", " ##  ", "", "", "" },
            ['k'] = new[] { "#    ", "#  # ", "###  ", "#  # ", "#   #", "#   #", "", "", "" },
            ['l'] = new[] { " ##  ", "  #  ", "  #  ", "  #  ", "  #  ", " ### ", "", "", "" },
            ['m'] = new[] { "", "#####", "# # #", "# # #", "#   #", "#   #", "", "", "" },
            ['n'] = new[] { "", "#### ", "#   #", "#   #", "#   #", "#   #", "", "", "" },
            ['o'] = new[] { "", " ### ", "#   #", "#   #", "#   #", " ### ", "", "", "" },
            ['p'] = new[] { "", "#### ", "#   #", "#### ", "#    ", "#    ", "", "", "" },
            ['q'] = new[] { "", " ####", "#   #", " ####", "    #", "    #", "", "", "" },
            ['r'] = new[] { "", " # ##", " ##  ", " #   ", " #   ", " #   ", "", "", "" },
            ['s'] = new[] { "", " ####", "#    ", " ### ", "    #", "#### ", "", "", "" },
            ['t'] = new[] { " #   ", "#### ", " #   ", " #   ", " #   ", "  ## ", "", "", "" },
            ['u'] = new[] { "", "#   #", "#   #", "#   #", "#   #", " ####", "", "", "" },
            ['v'] = new[] { "", "#   #", "#   #", " # # ", " # # ", "  #  ", "", "", "" },
            ['w'] = new[] { "", "#   #", "#   #", "# # #", "# # #", " # # ", "", "", "" },
            ['x'] = new[] { "", "#   #", " # # ", "  #  ", " # # ", "#   #", "", "", "" },
            ['y'] = new[] { "", "#   #", " # # ", "  #  ", " #   ", "#    ", "", "", "" },
            ['z'] = new[] { "", "#####", "   # ", "  #  ", " #   ", "#####", "", "", "" },
            ['0'] = new[] { " ### ", "#  ##", "# # #", "##  #", "#   #", " ### ", "", "", "" },
            ['1'] = new[] { "  #  ", " ##  ", "  #  ", "  #  ", "  #  ", " ### ", "", "", "" },
            ['2'] = new[] { " ### ", "#   #", "   # ", "  #  ", " #   ", "#####", "", "", "" },
            ['3'] = new[] { "#####", "   # ", "  #  ", "   # ", "#   #", " ### ", "", "", "" },
            ['4'] = new[] { "   # ", "  ## ", " # # ", "#####", "   # ", "   # ", "", "", "" },
            ['5'] = new[] { "#####", "#    ", "#### ", "    #", "#   #", " ### ", "", "", "" },
            ['6'] = new[] { "  ## ", " #   ", "#### ", "#   #", "#   #", " ### ", "", "", "" },
            ['7'] = new[] { "#####", "    #", "   # ", "  #  ", " #   ", " #   ", "", "", "" },
            ['8'] = new[] { " ### ", "#   #", " ### ", "#   #", "#   #", " ### ", "", "", "" },
            ['9'] = new[] { " ### ", "#   #", " ####", "    #", "   # ", " ##  ", "", "", "" },
            [':'] = new[] { "", "  #  ", "  #  ", "     ", "  #  ", "  #  ", "", "", "" },
            ['.'] = new[] { "", "", "", "", "", "  #  ", "", "", "" },
            [','] = new[] { "", "", "", "", "  #  ", "  #  ", " #   ", "", "" },
            ['!'] = new[] { "  #  ", "  #  ", "  #  ", "  #  ", "     ", "  #  ", "", "", "" },
            ['?'] = new[] { " ### ", "#   #", "   # ", "  #  ", "     ", "  #  ", "", "", "" },
            ['-'] = new[] { "", "", "", "#####", "", "", "", "", "" },
            ['+'] = new[] { "", "  #  ", "  #  ", "#####", "  #  ", "  #  ", "", "", "" },
            ['='] = new[] { "", "", "#####", "", "#####", "", "", "", "" },
            ['/'] = new[] { "    #", "   # ", "  #  ", " #   ", "#    ", "", "", "", "" },
            ['('] = new[] { "  #  ", " #   ", "#    ", "#    ", " #   ", "  #  ", "", "", "" },
            [')'] = new[] { "  #  ", "   # ", "    #", "    #", "   # ", "  #  ", "", "", "" },
            ['['] = new[] { " ### ", " #   ", " #   ", " #   ", " #   ", " ### ", "", "", "" },
            [']'] = new[] { " ### ", "   # ", "   # ", "   # ", "   # ", " ### ", "", "", "" },
            ['\''] = new[] { "  #  ", "  #  ", "", "", "", "", "", "", "" },
            ['"'] = new[] { " # # ", " # # ", "", "", "", "", "", "", "" },
            ['_'] = new[] { "", "", "", "", "", "", "#####", "", "" },
            ['<'] = new[] { "   # ", "  #  ", " #   ", "  #  ", "   # ", "", "", "", "" },
            ['>'] = new[] { " #   ", "  #  ", "   # ", "  #  ", " #   ", "", "", "", "" },
        };
    }
    
    #endregion
}
