using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RealmOfReality.Client.Assets;

/// <summary>
/// UO Body types - determines animation structure
/// Based on ClassicUO's ANIMATION_GROUPS_TYPE
/// </summary>
public enum UOBodyType
{
    Human = 0,      // Body 400 (male), 401 (female) - 35 actions, 5 directions
    Animal = 1,     // Regular animals - 13 actions, 5 directions
    Monster = 2,    // Monsters - 22 actions, 5 directions  
    SeaMonster = 3, // Sea creatures - 4 actions
    Equipment = 4   // Equipment layers
}

/// <summary>
/// Animation groups for Human body type (People Animation Groups)
/// Based on ClassicUO's PEOPLE_ANIMATION_GROUP
/// </summary>
public enum HumanAnimGroup
{
    WalkUnarmed = 0,
    WalkArmed = 1,
    RunUnarmed = 2,
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
    FidgetYawn = 33
}

/// <summary>
/// Animation groups for Monster body type
/// </summary>
public enum MonsterAnimGroup
{
    Walk = 0,
    Stand = 1,
    Die1 = 2,
    Die2 = 3,
    Attack1 = 4,
    Attack2 = 5,
    Attack3 = 6,
    Pillage = 7,
    GetHitFidget = 8,
    GetHit2 = 9,
    GetHit3 = 10,
    Misc = 11,
    Attack4 = 12,
    Attack5 = 13,
    Flying = 14,
    Falling = 15,
    LandingLight = 16,
    Die3 = 17,
    LandingHard = 18,
    TakeOff = 19,
    GetHit4 = 20
}

/// <summary>
/// Animation groups for Animal body type
/// </summary>
public enum AnimalAnimGroup
{
    Walk = 0,
    Run = 1,
    Stand = 2,
    Eat = 3,
    Unknown = 4,
    Attack1 = 5,
    Die = 6,
    Fidget1 = 7,
    Attack2 = 8,
    Fidget2 = 9,
    Die2 = 10,
    Run2 = 11
}

/// <summary>
/// Handles UO character/creature animations with proper body type detection
/// </summary>
public class UOAnimationController
{
    private readonly AnimLoader _animLoader;
    private readonly Dictionary<int, UOBodyType> _bodyTypes = new();
    
    // Standard UO body IDs
    public const int BodyHumanMale = 400;
    public const int BodyHumanFemale = 401;
    public const int BodyElfMale = 605;
    public const int BodyElfFemale = 606;
    public const int BodyGargoyleMale = 666;
    public const int BodyGargoyleFemale = 667;
    
    // Frame timing (milliseconds per frame)
    // ClassicUO uses 80ms as the base frame interval
    // delay_milliseconds = (frameInterval + 1) * 80
    // Walk: frameInterval=0 -> 80ms per frame
    // Run: faster, approximately 50ms per frame
    public const int DefaultFrameDelay = 100;
    public const int WalkFrameDelay = 80;    // ClassicUO base: 80ms
    public const int RunFrameDelay = 50;     // Faster for running
    public const int CastFrameDelay = 120;
    public const int AttackFrameDelay = 80;
    
    public UOAnimationController(AnimLoader animLoader)
    {
        _animLoader = animLoader;
        InitializeBodyTypes();
    }
    
    /// <summary>
    /// Initialize known body types
    /// </summary>
    private void InitializeBodyTypes()
    {
        // Humans
        _bodyTypes[400] = UOBodyType.Human;
        _bodyTypes[401] = UOBodyType.Human;
        
        // Elves
        _bodyTypes[605] = UOBodyType.Human;
        _bodyTypes[606] = UOBodyType.Human;
        
        // Gargoyles
        _bodyTypes[666] = UOBodyType.Human;
        _bodyTypes[667] = UOBodyType.Human;
        
        // Common monsters (examples)
        _bodyTypes[1] = UOBodyType.Monster;   // Ogre
        _bodyTypes[2] = UOBodyType.Monster;   // Ettin
        _bodyTypes[3] = UOBodyType.Monster;   // Zombie
        _bodyTypes[4] = UOBodyType.Monster;   // Gargoyle (monster form)
        _bodyTypes[5] = UOBodyType.Monster;   // Eagle
        _bodyTypes[9] = UOBodyType.Monster;   // Daemon
        _bodyTypes[12] = UOBodyType.Monster;  // Dragon
        _bodyTypes[50] = UOBodyType.Monster;  // Skeleton
        _bodyTypes[57] = UOBodyType.Monster;  // Skeleton with axe
        
        // Animals
        _bodyTypes[200] = UOBodyType.Animal;  // Horse
        _bodyTypes[201] = UOBodyType.Animal;  // Cat
        _bodyTypes[202] = UOBodyType.Animal;  // Alligator
        _bodyTypes[203] = UOBodyType.Animal;  // Pig
        _bodyTypes[204] = UOBodyType.Animal;  // Horse variant
        _bodyTypes[205] = UOBodyType.Animal;  // Deer
        _bodyTypes[206] = UOBodyType.Animal;  // Wolf (legacy)
        _bodyTypes[208] = UOBodyType.Animal;  // Bear
        _bodyTypes[209] = UOBodyType.Animal;  // Giant Rat
        _bodyTypes[215] = UOBodyType.Animal;  // Cow
        _bodyTypes[217] = UOBodyType.Animal;  // Dog
        _bodyTypes[219] = UOBodyType.Animal;  // Gorilla
        _bodyTypes[225] = UOBodyType.Animal;  // Panther
        _bodyTypes[227] = UOBodyType.Animal;  // Rabbit
        _bodyTypes[234] = UOBodyType.Animal;  // Wolf
    }
    
    /// <summary>
    /// Get the body type for a given body ID
    /// </summary>
    public UOBodyType GetBodyType(int bodyId)
    {
        if (_bodyTypes.TryGetValue(bodyId, out var type))
            return type;
        
        // Guess based on ID range
        if (bodyId >= 400 && bodyId <= 401)
            return UOBodyType.Human;
        if (bodyId >= 605 && bodyId <= 606)
            return UOBodyType.Human;
        if (bodyId >= 666 && bodyId <= 667)
            return UOBodyType.Human;
        if (bodyId >= 200 && bodyId < 400)
            return UOBodyType.Animal;
        
        return UOBodyType.Monster;
    }
    
    /// <summary>
    /// Convert game direction to UO animation direction
    /// 
    /// Game world coordinate system:
    /// - X increases going East (right on screen in isometric)
    /// - Y increases going South (down on screen in isometric)
    /// 
    /// Game Direction enum:
    /// - North (0) = -Y = up-left on screen
    /// - NorthEast (1) = +X,-Y = up on screen
    /// - East (2) = +X = up-right on screen  
    /// - SouthEast (3) = +X,+Y = right on screen
    /// - South (4) = +Y = down-right on screen
    /// - SouthWest (5) = -X,+Y = down on screen
    /// - West (6) = -X = down-left on screen
    /// - NorthWest (7) = -X,-Y = left on screen
    /// 
    /// UO Animation directions stored in files (only 5 directions):
    /// - Index 0 = South (facing down-right, toward camera) 
    /// - Index 1 = Southwest (facing down)
    /// - Index 2 = West (facing down-left)
    /// - Index 3 = Northwest (facing left)
    /// - Index 4 = North (facing up-left, away from camera)
    /// 
    /// ISOMETRIC MAPPING: World directions map to screen directions rotated 45°:
    /// - World NorthWest → Screen UP → AnimDirection.North
    /// - World North → Screen UP-RIGHT → AnimDirection.NorthWest + mirror
    /// - World NorthEast → Screen RIGHT → AnimDirection.West + mirror
    /// - World East → Screen DOWN-RIGHT → AnimDirection.SouthWest + mirror
    /// - World SouthEast → Screen DOWN → AnimDirection.South
    /// - World South → Screen DOWN-LEFT → AnimDirection.SouthWest
    /// - World SouthWest → Screen LEFT → AnimDirection.West
    /// - World West → Screen UP-LEFT → AnimDirection.NorthWest
    /// </summary>
    public static (AnimDirection dir, bool mirror) ConvertDirection(int gameDirection)
    {
        int gameDir = gameDirection & 7;
        
        // Map game direction to animation file index with mirror flag (ISOMETRIC CORRECTED)
        return gameDir switch
        {
            0 => (AnimDirection.NorthWest, true),   // Game N (world) -> Screen UP-RIGHT -> NW + mirror
            1 => (AnimDirection.West, true),        // Game NE -> Screen RIGHT -> W + mirror
            2 => (AnimDirection.SouthWest, true),   // Game E -> Screen DOWN-RIGHT -> SW + mirror
            3 => (AnimDirection.South, false),      // Game SE -> Screen DOWN -> South
            4 => (AnimDirection.SouthWest, false),  // Game S -> Screen DOWN-LEFT -> SW
            5 => (AnimDirection.West, false),       // Game SW -> Screen LEFT -> W
            6 => (AnimDirection.NorthWest, false),  // Game W -> Screen UP-LEFT -> NW
            7 => (AnimDirection.North, false),      // Game NW -> Screen UP -> North
            _ => (AnimDirection.South, false)
        };
    }
    
    /// <summary>
    /// Get animation group for standing
    /// </summary>
    public int GetStandGroup(int bodyId, bool armed = false, bool twoHanded = false)
    {
        var bodyType = GetBodyType(bodyId);
        
        return bodyType switch
        {
            UOBodyType.Human => armed ? 
                (twoHanded ? (int)HumanAnimGroup.StandTwoHanded : (int)HumanAnimGroup.StandOneHanded) :
                (int)HumanAnimGroup.Stand,
            UOBodyType.Animal => (int)AnimalAnimGroup.Stand,
            UOBodyType.Monster => (int)MonsterAnimGroup.Stand,
            _ => 4 // Default stand
        };
    }
    
    /// <summary>
    /// Get animation group for walking
    /// </summary>
    public int GetWalkGroup(int bodyId, bool armed = false, bool running = false, bool mounted = false)
    {
        var bodyType = GetBodyType(bodyId);
        
        if (bodyType == UOBodyType.Human)
        {
            if (mounted)
                return running ? (int)HumanAnimGroup.OnMountRun : (int)HumanAnimGroup.OnMountWalk;
            if (running)
                return armed ? (int)HumanAnimGroup.RunArmed : (int)HumanAnimGroup.RunUnarmed;
            return armed ? (int)HumanAnimGroup.WalkArmed : (int)HumanAnimGroup.WalkUnarmed;
        }
        else if (bodyType == UOBodyType.Animal)
        {
            return running ? (int)AnimalAnimGroup.Run : (int)AnimalAnimGroup.Walk;
        }
        else // Monster
        {
            return (int)MonsterAnimGroup.Walk;
        }
    }
    
    /// <summary>
    /// Get animation group for attacking
    /// </summary>
    public int GetAttackGroup(int bodyId, int attackType = 0, bool mounted = false)
    {
        var bodyType = GetBodyType(bodyId);
        
        if (bodyType == UOBodyType.Human)
        {
            if (mounted)
                return (int)HumanAnimGroup.OnMountAttack;
            
            return attackType switch
            {
                0 => (int)HumanAnimGroup.AttackOneHanded,
                1 => (int)HumanAnimGroup.AttackTwoHandedDown,
                2 => (int)HumanAnimGroup.AttackTwoHandedWide,
                3 => (int)HumanAnimGroup.AttackBow,
                4 => (int)HumanAnimGroup.AttackCrossbow,
                _ => (int)HumanAnimGroup.AttackUnarmed
            };
        }
        else if (bodyType == UOBodyType.Animal)
        {
            return (int)AnimalAnimGroup.Attack1;
        }
        else // Monster
        {
            return (int)MonsterAnimGroup.Attack1 + (attackType % 3);
        }
    }
    
    /// <summary>
    /// Get animation group for casting spells
    /// </summary>
    public int GetCastGroup(int bodyId, bool targetedSpell = true, bool mounted = false)
    {
        var bodyType = GetBodyType(bodyId);
        
        if (bodyType == UOBodyType.Human)
        {
            // CastDirected (15) for targeted spells, CastArea (16) for area spells
            return targetedSpell ? (int)HumanAnimGroup.CastDirected : (int)HumanAnimGroup.CastArea;
        }
        else if (bodyType == UOBodyType.Monster)
        {
            return (int)MonsterAnimGroup.Attack3; // Monsters use attack animation for casting
        }
        
        return (int)HumanAnimGroup.CastDirected;
    }
    
    /// <summary>
    /// Get animation group for getting hit
    /// </summary>
    public int GetHitGroup(int bodyId)
    {
        var bodyType = GetBodyType(bodyId);
        
        return bodyType switch
        {
            UOBodyType.Human => (int)HumanAnimGroup.GetHit,
            UOBodyType.Monster => (int)MonsterAnimGroup.GetHitFidget,
            _ => (int)HumanAnimGroup.GetHit
        };
    }
    
    /// <summary>
    /// Get animation group for dying
    /// </summary>
    public int GetDeathGroup(int bodyId, bool variant = false)
    {
        var bodyType = GetBodyType(bodyId);
        
        return bodyType switch
        {
            UOBodyType.Human => variant ? (int)HumanAnimGroup.Die2 : (int)HumanAnimGroup.Die1,
            UOBodyType.Animal => variant ? (int)AnimalAnimGroup.Die2 : (int)AnimalAnimGroup.Die,
            UOBodyType.Monster => variant ? (int)MonsterAnimGroup.Die2 : (int)MonsterAnimGroup.Die1,
            _ => (int)HumanAnimGroup.Die1
        };
    }
    
    /// <summary>
    /// Get animation for a specific state
    /// </summary>
    public Animation? GetAnimation(int bodyId, int animGroup, AnimDirection direction)
    {
        // Convert the generic AnimAction to specific group
        return _animLoader.GetAnimation(bodyId, (AnimAction)animGroup, direction);
    }
    
    /// <summary>
    /// Calculate animation file index for humans
    /// Based on ClassicUO's calculation
    /// </summary>
    public static int CalculateHumanAnimIndex(int bodyId, int animGroup, int direction)
    {
        // Humans have 35 animation groups, 5 directions each
        const int actionsPerBody = 35;
        const int directionsPerAction = 5;
        
        // Map body ID offset
        int bodyOffset = bodyId switch
        {
            400 => 0,   // Human male
            401 => 1,   // Human female
            _ => 0
        };
        
        // Calculate index
        return bodyOffset * actionsPerBody * directionsPerAction +
               animGroup * directionsPerAction +
               direction;
    }
}

/// <summary>
/// Manages animation state for an entity
/// </summary>
public class EntityAnimationState
{
    public int BodyId { get; set; } = 400; // Default to human male
    public int CurrentGroup { get; set; } = (int)HumanAnimGroup.Stand;
    public AnimDirection Direction { get; set; } = AnimDirection.South;
    public bool Mirror { get; set; } = false;
    public int CurrentFrame { get; set; } = 0;
    public double FrameTimer { get; set; } = 0;
    public int FrameDelay { get; set; } = 100;
    public bool IsLooping { get; set; } = true;
    public bool IsFinished { get; set; } = false;
    public Animation? CurrentAnimation { get; set; }
    
    /// <summary>
    /// Update animation frame
    /// </summary>
    public void Update(double deltaMs)
    {
        if (CurrentAnimation == null || CurrentAnimation.FrameCount == 0)
            return;
        
        FrameTimer += deltaMs;
        
        while (FrameTimer >= FrameDelay)
        {
            FrameTimer -= FrameDelay;
            CurrentFrame++;
            
            if (CurrentFrame >= CurrentAnimation.FrameCount)
            {
                if (IsLooping)
                    CurrentFrame = 0;
                else
                {
                    CurrentFrame = CurrentAnimation.FrameCount - 1;
                    IsFinished = true;
                }
            }
        }
    }
    
    /// <summary>
    /// Get current frame texture
    /// </summary>
    public AnimFrame? GetCurrentFrame()
    {
        if (CurrentAnimation == null || CurrentFrame >= CurrentAnimation.FrameCount)
            return null;
        
        return CurrentAnimation.Frames[CurrentFrame];
    }
    
    /// <summary>
    /// Set new animation
    /// </summary>
    public void SetAnimation(Animation? animation, int frameDelay = 100, bool loop = true)
    {
        CurrentAnimation = animation;
        CurrentFrame = 0;
        FrameTimer = 0;
        FrameDelay = frameDelay;
        IsLooping = loop;
        IsFinished = false;
    }
}
