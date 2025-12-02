using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RealmOfReality.Client.Engine;
using RealmOfReality.Client.Game;
using RealmOfReality.Client.Network;

namespace RealmOfReality.Client.UI;

/// <summary>
/// Login screen for entering credentials
/// </summary>
public class LoginScreen : IScreen
{
    private readonly GameState _gameState;
    private readonly GameClient _networkClient;
    private readonly UIRenderer _ui;
    private readonly InputManager _input;
    private readonly AssetManager _assets;
    private readonly ClientSettings? _settings;
    private readonly Action? _onSettingsClick;
    
    // UI State
    private string _username = "";
    private string _password = "";
    private string _serverAddress = "127.0.0.1";
    private string _statusMessage = "";
    private Color _statusColor = Color.White;
    private int _focusedField = 0; // 0=server, 1=username, 2=password
    private bool _connecting = false;
    
    // UI Layout
    private Rectangle _panelRect;
    private Rectangle _serverRect;
    private Rectangle _usernameRect;
    private Rectangle _passwordRect;
    private Rectangle _connectButtonRect;
    private Rectangle _quitButtonRect;
    private Rectangle _settingsButtonRect;
    
    public LoginScreen(GameState gameState, GameClient networkClient, UIRenderer ui, InputManager input,
        AssetManager assets, ClientSettings? settings = null, Action? onSettingsClick = null)
    {
        _gameState = gameState;
        _networkClient = networkClient;
        _ui = ui;
        _input = input;
        _assets = assets;
        _settings = settings;
        _onSettingsClick = onSettingsClick;
    }
    
    public void Enter()
    {
        _statusMessage = "Enter server address and credentials";
        _statusColor = Color.Gray;
        _connecting = false;
        _focusedField = 1; // Start on username
        
        // Load saved values from settings
        if (_settings != null)
        {
            _serverAddress = _settings.LastServerAddress;
            _username = _settings.LastUsername;
        }
        
        // Calculate layout
        UpdateLayout();
    }
    
    public void Exit()
    {
        _connecting = false;
    }
    
    private void UpdateLayout()
    {
        // Center panel
        var panelWidth = 400;
        var panelHeight = 380;
        var screenWidth = 1280; // Will be updated in Draw
        var screenHeight = 720;
        
        _panelRect = new Rectangle(
            (screenWidth - panelWidth) / 2,
            (screenHeight - panelHeight) / 2,
            panelWidth,
            panelHeight
        );
        
        var fieldWidth = panelWidth - 60;
        var fieldHeight = 36;
        var startY = _panelRect.Y + 80;
        var spacing = 60;
        
        _serverRect = new Rectangle(_panelRect.X + 30, startY, fieldWidth, fieldHeight);
        _usernameRect = new Rectangle(_panelRect.X + 30, startY + spacing, fieldWidth, fieldHeight);
        _passwordRect = new Rectangle(_panelRect.X + 30, startY + spacing * 2, fieldWidth, fieldHeight);
        
        var buttonWidth = 120;
        var buttonHeight = 40;
        var buttonY = _panelRect.Bottom - 70;
        
        _connectButtonRect = new Rectangle(_panelRect.X + 25, buttonY, buttonWidth, buttonHeight);
        _settingsButtonRect = new Rectangle(_panelRect.X + 155, buttonY, buttonWidth, buttonHeight);
        _quitButtonRect = new Rectangle(_panelRect.Right - 25 - buttonWidth, buttonY, buttonWidth, buttonHeight);
    }
    
    public void Update(GameTime gameTime)
    {
        // Handle tab to switch fields
        if (_input.IsKeyPressed(Keys.Tab))
        {
            if (_input.IsShiftDown)
                _focusedField = (_focusedField - 1 + 3) % 3;
            else
                _focusedField = (_focusedField + 1) % 3;
        }
        
        // Handle enter to submit
        if (_input.IsKeyPressed(Keys.Enter) && !_connecting)
        {
            _ = ConnectAsync();
        }
        
        // Handle text input for focused field
        HandleTextInput();
        
        // Handle mouse clicks
        HandleMouseInput();
    }
    
    private void HandleTextInput()
    {
        var keys = _input.GetPressedKeys();
        
        foreach (var key in keys)
        {
            if (!_input.IsKeyPressed(key)) continue;
            
            // Get current field value
            string fieldValue = _focusedField switch
            {
                0 => _serverAddress,
                1 => _username,
                _ => _password
            };
            
            string newValue = fieldValue;
            
            if (key == Keys.Back && fieldValue.Length > 0)
            {
                newValue = fieldValue[..^1];
            }
            else if (key >= Keys.A && key <= Keys.Z)
            {
                char c = (char)('a' + (key - Keys.A));
                if (_input.IsShiftDown) c = char.ToUpper(c);
                newValue = fieldValue + c;
            }
            else if (key >= Keys.D0 && key <= Keys.D9)
            {
                newValue = fieldValue + (char)('0' + (key - Keys.D0));
            }
            else if (key == Keys.OemPeriod)
            {
                newValue = fieldValue + '.';
            }
            else if (key == Keys.OemMinus)
            {
                newValue = fieldValue + (_input.IsShiftDown ? '_' : '-');
            }
            
            // Assign back to the correct field
            switch (_focusedField)
            {
                case 0: _serverAddress = newValue; break;
                case 1: _username = newValue; break;
                default: _password = newValue; break;
            }
        }
    }
    
    private void HandleMouseInput()
    {
        var mousePos = _input.MousePosition;
        
        if (_input.IsLeftMousePressed)
        {
            if (_ui.IsInside(_serverRect, mousePos))
                _focusedField = 0;
            else if (_ui.IsInside(_usernameRect, mousePos))
                _focusedField = 1;
            else if (_ui.IsInside(_passwordRect, mousePos))
                _focusedField = 2;
            else if (_ui.IsInside(_connectButtonRect, mousePos) && !_connecting)
                _ = ConnectAsync();
            else if (_ui.IsInside(_settingsButtonRect, mousePos))
                _onSettingsClick?.Invoke();
            else if (_ui.IsInside(_quitButtonRect, mousePos))
                Environment.Exit(0);
        }
    }
    
    private async Task ConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(_username))
        {
            _statusMessage = "Please enter a username";
            _statusColor = Color.Orange;
            return;
        }
        
        if (string.IsNullOrWhiteSpace(_password))
        {
            _statusMessage = "Please enter a password";
            _statusColor = Color.Orange;
            return;
        }
        
        _connecting = true;
        _statusMessage = "Connecting...";
        _statusColor = Color.Yellow;
        
        try
        {
            // Parse server address
            var parts = _serverAddress.Split(':');
            var host = parts[0];
            var port = parts.Length > 1 ? int.Parse(parts[1]) : 7775;
            
            // Create new client with address
            // Note: In a real implementation, you'd update the existing client
            
            if (await _networkClient.ConnectAsync())
            {
                _statusMessage = "Connected! Logging in...";
                _statusColor = Color.Green;
                
                // Send login request
                await _gameState.LoginAsync(_username, _password);
            }
            else
            {
                _statusMessage = "Failed to connect to server";
                _statusColor = Color.Red;
                _connecting = false;
            }
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error: {ex.Message}";
            _statusColor = Color.Red;
            _connecting = false;
        }
    }
    
    public void Draw(GameTime gameTime)
    {
        _ui.Begin();
        
        // Draw background
        _ui.DrawRectangle(new Rectangle(0, 0, 1280, 720), new Color(20, 25, 35));
        
        // Draw title
        _ui.DrawTextCentered("REALM OF REALITY", new Vector2(640, 80), new Color(200, 180, 120), 4f);
        _ui.DrawTextCentered("A 2.5D ISOMETRIC MMORPG", new Vector2(640, 130), new Color(120, 120, 140), 2f);
        
        // Draw panel
        _ui.DrawPanel(_panelRect);
        
        // Draw panel title
        _ui.DrawTextCentered("LOGIN", new Vector2(_panelRect.X + _panelRect.Width / 2, _panelRect.Y + 30), Color.White, 2.5f);
        
        // Draw labels
        _ui.DrawText("SERVER:", new Vector2(_serverRect.X, _serverRect.Y - 22), Color.Gray);
        _ui.DrawText("USERNAME:", new Vector2(_usernameRect.X, _usernameRect.Y - 22), Color.Gray);
        _ui.DrawText("PASSWORD:", new Vector2(_passwordRect.X, _passwordRect.Y - 22), Color.Gray);
        
        // Draw input fields
        _ui.DrawTextBox(_serverRect, _serverAddress, _focusedField == 0);
        _ui.DrawTextBox(_usernameRect, _username, _focusedField == 1);
        _ui.DrawTextBox(_passwordRect, _password, _focusedField == 2, isPassword: true);
        
        // Draw buttons
        var mousePos = _input.MousePosition;
        var connectHover = _ui.IsInside(_connectButtonRect, mousePos);
        var settingsHover = _ui.IsInside(_settingsButtonRect, mousePos);
        var quitHover = _ui.IsInside(_quitButtonRect, mousePos);
        
        _ui.DrawButton(_connectButtonRect, _connecting ? "..." : "CONNECT", connectHover, _input.IsLeftMouseDown && connectHover);
        _ui.DrawButton(_settingsButtonRect, "SETTINGS", settingsHover, _input.IsLeftMouseDown && settingsHover);
        _ui.DrawButton(_quitButtonRect, "QUIT", quitHover, _input.IsLeftMouseDown && quitHover);
        
        // Draw UO assets status
        var uoStatus = RealmGame.UOAssets != null ? "UO Assets: Loaded" : "UO Assets: Not loaded";
        var uoColor = RealmGame.UOAssets != null ? Color.LightGreen : Color.Yellow;
        _ui.DrawText(uoStatus, new Vector2(_panelRect.X + 30, _panelRect.Y + 315), uoColor, 1.1f);
        
        // Draw status message
        _ui.DrawTextCentered(_statusMessage, new Vector2(640, _panelRect.Bottom + 30), _statusColor);
        
        // Draw version
        _ui.DrawText("V0.1.0", new Vector2(10, 700), Color.DarkGray);
        
        // Draw cursor
        DrawCursor();
        
        _ui.End();
    }
    
    private void DrawCursor()
    {
        var mousePos = _input.MousePosition;
        var cursor = _assets.GetCursor(false, false);
        _ui.DrawTexture(cursor, new Rectangle((int)mousePos.X, (int)mousePos.Y, cursor.Width, cursor.Height));
    }
}
