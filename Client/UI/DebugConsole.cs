using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Text;

namespace RealmOfReality.Client.UI;

/// <summary>
/// In-game debug console for viewing logs and executing commands
/// Toggle with ~ (tilde) or F12
/// </summary>
public class DebugConsole
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly UIRenderer _ui;
    
    // Console state
    public bool IsVisible { get; private set; } = false;
    private readonly List<LogEntry> _logEntries = new();
    private readonly StringBuilder _inputBuffer = new();
    private readonly List<string> _commandHistory = new();
    private int _historyIndex = -1;
    private int _scrollOffset = 0;
    
    // Rendering
    private Texture2D _pixel;
    private const int MaxLogEntries = 500;
    private const int VisibleLines = 18;
    private const int ConsoleHeight = 350;
    private const int LineHeight = 16;
    private int _cursorBlink = 0;
    
    // Input handling
    private KeyboardState _prevKeyState;
    private Keys[] _prevPressedKeys = Array.Empty<Keys>();
    
    // Command handlers
    private readonly Dictionary<string, (Action<string[]> handler, string description)> _commands = new();
    
    // Event for external command handling
    public event Action<string, string[]>? OnCommand;
    
    // Static instance for global logging
    private static DebugConsole? _instance;
    public static DebugConsole? Instance => _instance;
    
    public DebugConsole(GraphicsDevice graphicsDevice, UIRenderer ui)
    {
        _graphicsDevice = graphicsDevice;
        _ui = ui;
        _instance = this;
        
        // Create pixel texture for backgrounds
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        
        // Register built-in commands
        RegisterBuiltInCommands();
        
        Log("Debug Console initialized. Press ~ or F12 to toggle.", Color.Cyan);
        Log("Type 'help' for available commands.", Color.Gray);
    }
    
    private void RegisterBuiltInCommands()
    {
        RegisterCommand("help", args => ShowHelp(), "Show help information");
        RegisterCommand("clear", args => _logEntries.Clear(), "Clear console output");
        RegisterCommand("cls", args => _logEntries.Clear(), "Clear console output");
        RegisterCommand("echo", args => Log(string.Join(" ", args), Color.White), "Echo text to console");
        RegisterCommand("commands", args => ListCommands(), "List all available commands");
    }
    
    /// <summary>
    /// Register a command handler
    /// </summary>
    public void RegisterCommand(string name, Action<string[]> handler, string description = "")
    {
        _commands[name.ToLower()] = (handler, description);
    }
    
    /// <summary>
    /// Log a message to the console
    /// </summary>
    public void Log(string message, Color? color = null)
    {
        var entry = new LogEntry
        {
            Message = message,
            Color = color ?? Color.White,
            Timestamp = DateTime.Now
        };
        
        _logEntries.Add(entry);
        
        // Trim old entries
        while (_logEntries.Count > MaxLogEntries)
            _logEntries.RemoveAt(0);
        
        // Auto-scroll to bottom
        _scrollOffset = Math.Max(0, _logEntries.Count - VisibleLines);
    }
    
    /// <summary>
    /// Static log method for easy access
    /// </summary>
    public static void WriteLine(string message, Color? color = null)
    {
        _instance?.Log(message, color);
        
        // Also write to actual console for debugging
        Console.WriteLine(message);
    }
    
    public static void WriteError(string message) => WriteLine($"[ERROR] {message}", Color.Red);
    public static void WriteWarning(string message) => WriteLine($"[WARN] {message}", Color.Yellow);
    public static void WriteInfo(string message) => WriteLine($"[INFO] {message}", Color.Cyan);
    public static void WriteSuccess(string message) => WriteLine(message, Color.LightGreen);
    
    /// <summary>
    /// Check if console is consuming input (so game shouldn't process it)
    /// </summary>
    public bool IsConsumingInput => IsVisible;
    
    public void Update(GameTime gameTime)
    {
        var keyState = Keyboard.GetState();
        var pressedKeys = keyState.GetPressedKeys();
        
        // Toggle console with ~ or F12
        if (IsKeyJustPressed(Keys.OemTilde, keyState) || IsKeyJustPressed(Keys.F12, keyState))
        {
            IsVisible = !IsVisible;
            _inputBuffer.Clear();
        }
        
        if (!IsVisible)
        {
            _prevKeyState = keyState;
            _prevPressedKeys = pressedKeys;
            return;
        }
        
        _cursorBlink = (_cursorBlink + 1) % 60;
        
        // Handle input
        foreach (var key in pressedKeys)
        {
            if (!_prevPressedKeys.Contains(key))
            {
                HandleKeyPress(key, keyState);
            }
        }
        
        _prevKeyState = keyState;
        _prevPressedKeys = pressedKeys;
    }
    
    private bool IsKeyJustPressed(Keys key, KeyboardState current)
    {
        return current.IsKeyDown(key) && _prevKeyState.IsKeyUp(key);
    }
    
    private void HandleKeyPress(Keys key, KeyboardState keyState)
    {
        bool shift = keyState.IsKeyDown(Keys.LeftShift) || keyState.IsKeyDown(Keys.RightShift);
        
        switch (key)
        {
            case Keys.Enter:
                ExecuteCommand();
                break;
                
            case Keys.Back:
                if (_inputBuffer.Length > 0)
                    _inputBuffer.Length--;
                break;
                
            case Keys.Escape:
                if (_inputBuffer.Length > 0)
                    _inputBuffer.Clear();
                else
                    IsVisible = false;
                break;
                
            case Keys.Up:
                NavigateHistory(-1);
                break;
                
            case Keys.Down:
                NavigateHistory(1);
                break;
                
            case Keys.PageUp:
                _scrollOffset = Math.Max(0, _scrollOffset - 5);
                break;
                
            case Keys.PageDown:
                _scrollOffset = Math.Min(_logEntries.Count - VisibleLines, _scrollOffset + 5);
                break;
                
            case Keys.Tab:
                AutoComplete();
                break;
                
            default:
                var c = KeyToChar(key, shift);
                if (c.HasValue && _inputBuffer.Length < 200)
                {
                    _inputBuffer.Append(c.Value);
                }
                break;
        }
    }
    
    private char? KeyToChar(Keys key, bool shift)
    {
        // Letters
        if (key >= Keys.A && key <= Keys.Z)
        {
            char c = (char)('a' + (key - Keys.A));
            return shift ? char.ToUpper(c) : c;
        }
        
        // Numbers
        if (key >= Keys.D0 && key <= Keys.D9)
        {
            if (shift)
            {
                return key switch
                {
                    Keys.D1 => '!',
                    Keys.D2 => '@',
                    Keys.D3 => '#',
                    Keys.D4 => '$',
                    Keys.D5 => '%',
                    Keys.D6 => '^',
                    Keys.D7 => '&',
                    Keys.D8 => '*',
                    Keys.D9 => '(',
                    Keys.D0 => ')',
                    _ => null
                };
            }
            return (char)('0' + (key - Keys.D0));
        }
        
        // Numpad
        if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            return (char)('0' + (key - Keys.NumPad0));
        
        // Special characters
        return key switch
        {
            Keys.Space => ' ',
            Keys.OemPeriod => shift ? '>' : '.',
            Keys.OemComma => shift ? '<' : ',',
            Keys.OemMinus => shift ? '_' : '-',
            Keys.OemPlus => shift ? '+' : '=',
            Keys.OemSemicolon => shift ? ':' : ';',
            Keys.OemQuotes => shift ? '"' : '\'',
            Keys.OemOpenBrackets => shift ? '{' : '[',
            Keys.OemCloseBrackets => shift ? '}' : ']',
            Keys.OemPipe => shift ? '|' : '\\',
            Keys.OemQuestion => shift ? '?' : '/',
            _ => null
        };
    }
    
    private void NavigateHistory(int direction)
    {
        if (_commandHistory.Count == 0) return;
        
        _historyIndex = Math.Clamp(_historyIndex + direction, -1, _commandHistory.Count - 1);
        
        _inputBuffer.Clear();
        if (_historyIndex >= 0 && _historyIndex < _commandHistory.Count)
        {
            _inputBuffer.Append(_commandHistory[_historyIndex]);
        }
    }
    
    private void AutoComplete()
    {
        var input = _inputBuffer.ToString().ToLower();
        if (string.IsNullOrEmpty(input)) return;
        
        var matches = _commands.Keys.Where(k => k.StartsWith(input)).ToList();
        
        if (matches.Count == 1)
        {
            _inputBuffer.Clear();
            _inputBuffer.Append(matches[0]);
        }
        else if (matches.Count > 1)
        {
            Log($"Matches: {string.Join(", ", matches)}", Color.Gray);
        }
    }
    
    private void ExecuteCommand()
    {
        var input = _inputBuffer.ToString().Trim();
        _inputBuffer.Clear();
        
        if (string.IsNullOrEmpty(input)) return;
        
        // Add to history
        _commandHistory.Insert(0, input);
        if (_commandHistory.Count > 50)
            _commandHistory.RemoveAt(_commandHistory.Count - 1);
        _historyIndex = -1;
        
        // Log the command
        Log($"> {input}", Color.Yellow);
        
        // Parse command and args
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;
        
        var command = parts[0].ToLower();
        var args = parts.Skip(1).ToArray();
        
        // Try registered commands
        if (_commands.TryGetValue(command, out var cmd))
        {
            try
            {
                cmd.handler(args);
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}", Color.Red);
            }
        }
        else
        {
            // Fire event for external handling
            if (OnCommand != null)
            {
                OnCommand.Invoke(command, args);
            }
            else
            {
                Log($"Unknown command: {command}", Color.Red);
            }
        }
    }
    
    private void ShowHelp()
    {
        Log("=== Debug Console Help ===", Color.Cyan);
        Log("~ or F12  - Toggle console", Color.Gray);
        Log("Enter     - Execute command", Color.Gray);
        Log("Up/Down   - Command history", Color.Gray);
        Log("Tab       - Auto-complete", Color.Gray);
        Log("PageUp/Dn - Scroll log", Color.Gray);
        Log("Escape    - Clear input or close", Color.Gray);
        Log("", Color.White);
        Log("Type 'commands' for list of available commands", Color.Gray);
    }
    
    private void ListCommands()
    {
        Log("=== Available Commands ===", Color.Cyan);
        foreach (var kvp in _commands.OrderBy(k => k.Key))
        {
            var desc = string.IsNullOrEmpty(kvp.Value.description) ? "" : $" - {kvp.Value.description}";
            Log($"  {kvp.Key}{desc}", Color.White);
        }
    }
    
    public void Draw()
    {
        if (!IsVisible) return;
        
        var screenWidth = _graphicsDevice.Viewport.Width;
        
        _ui.Begin();
        
        // Background
        _ui.DrawRectangle(new Rectangle(0, 0, screenWidth, ConsoleHeight), new Color(0, 0, 0, 230));
        
        // Border
        _ui.DrawRectangle(new Rectangle(0, ConsoleHeight - 2, screenWidth, 2), Color.DarkCyan);
        
        // Title bar
        _ui.DrawRectangle(new Rectangle(0, 0, screenWidth, 20), new Color(20, 60, 80));
        _ui.DrawText("DEBUG CONSOLE (~ to close)", new Vector2(10, 2), Color.Cyan, 1.0f);
        
        // Log entries
        int y = 24;
        int startIdx = Math.Max(0, Math.Min(_scrollOffset, _logEntries.Count - VisibleLines));
        int endIdx = Math.Min(startIdx + VisibleLines, _logEntries.Count);
        
        for (int i = startIdx; i < endIdx; i++)
        {
            var entry = _logEntries[i];
            var timestamp = entry.Timestamp.ToString("HH:mm:ss");
            _ui.DrawText($"[{timestamp}] {entry.Message}", new Vector2(8, y), entry.Color, 1.0f);
            y += LineHeight;
        }
        
        // Input line background
        _ui.DrawRectangle(new Rectangle(0, ConsoleHeight - 24, screenWidth, 22), new Color(30, 30, 50));
        
        // Input prompt and text
        var inputText = $"> {_inputBuffer}";
        if (_cursorBlink < 30)
            inputText += "_";
        _ui.DrawText(inputText, new Vector2(8, ConsoleHeight - 22), Color.LightGreen, 1.0f);
        
        // Scroll indicator
        if (_logEntries.Count > VisibleLines)
        {
            int scrollBarHeight = ConsoleHeight - 50;
            var scrollPercent = (float)_scrollOffset / Math.Max(1, _logEntries.Count - VisibleLines);
            var scrollY = (int)(24 + scrollPercent * (scrollBarHeight - 30));
            _ui.DrawRectangle(new Rectangle(screenWidth - 8, scrollY, 4, 30), Color.DarkCyan);
        }
        
        _ui.End();
    }
    
    private struct LogEntry
    {
        public string Message;
        public Color Color;
        public DateTime Timestamp;
    }
}
