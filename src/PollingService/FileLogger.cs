using Microsoft.Extensions.Logging;

namespace Finalmouse.PollingService;

public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _path;
    private readonly object _lock = new();

    public FileLoggerProvider(string path)
    {
        _path = path;
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(_path, _lock, categoryName);
    public void Dispose() { }
}

public class FileLogger : ILogger
{
    private readonly string _path;
    private readonly object _lock;
    private readonly string _category;

    public FileLogger(string path, object @lock, string category)
    {
        _path = path;
        _lock = @lock;
        _category = category;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var level = logLevel switch
        {
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRIT",
            _ => "INFO"
        };

        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss,fff} [{level}] {formatter(state, exception)}";
        if (exception != null)
            line += $"\n{exception}";

        lock (_lock)
        {
            try { File.AppendAllText(_path, line + Environment.NewLine); }
            catch { /* Don't crash the service over logging */ }
        }
    }
}
