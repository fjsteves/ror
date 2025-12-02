// ==========================================================================
// ColorConversion.cs - UO color format conversion utilities
// ==========================================================================
// UO uses ARGB1555 format for most textures:
// - Bit 15: Alpha (1 = opaque, 0 = transparent)
// - Bits 14-10: Red (5 bits, 0-31)
// - Bits 9-5: Green (5 bits, 0-31)
// - Bits 4-0: Blue (5 bits, 0-31)
// ==========================================================================

using Microsoft.Xna.Framework;

namespace RealmOfReality.Client.Rendering;

/// <summary>
/// Color format conversion utilities for UO asset loading.
/// </summary>
public static class ColorConversion
{
    // ========================================================================
    // ARGB1555 TO RGBA8888 CONVERSION
    // ========================================================================
    
    /// <summary>
    /// Convert ARGB1555 color to RGBA8888 (MonoGame Color format).
    /// </summary>
    /// <param name="color1555">16-bit ARGB1555 color</param>
    /// <returns>32-bit RGBA color</returns>
    public static Color Argb1555ToColor(ushort color1555)
    {
        // Color 0 is always transparent in UO
        if (color1555 == 0)
            return Color.Transparent;
        
        // Extract 5-bit components
        // Bit 15 = alpha, bits 14-10 = red, bits 9-5 = green, bits 4-0 = blue
        int r5 = (color1555 >> 10) & 0x1F;
        int g5 = (color1555 >> 5) & 0x1F;
        int b5 = color1555 & 0x1F;
        
        // Scale 5-bit to 8-bit (multiply by 255/31 ≈ 8.226)
        // Using bit replication for better accuracy: (x << 3) | (x >> 2)
        int r8 = (r5 << 3) | (r5 >> 2);
        int g8 = (g5 << 3) | (g5 >> 2);
        int b8 = (b5 << 3) | (b5 >> 2);
        
        return new Color(r8, g8, b8, 255);
    }
    
    /// <summary>
    /// Convert ARGB1555 color to RGBA8888 packed uint.
    /// MonoGame uses RGBA format in memory.
    /// </summary>
    /// <param name="color1555">16-bit ARGB1555 color</param>
    /// <returns>32-bit RGBA packed value</returns>
    public static uint Argb1555ToRgba(ushort color1555)
    {
        if (color1555 == 0)
            return 0; // Fully transparent
        
        // Extract and scale components
        uint r = (uint)((color1555 >> 10) & 0x1F);
        uint g = (uint)((color1555 >> 5) & 0x1F);
        uint b = (uint)(color1555 & 0x1F);
        
        // Scale 5-bit to 8-bit with bit replication
        r = (r << 3) | (r >> 2);
        g = (g << 3) | (g >> 2);
        b = (b << 3) | (b >> 2);
        
        // Pack as RGBA (R in high byte)
        return (r << 24) | (g << 16) | (b << 8) | 0xFF;
    }
    
    /// <summary>
    /// Convert ARGB1555 color to ABGR8888 packed uint.
    /// Some MonoGame platforms use ABGR format.
    /// </summary>
    public static uint Argb1555ToAbgr(ushort color1555)
    {
        if (color1555 == 0)
            return 0;
        
        uint r = (uint)((color1555 >> 10) & 0x1F);
        uint g = (uint)((color1555 >> 5) & 0x1F);
        uint b = (uint)(color1555 & 0x1F);
        
        r = (r << 3) | (r >> 2);
        g = (g << 3) | (g >> 2);
        b = (b << 3) | (b >> 2);
        
        // Pack as ABGR (A in high byte)
        return 0xFF000000 | (b << 16) | (g << 8) | r;
    }
    
    // ========================================================================
    // BATCH CONVERSION
    // ========================================================================
    
    /// <summary>
    /// Convert an array of ARGB1555 colors to Color array.
    /// </summary>
    public static Color[] ConvertToColors(ushort[] colors1555)
    {
        var result = new Color[colors1555.Length];
        for (int i = 0; i < colors1555.Length; i++)
        {
            result[i] = Argb1555ToColor(colors1555[i]);
        }
        return result;
    }
    
    /// <summary>
    /// Convert raw bytes (ARGB1555) to Color array.
    /// </summary>
    public static Color[] ConvertBytesToColors(byte[] data, int offset, int pixelCount)
    {
        var result = new Color[pixelCount];
        for (int i = 0; i < pixelCount && offset + 1 < data.Length; i++)
        {
            ushort color1555 = (ushort)(data[offset] | (data[offset + 1] << 8));
            result[i] = Argb1555ToColor(color1555);
            offset += 2;
        }
        return result;
    }
    
    // ========================================================================
    // HUE APPLICATION
    // ========================================================================
    
    /// <summary>
    /// Apply a hue to a color.
    /// In UO, hues replace the color's brightness with the hue palette.
    /// </summary>
    /// <param name="original">Original color</param>
    /// <param name="huePalette">32-color hue palette</param>
    /// <param name="partialHue">If true, only applies to grayscale pixels</param>
    /// <returns>Hued color</returns>
    public static Color ApplyHue(Color original, Color[] huePalette, bool partialHue = false)
    {
        if (huePalette == null || huePalette.Length < 32)
            return original;
        
        if (original.A == 0)
            return original;
        
        // For partial hue, only apply to grayscale pixels
        if (partialHue)
        {
            int tolerance = 4;
            if (Math.Abs(original.R - original.G) > tolerance ||
                Math.Abs(original.G - original.B) > tolerance)
            {
                return original; // Not grayscale, keep original
            }
        }
        
        // Calculate brightness (0-31 range for hue index)
        int brightness = (original.R + original.G + original.B) / 3;
        int hueIndex = brightness * 31 / 255;
        hueIndex = Math.Clamp(hueIndex, 0, 31);
        
        var hueColor = huePalette[hueIndex];
        return new Color(hueColor.R, hueColor.G, hueColor.B, original.A);
    }
}

/// <summary>
/// Binary data reading utilities for UO file formats.
/// </summary>
public static class BinaryUtils
{
    /// <summary>
    /// Read a little-endian 16-bit unsigned integer from a byte array.
    /// </summary>
    public static ushort ReadUInt16(byte[] data, int offset)
    {
        if (offset + 1 >= data.Length)
            return 0;
        return (ushort)(data[offset] | (data[offset + 1] << 8));
    }
    
    /// <summary>
    /// Read a little-endian 32-bit signed integer from a byte array.
    /// </summary>
    public static int ReadInt32(byte[] data, int offset)
    {
        if (offset + 3 >= data.Length)
            return 0;
        return data[offset] |
               (data[offset + 1] << 8) |
               (data[offset + 2] << 16) |
               (data[offset + 3] << 24);
    }
    
    /// <summary>
    /// Read a little-endian 32-bit unsigned integer from a byte array.
    /// </summary>
    public static uint ReadUInt32(byte[] data, int offset)
    {
        return (uint)ReadInt32(data, offset);
    }
    
    /// <summary>
    /// Read a little-endian 64-bit unsigned integer from a byte array.
    /// </summary>
    public static ulong ReadUInt64(byte[] data, int offset)
    {
        if (offset + 7 >= data.Length)
            return 0;
        uint low = ReadUInt32(data, offset);
        uint high = ReadUInt32(data, offset + 4);
        return low | ((ulong)high << 32);
    }
    
    /// <summary>
    /// Read a fixed-length ASCII string from a byte array.
    /// </summary>
    public static string ReadFixedString(byte[] data, int offset, int length)
    {
        if (offset + length > data.Length)
            length = Math.Max(0, data.Length - offset);
        
        int end = Array.IndexOf(data, (byte)0, offset, length);
        if (end < 0)
            end = offset + length;
        
        return System.Text.Encoding.ASCII.GetString(data, offset, end - offset);
    }
}
