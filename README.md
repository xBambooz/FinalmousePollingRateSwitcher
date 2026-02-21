# Finalmouse Polling Rate Switcher

![Downloads](https://img.shields.io/github/downloads/xBambooz/FinalmousePollingRateSwitcher/total?style=flat&label=Downloads&color=00e550) ![Release](https://img.shields.io/github/v/release/xBambooz/FinalmousePollingRateSwitcher?label=Release&color=00e550) ![Platform](https://img.shields.io/badge/Platform-Windows-0078D6)

Automatically switches your Finalmouse ULX polling rate between **idle** (battery-saving) and **gaming** (high performance) modes based on which games you're running.

<img width="946" height="611" alt="FinalmousePollingRateConfig_CoPBskUYEI" src="https://github.com/user-attachments/assets/8b3f69f2-b6a2-4cbc-8cd7-348fbae7ebee" />


Runs as a **Windows Service** 

## How It Works

| State | Polling Rate | When |
|-------|-------------|------|
| Idle | 1000Hz (configurable) | No game process detected |
| Gaming | 4000Hz (configurable) | A configured game is running |

The service scans your running processes every few seconds. When it detects a game from your list, it sends an HID command to your Finalmouse to bump up the polling rate. When the game closes, it drops back down to save battery.

## Download

Grab the latest release from the [Releases](../../releases) page. You'll get two files:

| File | What It Does |
|------|-------------|
| `FinalmousePollingService.exe` | The background Windows Service |
| `FinalmousePollingRateConfig.exe` | Config UI (run this to set up) |

## Setup

1. **Put both `.exe` files in the same folder** (e.g. `C:\FinalmousePollingRateSwitcher\`)
2. **Right-click `FinalmousePollingRateConfig.exe` → Run as administrator** (needed to install the service)
3. **Configure your preferences:**
   - Set your idle and gaming polling rates
   - Add/remove games from the Game Profiles page
4. **Go to the Service page → click "Install & Start Service"**
5. **Close the config tool** — the service keeps running in the background

That's it. The service will start automatically when Windows boots.

## Supported Games (Default)

| Game | Process |
|------|---------|
| Valorant | `VALORANT-Win64-Shipping.exe` |
| Counter-Strike 2 | `cs2.exe` |
| Fortnite | `FortniteClient-Win64-Shipping.exe` |
| Apex Legends | `r5apex.exe` |
| Overwatch 2 | `overwatch.exe` |
| Call of Duty | `cod.exe` |
| Arc Raiders | `PioneerGame.exe` |
| The Finals | `FLClient-Win64-Shipping.exe` |
| KovaaK's | `FPSAimTrainer-Win64-Shipping.exe` |
| Aim Lab | `AimLab_tb.exe` |

You can add any game — just find its process name in **Task Manager → Details**.

## Important Notes

- Config is stored at `C:\ProgramData\FinalmousePollingRateSwitcher\config.json`
- Logs are at `C:\ProgramData\FinalmousePollingRateSwitcher\polling_switcher.log`

## Uninstall

1. Open the config UI as admin
2. Go to Service page → click "Uninstall Service"
3. Delete the exe files and the `C:\ProgramData\FinalmousePollingRateSwitcher` folder

## Building From Source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```
git clone https://github.com/YOUR_USERNAME/FinalmousePollingRateSwitcher.git
cd FinalmousePollingRateSwitcher
build.bat
```

Output goes to the `publish/` folder.

## Architecture

```
┌──────────────────────────────────┐
│  FinalmousePollingRateConfig.exe  │  ← WPF Config UI (run as admin)
│  - Edit rates, games, interval   │
│  - Install/start/stop service    │
│  - Writes config.json            │
└──────────────┬───────────────────┘
               │ config.json
               ▼
┌──────────────────────────────────┐
│  FinalmousePollingService.exe     │  ← Windows Service (always running)
│  - Reads config.json             │
│  - Monitors game processes       │
│  - Sends HID commands to mouse   │
│  - Runs at boot, survives logoff │
└──────────────────────────────────┘
```

## Compatible Mice

Tested with Finalmouse ULX

## License

MIT
