# Finalmouse Polling Rate Switcher

## Project Overview
A Windows utility that automatically switches Finalmouse ULX mouse polling rate between idle (battery-saving) and gaming (high performance) modes based on which game processes are running.

## Architecture
- **Two-exe design**: a Windows Service (headless background worker) + a WPF Config UI (admin tool)
- Service runs independently, survives reboots, shows in Task Manager → Services
- UI is config-only — closing it minimizes to tray; service keeps running
- Shared `config.json` at `C:\ProgramData\FinalmousePollingRateSwitcher\`
- Service re-reads config every ~30s to pick up UI changes

## Solution Structure
```
FinalmousePollingRateSwitcher/
├── shared/                          # Code shared by both projects (linked, not a separate project)
│   ├── AppConfig.cs                 # Config model + JSON load/save
│   ├── FinalmouseHid.cs             # HID communication with Finalmouse ULX
│   └── ServiceStatus.cs             # Shared status file for service→UI communication
├── src/
│   ├── PollingService/              # Windows Service (.NET 8 Worker Service)
│   │   ├── Program.cs               # Entry point, host builder
│   │   ├── PollingWorker.cs         # Core loop: scan processes, switch polling rate
│   │   ├── FileLogger.cs            # Simple file logger
│   │   └── PollingService.csproj
│   └── ConfigUI/                    # WPF Config UI (.NET 8 WPF + WinForms for NotifyIcon)
│       ├── App.xaml / App.xaml.cs    # Application entry
│       ├── GlobalUsings.cs          # Disambiguates WPF vs WinForms types
│       ├── ServiceManager.cs        # Install/start/stop/uninstall service via sc.exe
│       ├── TrayIconManager.cs       # System tray icon with state-based icon switching
│       ├── Views/MainWindow.xaml    # Main UI layout (sidebar + pages)
│       ├── Views/MainWindow.xaml.cs # Code-behind
│       ├── ViewModels/MainViewModel.cs
│       ├── Controls/RateButton.cs   # Custom polling rate selector button
│       ├── Converters/Converters.cs # XAML value converters
│       ├── app.manifest             # Requires admin elevation
│       └── ConfigUI.csproj
├── assets/
│   ├── IdleIcon.ico                 # Tray icon: idle/no game
│   └── GameIcon.ico                 # Tray icon: game detected
├── build.bat                        # Publishes both exes as single-file self-contained
├── FinalmousePollingRateSwitcher.sln
└── claude.md                        # This file
```

## Key Technical Details
- **HID Protocol**: VID `0x361D`, PID `0x0100`, Usage Page `0xFF00`, Usage `0x0001`
- **Polling rate command**: Report ID `0x04`, bytes `[0x04, 0x91, 0x02, rate_lo, rate_hi]`
- **Supported rates**: 500, 1000, 2000, 4000, 8000 Hz
- **NuGet deps**: HidSharp 2.1.0, Microsoft.Extensions.Hosting.WindowsServices 8.0.1, System.ServiceProcess.ServiceController 8.0.1
- **Config UI uses both WPF and WinForms** (WinForms needed for NotifyIcon/tray). GlobalUsings.cs resolves type ambiguities.
- **Build output**: Two self-contained single-file exes in `publish/` — no .NET runtime needed by end users

## Tray Icon Behavior
- IdleIcon.ico shown when service stopped or idle (no game detected)
- GameIcon.ico shown when service is running and a game process is detected
- Tooltip shows current state (e.g. "Gaming – 4000Hz" or "Idle – 1000Hz")
- Double-click tray icon → show config window
- Right-click → context menu (Show Config, Start/Stop Service, Exit)
- Closing window → minimizes to tray (Exit via tray menu)

## Build
Requires .NET 8 SDK + ".NET desktop development" workload.
```
build.bat
```
Output: `publish/FinalmousePollingService.exe` + `publish/FinalmousePollingRateConfig.exe`

## Known Issues / Notes
- XPanel must be closed — only one process can talk to the HID config interface
- Config UI requires admin (needed for sc.exe service management)
- Service publishes with PublishTrimmed=true; may need TrimmerRootAssembly if trimming breaks reflection
