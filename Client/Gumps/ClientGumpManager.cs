using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RealmOfReality.Client.Engine;
using RealmOfReality.Shared.Gumps;

namespace RealmOfReality.Client.Gumps;

/// <summary>
/// Client-side manager for active gumps
/// </summary>
public class ClientGumpManager
{
    private readonly GumpRenderer _renderer;
    private readonly List<GumpWindow> _gumps = new();
    
    private GumpControl? _hoveredControl;
    private GumpControl? _focusedControl;
    private GumpControl? _draggedControl;
    private int _lastMouseX, _lastMouseY;
    private bool _wasLeftPressed, _wasRightPressed;
    
    /// <summary>
    /// Event raised when a gump response should be sent to server
    /// </summary>
    public event Action<GumpResponse>? OnGumpResponse;
    
    public ClientGumpManager(GumpRenderer renderer)
    {
        _renderer = renderer;
    }
    
    /// <summary>
    /// Open a gump from server data
    /// </summary>
    public GumpWindow OpenGump(GumpData data)
    {
        // Close any existing gump of same type
        CloseGump(data.GumpTypeId, data.Serial, sendResponse: false);
        
        // Build the gump window
        var window = BuildGump(data);
        
        // Subscribe to response event
        window.OnResponse += response =>
        {
            _gumps.Remove(window);
            OnGumpResponse?.Invoke(response);
        };
        
        _gumps.Add(window);
        
        // Bring to front
        BringToFront(window);
        
        return window;
    }
    
    /// <summary>
    /// Close a gump
    /// </summary>
    public void CloseGump(uint typeId, uint serial, bool sendResponse = true)
    {
        var gump = _gumps.FirstOrDefault(g => g.TypeId == typeId && g.Serial == serial);
        if (gump != null)
        {
            _gumps.Remove(gump);
            if (sendResponse)
            {
                gump.Close(0); // Close without button
            }
        }
    }
    
    /// <summary>
    /// Close all gumps of a type
    /// </summary>
    public void CloseGumpsOfType(uint typeId)
    {
        var toClose = _gumps.Where(g => g.TypeId == typeId).ToList();
        foreach (var gump in toClose)
        {
            _gumps.Remove(gump);
        }
    }
    
    /// <summary>
    /// Update all gumps
    /// </summary>
    public void Update(GameTime gameTime, InputManager input)
    {
        var mouseState = Mouse.GetState();
        int mouseX = mouseState.X;
        int mouseY = mouseState.Y;
        bool leftPressed = mouseState.LeftButton == ButtonState.Pressed;
        bool rightPressed = mouseState.RightButton == ButtonState.Pressed;
        
        // Find control under mouse
        GumpControl? controlUnderMouse = null;
        GumpWindow? windowUnderMouse = null;
        
        // Check gumps in reverse order (top to bottom)
        for (int i = _gumps.Count - 1; i >= 0; i--)
        {
            var gump = _gumps[i];
            var control = gump.GetControlAt(mouseX, mouseY);
            if (control != null)
            {
                controlUnderMouse = control;
                windowUnderMouse = gump;
                break;
            }
        }
        
        // Handle hover state changes
        if (controlUnderMouse != _hoveredControl)
        {
            _hoveredControl?.OnMouseLeave();
            _hoveredControl = controlUnderMouse;
            _hoveredControl?.OnMouseEnter();
        }
        
        // Handle mouse button events
        if (leftPressed && !_wasLeftPressed)
        {
            // Mouse down
            if (controlUnderMouse != null)
            {
                int localX = mouseX - controlUnderMouse.ScreenX;
                int localY = mouseY - controlUnderMouse.ScreenY;
                controlUnderMouse.OnMouseDown(localX, localY, MouseButton.Left);
                
                // Update focus
                if (controlUnderMouse.AcceptsKeyboardInput)
                {
                    if (_focusedControl != controlUnderMouse)
                    {
                        if (_focusedControl != null) _focusedControl.IsFocused = false;
                        _focusedControl = controlUnderMouse;
                        _focusedControl.IsFocused = true;
                    }
                }
                
                // Start drag
                _draggedControl = controlUnderMouse;
                
                // Bring window to front
                if (windowUnderMouse != null)
                {
                    BringToFront(windowUnderMouse);
                }
            }
            else
            {
                // Click outside - clear focus
                if (_focusedControl != null)
                {
                    _focusedControl.IsFocused = false;
                    _focusedControl = null;
                }
            }
        }
        else if (!leftPressed && _wasLeftPressed)
        {
            // Mouse up
            if (_draggedControl != null)
            {
                int localX = mouseX - _draggedControl.ScreenX;
                int localY = mouseY - _draggedControl.ScreenY;
                _draggedControl.OnMouseUp(localX, localY, MouseButton.Left);
                
                // Check for click (released on same control)
                if (_draggedControl == controlUnderMouse)
                {
                    _draggedControl.OnMouseClick(localX, localY, MouseButton.Left);
                }
                
                _draggedControl = null;
            }
        }
        
        if (rightPressed && !_wasRightPressed)
        {
            // Right mouse down
            if (controlUnderMouse != null)
            {
                int localX = mouseX - controlUnderMouse.ScreenX;
                int localY = mouseY - controlUnderMouse.ScreenY;
                controlUnderMouse.OnMouseDown(localX, localY, MouseButton.Right);
            }
        }
        else if (!rightPressed && _wasRightPressed)
        {
            // Right mouse up
            if (controlUnderMouse != null)
            {
                int localX = mouseX - controlUnderMouse.ScreenX;
                int localY = mouseY - controlUnderMouse.ScreenY;
                controlUnderMouse.OnMouseUp(localX, localY, MouseButton.Right);
                controlUnderMouse.OnMouseClick(localX, localY, MouseButton.Right);
            }
        }
        
        // Handle dragging
        if (_draggedControl != null && leftPressed)
        {
            int deltaX = mouseX - _lastMouseX;
            int deltaY = mouseY - _lastMouseY;
            if (deltaX != 0 || deltaY != 0)
            {
                _draggedControl.OnMouseDrag(deltaX, deltaY);
            }
        }
        
        // Handle scroll wheel
        int scrollDelta = mouseState.ScrollWheelValue - _lastScrollValue;
        if (scrollDelta != 0 && controlUnderMouse != null)
        {
            controlUnderMouse.OnMouseScroll(scrollDelta);
        }
        _lastScrollValue = mouseState.ScrollWheelValue;
        
        // Handle keyboard input
        if (_focusedControl != null)
        {
            foreach (var key in input.GetPressedKeys())
            {
                if (input.IsKeyPressed(key))
                {
                    _focusedControl.OnKeyDown(key);
                }
            }
        }
        
        // Handle text input (from TextInput event if available)
        // For simplicity, we'll check for character keys
        if (_focusedControl != null)
        {
            var keys = input.GetPressedKeys();
            foreach (var key in keys)
            {
                if (input.IsKeyPressed(key))
                {
                    char? c = KeyToChar(key, input.IsShiftDown);
                    if (c.HasValue)
                    {
                        _focusedControl.OnTextInput(c.Value);
                    }
                }
            }
        }
        
        // Update gumps
        foreach (var gump in _gumps)
        {
            gump.Update(gameTime);
        }
        
        _lastMouseX = mouseX;
        _lastMouseY = mouseY;
        _wasLeftPressed = leftPressed;
        _wasRightPressed = rightPressed;
    }
    
    private int _lastScrollValue;
    
    /// <summary>
    /// Draw all gumps
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var gump in _gumps)
        {
            gump.Draw(spriteBatch, _renderer);
        }
    }
    
    /// <summary>
    /// Check if any gump is under the mouse
    /// </summary>
    public bool IsMouseOverGump(int mouseX, int mouseY)
    {
        for (int i = _gumps.Count - 1; i >= 0; i--)
        {
            if (_gumps[i].Contains(mouseX, mouseY))
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Check if any modal gump is open
    /// </summary>
    public bool HasModalGump => _gumps.Any(g => g.Modal);
    
    /// <summary>
    /// Bring a gump to the front
    /// </summary>
    private void BringToFront(GumpWindow gump)
    {
        if (_gumps.Remove(gump))
        {
            _gumps.Add(gump);
        }
    }
    
    /// <summary>
    /// Build a GumpWindow from GumpData
    /// </summary>
    private GumpWindow BuildGump(GumpData data)
    {
        var window = new GumpWindow(data);
        
        // Calculate bounds from elements
        int maxX = 0, maxY = 0;
        
        foreach (var element in data.Elements)
        {
            var control = BuildControl(element, window);
            if (control != null)
            {
                window.AddChild(control);
                
                // Try to get actual size from gump texture for images
                if (control is GumpImageControl img)
                {
                    var (w, h) = _renderer.GetGumpSize(img.GumpId);
                    if (w > 0 && h > 0)
                    {
                        control.Width = w;
                        control.Height = h;
                    }
                }
                else if (control is GumpButtonControl btn)
                {
                    var (w, h) = _renderer.GetGumpSize(btn.NormalId);
                    if (w > 0 && h > 0)
                    {
                        control.Width = w;
                        control.Height = h;
                    }
                }
                
                maxX = Math.Max(maxX, control.X + control.Width);
                maxY = Math.Max(maxY, control.Y + control.Height);
            }
        }
        
        window.Width = maxX;
        window.Height = maxY;
        
        return window;
    }
    
    /// <summary>
    /// Build a control from a gump element
    /// </summary>
    private GumpControl? BuildControl(GumpElement element, GumpWindow window)
    {
        GumpControl? control = element switch
        {
            GumpBackground bg => new GumpBackgroundControl
            {
                X = bg.X,
                Y = bg.Y,
                Width = bg.Width,
                Height = bg.Height,
                Page = bg.Page,
                GumpId = bg.GumpId
            },
            
            GumpImage img => new GumpImageControl
            {
                X = img.X,
                Y = img.Y,
                Width = 44, // Will be set from texture
                Height = 44,
                Page = img.Page,
                GumpId = img.GumpId,
                Hue = img.Hue
            },
            
            GumpImageTiled tiled => new GumpImageTiledControl
            {
                X = tiled.X,
                Y = tiled.Y,
                Width = tiled.Width,
                Height = tiled.Height,
                Page = tiled.Page,
                GumpId = tiled.GumpId
            },
            
            GumpLabel label => new GumpLabelControl
            {
                X = label.X,
                Y = label.Y,
                Width = 200,
                Height = 20,
                Page = label.Page,
                Text = window.GetText(label.TextIndex),
                Hue = label.Hue,
                Cropped = false
            },
            
            GumpLabelCropped cropped => new GumpLabelControl
            {
                X = cropped.X,
                Y = cropped.Y,
                Width = cropped.Width,
                Height = cropped.Height,
                Page = cropped.Page,
                Text = window.GetText(cropped.TextIndex),
                Hue = cropped.Hue,
                Cropped = true
            },
            
            GumpHtml html => new GumpHtmlControl
            {
                X = html.X,
                Y = html.Y,
                Width = html.Width,
                Height = html.Height,
                Page = html.Page,
                Html = window.GetText(html.TextIndex),
                HasBackground = html.HasBackground,
                HasScrollbar = html.HasScrollbar
            },
            
            GumpButton btn => new GumpButtonControl
            {
                X = btn.X,
                Y = btn.Y,
                Width = 30, // Default button size for fallback
                Height = 25,
                Page = btn.Page,
                NormalId = btn.NormalId,
                PressedId = btn.PressedId,
                ButtonType = btn.ButtonType,
                Param = btn.Param,
                ButtonId = btn.ButtonId
            },
            
            GumpTextEntry entry => BuildTextEntry(entry, window),
            
            GumpCheckbox cb => new GumpCheckboxControl
            {
                X = cb.X,
                Y = cb.Y,
                Width = 20,
                Height = 20,
                Page = cb.Page,
                UncheckedId = cb.UncheckedId,
                CheckedId = cb.CheckedId,
                SwitchId = cb.SwitchId,
                IsChecked = cb.InitialState
            },
            
            GumpRadio radio => new GumpRadioControl
            {
                X = radio.X,
                Y = radio.Y,
                Width = 20,
                Height = 20,
                Page = radio.Page,
                UncheckedId = radio.UncheckedId,
                CheckedId = radio.CheckedId,
                SwitchId = radio.SwitchId,
                GroupId = radio.GroupId,
                IsSelected = radio.InitialState
            },
            
            GumpItem item => new GumpItemControl
            {
                X = item.X,
                Y = item.Y,
                Width = 44,
                Height = 44,
                Page = item.Page,
                ItemId = item.ItemId,
                Hue = item.Hue
            },
            
            GumpAlphaRegion alpha => new GumpAlphaRegionControl
            {
                X = alpha.X,
                Y = alpha.Y,
                Width = alpha.Width,
                Height = alpha.Height,
                Page = alpha.Page
            },
            
            GumpPaperdollBody body => BuildPaperdollBody(body),
            
            _ => null
        };
        
        // Set initial switch states
        if (control is GumpCheckboxControl checkbox && checkbox.IsChecked)
        {
            window.SetSwitch(checkbox.SwitchId, true);
        }
        if (control is GumpRadioControl radioCtrl && radioCtrl.IsSelected)
        {
            window.SetSwitch(radioCtrl.SwitchId, true);
        }
        
        return control;
    }
    
    private GumpPaperdollBodyControl BuildPaperdollBody(GumpPaperdollBody body)
    {
        var control = new GumpPaperdollBodyControl
        {
            X = body.X,
            Y = body.Y,
            Page = body.Page,
            BodyId = body.BodyId,
            Gender = body.Gender,
            SkinHue = body.SkinHue,
            HairStyle = body.HairStyle,
            HairHue = body.HairHue,
            BeardStyle = body.BeardStyle,
            BeardHue = body.BeardHue
        };
        
        foreach (var layer in body.Layers)
        {
            control.Layers.Add((layer.Layer, layer.Serial, layer.ItemId, layer.GumpId, layer.Hue));
        }
        
        return control;
    }
    
    private GumpTextEntryControl BuildTextEntry(GumpTextEntry entry, GumpWindow window)
    {
        var control = new GumpTextEntryControl
        {
            X = entry.X,
            Y = entry.Y,
            Width = entry.Width,
            Height = entry.Height,
            Page = entry.Page,
            EntryId = entry.EntryId,
            Text = window.GetText(entry.InitialTextIndex),
            MaxLength = entry.MaxLength,
            Hue = entry.Hue
        };
        
        // Set initial text in window
        window.SetTextEntry(entry.EntryId, control.Text);
        
        return control;
    }
    
    /// <summary>
    /// Convert key to character (simple implementation)
    /// </summary>
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
                char[] shifted = { ')', '!', '@', '#', '$', '%', '^', '&', '*', '(' };
                return shifted[key - Keys.D0];
            }
            return (char)('0' + (key - Keys.D0));
        }
        
        // Numpad
        if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
        {
            return (char)('0' + (key - Keys.NumPad0));
        }
        
        // Special characters
        return key switch
        {
            Keys.Space => ' ',
            Keys.OemPeriod => shift ? '>' : '.',
            Keys.OemComma => shift ? '<' : ',',
            Keys.OemMinus => shift ? '_' : '-',
            Keys.OemPlus => shift ? '+' : '=',
            Keys.OemQuestion => shift ? '?' : '/',
            Keys.OemSemicolon => shift ? ':' : ';',
            Keys.OemQuotes => shift ? '"' : '\'',
            Keys.OemOpenBrackets => shift ? '{' : '[',
            Keys.OemCloseBrackets => shift ? '}' : ']',
            Keys.OemPipe => shift ? '|' : '\\',
            Keys.OemTilde => shift ? '~' : '`',
            _ => null
        };
    }
}
