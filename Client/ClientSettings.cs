using System.Text.Json;

namespace RealmOfReality.Client;

/// <summary>
/// Client settings that persist between sessions
/// </summary>
public class ClientSettings
{
    // UO Asset Settings
    public string UODataPath { get; set; } = @"C:\Program Files (x86)\Electronic Arts\Ultima Online Classic";
    public bool UseUOGraphics { get; set; } = true;
    public bool UseUOTiles { get; set; } = true;
    public bool UseUOAnimations { get; set; } = true;
    public bool UseUOSpellIcons { get; set; } = true;
    
    // Network Settings
    public string LastServerAddress { get; set; } = "127.0.0.1";
    public int ServerPort { get; set; } = 7775;
    public string LastUsername { get; set; } = "";
    
    // Graphics Settings
    public int ScreenWidth { get; set; } = 1280;
    public int ScreenHeight { get; set; } = 720;
    public bool Fullscreen { get; set; } = false;
    public bool VSync { get; set; } = true;
    public float Zoom { get; set; } = 1.0f;
    
    // Gameplay Settings
    public bool AlwaysRun { get; set; } = false;
    public bool ShowHealthBars { get; set; } = true;
    public bool ShowNames { get; set; } = true;
    public bool ShowGrid { get; set; } = false;
    public bool ShowFPS { get; set; } = true;
    
    // Audio Settings (placeholder for future)
    public float MusicVolume { get; set; } = 0.5f;
    public float SoundVolume { get; set; } = 0.8f;
    
    // File path for settings
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RealmOfReality",
        "settings.json"
    );
    
    /// <summary>
    /// Load settings from disk or create defaults
    /// </summary>
    public static ClientSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<ClientSettings>(json);
                if (settings != null)
                {
                    Console.WriteLine($"Settings loaded from: {SettingsPath}");
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load settings: {ex.Message}");
        }
        
        Console.WriteLine("Using default settings");
        return new ClientSettings();
    }
    
    /// <summary>
    /// Save settings to disk
    /// </summary>
    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(SettingsPath, json);
            Console.WriteLine($"Settings saved to: {SettingsPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Try to auto-detect UO installation path
    /// </summary>
    public bool AutoDetectUOPath()
    {
        var possiblePaths = new[]
        {
            @"C:\Program Files (x86)\Electronic Arts\Ultima Online Classic",
            @"C:\Program Files\Electronic Arts\Ultima Online Classic",
            @"C:\ClassicUO",
            @"C:\ClassicUO\Data",
            @"C:\UO",
            @"C:\Ultima Online",
            @"C:\Games\Ultima Online",
            @"C:\Games\ClassicUO",
            @"D:\Ultima Online",
            @"D:\ClassicUO",
            @"C:\test\gamedata", // Dev path
        };
        
        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                var oldPath = UODataPath;
                UODataPath = path;
                if (ValidateUOPath())
                {
                    Console.WriteLine($"Auto-detected UO path: {path}");
                    return true;
                }
                UODataPath = oldPath;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Validate that the UO data path contains required files
    /// Supports both classic MUL format and newer LegacyMUL/UOP formats
    /// </summary>
    public bool ValidateUOPath()
    {
        if (string.IsNullOrEmpty(UODataPath) || !Directory.Exists(UODataPath))
            return false;
        
        // Check for art files - any of these formats work
        var hasArt = File.Exists(Path.Combine(UODataPath, "art.mul")) ||
                     File.Exists(Path.Combine(UODataPath, "artLegacyMUL.uop")) ||
                     File.Exists(Path.Combine(UODataPath, "artLegacyMUL.mul"));
        
        // For classic format, we need idx files; for UOP/LegacyMUL, index is embedded
        var hasArtIdx = File.Exists(Path.Combine(UODataPath, "artidx.mul")) ||
                        File.Exists(Path.Combine(UODataPath, "artLegacyMUL.uop")) ||
                        File.Exists(Path.Combine(UODataPath, "artLegacyMUL.mul"));
        
        return hasArt && hasArtIdx;
    }
    
    /// <summary>
    /// Get list of UO files present in data path
    /// </summary>
    public List<string> GetPresentFiles()
    {
        var files = new List<string>();
        if (!Directory.Exists(UODataPath)) return files;
        
        // Check both classic MUL and newer LegacyMUL/UOP formats
        var checkFiles = new[]
        {
            // Classic format
            "art.mul", "artidx.mul",
            "gumpart.mul", "gumpidx.mul",
            "anim.mul", "animidx.mul",
            "anim2.mul", "anim2idx.mul",
            "anim3.mul", "anim3idx.mul",
            "anim4.mul", "anim4idx.mul",
            "anim5.mul", "anim5idx.mul",
            "tiledata.mul",
            "hues.mul",
            "map0.mul", "staidx0.mul", "statics0.mul",
            "texmaps.mul", "texidx.mul",
            "multi.mul", "multiidx.mul",
            // LegacyMUL format (used by ClassicUO)
            "artLegacyMUL.mul", "artLegacyMUL.uop",
            "gumpartLegacyMUL.mul", "gumpartLegacyMUL.uop",
            "animationFrame1.uop", "animationFrame2.uop",
            "AnimationSequence.uop",
            "tileart.uop",
            // UOP format
            "artart.uop",
            "gumpart.uop",
            "map0LegacyMUL.uop", "map0xLegacyMUL.uop",
            // Definition files
            "art.def", "gump.def", "body.def", "bodyconv.def",
            // Sound
            "sound.mul", "soundidx.mul", "soundLegacyMUL.uop"
        };
        
        foreach (var file in checkFiles)
        {
            var path = Path.Combine(UODataPath, file);
            if (File.Exists(path))
                files.Add(file);
        }
        
        return files;
    }
}
