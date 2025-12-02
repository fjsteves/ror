using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace RealmOfReality.Client.Engine;

/// <summary>
/// Manages keyboard and mouse input with state tracking
/// </summary>
public class InputManager
{
    private KeyboardState _currentKeyboard;
    private KeyboardState _previousKeyboard;
    private MouseState _currentMouse;
    private MouseState _previousMouse;
    
    // Text input buffer for UI
    public string TextBuffer { get; private set; } = "";
    public bool TextInputActive { get; set; }
    public int MaxTextLength { get; set; } = 256;
    
    // Movement keys mapping - ISOMETRIC corrected
    // Screen direction → World direction:
    // Screen Up = NW, Screen Down = SE, Screen Left = SW, Screen Right = NE
    // Screen Up-Left = W, Screen Up-Right = N, Screen Down-Left = S, Screen Down-Right = E
    private static readonly Dictionary<Keys, (int dx, int dy)> MovementKeys = new()
    {
        // Cardinal screen directions → Diagonal world directions
        { Keys.W, (-1, -1) },      // Screen up → World NW
        { Keys.S, (1, 1) },        // Screen down → World SE
        { Keys.A, (-1, 1) },       // Screen left → World SW
        { Keys.D, (1, -1) },       // Screen right → World NE
        { Keys.Up, (-1, -1) },     // Screen up → World NW
        { Keys.Down, (1, 1) },     // Screen down → World SE
        { Keys.Left, (-1, 1) },    // Screen left → World SW
        { Keys.Right, (1, -1) },   // Screen right → World NE
        { Keys.NumPad8, (-1, -1) },// Screen up → World NW
        { Keys.NumPad2, (1, 1) },  // Screen down → World SE
        { Keys.NumPad4, (-1, 1) }, // Screen left → World SW
        { Keys.NumPad6, (1, -1) }, // Screen right → World NE
        // Diagonal screen directions → Cardinal world directions
        { Keys.NumPad7, (-1, 0) }, // Screen up-left → World West
        { Keys.NumPad9, (0, -1) }, // Screen up-right → World North
        { Keys.NumPad1, (0, 1) },  // Screen down-left → World South
        { Keys.NumPad3, (1, 0) },  // Screen down-right → World East
    };
    
    public InputManager()
    {
        _currentKeyboard = Keyboard.GetState();
        _previousKeyboard = _currentKeyboard;
        _currentMouse = Mouse.GetState();
        _previousMouse = _currentMouse;
    }
    
    public void Update()
    {
        _previousKeyboard = _currentKeyboard;
        _previousMouse = _currentMouse;
        _currentKeyboard = Keyboard.GetState();
        _currentMouse = Mouse.GetState();
    }
    
    // Keyboard
    public bool IsKeyDown(Keys key) => _currentKeyboard.IsKeyDown(key);
    public bool IsKeyUp(Keys key) => _currentKeyboard.IsKeyUp(key);
    public bool IsKeyPressed(Keys key) => _currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
    public bool IsKeyReleased(Keys key) => _currentKeyboard.IsKeyUp(key) && _previousKeyboard.IsKeyDown(key);
    
    public bool IsShiftDown => IsKeyDown(Keys.LeftShift) || IsKeyDown(Keys.RightShift);
    public bool IsControlDown => IsKeyDown(Keys.LeftControl) || IsKeyDown(Keys.RightControl);
    public bool IsAltDown => IsKeyDown(Keys.LeftAlt) || IsKeyDown(Keys.RightAlt);
    
    // Mouse
    public Vector2 MousePosition => new(_currentMouse.X, _currentMouse.Y);
    public Vector2 MouseDelta => new(_currentMouse.X - _previousMouse.X, _currentMouse.Y - _previousMouse.Y);
    public int ScrollWheelDelta => _currentMouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;
    
    public bool IsLeftMouseDown => _currentMouse.LeftButton == ButtonState.Pressed;
    public bool IsRightMouseDown => _currentMouse.RightButton == ButtonState.Pressed;
    public bool IsMiddleMouseDown => _currentMouse.MiddleButton == ButtonState.Pressed;
    
    public bool IsLeftMousePressed => _currentMouse.LeftButton == ButtonState.Pressed && 
                                      _previousMouse.LeftButton == ButtonState.Released;
    public bool IsRightMousePressed => _currentMouse.RightButton == ButtonState.Pressed && 
                                       _previousMouse.RightButton == ButtonState.Released;
    public bool IsLeftMouseReleased => _currentMouse.LeftButton == ButtonState.Released && 
                                       _previousMouse.LeftButton == ButtonState.Pressed;
    
    /// <summary>
    /// Get movement direction from WASD/Arrow keys
    /// </summary>
    public (int dx, int dy) GetMovementDirection()
    {
        int dx = 0, dy = 0;
        
        foreach (var (key, offset) in MovementKeys)
        {
            if (IsKeyDown(key))
            {
                dx += offset.dx;
                dy += offset.dy;
            }
        }
        
        // Normalize diagonal movement
        if (dx != 0 && dy != 0)
        {
            // Keep full magnitude for 8-direction movement
        }
        
        return (Math.Sign(dx), Math.Sign(dy));
    }
    
    /// <summary>
    /// Check if any movement key is pressed
    /// </summary>
    public bool IsMoving => GetMovementDirection() != (0, 0);
    
    /// <summary>
    /// Handle text input for UI text fields
    /// </summary>
    public void ProcessTextInput(Keys[] pressedKeys)
    {
        if (!TextInputActive) return;
        
        foreach (var key in pressedKeys)
        {
            if (!IsKeyPressed(key)) continue;
            
            if (key == Keys.Back && TextBuffer.Length > 0)
            {
                TextBuffer = TextBuffer[..^1];
            }
            else if (key == Keys.Space)
            {
                if (TextBuffer.Length < MaxTextLength)
                    TextBuffer += ' ';
            }
            else if (key >= Keys.A && key <= Keys.Z)
            {
                if (TextBuffer.Length < MaxTextLength)
                {
                    char c = (char)('a' + (key - Keys.A));
                    if (IsShiftDown) c = char.ToUpper(c);
                    TextBuffer += c;
                }
            }
            else if (key >= Keys.D0 && key <= Keys.D9)
            {
                if (TextBuffer.Length < MaxTextLength)
                {
                    if (IsShiftDown)
                    {
                        TextBuffer += key switch
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
                            _ => (char)('0' + (key - Keys.D0))
                        };
                    }
                    else
                    {
                        TextBuffer += (char)('0' + (key - Keys.D0));
                    }
                }
            }
            else if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            {
                if (TextBuffer.Length < MaxTextLength)
                    TextBuffer += (char)('0' + (key - Keys.NumPad0));
            }
            else if (key == Keys.OemPeriod)
            {
                if (TextBuffer.Length < MaxTextLength)
                    TextBuffer += IsShiftDown ? '>' : '.';
            }
            else if (key == Keys.OemComma)
            {
                if (TextBuffer.Length < MaxTextLength)
                    TextBuffer += IsShiftDown ? '<' : ',';
            }
            else if (key == Keys.OemMinus)
            {
                if (TextBuffer.Length < MaxTextLength)
                    TextBuffer += IsShiftDown ? '_' : '-';
            }
        }
    }
    
    public void ClearTextBuffer() => TextBuffer = "";
    public void SetTextBuffer(string text) => TextBuffer = text ?? "";
    
    /// <summary>
    /// Get all currently pressed keys
    /// </summary>
    public Keys[] GetPressedKeys() => _currentKeyboard.GetPressedKeys();
}
