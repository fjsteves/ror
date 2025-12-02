using Microsoft.Xna.Framework;

namespace RealmOfReality.Client.Assets;

/// <summary>
/// A single hue entry (color palette)
/// Each hue has 32 colors that map to grayscale values
/// </summary>
public class HueEntry
{
    public int Id { get; set; }
    public ushort[] Colors { get; } = new ushort[32];
    public ushort TableStart { get; set; }
    public ushort TableEnd { get; set; }
    public string Name { get; set; } = "";
    
    /// <summary>
    /// Apply this hue to an ARGB1555 color
    /// </summary>
    public ushort ApplyTo(ushort color)
    {
        if (color == 0)
            return 0; // Keep transparent
        
        // Convert to grayscale (use red channel as brightness since UO art is grayscale)
        int r = (color >> 10) & 0x1F;
        
        // Map to hue table index (0-31)
        int index = r;
        
        return Colors[index];
    }
    
    /// <summary>
    /// Get the color at a specific index (0-31)
    /// </summary>
    public Color GetColor(int index)
    {
        if (index < 0 || index >= 32)
            return Color.White;
        
        ushort c = Colors[index];
        int r = ((c >> 10) & 0x1F) * 255 / 31;
        int g = ((c >> 5) & 0x1F) * 255 / 31;
        int b = (c & 0x1F) * 255 / 31;
        
        return new Color(r, g, b);
    }
    
    /// <summary>
    /// Get the primary color of this hue (middle of the gradient)
    /// </summary>
    public Color PrimaryColor => GetColor(16);
}

/// <summary>
/// Loads hue (color palette) data from UO's hues.mul
/// 
/// Hues.mul format:
/// - 3000 groups of 8 hues each = 24000 total hues
/// - Each group: 4 byte header + 8 * 88 bytes
/// - Each hue: 32 colors (2 bytes each) + 2 bytes table start + 2 bytes table end + 20 bytes name
/// </summary>
public class HuesLoader : IDisposable
{
    private HueEntry[]? _hues;
    
    public int HueCount => _hues?.Length ?? 0;
    public bool IsLoaded => _hues != null;
    
    /// <summary>
    /// Load hues.mul
    /// </summary>
    public bool Load(string path)
    {
        if (!File.Exists(path))
            return false;
        
        try
        {
            var data = File.ReadAllBytes(path);
            
            // 3000 groups of 8 hues each
            const int groupCount = 3000;
            const int huesPerGroup = 8;
            const int groupHeaderSize = 4;
            const int hueSize = 88; // 32 colors * 2 + 2 + 2 + 20
            
            _hues = new HueEntry[groupCount * huesPerGroup];
            
            int offset = 0;
            for (int group = 0; group < groupCount; group++)
            {
                offset += groupHeaderSize; // Skip group header
                
                for (int hue = 0; hue < huesPerGroup; hue++)
                {
                    if (offset + hueSize > data.Length)
                        break;
                    
                    int hueId = group * huesPerGroup + hue;
                    var entry = new HueEntry { Id = hueId };
                    
                    // Read 32 colors
                    for (int i = 0; i < 32; i++)
                    {
                        entry.Colors[i] = BitConverter.ToUInt16(data, offset + i * 2);
                    }
                    
                    entry.TableStart = BitConverter.ToUInt16(data, offset + 64);
                    entry.TableEnd = BitConverter.ToUInt16(data, offset + 66);
                    
                    // Read name (20 bytes, null-terminated)
                    int nameEnd = 20;
                    for (int i = 0; i < 20; i++)
                    {
                        if (data[offset + 68 + i] == 0)
                        {
                            nameEnd = i;
                            break;
                        }
                    }
                    entry.Name = System.Text.Encoding.ASCII.GetString(data, offset + 68, nameEnd).Trim();
                    
                    _hues[hueId] = entry;
                    offset += hueSize;
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading hues: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Get a hue entry
    /// </summary>
    public HueEntry? GetHue(int hueId)
    {
        // Hue 0 means no hue (use original colors)
        if (hueId <= 0 || _hues == null || hueId > _hues.Length)
            return null;
        
        // UO hue IDs are 1-based, array is 0-based
        return _hues[hueId - 1];
    }
    
    /// <summary>
    /// Find hues by name
    /// </summary>
    public IEnumerable<HueEntry> FindByName(string name)
    {
        if (_hues == null)
            yield break;
        
        var search = name.ToLowerInvariant();
        foreach (var hue in _hues)
        {
            if (hue?.Name?.ToLowerInvariant().Contains(search) == true)
                yield return hue;
        }
    }
    
    /// <summary>
    /// Get common named hues
    /// </summary>
    public static class CommonHues
    {
        public const int None = 0;
        public const int Red = 33;
        public const int Blue = 88;
        public const int Green = 68;
        public const int Yellow = 53;
        public const int Purple = 16;
        public const int Orange = 43;
        public const int White = 1153;
        public const int Black = 1;
        public const int Gold = 2213;
        public const int Silver = 2500;
    }
    
    public void Dispose() { }
}
