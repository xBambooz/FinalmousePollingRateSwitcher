using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Threading;
using Finalmouse.Shared;
using WinForms = System.Windows.Forms;

namespace Finalmouse.ConfigUI;

/// <summary>
/// Manages the system tray icon. Reads status.json written by the service
/// to determine whether to show the idle or gaming icon.
/// </summary>
public class TrayIconManager : IDisposable
{
    private readonly WinForms.NotifyIcon _notifyIcon;
    private readonly Icon _idleIcon;
    private readonly Icon _gameIcon;
    private readonly DispatcherTimer _pollTimer;
    private bool _lastIsGaming;
    private bool _serviceWasRunning;

    public event Action? ShowWindowRequested;
    public event Action? ExitRequested;

    public TrayIconManager()
    {
        _idleIcon = LoadEmbeddedIcon("IdleIcon.ico");
        _gameIcon = LoadEmbeddedIcon("GameIcon.ico");

        _notifyIcon = new WinForms.NotifyIcon
        {
            Icon = _idleIcon,
            Text = "Finalmouse Polling Rate Switcher\nIdle",
            Visible = true,
        };

        // Double-click tray icon to show the config window
        _notifyIcon.DoubleClick += (_, _) => ShowWindowRequested?.Invoke();

        // Right-click context menu
        var menu = new WinForms.ContextMenuStrip();
        menu.Renderer = new DarkMenuRenderer();

        var openItem = menu.Items.Add("Open Config");
        openItem.Click += (_, _) => ShowWindowRequested?.Invoke();

        menu.Items.Add(new WinForms.ToolStripSeparator());

        var exitItem = menu.Items.Add("Exit");
        exitItem.Click += (_, _) => ExitRequested?.Invoke();

        _notifyIcon.ContextMenuStrip = menu;

        // Poll status.json every 2 seconds to update the icon
        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _pollTimer.Tick += PollStatus;
        _pollTimer.Start();
    }

    private void PollStatus(object? sender, EventArgs e)
    {
        var status = ServiceStatus.Read();

        if (status == null)
        {
            if (_serviceWasRunning || _lastIsGaming)
            {
                _notifyIcon.Icon = _idleIcon;
                _notifyIcon.Text = "Finalmouse Polling Rate Switcher\nService not running";
                _lastIsGaming = false;
                _serviceWasRunning = false;
            }
            return;
        }

        _serviceWasRunning = true;

        // Check if status is stale (service probably crashed)
        if ((DateTime.UtcNow - status.UpdatedAt).TotalSeconds > 30)
        {
            _notifyIcon.Icon = _idleIcon;
            _notifyIcon.Text = "Finalmouse Polling Rate Switcher\nService not responding";
            _lastIsGaming = false;
            return;
        }

        if (status.IsGaming != _lastIsGaming)
        {
            _lastIsGaming = status.IsGaming;

            if (status.IsGaming)
            {
                _notifyIcon.Icon = _gameIcon;
                _notifyIcon.Text = TruncateTooltip($"Finalmouse Polling Rate Switcher\n{status.CurrentRateHz}Hz — {status.GameName}");

                // Brief toast notification when a game is detected
                var cfg = AppConfig.Load();
                if (cfg.ShowNotifications)
                {
                    _notifyIcon.BalloonTipTitle = "Game Detected";
                    _notifyIcon.BalloonTipText = $"{status.GameName} → {status.CurrentRateHz}Hz";
                    _notifyIcon.BalloonTipIcon = WinForms.ToolTipIcon.Info;
                    _notifyIcon.ShowBalloonTip(2000);
                }
            }
            else
            {
                _notifyIcon.Icon = _idleIcon;
                _notifyIcon.Text = TruncateTooltip($"Finalmouse Polling Rate Switcher\n{status.CurrentRateHz}Hz — Idle");
            }
        }
        else
        {
            // Update tooltip even if icon hasn't changed
            if (status.IsGaming)
                _notifyIcon.Text = TruncateTooltip($"Finalmouse Polling Rate Switcher\n{status.CurrentRateHz}Hz — {status.GameName}");
            else
                _notifyIcon.Text = TruncateTooltip($"Finalmouse Polling Rate Switcher\n{status.CurrentRateHz}Hz — Idle");
        }
    }

    /// <summary>NotifyIcon.Text has a 64-character limit.</summary>
    private static string TruncateTooltip(string text)
        => text.Length <= 63 ? text : text[..60] + "...";

    private static Icon LoadEmbeddedIcon(string resourceName)
    {
        // Try embedded resource first (works in published single-file)
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream != null)
            return new Icon(stream);

        // Fallback: load from assets folder next to the exe
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        var path = Path.Combine(dir, "assets", resourceName);
        if (File.Exists(path))
            return new Icon(path);

        // Last resort
        return SystemIcons.Application;
    }

    public void Show()
    {
        _notifyIcon.Visible = true;
        if (!_pollTimer.IsEnabled)
            _pollTimer.Start();
    }

    public void Hide()
    {
        _notifyIcon.Visible = false;
        _pollTimer.Stop();
    }

    public void Dispose()
    {
        _pollTimer.Stop();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _idleIcon.Dispose();
        _gameIcon.Dispose();
    }
}

/// <summary>
/// Dark-themed renderer for the tray right-click menu.
/// </summary>
internal class DarkMenuRenderer : WinForms.ToolStripProfessionalRenderer
{
    public DarkMenuRenderer() : base(new DarkMenuColors()) { }

    protected override void OnRenderItemText(WinForms.ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = Color.FromArgb(232, 232, 232);
        base.OnRenderItemText(e);
    }
}

internal class DarkMenuColors : WinForms.ProfessionalColorTable
{
    private static readonly Color Bg = Color.FromArgb(22, 22, 22);
    private static readonly Color Border = Color.FromArgb(42, 42, 42);

    public override Color MenuItemSelected => Color.FromArgb(26, 42, 26);
    public override Color MenuItemBorder => Color.FromArgb(0, 229, 80);
    public override Color MenuBorder => Border;
    public override Color MenuItemSelectedGradientBegin => Color.FromArgb(26, 42, 26);
    public override Color MenuItemSelectedGradientEnd => Color.FromArgb(26, 42, 26);
    public override Color ToolStripDropDownBackground => Bg;
    public override Color ImageMarginGradientBegin => Bg;
    public override Color ImageMarginGradientMiddle => Bg;
    public override Color ImageMarginGradientEnd => Bg;
    public override Color SeparatorDark => Border;
    public override Color SeparatorLight => Border;
}
