using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RealmOfReality.Client.Assets;

/// <summary>
/// Loads and renders UO fonts from fonts.mul (ASCII) and unifont*.mul (Unicode)
/// Based on ClassicUO's FontsLoader implementation
/// </summary>
public class FontsLoader
{
    private const int UNICODE_SPACE_WIDTH = 8;
    private const byte NOPRINT_CHARS = 32;
    
    private readonly GraphicsDevice _graphicsDevice;
    private readonly string _dataPath;
    
    // ASCII font data (from fonts.mul)
    private FontCharacterData[,]? _fontDataASCII;
    private int _fontCount;
    
    // Unicode font data (from unifont*.mul)
    private readonly byte[]?[] _unicodeFontData = new byte[20][];
    private readonly FontCharacterDataUnicode[,] _fontDataUnicode = new FontCharacterDataUnicode[20, 0x10000];
    
    // Texture cache for rendered text
    private readonly Dictionary<string, Texture2D> _textureCache = new();
    
    public int FontCount => _fontCount;
    public bool IsLoaded => _fontCount > 0 || _unicodeFontData[0] != null;
    
    public FontsLoader(GraphicsDevice graphicsDevice, string dataPath)
    {
        _graphicsDevice = graphicsDevice;
        _dataPath = dataPath;
    }
    
    /// <summary>
    /// Load all font files
    /// </summary>
    public void Load()
    {
        LoadASCIIFonts();
        LoadUnicodeFonts();
    }
    
    private void LoadASCIIFonts()
    {
        string fontsPath = Path.Combine(_dataPath, "fonts.mul");
        if (!File.Exists(fontsPath))
            return;
        
        try
        {
            byte[] data = File.ReadAllBytes(fontsPath);
            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);
            
            // First pass: count fonts
            _fontCount = 0;
            while (ms.Position < ms.Length)
            {
                bool exit = false;
                reader.ReadByte(); // Header byte
                
                for (int i = 0; i < 224; i++)
                {
                    if (ms.Position + 3 >= ms.Length)
                    {
                        exit = true;
                        break;
                    }
                    
                    byte w = reader.ReadByte();
                    byte h = reader.ReadByte();
                    reader.ReadByte(); // Unknown
                    
                    int bcount = w * h * 2;
                    if (ms.Position + bcount > ms.Length)
                    {
                        exit = true;
                        break;
                    }
                    
                    ms.Seek(bcount, SeekOrigin.Current);
                }
                
                if (exit) break;
                _fontCount++;
            }
            
            if (_fontCount < 1)
            {
                _fontCount = 0;
                return;
            }
            
            // Second pass: load font data
            _fontDataASCII = new FontCharacterData[_fontCount, 224];
            ms.Seek(0, SeekOrigin.Begin);
            
            for (int font = 0; font < _fontCount; font++)
            {
                reader.ReadByte(); // Header
                
                for (int charIndex = 0; charIndex < 224; charIndex++)
                {
                    if (ms.Position + 3 >= ms.Length)
                        continue;
                    
                    byte w = reader.ReadByte();
                    byte h = reader.ReadByte();
                    reader.ReadByte(); // Unknown
                    
                    ushort[] charData = new ushort[w * h];
                    for (int p = 0; p < charData.Length; p++)
                    {
                        charData[p] = reader.ReadUInt16();
                    }
                    
                    _fontDataASCII[font, charIndex] = new FontCharacterData(w, h, charData);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load ASCII fonts: {ex.Message}");
        }
    }
    
    private void LoadUnicodeFonts()
    {
        for (int i = 0; i < 20; i++)
        {
            string filename = i == 0 ? "unifont.mul" : $"unifont{i}.mul";
            string path = Path.Combine(_dataPath, filename);
            
            if (File.Exists(path))
            {
                try
                {
                    _unicodeFontData[i] = File.ReadAllBytes(path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load {filename}: {ex.Message}");
                }
            }
        }
        
        // Fall back to font 0 for font 1 if not present
        if (_unicodeFontData[1] == null && _unicodeFontData[0] != null)
        {
            _unicodeFontData[1] = _unicodeFontData[0];
        }
    }
    
    /// <summary>
    /// Get Unicode character data, loading on demand
    /// </summary>
    private ref FontCharacterDataUnicode GetCharUnicode(byte font, char c)
    {
        int index = (int)c;
        if (index < 0 || font >= 20 || _unicodeFontData[font] == null)
            return ref _nullChar;
        
        ref var cc = ref _fontDataUnicode[font, index];
        if (cc.Data == null)
        {
            LoadUnicodeChar(font, index);
        }
        
        return ref cc;
    }
    
    private static FontCharacterDataUnicode _nullChar = new();
    
    private void LoadUnicodeChar(byte font, int index)
    {
        var data = _unicodeFontData[font];
        if (data == null || index * 4 + 4 > data.Length)
            return;
        
        // Read lookup offset
        int lookup = BitConverter.ToInt32(data, index * 4);
        if (lookup == 0 || lookup >= data.Length)
            return;
        
        ref var cc = ref _fontDataUnicode[font, index];
        
        cc.OffsetX = (sbyte)data[lookup];
        cc.OffsetY = (sbyte)data[lookup + 1];
        cc.Width = (sbyte)data[lookup + 2];
        cc.Height = (sbyte)data[lookup + 3];
        
        if (cc.Width > 0 && cc.Height > 0)
        {
            int scanlineBytes = ((cc.Width - 1) / 8) + 1;
            int dataSize = scanlineBytes * cc.Height;
            
            if (lookup + 4 + dataSize <= data.Length)
            {
                cc.Data = new byte[dataSize];
                Array.Copy(data, lookup + 4, cc.Data, 0, dataSize);
            }
        }
    }
    
    /// <summary>
    /// Check if a Unicode font exists
    /// </summary>
    public bool UnicodeFontExists(byte font)
    {
        return font < 20 && _unicodeFontData[font] != null;
    }
    
    /// <summary>
    /// Check if ASCII font exists
    /// </summary>
    public bool ASCIIFontExists(byte font)
    {
        return _fontDataASCII != null && font < _fontCount;
    }
    
    /// <summary>
    /// Get ASCII character index
    /// </summary>
    private int GetASCIIIndex(char c)
    {
        byte ch = (byte)c;
        if (ch < NOPRINT_CHARS)
            return 0;
        return ch - NOPRINT_CHARS;
    }
    
    /// <summary>
    /// Measure ASCII text width
    /// </summary>
    public int GetWidthASCII(byte font, string text)
    {
        if (_fontDataASCII == null || font >= _fontCount || string.IsNullOrEmpty(text))
            return 0;
        
        int width = 0;
        foreach (char c in text)
        {
            width += _fontDataASCII[font, GetASCIIIndex(c)].Width;
        }
        return width;
    }
    
    /// <summary>
    /// Measure Unicode text width
    /// </summary>
    public int GetWidthUnicode(byte font, string text)
    {
        if (font >= 20 || _unicodeFontData[font] == null || string.IsNullOrEmpty(text))
            return 0;
        
        int width = 0;
        foreach (char c in text)
        {
            if (c == ' ')
            {
                width += UNICODE_SPACE_WIDTH;
            }
            else
            {
                ref var ch = ref GetCharUnicode(font, c);
                if (ch.Data != null)
                {
                    width += ch.OffsetX + ch.Width + 1;
                }
            }
        }
        return width;
    }
    
    /// <summary>
    /// Get height of ASCII font
    /// </summary>
    public int GetHeightASCII(byte font)
    {
        if (_fontDataASCII == null || font >= _fontCount)
            return 14;
        
        // Find max height across common characters
        int maxHeight = 0;
        for (int i = 0; i < 224; i++)
        {
            if (_fontDataASCII[font, i].Height > maxHeight)
                maxHeight = _fontDataASCII[font, i].Height;
        }
        return maxHeight > 0 ? maxHeight : 14;
    }
    
    /// <summary>
    /// Get height of Unicode font (approximate)
    /// </summary>
    public int GetHeightUnicode(byte font)
    {
        // Sample some common characters to get height
        int maxHeight = 0;
        string sample = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        
        foreach (char c in sample)
        {
            ref var ch = ref GetCharUnicode(font, c);
            if (ch.Data != null)
            {
                int h = ch.OffsetY + ch.Height;
                if (h > maxHeight) maxHeight = h;
            }
        }
        
        return maxHeight > 0 ? maxHeight : 14;
    }
    
    /// <summary>
    /// Draw ASCII text directly to SpriteBatch
    /// </summary>
    public void DrawASCII(SpriteBatch spriteBatch, byte font, string text, int x, int y, Color color)
    {
        if (_fontDataASCII == null || font >= _fontCount || string.IsNullOrEmpty(text))
            return;
        
        int currentX = x;
        
        foreach (char c in text)
        {
            ref var fcd = ref _fontDataASCII[font, GetASCIIIndex(c)];
            
            if (fcd.Width > 0 && fcd.Height > 0 && fcd.Data != null)
            {
                // Get or create texture for this character
                var texture = GetOrCreateASCIICharTexture(font, c, ref fcd);
                if (texture != null)
                {
                    spriteBatch.Draw(texture, new Vector2(currentX, y), color);
                }
            }
            
            currentX += fcd.Width;
        }
    }
    
    /// <summary>
    /// Draw Unicode text directly to SpriteBatch
    /// </summary>
    public void DrawUnicode(SpriteBatch spriteBatch, byte font, string text, int x, int y, Color color)
    {
        if (font >= 20 || _unicodeFontData[font] == null || string.IsNullOrEmpty(text))
            return;
        
        int currentX = x;
        
        foreach (char c in text)
        {
            if (c == ' ')
            {
                currentX += UNICODE_SPACE_WIDTH;
                continue;
            }
            
            ref var ch = ref GetCharUnicode(font, c);
            if (ch.Data != null && ch.Width > 0 && ch.Height > 0)
            {
                var texture = GetOrCreateUnicodeCharTexture(font, c, ref ch);
                if (texture != null)
                {
                    spriteBatch.Draw(texture, new Vector2(currentX + ch.OffsetX, y + ch.OffsetY), color);
                }
                currentX += ch.OffsetX + ch.Width + 1;
            }
        }
    }
    
    /// <summary>
    /// Draw text using best available font (Unicode preferred, falls back to ASCII)
    /// </summary>
    public void DrawText(SpriteBatch spriteBatch, string text, int x, int y, Color color, byte font = 1)
    {
        if (string.IsNullOrEmpty(text))
            return;
        
        if (UnicodeFontExists(font))
        {
            DrawUnicode(spriteBatch, font, text, x, y, color);
        }
        else if (ASCIIFontExists(font))
        {
            DrawASCII(spriteBatch, font, text, x, y, color);
        }
    }
    
    /// <summary>
    /// Measure text width using best available font
    /// </summary>
    public int MeasureText(string text, byte font = 1)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        
        if (UnicodeFontExists(font))
            return GetWidthUnicode(font, text);
        else if (ASCIIFontExists(font))
            return GetWidthASCII(font, text);
        
        // Fallback estimate
        return text.Length * 8;
    }
    
    /// <summary>
    /// Get line height using best available font
    /// </summary>
    public int GetLineHeight(byte font = 1)
    {
        if (UnicodeFontExists(font))
            return GetHeightUnicode(font);
        else if (ASCIIFontExists(font))
            return GetHeightASCII(font);
        
        return 14;
    }
    
    // Character texture cache
    private readonly Dictionary<(byte font, char c, bool unicode), Texture2D> _charTextures = new();
    
    private Texture2D? GetOrCreateASCIICharTexture(byte font, char c, ref FontCharacterData fcd)
    {
        var key = (font, c, false);
        if (_charTextures.TryGetValue(key, out var cached))
            return cached;
        
        if (fcd.Width == 0 || fcd.Height == 0 || fcd.Data == null)
            return null;
        
        var texture = new Texture2D(_graphicsDevice, fcd.Width, fcd.Height);
        var pixels = new Color[fcd.Width * fcd.Height];
        
        for (int i = 0; i < fcd.Data.Length; i++)
        {
            ushort pixel = fcd.Data[i];
            if (pixel != 0)
            {
                // Convert 16-bit color to 32-bit (ARGB1555 format)
                int r = ((pixel >> 10) & 0x1F) * 255 / 31;
                int g = ((pixel >> 5) & 0x1F) * 255 / 31;
                int b = (pixel & 0x1F) * 255 / 31;
                pixels[i] = new Color(r, g, b, 255);
            }
        }
        
        texture.SetData(pixels);
        _charTextures[key] = texture;
        return texture;
    }
    
    private Texture2D? GetOrCreateUnicodeCharTexture(byte font, char c, ref FontCharacterDataUnicode ch)
    {
        var key = (font, c, true);
        if (_charTextures.TryGetValue(key, out var cached))
            return cached;
        
        if (ch.Width <= 0 || ch.Height <= 0 || ch.Data == null)
            return null;
        
        var texture = new Texture2D(_graphicsDevice, ch.Width, ch.Height);
        var pixels = new Color[ch.Width * ch.Height];
        
        int scanlineBytes = ((ch.Width - 1) / 8) + 1;
        
        for (int y = 0; y < ch.Height; y++)
        {
            int scanlineOffset = y * scanlineBytes;
            int bitX = 7;
            int byteX = 0;
            
            for (int x = 0; x < ch.Width; x++)
            {
                if (scanlineOffset + byteX < ch.Data.Length)
                {
                    bool isSet = (ch.Data[scanlineOffset + byteX] & (1 << bitX)) != 0;
                    if (isSet)
                    {
                        pixels[y * ch.Width + x] = Color.White;
                    }
                }
                
                bitX--;
                if (bitX < 0)
                {
                    bitX = 7;
                    byteX++;
                }
            }
        }
        
        texture.SetData(pixels);
        _charTextures[key] = texture;
        return texture;
    }
}

/// <summary>
/// ASCII font character data
/// </summary>
public struct FontCharacterData
{
    public byte Width;
    public byte Height;
    public ushort[]? Data;
    
    public FontCharacterData(byte w, byte h, ushort[] data)
    {
        Width = w;
        Height = h;
        Data = data;
    }
}

/// <summary>
/// Unicode font character data
/// </summary>
public struct FontCharacterDataUnicode
{
    public sbyte OffsetX;
    public sbyte OffsetY;
    public sbyte Width;
    public sbyte Height;
    public byte[]? Data;
}
