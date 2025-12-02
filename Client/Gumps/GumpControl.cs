using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RealmOfReality.Shared.Gumps;

namespace RealmOfReality.Client.Gumps;

/// <summary>
/// Base class for all gump UI controls
/// </summary>
public abstract class GumpControl
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Page { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool AcceptsMouseInput { get; set; } = true;
    public bool AcceptsKeyboardInput { get; set; } = false;
    public bool IsFocused { get; set; } = false;
    
    public GumpControl? Parent { get; set; }
    public List<GumpControl> Children { get; } = new();
    
    /// <summary>
    /// Absolute screen position
    /// </summary>
    public int ScreenX => X + (Parent?.ScreenX ?? 0);
    public int ScreenY => Y + (Parent?.ScreenY ?? 0);
    
    /// <summary>
    /// Bounds in screen coordinates
    /// </summary>
    public Rectangle ScreenBounds => new Rectangle(ScreenX, ScreenY, Width, Height);
    
    public virtual void Update(GameTime gameTime) 
    {
        foreach (var child in Children)
        {
            child.Update(gameTime);
        }
    }
    
    public abstract void Draw(SpriteBatch spriteBatch, GumpRenderer renderer);
    
    public virtual void DrawChildren(SpriteBatch spriteBatch, GumpRenderer renderer)
    {
        foreach (var child in Children)
        {
            if (child.IsVisible)
            {
                child.Draw(spriteBatch, renderer);
            }
        }
    }
    
    /// <summary>
    /// Check if point is inside this control
    /// </summary>
    public virtual bool Contains(int x, int y)
    {
        return ScreenBounds.Contains(x, y);
    }
    
    /// <summary>
    /// Get the control at the given screen position
    /// </summary>
    public virtual GumpControl? GetControlAt(int x, int y)
    {
        if (!IsVisible || !Contains(x, y))
            return null;
        
        // Check children in reverse order (top to bottom)
        for (int i = Children.Count - 1; i >= 0; i--)
        {
            var child = Children[i];
            var result = child.GetControlAt(x, y);
            if (result != null)
                return result;
        }
        
        return AcceptsMouseInput ? this : null;
    }
    
    #region Mouse Events
    
    public virtual void OnMouseEnter() { }
    public virtual void OnMouseLeave() { }
    public virtual void OnMouseDown(int localX, int localY, MouseButton button) { }
    public virtual void OnMouseUp(int localX, int localY, MouseButton button) { }
    public virtual void OnMouseClick(int localX, int localY, MouseButton button) { }
    public virtual void OnMouseDoubleClick(int localX, int localY, MouseButton button) { }
    public virtual void OnMouseDrag(int deltaX, int deltaY) { }
    public virtual void OnMouseScroll(int delta) { }
    
    #endregion
    
    #region Keyboard Events
    
    public virtual void OnKeyDown(Keys key) { }
    public virtual void OnKeyUp(Keys key) { }
    public virtual void OnTextInput(char character) { }
    
    #endregion
    
    public void AddChild(GumpControl child)
    {
        child.Parent = this;
        Children.Add(child);
    }
}

public enum MouseButton
{
    Left,
    Right,
    Middle
}

/// <summary>
/// A complete gump window containing controls
/// </summary>
public class GumpWindow : GumpControl
{
    public uint TypeId { get; set; }
    public uint Serial { get; set; }
    public GumpFlags Flags { get; set; }
    public int CurrentPage { get; set; } = 1;
    
    public bool Closable => (Flags & GumpFlags.Closable) != 0;
    public bool Dragable => (Flags & GumpFlags.Dragable) != 0;
    public bool Modal => (Flags & GumpFlags.Modal) != 0;
    
    public bool IsDragging { get; private set; }
    private int _dragOffsetX, _dragOffsetY;
    
    public List<string> Texts { get; } = new();
    
    /// <summary>
    /// Event raised when user responds to the gump
    /// </summary>
    public event Action<GumpResponse>? OnResponse;
    
    /// <summary>
    /// List of currently checked switches
    /// </summary>
    private readonly HashSet<int> _checkedSwitches = new();
    
    /// <summary>
    /// Text entry values
    /// </summary>
    private readonly Dictionary<int, string> _textEntries = new();
    
    public GumpWindow(GumpData data)
    {
        TypeId = data.GumpTypeId;
        Serial = data.Serial;
        X = data.X;
        Y = data.Y;
        Flags = data.Flags;
        Texts.AddRange(data.Texts);
        
        // Set default size (will be calculated from children)
        Width = 100;
        Height = 100;
    }
    
    public override void Draw(SpriteBatch spriteBatch, GumpRenderer renderer)
    {
        // Draw children that match current page or page 0
        foreach (var child in Children)
        {
            if (child.IsVisible && (child.Page == 0 || child.Page == CurrentPage))
            {
                child.Draw(spriteBatch, renderer);
            }
        }
    }
    
    public override void OnMouseDown(int localX, int localY, MouseButton button)
    {
        if (button == MouseButton.Left && Dragable)
        {
            IsDragging = true;
            _dragOffsetX = localX;
            _dragOffsetY = localY;
        }
        else if (button == MouseButton.Right && Closable)
        {
            // Right-click to close
            Close(0);
        }
    }
    
    public override void OnMouseUp(int localX, int localY, MouseButton button)
    {
        if (button == MouseButton.Left)
        {
            IsDragging = false;
        }
    }
    
    public override void OnMouseDrag(int deltaX, int deltaY)
    {
        if (IsDragging)
        {
            X += deltaX;
            Y += deltaY;
        }
    }
    
    /// <summary>
    /// Set a switch state (checkbox/radio)
    /// </summary>
    public void SetSwitch(int switchId, bool state)
    {
        if (state)
            _checkedSwitches.Add(switchId);
        else
            _checkedSwitches.Remove(switchId);
    }
    
    /// <summary>
    /// Get switch state
    /// </summary>
    public bool IsSwitchOn(int switchId) => _checkedSwitches.Contains(switchId);
    
    /// <summary>
    /// Set text entry value
    /// </summary>
    public void SetTextEntry(int entryId, string text)
    {
        _textEntries[entryId] = text;
    }
    
    /// <summary>
    /// Get text entry value
    /// </summary>
    public string GetTextEntry(int entryId)
    {
        return _textEntries.TryGetValue(entryId, out var text) ? text : "";
    }
    
    /// <summary>
    /// Handle button click
    /// </summary>
    public void OnButtonClick(GumpButtonType type, int buttonId, int param)
    {
        if (type == GumpButtonType.Page)
        {
            // Page navigation
            CurrentPage = param;
        }
        else
        {
            // Reply to server
            Close(buttonId);
        }
    }
    
    /// <summary>
    /// Close the gump and send response
    /// </summary>
    public void Close(int buttonId)
    {
        var response = new GumpResponse
        {
            GumpTypeId = TypeId,
            Serial = Serial,
            ButtonId = buttonId
        };
        
        // Add checked switches
        response.Switches.AddRange(_checkedSwitches);
        
        // Add text entries
        foreach (var kvp in _textEntries)
        {
            response.TextEntries[kvp.Key] = kvp.Value;
        }
        
        OnResponse?.Invoke(response);
    }
    
    /// <summary>
    /// Get text from pool by index
    /// </summary>
    public string GetText(int index)
    {
        return index >= 0 && index < Texts.Count ? Texts[index] : "";
    }
}
