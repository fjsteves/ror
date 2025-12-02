using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RealmOfReality.Client.Engine;
using RealmOfReality.Client.Game;
using RealmOfReality.Shared.Network;

namespace RealmOfReality.Client.UI;

/// <summary>
/// Character selection screen
/// </summary>
public class CharacterSelectScreen : IScreen
{
    private readonly GameState _gameState;
    private readonly UIRenderer _ui;
    private readonly InputManager _input;
    private readonly AssetManager _assets;
    private readonly Action? _onCreateCharacter;
    
    // State
    private int _selectedIndex = 0;
    private string _statusMessage = "";
    private Color _statusColor = Color.White;
    
    // Layout
    private Rectangle _panelRect;
    private Rectangle _charListRect;
    private Rectangle _playButtonRect;
    private Rectangle _createButtonRect;
    private Rectangle _deleteButtonRect;
    private Rectangle _backButtonRect;
    
    public CharacterSelectScreen(GameState gameState, UIRenderer ui, InputManager input, AssetManager assets, Action? onCreateCharacter = null)
    {
        _gameState = gameState;
        _ui = ui;
        _input = input;
        _assets = assets;
        _onCreateCharacter = onCreateCharacter;
        
        // Listen for state changes (e.g., character created response)
        _gameState.StateChanged += OnStateChanged;
    }
    
    private void OnStateChanged()
    {
        // Check for messages from server
        if (!string.IsNullOrEmpty(_gameState.AccountMessage))
        {
            _statusMessage = _gameState.AccountMessage;
            // Determine color based on content
            if (_statusMessage.Contains("Welcome") || _statusMessage.Contains("Success"))
                _statusColor = Color.Green;
            else if (_statusMessage.Contains("Error") || _statusMessage.Contains("Failed") || _statusMessage.Contains("taken"))
                _statusColor = Color.Red;
            else
                _statusColor = Color.Yellow;
        }
    }
    
    public void Enter()
    {
        _selectedIndex = 0;
        _statusMessage = _gameState.Characters.Count > 0 
            ? "Select a character to play" 
            : "Create your first character!";
        _statusColor = Color.Gray;
        
        UpdateLayout();
    }
    
    public void Exit()
    {
    }
    
    private void UpdateLayout()
    {
        var panelWidth = 500;
        var panelHeight = 450;
        
        _panelRect = new Rectangle(
            (1280 - panelWidth) / 2,
            (720 - panelHeight) / 2,
            panelWidth,
            panelHeight
        );
        
        _charListRect = new Rectangle(
            _panelRect.X + 20,
            _panelRect.Y + 60,
            panelWidth - 40,
            260
        );
        
        var buttonWidth = 140;
        var buttonHeight = 36;
        var buttonY = _panelRect.Bottom - 60;
        var buttonSpacing = 10;
        
        _playButtonRect = new Rectangle(_panelRect.X + 20, buttonY, buttonWidth, buttonHeight);
        _createButtonRect = new Rectangle(_playButtonRect.Right + buttonSpacing, buttonY, buttonWidth, buttonHeight);
        _deleteButtonRect = new Rectangle(_createButtonRect.Right + buttonSpacing, buttonY, buttonWidth, buttonHeight);
        
        _backButtonRect = new Rectangle(_panelRect.X + 20, _panelRect.Y + 14, 80, 30);
    }
    
    public void Update(GameTime gameTime)
    {
        UpdateMainScreen();
    }
    
    private void UpdateMainScreen()
    {
        var mousePos = _input.MousePosition;
        
        // Keyboard navigation
        if (_input.IsKeyPressed(Keys.Up) && _selectedIndex > 0)
            _selectedIndex--;
        if (_input.IsKeyPressed(Keys.Down) && _selectedIndex < _gameState.Characters.Count - 1)
            _selectedIndex++;
        
        if (_input.IsKeyPressed(Keys.Enter) && _gameState.Characters.Count > 0)
            _ = PlaySelectedCharacterAsync();
        
        // Mouse input
        if (_input.IsLeftMousePressed)
        {
            // Check character list clicks
            for (int i = 0; i < _gameState.Characters.Count; i++)
            {
                var itemRect = new Rectangle(
                    _charListRect.X + 5,
                    _charListRect.Y + 5 + i * 50,
                    _charListRect.Width - 10,
                    45
                );
                
                if (_ui.IsInside(itemRect, mousePos))
                {
                    if (_selectedIndex == i)
                        _ = PlaySelectedCharacterAsync(); // Double-click effect
                    else
                        _selectedIndex = i;
                }
            }
            
            // Check buttons
            if (_ui.IsInside(_playButtonRect, mousePos) && _gameState.Characters.Count > 0)
                _ = PlaySelectedCharacterAsync();
            else if (_ui.IsInside(_createButtonRect, mousePos))
                _onCreateCharacter?.Invoke();
            else if (_ui.IsInside(_deleteButtonRect, mousePos) && _gameState.Characters.Count > 0)
                _ = DeleteSelectedCharacterAsync();
            else if (_ui.IsInside(_backButtonRect, mousePos))
                _gameState.GetType().GetMethod("Disconnect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        }
    }
    
    private async Task PlaySelectedCharacterAsync()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _gameState.Characters.Count)
            return;
        
        _statusMessage = "Entering world...";
        _statusColor = Color.Yellow;
        
        var character = _gameState.Characters[_selectedIndex];
        await _gameState.SelectCharacterAsync(character.Id);
    }
    
    private async Task DeleteSelectedCharacterAsync()
    {
        // Not implemented in this version
        _statusMessage = "Delete not available in beta";
        _statusColor = Color.Orange;
        await Task.CompletedTask;
    }
    
    public void Draw(GameTime gameTime)
    {
        _ui.Begin();
        
        // Background
        _ui.DrawRectangle(new Rectangle(0, 0, 1280, 720), new Color(20, 25, 35));
        
        // Title
        _ui.DrawTextCentered("CHARACTER SELECT", new Vector2(640, 60), new Color(200, 180, 120), 3f);
        
        // Main panel
        _ui.DrawPanel(_panelRect);
        
        // Back button
        var mousePos = _input.MousePosition;
        _ui.DrawButton(_backButtonRect, "BACK", _ui.IsInside(_backButtonRect, mousePos), false);
        
        // Character count
        _ui.DrawText(
            $"CHARACTERS: {_gameState.Characters.Count}/{_gameState.MaxCharacters}",
            new Vector2(_panelRect.X + 120, _panelRect.Y + 18),
            Color.Gray
        );
        
        // Character list background
        _ui.DrawRectangle(_charListRect, new Color(30, 30, 40));
        _ui.DrawRectangleOutline(_charListRect, Engine.AssetManager.BorderColor);
        
        // Character list items
        for (int i = 0; i < _gameState.Characters.Count; i++)
        {
            var character = _gameState.Characters[i];
            var itemRect = new Rectangle(
                _charListRect.X + 5,
                _charListRect.Y + 5 + i * 50,
                _charListRect.Width - 10,
                45
            );
            
            var isSelected = i == _selectedIndex;
            var isHovered = _ui.IsInside(itemRect, mousePos);
            
            // Item background
            if (isSelected)
                _ui.DrawRectangle(itemRect, new Color(60, 80, 120));
            else if (isHovered)
                _ui.DrawRectangle(itemRect, new Color(50, 50, 70));
            
            // Character info
            _ui.DrawText(character.Name, new Vector2(itemRect.X + 10, itemRect.Y + 5), Color.White);
            _ui.DrawText(
                $"LEVEL {character.Level}",
                new Vector2(itemRect.X + 10, itemRect.Y + 25),
                Color.Gray,
                1.5f
            );
            
            // Location (right side)
            _ui.DrawText(
                character.Location,
                new Vector2(itemRect.Right - 100, itemRect.Y + 12),
                Color.DarkGray,
                1.5f
            );
        }
        
        // Empty state
        if (_gameState.Characters.Count == 0)
        {
            _ui.DrawTextCentered(
                "NO CHARACTERS",
                new Vector2(_charListRect.X + _charListRect.Width / 2, _charListRect.Y + _charListRect.Height / 2 - 20),
                Color.Gray
            );
            _ui.DrawTextCentered(
                "CLICK CREATE TO START",
                new Vector2(_charListRect.X + _charListRect.Width / 2, _charListRect.Y + _charListRect.Height / 2 + 10),
                Color.DarkGray,
                1.5f
            );
        }
        
        // Buttons
        var playHover = _ui.IsInside(_playButtonRect, mousePos);
        var createHover = _ui.IsInside(_createButtonRect, mousePos);
        var deleteHover = _ui.IsInside(_deleteButtonRect, mousePos);
        
        _ui.DrawButton(_playButtonRect, "PLAY", playHover && _gameState.Characters.Count > 0, 
            _input.IsLeftMouseDown && playHover);
        _ui.DrawButton(_createButtonRect, "CREATE", createHover, _input.IsLeftMouseDown && createHover);
        _ui.DrawButton(_deleteButtonRect, "DELETE", deleteHover && _gameState.Characters.Count > 0, 
            _input.IsLeftMouseDown && deleteHover);
        
        // Status message
        _ui.DrawTextCentered(_statusMessage, new Vector2(640, _panelRect.Bottom + 30), _statusColor);
        
        // Draw cursor (reuse mousePos from earlier in Draw method)
        var cursor = _assets.GetCursor(false, false);
        _ui.DrawTexture(cursor, new Rectangle((int)mousePos.X, (int)mousePos.Y, cursor.Width, cursor.Height));
        
        _ui.End();
    }
}
