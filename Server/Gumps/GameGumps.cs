using RealmOfReality.Shared.Entities;
using RealmOfReality.Shared.Gumps;

namespace RealmOfReality.Server.Gumps;

/// <summary>
/// Skills gump showing all player skills
/// Uses authentic UO skill window artwork (gump 0x0823)
/// </summary>
public class SkillsGump : Gump
{
    private const int BackgroundGump = 0x0823; // Skills window background
    private const int ScrollbarBg = 0x0001;
    private const int ScrollbarThumb = 0x0002;
    
    // Skill lock button graphics
    private const int LockUp = 0x0983;     // Arrow up
    private const int LockDown = 0x0985;   // Arrow down  
    private const int LockLocked = 0x082C; // Lock icon
    
    public override uint TypeId => GumpTypeIds.Skills;
    
    private readonly PlayerEntity _player;
    
    public SkillsGump(PlayerEntity player) : base(100, 100)
    {
        _player = player;
        Closable = true;
        Dragable = true;
        Build();
    }
    
    private void Build()
    {
        // Background
        AddBackground(0, 0, 320, 400, GumpGraphics.BackgroundParchment);
        
        // Title
        AddLabel(130, 10, 0x0035, "Skills");
        
        // Column headers
        AddLabel(20, 35, 0x0386, "Skill");
        AddLabel(180, 35, 0x0386, "Value");
        AddLabel(240, 35, 0x0386, "Cap");
        AddLabel(285, 35, 0x0386, "Lock");
        
        // Divider line (using alpha region)
        AddAlphaRegion(15, 55, 290, 1);
        
        // Skills list
        int y = 60;
        int skillIndex = 0;
        
        foreach (var skill in GetSkillList())
        {
            if (y > 360) break; // Pagination would go here
            
            // Skill name
            AddLabel(20, y, 0x0386, skill.Name);
            
            // Skill value (e.g., "100.0")
            var value = GetSkillValue(skill.Id);
            AddLabel(180, y, 0x0386, value.ToString("F1"));
            
            // Skill cap
            var cap = GetSkillCap(skill.Id);
            AddLabel(240, y, 0x0386, cap.ToString("F1"));
            
            // Lock button
            var lockState = GetSkillLock(skill.Id);
            var lockGump = lockState switch
            {
                SkillLock.Up => LockUp,
                SkillLock.Down => LockDown,
                _ => LockLocked
            };
            AddButton(285, y, lockGump, lockGump, 100 + skillIndex);
            
            y += 18;
            skillIndex++;
        }
        
        // Total skill points
        var totalSkills = CalculateTotalSkills();
        var skillsCap = _player.StatsCap; // Would be separate skills cap in full implementation
        AddLabel(20, 370, 0x0386, $"Total: {totalSkills:F1} / 700.0");
        
        // Close button
        AddButton(140, 375, GumpGraphics.ButtonOkNormal, GumpGraphics.ButtonOkPressed, 0);
    }
    
    private double GetSkillValue(int skillId)
    {
        // In full implementation, would read from player.Skills[skillId]
        // For now, return placeholder values
        return skillId switch
        {
            0 => 50.0,  // Alchemy
            1 => 35.5,  // Anatomy
            2 => 0.0,   // Animal Lore
            3 => 100.0, // Archery (maxed for testing)
            4 => 25.0,  // Arms Lore
            _ => 0.0
        };
    }
    
    private double GetSkillCap(int skillId)
    {
        return 100.0; // Standard cap
    }
    
    private SkillLock GetSkillLock(int skillId)
    {
        return SkillLock.Up; // Default to "can increase"
    }
    
    private double CalculateTotalSkills()
    {
        // Sum all skills
        double total = 0;
        foreach (var skill in GetSkillList())
        {
            total += GetSkillValue(skill.Id);
        }
        return total;
    }
    
    private static IEnumerable<SkillInfo> GetSkillList()
    {
        // All 58 UO skills
        yield return new SkillInfo(0, "Alchemy");
        yield return new SkillInfo(1, "Anatomy");
        yield return new SkillInfo(2, "Animal Lore");
        yield return new SkillInfo(3, "Archery");
        yield return new SkillInfo(4, "Arms Lore");
        yield return new SkillInfo(5, "Parrying");
        yield return new SkillInfo(6, "Begging");
        yield return new SkillInfo(7, "Blacksmithing");
        yield return new SkillInfo(8, "Bowcraft");
        yield return new SkillInfo(9, "Peacemaking");
        yield return new SkillInfo(10, "Camping");
        yield return new SkillInfo(11, "Carpentry");
        yield return new SkillInfo(12, "Cartography");
        yield return new SkillInfo(13, "Cooking");
        yield return new SkillInfo(14, "Detecting Hidden");
        yield return new SkillInfo(15, "Discordance");
        yield return new SkillInfo(16, "Eval Intelligence");
        yield return new SkillInfo(17, "Healing");
        yield return new SkillInfo(18, "Fishing");
        yield return new SkillInfo(19, "Forensic Eval");
        yield return new SkillInfo(20, "Herding");
        yield return new SkillInfo(21, "Hiding");
        yield return new SkillInfo(22, "Provocation");
        yield return new SkillInfo(23, "Inscription");
        yield return new SkillInfo(24, "Lockpicking");
        yield return new SkillInfo(25, "Magery");
        yield return new SkillInfo(26, "Magic Resist");
        yield return new SkillInfo(27, "Tactics");
        yield return new SkillInfo(28, "Snooping");
        yield return new SkillInfo(29, "Musicianship");
        yield return new SkillInfo(30, "Poisoning");
        yield return new SkillInfo(31, "Fencing");
        yield return new SkillInfo(32, "Wrestling");
        yield return new SkillInfo(33, "Lumberjacking");
        yield return new SkillInfo(34, "Mining");
        yield return new SkillInfo(35, "Meditation");
        yield return new SkillInfo(36, "Stealth");
        yield return new SkillInfo(37, "Remove Trap");
        yield return new SkillInfo(38, "Necromancy");
        yield return new SkillInfo(39, "Focus");
        yield return new SkillInfo(40, "Chivalry");
        yield return new SkillInfo(41, "Bushido");
        yield return new SkillInfo(42, "Ninjitsu");
        yield return new SkillInfo(43, "Spellweaving");
        yield return new SkillInfo(44, "Mysticism");
        yield return new SkillInfo(45, "Imbuing");
        yield return new SkillInfo(46, "Throwing");
        yield return new SkillInfo(47, "Swordsmanship");
        yield return new SkillInfo(48, "Mace Fighting");
        yield return new SkillInfo(49, "Tailoring");
        yield return new SkillInfo(50, "Tinkering");
        yield return new SkillInfo(51, "Animal Taming");
        yield return new SkillInfo(52, "Taste ID");
        yield return new SkillInfo(53, "Tracking");
        yield return new SkillInfo(54, "Veterinary");
        yield return new SkillInfo(55, "Spirit Speak");
        yield return new SkillInfo(56, "Stealing");
        yield return new SkillInfo(57, "Item ID");
    }
    
    public override void OnResponse(object player, GumpResponse response)
    {
        if (response.ButtonId >= 100)
        {
            // Toggle skill lock
            int skillIndex = response.ButtonId - 100;
            // Would toggle between Up/Down/Locked
        }
    }
}

/// <summary>
/// Skill information
/// </summary>
public record SkillInfo(int Id, string Name);

/// <summary>
/// Skill lock state
/// </summary>
public enum SkillLock : byte
{
    Up = 0,
    Down = 1,
    Locked = 2
}

/// <summary>
/// Options/Settings gump
/// </summary>
public class OptionsGump : Gump
{
    public override uint TypeId => GumpTypeIds.Options;
    
    public OptionsGump() : base(100, 100)
    {
        Closable = true;
        Dragable = true;
        Build();
    }
    
    private void Build()
    {
        AddBackground(0, 0, 400, 350, GumpGraphics.BackgroundParchment);
        
        AddLabel(170, 15, 0x0035, "Options");
        
        // Sound options
        AddLabel(20, 50, 0x0386, "Sound Options");
        AddCheckbox(20, 75, GumpGraphics.CheckboxUnchecked, GumpGraphics.CheckboxChecked, 1, true);
        AddLabel(45, 75, 0x0386, "Enable Sound");
        AddCheckbox(20, 100, GumpGraphics.CheckboxUnchecked, GumpGraphics.CheckboxChecked, 2, true);
        AddLabel(45, 100, 0x0386, "Enable Music");
        
        // Display options  
        AddLabel(20, 140, 0x0386, "Display Options");
        AddCheckbox(20, 165, GumpGraphics.CheckboxUnchecked, GumpGraphics.CheckboxChecked, 3, true);
        AddLabel(45, 165, 0x0386, "Show Names");
        AddCheckbox(20, 190, GumpGraphics.CheckboxUnchecked, GumpGraphics.CheckboxChecked, 4, false);
        AddLabel(45, 190, 0x0386, "Show Health Bars");
        AddCheckbox(20, 215, GumpGraphics.CheckboxUnchecked, GumpGraphics.CheckboxChecked, 5, true);
        AddLabel(45, 215, 0x0386, "Show Speech");
        
        // Interface options
        AddLabel(220, 50, 0x0386, "Interface");
        AddCheckbox(220, 75, GumpGraphics.CheckboxUnchecked, GumpGraphics.CheckboxChecked, 6, false);
        AddLabel(245, 75, 0x0386, "Always Run");
        AddCheckbox(220, 100, GumpGraphics.CheckboxUnchecked, GumpGraphics.CheckboxChecked, 7, true);
        AddLabel(245, 100, 0x0386, "Pathfinding");
        AddCheckbox(220, 125, GumpGraphics.CheckboxUnchecked, GumpGraphics.CheckboxChecked, 8, true);
        AddLabel(245, 125, 0x0386, "Circle of Transparency");
        
        // Macro hint
        AddLabel(20, 260, 0x0386, "Macros can be configured in the");
        AddLabel(20, 280, 0x0386, "game client settings file.");
        
        // Buttons
        AddButton(120, 310, GumpGraphics.ButtonOkNormal, GumpGraphics.ButtonOkPressed, 1);
        AddLabel(165, 312, 0x0386, "Apply");
        
        AddButton(220, 310, GumpGraphics.ButtonCancelNormal, GumpGraphics.ButtonCancelPressed, 0);
        AddLabel(265, 312, 0x0386, "Cancel");
    }
    
    public override void OnResponse(object player, GumpResponse response)
    {
        if (response.ButtonId == 1)
        {
            // Apply settings
            // Would save checkbox states
        }
    }
}

/// <summary>
/// Quest log gump
/// </summary>
public class QuestsGump : Gump
{
    public override uint TypeId => GumpTypeIds.Quests;
    
    private readonly PlayerEntity _player;
    
    public QuestsGump(PlayerEntity player) : base(100, 100)
    {
        _player = player;
        Closable = true;
        Dragable = true;
        Build();
    }
    
    private void Build()
    {
        AddBackground(0, 0, 350, 300, GumpGraphics.BackgroundParchment);
        
        AddLabel(140, 15, 0x0035, "Quest Log");
        
        // Quest list area
        AddHtml(20, 50, 310, 200, @"
<b>Active Quests:</b><br><br>
No active quests.<br><br>
<i>Speak to NPCs to receive quests.</i><br><br>
<b>Completed Quests:</b><br>
- Tutorial (1/1)<br>
", true, true);
        
        // Close button
        AddButton(150, 260, GumpGraphics.ButtonOkNormal, GumpGraphics.ButtonOkPressed, 0);
    }
    
    public override void OnResponse(object player, GumpResponse response)
    {
        // Quest interactions
    }
}

/// <summary>
/// Guild gump
/// </summary>
public class GuildGump : Gump
{
    public override uint TypeId => GumpTypeIds.Guild;
    
    private readonly PlayerEntity _player;
    
    public GuildGump(PlayerEntity player) : base(100, 100)
    {
        _player = player;
        Closable = true;
        Dragable = true;
        Build();
    }
    
    private void Build()
    {
        AddBackground(0, 0, 400, 350, GumpGraphics.BackgroundParchment);
        
        AddLabel(175, 15, 0x0035, "Guild");
        
        AddHtml(20, 50, 360, 250, @"
<b>Guild Membership</b><br><br>
You are not a member of any guild.<br><br>
To join a guild, you must be invited by a guild leader.<br><br>
To create a guild, speak to an NPC guildmaster and pay the registration fee of 25,000 gold.<br><br>
<b>Guild Commands:</b><br>
/guild - Show this menu<br>
/gc [message] - Guild chat<br>
/accept - Accept guild invitation<br>
/resign - Leave your guild<br>
", true, true);
        
        // Close button
        AddButton(175, 310, GumpGraphics.ButtonOkNormal, GumpGraphics.ButtonOkPressed, 0);
    }
    
    public override void OnResponse(object player, GumpResponse response)
    {
        // Guild interactions
    }
}

/// <summary>
/// Logout confirmation gump
/// </summary>
public class LogoutGump : Gump
{
    public override uint TypeId => 0x5100; // Logout confirmation
    
    public LogoutGump() : base(200, 200)
    {
        Closable = true;
        Dragable = true;
        Build();
    }
    
    private void Build()
    {
        AddBackground(0, 0, 250, 120, GumpGraphics.BackgroundParchment);
        
        AddLabel(90, 15, 0x0035, "Log Out");
        
        AddLabel(30, 45, 0x0386, "Are you sure you want to");
        AddLabel(30, 65, 0x0386, "log out?");
        
        // Yes button
        AddButton(40, 85, GumpGraphics.ButtonOkNormal, GumpGraphics.ButtonOkPressed, 1);
        AddLabel(80, 87, 0x0386, "Yes");
        
        // No button
        AddButton(140, 85, GumpGraphics.ButtonCancelNormal, GumpGraphics.ButtonCancelPressed, 0);
        AddLabel(180, 87, 0x0386, "No");
    }
    
    public override void OnResponse(object player, GumpResponse response)
    {
        if (response.ButtonId == 1)
        {
            // Trigger logout
            LogoutConfirmed?.Invoke(player as PlayerEntity);
        }
    }
    
    public static event Action<PlayerEntity?>? LogoutConfirmed;
}
