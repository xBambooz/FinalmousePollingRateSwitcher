using System.Diagnostics;
using System.IO;
using System.ServiceProcess;

namespace Finalmouse.ConfigUI;

/// <summary>
/// Manages the FinalmousePollingRateSwitcher Windows Service lifecycle.
/// Uses sc.exe for install/uninstall and ServiceController for start/stop/status.
/// </summary>
public static class ServiceManager
{
    public const string ServiceName = "FinalmousePollingRateSwitcher";
    public const string DisplayName = "Finalmouse Polling Rate Switcher";
    public const string Description = "Automatically switches Finalmouse ULX polling rate between idle and gaming modes based on running processes.";

    /// <summary>
    /// Returns the expected path to the service exe, which lives next to the config UI exe.
    /// </summary>
    public static string GetServiceExePath()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(dir, "FinalmousePollingService.exe");
    }

    public static bool IsInstalled()
    {
        try
        {
            using var sc = new ServiceController(ServiceName);
            _ = sc.Status; // Will throw if not installed
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static ServiceControllerStatus? GetStatus()
    {
        try
        {
            using var sc = new ServiceController(ServiceName);
            return sc.Status;
        }
        catch
        {
            return null;
        }
    }

    public static bool IsRunning()
    {
        var status = GetStatus();
        return status == ServiceControllerStatus.Running ||
               status == ServiceControllerStatus.StartPending;
    }

    public static (bool success, string message) Install()
    {
        var exePath = GetServiceExePath();
        if (!File.Exists(exePath))
            return (false, $"Service executable not found at:\n{exePath}\n\nMake sure FinalmousePollingService.exe is in the same folder as this config tool.");

        if (IsInstalled())
            return (true, "Service is already installed.");

        // sc create
        var result = RunSc($"create {ServiceName} binPath= \"\\\"{exePath}\\\"\" start= auto DisplayName= \"{DisplayName}\"");
        if (!result.success)
            return result;

        // Set description
        RunSc($"description {ServiceName} \"{Description}\"");

        // Configure recovery: restart on failure
        RunSc($"failure {ServiceName} reset= 86400 actions= restart/5000/restart/10000/restart/30000");

        return (true, "Service installed successfully.");
    }

    public static (bool success, string message) Uninstall()
    {
        if (!IsInstalled())
            return (true, "Service is not installed.");

        // Stop first if running
        if (IsRunning())
        {
            var stopResult = Stop();
            if (!stopResult.success)
                return stopResult;
        }

        return RunSc($"delete {ServiceName}");
    }

    public static (bool success, string message) Start()
    {
        if (!IsInstalled())
            return (false, "Service is not installed. Install it first.");

        try
        {
            using var sc = new ServiceController(ServiceName);
            if (sc.Status == ServiceControllerStatus.Running)
                return (true, "Service is already running.");

            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
            return (true, "Service started.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to start service: {ex.Message}");
        }
    }

    public static (bool success, string message) Stop()
    {
        if (!IsInstalled())
            return (true, "Service is not installed.");

        try
        {
            using var sc = new ServiceController(ServiceName);
            if (sc.Status == ServiceControllerStatus.Stopped)
                return (true, "Service is already stopped.");

            sc.Stop();
            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15));
            return (true, "Service stopped.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to stop service: {ex.Message}");
        }
    }

    public static (bool success, string message) Restart()
    {
        var stop = Stop();
        if (!stop.success && IsRunning())
            return stop;

        return Start();
    }

    private static (bool success, string message) RunSc(string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var proc = Process.Start(psi)!;
            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit(15000);

            if (proc.ExitCode == 0)
                return (true, stdout.Trim());
            else
                return (false, $"sc.exe failed (exit {proc.ExitCode}):\n{stdout}\n{stderr}".Trim());
        }
        catch (Exception ex)
        {
            return (false, $"Failed to run sc.exe: {ex.Message}");
        }
    }
}
