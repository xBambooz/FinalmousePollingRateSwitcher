using Finalmouse.PollingService;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "FinalmousePollingRateSwitcher";
});

builder.Services.AddHostedService<PollingWorker>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddEventLog(settings =>
{
    settings.SourceName = "FinalmousePollingRateSwitcher";
});

// Also log to file
var logPath = Finalmouse.Shared.AppConfig.GetLogPath();
builder.Logging.AddProvider(new FileLoggerProvider(logPath));

var host = builder.Build();
host.Run();
