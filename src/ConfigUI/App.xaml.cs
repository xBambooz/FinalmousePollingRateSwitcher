using System.Windows;

namespace Finalmouse.ConfigUI;

public partial class App : Application
{
    public static bool StartSilent { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // OnExplicitShutdown so hiding the window (minimize to tray) doesn't kill the app.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Check for --silent flag (used by startup scheduled task)
        StartSilent = e.Args.Contains("--silent", StringComparer.OrdinalIgnoreCase);

        var mainWindow = new Views.MainWindow();
        MainWindow = mainWindow;

        if (!StartSilent)
        {
            mainWindow.Show();
        }
        // If silent, window stays hidden but tray icon is created (via MainWindow constructor).
    }
}
