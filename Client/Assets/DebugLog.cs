namespace RealmOfReality.Client.Assets;

/// <summary>
/// Simple debug logger that writes to both console and file
/// </summary>
public static class DebugLog
{
    private static string? _logPath;
    private static readonly object _lock = new();
    
    /// <summary>
    /// Initialize the debug log file on user's desktop
    /// </summary>
    public static void Initialize()
    {
        try
        {
            _logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "uo_debug.log");
            File.WriteAllText(_logPath, $"=== UO Asset Debug Log ===\nTime: {DateTime.Now}\n\n");
        }
        catch
        {
            _logPath = null;
        }
    }
    
    /// <summary>
    /// Write a message to the debug log
    /// </summary>
    public static void Write(string message)
    {
        Console.WriteLine(message);
        
        if (_logPath == null) return;
        
        try
        {
            lock (_lock)
            {
                File.AppendAllText(_logPath, message + "\n");
            }
        }
        catch { }
    }
    
    /// <summary>
    /// Write a formatted message
    /// </summary>
    public static void Write(string format, params object[] args)
    {
        Write(string.Format(format, args));
    }
}
