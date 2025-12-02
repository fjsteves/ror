using System.Text.Json.Serialization;
using RealmOfReality.Shared.Core;
using RealmOfReality.Shared.Serialization;

namespace RealmOfReality.Shared.Graphics;

/// <summary>
/// Definition for a sprite/graphic asset
/// </summary>
public class SpriteDefinition
{
    /// <summary>
    /// Unique ID for this sprite
    /// </summary>
    public ushort Id { get; set; }
    
    /// <summary>
    /// Name/identifier for this sprite
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// Path to the image file (relative to assets folder)
    /// </summary>
    public string ImagePath { get; set; } = "";
    
    /// <summary>
    /// X offset within the image atlas (if using atlas)
    /// </summary>
    public int AtlasX { get; set; }
    
    /// <summary>
    /// Y offset within the image atlas
    /// </summary>
    public int AtlasY { get; set; }
    
    /// <summary>
    /// Width in pixels
    /// </summary>
    public int Width { get; set; }
    
    /// <summary>
    /// Height in pixels
    /// </summary>
    public int Height { get; set; }
    
    /// <summary>
    /// X offset for rendering (to align sprite foot with tile center)
    /// </summary>
    public int OffsetX { get; set; }
    
    /// <summary>
    /// Y offset for rendering
    /// </summary>
    public int OffsetY { get; set; }
    
    /// <summary>
    /// Category for organization
    /// </summary>
    public SpriteCategory Category { get; set; }
    
    /// <summary>
    /// Animation frames (if animated)
    /// </summary>
    public AnimationFrames? Animation { get; set; }
}

/// <summary>
/// Sprite categories
/// </summary>
public enum SpriteCategory
{
    Ground,
    Static,
    Character,
    Monster,
    Item,
    Effect,
    UI
}

/// <summary>
/// Animation frame data
/// </summary>
public class AnimationFrames
{
    /// <summary>
    /// Number of frames in the animation
    /// </summary>
    public int FrameCount { get; set; }
    
    /// <summary>
    /// Width of each frame
    /// </summary>
    public int FrameWidth { get; set; }
    
    /// <summary>
    /// Height of each frame
    /// </summary>
    public int FrameHeight { get; set; }
    
    /// <summary>
    /// Milliseconds per frame
    /// </summary>
    public int FrameDuration { get; set; } = 100;
    
    /// <summary>
    /// Whether the animation loops
    /// </summary>
    public bool Loop { get; set; } = true;
}

/// <summary>
/// Complete sprite for an entity with directional animations
/// </summary>
public class EntitySprite
{
    public ushort EntityTypeId { get; set; }
    public string Name { get; set; } = "";
    
    /// <summary>
    /// Sprite IDs for each direction (8 directions)
    /// </summary>
    public DirectionalSprites Idle { get; set; } = new();
    public DirectionalSprites Walk { get; set; } = new();
    public DirectionalSprites? Run { get; set; }
    public DirectionalSprites? Attack { get; set; }
    public DirectionalSprites? Death { get; set; }
}

/// <summary>
/// Sprite IDs for 8 directions
/// </summary>
public class DirectionalSprites
{
    public ushort North { get; set; }
    public ushort NorthEast { get; set; }
    public ushort East { get; set; }
    public ushort SouthEast { get; set; }
    public ushort South { get; set; }
    public ushort SouthWest { get; set; }
    public ushort West { get; set; }
    public ushort NorthWest { get; set; }
    
    public ushort GetForDirection(Direction dir) => dir switch
    {
        Direction.North => North,
        Direction.NorthEast => NorthEast,
        Direction.East => East,
        Direction.SouthEast => SouthEast,
        Direction.South => South,
        Direction.SouthWest => SouthWest,
        Direction.West => West,
        Direction.NorthWest => NorthWest,
        _ => South
    };
    
    /// <summary>
    /// Set all directions to the same sprite
    /// </summary>
    public void SetAll(ushort spriteId)
    {
        North = NorthEast = East = SouthEast = South = SouthWest = West = NorthWest = spriteId;
    }
}

/// <summary>
/// Tile graphics definition
/// </summary>
public class TileGraphics
{
    public ushort TileId { get; set; }
    public string Name { get; set; } = "";
    public ushort SpriteId { get; set; }
    
    /// <summary>
    /// Alternative sprites for variety (randomly selected)
    /// </summary>
    public ushort[]? Variants { get; set; }
    
    /// <summary>
    /// Whether this tile is animated
    /// </summary>
    public bool Animated { get; set; }
}

/// <summary>
/// Graphics asset database
/// </summary>
public class AssetDatabase
{
    public Dictionary<ushort, SpriteDefinition> Sprites { get; set; } = new();
    public Dictionary<ushort, EntitySprite> EntitySprites { get; set; } = new();
    public Dictionary<ushort, TileGraphics> TileGraphics { get; set; } = new();
    
    public void AddSprite(SpriteDefinition sprite)
    {
        Sprites[sprite.Id] = sprite;
    }
    
    public void AddEntitySprite(EntitySprite sprite)
    {
        EntitySprites[sprite.EntityTypeId] = sprite;
    }
    
    public void AddTileGraphics(TileGraphics tile)
    {
        TileGraphics[tile.TileId] = tile;
    }
    
    public SpriteDefinition? GetSprite(ushort id)
    {
        Sprites.TryGetValue(id, out var sprite);
        return sprite;
    }
    
    public EntitySprite? GetEntitySprite(ushort typeId)
    {
        EntitySprites.TryGetValue(typeId, out var sprite);
        return sprite;
    }
    
    public TileGraphics? GetTileGraphics(ushort tileId)
    {
        TileGraphics.TryGetValue(tileId, out var tile);
        return tile;
    }
    
    /// <summary>
    /// Save database to JSON
    /// </summary>
    public async Task SaveAsync(string path)
    {
        await JsonConfig.SerializeFileAsync(path, this, true);
    }
    
    /// <summary>
    /// Load database from JSON
    /// </summary>
    public static async Task<AssetDatabase> LoadAsync(string path)
    {
        return await JsonConfig.DeserializeFileAsync<AssetDatabase>(path) ?? new AssetDatabase();
    }
    
    /// <summary>
    /// Create default asset database with placeholder graphics
    /// </summary>
    public static AssetDatabase CreateDefault()
    {
        var db = new AssetDatabase();
        
        // Ground tiles
        db.AddTileGraphics(new TileGraphics { TileId = 1, Name = "Grass", SpriteId = 1 });
        db.AddTileGraphics(new TileGraphics { TileId = 2, Name = "Water", SpriteId = 2, Animated = true });
        db.AddTileGraphics(new TileGraphics { TileId = 3, Name = "Stone", SpriteId = 3 });
        db.AddTileGraphics(new TileGraphics { TileId = 4, Name = "Dirt", SpriteId = 4 });
        db.AddTileGraphics(new TileGraphics { TileId = 5, Name = "Sand", SpriteId = 5 });
        
        // Placeholder sprites
        db.AddSprite(new SpriteDefinition
        {
            Id = 1,
            Name = "Grass",
            Width = IsometricHelper.TileWidth,
            Height = IsometricHelper.TileHeight,
            Category = SpriteCategory.Ground
        });
        
        db.AddSprite(new SpriteDefinition
        {
            Id = 2,
            Name = "Water",
            Width = IsometricHelper.TileWidth,
            Height = IsometricHelper.TileHeight,
            Category = SpriteCategory.Ground,
            Animation = new AnimationFrames { FrameCount = 4, FrameWidth = 64, FrameHeight = 32, FrameDuration = 200 }
        });
        
        // Player sprite
        var playerSprite = new EntitySprite
        {
            EntityTypeId = 1,
            Name = "Player Male"
        };
        playerSprite.Idle.SetAll(100);
        playerSprite.Walk.SetAll(101);
        db.AddEntitySprite(playerSprite);
        
        return db;
    }
}

/// <summary>
/// Runtime animation state
/// </summary>
public class AnimationState
{
    public ushort CurrentSpriteId { get; private set; }
    public int CurrentFrame { get; private set; }
    public float FrameTime { get; private set; }
    
    private AnimationFrames? _animation;
    private bool _playing;
    
    public void Play(ushort spriteId, AnimationFrames? animation)
    {
        CurrentSpriteId = spriteId;
        _animation = animation;
        CurrentFrame = 0;
        FrameTime = 0;
        _playing = true;
    }
    
    public void Stop()
    {
        _playing = false;
        CurrentFrame = 0;
        FrameTime = 0;
    }
    
    public void Update(float deltaTime)
    {
        if (!_playing || _animation == null) return;
        
        FrameTime += deltaTime * 1000; // Convert to milliseconds
        
        if (FrameTime >= _animation.FrameDuration)
        {
            FrameTime -= _animation.FrameDuration;
            CurrentFrame++;
            
            if (CurrentFrame >= _animation.FrameCount)
            {
                if (_animation.Loop)
                    CurrentFrame = 0;
                else
                {
                    CurrentFrame = _animation.FrameCount - 1;
                    _playing = false;
                }
            }
        }
    }
}
