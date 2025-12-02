namespace RealmOfReality.Client.Assets;

/// <summary>
/// Configuration for UO asset paths
/// Supports both classic MUL format and newer LegacyMUL format used by ClassicUO
/// </summary>
public class UOAssetConfig
{
    /// <summary>
    /// Path to UO client data folder (e.g., C:\test\gamedata\)
    /// </summary>
    public string DataPath { get; set; } = "";
    
    // Classic MUL file paths
    public string ArtMulPath => Path.Combine(DataPath, "art.mul");
    public string ArtIdxPath => Path.Combine(DataPath, "artidx.mul");
    public string GumpArtPath => Path.Combine(DataPath, "gumpart.mul");
    public string GumpIdxPath => Path.Combine(DataPath, "gumpidx.mul");
    public string HuesPath => Path.Combine(DataPath, "hues.mul");
    public string TileDataPath => Path.Combine(DataPath, "tiledata.mul");
    public string TexMapsPath => Path.Combine(DataPath, "texmaps.mul");
    public string TexIdxPath => Path.Combine(DataPath, "texidx.mul");
    public string AnimPath => Path.Combine(DataPath, "anim.mul");
    public string AnimIdxPath => Path.Combine(DataPath, "anim.idx");
    public string Map0Path => Path.Combine(DataPath, "map0.mul");
    public string Statics0Path => Path.Combine(DataPath, "statics0.mul");
    public string StaIdx0Path => Path.Combine(DataPath, "staidx0.mul");
    public string MultiPath => Path.Combine(DataPath, "multi.mul");
    public string MultiIdxPath => Path.Combine(DataPath, "multi.idx");
    public string SoundsPath => Path.Combine(DataPath, "sound.mul");
    public string SoundsIdxPath => Path.Combine(DataPath, "soundidx.mul");
    
    // LegacyMUL format paths (ClassicUO style - MUL data in .uop container or renamed .mul)
    public string ArtLegacyMulPath => Path.Combine(DataPath, "artLegacyMUL.mul");
    public string ArtLegacyUopPath => Path.Combine(DataPath, "artLegacyMUL.uop");
    public string GumpLegacyMulPath => Path.Combine(DataPath, "gumpartLegacyMUL.mul");
    public string GumpLegacyUopPath => Path.Combine(DataPath, "gumpartLegacyMUL.uop");
    public string AnimLegacyMulPath => Path.Combine(DataPath, "animLegacyMUL.mul");
    public string AnimLegacyUopPath => Path.Combine(DataPath, "animLegacyMUL.uop");
    
    // UOP animation files (newer format)
    public string AnimationFrame1Path => Path.Combine(DataPath, "animationFrame1.uop");
    public string AnimationFrame2Path => Path.Combine(DataPath, "animationFrame2.uop");
    public string AnimationSequencePath => Path.Combine(DataPath, "AnimationSequence.uop");
    
    // Definition files
    public string BodyDefPath => Path.Combine(DataPath, "body.def");
    public string BodyConvDefPath => Path.Combine(DataPath, "bodyconv.def");
    
    /// <summary>
    /// Get the best available art file path (prefers classic MUL, falls back to LegacyMUL)
    /// </summary>
    public string GetArtMulPath()
    {
        // Prefer classic art.mul
        if (File.Exists(ArtMulPath)) return ArtMulPath;
        // Fall back to LegacyMUL.mul (same format as art.mul, just renamed)
        if (File.Exists(ArtLegacyMulPath)) return ArtLegacyMulPath;
        return ArtMulPath; // Return default even if not exists
    }
    
    /// <summary>
    /// Get the best available art index path
    /// artidx.mul is always the index file, even for LegacyMUL data files
    /// </summary>
    public string GetArtIdxPath()
    {
        // artidx.mul is the index for both art.mul and artLegacyMUL.mul
        if (File.Exists(ArtIdxPath)) return ArtIdxPath;
        return "";
    }
    
    /// <summary>
    /// Get the best available gump file path
    /// </summary>
    public string GetGumpMulPath()
    {
        if (File.Exists(GumpArtPath)) return GumpArtPath;
        if (File.Exists(GumpLegacyMulPath)) return GumpLegacyMulPath;
        return GumpArtPath;
    }
    
    /// <summary>
    /// Get the best available gump index path
    /// </summary>
    public string GetGumpIdxPath()
    {
        if (File.Exists(GumpIdxPath)) return GumpIdxPath;
        return "";
    }
    
    /// <summary>
    /// Get the best available anim file path
    /// </summary>
    public string GetAnimMulPath()
    {
        if (File.Exists(AnimPath)) return AnimPath;
        if (File.Exists(AnimLegacyMulPath)) return AnimLegacyMulPath;
        return AnimPath;
    }
    
    /// <summary>
    /// Get the best available anim index path
    /// </summary>
    public string GetAnimIdxPath()
    {
        if (File.Exists(AnimIdxPath)) return AnimIdxPath;
        return "";
    }
    
    /// <summary>
    /// Check if we have LegacyMUL format files (no separate idx files)
    /// </summary>
    public bool IsLegacyMulFormat => File.Exists(ArtLegacyMulPath) && !File.Exists(ArtIdxPath);
    
    /// <summary>
    /// Check if required files exist
    /// </summary>
    public bool ValidateFiles(out List<string> missingFiles)
    {
        missingFiles = new List<string>();
        
        // Check for art files - either classic or legacy format
        bool hasArt = File.Exists(ArtMulPath) || File.Exists(ArtLegacyMulPath) || File.Exists(ArtLegacyUopPath);
        if (!hasArt)
            missingFiles.Add("art.mul or artLegacyMUL.mul");
        
        // TileData is always required for metadata
        if (!File.Exists(TileDataPath))
            missingFiles.Add(TileDataPath);
        
        return missingFiles.Count == 0;
    }
    
    /// <summary>
    /// Check if UOP files are available (newer client format)
    /// </summary>
    public bool HasUopFiles => File.Exists(ArtLegacyUopPath);
    
    /// <summary>
    /// Check if classic MUL files are available
    /// </summary>
    public bool HasMulFiles => File.Exists(ArtMulPath) && File.Exists(ArtIdxPath);
    
    /// <summary>
    /// Check if LegacyMUL files are available (ClassicUO format)
    /// </summary>
    public bool HasLegacyMulFiles => File.Exists(ArtLegacyMulPath);
}
