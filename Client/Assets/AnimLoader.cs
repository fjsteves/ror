using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RealmOfReality.Client.Assets;

/// <summary>
/// Animation actions for UO bodies
/// Human bodies have 35 animation groups
/// Monster bodies have fewer (typically 22)
/// Low bodies (animals) have 13
/// </summary>
public enum AnimAction
{
    // Human animation groups (35 total)
    Walk = 0,
    WalkArmed = 1,
    Run = 2,
    RunArmed = 3,
    Stand = 4,
    Fidget1 = 5,
    Fidget2 = 6,
    StandOneHanded = 7,
    StandTwoHanded = 8,
    AttackOneHanded = 9,
    AttackUnarmed = 10,
    AttackTwoHandedDown = 11,
    AttackTwoHandedWide = 12,
    AttackTwoHandedJab = 13,
    WalkWarmode = 14,
    CastDirected = 15,
    CastArea = 16,
    AttackBow = 17,
    AttackCrossbow = 18,
    GetHit = 19,
    Die1 = 20,
    Die2 = 21,
    OnMountWalk = 22,
    OnMountRun = 23,
    OnMountStand = 24,
    OnMountAttack = 25,
    OnMountAttackBow = 26,
    OnMountAttackCrossbow = 27,
    OnMountSlash = 28,
    Turn = 29,
    AttackUnarmedAndWalk = 30,
    EmoteBow = 31,
    EmoteSalute = 32,
    FidgetYawn = 33,
    
    // Monster animation groups (22 total, indices 0-21)
    MonsterWalk = 0,
    MonsterStand = 1,
    MonsterDie = 2,
    // ... etc
    
    // Low body (animal) animation groups (13 total)
    AnimalWalk = 0,
    AnimalRun = 1,
    AnimalStand = 2,
    AnimalEat = 3,
    AnimalAlert = 4,
    AnimalAttack1 = 5,
    AnimalAttack2 = 6,
    AnimalGetHit = 7,
    AnimalDie = 8
}

/// <summary>
/// Animation file direction indices. UO stores only 5 directions (0-4).
/// The names reflect what direction the character faces in that animation index.
/// 
/// CRITICAL: These are animation FILE indices, not game directions!
/// - Index 0: Character facing South (toward camera)
/// - Index 1: Character facing Southwest
/// - Index 2: Character facing West
/// - Index 3: Character facing Northwest  
/// - Index 4: Character facing North (away from camera)
/// 
/// Game directions East, Southeast, Northeast are rendered by mirroring.
/// </summary>
public enum AnimDirection
{
    South = 0,      // Anim file index 0 - facing camera
    SouthWest = 1,  // Anim file index 1 - down-left
    West = 2,       // Anim file index 2 - left
    NorthWest = 3,  // Anim file index 3 - up-left
    North = 4       // Anim file index 4 - facing away
}

/// <summary>
/// A single animation frame
/// </summary>
public class AnimFrame
{
    public Texture2D? Texture { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int CenterX { get; set; }  // X offset from center (for proper positioning)
    public int CenterY { get; set; }  // Y offset from bottom (for proper positioning)
}

/// <summary>
/// A complete animation (all frames for one action/direction)
/// </summary>
public class Animation
{
    public int BodyId { get; set; }
    public AnimAction Action { get; set; }
    public AnimDirection Direction { get; set; }
    public AnimFrame[] Frames { get; set; } = Array.Empty<AnimFrame>();
    public int FrameCount => Frames.Length;
    
    public AnimFrame? GetFrame(int index)
    {
        if (index < 0 || index >= Frames.Length)
            return null;
        return Frames[index];
    }
}

/// <summary>
/// UO body types for determining animation group structure
/// </summary>
public enum BodyType
{
    Human,      // 35 groups, 5 directions each = 175 slots
    Monster,    // 22 groups, 5 directions each = 110 slots
    Animal,     // 13 groups, 5 directions each = 65 slots
    Equipment   // 35 groups, 5 directions each = 175 slots
}

/// <summary>
/// Loads animations from UO's anim.mul and animidx.mul
/// 
/// CRITICAL: Animation file structure based on ClassicUO and UO documentation:
/// 
/// anim.mul layout (index slots based on body type):
/// - Bodies 0-199: EQUIPMENT/LOW animations (slot = body * 110)
/// - Bodies 200-399: ANIMAL/LOW animations (slot = 22000 + (body-200)*65)
/// - Bodies 400+: HIGH/HUMAN animations (slot = 35000 + (body-400)*175)
/// 
/// Note: Body 200 = classic human male starts at slot 35000
/// Body 400 = HD human male, typically in anim4.mul via bodyconv.def
/// </summary>
public class AnimLoader : IDisposable
{
    private readonly GraphicsDevice _graphics;
    private readonly Dictionary<(int body, int group, int dir), Animation> _cache = new();
    
    // Multiple MUL file handles for different body ranges
    private FileStream?[] _mulFiles = new FileStream?[6];
    private IndexEntry[]?[] _mulIndices = new IndexEntry[]?[6];
    
    private readonly string _basePath;
    
    // Body conversion mapping from bodyconv.def
    private Dictionary<int, int[]> _bodyConv = new();
    
    // Body type mapping from mobtypes.txt
    private Dictionary<int, (string type, int flags)> _mobTypes = new();
    
    public int EntryCount => _mulIndices[0]?.Length ?? 0;
    public bool IsLoaded => _mulFiles.Any(f => f != null);
    public bool IsUsingUop => false;
    
    // Animation file constants based on ClassicUO
    private const int LOW_ANIMATION_COUNT = 200;        // Bodies 0-199 (equipment/monsters)
    private const int HIGH_ANIMATION_COUNT = 200;       // Bodies 200-399 (animals/old humans)
    private const int PEOPLE_ANIMATION_COUNT = 400;     // Bodies 400-799 (HD humans)
    
    private const int LOW_ANIMATION_GROUPS = 22;        // Monster groups
    private const int HIGH_ANIMATION_GROUPS = 13;       // Animal groups
    private const int PEOPLE_ANIMATION_GROUPS = 35;     // Human groups
    
    public AnimLoader(GraphicsDevice graphics, string mulPath, string idxPath)
    {
        _graphics = graphics;
        _basePath = Path.GetDirectoryName(mulPath) ?? ".";
    }
    
    /// <summary>
    /// Load all animation index files
    /// </summary>
    public bool Load()
    {
        bool anyLoaded = false;
        
        string[] mulNames = { "anim.mul", "anim2.mul", "anim3.mul", "anim4.mul", "anim5.mul" };
        string[] idxNames = { "anim.idx", "anim2.idx", "anim3.idx", "anim4.idx", "anim5.idx" };
        
        for (int i = 0; i < 5; i++)
        {
            var mulPath = Path.Combine(_basePath, mulNames[i]);
            var idxPath = Path.Combine(_basePath, idxNames[i]);
            
            if (File.Exists(mulPath) && File.Exists(idxPath))
            {
                try
                {
                    var idxData = File.ReadAllBytes(idxPath);
                    var entryCount = idxData.Length / 12;
                    _mulIndices[i] = new IndexEntry[entryCount];
                    
                    for (int j = 0; j < entryCount; j++)
                    {
                        _mulIndices[i]![j] = new IndexEntry
                        {
                            Lookup = BitConverter.ToInt32(idxData, j * 12),
                            Length = BitConverter.ToInt32(idxData, j * 12 + 4),
                            Extra = BitConverter.ToInt32(idxData, j * 12 + 8)
                        };
                    }
                    
                    _mulFiles[i] = new FileStream(mulPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    
                    // Count valid entries
                    int validCount = 0;
                    for (int j = 0; j < Math.Min(entryCount, 1000); j++)
                    {
                        if (_mulIndices[i]![j].Lookup >= 0 && _mulIndices[i]![j].Length > 512)
                            validCount++;
                    }
                    
                    DebugLog.Write($"AnimLoader: Loaded {mulNames[i]} with {entryCount} entries ({validCount} valid in first 1000)");
                    anyLoaded = true;
                }
                catch (Exception ex)
                {
                    DebugLog.Write($"AnimLoader: Failed to load {mulNames[i]}: {ex.Message}");
                }
            }
        }
        
        if (!anyLoaded)
        {
            DebugLog.Write($"AnimLoader: No animation files found in {_basePath}");
        }
        else
        {
            LoadBodyConv(_basePath);
            LoadMobTypes(_basePath);
            
            // Diagnostic: dump info about key bodies
            DumpBodyDiagnostics();
        }
        
        return anyLoaded;
    }
    
    /// <summary>
    /// Diagnostic dump for debugging body lookups
    /// </summary>
    private void DumpBodyDiagnostics()
    {
        DebugLog.Write("=== BODY DIAGNOSTICS ===");
        
        // Check body 200 (classic human male)
        CheckBodyAtIndex(200, 0, 0, "Body 200 group 0 dir 0");
        CheckBodyAtIndex(200, 4, 0, "Body 200 group 4 dir 0");
        
        // Check body 400 (HD human male)
        CheckBodyAtIndex(400, 0, 0, "Body 400 group 0 dir 0");
        CheckBodyAtIndex(400, 4, 0, "Body 400 group 4 dir 0");
        
        // Try various index calculations for body 400
        DebugLog.Write("Body 400 index calculations:");
        DebugLog.Write($"  Simple 400*175+20 = {400 * 175 + 20}");
        DebugLog.Write($"  Offset (400-400)*175+20 = {(400 - 400) * 175 + 20}");
        DebugLog.Write($"  As slot 0: 35000+(0)*175+20 = {35000 + 0 * 175 + 20}");
        
        // Check bodyconv.def for body 400
        if (_bodyConv.TryGetValue(400, out var conv))
        {
            DebugLog.Write($"  bodyconv.def: anim2={conv[0]}, anim3={conv[1]}, anim4={conv[2]}, anim5={conv[3]}");
        }
        else
        {
            DebugLog.Write("  No bodyconv.def entry for body 400");
        }
        
        // Check mobtypes for body 400
        if (_mobTypes.TryGetValue(400, out var mobType))
        {
            DebugLog.Write($"  mobtypes.txt: {mobType.type} flags={mobType.flags}");
        }
        
        DebugLog.Write("=== END DIAGNOSTICS ===");
    }
    
    private void CheckBodyAtIndex(int body, int group, int dir, string label)
    {
        int offset = group * 5 + dir;
        
        // Try various calculations
        var indices = new (int file, int index, string desc)[]
        {
            (0, body * 175 + offset, "body*175"),
            (0, body * 110 + offset, "body*110"),
            (0, body * 65 + offset, "body*65"),
            (0, CalculateLowGroupOffset(body) + offset, "LowGroupOffset"),
            (0, CalculateHighGroupOffset(body) + offset, "HighGroupOffset"),
            (0, CalculatePeopleGroupOffset(body) + offset, "PeopleGroupOffset"),
            (3, body * 175 + offset, "anim4: body*175"),
            (3, (body - 400) * 175 + offset, "anim4: (body-400)*175"),
        };
        
        DebugLog.Write($"{label}:");
        foreach (var (file, index, desc) in indices)
        {
            var entry = GetIndexEntry(file, index);
            if (entry.HasValue && entry.Value.Lookup >= 0 && entry.Value.Length > 512)
            {
                DebugLog.Write($"  VALID: file={file}, {desc}={index}, len={entry.Value.Length}");
            }
        }
    }
    
    private IndexEntry? GetIndexEntry(int fileIndex, int index)
    {
        if (fileIndex < 0 || fileIndex >= _mulIndices.Length)
            return null;
        var indices = _mulIndices[fileIndex];
        if (indices == null || index < 0 || index >= indices.Length)
            return null;
        return indices[index];
    }
    
    /// <summary>
    /// Calculate offset for LOW animations (monsters, equipment): bodies 0-199
    /// </summary>
    private static int CalculateLowGroupOffset(int bodyId)
    {
        // LOW: bodies 0-199, 22 groups, 5 directions each = 110 slots per body
        if (bodyId < LOW_ANIMATION_COUNT)
            return bodyId * LOW_ANIMATION_GROUPS * 5;
        return -1;
    }
    
    /// <summary>
    /// Calculate offset for HIGH animations (animals, old humans): bodies 200-399
    /// </summary>
    private static int CalculateHighGroupOffset(int bodyId)
    {
        // HIGH: bodies 200-399 start after LOW section
        // Start offset = 200 * 110 = 22000
        // Each body has 13 groups * 5 directions = 65 slots
        if (bodyId >= LOW_ANIMATION_COUNT && bodyId < LOW_ANIMATION_COUNT + HIGH_ANIMATION_COUNT)
        {
            int baseOffset = LOW_ANIMATION_COUNT * LOW_ANIMATION_GROUPS * 5; // 22000
            return baseOffset + (bodyId - LOW_ANIMATION_COUNT) * HIGH_ANIMATION_GROUPS * 5;
        }
        return -1;
    }
    
    /// <summary>
    /// Calculate offset for PEOPLE animations (HD humans): bodies 400+
    /// </summary>
    private static int CalculatePeopleGroupOffset(int bodyId)
    {
        // PEOPLE: bodies 400+ start after HIGH section
        // Start offset = 22000 + 200*65 = 35000
        // Each body has 35 groups * 5 directions = 175 slots
        if (bodyId >= LOW_ANIMATION_COUNT + HIGH_ANIMATION_COUNT)
        {
            int baseOffset = LOW_ANIMATION_COUNT * LOW_ANIMATION_GROUPS * 5 + 
                             HIGH_ANIMATION_COUNT * HIGH_ANIMATION_GROUPS * 5; // 35000
            return baseOffset + (bodyId - LOW_ANIMATION_COUNT - HIGH_ANIMATION_COUNT) * PEOPLE_ANIMATION_GROUPS * 5;
        }
        return -1;
    }
    
    /// <summary>
    /// Get the body type for determining animation structure
    /// </summary>
    public BodyType GetBodyType(int bodyId)
    {
        // First check mobtypes.txt if loaded
        if (_mobTypes.TryGetValue(bodyId, out var mobType))
        {
            return mobType.type switch
            {
                "HUMAN" => BodyType.Human,
                "EQUIPMENT" => BodyType.Equipment,
                "MONSTER" => BodyType.Monster,
                "ANIMAL" => BodyType.Animal,
                _ => BodyType.Monster
            };
        }
        
        return GetBodyTypeFallback(bodyId);
    }
    
    private static BodyType GetBodyTypeFallback(int bodyId)
    {
        // Human bodies
        if (bodyId == 400 || bodyId == 401 || 
            bodyId == 605 || bodyId == 606 ||
            bodyId == 666 || bodyId == 667 ||
            bodyId == 744 || bodyId == 745 ||
            bodyId == 200 || bodyId == 201)  // Classic humans
            return BodyType.Human;
        
        if (bodyId >= 0 && bodyId < 200)
            return BodyType.Equipment;
        
        if (bodyId >= 200 && bodyId < 400)
            return BodyType.Animal;
        
        return BodyType.Monster;
    }
    
    public static int GetGroupCount(BodyType type)
    {
        return type switch
        {
            BodyType.Human => 35,
            BodyType.Monster => 22,
            BodyType.Animal => 13,
            BodyType.Equipment => 35,
            _ => 22
        };
    }
    
    /// <summary>
    /// Load mobtypes.txt
    /// </summary>
    private void LoadMobTypes(string dataPath)
    {
        var mobtypesPath = Path.Combine(dataPath, "mobtypes.txt");
        if (!File.Exists(mobtypesPath))
        {
            DebugLog.Write($"mobtypes.txt not found at {mobtypesPath}");
            return;
        }
        
        try
        {
            var lines = File.ReadAllLines(mobtypesPath);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;
                
                var commentIdx = trimmed.IndexOf('#');
                if (commentIdx > 0)
                    trimmed = trimmed.Substring(0, commentIdx).Trim();
                
                var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3 && int.TryParse(parts[0], out int bodyId))
                {
                    string bodyType = parts[1].ToUpperInvariant();
                    int.TryParse(parts[2], out int flags);
                    _mobTypes[bodyId] = (bodyType, flags);
                }
            }
            DebugLog.Write($"Loaded {_mobTypes.Count} body types from mobtypes.txt");
            
            if (_mobTypes.TryGetValue(400, out var type400))
                DebugLog.Write($"  Body 400: {type400.type} flags={type400.flags}");
            if (_mobTypes.TryGetValue(200, out var type200))
                DebugLog.Write($"  Body 200: {type200.type} flags={type200.flags}");
        }
        catch (Exception ex)
        {
            DebugLog.Write($"Error loading mobtypes.txt: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Load bodyconv.def - maps body ID to slots in anim2-5.mul
    /// Format: bodyId anim2_slot anim3_slot anim4_slot anim5_slot
    /// </summary>
    private void LoadBodyConv(string dataPath)
    {
        var bodyconvPath = Path.Combine(dataPath, "bodyconv.def");
        if (!File.Exists(bodyconvPath))
        {
            DebugLog.Write($"bodyconv.def not found at {bodyconvPath}");
            return;
        }
        
        try
        {
            var lines = File.ReadAllLines(bodyconvPath);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;
                
                var commentIdx = trimmed.IndexOf('#');
                if (commentIdx > 0)
                    trimmed = trimmed.Substring(0, commentIdx).Trim();
                
                var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 5 && int.TryParse(parts[0], out int bodyId))
                {
                    var slots = new int[4];
                    for (int i = 0; i < 4; i++)
                    {
                        int.TryParse(parts[i + 1], out slots[i]);
                    }
                    _bodyConv[bodyId] = slots;
                }
            }
            DebugLog.Write($"Loaded {_bodyConv.Count} body conversions from bodyconv.def");
            
            // Log key entries
            if (_bodyConv.TryGetValue(400, out var conv400))
                DebugLog.Write($"  Body 400 -> anim2:{conv400[0]}, anim3:{conv400[1]}, anim4:{conv400[2]}, anim5:{conv400[3]}");
        }
        catch (Exception ex)
        {
            DebugLog.Write($"Error loading bodyconv.def: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Get an animation for a body/action/direction
    /// </summary>
    public Animation? GetAnimation(int bodyId, AnimAction action, AnimDirection direction)
    {
        int storedDir = (int)direction;
        if (storedDir > 4)
        {
            storedDir = 8 - storedDir;
        }
        
        var key = (bodyId, (int)action, storedDir);
        
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }
        
        var anim = LoadAnimation(bodyId, (int)action, storedDir);
        if (anim != null)
        {
            _cache[key] = anim;
        }
        
        return anim;
    }
    
    /// <summary>
    /// Load an animation from the MUL files using ClassicUO's index calculation
    /// </summary>
    private Animation? LoadAnimation(int bodyId, int group, int direction)
    {
        var bodyType = GetBodyType(bodyId);
        int groupOffset = group * 5 + direction;
        
        DebugLog.Write($"AnimLoader: Loading body={bodyId} (type={bodyType}), group={group}, dir={direction}");
        
        // PRIORITY 1: Check bodyconv.def for explicit mapping
        if (_bodyConv.TryGetValue(bodyId, out var convSlots))
        {
            for (int fileIdx = 0; fileIdx < 4; fileIdx++)
            {
                int slot = convSlots[fileIdx];
                if (slot >= 0)
                {
                    int slotsPerBody = bodyType switch
                    {
                        BodyType.Human => 175,
                        BodyType.Equipment => 175,
                        BodyType.Monster => 110,
                        BodyType.Animal => 65,
                        _ => 110
                    };
                    
                    int index = slot * slotsPerBody + groupOffset;
                    int mulFileIdx = fileIdx + 1;
                    
                    DebugLog.Write($"  bodyconv: file={mulFileIdx}, slot={slot}, index={index}");
                    
                    var anim = TryLoadFromFile(mulFileIdx, index, bodyId, group, direction);
                    if (anim != null)
                    {
                        DebugLog.Write($"  SUCCESS via bodyconv!");
                        return anim;
                    }
                }
            }
        }
        
        // PRIORITY 2: For body 400/401, try multiple known indices in all files
        if (bodyId == 400 || bodyId == 401)
        {
            return LoadHumanAnimation(bodyId, group, direction, groupOffset);
        }
        
        // PRIORITY 3: Standard calculation for other bodies
        int anim0Index = -1;
        
        if (bodyId < LOW_ANIMATION_COUNT)
        {
            anim0Index = CalculateLowGroupOffset(bodyId) + groupOffset;
            DebugLog.Write($"  anim.mul LOW: index={anim0Index}");
        }
        else if (bodyId < LOW_ANIMATION_COUNT + HIGH_ANIMATION_COUNT)
        {
            anim0Index = CalculateHighGroupOffset(bodyId) + groupOffset;
            DebugLog.Write($"  anim.mul HIGH: index={anim0Index}");
        }
        else
        {
            anim0Index = CalculatePeopleGroupOffset(bodyId) + groupOffset;
            DebugLog.Write($"  anim.mul PEOPLE: index={anim0Index}");
        }
        
        if (anim0Index >= 0)
        {
            var anim = TryLoadFromFile(0, anim0Index, bodyId, group, direction);
            if (anim != null)
            {
                DebugLog.Write($"  SUCCESS in anim.mul!");
                return anim;
            }
        }
        
        // Try all anim files with simple calculation
        int slotsPerBodyType = bodyType switch
        {
            BodyType.Human => 175,
            BodyType.Monster => 110,
            BodyType.Animal => 65,
            _ => 110
        };
        
        int simpleIndex = bodyId * slotsPerBodyType + groupOffset;
        for (int f = 0; f < 5; f++)
        {
            var anim = TryLoadFromFile(f, simpleIndex, bodyId, group, direction);
            if (anim != null)
            {
                DebugLog.Write($"  SUCCESS in anim{f}.mul with simple index!");
                return anim;
            }
        }
        
        DebugLog.Write($"  No valid animation found for body {bodyId}");
        return null;
    }
    
    /// <summary>
    /// Special handling for body 400/401 (HD humans) - tries multiple approaches
    /// </summary>
    private Animation? LoadHumanAnimation(int bodyId, int group, int direction, int groupOffset)
    {
        DebugLog.Write($"  Special human body {bodyId} handling:");
        
        // Approach 1: Standard PEOPLE offset in anim.mul
        int peopleIndex = 35000 + (bodyId - 400) * 175 + groupOffset;
        DebugLog.Write($"    Approach 1: PEOPLE index {peopleIndex} in anim.mul");
        var anim = TryLoadFromFile(0, peopleIndex, bodyId, group, direction);
        if (anim != null) return anim;
        
        // Approach 2: Direct slot in anim4.mul (slot 0 for body 400)
        int anim4Index = (bodyId - 400) * 175 + groupOffset;
        DebugLog.Write($"    Approach 2: index {anim4Index} in anim4.mul");
        anim = TryLoadFromFile(3, anim4Index, bodyId, group, direction);
        if (anim != null) return anim;
        
        // Approach 3: Try as slot 0 in anim5.mul  
        DebugLog.Write($"    Approach 3: index {anim4Index} in anim5.mul");
        anim = TryLoadFromFile(4, anim4Index, bodyId, group, direction);
        if (anim != null) return anim;
        
        // Approach 4: Simple body*175 in all files
        int simpleIndex = bodyId * 175 + groupOffset;
        DebugLog.Write($"    Approach 4: simple index {simpleIndex} in all files");
        for (int f = 0; f < 5; f++)
        {
            anim = TryLoadFromFile(f, simpleIndex, bodyId, group, direction);
            if (anim != null) return anim;
        }
        
        // Approach 5: Try lower indices that might contain human animations
        // Some clients put humans at the start of anim4/anim5
        int[] testIndices = { groupOffset, 175 + groupOffset, 350 + groupOffset };
        for (int f = 3; f < 5; f++)
        {
            foreach (var idx in testIndices)
            {
                DebugLog.Write($"    Approach 5: trying index {idx} in anim{f+1}.mul");
                anim = TryLoadFromFile(f, idx, bodyId, group, direction);
                if (anim != null) return anim;
            }
        }
        
        DebugLog.Write($"    All approaches failed for body {bodyId}!");
        return null;
    }
    
    private Animation? TryLoadFromFile(int fileIndex, int index, int bodyId, int group, int direction)
    {
        if (fileIndex < 0 || fileIndex >= _mulFiles.Length)
            return null;
            
        var mulFile = _mulFiles[fileIndex];
        var mulIndex = _mulIndices[fileIndex];
        
        if (mulFile == null || mulIndex == null)
            return null;
            
        if (index < 0 || index >= mulIndex.Length)
            return null;
            
        var entry = mulIndex[index];
        if (entry.Lookup < 0 || entry.Length <= 512)
            return null;
        
        DebugLog.Write($"    Found in file {fileIndex} at index {index}: offset={entry.Lookup}, length={entry.Length}");
        
        try
        {
            byte[] data;
            lock (mulFile)
            {
                mulFile.Seek(entry.Lookup, SeekOrigin.Begin);
                data = new byte[entry.Length];
                mulFile.Read(data, 0, entry.Length);
            }
            
            var anim = ParseAnimationData(data, bodyId, group, direction);
            if (anim != null && anim.FrameCount > 0)
            {
                DebugLog.Write($"    Parsed {anim.FrameCount} frames successfully!");
                return anim;
            }
        }
        catch (Exception ex)
        {
            DebugLog.Write($"    Parse error: {ex.Message}");
        }
        
        return null;
    }
    
    /// <summary>
    /// Parse animation data from raw bytes
    /// </summary>
    private Animation? ParseAnimationData(byte[] data, int bodyId, int group, int direction)
    {
        try
        {
            // Read palette (256 colors * 2 bytes = 512 bytes)
            var palette = new ushort[256];
            for (int i = 0; i < 256; i++)
            {
                palette[i] = BitConverter.ToUInt16(data, i * 2);
            }
            
            int offset = 512;
            
            if (offset + 4 > data.Length)
                return null;
                
            int frameCount = BitConverter.ToInt32(data, offset);
            offset += 4;
            
            if (frameCount <= 0 || frameCount > 50)
            {
                DebugLog.Write($"    Invalid frame count: {frameCount}");
                return null;
            }
            
            // Read frame offsets
            var frameOffsets = new int[frameCount];
            for (int i = 0; i < frameCount && offset + 4 <= data.Length; i++)
            {
                frameOffsets[i] = BitConverter.ToInt32(data, offset);
                offset += 4;
            }
            
            // Load frames
            var frames = new List<AnimFrame>();
            int headerEnd = 512;
            
            for (int i = 0; i < frameCount; i++)
            {
                int frameStart = headerEnd + frameOffsets[i];
                if (frameStart >= headerEnd && frameStart + 8 <= data.Length)
                {
                    var frame = LoadFrameData(data, frameStart, palette);
                    if (frame != null)
                        frames.Add(frame);
                }
            }
            
            if (frames.Count == 0)
                return null;
            
            return new Animation
            {
                BodyId = bodyId,
                Action = (AnimAction)group,
                Direction = (AnimDirection)direction,
                Frames = frames.ToArray()
            };
        }
        catch (Exception ex)
        {
            DebugLog.Write($"    ParseAnimationData exception: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Load a single animation frame using UO's run-length encoding format
    /// </summary>
    private AnimFrame? LoadFrameData(byte[] data, int frameOffset, ushort[] palette)
    {
        if (frameOffset < 0 || frameOffset + 8 > data.Length)
            return null;
        
        try
        {
            short centerX = BitConverter.ToInt16(data, frameOffset);
            short centerY = BitConverter.ToInt16(data, frameOffset + 2);
            short width = BitConverter.ToInt16(data, frameOffset + 4);
            short height = BitConverter.ToInt16(data, frameOffset + 6);
            
            if (width <= 0 || height <= 0 || width > 512 || height > 512)
                return null;
            
            var pixels = new uint[width * height];
            int dataOffset = frameOffset + 8;
            
            if (dataOffset + 4 > data.Length)
                return null;
                
            uint header = BitConverter.ToUInt32(data, dataOffset);
            dataOffset += 4;
            
            int runCount = 0;
            
            while (header != 0x7FFF7FFF && runCount < 50000)
            {
                runCount++;
                
                int runLength = (int)(header & 0x0FFF);
                int yOffset = (int)((header >> 12) & 0x3FF);
                int xOffset = (int)((header >> 22) & 0x3FF);
                
                // Sign-extend 10-bit values
                if ((xOffset & 0x200) != 0)
                    xOffset |= unchecked((int)0xFFFFFC00);
                if ((yOffset & 0x200) != 0)
                    yOffset |= unchecked((int)0xFFFFFC00);
                
                int x = xOffset + centerX;
                int y = yOffset + centerY + height;
                
                if (runLength > 0 && runLength <= 512)
                {
                    for (int k = 0; k < runLength && dataOffset < data.Length; k++)
                    {
                        byte paletteIdx = data[dataOffset++];
                        
                        int pixelX = x + k;
                        int pixelY = y;
                        
                        if (pixelX >= 0 && pixelX < width && 
                            pixelY >= 0 && pixelY < height && 
                            paletteIdx > 0)
                        {
                            int pixelIndex = pixelY * width + pixelX;
                            if (pixelIndex >= 0 && pixelIndex < pixels.Length)
                            {
                                pixels[pixelIndex] = UODataReader.Argb1555ToRgba(palette[paletteIdx]);
                            }
                        }
                    }
                }
                else
                {
                    dataOffset += Math.Max(0, Math.Min(runLength, data.Length - dataOffset));
                }
                
                if (dataOffset + 4 > data.Length)
                    break;
                    
                header = BitConverter.ToUInt32(data, dataOffset);
                dataOffset += 4;
            }
            
            var texture = new Texture2D(_graphics, width, height);
            texture.SetData(pixels);
            
            return new AnimFrame
            {
                Texture = texture,
                Width = width,
                Height = height,
                CenterX = centerX,
                CenterY = centerY
            };
        }
        catch
        {
            return null;
        }
    }
    
    public void ClearCache()
    {
        foreach (var anim in _cache.Values)
        {
            foreach (var frame in anim.Frames)
                frame.Texture?.Dispose();
        }
        _cache.Clear();
    }
    
    public void Dispose()
    {
        ClearCache();
        for (int i = 0; i < _mulFiles.Length; i++)
        {
            _mulFiles[i]?.Dispose();
            _mulFiles[i] = null;
            _mulIndices[i] = null;
        }
    }
}
