using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.ServiceProcess;
using System.Windows.Threading;
using Finalmouse.Shared;

namespace Finalmouse.ConfigUI.ViewModels;

public class GameEntry : INotifyPropertyChanged
{
    public string ProcessName { get; set; } = "";
    public string DisplayName { get; set; } = "";

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class MainViewModel : INotifyPropertyChanged
{
    private readonly DispatcherTimer _statusTimer;
    private AppConfig _config;

    // ── Polling Rates ──
    public int[] AvailableRates { get; } = [500, 1000, 2000, 4000, 8000];

    private int _idleRate;
    public int IdleRate
    {
        get => _idleRate;
        set { if (SetField(ref _idleRate, value)) SaveConfig(); }
    }

    private int _gamingRate;
    public int GamingRate
    {
        get => _gamingRate;
        set { if (SetField(ref _gamingRate, value)) SaveConfig(); }
    }

    // ── Scan Interval ──
    private int _scanInterval;
    public int ScanInterval
    {
        get => _scanInterval;
        set { if (SetField(ref _scanInterval, value)) SaveConfig(); }
    }

    // ── Tray & Startup Settings ──
    private bool _showTrayIcon;
    public bool ShowTrayIcon
    {
        get => _showTrayIcon;
        set
        {
            if (SetField(ref _showTrayIcon, value))
            {
                SaveConfig();
                OnPropertyChanged(nameof(ShowStartOnStartup));
            }
        }
    }

    private bool _startOnStartup;
    public bool StartOnStartup
    {
        get => _startOnStartup;
        set { if (SetField(ref _startOnStartup, value)) SaveConfig(); }
    }

    /// <summary>Only show "Start on startup" when tray icon is enabled.</summary>
    public bool ShowStartOnStartup => ShowTrayIcon;

    // ── Games ──
    public ObservableCollection<GameEntry> Games { get; } = new();

    private string _newProcessName = "";
    public string NewProcessName
    {
        get => _newProcessName;
        set => SetField(ref _newProcessName, value);
    }

    private string _newDisplayName = "";
    public string NewDisplayName
    {
        get => _newDisplayName;
        set => SetField(ref _newDisplayName, value);
    }

    // ── Service Status ──
    private bool _serviceInstalled;
    public bool ServiceInstalled
    {
        get => _serviceInstalled;
        set
        {
            SetField(ref _serviceInstalled, value);
            OnPropertyChanged(nameof(ServiceNotInstalled));
        }
    }

    public bool ServiceNotInstalled => !ServiceInstalled;

    private bool _serviceRunning;
    public bool ServiceRunning
    {
        get => _serviceRunning;
        set => SetField(ref _serviceRunning, value);
    }

    private string _serviceStatusText = "Checking...";
    public string ServiceStatusText
    {
        get => _serviceStatusText;
        set => SetField(ref _serviceStatusText, value);
    }

    private string _lastLogLine = "";
    public string LastLogLine
    {
        get => _lastLogLine;
        set => SetField(ref _lastLogLine, value);
    }

    private bool _gameDetected;
    public bool GameDetected
    {
        get => _gameDetected;
        set => SetField(ref _gameDetected, value);
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetField(ref _isBusy, value);
    }

    public MainViewModel()
    {
        _config = AppConfig.Load();
        _idleRate = _config.IdleRateHz;
        _gamingRate = _config.GamingRateHz;
        _scanInterval = _config.ScanIntervalSeconds;
        _showTrayIcon = _config.ShowTrayIcon;
        _startOnStartup = _config.StartOnStartup;

        RefreshGamesList();
        RefreshServiceStatus();

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _statusTimer.Tick += (_, _) => RefreshServiceStatus();
        _statusTimer.Start();
    }

    // ── Config Persistence ──

    private void SaveConfig()
    {
        _config.IdleRateHz = _idleRate;
        _config.GamingRateHz = _gamingRate;
        _config.ScanIntervalSeconds = _scanInterval;
        _config.ShowTrayIcon = _showTrayIcon;
        _config.StartOnStartup = _startOnStartup;

        _config.GameProcesses.Clear();
        foreach (var g in Games)
            _config.GameProcesses[g.ProcessName] = g.DisplayName;

        _config.Save();
    }

    // ── Game List Management ──

    public void RefreshGamesList()
    {
        Games.Clear();
        foreach (var (proc, name) in _config.GameProcesses)
            Games.Add(new GameEntry { ProcessName = proc, DisplayName = name });
    }

    public void AddGame()
    {
        var proc = NewProcessName.Trim();
        var name = NewDisplayName.Trim();
        if (string.IsNullOrEmpty(proc)) return;
        if (string.IsNullOrEmpty(name))
            name = proc.Replace(".exe", "", StringComparison.OrdinalIgnoreCase);

        // Prevent duplicates
        if (Games.Any(g => g.ProcessName.Equals(proc, StringComparison.OrdinalIgnoreCase)))
            return;

        Games.Add(new GameEntry { ProcessName = proc, DisplayName = name });
        NewProcessName = "";
        NewDisplayName = "";
        SaveConfig();
    }

    public void RemoveGame(GameEntry game)
    {
        Games.Remove(game);
        SaveConfig();
    }

    // ── Service Control ──

    public void RefreshServiceStatus()
    {
        ServiceInstalled = ServiceManager.IsInstalled();

        if (!ServiceInstalled)
        {
            ServiceRunning = false;
            ServiceStatusText = "NOT INSTALLED";
            LastLogLine = "";
            return;
        }

        var status = ServiceManager.GetStatus();
        ServiceRunning = status == ServiceControllerStatus.Running;

        ServiceStatusText = status switch
        {
            ServiceControllerStatus.Running => "RUNNING",
            ServiceControllerStatus.Stopped => "STOPPED",
            ServiceControllerStatus.StartPending => "STARTING...",
            ServiceControllerStatus.StopPending => "STOPPING...",
            _ => status?.ToString().ToUpper() ?? "UNKNOWN",
        };

        // Read last meaningful log line
        try
        {
            var logPath = AppConfig.GetLogPath();
            if (File.Exists(logPath))
            {
                var lines = File.ReadLines(logPath).Reverse().Take(10);
                var last = lines.FirstOrDefault(l => l.Contains("Hz") || l.Contains("game") || l.Contains("HID"));
                if (!string.IsNullOrEmpty(last))
                {
                    // Extract just the message part
                    var idx = last.IndexOf("] ");
                    LastLogLine = idx >= 0 ? last[(idx + 2)..] : last;

                    // Determine if a game is currently active:
                    // Look for the most recent rate-change log line
                    var lastRateLine = lines.FirstOrDefault(l => l.Contains("-> ") && l.Contains("Hz"));
                    if (lastRateLine != null)
                    {
                        // Game is active if the last switch was NOT "No game detected"
                        GameDetected = !lastRateLine.Contains("No game detected") && !lastRateLine.Contains("startup");
                    }
                }
            }
        }
        catch { }
    }

    public async Task<(bool success, string message)> InstallServiceAsync()
    {
        IsBusy = true;
        try
        {
            // Ensure config is saved before service starts reading it
            SaveConfig();

            var result = await Task.Run(() => ServiceManager.Install());
            if (result.success)
            {
                // Start the service immediately after install
                var startResult = await Task.Run(() => ServiceManager.Start());
                RefreshServiceStatus();
                return startResult.success
                    ? (true, "Service installed and started successfully.")
                    : (true, $"Service installed but failed to start: {startResult.message}");
            }
            RefreshServiceStatus();
            return result;
        }
        finally { IsBusy = false; }
    }

    public async Task<(bool success, string message)> UninstallServiceAsync()
    {
        IsBusy = true;
        try
        {
            var result = await Task.Run(() => ServiceManager.Uninstall());
            RefreshServiceStatus();
            return result;
        }
        finally { IsBusy = false; }
    }

    public async Task<(bool success, string message)> StartServiceAsync()
    {
        IsBusy = true;
        try
        {
            SaveConfig();
            var result = await Task.Run(() => ServiceManager.Start());
            RefreshServiceStatus();
            return result;
        }
        finally { IsBusy = false; }
    }

    public async Task<(bool success, string message)> StopServiceAsync()
    {
        IsBusy = true;
        try
        {
            var result = await Task.Run(() => ServiceManager.Stop());
            RefreshServiceStatus();
            return result;
        }
        finally { IsBusy = false; }
    }

    public async Task<(bool success, string message)> RestartServiceAsync()
    {
        IsBusy = true;
        try
        {
            SaveConfig();
            var result = await Task.Run(() => ServiceManager.Restart());
            RefreshServiceStatus();
            return result;
        }
        finally { IsBusy = false; }
    }

    // ── INotifyPropertyChanged ──

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
