using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RealmOfReality.Client.UI;

/// <summary>
/// Renders UI elements - panels, buttons, text
/// </summary>
public class UIRenderer
{
    private readonly SpriteBatch _spriteBatch;
    private readonly Engine.AssetManager _assets;
    private readonly Texture2D _pixel;
    
    /// <summary>
    /// Get the underlying SpriteBatch for custom drawing
    /// </summary>
    public SpriteBatch SpriteBatch => _spriteBatch;
    
    // Built-in bitmap font (7x9 characters)
    private readonly Dictionary<char, Rectangle> _fontGlyphs;
    private Texture2D _fontTexture = null!;
    private const int GlyphWidth = 7;
    private const int GlyphHeight = 9;
    private const int GlyphSpacing = 1;
    
    public UIRenderer(SpriteBatch spriteBatch, Engine.AssetManager assets)
    {
        _spriteBatch = spriteBatch;
        _assets = assets;
        _fontGlyphs = new Dictionary<char, Rectangle>();
        
        // Create 1x1 white pixel for line drawing
        _pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        
        GenerateBitmapFont();
    }
    
    private void GenerateBitmapFont()
    {
        // Generate a simple bitmap font texture
        const string chars = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
        var charsPerRow = 16;
        var rows = (int)Math.Ceiling(chars.Length / (float)charsPerRow);
        var texWidth = charsPerRow * GlyphWidth;
        var texHeight = rows * GlyphHeight;
        
        _fontTexture = new Texture2D(_spriteBatch.GraphicsDevice, texWidth, texHeight);
        var data = new Color[texWidth * texHeight];
        Array.Fill(data, Color.Transparent);
        
        // Generate simple pixel font glyphs
        for (int i = 0; i < chars.Length; i++)
        {
            var col = i % charsPerRow;
            var row = i / charsPerRow;
            var x = col * GlyphWidth;
            var y = row * GlyphHeight;
            
            _fontGlyphs[chars[i]] = new Rectangle(x, y, GlyphWidth, GlyphHeight);
            
            // Draw glyph pixels
            DrawGlyph(data, texWidth, x, y, chars[i]);
        }
        
        _fontTexture.SetData(data);
    }
    
    private void DrawGlyph(Color[] data, int texWidth, int x, int y, char c)
    {
        // Get the 5x7 bitmap pattern for the character
        var pattern = GetCharPattern(c);
        
        for (int py = 0; py < 7; py++)
        {
            for (int px = 0; px < 5; px++)
            {
                if ((pattern[py] & (1 << (4 - px))) != 0)
                {
                    var idx = (y + py + 1) * texWidth + (x + px + 1);
                    if (idx >= 0 && idx < data.Length)
                        data[idx] = Color.White;
                }
            }
        }
    }
    
    private byte[] GetCharPattern(char c)
    {
        // Simple 5x7 font patterns for common characters
        return c switch
        {
            ' ' => new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
            '!' => new byte[] { 0x04, 0x04, 0x04, 0x04, 0x04, 0x00, 0x04 },
            '.' => new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04 },
            ',' => new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x08 },
            ':' => new byte[] { 0x00, 0x04, 0x00, 0x00, 0x00, 0x04, 0x00 },
            ';' => new byte[] { 0x00, 0x04, 0x00, 0x00, 0x00, 0x04, 0x08 },
            '?' => new byte[] { 0x0E, 0x11, 0x01, 0x02, 0x04, 0x00, 0x04 },
            '-' => new byte[] { 0x00, 0x00, 0x00, 0x1F, 0x00, 0x00, 0x00 },
            '_' => new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1F },
            '+' => new byte[] { 0x00, 0x04, 0x04, 0x1F, 0x04, 0x04, 0x00 },
            '=' => new byte[] { 0x00, 0x00, 0x1F, 0x00, 0x1F, 0x00, 0x00 },
            '/' => new byte[] { 0x01, 0x01, 0x02, 0x04, 0x08, 0x10, 0x10 },
            '\\' => new byte[] { 0x10, 0x10, 0x08, 0x04, 0x02, 0x01, 0x01 },
            '(' => new byte[] { 0x02, 0x04, 0x08, 0x08, 0x08, 0x04, 0x02 },
            ')' => new byte[] { 0x08, 0x04, 0x02, 0x02, 0x02, 0x04, 0x08 },
            '[' => new byte[] { 0x0E, 0x08, 0x08, 0x08, 0x08, 0x08, 0x0E },
            ']' => new byte[] { 0x0E, 0x02, 0x02, 0x02, 0x02, 0x02, 0x0E },
            '<' => new byte[] { 0x02, 0x04, 0x08, 0x10, 0x08, 0x04, 0x02 },
            '>' => new byte[] { 0x08, 0x04, 0x02, 0x01, 0x02, 0x04, 0x08 },
            '@' => new byte[] { 0x0E, 0x11, 0x17, 0x15, 0x17, 0x10, 0x0E },
            '#' => new byte[] { 0x0A, 0x0A, 0x1F, 0x0A, 0x1F, 0x0A, 0x0A },
            '$' => new byte[] { 0x04, 0x0F, 0x14, 0x0E, 0x05, 0x1E, 0x04 },
            '%' => new byte[] { 0x18, 0x19, 0x02, 0x04, 0x08, 0x13, 0x03 },
            '&' => new byte[] { 0x0C, 0x12, 0x14, 0x08, 0x15, 0x12, 0x0D },
            '*' => new byte[] { 0x00, 0x04, 0x15, 0x0E, 0x15, 0x04, 0x00 },
            '0' => new byte[] { 0x0E, 0x11, 0x13, 0x15, 0x19, 0x11, 0x0E },
            '1' => new byte[] { 0x04, 0x0C, 0x04, 0x04, 0x04, 0x04, 0x0E },
            '2' => new byte[] { 0x0E, 0x11, 0x01, 0x02, 0x04, 0x08, 0x1F },
            '3' => new byte[] { 0x1F, 0x02, 0x04, 0x02, 0x01, 0x11, 0x0E },
            '4' => new byte[] { 0x02, 0x06, 0x0A, 0x12, 0x1F, 0x02, 0x02 },
            '5' => new byte[] { 0x1F, 0x10, 0x1E, 0x01, 0x01, 0x11, 0x0E },
            '6' => new byte[] { 0x06, 0x08, 0x10, 0x1E, 0x11, 0x11, 0x0E },
            '7' => new byte[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x08, 0x08 },
            '8' => new byte[] { 0x0E, 0x11, 0x11, 0x0E, 0x11, 0x11, 0x0E },
            '9' => new byte[] { 0x0E, 0x11, 0x11, 0x0F, 0x01, 0x02, 0x0C },
            'A' or 'a' => new byte[] { 0x0E, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 },
            'B' or 'b' => new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x11, 0x11, 0x1E },
            'C' or 'c' => new byte[] { 0x0E, 0x11, 0x10, 0x10, 0x10, 0x11, 0x0E },
            'D' or 'd' => new byte[] { 0x1C, 0x12, 0x11, 0x11, 0x11, 0x12, 0x1C },
            'E' or 'e' => new byte[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x1F },
            'F' or 'f' => new byte[] { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x10 },
            'G' or 'g' => new byte[] { 0x0E, 0x11, 0x10, 0x17, 0x11, 0x11, 0x0F },
            'H' or 'h' => new byte[] { 0x11, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 },
            'I' or 'i' => new byte[] { 0x0E, 0x04, 0x04, 0x04, 0x04, 0x04, 0x0E },
            'J' or 'j' => new byte[] { 0x07, 0x02, 0x02, 0x02, 0x02, 0x12, 0x0C },
            'K' or 'k' => new byte[] { 0x11, 0x12, 0x14, 0x18, 0x14, 0x12, 0x11 },
            'L' or 'l' => new byte[] { 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x1F },
            'M' or 'm' => new byte[] { 0x11, 0x1B, 0x15, 0x15, 0x11, 0x11, 0x11 },
            'N' or 'n' => new byte[] { 0x11, 0x19, 0x15, 0x13, 0x11, 0x11, 0x11 },
            'O' or 'o' => new byte[] { 0x0E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E },
            'P' or 'p' => new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x10, 0x10, 0x10 },
            'Q' or 'q' => new byte[] { 0x0E, 0x11, 0x11, 0x11, 0x15, 0x12, 0x0D },
            'R' or 'r' => new byte[] { 0x1E, 0x11, 0x11, 0x1E, 0x14, 0x12, 0x11 },
            'S' or 's' => new byte[] { 0x0F, 0x10, 0x10, 0x0E, 0x01, 0x01, 0x1E },
            'T' or 't' => new byte[] { 0x1F, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04 },
            'U' or 'u' => new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E },
            'V' or 'v' => new byte[] { 0x11, 0x11, 0x11, 0x11, 0x11, 0x0A, 0x04 },
            'W' or 'w' => new byte[] { 0x11, 0x11, 0x11, 0x15, 0x15, 0x1B, 0x11 },
            'X' or 'x' => new byte[] { 0x11, 0x11, 0x0A, 0x04, 0x0A, 0x11, 0x11 },
            'Y' or 'y' => new byte[] { 0x11, 0x11, 0x0A, 0x04, 0x04, 0x04, 0x04 },
            'Z' or 'z' => new byte[] { 0x1F, 0x01, 0x02, 0x04, 0x08, 0x10, 0x1F },
            _ => new byte[] { 0x1F, 0x11, 0x11, 0x11, 0x11, 0x11, 0x1F } // Unknown char box
        };
    }
    
    /// <summary>
    /// Begin UI drawing (screen space, no transform)
    /// </summary>
    public void Begin()
    {
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
    }
    
    /// <summary>
    /// End UI drawing
    /// </summary>
    public void End()
    {
        _spriteBatch.End();
    }
    
    /// <summary>
    /// Draw text at position
    /// </summary>
    public void DrawText(string text, Vector2 position, Color color, float scale = 2f)
    {
        if (string.IsNullOrEmpty(text)) return;
        
        var cursor = position;
        foreach (var c in text.ToUpper())
        {
            if (_fontGlyphs.TryGetValue(c, out var glyph))
            {
                _spriteBatch.Draw(
                    _fontTexture,
                    cursor,
                    glyph,
                    color,
                    0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }
            cursor.X += (GlyphWidth + GlyphSpacing) * scale;
        }
    }
    
    /// <summary>
    /// Draw text centered at position
    /// </summary>
    public void DrawTextCentered(string text, Vector2 position, Color color, float scale = 2f)
    {
        var size = MeasureText(text, scale);
        DrawText(text, new Vector2(position.X - size.X / 2, position.Y - size.Y / 2), color, scale);
    }
    
    /// <summary>
    /// Measure text dimensions
    /// </summary>
    public Vector2 MeasureText(string text, float scale = 2f)
    {
        return new Vector2(
            text.Length * (GlyphWidth + GlyphSpacing) * scale,
            GlyphHeight * scale
        );
    }
    
    /// <summary>
    /// Draw a filled rectangle
    /// </summary>
    public void DrawRectangle(Rectangle rect, Color color)
    {
        _spriteBatch.Draw(_assets.Pixel, rect, color);
    }
    
    /// <summary>
    /// Draw a rectangle outline
    /// </summary>
    public void DrawRectangleOutline(Rectangle rect, Color color, int thickness = 1)
    {
        // Top
        DrawRectangle(new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        // Bottom
        DrawRectangle(new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        // Left
        DrawRectangle(new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        // Right
        DrawRectangle(new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
    
    /// <summary>
    /// Draw a line between two points
    /// </summary>
    public void DrawLine(Vector2 start, Vector2 end, Color color, int thickness = 1)
    {
        var distance = Vector2.Distance(start, end);
        var angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);
        
        _spriteBatch.Draw(
            _pixel,
            start,
            null,
            color,
            angle,
            Vector2.Zero,
            new Vector2(distance, thickness),
            SpriteEffects.None,
            0);
    }
    
    /// <summary>
    /// Draw a texture at a position
    /// </summary>
    public void DrawTexture(Texture2D texture, Rectangle destRect, Color? tint = null)
    {
        _spriteBatch.Draw(texture, destRect, tint ?? Color.White);
    }
    
    /// <summary>
    /// Draw a sprite at a position
    /// </summary>
    public void DrawSprite(Texture2D texture, Vector2 position, Color? tint = null)
    {
        _spriteBatch.Draw(texture, position, tint ?? Color.White);
    }
    
    /// <summary>
    /// Draw a panel with border
    /// </summary>
    public void DrawPanel(Rectangle rect, Color? backgroundColor = null, Color? borderColor = null)
    {
        var bg = backgroundColor ?? Engine.AssetManager.PanelBackground;
        var border = borderColor ?? Engine.AssetManager.BorderColor;
        
        DrawRectangle(rect, bg);
        DrawRectangleOutline(rect, border, 2);
    }
    
    /// <summary>
    /// Draw a button
    /// </summary>
    public void DrawButton(Rectangle rect, string text, bool hover, bool pressed)
    {
        var color = pressed ? Engine.AssetManager.ButtonPressed :
                   hover ? Engine.AssetManager.ButtonHover : Engine.AssetManager.ButtonNormal;
        
        DrawRectangle(rect, color);
        DrawRectangleOutline(rect, Engine.AssetManager.BorderColor, 1);
        
        // Highlight/shadow
        if (!pressed)
        {
            DrawRectangle(new Rectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, 1), new Color(100, 100, 130));
        }
        
        var textPos = new Vector2(
            rect.X + rect.Width / 2,
            rect.Y + rect.Height / 2
        );
        DrawTextCentered(text, textPos, Engine.AssetManager.TextColor);
    }
    
    /// <summary>
    /// Draw a text input box
    /// </summary>
    public void DrawTextBox(Rectangle rect, string text, bool focused, bool isPassword = false)
    {
        var bgColor = new Color(30, 30, 40);
        var borderColor = focused ? Color.CornflowerBlue : Engine.AssetManager.BorderColor;
        
        DrawRectangle(rect, bgColor);
        DrawRectangleOutline(rect, borderColor, focused ? 2 : 1);
        
        var displayText = isPassword ? new string('*', text.Length) : text;
        
        // Draw text with cursor
        var textPos = new Vector2(rect.X + 8, rect.Y + rect.Height / 2 - GlyphHeight);
        DrawText(displayText, textPos, Engine.AssetManager.TextColor);
        
        // Blinking cursor
        if (focused && (DateTime.Now.Millisecond / 500) % 2 == 0)
        {
            var cursorX = textPos.X + MeasureText(displayText).X;
            DrawRectangle(new Rectangle((int)cursorX, (int)textPos.Y, 2, GlyphHeight * 2), Color.White);
        }
    }
    
    /// <summary>
    /// Draw a progress bar
    /// </summary>
    public void DrawProgressBar(Rectangle rect, float progress, Color fillColor, Color? bgColor = null)
    {
        var bg = bgColor ?? new Color(40, 40, 50);
        
        DrawRectangle(rect, bg);
        
        var fillWidth = (int)(rect.Width * Math.Clamp(progress, 0, 1));
        if (fillWidth > 0)
        {
            DrawRectangle(new Rectangle(rect.X, rect.Y, fillWidth, rect.Height), fillColor);
        }
        
        DrawRectangleOutline(rect, Engine.AssetManager.BorderColor, 1);
    }
    
    /// <summary>
    /// Check if point is inside rectangle
    /// </summary>
    public bool IsInside(Rectangle rect, Vector2 point)
    {
        return rect.Contains((int)point.X, (int)point.Y);
    }
}
