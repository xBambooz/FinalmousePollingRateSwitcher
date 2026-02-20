using System.Diagnostics;
using Finalmouse.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Finalmouse.PollingService;

public class PollingWorker : BackgroundService
{
    private readonly ILogger<PollingWorker> _logger;
    private readonly FinalmouseHid _mouse = new();
    private readonly ServiceStatus _status = new();
    private int? _currentRate;
    private DateTime _lastConfigLoad = DateTime.MinValue;
    private AppConfig _config = new();

    public PollingWorker(ILogger<PollingWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Finalmouse Polling Rate Switcher service starting");

        ReloadConfig();

        if (!_mouse.Open())
        {
            _logger.LogError("Could not open HID connection to Finalmouse. Is XPanel closed?");
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(5000, stoppingToken);
                if (_mouse.Open())
                {
                    _logger.LogInformation("HID connection opened");
                    break;
                }
            }
        }
        else
        {
            _logger.LogInformation("HID connection opened");
        }

        SetRate(_config.IdleRateHz, "startup", isGaming: false, gameName: "");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if ((DateTime.UtcNow - _lastConfigLoad).TotalSeconds > 30)
                    ReloadConfig();

                var (gameRunning, gameName) = CheckForGames();

                if (gameRunning)
                    SetRate(_config.GamingRateHz, gameName, isGaming: true, gameName);
                else
                    SetRate(_config.IdleRateHz, "No game detected", isGaming: false, "");

                await Task.Delay(_config.ScanIntervalSeconds * 1000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in polling loop");
                await Task.Delay(5000, stoppingToken);
            }
        }

        _logger.LogInformation("Service stopping, restoring idle rate");
        _mouse.SetPollingRate(_config.IdleRateHz);
        _mouse.Dispose();

        // Clear status file on shutdown
        try { File.Delete(ServiceStatus.GetStatusPath()); } catch { }
    }

    private void ReloadConfig()
    {
        try
        {
            _config = AppConfig.Load();
            _lastConfigLoad = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to reload config, keeping current settings");
        }
    }

    private void SetRate(int hz, string reason, bool isGaming, string gameName)
    {
        // Always update status file (keeps timestamp fresh for tray icon)
        _status.IsGaming = isGaming;
        _status.CurrentRateHz = hz;
        _status.GameName = gameName;
        _status.Reason = reason;
        _status.Write();

        if (_currentRate == hz)
            return;

        _logger.LogInformation("-> {Hz}Hz ({Reason})", hz, reason);

        if (_mouse.SetPollingRate(hz))
        {
            _currentRate = hz;
            _logger.LogInformation("Set {Hz}Hz OK", hz);
        }
        else
        {
            _logger.LogWarning("Failed to set {Hz}Hz, attempting reconnect", hz);
            if (_mouse.Open() && _mouse.SetPollingRate(hz))
            {
                _currentRate = hz;
                _logger.LogInformation("Reconnected and set {Hz}Hz OK", hz);
            }
            else
            {
                _logger.LogError("Could not set polling rate. Mouse disconnected?");
            }
        }
    }

    private (bool running, string name) CheckForGames()
    {
        try
        {
            var processNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    processNames.Add(proc.ProcessName + ".exe");
                }
                catch { }
            }

            foreach (var (exe, name) in _config.GameProcesses)
            {
                if (processNames.Contains(exe))
                    return (true, name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error scanning processes");
        }

        return (false, "");
    }
}
