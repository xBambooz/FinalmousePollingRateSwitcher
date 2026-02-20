using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Finalmouse.ConfigUI.Controls;
using Finalmouse.ConfigUI.ViewModels;
using Finalmouse.Shared;

namespace Finalmouse.ConfigUI.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly TrayIconManager _tray;
    private readonly List<RateButton> _idleButtons = new();
    private readonly List<RateButton> _gamingButtons = new();

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;

        BuildRateButtons();

        // Select the ComboBox item matching the current scan interval
        foreach (ComboBoxItem item in IntervalCombo.Items)
        {
            if (int.TryParse(item.Tag?.ToString(), out var sec) && sec == _vm.ScanInterval)
            {
                IntervalCombo.SelectedItem = item;
                break;
            }
        }
        if (IntervalCombo.SelectedItem == null)
            IntervalCombo.SelectedIndex = 0;

        LogPathLabel.Text = AppConfig.GetLogPath();
        ConfigPathLabel.Text = AppConfig.GetConfigPath();

        // ── Tray Icon ──
        _tray = new TrayIconManager();
        _tray.ShowWindowRequested += () =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        };
        _tray.ExitRequested += () =>
        {
            Dispatcher.BeginInvoke(() => Close());
        };

        if (!_vm.ShowTrayIcon)
            _tray.Hide();

        // ── Minimize to tray ──
        StateChanged += MainWindow_StateChanged;

        // ── Window/Taskbar Icon ──
        LoadProgramIcon();
        ApplyDarkTitleBar();

        // Sync startup task with config
        if (_vm.StartOnStartup)
            SetStartupTask();
        else
            RemoveStartupTask();

        // If launched with --silent, start hidden (minimized to tray)
        if (App.StartSilent)
        {
            WindowState = WindowState.Normal;
            Hide();
        }
    }

    private void LoadProgramIcon()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var iconStream = assembly.GetManifestResourceStream("ProgramIcon.ico");
            if (iconStream != null)
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = iconStream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                Icon = bitmap;
            }
        }
        catch { }
    }

    private void ApplyDarkTitleBar()
    {
        try
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).EnsureHandle();
            const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
            int value = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int));
        }
        catch { }
    }

    [System.Runtime.InteropServices.DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    // ── Window Close / Minimize ──

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // X button always closes the app. Service keeps running independently.
        _tray.Dispose();
        Application.Current.Shutdown();
        base.OnClosing(e);
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && _vm.ShowTrayIcon)
        {
            // Minimize to tray: hide window, keep tray icon
            Hide();
            WindowState = WindowState.Normal; // Reset so Show() restores to normal
        }
        // If ShowTrayIcon is false, minimize works normally (stays in taskbar)
    }

    // ── Rate Buttons ──

    private void BuildRateButtons()
    {
        foreach (var rate in _vm.AvailableRates)
        {
            var btn = new RateButton
            {
                RateHz = rate,
                IsSelected = rate == _vm.IdleRate,
                Margin = new Thickness(0, 0, 8, 0),
            };
            btn.RateSelected += IdleRate_Selected;
            IdleRatePanel.Children.Add(btn);
            _idleButtons.Add(btn);
        }

        foreach (var rate in _vm.AvailableRates)
        {
            var btn = new RateButton
            {
                RateHz = rate,
                IsSelected = rate == _vm.GamingRate,
                Margin = new Thickness(0, 0, 8, 0),
            };
            btn.RateSelected += GamingRate_Selected;
            GamingRatePanel.Children.Add(btn);
            _gamingButtons.Add(btn);
        }
    }

    private void IdleRate_Selected(object sender, RoutedEventArgs e)
    {
        var clicked = (RateButton)sender;
        _vm.IdleRate = clicked.RateHz;
        foreach (var b in _idleButtons) b.IsSelected = b.RateHz == clicked.RateHz;
    }

    private void GamingRate_Selected(object sender, RoutedEventArgs e)
    {
        var clicked = (RateButton)sender;
        _vm.GamingRate = clicked.RateHz;
        foreach (var b in _gamingButtons) b.IsSelected = b.RateHz == clicked.RateHz;
    }

    // ── Interval ComboBox ──

    private void IntervalCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IntervalCombo.SelectedItem is ComboBoxItem item &&
            int.TryParse(item.Tag?.ToString(), out var seconds))
        {
            if (_vm != null) _vm.ScanInterval = seconds;
        }
    }

    // ── Page Navigation ──

    private void NavButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string pageName)
        {
            PagePollingRate.Visibility = pageName == "PagePollingRate" ? Visibility.Visible : Visibility.Collapsed;
            PageGames.Visibility = pageName == "PageGames" ? Visibility.Visible : Visibility.Collapsed;
            PageService.Visibility = pageName == "PageService" ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    // ── Game Management ──

    private void AddGame_Click(object sender, RoutedEventArgs e) => _vm.AddGame();

    private void NewProcessBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) _vm.AddGame();
    }

    private void RemoveGame_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is GameEntry game)
            _vm.RemoveGame(game);
    }

    // ── Service Control ──

    private async void InstallService_Click(object sender, RoutedEventArgs e)
    {
        var (success, msg) = await _vm.InstallServiceAsync();
        if (!success)
            MessageBox.Show(msg, "Service Install", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private async void UninstallService_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "This will stop and remove the Windows Service.\nAre you sure?",
            "Uninstall Service", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            var (success, msg) = await _vm.UninstallServiceAsync();
            if (!success)
                MessageBox.Show(msg, "Service Uninstall", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void StartService_Click(object sender, RoutedEventArgs e)
    {
        var (success, msg) = await _vm.StartServiceAsync();
        if (!success)
            MessageBox.Show(msg, "Service Start", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private async void StopService_Click(object sender, RoutedEventArgs e)
    {
        var (success, msg) = await _vm.StopServiceAsync();
        if (!success)
            MessageBox.Show(msg, "Service Stop", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private async void RestartService_Click(object sender, RoutedEventArgs e)
    {
        var (success, msg) = await _vm.RestartServiceAsync();
        if (!success)
            MessageBox.Show(msg, "Service Restart", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    // ── File Links ──

    private void LogPath_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            var path = AppConfig.GetLogPath();
            if (System.IO.File.Exists(path))
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            else
                Process.Start("explorer.exe", $"/select,\"{AppConfig.GetConfigDir()}\"");
        }
        catch { }
    }

    private void ConfigPath_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            var path = AppConfig.GetConfigPath();
            if (System.IO.File.Exists(path))
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch { }
    }

    // ── Settings: Tray Icon & Startup ──

    private void ShowTrayIcon_Changed(object sender, RoutedEventArgs e)
    {
        if (_vm.ShowTrayIcon)
        {
            _tray.Show();
        }
        else
        {
            _tray.Hide();
            // If disabling tray, also disable startup (startup starts minimized to tray)
            if (_vm.StartOnStartup)
            {
                _vm.StartOnStartup = false;
                RemoveStartupTask();
            }
        }
    }

    private void StartOnStartup_Changed(object sender, RoutedEventArgs e)
    {
        if (_vm.StartOnStartup)
            SetStartupTask();
        else
            RemoveStartupTask();
    }

    private const string StartupTaskName = "FinalmousePollingRateConfig";

    private void SetStartupTask()
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath)) return;

            // Use schtasks.exe to create a logon task that runs elevated (no UAC prompt)
            var args = $"/Create /TN \"{StartupTaskName}\" /TR \"\\\"{exePath}\\\" --silent\" " +
                       $"/SC ONLOGON /RL HIGHEST /F";
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);
        }
        catch { }
    }

    private void RemoveStartupTask()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/Delete /TN \"{StartupTaskName}\" /F",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);
        }
        catch { }
    }
}
