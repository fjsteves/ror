using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RealmOfReality.Client.Engine;
using RealmOfReality.Client.Game;
using RealmOfReality.Shared.Network;

namespace RealmOfReality.Client.UI;

/// <summary>
/// Full character creation screen with stat allocation and skill selection
/// </summary>
public class CharacterCreationScreen : IScreen
{
    private readonly GameState _gameState;
    private readonly UIRenderer _ui;
    private readonly InputManager _input;
    private readonly Action? _onCancel;
    private readonly Action? _onCreate;
    
    // Character data
    private string _name = "";
    private byte _gender = 0; // 0 = male, 1 = female
    private ushort _bodyType = 1;
    private ushort _skinHue = 1002;
    private ushort _hairStyle = 1;
    private ushort _hairHue = 1102;
    
    // Stats (must total 80)
    private int _strength = 27;
    private int _dexterity = 27;
    private int _intelligence = 26;
    private const int StatTotal = 80;
    private const int MinStat = 10;
    private const int MaxStat = 60;
    
    // Starting skills (3 skills, each starts at 500 = 50.0)
    private int _skill1 = 0; // Swordsmanship
    private int _skill2 = 7; // Tactics
    private int _skill3 = 9; // Healing
    private int _skill1Value = 500;
    private int _skill2Value = 300;
    private int _skill3Value = 200;
    private const int SkillTotal = 1000; // 100.0 total
    
    // Available skills for selection
    private static readonly (int Id, string Name, string Category)[] AvailableSkills = new[]
    {
        // Combat
        (0, "Swordsmanship", "Combat"),
        (1, "Mace Fighting", "Combat"),
        (2, "Fencing", "Combat"),
        (3, "Archery", "Combat"),
        (4, "Wrestling", "Combat"),
        (5, "Parrying", "Combat"),
        (6, "Tactics", "Combat"),
        (7, "Anatomy", "Combat"),
        (8, "Healing", "Combat"),
        
        // Magic
        (10, "Magery", "Magic"),
        (11, "Eval Intelligence", "Magic"),
        (12, "Meditation", "Magic"),
        (13, "Magic Resist", "Magic"),
        
        // Crafting
        (20, "Blacksmithy", "Crafting"),
        (21, "Tailoring", "Crafting"),
        (22, "Tinkering", "Crafting"),
        (23, "Carpentry", "Crafting"),
        (24, "Alchemy", "Crafting"),
        
        // Gathering
        (30, "Mining", "Gathering"),
        (31, "Lumberjacking", "Gathering"),
        (32, "Fishing", "Gathering"),
        
        // Misc
        (40, "Stealth", "Misc"),
        (41, "Hiding", "Misc"),
        (44, "Lockpicking", "Misc"),
        (47, "Animal Taming", "Misc"),
        (49, "Musicianship", "Misc"),
    };
    
    // Skin hue presets
    private static readonly ushort[] SkinHues = { 1002, 1003, 1004, 1005, 1006, 1007, 1008, 1009, 1010 };
    private int _skinHueIndex = 0;
    
    // Hair style presets
    private static readonly (ushort Id, string Name)[] HairStyles = new[]
    {
        ((ushort)0, "Bald"),
        ((ushort)1, "Short"),
        ((ushort)2, "Long"),
        ((ushort)3, "Ponytail"),
        ((ushort)4, "Mohawk"),
        ((ushort)5, "Topknot"),
        ((ushort)6, "Curly"),
        ((ushort)7, "Receding"),
    };
    private int _hairStyleIndex = 1;
    
    // Hair color presets
    private static readonly ushort[] HairHues = { 1102, 1103, 1104, 1105, 1106, 1107, 1108, 1109, 1110 };
    private int _hairHueIndex = 0;
    
    // UI State
    private int _focusedField = 0; // 0 = name, 1-3 = stats, 4-6 = skills
    private int _currentPage = 0; // 0 = appearance, 1 = stats, 2 = skills
    private int _skillDropdown = -1; // Which skill dropdown is open (-1 = none)
    private int _dropdownScroll = 0;
    private string _statusMessage = "";
    private Color _statusColor = Color.Gray;
    private bool _isCreating = false;
    
    // Layout
    private Rectangle _mainPanel;
    private Rectangle _previewArea;
    private Rectangle _nameInputRect;
    
    // Assets for preview
    private readonly AssetManager? _assets;
    
    public CharacterCreationScreen(GameState gameState, UIRenderer ui, InputManager input, 
        Action onCancel, Action onCreate, AssetManager? assets = null)
    {
        _gameState = gameState;
        _ui = ui;
        _input = input;
        _onCancel = onCancel;
        _onCreate = onCreate;
        _assets = assets;
        
        _gameState.StateChanged += OnStateChanged;
    }
    
    private bool _hasHandledSuccess = false;
    
    private void OnStateChanged()
    {
        // Only handle state changes while we're still the active screen and haven't already handled success
        if (_hasHandledSuccess) return;
        if (_gameState.Phase == ClientPhase.InWorld) return; // Already transitioned to game
        
        if (!string.IsNullOrEmpty(_gameState.AccountMessage))
        {
            _statusMessage = _gameState.AccountMessage;
            _statusColor = _statusMessage.Contains("Welcome") ? Color.Green : Color.Red;
            _isCreating = false;
            
            if (_statusMessage.Contains("Welcome"))
            {
                _hasHandledSuccess = true;
                _onCreate?.Invoke();
            }
        }
    }
    
    public void Enter()
    {
        _currentPage = 0;
        _focusedField = 0;
        _skillDropdown = -1;
        _statusMessage = "Create your character";
        _statusColor = Color.Gray;
        _isCreating = false;
        _hasHandledSuccess = false;
        _name = "";
        
        UpdateLayout();
    }
    
    public void Exit()
    {
        _skillDropdown = -1;
    }
    
    private void UpdateLayout()
    {
        // Main panel - larger to fit all content
        _mainPanel = new Rectangle(140, 60, 1000, 600);
        _previewArea = new Rectangle(_mainPanel.X + 20, _mainPanel.Y + 60, 200, 300);
        _nameInputRect = new Rectangle(_mainPanel.X + 20, _mainPanel.Y + 380, 200, 36);
    }
    
    public void Update(GameTime gameTime)
    {
        // Close dropdown on click outside
        if (_input.IsLeftMousePressed && _skillDropdown >= 0)
        {
            var dropdownRect = GetSkillDropdownRect(_skillDropdown);
            if (!_ui.IsInside(dropdownRect, _input.MousePosition))
            {
                _skillDropdown = -1;
            }
        }
        
        HandleInput();
        HandleMouseInput();
    }
    
    private void HandleInput()
    {
        // Tab to switch pages
        if (_input.IsKeyPressed(Keys.Tab) && !_input.IsShiftDown)
        {
            _currentPage = (_currentPage + 1) % 3;
            _skillDropdown = -1;
        }
        else if (_input.IsKeyPressed(Keys.Tab) && _input.IsShiftDown)
        {
            _currentPage = (_currentPage - 1 + 3) % 3;
            _skillDropdown = -1;
        }
        
        // Escape to cancel
        if (_input.IsKeyPressed(Keys.Escape))
        {
            if (_skillDropdown >= 0)
                _skillDropdown = -1;
            else
                _onCancel?.Invoke();
        }
        
        // Enter to create (if on last page)
        if (_input.IsKeyPressed(Keys.Enter) && _currentPage == 2 && !_isCreating)
        {
            CreateCharacter();
        }
        
        // Name input
        if (_focusedField == 0)
        {
            HandleNameInput();
        }
    }
    
    private void HandleNameInput()
    {
        var keys = _input.GetPressedKeys();
        foreach (var key in keys)
        {
            if (!_input.IsKeyPressed(key)) continue;
            
            if (key == Keys.Back && _name.Length > 0)
            {
                _name = _name[..^1];
            }
            else if (key >= Keys.A && key <= Keys.Z && _name.Length < 20)
            {
                char c = (char)('a' + (key - Keys.A));
                if (_input.IsShiftDown || _name.Length == 0)
                    c = char.ToUpper(c);
                _name += c;
            }
            else if (key == Keys.Space && _name.Length > 0 && _name.Length < 20 && !_name.EndsWith(' '))
            {
                _name += ' ';
            }
        }
    }
    
    private void HandleMouseInput()
    {
        var mousePos = _input.MousePosition;
        
        if (_input.IsLeftMousePressed)
        {
            // Name field
            if (_ui.IsInside(_nameInputRect, mousePos))
            {
                _focusedField = 0;
            }
            
            // Page tabs
            var tabY = _mainPanel.Y + 20;
            for (int i = 0; i < 3; i++)
            {
                var tabRect = new Rectangle(_mainPanel.X + 240 + i * 120, tabY, 110, 30);
                if (_ui.IsInside(tabRect, mousePos))
                {
                    _currentPage = i;
                    _skillDropdown = -1;
                }
            }
            
            // Page-specific interactions
            switch (_currentPage)
            {
                case 0: HandleAppearanceClick(mousePos); break;
                case 1: HandleStatsClick(mousePos); break;
                case 2: HandleSkillsClick(mousePos); break;
            }
            
            // Bottom buttons
            var createBtn = new Rectangle(_mainPanel.Right - 170, _mainPanel.Bottom - 50, 150, 36);
            var cancelBtn = new Rectangle(_mainPanel.X + 20, _mainPanel.Bottom - 50, 100, 36);
            
            if (_ui.IsInside(createBtn, mousePos) && !_isCreating)
            {
                CreateCharacter();
            }
            else if (_ui.IsInside(cancelBtn, mousePos))
            {
                _onCancel?.Invoke();
            }
        }
        
        // Mouse wheel for dropdowns
        if (_skillDropdown >= 0 && _input.ScrollWheelDelta != 0)
        {
            _dropdownScroll -= _input.ScrollWheelDelta / 120;
            _dropdownScroll = Math.Max(0, Math.Min(_dropdownScroll, AvailableSkills.Length - 5));
        }
    }
    
    private void HandleAppearanceClick(Vector2 mousePos)
    {
        var x = _mainPanel.X + 250;
        var y = _mainPanel.Y + 80;
        var btnWidth = 30;
        var spacing = 50;
        
        // Gender
        var maleBtn = new Rectangle(x, y, 80, 30);
        var femaleBtn = new Rectangle(x + 90, y, 80, 30);
        if (_ui.IsInside(maleBtn, mousePos)) _gender = 0;
        if (_ui.IsInside(femaleBtn, mousePos)) _gender = 1;
        
        // Skin tone
        y += spacing;
        var leftBtn = new Rectangle(x, y, btnWidth, 30);
        var rightBtn = new Rectangle(x + 200, y, btnWidth, 30);
        if (_ui.IsInside(leftBtn, mousePos))
        {
            _skinHueIndex = (_skinHueIndex - 1 + SkinHues.Length) % SkinHues.Length;
            _skinHue = SkinHues[_skinHueIndex];
        }
        if (_ui.IsInside(rightBtn, mousePos))
        {
            _skinHueIndex = (_skinHueIndex + 1) % SkinHues.Length;
            _skinHue = SkinHues[_skinHueIndex];
        }
        
        // Hair style
        y += spacing;
        leftBtn = new Rectangle(x, y, btnWidth, 30);
        rightBtn = new Rectangle(x + 200, y, btnWidth, 30);
        if (_ui.IsInside(leftBtn, mousePos))
        {
            _hairStyleIndex = (_hairStyleIndex - 1 + HairStyles.Length) % HairStyles.Length;
            _hairStyle = HairStyles[_hairStyleIndex].Id;
        }
        if (_ui.IsInside(rightBtn, mousePos))
        {
            _hairStyleIndex = (_hairStyleIndex + 1) % HairStyles.Length;
            _hairStyle = HairStyles[_hairStyleIndex].Id;
        }
        
        // Hair color
        y += spacing;
        leftBtn = new Rectangle(x, y, btnWidth, 30);
        rightBtn = new Rectangle(x + 200, y, btnWidth, 30);
        if (_ui.IsInside(leftBtn, mousePos))
        {
            _hairHueIndex = (_hairHueIndex - 1 + HairHues.Length) % HairHues.Length;
            _hairHue = HairHues[_hairHueIndex];
        }
        if (_ui.IsInside(rightBtn, mousePos))
        {
            _hairHueIndex = (_hairHueIndex + 1) % HairHues.Length;
            _hairHue = HairHues[_hairHueIndex];
        }
    }
    
    private void HandleStatsClick(Vector2 mousePos)
    {
        var x = _mainPanel.X + 250;
        var y = _mainPanel.Y + 100;
        var spacing = 60;
        var btnWidth = 30;
        var btnHeight = 25;
        
        // Strength (buttons are at y + 25 in DrawStatRow)
        var strDown = new Rectangle(x, y + 25, btnWidth, btnHeight);
        var strUp = new Rectangle(x + 200, y + 25, btnWidth, btnHeight);
        if (_ui.IsInside(strDown, mousePos) && _strength > MinStat)
        {
            _strength--;
        }
        if (_ui.IsInside(strUp, mousePos) && _strength < MaxStat && GetStatTotal() < StatTotal)
        {
            _strength++;
        }
        
        // Dexterity
        y += spacing;
        var dexDown = new Rectangle(x, y + 25, btnWidth, btnHeight);
        var dexUp = new Rectangle(x + 200, y + 25, btnWidth, btnHeight);
        if (_ui.IsInside(dexDown, mousePos) && _dexterity > MinStat)
        {
            _dexterity--;
        }
        if (_ui.IsInside(dexUp, mousePos) && _dexterity < MaxStat && GetStatTotal() < StatTotal)
        {
            _dexterity++;
        }
        
        // Intelligence
        y += spacing;
        var intDown = new Rectangle(x, y + 25, btnWidth, btnHeight);
        var intUp = new Rectangle(x + 200, y + 25, btnWidth, btnHeight);
        if (_ui.IsInside(intDown, mousePos) && _intelligence > MinStat)
        {
            _intelligence--;
        }
        if (_ui.IsInside(intUp, mousePos) && _intelligence < MaxStat && GetStatTotal() < StatTotal)
        {
            _intelligence++;
        }
    }
    
    private void HandleSkillsClick(Vector2 mousePos)
    {
        var x = _mainPanel.X + 250;
        var y = _mainPanel.Y + 100;
        var spacing = 80;
        
        // Check skill dropdowns
        for (int i = 0; i < 3; i++)
        {
            var dropdownBtn = new Rectangle(x, y + i * spacing, 200, 30);
            if (_ui.IsInside(dropdownBtn, mousePos))
            {
                _skillDropdown = _skillDropdown == i ? -1 : i;
                _dropdownScroll = 0;
                return;
            }
            
            // Value adjustment
            var valueX = x + 220;
            var downBtn = new Rectangle(valueX, y + i * spacing + 35, 30, 25);
            var upBtn = new Rectangle(valueX + 150, y + i * spacing + 35, 30, 25);
            
            if (_ui.IsInside(downBtn, mousePos))
            {
                AdjustSkillValue(i, -50);
            }
            if (_ui.IsInside(upBtn, mousePos))
            {
                AdjustSkillValue(i, 50);
            }
        }
        
        // Dropdown selection
        if (_skillDropdown >= 0)
        {
            var dropdownRect = GetSkillDropdownRect(_skillDropdown);
            if (_ui.IsInside(dropdownRect, mousePos))
            {
                var itemHeight = 25;
                var relativeY = (int)mousePos.Y - dropdownRect.Y;
                var index = relativeY / itemHeight + _dropdownScroll;
                
                if (index >= 0 && index < AvailableSkills.Length)
                {
                    SetSkill(_skillDropdown, AvailableSkills[index].Id);
                    _skillDropdown = -1;
                }
            }
        }
    }
    
    private Rectangle GetSkillDropdownRect(int skillIndex)
    {
        var x = _mainPanel.X + 250;
        var y = _mainPanel.Y + 100 + skillIndex * 80 + 30;
        return new Rectangle(x, y, 200, 125); // Show 5 items
    }
    
    private void SetSkill(int index, int skillId)
    {
        switch (index)
        {
            case 0: _skill1 = skillId; break;
            case 1: _skill2 = skillId; break;
            case 2: _skill3 = skillId; break;
        }
    }
    
    private void AdjustSkillValue(int index, int delta)
    {
        ref int value = ref _skill1Value;
        switch (index)
        {
            case 1: value = ref _skill2Value; break;
            case 2: value = ref _skill3Value; break;
        }
        
        var newValue = Math.Clamp(value + delta, 0, 500);
        var totalChange = newValue - value;
        
        // Check if we have room
        var currentTotal = _skill1Value + _skill2Value + _skill3Value;
        if (currentTotal + totalChange > SkillTotal && delta > 0)
            return;
        
        value = newValue;
    }
    
    private int GetStatTotal() => _strength + _dexterity + _intelligence;
    
    private void BalanceStats()
    {
        // Auto-balance when reducing a stat
        var total = GetStatTotal();
        if (total < StatTotal)
        {
            // Add to the lowest stat
            if (_strength <= _dexterity && _strength <= _intelligence && _strength < MaxStat)
                _strength++;
            else if (_dexterity <= _intelligence && _dexterity < MaxStat)
                _dexterity++;
            else if (_intelligence < MaxStat)
                _intelligence++;
        }
    }
    
    private void CreateCharacter()
    {
        if (string.IsNullOrWhiteSpace(_name) || _name.Length < 3)
        {
            _statusMessage = "Name must be at least 3 characters";
            _statusColor = Color.Orange;
            return;
        }
        
        if (GetStatTotal() != StatTotal)
        {
            _statusMessage = $"Stats must total {StatTotal}";
            _statusColor = Color.Orange;
            return;
        }
        
        _isCreating = true;
        _statusMessage = "Creating character...";
        _statusColor = Color.Yellow;
        
        _ = _gameState.CreateCharacterAsync(new CreateCharacterRequestPacket
        {
            Name = _name,
            Gender = _gender,
            BodyType = _bodyType,
            SkinHue = _skinHue,
            HairStyle = _hairStyle,
            HairHue = _hairHue,
            Strength = (byte)_strength,
            Dexterity = (byte)_dexterity,
            Intelligence = (byte)_intelligence,
            StartingSkill1 = (ushort)_skill1,
            StartingSkill2 = (ushort)_skill2,
            StartingSkill3 = (ushort)_skill3,
        });
    }
    
    public void Draw(GameTime gameTime)
    {
        _ui.Begin();
        
        // Background
        _ui.DrawRectangle(new Rectangle(0, 0, 1280, 720), new Color(20, 25, 35));
        
        // Title
        _ui.DrawTextCentered("CREATE CHARACTER", new Vector2(640, 30), new Color(200, 180, 120), 3f);
        
        // Main panel
        _ui.DrawPanel(_mainPanel);
        
        // Page tabs
        var tabs = new[] { "APPEARANCE", "STATS", "SKILLS" };
        for (int i = 0; i < 3; i++)
        {
            var tabRect = new Rectangle(_mainPanel.X + 240 + i * 120, _mainPanel.Y + 20, 110, 30);
            var tabColor = i == _currentPage ? new Color(80, 80, 110) : new Color(50, 50, 70);
            var tabHover = _ui.IsInside(tabRect, _input.MousePosition);
            
            _ui.DrawRectangle(tabRect, tabHover && i != _currentPage ? new Color(60, 60, 80) : tabColor);
            _ui.DrawRectangleOutline(tabRect, i == _currentPage ? Color.CornflowerBlue : AssetManager.BorderColor);
            _ui.DrawTextCentered(tabs[i], new Vector2(tabRect.X + tabRect.Width / 2, tabRect.Y + 8), Color.White, 1.5f);
        }
        
        // Character preview area
        _ui.DrawRectangle(_previewArea, new Color(30, 30, 40));
        _ui.DrawRectangleOutline(_previewArea, AssetManager.BorderColor);
        _ui.DrawTextCentered("PREVIEW", new Vector2(_previewArea.X + _previewArea.Width / 2, _previewArea.Y + 10), Color.Gray, 1.5f);
        
        // Draw character sprite preview
        if (_assets != null)
        {
            var spriteRect = new Rectangle(_previewArea.X + 84, _previewArea.Y + 80, 32, 48);
            _ui.DrawTexture(_assets.PlayerSprite, spriteRect);
            
            // Draw gender icon
            _ui.DrawText(_gender == 0 ? "♂" : "♀", new Vector2(_previewArea.X + 150, _previewArea.Y + 80), 
                _gender == 0 ? Color.CornflowerBlue : Color.Pink, 2f);
            
            // Draw stats summary
            _ui.DrawText($"STR: {_strength}", new Vector2(_previewArea.X + 10, _previewArea.Y + 180), Color.White, 1.5f);
            _ui.DrawText($"DEX: {_dexterity}", new Vector2(_previewArea.X + 10, _previewArea.Y + 200), Color.White, 1.5f);
            _ui.DrawText($"INT: {_intelligence}", new Vector2(_previewArea.X + 10, _previewArea.Y + 220), Color.White, 1.5f);
        }
        else
        {
            // Fallback to text
            var charY = _previewArea.Y + 80;
            _ui.DrawTextCentered(_gender == 0 ? "♂" : "♀", new Vector2(_previewArea.X + 100, charY), Color.White, 8f);
        }
        
        // Name input
        _ui.DrawText("NAME:", new Vector2(_nameInputRect.X, _nameInputRect.Y - 20), Color.Gray, 1.5f);
        _ui.DrawTextBox(_nameInputRect, _name, _focusedField == 0);
        
        // Draw current page content
        switch (_currentPage)
        {
            case 0: DrawAppearancePage(); break;
            case 1: DrawStatsPage(); break;
            case 2: DrawSkillsPage(); break;
        }
        
        // Bottom buttons
        var createBtn = new Rectangle(_mainPanel.Right - 170, _mainPanel.Bottom - 50, 150, 36);
        var cancelBtn = new Rectangle(_mainPanel.X + 20, _mainPanel.Bottom - 50, 100, 36);
        
        var createHover = _ui.IsInside(createBtn, _input.MousePosition);
        var cancelHover = _ui.IsInside(cancelBtn, _input.MousePosition);
        
        _ui.DrawButton(createBtn, _isCreating ? "CREATING..." : "CREATE", createHover, _input.IsLeftMouseDown && createHover);
        _ui.DrawButton(cancelBtn, "CANCEL", cancelHover, _input.IsLeftMouseDown && cancelHover);
        
        // Status message
        _ui.DrawTextCentered(_statusMessage, new Vector2(640, _mainPanel.Bottom + 20), _statusColor);
        
        // Draw dropdown last (on top)
        if (_skillDropdown >= 0 && _currentPage == 2)
        {
            DrawSkillDropdown(_skillDropdown);
        }
        
        // Draw cursor
        if (_assets != null)
        {
            var mousePos = _input.MousePosition;
            var cursor = _assets.GetCursor(false, false);
            _ui.DrawTexture(cursor, new Rectangle((int)mousePos.X, (int)mousePos.Y, cursor.Width, cursor.Height));
        }
        
        _ui.End();
    }
    
    private void DrawAppearancePage()
    {
        var x = _mainPanel.X + 250;
        var y = _mainPanel.Y + 80;
        var spacing = 50;
        
        // Gender
        _ui.DrawText("GENDER:", new Vector2(x, y - 20), Color.Gray, 1.5f);
        var maleBtn = new Rectangle(x, y, 80, 30);
        var femaleBtn = new Rectangle(x + 90, y, 80, 30);
        _ui.DrawButton(maleBtn, "MALE", _gender == 0 || _ui.IsInside(maleBtn, _input.MousePosition), _gender == 0);
        _ui.DrawButton(femaleBtn, "FEMALE", _gender == 1 || _ui.IsInside(femaleBtn, _input.MousePosition), _gender == 1);
        
        // Skin tone
        y += spacing;
        _ui.DrawText("SKIN TONE:", new Vector2(x, y - 20), Color.Gray, 1.5f);
        DrawOptionSelector(x, y, $"Tone {_skinHueIndex + 1}");
        
        // Hair style
        y += spacing;
        _ui.DrawText("HAIR STYLE:", new Vector2(x, y - 20), Color.Gray, 1.5f);
        DrawOptionSelector(x, y, HairStyles[_hairStyleIndex].Name);
        
        // Hair color
        y += spacing;
        _ui.DrawText("HAIR COLOR:", new Vector2(x, y - 20), Color.Gray, 1.5f);
        DrawOptionSelector(x, y, $"Color {_hairHueIndex + 1}");
        
        // Instructions
        _ui.DrawText("Use the arrows to customize your character's appearance.",
            new Vector2(x, _mainPanel.Bottom - 100), Color.Gray, 1.5f);
    }
    
    private void DrawOptionSelector(int x, int y, string value)
    {
        var leftBtn = new Rectangle(x, y, 30, 30);
        var rightBtn = new Rectangle(x + 200, y, 30, 30);
        var valueRect = new Rectangle(x + 40, y, 150, 30);
        
        var leftHover = _ui.IsInside(leftBtn, _input.MousePosition);
        var rightHover = _ui.IsInside(rightBtn, _input.MousePosition);
        
        _ui.DrawButton(leftBtn, "<", leftHover, _input.IsLeftMouseDown && leftHover);
        _ui.DrawButton(rightBtn, ">", rightHover, _input.IsLeftMouseDown && rightHover);
        _ui.DrawRectangle(valueRect, new Color(40, 40, 50));
        _ui.DrawTextCentered(value, new Vector2(valueRect.X + valueRect.Width / 2, valueRect.Y + 8), Color.White, 1.5f);
    }
    
    private void DrawStatsPage()
    {
        var x = _mainPanel.X + 250;
        var y = _mainPanel.Y + 100;
        var spacing = 60;
        
        // Header
        _ui.DrawText($"TOTAL: {GetStatTotal()}/{StatTotal}", new Vector2(x + 100, y - 40), 
            GetStatTotal() == StatTotal ? Color.Green : Color.Yellow, 2f);
        
        // Strength
        DrawStatRow(x, y, "STRENGTH", _strength, 
            "Increases melee damage and carrying capacity");
        
        // Dexterity
        y += spacing;
        DrawStatRow(x, y, "DEXTERITY", _dexterity,
            "Increases attack speed and defense");
        
        // Intelligence
        y += spacing;
        DrawStatRow(x, y, "INTELLIGENCE", _intelligence,
            "Increases mana pool and spell power");
        
        // Description
        _ui.DrawText("Minimum: 10  |  Maximum: 60  |  Total must equal 80",
            new Vector2(x, _mainPanel.Bottom - 100), Color.Gray, 1.5f);
    }
    
    private void DrawStatRow(int x, int y, string name, int value, string description)
    {
        _ui.DrawText($"{name}:", new Vector2(x, y), Color.White, 2f);
        
        var downBtn = new Rectangle(x, y + 25, 30, 25);
        var upBtn = new Rectangle(x + 200, y + 25, 30, 25);
        var valueRect = new Rectangle(x + 40, y + 25, 150, 25);
        
        var downHover = _ui.IsInside(downBtn, _input.MousePosition);
        var upHover = _ui.IsInside(upBtn, _input.MousePosition);
        
        _ui.DrawButton(downBtn, "-", downHover, _input.IsLeftMouseDown && downHover);
        _ui.DrawButton(upBtn, "+", upHover, _input.IsLeftMouseDown && upHover);
        
        // Value bar
        _ui.DrawRectangle(valueRect, new Color(30, 30, 40));
        var fillWidth = (int)(valueRect.Width * ((value - MinStat) / (float)(MaxStat - MinStat)));
        _ui.DrawRectangle(new Rectangle(valueRect.X, valueRect.Y, fillWidth, valueRect.Height), 
            new Color(60, 120, 60));
        _ui.DrawTextCentered(value.ToString(), new Vector2(valueRect.X + valueRect.Width / 2, valueRect.Y + 5), 
            Color.White, 1.5f);
        
        // Description
        _ui.DrawText(description, new Vector2(x + 250, y + 10), Color.Gray, 1.2f);
    }
    
    private void DrawSkillsPage()
    {
        var x = _mainPanel.X + 250;
        var y = _mainPanel.Y + 100;
        var spacing = 80;
        
        // Header
        var skillTotal = _skill1Value + _skill2Value + _skill3Value;
        _ui.DrawText($"TOTAL: {skillTotal / 10f:F1}/{SkillTotal / 10f:F1}", new Vector2(x + 100, y - 40),
            skillTotal <= SkillTotal ? Color.Green : Color.Red, 2f);
        
        // Skill 1
        DrawSkillRow(x, y, 0, "PRIMARY SKILL", _skill1, _skill1Value);
        
        // Skill 2
        y += spacing;
        DrawSkillRow(x, y, 1, "SECONDARY SKILL", _skill2, _skill2Value);
        
        // Skill 3
        y += spacing;
        DrawSkillRow(x, y, 2, "TERTIARY SKILL", _skill3, _skill3Value);
        
        // Instructions
        _ui.DrawText("Click skill name to change. Adjust starting values (max 50.0 each).",
            new Vector2(x, _mainPanel.Bottom - 100), Color.Gray, 1.5f);
    }
    
    private void DrawSkillRow(int x, int y, int index, string label, int skillId, int value)
    {
        _ui.DrawText($"{label}:", new Vector2(x, y - 20), Color.Gray, 1.2f);
        
        // Skill dropdown button
        var skillName = AvailableSkills.FirstOrDefault(s => s.Id == skillId).Name ?? "Unknown";
        var dropdownBtn = new Rectangle(x, y, 200, 30);
        var dropdownHover = _ui.IsInside(dropdownBtn, _input.MousePosition);
        
        _ui.DrawRectangle(dropdownBtn, dropdownHover ? new Color(60, 60, 80) : new Color(50, 50, 70));
        _ui.DrawRectangleOutline(dropdownBtn, _skillDropdown == index ? Color.CornflowerBlue : AssetManager.BorderColor);
        _ui.DrawText(skillName, new Vector2(dropdownBtn.X + 10, dropdownBtn.Y + 8), Color.White, 1.5f);
        _ui.DrawText("▼", new Vector2(dropdownBtn.Right - 20, dropdownBtn.Y + 8), Color.Gray, 1.2f);
        
        // Value adjustment
        var valueX = x + 220;
        var downBtn = new Rectangle(valueX, y + 5, 30, 25);
        var upBtn = new Rectangle(valueX + 150, y + 5, 30, 25);
        var valueRect = new Rectangle(valueX + 40, y + 5, 100, 25);
        
        var downHover = _ui.IsInside(downBtn, _input.MousePosition);
        var upHover = _ui.IsInside(upBtn, _input.MousePosition);
        
        _ui.DrawButton(downBtn, "-", downHover, _input.IsLeftMouseDown && downHover);
        _ui.DrawButton(upBtn, "+", upHover, _input.IsLeftMouseDown && upHover);
        
        _ui.DrawRectangle(valueRect, new Color(30, 30, 40));
        _ui.DrawTextCentered($"{value / 10f:F1}", new Vector2(valueRect.X + valueRect.Width / 2, valueRect.Y + 5),
            Color.White, 1.5f);
    }
    
    private void DrawSkillDropdown(int index)
    {
        var rect = GetSkillDropdownRect(index);
        
        // Background
        _ui.DrawRectangle(rect, new Color(40, 40, 50, 250));
        _ui.DrawRectangleOutline(rect, Color.CornflowerBlue);
        
        // Items
        var itemHeight = 25;
        var visibleCount = Math.Min(5, AvailableSkills.Length - _dropdownScroll);
        
        for (int i = 0; i < visibleCount; i++)
        {
            var skillIndex = i + _dropdownScroll;
            var skill = AvailableSkills[skillIndex];
            var itemRect = new Rectangle(rect.X, rect.Y + i * itemHeight, rect.Width, itemHeight);
            var isHover = _ui.IsInside(itemRect, _input.MousePosition);
            
            if (isHover)
            {
                _ui.DrawRectangle(itemRect, new Color(60, 80, 120));
            }
            
            _ui.DrawText(skill.Name, new Vector2(itemRect.X + 10, itemRect.Y + 5), Color.White, 1.3f);
            _ui.DrawText(skill.Category, new Vector2(itemRect.Right - 60, itemRect.Y + 5), Color.Gray, 1f);
        }
        
        // Scroll indicator
        if (AvailableSkills.Length > 5)
        {
            var scrollText = $"↑↓ {_dropdownScroll + 1}-{_dropdownScroll + visibleCount}/{AvailableSkills.Length}";
            _ui.DrawText(scrollText, new Vector2(rect.X + 5, rect.Bottom + 5), Color.Gray, 1f);
        }
    }
}
