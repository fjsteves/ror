namespace RealmOfReality.Shared.Core;

/// <summary>
/// Simple logging interface to avoid external dependencies
/// </summary>
public interface ILogger
{
    void Log(LogLevel level, string message);
    void Log(LogLevel level, Exception? exception, string message);
}

public interface ILogger<T> : ILogger { }

public enum LogLevel
{
    Debug,
    Information,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Console-based logger implementation
/// </summary>
public class ConsoleLogger : ILogger
{
    private readonly string _categoryName;
    private static readonly object _lock = new();
    
    public ConsoleLogger(string categoryName = "")
    {
        _categoryName = categoryName;
    }
    
    public void Log(LogLevel level, string message)
    {
        Log(level, null, message);
    }
    
    public void Log(LogLevel level, Exception? exception, string message)
    {
        var color = level switch
        {
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Information => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Critical => ConsoleColor.DarkRed,
            _ => ConsoleColor.White
        };
        
        var levelStr = level switch
        {
            LogLevel.Debug => "DBG",
            LogLevel.Information => "INF",
            LogLevel.Warning => "WRN",
            LogLevel.Error => "ERR",
            LogLevel.Critical => "CRT",
            _ => "???"
        };
        
        lock (_lock)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
            Console.ForegroundColor = color;
            Console.Write($"[{levelStr}] ");
            if (!string.IsNullOrEmpty(_categoryName))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"[{_categoryName}] ");
            }
            Console.ForegroundColor = originalColor;
            Console.WriteLine(message);
            
            if (exception != null)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"  Exception: {exception.Message}");
                if (exception.StackTrace != null)
                {
                    foreach (var line in exception.StackTrace.Split('\n').Take(5))
                    {
                        Console.WriteLine($"    {line.Trim()}");
                    }
                }
                Console.ForegroundColor = originalColor;
            }
        }
    }
}

public class ConsoleLogger<T> : ConsoleLogger, ILogger<T>
{
    public ConsoleLogger() : base(typeof(T).Name) { }
}

/// <summary>
/// Extension methods for logging
/// </summary>
public static class LoggerExtensions
{
    public static void LogDebug(this ILogger logger, string message) =>
        logger.Log(LogLevel.Debug, message);
    
    public static void LogInformation(this ILogger logger, string message) =>
        logger.Log(LogLevel.Information, message);
    
    public static void LogWarning(this ILogger logger, string message) =>
        logger.Log(LogLevel.Warning, message);
    
    public static void LogError(this ILogger logger, string message) =>
        logger.Log(LogLevel.Error, message);
    
    public static void LogError(this ILogger logger, Exception ex, string message) =>
        logger.Log(LogLevel.Error, ex, message);
    
    public static void LogCritical(this ILogger logger, string message) =>
        logger.Log(LogLevel.Critical, message);
    
    public static void LogCritical(this ILogger logger, Exception ex, string message) =>
        logger.Log(LogLevel.Critical, ex, message);
    
    // Format string versions - convert {Name} style to {0} style
    private static string ConvertNamedPlaceholders(string format, int argCount)
    {
        if (argCount == 0) return format;
        
        // Replace named placeholders {Name} with numbered ones {0}, {1}, etc.
        var result = new System.Text.StringBuilder();
        var index = 0;
        var i = 0;
        
        while (i < format.Length)
        {
            if (format[i] == '{')
            {
                var end = format.IndexOf('}', i);
                if (end > i)
                {
                    var placeholder = format.Substring(i + 1, end - i - 1);
                    // Only replace if it's not already a number
                    if (!int.TryParse(placeholder, out _) && index < argCount)
                    {
                        result.Append('{');
                        result.Append(index);
                        result.Append('}');
                        index++;
                        i = end + 1;
                        continue;
                    }
                }
            }
            result.Append(format[i]);
            i++;
        }
        
        return result.ToString();
    }
    
    public static void LogInformation(this ILogger logger, string format, params object?[] args)
    {
        if (args == null || args.Length == 0)
        {
            logger.Log(LogLevel.Information, format);
            return;
        }
        var converted = ConvertNamedPlaceholders(format, args.Length);
        logger.Log(LogLevel.Information, string.Format(converted, args));
    }
    
    public static void LogWarning(this ILogger logger, string format, params object?[] args)
    {
        if (args == null || args.Length == 0)
        {
            logger.Log(LogLevel.Warning, format);
            return;
        }
        var converted = ConvertNamedPlaceholders(format, args.Length);
        logger.Log(LogLevel.Warning, string.Format(converted, args));
    }
    
    public static void LogError(this ILogger logger, Exception ex, string format, params object?[] args)
    {
        if (args == null || args.Length == 0)
        {
            logger.Log(LogLevel.Error, ex, format);
            return;
        }
        var converted = ConvertNamedPlaceholders(format, args.Length);
        logger.Log(LogLevel.Error, ex, string.Format(converted, args));
    }
    
    public static void LogError(this ILogger logger, string format, params object?[] args)
    {
        if (args == null || args.Length == 0)
        {
            logger.Log(LogLevel.Error, format);
            return;
        }
        var converted = ConvertNamedPlaceholders(format, args.Length);
        logger.Log(LogLevel.Error, string.Format(converted, args));
    }
    
    public static void LogDebug(this ILogger logger, string format, params object?[] args)
    {
        if (args == null || args.Length == 0)
        {
            logger.Log(LogLevel.Debug, format);
            return;
        }
        var converted = ConvertNamedPlaceholders(format, args.Length);
        logger.Log(LogLevel.Debug, string.Format(converted, args));
    }
}
