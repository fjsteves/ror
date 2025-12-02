using System.Diagnostics;

namespace RealmOfReality.Shared.Core;

/// <summary>
/// Game time management - handles ticks, server time synchronization, and delta time
/// </summary>
public sealed class GameTime
{
    public const int TicksPerSecond = 20; // 50ms tick rate, good for MMO
    public const double SecondsPerTick = 1.0 / TicksPerSecond;
    public const long MillisecondsPerTick = 1000 / TicksPerSecond;
    
    private readonly Stopwatch _stopwatch = new();
    private long _tickCount;
    private long _lastTickTime;
    
    /// <summary>
    /// Current server tick number (monotonically increasing)
    /// </summary>
    public long TickCount => _tickCount;
    
    /// <summary>
    /// Total elapsed time since game start
    /// </summary>
    public TimeSpan TotalTime => _stopwatch.Elapsed;
    
    /// <summary>
    /// Total elapsed milliseconds
    /// </summary>
    public long TotalMilliseconds => _stopwatch.ElapsedMilliseconds;
    
    /// <summary>
    /// Delta time since last update (in seconds)
    /// </summary>
    public double DeltaTime { get; private set; }
    
    /// <summary>
    /// Start the game time
    /// </summary>
    public void Start()
    {
        _stopwatch.Start();
        _lastTickTime = _stopwatch.ElapsedMilliseconds;
    }
    
    /// <summary>
    /// Update game time (call once per frame/tick)
    /// </summary>
    public void Update()
    {
        var currentTime = _stopwatch.ElapsedMilliseconds;
        DeltaTime = (currentTime - _lastTickTime) / 1000.0;
        _lastTickTime = currentTime;
    }
    
    /// <summary>
    /// Advance tick counter (call once per server tick)
    /// </summary>
    public void Tick()
    {
        _tickCount++;
    }
    
    /// <summary>
    /// Check if enough time has passed for the next tick
    /// </summary>
    public bool ShouldTick()
    {
        var expectedTicks = _stopwatch.ElapsedMilliseconds / MillisecondsPerTick;
        return expectedTicks > _tickCount;
    }
    
    /// <summary>
    /// Get milliseconds until next tick
    /// </summary>
    public int MillisecondsUntilNextTick()
    {
        var nextTickTime = (_tickCount + 1) * MillisecondsPerTick;
        return (int)Math.Max(0, nextTickTime - _stopwatch.ElapsedMilliseconds);
    }
    
    /// <summary>
    /// Convert tick count to time span
    /// </summary>
    public static TimeSpan TicksToTime(long ticks) => 
        TimeSpan.FromMilliseconds(ticks * MillisecondsPerTick);
    
    /// <summary>
    /// Convert time span to tick count
    /// </summary>
    public static long TimeToTicks(TimeSpan time) => 
        (long)(time.TotalMilliseconds / MillisecondsPerTick);
}

/// <summary>
/// Server timestamp for synchronization
/// </summary>
public readonly struct ServerTime : IEquatable<ServerTime>, IComparable<ServerTime>
{
    private readonly long _ticks;
    
    public ServerTime(long ticks) => _ticks = ticks;
    
    public long Ticks => _ticks;
    
    public static ServerTime Zero => new(0);
    
    public ServerTime AddTicks(long ticks) => new(_ticks + ticks);
    public ServerTime AddSeconds(double seconds) => new(_ticks + (long)(seconds * GameTime.TicksPerSecond));
    
    public bool Equals(ServerTime other) => _ticks == other._ticks;
    public override bool Equals(object? obj) => obj is ServerTime other && Equals(other);
    public override int GetHashCode() => _ticks.GetHashCode();
    public int CompareTo(ServerTime other) => _ticks.CompareTo(other._ticks);
    
    public static bool operator ==(ServerTime a, ServerTime b) => a._ticks == b._ticks;
    public static bool operator !=(ServerTime a, ServerTime b) => a._ticks != b._ticks;
    public static bool operator <(ServerTime a, ServerTime b) => a._ticks < b._ticks;
    public static bool operator >(ServerTime a, ServerTime b) => a._ticks > b._ticks;
    public static bool operator <=(ServerTime a, ServerTime b) => a._ticks <= b._ticks;
    public static bool operator >=(ServerTime a, ServerTime b) => a._ticks >= b._ticks;
    
    public static ServerTime operator +(ServerTime a, long ticks) => new(a._ticks + ticks);
    public static ServerTime operator -(ServerTime a, long ticks) => new(a._ticks - ticks);
    public static long operator -(ServerTime a, ServerTime b) => a._ticks - b._ticks;
    
    public override string ToString() => $"T:{_ticks}";
}

/// <summary>
/// In-game day/night cycle
/// </summary>
public sealed class WorldClock
{
    public const int MinutesPerGameDay = 120; // 2 real hours = 1 game day
    public const int TicksPerGameMinute = GameTime.TicksPerSecond * 60 / MinutesPerGameDay * 24;
    
    /// <summary>
    /// Current game hour (0-23)
    /// </summary>
    public int Hour { get; private set; }
    
    /// <summary>
    /// Current game minute (0-59)
    /// </summary>
    public int Minute { get; private set; }
    
    /// <summary>
    /// Current game day (starts at 1)
    /// </summary>
    public int Day { get; private set; } = 1;
    
    /// <summary>
    /// Is it currently night time? (between 20:00 and 06:00)
    /// </summary>
    public bool IsNight => Hour >= 20 || Hour < 6;
    
    /// <summary>
    /// Light level (0.0 = midnight darkness, 1.0 = noon brightness)
    /// </summary>
    public float LightLevel
    {
        get
        {
            // Smooth light transition
            var hour = Hour + Minute / 60.0f;
            if (hour >= 6 && hour < 12)
                return 0.3f + 0.7f * (hour - 6) / 6; // Dawn to noon
            if (hour >= 12 && hour < 18)
                return 1.0f; // Midday
            if (hour >= 18 && hour < 21)
                return 1.0f - 0.7f * (hour - 18) / 3; // Dusk
            // Night
            return 0.3f;
        }
    }
    
    public void Update(long serverTicks)
    {
        var totalMinutes = (serverTicks / TicksPerGameMinute) % (24 * 60);
        Hour = (int)(totalMinutes / 60);
        Minute = (int)(totalMinutes % 60);
        Day = (int)(serverTicks / (TicksPerGameMinute * 24 * 60)) + 1;
    }
    
    public override string ToString() => $"Day {Day}, {Hour:D2}:{Minute:D2}";
}
