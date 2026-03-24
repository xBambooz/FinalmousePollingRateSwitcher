using System.Runtime.InteropServices;
using System.Windows;

namespace Finalmouse.ConfigUI;

public partial class App : Application
{
    public const string AppId = "Finalmouse.PollingRateSwitcher";

    public static bool StartSilent { get; private set; }

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string appId);

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Set Application User Model ID so Windows groups this with the service
        SetCurrentProcessExplicitAppUserModelID(AppId);

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
