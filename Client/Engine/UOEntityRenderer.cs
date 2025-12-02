using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RealmOfReality.Client.Assets;
using RealmOfReality.Shared.Core;
using RealmOfReality.Shared.Entities;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace RealmOfReality.Client.Engine;

/// <summary>
/// Entity state for animation tracking
/// </summary>
public enum EntityState
{
    Standing,
    Walking,
    Running,
    Attacking,
    CastingSpell,
    GettingHit,
    Dying,
    Dead
}

/// <summary>
/// Renders entities using UO animations and assets
/// </summary>
public class UOEntityRenderer
{
    private readonly SpriteBatch _spriteBatch;
    private readonly UOAssetManager _uoAssets;
    private readonly UOAnimationController _animController;
    private readonly AssetManager _fallbackAssets;
    
    // Animation state per entity (keyed by entity ID)
    private readonly Dictionary<ulong, EntityAnimationState> _animStates = new();
    
    // Movement tracking - store last known positions to detect movement
    private readonly Dictionary<ulong, (WorldPosition pos, Direction facing, double lastMoveTime, bool wasMoving)> _lastPositions = new();
    
    // Movement animation duration - how long to keep playing walk animation after a step
    // This should be longer than the movement interval to ensure smooth animation
    // Typical walk interval is 100ms per tile, so we use 250ms to allow for some lag
    private const double MoveAnimDuration = 250.0; // ms - animation continues for this long after movement detected
    
    // Debug: last known state for player
    private EntityState _lastPlayerState = EntityState.Standing;
    
    // Map game TypeId to UO body ID
    private readonly Dictionary<int, int> _typeToBodyMap = new()
    {
        // Player bodies
        [0] = 400,      // Player -> Human Male
        [1] = 401,      // Player Female -> Human Female
        
        // Monsters
        [10] = 723,     // Goblin -> Goblin body
        [20] = 50,      // Skeleton -> Skeleton
        [30] = 225,     // Wolf -> Wolf
        [60] = 7,       // Orc -> Orc
        [70] = 54,      // Troll -> Troll  
        [80] = 24,      // Lich -> Lich
        [90] = 9,       // Daemon -> Daemon
        [100] = 12,     // Dragon -> Dragon
        
        // Vendors/NPCs
        [40] = 400,     // Healer -> Human
        [200] = 400,    // Blacksmith
        [201] = 400,    // Provisioner
        [202] = 400,    // Mage
        [203] = 400,    // Healer vendor
        [204] = 400,    // Innkeeper
        [205] = 401,    // Tailor (female)
        [206] = 401,    // Jeweler (female)
        [207] = 400,    // Banker
    };
    
    public UOEntityRenderer(SpriteBatch spriteBatch, UOAssetManager uoAssets, AssetManager fallbackAssets)
    {
        _spriteBatch = spriteBatch;
        _uoAssets = uoAssets;
        _fallbackAssets = fallbackAssets;
        _animController = new UOAnimationController(uoAssets.Animations!);
    }
    
    /// <summary>
    /// Get or create animation state for an entity
    /// </summary>
    private EntityAnimationState GetAnimState(Entity entity)
    {
        if (!_animStates.TryGetValue(entity.Id.Value, out var state))
        {
            state = new EntityAnimationState
            {
                BodyId = GetBodyId(entity)
            };
            _animStates[entity.Id.Value] = state;
        }
        return state;
    }
    
    /// <summary>
    /// Get UO body ID for an entity
    /// </summary>
    private int GetBodyId(Entity entity)
    {
        if (entity is PlayerEntity player)
        {
            // Use character's body based on gender (if we track it)
            return UOAnimationController.BodyHumanMale;
        }
        
        if (entity is NpcEntity npc)
        {
            if (_typeToBodyMap.TryGetValue(npc.TypeId, out var bodyId))
                return bodyId;
        }
        
        return UOAnimationController.BodyHumanMale;
    }
    
    /// <summary>
    /// Get current entity state - checks actual movement by comparing positions
    /// </summary>
    private EntityState GetEntityState(Entity entity, double currentTimeMs)
    {
        if (entity is Mobile mobile)
        {
            if (mobile.Health <= 0)
                return EntityState.Dead;
            
            // Check for recent movement by comparing position/facing
            bool isActuallyMoving = false;
            
            if (_lastPositions.TryGetValue(entity.Id.Value, out var lastInfo))
            {
                // Check if position changed (using a small epsilon for float comparison)
                float dx = Math.Abs(lastInfo.pos.X - entity.Position.X);
                float dy = Math.Abs(lastInfo.pos.Y - entity.Position.Y);
                float dz = Math.Abs(lastInfo.pos.Z - entity.Position.Z);
                bool posChanged = dx > 0.01f || dy > 0.01f || dz > 0.01f;
                
                // Also check facing change as indicator of movement intent
                bool facingChanged = lastInfo.facing != entity.Facing;
                
                if (posChanged)
                {
                    // Position changed - we ARE moving, update timestamp
                    _lastPositions[entity.Id.Value] = (entity.Position, entity.Facing, currentTimeMs, true);
                    isActuallyMoving = true;
                    
                    // Debug log for player
                    if (entity is PlayerEntity)
                    {
                        DebugLog.Write($"Player MOVE detected: dx={dx:F3} dy={dy:F3}");
                    }
                }
                else if (facingChanged)
                {
                    // Facing changed but position didn't - still update, might be turning
                    _lastPositions[entity.Id.Value] = (entity.Position, entity.Facing, lastInfo.lastMoveTime, lastInfo.wasMoving);
                }
                else
                {
                    // Position same - check if we're within animation window
                    double timeSinceMove = currentTimeMs - lastInfo.lastMoveTime;
                    if (timeSinceMove < MoveAnimDuration && lastInfo.wasMoving)
                    {
                        isActuallyMoving = true;
                    }
                    else if (timeSinceMove >= MoveAnimDuration && lastInfo.wasMoving)
                    {
                        // Animation window expired, clear the moving flag
                        _lastPositions[entity.Id.Value] = (entity.Position, entity.Facing, lastInfo.lastMoveTime, false);
                    }
                }
            }
            else
            {
                // First time seeing this entity
                _lastPositions[entity.Id.Value] = (entity.Position, entity.Facing, currentTimeMs, false);
            }
            
            // Determine final state
            EntityState result;
            if (isActuallyMoving || mobile.IsMoving)
            {
                result = mobile.IsRunning ? EntityState.Running : EntityState.Walking;
            }
            else
            {
                result = EntityState.Standing;
            }
            
            // Debug: log state changes for player
            if (entity is PlayerEntity && result != _lastPlayerState)
            {
                DebugLog.Write($"Player state: {_lastPlayerState} -> {result} (isActuallyMoving={isActuallyMoving}, mobile.IsMoving={mobile.IsMoving})");
                _lastPlayerState = result;
            }
            
            return result;
        }
        return EntityState.Standing;
    }
    
    /// <summary>
    /// Convert game direction to animation direction
    /// </summary>
    private (AnimDirection dir, bool mirror) ConvertDirection(Direction facing)
    {
        int gameDir = (int)facing;
        return UOAnimationController.ConvertDirection(gameDir);
    }
    
    // Track cumulative time for movement detection
    private double _totalTimeMs = 0;
    
    /// <summary>
    /// Update and draw an entity with UO animations
    /// </summary>
    public void DrawEntity(Entity entity, ScreenPosition screenPos, double deltaMs)
    {
        _totalTimeMs += deltaMs;
        
        var animState = GetAnimState(entity);
        var bodyId = animState.BodyId;
        var entityState = GetEntityState(entity, _totalTimeMs);
        var (direction, mirror) = ConvertDirection(entity.Facing);
        
        // Determine animation group based on state
        int animGroup = entityState switch
        {
            EntityState.Walking => _animController.GetWalkGroup(bodyId),
            EntityState.Running => _animController.GetWalkGroup(bodyId, running: true),
            EntityState.Attacking => _animController.GetAttackGroup(bodyId),
            EntityState.CastingSpell => _animController.GetCastGroup(bodyId),
            EntityState.GettingHit => _animController.GetHitGroup(bodyId),
            EntityState.Dying => _animController.GetDeathGroup(bodyId),
            EntityState.Dead => _animController.GetDeathGroup(bodyId),
            _ => _animController.GetStandGroup(bodyId)
        };
        
        // Debug: Log state changes for player
        if (entity is PlayerEntity && animState.CurrentGroup != animGroup)
        {
            DebugLog.Write($"Player anim change: state={entityState}, group={animState.CurrentGroup}->{animGroup}, dir={direction}");
        }
        
        // Check if we need to change animation
        if (animState.CurrentGroup != animGroup || animState.Direction != direction)
        {
            animState.CurrentGroup = animGroup;
            animState.Direction = direction;
            animState.Mirror = mirror;
            
            // Load new animation
            var animation = _animController.GetAnimation(bodyId, animGroup, direction);
            
            // Fallback: If walking animation not found, try stand animation
            if (animation == null && (entityState == EntityState.Walking || entityState == EntityState.Running))
            {
                int standGroup = _animController.GetStandGroup(bodyId);
                DebugLog.Write($"Fallback: walk anim not found, trying stand group {standGroup}");
                animation = _animController.GetAnimation(bodyId, standGroup, direction);
            }
            
            // Debug: Log animation loading for player
            if (entity is PlayerEntity)
            {
                DebugLog.Write($"Player loading anim: body={bodyId}, group={animGroup}, dir={direction}, found={animation != null}, frames={animation?.FrameCount ?? 0}");
            }
            
            int frameDelay = entityState switch
            {
                EntityState.Walking => UOAnimationController.WalkFrameDelay,
                EntityState.Running => UOAnimationController.RunFrameDelay,
                EntityState.CastingSpell => UOAnimationController.CastFrameDelay,
                EntityState.Attacking => UOAnimationController.AttackFrameDelay,
                _ => UOAnimationController.DefaultFrameDelay
            };
            
            bool loop = entityState != EntityState.Dying && entityState != EntityState.Dead;
            animState.SetAnimation(animation, frameDelay, loop);
        }
        
        // Update animation
        animState.Update(deltaMs);
        
        // Draw current frame
        var frame = animState.GetCurrentFrame();
        if (frame?.Texture != null)
        {
            DrawAnimationFrame(frame, screenPos, animState.Mirror, entity.Hue);
        }
        else
        {
            // Fallback to static sprite
            DrawFallbackSprite(entity, screenPos);
        }
    }
    
    /// <summary>
    /// Draw an animation frame at the specified position
    /// </summary>
    private void DrawAnimationFrame(AnimFrame frame, ScreenPosition screenPos, bool mirror, ushort hue)
    {
        if (frame.Texture == null) return;
        
        // Calculate draw position using frame center offsets
        // UO frames have center point stored as offset from top-left
        var drawPos = new Vector2(
            screenPos.X - frame.CenterX,
            screenPos.Y - frame.CenterY
        );
        
        // Apply hue tint
        var tint = XnaColor.White;
        if (hue != 0 && _uoAssets.Hues != null)
        {
            var hueEntry = _uoAssets.Hues.GetHue(hue);
            if (hueEntry != null)
            {
                tint = hueEntry.PrimaryColor;
            }
        }
        
        var effects = mirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        
        _spriteBatch.Draw(
            frame.Texture,
            drawPos,
            null,
            tint,
            0f,
            Vector2.Zero,
            1f,
            effects,
            0f
        );
    }
    
    /// <summary>
    /// Draw fallback sprite when UO animation not available
    /// </summary>
    private void DrawFallbackSprite(Entity entity, ScreenPosition screenPos)
    {
        var sprite = entity switch
        {
            PlayerEntity => _fallbackAssets.PlayerSprite,
            NpcEntity npc when npc.TypeId == 999 => _fallbackAssets.CorpseSprite,
            NpcEntity npc when npc.TypeId == 100 => _fallbackAssets.DragonSprite,
            NpcEntity npc when npc.TypeId == 50 => _fallbackAssets.AnkhSprite,
            NpcEntity npc when npc.TypeId == 40 => _fallbackAssets.HealerSprite,
            NpcEntity npc when npc.TypeId == 10 => _fallbackAssets.GoblinSprite,
            NpcEntity npc when npc.TypeId == 20 => _fallbackAssets.SkeletonSprite,
            NpcEntity npc when npc.TypeId == 30 => _fallbackAssets.WolfSprite,
            _ => _fallbackAssets.NpcSprite
        };
        
        var drawPos = new Vector2(
            screenPos.X - sprite.Width / 2,
            screenPos.Y - sprite.Height + 16  // Offset for feet
        );
        
        var effects = SpriteEffects.None;
        if (entity.Facing == Direction.West ||
            entity.Facing == Direction.NorthWest ||
            entity.Facing == Direction.SouthWest)
        {
            effects = SpriteEffects.FlipHorizontally;
        }
        
        _spriteBatch.Draw(sprite, drawPos, null, XnaColor.White, 0f, Vector2.Zero, 1f, effects, 0f);
    }
    
    /// <summary>
    /// Play attack animation for entity
    /// </summary>
    public void PlayAttackAnimation(Entity entity, int attackType = 0)
    {
        var state = GetAnimState(entity);
        var bodyId = state.BodyId;
        var (direction, mirror) = ConvertDirection(entity.Facing);
        
        int animGroup = _animController.GetAttackGroup(bodyId, attackType);
        var animation = _animController.GetAnimation(bodyId, animGroup, direction);
        
        state.CurrentGroup = animGroup;
        state.Direction = direction;
        state.Mirror = mirror;
        state.SetAnimation(animation, UOAnimationController.AttackFrameDelay, false);
    }
    
    /// <summary>
    /// Play spell cast animation for entity
    /// </summary>
    public void PlayCastAnimation(Entity entity, bool targetedSpell = true)
    {
        var state = GetAnimState(entity);
        var bodyId = state.BodyId;
        var (direction, mirror) = ConvertDirection(entity.Facing);
        
        int animGroup = _animController.GetCastGroup(bodyId, targetedSpell);
        var animation = _animController.GetAnimation(bodyId, animGroup, direction);
        
        state.CurrentGroup = animGroup;
        state.Direction = direction;
        state.Mirror = mirror;
        state.SetAnimation(animation, UOAnimationController.CastFrameDelay, false);
    }
    
    /// <summary>
    /// Play hit reaction animation for entity
    /// </summary>
    public void PlayHitAnimation(Entity entity)
    {
        var state = GetAnimState(entity);
        var bodyId = state.BodyId;
        var (direction, mirror) = ConvertDirection(entity.Facing);
        
        int animGroup = _animController.GetHitGroup(bodyId);
        var animation = _animController.GetAnimation(bodyId, animGroup, direction);
        
        state.CurrentGroup = animGroup;
        state.Direction = direction;
        state.Mirror = mirror;
        state.SetAnimation(animation, 80, false);
    }
    
    /// <summary>
    /// Play death animation for entity
    /// </summary>
    public void PlayDeathAnimation(Entity entity)
    {
        var state = GetAnimState(entity);
        var bodyId = state.BodyId;
        var (direction, mirror) = ConvertDirection(entity.Facing);
        
        int animGroup = _animController.GetDeathGroup(bodyId);
        var animation = _animController.GetAnimation(bodyId, animGroup, direction);
        
        state.CurrentGroup = animGroup;
        state.Direction = direction;
        state.Mirror = mirror;
        state.SetAnimation(animation, 120, false);
    }
    
    /// <summary>
    /// Clean up animation states for removed entities
    /// </summary>
    public void CleanupRemovedEntities(IEnumerable<ulong> activeEntityIds)
    {
        var activeSet = new HashSet<ulong>(activeEntityIds);
        var toRemove = _animStates.Keys.Where(id => !activeSet.Contains(id)).ToList();
        
        foreach (var id in toRemove)
        {
            _animStates.Remove(id);
            _lastPositions.Remove(id);
        }
    }
}
