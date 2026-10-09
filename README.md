# Darshan Player — Free Open-Source Media Player for Windows

[![Version](https://img.shields.io/github/v/release/Ujjwal-08/DarshanPlayer?style=for-the-badge&label=latest)](https://github.com/Ujjwal-08/DarshanPlayer/releases/latest)
[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stars][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![License][license-shield]][license-url]

**Darshan Player** is a fast, open-source desktop media player for Windows built with C# and WPF. Designed for Indian audiences — smooth playback of local videos, Hindi/regional audio tracks, and subtitle auto-loading out of the box.

> **[Download v1.2.0 →](https://github.com/Ujjwal-08/DarshanPlayer/releases/latest)**

---

## Table of Contents

- [Features](#features)
- [Screenshots](#screenshots)
- [Download & Install](#download--install)
- [Keyboard Shortcuts](#keyboard-shortcuts)
- [Built With](#built-with)
- [Getting Started (Developers)](#getting-started-developers)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [Support](#support)
- [License](#license)
- [Contact](#contact)

---

## Features

### Playback
- Hardware-accelerated video playback via **LibVLC** (MP4, MKV, AVI, MOV, WMV, FLV, WebM, TS, M2TS, 3GP, and more)
- Audio formats: MP3, AAC, FLAC, WAV, OGG, M4A
- Variable playback speed: 0.25× to 4× with ComboBox or keyboard
- Frame-by-frame stepping (when paused)
- A-B loop repeat between two points

### Subtitles
- Auto-loads subtitle file from same folder as video (SRT, ASS, SSA, VTT, SUB)
- External subtitle load via menu
- Subtitle delay adjustment
- Multiple subtitle track switching in controls bar

### Audio & Tracks
- Multi-audio track switching directly in controls bar
- Audio delay adjustment
- Volume 0–100 with mouse wheel

### Playlist
- Drag-and-drop files and folders
- Shuffle (Ctrl+S) and Repeat modes — None / One / All (R key)
- M3U / M3U8 save and load
- Sort by name or duration; filter by keyword
- Watch history — remembers resume position per file

### Window & UI
- True fullscreen covering entire monitor (no taskbar gap)
- Picture-in-Picture (PiP) mini player
- Taskbar progress bar and thumbnail play/pause/skip buttons
- Zoom / video scale (portrait reels, 9:16 content)
- Session restore — resumes last position on relaunch

### Windows Integration
- Appears in **Open with** for every supported video and audio format
- Listed in **Settings ▸ Apps ▸ Default apps** — pick Darshan Player as your default there, or use *Set as default player…* in the app (Windows always asks you to confirm; the app never changes your defaults on its own)
- **Add to Darshan Player playlist** on media files, **Play with Darshan Player** on folders (under *Show more options* on Windows 11)
- Start Menu and Desktop shortcuts, and `Win+R` → `DarshanPlayer`
- Recent files in the taskbar and Start Menu jump list
- One window: files opened from Explorer go to the running player — select several and the first plays while the rest queue (can be turned off in Settings)
- Everything is per-user (no admin prompt) and removed cleanly on uninstall, without touching other players' registrations

### Auto-Update
- Silent background updates via **Velopack** (v1.1.0 onward)
- In-app updates download over HTTPS with no browser and no SmartScreen prompt (the first install of an unsigned build can still show one)
- Notification shown when update is ready; applies on next restart
- Download icon in the title bar opens the releases page for a manual update

---

## Screenshots

> _Screenshots coming soon. Star the repo to follow updates._

---

## Download & Install

### Option 1 — Installer (Recommended)

1. Download `DarshanPlayer-win-Setup.exe` from [Releases](https://github.com/Ujjwal-08/DarshanPlayer/releases/latest)
2. Run it — no admin rights needed; it installs for your user and launches the player
3. Optional: choose *Set as default player…* in the menu to open Windows Default apps

**System requirements:** Windows 10 (1903+) or Windows 11, 64-bit

> No .NET runtime needed — installer is fully self-contained.

### Option 2 — winget

```powershell
winget install UjjwalDadhich.Darshan
```

### Option 3 — Portable

Download the `.zip` from Releases, extract, run `DarshanPlayer.exe` directly.

### Code signing

Free code signing for this project is provided by [SignPath.io](https://signpath.io/),
with a certificate issued by the [SignPath Foundation](https://signpath.org/).

---

## Keyboard Shortcuts

**Playback**

| Key | Action |
|-----|--------|
| Space | Play / Pause |
| S | Stop |
| ← / → | Seek backward / forward |
| `,` / `.` | Previous / next frame (paused) |
| `[` / `]` | Slow down / speed up |
| `\` | Reset speed to 1× |
| N / P | Next / previous track |

**Audio & subtitles**

| Key | Action |
|-----|--------|
| ↑ / ↓ | Volume up / down |
| M | Mute |
| H / J | Subtitle delay −100ms / +100ms |
| Ctrl+H | Reset subtitle delay |
| Ctrl+E | Audio & equalizer panel |

**View**

| Key | Action |
|-----|--------|
| F / F11 | Toggle fullscreen |
| Escape | Exit fullscreen |
| Z | Cycle aspect ratio |
| `+` / `-` | Zoom in / out |
| `*` | Reset zoom to auto-fit |
| I | Toggle media info |
| Ctrl+L | Toggle playlist |

**Playlist & loops**

| Key | Action |
|-----|--------|
| R | Cycle repeat (None → One → All) |
| Ctrl+← / Ctrl+→ | Previous / next chapter |
| Ctrl+S | Toggle shuffle |
| A / B | Set A-B loop start / end |
| Ctrl+A | Clear A-B loop |
| Ctrl+R | Set A-B loop point |
| Ctrl+Shift+S | Take screenshot |

---

## Built With

- [C# / .NET 10](https://dotnet.microsoft.com/) — WPF (Windows Presentation Foundation)
- [LibVLCSharp](https://github.com/videolan/libvlcsharp) — media engine
- [Velopack](https://github.com/velopack/velopack) — auto-update
- [MahApps.Metro.IconPacks](https://github.com/MahApps/MahApps.Metro.IconPacks) — icons

---

## Getting Started (Developers)

### Prerequisites

- Windows 10/11 64-bit
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022+ or Rider

### Clone & Run

```bash
git clone https://github.com/Ujjwal-08/DarshanPlayer.git
cd DarshanPlayer
dotnet restore
dotnet run --project DarshanPlayer.csproj
```

### Build Release

```powershell
dotnet publish DarshanPlayer.csproj -c Release -r win-x64 --self-contained -o publish
```

### Create Installer

Requires [Inno Setup 6](https://jrsoftware.org/isinfo.php) and [Velopack CLI](https://github.com/velopack/velopack):

```powershell
# Velopack package (for auto-update delta)
vpk pack --packId DarshanPlayer --packVersion 1.1.0 --packDir publish --mainExe DarshanPlayer.exe --outputDir Releases

# Inno Setup installer
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" Installer.iss
```

---

## Roadmap

- [x] Core video/audio playback (LibVLC)
- [x] Playlist — shuffle, repeat, sort, filter, M3U
- [x] Subtitle auto-load and multi-track switching
- [x] Multi-audio track switching in controls bar
- [x] Variable speed (0.25×–4×)
- [x] Frame step, A-B loop
- [x] Watch history — per-file resume position
- [x] PiP mini player
- [x] True fullscreen (no taskbar gap)
- [x] Taskbar progress + thumbnail controls (SMTC)
- [x] Session restore
- [x] Auto-update (Velopack, v1.1.0+)
- [x] File associations + right-click context menu
- [ ] Equalizer
- [ ] Chapter navigation
- [ ] Network stream (RTMP, HLS)
- [ ] Jump List (recent files in taskbar right-click)
- [ ] System tray / minimize to tray
- [ ] Protocol handler (`darshan://`)
- [ ] Thumbnail seek preview
- [ ] Android / cross-platform (future)

---

## Contributing

Contributions welcome — bug reports, translations, UI improvements, new features.

1. Fork the repo
2. Create feature branch: `git checkout -b feature/your-feature`
3. Commit: `git commit -m "Add your feature"`
4. Push: `git push origin feature/your-feature`
5. Open a Pull Request

Please check [open issues](https://github.com/Ujjwal-08/DarshanPlayer/issues) before starting new work.

---

## Support

If Darshan Player is useful to you:

- Star the repository
- Report bugs via [Issues](https://github.com/Ujjwal-08/DarshanPlayer/issues)
- Share with others

### Donate

- **Patreon:** https://www.patreon.com/cw/BABU_ISHU
- **PayPal:** https://www.paypal.com/ncp/payment/SECBQ62TRZZ6Y

### Business / Custom Builds

**Email:** help@chapterchase.com

---

## License

Distributed under the **Unlicense** — public domain, no restrictions.  
See [`LICENSE`](LICENSE) for details.

---

## Contact

**Maintainer:** Ujjwal Dadhich  
**Email:** [help@chapterchase.com](mailto:help@chapterchase.com)  
**Repository:** [https://github.com/Ujjwal-08/DarshanPlayer](https://github.com/Ujjwal-08/DarshanPlayer)

---

[contributors-shield]: https://img.shields.io/github/contributors/Ujjwal-08/DarshanPlayer.svg?style=for-the-badge
[contributors-url]: https://github.com/Ujjwal-08/DarshanPlayer/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/Ujjwal-08/DarshanPlayer.svg?style=for-the-badge
[forks-url]: https://github.com/Ujjwal-08/DarshanPlayer/forks
[stars-shield]: https://img.shields.io/github/stars/Ujjwal-08/DarshanPlayer.svg?style=for-the-badge
[stars-url]: https://github.com/Ujjwal-08/DarshanPlayer/stargazers
[issues-shield]: https://img.shields.io/github/issues/Ujjwal-08/DarshanPlayer.svg?style=for-the-badge
[issues-url]: https://github.com/Ujjwal-08/DarshanPlayer/issues
[license-shield]: https://img.shields.io/github/license/Ujjwal-08/DarshanPlayer.svg?style=for-the-badge
[license-url]: https://github.com/Ujjwal-08/DarshanPlayer/blob/main/LICENSE
