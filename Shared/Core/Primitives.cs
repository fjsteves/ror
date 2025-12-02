using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace RealmOfReality.Shared.Core;

/// <summary>
/// 2D integer vector for tile coordinates
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct TilePosition : IEquatable<TilePosition>
{
    public int X { get; }
    public int Y { get; }
    
    [JsonConstructor]
    public TilePosition(int x, int y)
    {
        X = x;
        Y = y;
    }
    
    public static TilePosition Zero => new(0, 0);
    
    public static TilePosition operator +(TilePosition a, TilePosition b) => new(a.X + b.X, a.Y + b.Y);
    public static TilePosition operator -(TilePosition a, TilePosition b) => new(a.X - b.X, a.Y - b.Y);
    public static bool operator ==(TilePosition a, TilePosition b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(TilePosition a, TilePosition b) => !(a == b);
    
    public bool Equals(TilePosition other) => this == other;
    public override bool Equals(object? obj) => obj is TilePosition other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"({X}, {Y})";
    
    /// <summary>
    /// Manhattan distance to another tile
    /// </summary>
    public int DistanceTo(TilePosition other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
    
    /// <summary>
    /// Chebyshev distance (diagonal movement allowed)
    /// </summary>
    public int ChebyshevDistanceTo(TilePosition other) => Math.Max(Math.Abs(X - other.X), Math.Abs(Y - other.Y));
}

/// <summary>
/// 3D world position with float precision for smooth movement
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct WorldPosition : IEquatable<WorldPosition>
{
    public float X { get; }
    public float Y { get; }
    public float Z { get; } // Height/elevation for 2.5D
    
    [JsonConstructor]
    public WorldPosition(float x, float y, float z = 0)
    {
        X = x;
        Y = y;
        Z = z;
    }
    
    public static WorldPosition Zero => new(0, 0, 0);
    
    public static WorldPosition operator +(WorldPosition a, WorldPosition b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static WorldPosition operator -(WorldPosition a, WorldPosition b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static WorldPosition operator *(WorldPosition a, float scalar) => new(a.X * scalar, a.Y * scalar, a.Z * scalar);
    public static bool operator ==(WorldPosition a, WorldPosition b) => 
        Math.Abs(a.X - b.X) < 0.001f && Math.Abs(a.Y - b.Y) < 0.001f && Math.Abs(a.Z - b.Z) < 0.001f;
    public static bool operator !=(WorldPosition a, WorldPosition b) => !(a == b);
    
    public bool Equals(WorldPosition other) => this == other;
    public override bool Equals(object? obj) => obj is WorldPosition other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";
    
    /// <summary>
    /// Convert to tile coordinates (truncate to integer)
    /// </summary>
    public TilePosition ToTile() => new((int)Math.Floor(X), (int)Math.Floor(Y));
    
    /// <summary>
    /// Euclidean distance to another position (ignoring Z)
    /// </summary>
    public float DistanceTo(WorldPosition other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
    
    /// <summary>
    /// Linear interpolation between two positions
    /// </summary>
    public static WorldPosition Lerp(WorldPosition a, WorldPosition b, float t)
    {
        t = Math.Clamp(t, 0, 1);
        return new WorldPosition(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t,
            a.Z + (b.Z - a.Z) * t
        );
    }
}

/// <summary>
/// Screen coordinates for rendering
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct ScreenPosition
{
    public int X { get; }
    public int Y { get; }
    
    public ScreenPosition(int x, int y)
    {
        X = x;
        Y = y;
    }
    
    public static ScreenPosition Zero => new(0, 0);
}

/// <summary>
/// Rectangle for collision and rendering bounds
/// </summary>
public readonly struct Bounds
{
    public float X { get; }
    public float Y { get; }
    public float Width { get; }
    public float Height { get; }
    
    [JsonConstructor]
    public Bounds(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
    
    public float Left => X;
    public float Right => X + Width;
    public float Top => Y;
    public float Bottom => Y + Height;
    
    public bool Contains(WorldPosition pos) =>
        pos.X >= Left && pos.X < Right && pos.Y >= Top && pos.Y < Bottom;
    
    public bool Intersects(Bounds other) =>
        Left < other.Right && Right > other.Left && Top < other.Bottom && Bottom > other.Top;
}

/// <summary>
/// Direction enum for 8-directional movement (Ultima Online style)
/// </summary>
public enum Direction : byte
{
    North = 0,
    NorthEast = 1,
    East = 2,
    SouthEast = 3,
    South = 4,
    SouthWest = 5,
    West = 6,
    NorthWest = 7
}

public static class DirectionExtensions
{
    private static readonly (int dx, int dy)[] Offsets = 
    {
        (0, -1),  // North
        (1, -1),  // NorthEast
        (1, 0),   // East
        (1, 1),   // SouthEast
        (0, 1),   // South
        (-1, 1),  // SouthWest
        (-1, 0),  // West
        (-1, -1)  // NorthWest
    };
    
    public static (int dx, int dy) GetOffset(this Direction dir) => Offsets[(int)dir];
    
    public static Direction FromOffset(int dx, int dy) => (dx, dy) switch
    {
        (0, -1) => Direction.North,
        (1, -1) => Direction.NorthEast,
        (1, 0) => Direction.East,
        (1, 1) => Direction.SouthEast,
        (0, 1) => Direction.South,
        (-1, 1) => Direction.SouthWest,
        (-1, 0) => Direction.West,
        (-1, -1) => Direction.NorthWest,
        _ => Direction.South
    };
    
    public static TilePosition GetNeighbor(this TilePosition pos, Direction dir)
    {
        var (dx, dy) = dir.GetOffset();
        return new TilePosition(pos.X + dx, pos.Y + dy);
    }
    
    public static Direction Opposite(this Direction dir) => (Direction)(((int)dir + 4) % 8);
    
    /// <summary>
    /// Calculate direction from one position to another
    /// </summary>
    public static Direction FromVector(float dx, float dy)
    {
        if (dx == 0 && dy == 0) return Direction.South;
        
        var angle = MathF.Atan2(dy, dx) * (180f / MathF.PI);
        angle = (angle + 360 + 90) % 360; // Normalize to 0-360, with 0 = North
        
        return (Direction)(((int)Math.Round(angle / 45)) % 8);
    }
}

/// <summary>
/// RGBA Color structure
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Color
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }
    
    [JsonConstructor]
    public Color(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
    
    public static Color White => new(255, 255, 255);
    public static Color Black => new(0, 0, 0);
    public static Color Transparent => new(0, 0, 0, 0);
    public static Color Red => new(255, 0, 0);
    public static Color Green => new(0, 255, 0);
    public static Color LightGreen => new(144, 238, 144);
    public static Color Blue => new(0, 0, 255);
    public static Color Yellow => new(255, 255, 0);
    public static Color Gold => new(255, 215, 0);
    public static Color Orange => new(255, 165, 0);
    public static Color Cyan => new(0, 255, 255);
    public static Color Magenta => new(255, 0, 255);
    public static Color Gray => new(128, 128, 128);
    
    public uint ToArgb() => ((uint)A << 24) | ((uint)R << 16) | ((uint)G << 8) | B;
    
    public static Color FromArgb(uint argb) => new(
        (byte)((argb >> 16) & 0xFF),
        (byte)((argb >> 8) & 0xFF),
        (byte)(argb & 0xFF),
        (byte)((argb >> 24) & 0xFF)
    );
}
