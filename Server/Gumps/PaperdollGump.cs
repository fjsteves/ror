using RealmOfReality.Shared.Gumps;
using RealmOfReality.Shared.Entities;
using RealmOfReality.Shared.Items;

namespace RealmOfReality.Server.Gumps;

/// <summary>
/// UO-authentic paperdoll gump
/// 
/// Now sends body + equipment layer data so client can render the character.
/// </summary>
public class PaperdollGump : Gump
{
    public const uint GumpId = 0x07d0; // Self paperdoll type ID
    public override uint TypeId => GumpId;
    
    // Base gump graphics - these are the paperdoll FRAME/BACKGROUND
    private const int GumpSelf = 0x07d0;      // 2000 - Paperdoll background (self)
    private const int GumpOther = 0x07d1;     // 2001 - Paperdoll background (other player)
    
    // Body gump IDs (for paperdoll rendering)
    private const int MaleBodyGump = 12;      // Male human body
    private const int FemaleBodyGump = 13;    // Female human body
    
    // Paperdoll equipment offset bases
    private const int MaleGumpOffset = 50000;
    private const int FemaleGumpOffset = 60000;
    
    // Button graphics (normal, pressed)
    private const int BtnHelpNormal = 0x07ef;
    private const int BtnHelpPressed = 0x07f0;
    
    private const int BtnOptionsNormal = 0x07d6;
    private const int BtnOptionsPressed = 0x07d7;
    
    private const int BtnLogoutNormal = 0x07d9;
    private const int BtnLogoutPressed = 0x07da;
    
    private const int BtnQuestsNormal = 0x57b5;
    private const int BtnQuestsPressed = 0x57b7;
    
    private const int BtnSkillsNormal = 0x07df;
    private const int BtnSkillsPressed = 0x07e0;
    
    private const int BtnGuildNormal = 0x57b2;
    private const int BtnGuildPressed = 0x57b4;
    
    private const int BtnPeaceNormal = 0x07e5;
    private const int BtnPeacePressed = 0x07e6;
    
    private const int BtnWarNormal = 0x07e8;
    private const int BtnWarPressed = 0x07e9;
    
    private const int BtnStatusNormal = 0x07eb;
    private const int BtnStatusPressed = 0x07ec;
    
    // Other graphics
    private const int GumpProfile = 0x07d2;     // 2002 - Profile scroll icon
    private const int GumpVirtue = 0x0071;      // 113 - Virtue button
    private const int GumpCombatBook = 0x2b34;  // 11060 - Combat abilities book
    private const int GumpRacialBook = 0x2b28;  // 11048 - Racial abilities book
    
    // Button IDs for response handling
    public const int ButtonHelp = 1;
    public const int ButtonOptions = 2;
    public const int ButtonLogout = 3;
    public const int ButtonJournal = 4;
    public const int ButtonQuests = 5;
    public const int ButtonSkills = 6;
    public const int ButtonGuild = 7;
    public const int ButtonPeaceWar = 8;
    public const int ButtonStatus = 9;
    public const int ButtonProfile = 10;
    public const int ButtonVirtue = 11;
    public const int ButtonCombatBook = 12;
    public const int ButtonRacialBook = 13;
    
    // Equipment layer button base (100 + layer = unequip that layer)
    public const int ButtonEquipLayerBase = 100;
    
    private readonly PlayerEntity _player;
    private readonly bool _isSelf;
    private readonly bool _isWarMode;
    
    public PaperdollGump(PlayerEntity player, bool isSelf, bool isWarMode = false)
        : base(100, 100)
    {
        _player = player;
        _isSelf = isSelf;
        _isWarMode = isWarMode;
        
        Build();
    }
    
    private void Build()
    {
        AddPage(0);
        
        // Main paperdoll background - this is just the frame
        AddImage(0, 0, _isSelf ? GumpSelf : GumpOther);
        
        // Add the paperdoll body element with all equipment
        AddPaperdollBody();
        
        if (_isSelf)
        {
            // Buttons on right side (Y positions: 44 + 27*n)
            AddButton(185, 44 + 27 * 0, BtnHelpNormal, BtnHelpPressed, ButtonHelp);
            AddButton(185, 44 + 27 * 1, BtnOptionsNormal, BtnOptionsPressed, ButtonOptions);
            AddButton(185, 44 + 27 * 2, BtnLogoutNormal, BtnLogoutPressed, ButtonLogout);
            AddButton(185, 44 + 27 * 3, BtnQuestsNormal, BtnQuestsPressed, ButtonQuests);
            AddButton(185, 44 + 27 * 4, BtnSkillsNormal, BtnSkillsPressed, ButtonSkills);
            AddButton(185, 44 + 27 * 5, BtnGuildNormal, BtnGuildPressed, ButtonGuild);
            
            // Peace/War toggle
            if (_isWarMode)
                AddButton(185, 44 + 27 * 6, BtnWarNormal, BtnWarPressed, ButtonPeaceWar);
            else
                AddButton(185, 44 + 27 * 6, BtnPeaceNormal, BtnPeacePressed, ButtonPeaceWar);
            
            // Profile and book buttons at bottom
            AddButton(39, 196, GumpProfile, GumpProfile, ButtonProfile);
            AddButton(156, 200, GumpCombatBook, GumpCombatBook, ButtonCombatBook);
            AddButton(23, 200, GumpRacialBook, GumpRacialBook, ButtonRacialBook);
        }
        else
        {
            AddButton(25, 196, GumpProfile, GumpProfile, ButtonProfile);
        }
        
        // Status button
        AddButton(185, 44 + 27 * 7, BtnStatusNormal, BtnStatusPressed, ButtonStatus);
        
        // Virtue menu at top
        AddButton(80, 4, GumpVirtue, GumpVirtue, ButtonVirtue);
        
        // Character name at bottom
        var displayName = string.IsNullOrEmpty(_player.Name) ? "Unknown" : _player.Name;
        AddLabel(39, 262, 0x0386, displayName);
    }
    
    /// <summary>
    /// Add paperdoll body element with equipment layers
    /// </summary>
    private void AddPaperdollBody()
    {
        bool isFemale = _player.Gender == 1;
        int gumpOffset = isFemale ? FemaleGumpOffset : MaleGumpOffset;
        
        var body = new GumpPaperdollBody
        {
            X = 8,
            Y = 19,
            BodyId = (ushort)(isFemale ? FemaleBodyGump : MaleBodyGump),
            Gender = (byte)_player.Gender,
            SkinHue = _player.SkinHue,
            HairStyle = _player.HairStyle,
            HairHue = _player.HairHue,
            BeardStyle = _player.BeardStyle,
            BeardHue = _player.BeardHue
        };
        
        // Add equipment layers in render order
        foreach (var (layer, item) in _player.Equipment.GetLayersInRenderOrder())
        {
            if (item?.Definition == null) continue;
            
            // Calculate paperdoll gump ID from item
            // Use GumpId if set, otherwise calculate from animation ID
            int gumpId = item.Definition.GumpId;
            if (gumpId == 0)
            {
                // Fallback: use sprite ID + offset
                gumpId = item.Definition.SpriteId + gumpOffset;
            }
            
            body.Layers.Add(new PaperdollLayer
            {
                Layer = (byte)layer,
                Serial = item.Id.Value,
                ItemId = item.Definition.SpriteId,
                GumpId = (ushort)gumpId,
                Hue = item.Definition.Hue
            });
        }
        
        Elements.Add(body);
    }
    
    public override void OnResponse(object player, GumpResponse response)
    {
        // Fire event for the packet handler to process
        var playerEntity = player as PlayerEntity;
        if (playerEntity == null) return;
        
        // Check for equipment layer unequip buttons
        if (response.ButtonId >= ButtonEquipLayerBase && response.ButtonId < ButtonEquipLayerBase + 30)
        {
            var layer = (Layer)(response.ButtonId - ButtonEquipLayerBase);
            EquipmentClicked?.Invoke(playerEntity, layer);
            return;
        }
        
        switch (response.ButtonId)
        {
            case ButtonHelp:
                ButtonClicked?.Invoke(playerEntity, PaperdollAction.Help);
                break;
                
            case ButtonOptions:
                ButtonClicked?.Invoke(playerEntity, PaperdollAction.Options);
                break;
                
            case ButtonLogout:
                ButtonClicked?.Invoke(playerEntity, PaperdollAction.Logout);
                break;
                
            case ButtonQuests:
                ButtonClicked?.Invoke(playerEntity, PaperdollAction.Quests);
                break;
                
            case ButtonSkills:
                ButtonClicked?.Invoke(playerEntity, PaperdollAction.Skills);
                break;
                
            case ButtonGuild:
                ButtonClicked?.Invoke(playerEntity, PaperdollAction.Guild);
                break;
                
            case ButtonPeaceWar:
                ButtonClicked?.Invoke(playerEntity, PaperdollAction.TogglePeaceWar);
                break;
                
            case ButtonStatus:
                ButtonClicked?.Invoke(playerEntity, PaperdollAction.Status);
                break;
                
            case ButtonProfile:
                ButtonClicked?.Invoke(playerEntity, PaperdollAction.Profile);
                break;
                
            case ButtonVirtue:
                ButtonClicked?.Invoke(playerEntity, PaperdollAction.Virtue);
                break;
                
            case ButtonCombatBook:
                ButtonClicked?.Invoke(playerEntity, PaperdollAction.CombatBook);
                break;
                
            case ButtonRacialBook:
                ButtonClicked?.Invoke(playerEntity, PaperdollAction.RacialBook);
                break;
        }
    }
    
    /// <summary>
    /// Event fired when a paperdoll button is clicked
    /// </summary>
    public static event Action<PlayerEntity, PaperdollAction>? ButtonClicked;
    
    /// <summary>
    /// Event fired when an equipment slot is clicked (for unequip)
    /// </summary>
    public static event Action<PlayerEntity, Layer>? EquipmentClicked;
}

/// <summary>
/// Actions from paperdoll button clicks
/// </summary>
public enum PaperdollAction
{
    None,
    Help,
    Options,
    Logout,
    Quests,
    Skills,
    Guild,
    TogglePeaceWar,
    Status,
    Profile,
    Virtue,
    CombatBook,
    RacialBook
}
