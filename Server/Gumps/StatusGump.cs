using RealmOfReality.Shared.Entities;
using RealmOfReality.Shared.Gumps;

namespace RealmOfReality.Server.Gumps;

/// <summary>
/// Common gump type IDs for the game
/// </summary>
public static class GumpTypeIds
{
    public const uint Status = 0x5001;
    public const uint Paperdoll = 0x5002;
    public const uint Backpack = 0x5003;
    public const uint Container = 0x5004;
    public const uint Skills = 0x5005;
    public const uint Spellbook = 0x5006;
    public const uint Help = 0x5007;
    public const uint Options = 0x5008;
    public const uint Quests = 0x5009;
    public const uint Guild = 0x500A;
}

/// <summary>
/// Status gump showing player stats - uses authentic UO artwork
/// Based on ClassicUO's StatusGumpOld (gump 0x0802)
/// </summary>
public class StatusGump : Gump
{
    // Gump artwork IDs from UO
    private const int BackgroundGump = 0x0802; // Classic status bar background
    private const int BuffButtonNormal = 0x7538;
    private const int BuffButtonPressed = 0x7539;
    
    // Stat lock graphics
    private const int LockUp = 0x0984;
    private const int LockDown = 0x0986;
    private const int LockLocked = 0x082C;
    
    private readonly Mobile _mobile;
    
    public override uint TypeId => GumpTypeIds.Status;
    
    public StatusGump(Mobile mobile) : base(250, 100)
    {
        _mobile = mobile;
        
        Closable = true;
        Dragable = true;
        
        Build();
    }
    
    private void Build()
    {
        // Classic status background (gump 0x0802)
        AddImage(0, 0, BackgroundGump);
        
        // Character name
        AddLabel(86, 42, 0x0386, _mobile.Name ?? "Unknown");
        
        // Buff button (for showing buff icons)
        AddButton(20, 42, BuffButtonNormal, BuffButtonPressed, (int)StatusButton.Buffs);
        
        // Stat values with their lock icons
        // STR
        AddImage(40, 62, GetLockGraphic(_mobile.StrLock));
        AddLabel(86, 62, 0x0386, _mobile.Strength.ToString());
        
        // DEX
        AddImage(40, 74, GetLockGraphic(_mobile.DexLock));
        AddLabel(86, 74, 0x0386, _mobile.Dexterity.ToString());
        
        // INT
        AddImage(40, 86, GetLockGraphic(_mobile.IntLock));
        AddLabel(86, 86, 0x0386, _mobile.Intelligence.ToString());
        
        // Sex
        AddLabel(86, 98, 0x0386, _mobile.IsFemale ? "Female" : "Male");
        
        // Armor (Physical Resistance)
        AddLabel(86, 110, 0x0386, _mobile.PhysicalResistance.ToString());
        
        // Right column
        // HP
        AddLabel(171, 62, 0x0386, $"{_mobile.Health}/{_mobile.MaxHealth}");
        
        // Mana
        AddLabel(171, 74, 0x0386, $"{_mobile.Mana}/{_mobile.MaxMana}");
        
        // Stamina
        AddLabel(171, 86, 0x0386, $"{_mobile.Stamina}/{_mobile.MaxStamina}");
        
        // Gold (only for players)
        var gold = (_mobile as PlayerEntity)?.Gold ?? 0;
        AddLabel(171, 98, 0x0386, gold.ToString());
        
        // Weight
        AddLabel(171, 110, 0x0386, $"{_mobile.Weight}/{_mobile.WeightMax}");
    }
    
    private static int GetLockGraphic(StatLock lockStatus)
    {
        return lockStatus switch
        {
            StatLock.Up => LockUp,
            StatLock.Down => LockDown,
            StatLock.Locked => LockLocked,
            _ => LockUp
        };
    }
    
    public override void OnResponse(object player, GumpResponse response)
    {
        var button = (StatusButton)response.ButtonId;
        
        switch (button)
        {
            case StatusButton.Buffs:
                // TODO: Open buff window
                break;
                
            case StatusButton.ToggleStrLock:
                _mobile.StrLock = (StatLock)(((int)_mobile.StrLock + 1) % 3);
                break;
                
            case StatusButton.ToggleDexLock:
                _mobile.DexLock = (StatLock)(((int)_mobile.DexLock + 1) % 3);
                break;
                
            case StatusButton.ToggleIntLock:
                _mobile.IntLock = (StatLock)(((int)_mobile.IntLock + 1) % 3);
                break;
        }
    }
    
    private enum StatusButton
    {
        None = 0,
        Buffs = 1,
        ToggleStrLock = 2,
        ToggleDexLock = 3,
        ToggleIntLock = 4
    }
}

/// <summary>
/// Extended status gump with more stats (for newer clients)
/// Uses gump 0x2A6C
/// </summary>
public class StatusGumpModern : Gump
{
    private const int BackgroundGump = 0x2A6C; // Modern status background
    
    private readonly Mobile _mobile;
    
    public override uint TypeId => GumpTypeIds.Status;
    
    public StatusGumpModern(Mobile mobile) : base(200, 100)
    {
        _mobile = mobile;
        
        Closable = true;
        Dragable = true;
        
        Build();
    }
    
    private void Build()
    {
        // Modern status background
        AddImage(0, 0, BackgroundGump);
        
        // Character name (centered at top)
        AddLabel(90, 50, 0x0386, _mobile.Name ?? "Unknown");
        
        // Left column - Primary stats
        AddLabel(80, 77, 0x0386, _mobile.Strength.ToString());
        AddLabel(80, 105, 0x0386, _mobile.Dexterity.ToString());
        AddLabel(80, 133, 0x0386, _mobile.Intelligence.ToString());
        
        // Middle column - Vitals (current/max)
        AddLabel(145, 70, 0x0386, _mobile.Health.ToString());
        AddLabel(145, 83, 0x0386, _mobile.MaxHealth.ToString());
        
        AddLabel(145, 98, 0x0386, _mobile.Stamina.ToString());
        AddLabel(145, 111, 0x0386, _mobile.MaxStamina.ToString());
        
        AddLabel(145, 126, 0x0386, _mobile.Mana.ToString());
        AddLabel(145, 139, 0x0386, _mobile.MaxMana.ToString());
        
        // Right column - Secondary stats
        AddLabel(220, 77, 0x0386, _mobile.StatsCap.ToString());  // Stat cap
        AddLabel(220, 105, 0x0386, _mobile.Luck.ToString());     // Luck
        
        // Weight
        AddLabel(210, 126, 0x0386, _mobile.Weight.ToString());
        AddLabel(210, 139, 0x0386, _mobile.WeightMax.ToString());
        
        // Far right - Damage, Followers, Gold
        AddLabel(280, 77, 0x0386, $"{_mobile.DamageMin}-{_mobile.DamageMax}");
        var gold = (_mobile as PlayerEntity)?.Gold ?? 0;
        AddLabel(280, 105, 0x0386, gold.ToString());
        AddLabel(280, 133, 0x0386, $"{_mobile.Followers}/{_mobile.FollowersMax}");
        
        // Resistances (far right bottom)
        AddLabel(354, 76, 0x0386, _mobile.PhysicalResistance.ToString());
        AddLabel(354, 92, 0x0386, _mobile.FireResistance.ToString());
        AddLabel(354, 106, 0x0386, _mobile.ColdResistance.ToString());
        AddLabel(354, 120, 0x0386, _mobile.PoisonResistance.ToString());
        AddLabel(354, 134, 0x0386, _mobile.EnergyResistance.ToString());
    }
    
    public override void OnResponse(object player, GumpResponse response)
    {
        // Handle button clicks
    }
}

/// <summary>
/// Help gump with basic game information
/// </summary>
public class HelpGump : Gump
{
    public override uint TypeId => GumpTypeIds.Help;
    
    public HelpGump() : base(100, 100)
    {
        Closable = true;
        Dragable = true;
        
        Build();
    }
    
    private void Build()
    {
        // Background
        AddBackground(0, 0, 400, 300, GumpGraphics.BackgroundParchment);
        
        // Title
        AddLabel(160, 15, 0x0035, "Help");
        
        // Help content
        AddHtml(20, 50, 360, 230, @"
<b>Welcome to Realm of Reality!</b><br><br>

<b>Controls:</b><br>
- Left-click: Move / Interact<br>
- Right-click: Context Menu<br>
- Double-click: Use item / Talk to NPC<br>
- Arrow keys: Move character<br><br>

<b>Commands:</b><br>
/help - Show this help<br>
/use [slot] - Use item from backpack<br>
/say [message] - Say something<br>
/who - Show online players<br><br>

<b>Paperdoll Buttons:</b><br>
- Status: Open character status<br>
- Skills: View your skills<br>
- Peace/War: Toggle combat mode<br>
", true, true);
        
        // Close button
        AddButton(175, 265, GumpGraphics.ButtonOkNormal, GumpGraphics.ButtonOkPressed, 1);
    }
    
    public override void OnResponse(object player, GumpResponse response)
    {
        // Just close on any response
    }
}
