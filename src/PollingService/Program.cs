using System.Runtime.InteropServices;
using Finalmouse.PollingService;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

[DllImport("shell32.dll", SetLastError = true)]
static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appId);
SetCurrentProcessExplicitAppUserModelID("Finalmouse.PollingRateSwitcher");

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "FinalmousePollingService";
});

builder.Services.AddHostedService<PollingWorker>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddEventLog(settings =>
{
    settings.SourceName = "FinalmousePollingService";
});

// Also log to file
var logPath = Finalmouse.Shared.AppConfig.GetLogPath();
builder.Logging.AddProvider(new FileLoggerProvider(logPath));

var host = builder.Build();
host.Run();
