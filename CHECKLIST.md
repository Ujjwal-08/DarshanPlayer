# Darshan Player Full Checklist

This checklist turns the current audit into an execution plan for making Darshan Player fast, reliable, modern, and ready for Indian audiences.

## Phase 0: Immediate Stabilization

- [x] Make the project build cleanly from the current local environment.
- [ ] Verify package restore and document the supported .NET SDK and Windows target.
- [ ] Add a repeatable local build command for contributors.
- [x] Add startup diagnostics for LibVLC initialization failures. *(App.OnStartup: try/catch with libvlc-dir diagnostics, log + dialog, clean exit.)*
- [x] Add structured logging for playback, window-state changes, subtitle loading, and crashes. *(Serilog file sink; Playback/Window/Startup/Crash/Input categories.)*
- [ ] Audit and remove dead code, placeholder hooks, and legacy window-proc experiments.

## Phase 1: Critical Bug Fixes

### User-reported test findings
- [x] Fix PiP exit affordance so users can always leave PiP mode.
- [x] Ensure dragging PiP remains possible while video is playing.
- [x] Make PiP controls auto-hide again when the pointer moves away from PiP.
- [x] Prevent PiP drag-to-top from leaving the player in a confusing half-PiP/half-maximized state.
- [x] Improve fullscreen control wake/hide behavior by keeping the fullscreen popup layer alive.
- [x] Keep fullscreen controls visible when playback is paused or no media is actively playing.
- [x] Stop fullscreen from immediately cancelling itself during the `WindowState.Normal` transition.
- [x] Ensure fullscreen controls reliably reappear on pointer activity during playback and idle states.
- [x] Ensure true fullscreen covers the entire monitor and does not leave the Windows taskbar visible.
- [x] Keep fullscreen idle actions and playlist interactions clickable while fullscreen controls are present.
- [x] Remove duplicate fullscreen control layers so only one fullscreen control surface is ever visible.
- [x] Fix PiP overlay controls so they remain visible and interactive on hover.
- [x] Improve icon fallback for main media/control buttons so they do not render as boxes.
- [x] Restore fullscreen topmost behavior so the taskbar is covered in true fullscreen mode.
- [x] Move PiP controls into a popup layer so they stay above the video host while playing.
- [ ] Investigate white/black flashing or partial white rendering during minimize, maximize, and PiP transitions.
- [x] Reduce forced topmost behavior so switching to other apps works more normally while the player is running.
- [x] Improve no-media state rendering so resizing/state changes do not flash broken colors.

### Windowing, focus, fullscreen, PiP
- [x] Stop minimize from implicitly forcing PiP when the user expects a normal minimize.
- [x] Fix the bug where other apps open behind Darshan Player.
- [x] Centralize `Topmost` behavior so it is only enabled when explicitly needed.
- [ ] Rebuild fullscreen handling so it does not depend on fragile popup timing.
- [ ] Rebuild PiP mode with correct enter, exit, restore, and resize behavior.
- [ ] Ensure fullscreen, PiP, maximized, minimized, and normal states are mutually consistent.
- [ ] Remove duplicate fullscreen toggle paths from mouse events, Win32 hooks, and host hooks.
- [ ] Validate alt-tab, taskbar, multi-monitor, and DPI behavior.

### Playback reliability
- [ ] Investigate black-and-white or incorrect video rendering during state transitions.
- [x] Add playback error handling for unsupported or corrupt media.
- [ ] Add hardware acceleration fallback when decode/rendering fails.
- [ ] Ensure stop, play, pause, seek, and track switching work consistently.
- [x] Make session restore deterministic instead of delay-based.
- [x] Prevent race conditions when switching files quickly.

### Controls and interaction
- [x] Fix broken or inconsistent keyboard shortcuts.
- [x] Verify seek dragging and throttled seek behavior.
- [ ] Ensure overlays hide and show correctly in fullscreen and PiP.
- [ ] Make metadata/info overlays stable over the video surface.
- [x] Fix volume range inconsistency between UI and backend.

## Phase 2: Architecture Cleanup

- [ ] Move window-state logic out of `MainWindow.xaml.cs` into a dedicated controller/service.
- [ ] Reduce code-behind and keep it focused on view wiring only.
- [ ] Separate playback orchestration from UI state in `MainViewModel`.
- [ ] Remove duplicate event subscriptions and repeated state updates.
- [ ] Replace service locator usage with dependency injection.
- [ ] Introduce a single player state model: `Normal`, `Maximized`, `Fullscreen`, `PiP`, `Minimized`.
- [ ] Make settings persistence debounced and explicit instead of scattered.

## Phase 3: Playlist and Library Fixes

- [x] Fix shuffle behavior so current track tracking stays correct.
- [x] Persist repeat and shuffle settings.
- [x] Improve remove/current-item behavior in playlists.
- [x] Improve drag-and-drop queueing behavior.
- [x] Fix dropped-files playback order.
- [x] Replace fake folder picking with a proper folder-selection flow.
- [x] Add recent folders in addition to recent files.
- [ ] Add favorites, pinned media, and smart playlists.

## Phase 4: Localization and Indian Audience Support

- [ ] Unify supported languages across the language manager, settings menu, and resource files.
- [ ] Audit every user-visible string for localization coverage.
- [ ] Add missing Indian languages to the UI where already supported by resources.
- [ ] Validate rendering for Hindi, Marathi, Gujarati, Punjabi, Tamil, Telugu, Kannada, Bengali, and Malayalam.
- [ ] Add subtitle font, size, color, and script-friendly rendering options.
- [ ] Handle Indian-language filenames and metadata sorting correctly.
- [ ] Add language-specific QA passes for layout clipping and truncation.

## Phase 5: UI/UX Redesign

- [ ] Redesign the title bar and window controls.
- [x] Add a clear PiP button instead of overloading minimize.
- [ ] Redesign fullscreen controls for clarity and accessibility.
- [x] Make playlist panel resizable. *(`GridSplitter` between video and playlist columns; 220–500 px range.)*
- [ ] Redesign the playlist panel and item states.
- [x] Add a modern settings surface instead of overloading context menus. *(Settings dialog with General + Keyboard Shortcuts tabs; opened via "More Settings…" in context menu.)*
- [ ] Make controls work well on small windows and high-DPI displays.
- [ ] Improve empty state, drop state, loading state, and error state visuals.
- [ ] Improve media info presentation.

## Phase 6: Modern Media Features

- [x] Add subtitle styling controls. *(font size/family/colour/outline/background via freetype-* options; applies to next-opened file. See Phase 10.3.)*
- [ ] Add subtitle offset presets and track memory per file.
- [ ] Add audio equalizer and presets.
- [x] Add time label on seek bar hover. *(Floating popup shows `m:ss`/`h:mm:ss` at cursor position; hides on mouse leave.)*
- [ ] Add thumbnail preview on seek.
- [ ] Add playback bookmarks and continue-watching history.
- [ ] Add screenshot gallery/history.
- [ ] Add playback speed presets and custom speed input.
- [ ] Add configurable shortcuts.
- [ ] Add intro skip / chapter navigation improvements.
- [ ] Add streaming URL playback support.
- [ ] Add audio-only optimized mode.

## Phase 7: Performance and Optimization

- [ ] Measure startup time, first-frame time, seek latency, CPU, memory, and GPU usage.
- [ ] Reduce UI-thread work in media event handlers.
- [x] Debounce or batch frequent settings writes. *(500ms `SaveDebounced()` via `System.Threading.Timer` — done in A2.)*
- [ ] Avoid unnecessary track refreshes and state churn.
- [ ] Test large playlists and long-running playback sessions.
- [ ] Test high-bitrate 1080p and 4K files.
- [ ] Profile subtitle-heavy content and frequent seeking scenarios.

## Phase 8: Quality and Testing

- [x] Create a dedicated test project.
- [x] Add unit tests for playlist behavior.
- [ ] Add unit tests for settings persistence.
- [ ] Add unit tests for repeat/shuffle/session restore behavior.
- [ ] Add manual QA checklists for fullscreen, PiP, minimize/restore, multi-monitor, and DPI.
- [ ] Build a media sample pack for regression testing.
- [ ] Add release validation steps before packaging MSI builds.

## Session log - 2026-09-15 (Windows integration)

Parity with, and beyond, the old Inno Setup installer, which registered file associations and
Default Programs entries that the Velopack build had silently dropped. Suite at 197 passing.

**Registration (ShellRegistration + AppIdentity).** Per-user (HKCU), so no admin prompt:
ProgIDs `DarshanPlayer.Video` / `DarshanPlayer.Audio`; `OpenWithProgids` for every extension in
`MediaFormats`; `Applications\DarshanPlayer.exe` with SupportedTypes; `Capabilities` +
`RegisteredApplications` for Settings > Default apps; "Add to Darshan Player playlist" via
SystemFileAssociations; "Play with Darshan Player" on folders; App Paths. Written by Velopack's
after-install and after-update hooks, re-applied idempotently on every launch of an installed copy
(never a dev build or portable copy), removed by the before-uninstall hook.
Images (.jpg/.png/.gif/.bmp), which the Inno script claimed, are deliberately not registered.

**Verified against the real shell, not just the fake registry:** `SHAssocEnumHandlers` lists
"Darshan Player" in Open with for .mp4/.mkv/.mp3 after the install hook; the uninstall hook removes
every entry and leaves the other four .mp4 handlers intact. The Settings > Default apps page itself
was not viewed (screen access to Settings was declined) - worth one manual look.

**Single instance (SingleInstance + OpenBatchPolicy).** Per-user mutex + named pipe. A multi-select
Open, which Windows turns into one process per file, now yields one window: first file plays, the
rest queue (verified: 3 simultaneous launches handed off and exited 0, one `Opening` logged). Opens
more than 2s apart replace playback as normal. Setting: "Open files in the already-running window".

**Fixed along the way:**
- A folder passed on the command line or dropped on the window was added to the playlist as if it
  were one file. `MediaFormats.ExpandToMediaFiles` now expands it (verified).
- The process never set an AppUserModelID, while Velopack's shortcuts carry
  `velopack.DarshanPlayer`, so a pinned taskbar icon and the running window could be two buttons
  and a jump list had nothing to attach to. Set explicitly before any window exists.
- Three extension lists had drifted (folder scan lacked .m2ts); `MediaFormats` is the single source.

**Packaging.** `vpk pack` now passes `--packTitle "Darshan Player"`, `--packAuthors`, `--icon`, and
explicit `--shortcuts`. Builds up to 1.1.0 had no title, so their shortcuts are named
"DarshanPlayer"; the after-update hook deletes such a shortcut only when a "Darshan Player" one
exists beside it. Whether Velopack recreates shortcuts under the new title on update has not been
observed - verify with a real 1.1.0 -> 1.2.0 update before relying on it.

Not done, deliberately: auto-start with Windows (the README claimed it; no code or installer ever
had it, and a player has no reason to run at login); `darshan://` protocol (no caller, and it would
let a web page open local paths).

## Session log - 2026-09-14 (release prep)

**Manual update link.** Download icon in the title bar, next to the support buttons, opening
`UpdateService.ReleasesPageUrl`. The URL is derived from the same `RepoUrl` Velopack's GithubSource
uses, so the in-app updater and the manual link cannot drift apart; a test pins both. Velopack still
updates silently in the background - this is the escape hatch for when that has not run, was blocked
by a network policy, or the user wants to read the release notes first.

**v1.2.0 built and verified.** `build-release.ps1 -Version 1.2.0` produced Setup.exe (195.7MB),
Portable.zip (191.5MB), full + delta nupkgs. plugins.dat generated (259.2KB). The shipped
`publish\DarshanPlayer.exe` reports 1.2.0.0, plays 4K HEVC, and the update button was confirmed
firing via the log.

Startup timing, measured on the release build: **19.6s cold, 1.5s warm.** The cold figure is
one-time AV/file-cache scanning of a freshly written 191MB self-contained tree, not a code problem -
it reproduces on any fresh publish and disappears on the second launch. Worth knowing because the
first launch after an install will look slow to users.

**Two things to decide before publishing:**

1. `[WRN] Velopack library version is lower than vpk version (0.0.915.0 < 1.2.0.0).` The NuGet
   package and the CLI are far apart. This is the component that performs auto-updates for existing
   1.1.0 users, so a mismatch is a real risk - but upgrading it is also a change to the update path
   that cannot be properly tested without publishing a release. Left alone deliberately.
2. `[WRN] No signing parameters provided, 1172 file(s) will not be signed.` Unsigned installers
   trigger SmartScreen on first run. Note the README currently claims "No SmartScreen prompt on
   update" - true for Velopack's in-app delta updates, misleading for the initial install.

Also noted: the git remote here is `DarshanSharp`, while `UpdateService` and the README both point
at `DarshanPlayer`. Both repos exist and both carry identical v1.1.0 releases, so they appear to be
mirrors - but the release step needs to target whichever one users actually download from.

## Session log - 2026-09-14

Logging, test seams, and the first real win from having both. Build clean (0 warnings), suite at
149 passing (was 107).

**Structured logging (A21, Phase 0).** Serilog file sink at
`%LocalAppData%\DarshanPlayer\logs\darshan-YYYYMMDD.log`, rolling daily and at 10MB, 7 files kept.
Categories: App, Startup, Playback, Window, MediaService, Input, Crash. Level is switchable at
runtime via `AppSettings.VerboseLogging` (default off) so Debug tracing can be turned on from
settings without a rebuild. Logging is failure-tolerant throughout - a broken sink degrades to
Debug output rather than taking playback down. LibVLC init now has real startup diagnostics: on
failure it logs the base directory and whether the libvlc folder is present, shows a dialog
pointing at the log, and exits cleanly instead of crashing.

**Mockable IMediaService (Phase 24).** `FakeMediaService` records what the view-model pushes.
`SettingsService` now takes an injectable directory, so tests never read or overwrite the real
settings file - this was the actual blocker, and it is narrower than the "wrap System.IO behind
IFileSystem" the checklist proposed. Together these unblocked 42 tests covering crop, deinterlace,
aspect, zoom, audio delay, subtitle delay, audio normalization, channel mode, the equalizer wiring
and chapter navigation. MainViewModel is now constructible in a unit test, which is the larger win.

**Keyboard shortcuts died in fullscreen - fixed.** Found within minutes of logging existing: the
log showed a single `ApplyFullscreen(true)` for three F presses. Cause: `UpdateFullscreenControlsWindowBounds`
called `SetWindowPos(... SWP_SHOWWINDOW)` without `SWP_NOACTIVATE`, so showing the controls overlay
took foreground *and* keyboard focus from the main window. Every shortcut then went to a window
with no key handler. Two fixes, both verified in the log:
  1. `SWP_NOACTIVATE` on the overlay, so it never steals activation.
  2. The overlay forwards `PreviewKeyDown` into the shared handler, covering the legitimate case
     where the user clicks the bar and genuinely activates it.
`Window_KeyDown`'s body was extracted into `HandleShortcut(Key, ModifierKeys)` so both paths run
identical logic, and `Escape` gained a case there - it previously existed only in `HwndHook`, on the
main window's HWND.

Notes:

- A key-forwarding hook on the LibVLC video host was written and then removed: traced across
  several runs and it never fires, for the same reason the codebase already polls the cursor
  instead of using that hook for mouse moves. The deep child does not route key messages to the
  HwndHost.
- One run showed LibVLC taking 28s to initialize; subsequent runs are ~0.8s. That looks like a
  cold file cache or on-access AV scan of the libvlc plugin tree right after a build, not a code
  problem - but the timing is in the log now if it recurs.
- `Escape` could not be verified end-to-end: the automation harness never delivered it (it did not
  reach even the raw Win32 hook, while Space and F did). The handler is in place and the same exit
  path is proven via F; Escape is worth one manual check.

## Session log - 2026-09-13

Feature pass. Build clean (0 warnings), suite at 107 passing, every item below exercised in a
running build against real media.

Shipped:

- **Equalizer** - 10 bands + preamp, 8 presets, hand edits auto-switch to "Custom" and persist.
  Applied through `MediaPlayer.SetEqualizer` / `UnsetEqualizer`.
- **Crop** - 12 ratios via `MediaPlayer.CropGeometry`. Verified: 4:3 visibly narrows a 16:9 source.
- **Deinterlace** - 9 modes via `MediaPlayer.SetDeinterlace`; empty string detaches the filter.
- **Audio routing** - volume levelling (compressor, applies from the next file) and
  stereo/mono/left/right/reversed downmix via `SetChannel`.
- **Chapter navigation** - `ChapterInfo` model, chapter list in the menu, `Ctrl+Left`/`Ctrl+Right`.
- **Audio panel** - new flyout hosting the equalizer, levelling, output mode and the A/V sync
  slider (`AudioDelay` existed but had no UI at all).

Notes for whoever picks this up:

- **Rotation was built and then removed.** LibVLCSharp 3.x exposes no runtime transform, so it went
  in as a per-media option with an in-place reload. The reload works; the filter never applies.
  Tried the CLI-style pair (`:video-filter=transform` + `:transform-type=90`), the module-argument
  form (`:video-filter=transform{type=90}`), and with hardware decoding off. The picture stayed
  upright every time - the WPF/D3D11 output path appears to skip the software transform filter.
  Rather than ship a dropdown that silently does nothing (the same defect as the old "Fill" aspect
  entry), the whole feature was reverted. Reviving it likely means a different video output module
  or doing the rotation outside LibVLC.
- Six combo boxes in the new panels initially rendered with the default light WPF style and were
  unreadable on the dark theme. `DarkCombo` already existed in DarkTheme.xaml - apply it to any
  new ComboBox.
- A **sleep timer already exists** in MainViewModel (pauses on expiry). A second implementation was
  written before that was noticed, and was removed. Check for an existing implementation before
  adding one - the checklist does not mention this feature at all.
- Volume levelling is an instance-level LibVLC option, so it only takes effect from the next file.
  The toast says so; a live-applying version would need the media reloaded.

## Session log — 2026-09-11

Fixed this pass (all verified against a running build; suite at 75 passing):

- **Fullscreen playlist button was a no-op.** `ApplyPlaylistColumns` suppressed the column whenever
  `IsFullscreen` was true, while the fullscreen bar still exposed `TogglePlaylistCommand`.
- **Fullscreen bar auto-hide was position-dependent.** Showing the layered overlay under a
  motionless cursor synthesised MouseEnter/MouseMove, which re-woke it in a loop; combined with an
  unbounded hover keep-alive, a cursor parked near the bottom centre latched the bar on screen
  permanently. Pointer wakes are now deduped by real cursor position and hover keep-alive is
  bounded to 3s.
- **Watch history** now records duration alongside position, applies the <95% resume rule, and does
  genuine LRU eviction (the old code removed an arbitrary dictionary key). Legacy entries migrate.
- **Playlist session persistence** — saved on clean exit, restored on launch, dead paths dropped.
- **Playlist duration column** populated from the player's LengthChanged event.
- **Subtitle delay keys** moved to the spec's H/J ±100ms with Ctrl+H reset (was undocumented
  G/H ±50ms). Aspect-ratio cycling went to **Z**, not A — A/Ctrl+A already drive the A-B loop.
- **"Fill" aspect ratio** resolved to null, making it a silent duplicate of "Default"; it now
  stretches to the host's live size and re-applies on resize and fullscreen transitions.
- **`dotnet test` was failing to build**, not failing tests: the untracked `publish/` artifact
  folder was globbed into the project as content, so MSBuild tried to copy a `libvlc/win-x86` tree
  into the test output from a path that doesn't exist. Excluded via `DefaultItemExcludes`.

Known gaps left open deliberately:

- Playlist durations only appear for items that have been played. Pre-parsing every entry with
  `Media.Parse` on a background thread **crashes LibVLC natively (0xC0000005)** — reproduced with
  both the shared playback instance and a dedicated one. Needs native-level investigation before
  anything in that shape ships.
- Recent-files thumbnails (14.2) untouched; they would need the same parse/snapshot path.
- Toast strings are still English-only (Phase 23 / Phase 4).
- Subtitle outline colour and vertical position remain deferred on uncertain VLC option names.
- App still starts minimized, and the welcome card's "Open" button does not raise a file dialog.
  Both are pre-existing and not yet triaged.

## Current Execution Order

- [x] Audit codebase and identify major failure areas.
- [x] Create this checklist file.
- [x] Fix build/stabilization blockers first.
- [ ] Continue fixing window-state and topmost/PiP/fullscreen behavior.
- [ ] Add test scaffolding and regression checks.
- [ ] Move on to redesign and feature work.

---

# Appendix A — 2026-05-21 Codebase Audit: Bug Inventory

Findings from a deep read of every source file. Numbered for cross-reference in PRs and commits.

## Critical (data loss, crashes, persistent leaks)

- [x] **A1** `SettingsService.Save()` is not atomic — interrupted writes corrupt `settings.json`. Use temp-file + replace.
- [x] **A2** `SettingsService.Save()` is not debounced — `MainViewModel.TimeChanged` triggers writes per tick (60+/sec during seek). Add `SaveDebounced()` with 500ms coalesce.
- [x] **A3** Empty `catch {}` blocks silently swallow errors in `SettingsService` (L25, L36), `MainWindow.xaml.cs` (L196 SMTC init, L1297 OpenUrl). Replace with `Debug.WriteLine`.
- [x] **A4** `MainViewModel` subscribes to 6 LibVLC media events in constructor but never unsubscribes. ViewModel lives for app lifetime so impact is bounded, but no `IDisposable`. Implement.
- [x] **A5** `ObservableCollection<MediaTrackInfo>` is reassigned inside `LengthChanged` and `Stopped` handlers. Currently wrapped in `Dispatcher.Invoke` (verified at L332+) — OK but fragile; add comments + unit test.
- [x] **A6** `MainWindow.OnClosing` does not call `MainViewModel.Dispose()`. Will matter once A4 is implemented.

## High (user-visible bugs)

- [x] **A7** `PlaylistItem.Title` throws `NullReferenceException` if `FilePath` is null (L12). Add null guard.
- [x] **A8** `PlaylistService.Add()` duplicate check is case-sensitive (L24). Windows paths are case-insensitive. Use `StringComparer.OrdinalIgnoreCase`.
- [x] **A9** `PlaylistViewModel.ShuffleIcon` returns the same glyph regardless of `IsShuffle` state (L65). Add visual differentiation.
- [x] **A10** `MainViewModel.RateCommand` accepts unbounded float from XAML CommandParameter (L528). Clamp to `[0.25, 4.0]`.
- [x] **A11** `MainViewModel.ResumeSessionCommand` uses `Task.Delay(1000)` then seeks — fragile if media isn't ready (L463). Seek inside `LengthChanged` handler instead.
- [x] **A12** `MainViewModel.CheckForSessionRestore` `DispatcherTimer` Tick handler never unsubscribes (L605). Single-shot pattern needed.

## Medium (perf, quality)

- [x] **A13** `RefreshTracks()` rebuilds `ObservableCollection` from scratch every call (L738). Diff-based update would avoid UI thrash.
- [x] **A14** `Position` setter throttles seeks via `DateTime.Now` (L91) — fragile if system clock adjusts. Use `Stopwatch`.
- [x] **A15** Codec FourCC decoding in `LibVlcMediaService.GetMediaMetadata` uses `BitConverter.GetBytes(int) → Select(b => (char)b)`. Non-ASCII bytes produce garbage. Use a proper FourCC decoder.
- [x] **A16** Window restore position not bounds-checked against `SystemParameters.WorkArea` — off-screen settings restore off-screen window.
- [x] **A17** `HwndHost.MessageHook` runs on window message thread; ViewModel access not always marshalled.
- [x] **A18** PiP size hard-coded 320×180 — no DPI/screen-relative scaling.
- [x] **A19** Volume default 80 but range 0–200 is inconsistent (LibVLC supports 0–200 but UI usually expects 0–100).

## Low (code hygiene)

- [ ] **A20** Static `ServiceLocator` instead of DI container. Defer until major refactor.
- [x] **A21** No structured logging anywhere. Add Serilog or `Microsoft.Extensions.Logging`. *(Serilog.Sinks.File; runtime-switchable level via AppSettings.VerboseLogging.)*
- [ ] **A22** Hardcoded screenshot path (`%MyPictures%\DarshanPlayer`). Make configurable.
- [x] **A23** Added `DarshanPlayer.Tests` (xUnit) with PlaylistService / NotificationService / VideoAdjustment / AppSettings suites + `DarshanPlayer.sln`. Coverage still partial.

---

# Appendix B — Full Feature Implementation Spec Tracker (2026-05-21)

Sourced from the comprehensive feature prompt. Each item must be implemented end-to-end (Model → Service → ViewModel → View) with unit tests per the spec's quality bar.

> **Conventions**: Each top-level item is a feature area. Checkbox at the area = all sub-items done. Tests are tracked separately at the bottom of each area.

## Phase 9 — Core Playback Engine Enhancements

### 9.1 Variable Playback Speed
- [x] Existing 8-rate menu already in place (0.25× … 2.0×).
- [x] Add segmented button/ComboBox in controls bar (not just menu).
- [x] Keyboard: `]` step up, `[` step down, `\` reset to 1×.
- [ ] Tests: `PlaybackRate_ChangesMediaPlayerRate`, `PlaybackRate_ClampsAtBounds`, `PlaybackRate_ResetsWithBackslash`.

### 9.2 Frame-by-Frame Stepping
- [x] Forward step exists (`NextFrameCommand`, `.` key).
- [x] Backward step: pause + seek back by ~33ms (one frame at 30fps); LibVLC has no native back-step.
- [x] Keyboard: `,` step-back, `.` step-forward (already present).
- [x] Buttons visible only when paused. *(`IsNotPlaying` computed prop + `BoolToVisConverter` on frame-step buttons in controls bar.)*
- [ ] Tests: `StepForward_CallsNextFrame_WhenPaused`, `StepBackward_SeeksOneFrameBack_WhenPaused`.

### 9.1 (continued)
- [x] Speed ComboBox in controls bar (8-item; bound via `SpeedIndex`). *(Shows ¼× through 4×; syncs with keyboard `]`/`[`/`\`.)*

### 9.3 A-B Loop
- [x] `LoopPointA`, `LoopPointB`, `IsABRepeatActive` exist.
- [x] Visual markers on seek bar (Rectangle overlays + converter). *(Canvas overlay with green A marker + orange B marker; positioned via `ABPointAFraction`/`ABPointBFraction` computed props.)*
- [x] Keyboard: `A` set A, `B` set B, `Ctrl+A` clear.
- [ ] Tests: `AbLoop_SeeksToA_WhenPositionExceedsB`, `AbLoop_DoesNotActivate_IfBBeforeA`.

### 9.4 Chapter Navigation
- [x] `ChapterList` ObservableCollection from `MediaPlayer.ChapterDescription()`.
- [x] Keyboard: `Ctrl+Right`/`Ctrl+Left` next/prev chapter.
- [ ] Tick marks on seek bar.
- [x] Model: `ChapterInfo { Index, Title, Start, Duration }`.
- [x] Tests: `ChapterList_IsPopulated_OnMediaLoaded`, `NextChapter_WrapsAround`. *(ChapterInfoTests covers labelling; population needs live media.)*

## Phase 10 — Subtitle System

### 10.1 External Subtitle Loading
- [x] `LoadExternalSubtitle` works via `LibVlcMediaService`.
- [ ] `ISubtitleService` abstraction.
- [x] Auto-load: scan folder for matching base-name on media open. *(Called from `PlayRequested` handler; notifications on match.)*
- [x] Formats: `.srt`, `.ass`, `.ssa`, `.vtt`, `.sub`. *(SubtitleExtensions HashSet in LibVlcMediaService.)*
- [ ] Tests: `LoadSubtitle_AddsTrackToMediaPlayer`, `AutoLoad_FindsMatchingSubtitleFile`.

### 10.2 Built-in Subtitle Track Selection
- [x] `SubtitleTracks` collection + menu selection exists.
- [x] Move into a dedicated controls-bar dropdown (not only context menu). *(`SubtitleTracksWithOff` ComboBox in controls bar.)*
- [ ] Tests: `SubtitleTracks_IsPopulated_WhenMediaHasEmbeddedSubs`.

### 10.3 Subtitle Appearance Customization
- [x] Settings: `SubtitleFontSize (0=auto,14–48)`, `SubtitleFontFamily`, `SubtitleColorRgb`, `SubtitleOutlineThickness`, `SubtitleBackgroundOpacity (0–255)` added to `AppSettings`.
- [ ] Still TODO: `SubtitleOutlineColor`, `SubtitleVerticalPosition (%)` (deferred — uncertain VLC option names).
- [x] Applied via `freetype-*` **media options** in `LibVlcMediaService.PlayFile` (takes effect on the **next opened file** — LibVLC 3.x cannot restyle SPU live; `SetSpuTextScale` is 4.x only). Custom WPF SRT overlay fallback deferred.
- [x] Settings panel: "Subtitle Settings" flyout (font size, font family, colour swatches + preview, outline, background). Live preview deferred.
- [ ] Tests: `SubtitleFontSize_ClampedBetween14And48` (clamping lives in VM setter; covered indirectly), `SubtitleVerticalPosition_AppliedToOverlay` (deferred).

### 10.4 Subtitle Delay / Sync
- [x] `SubtitleDelay` property bound to MediaPlayer.
- [x] Spec keys: `H` shift −100ms, `J` shift +100ms, `Ctrl+H` reset. *(Adopted 2026-09-11; the old G/H ±50ms was undocumented.)*
- [x] Reset on new media load. *(`SubtitleDelay = 0` in `PlayRequested` handler.)*
- [x] Tests: `SubtitleDelay_AppliedToMediaPlayer`, `SubtitleDelay_Reset_OnNewMedia`.

## Phase 11 — Audio System

### 11.1 Audio Track Selection
- [x] Working via `AudioTracks` + menu.
- [x] Reload every media change. *(`RefreshTracks()` called on `Playing` event.)*
- [x] Controls-bar ComboBox added. *(`HasMultipleAudioTracks`-gated ComboBox in controls bar.)*
- [x] Tests: `AudioTracks_Repopulated_OnMediaChange`.

### 11.2 Audio Delay
- [x] `AudioDelay` property (−2000 to +2000ms) via `MediaPlayer.SetAudioDelay()`. Resets to 0 on new media. `AdjustAudioDelayCommand` accepts delta string.
- [ ] Keyboard: conflicts with subtitle delay keys — deferred.
- [x] Tests: `AudioDelay_AppliedToMediaPlayer`.

### 11.3 Equalizer
- [x] Model `EqualizerProfile { Name, PreAmp, Bands[10] }`.
- [x] Presets: Flat, Bass Boost, Vocal Boost, Classical, Dance, Pop, Rock, Podcast.
- [x] `IEqualizerService.Apply/Reset/SaveCustom`. *(EqualizerPresets + MainViewModel; no separate service needed.)*
- [x] WPF panel: 10 vertical sliders (−20 to +20 dB), PreAmp slider.
- [x] Apply via `LibVLC.Equalizer` API.
- [x] Persist active preset + custom presets.
- [x] Tests: `Equalizer_Apply_SetsAllBandValues`, `SaveCustomPreset_PersistsToSettings`, `LoadPreset_RestoresBandValues`. *(EqualizerTests, 26 tests over profile/presets/band VM.)*

### 11.4 Audio Normalization
- [x] `NormalizeAudio` bool → VLC `--audio-filter=compressor`.
- [x] Toggle button in audio menu.
- [x] Tests: `AudioNormalization_SetsVlcAudioFilter`.

### 11.5 Stereo / Mono / Surround Mix
- [x] `AudioChannelMode` enum → `MediaPlayer.AudioChannel`.
- [x] Picker in audio menu.
- [x] Tests: `AudioChannel_SetCorrectly_ForEachMode`.

## Phase 12 — Video Adjustments

### 12.1 Adjustments Panel
- [x] Brightness (0–2), Contrast (0–2), Saturation (0–3), Gamma (0.01–10), Hue (−180 to +180). Sharpness deferred (separate `sharpen` module, not part of the `adjust` filter).
- [x] Apply via VLC `adjust` filter (`SetAdjustInt(Enable,1)` + `SetAdjustFloat(...)`), re-applied on the `Playing` event since VLC resets it per media. Values persisted in `AppSettings` and clamped in `Models/VideoAdjustment`.
- [x] "Reset All" button (`ResetVideoAdjustmentsCommand`).
- [x] Tests: `ClampBrightness/Saturation/Gamma/Hue_BoundsToRange`, `IsNeutral_*` (in `VideoAdjustmentTests`). `VideoAdjust_AppliedToMediaPlayer` integration test deferred (needs a mockable IMediaService).

### 12.2 Aspect Ratio
- [x] Existing menu (Default/16:9/4:3/1:1/21:9/Fill).
- [x] Add `Stretch` mode per spec. *(Implemented as "Fill", which previously resolved to null and so silently duplicated "Default"; now stretches to the host's live size.)*
- [x] Keyboard: cycling bound to `Z`, not `A` — A/Ctrl+A already drive the A-B loop, which shipped first.
- [x] Tests: `AspectRatio_SetsMediaPlayerAspectRatioString`.

### 12.3 Crop
- [x] `CropMode` enum (12 values).
- [x] Apply via `MediaPlayer.CropGeometry`.
- [x] Tests: `Crop_SetsMediaPlayerCropGeometry`.

### 12.4 Zoom
- [x] `Zoom` 0.25–4.0 via `MediaPlayer.Scale`. Persisted to `AppSettings.VideoScale`. `VideoScale` added to `IMediaService`.
- [x] Keyboard: `+` zoom in, `-` zoom out, `*` reset (numpad).
- [x] Tests: `Zoom_ClampsAtBounds`.

### 12.5 Rotation
- [ ] 0/90/180/270 via VLC `transform` filter. **BLOCKED** - built and reverted 2026-09-13. Three option forms tried, plus hardware decoding off; the D3D11/WPF output never applies the transform filter. See the 2026-09-13 session log. Needs a different video output module, or rotation done outside LibVLC.
- [ ] Tests: `Rotation_SetsTransformFilter`. *(moot while the feature is blocked)*

### 12.6 Deinterlace
- [x] Modes: Off/Discard/Blend/Mean/Bob/Linear/X/Yadif/Yadif2x.
- [x] Apply via `MediaPlayer.Deinterlace`.
- [x] Tests: `Deinterlace_SetsMode`.

## Phase 13 — Playlist System

### 13.1 Shuffle Mode
- [x] Basic shuffle works.
- [x] "No-repeat until exhausted" pool semantics. *(`_shufflePool` list in `PlaylistService`; refills and randomizes when exhausted.)*
- [x] Keyboard: `Ctrl+S` toggles shuffle. *(Added to Key.S case in Window_KeyDown.)*
- [ ] Tests: `Shuffle_DoesNotRepeatUntilAllPlayed`, `Shuffle_ResetsPoolWhenExhausted`.

### 13.2 Repeat Modes
- [x] `None/One/All` exists.
- [x] Keyboard `R` cycles repeat (`Ctrl+R` = A-B point). *(Key.R → PlaylistVM.ToggleRepeatCommand.)*
- [ ] Tests: `RepeatOne_RestartsCurrentTrack`, `RepeatAll_WrapsToFirst_AfterLast`.

### 13.3 Playlist Persistence
- [x] M3U8 save/load (`SaveM3U`, `LoadM3U`) in `PlaylistService`. `SavePlaylistCommand`/`LoadPlaylistCommand` in `MainViewModel`. "Save M3U"/"Load M3U" buttons in playlist panel.
- [x] Auto-save last playlist on close, restore on startup.
- [x] Tests: `SavePlaylist_WritesM3U8Format`, `LoadPlaylist_ParsesM3U8_AndPopulatesItems`. *(PlaylistPersistenceTests, 8 tests.)*

### 13.4 Playlist Sorting & Filtering
- [x] `SortMode` enum (`None`/`ByName`) in `Models/SortMode.cs`. `ByDuration`/`ByDateAdded` deferred (needs populated `PlaylistItem.Duration`).
- [x] `FilterText` + `ICollectionView FilteredItems` in `PlaylistViewModel` via `CollectionViewSource`. `PlaylistBox` now binds to `PlaylistVM.FilteredItems`.
- [x] Filter TextBox + "A↓" sort toggle button above playlist panel.
- [ ] Tests: `Filter_HidesNonMatchingItems`, `Sort_ByName_OrdersAlphabetically`.

### 13.5 Drag-and-Drop Reorder
- [x] **Service support added** — `PlaylistService.MoveItem(from, to)` available.
- [x] WPF `ListBox` drag-drop visuals. *(`AllowDrop` + `PreviewMouseMove`/`Drop` on `PlaylistBox`; calls existing `MoveItem(from, to)`.)*
- [x] Tests: `MoveItem_ReordersCollection`, `MoveItem_ThrowsOnOutOfRange`. *(In PlaylistServiceTests.cs.)*

### 13.6 Item Metadata Display
- [ ] `PlaylistItem` additions: `Duration (TimeSpan)`, `ThumbnailPath`, `Title`, `Artist`. (Partial — `DurationTimeSpan` + `Artist` added.)
- [ ] Background `Task` extracts via LibVLC or `TagLib#`.
- [x] Show duration in playlist panel.
- [ ] Tests: `MetadataExtraction_PopulatesDuration`.

## Phase 14 — Session & History

### 14.1 Resume Playback (Watch History)
- [x] Basic session resume exists.
- [x] `WatchHistory<path, long>` dict in `AppSettings`. *(Saves position every 10 ticks; evicts when >100 entries; checked on session restore.)*
- [x] Prompt only if position < 95% of duration.
- [x] LRU eviction at 100 entries.
- [x] Tests: `WatchHistory_SavesPosition_OnStop`, `WatchHistory_PromptResume_IfPositionUnder95Percent`, `WatchHistory_LruEvicts_WhenOver100`. *(WatchHistoryServiceTests, 19 tests.)*

### 14.2 Recent Files with Thumbnails
- [x] Recent files list exists (capped 20).
- [ ] Extract thumbnail at 10% duration via `MediaPlayer.TakeSnapshot`.
- [ ] Store in `%AppData%\DarshanPlayer\Thumbnails\`.
- [ ] Styled popup with thumbnails.
- [ ] Tests: `RecentFiles_LimitedTo20Entries`, `ThumbnailPath_SetAfterExtraction`.

## Phase 15 — Screenshot & Clip Export

### 15.1 Screenshot
- [x] Works via `TakeScreenshotCommand`.
- [x] Use toast notification instead of `MessageBox`.
- [x] Keyboard: `Ctrl+Shift+S`.
- [ ] Tests: `Screenshot_SavesFileToExpectedPath`, `Screenshot_ShowsToastOnSuccess`.

### 15.2 GIF Export
- [ ] `ExportGifCommand` with start/end dialog.
- [ ] Uses `FFMpegCore` NuGet (deferred — need ffmpeg.exe bundled).
- [ ] Progress dialog.
- [ ] Max 30s duration.
- [ ] Tests: `GifExport_ThrowsIfDurationExceeds30Seconds`, `GifExport_CallsFFMpegWithCorrectArguments`.

## Phase 16 — Keyboard Shortcut Customization

- [ ] `ShortcutMap` in settings (action → KeyGesture).
- [ ] `IShortcutService.Register/GetGesture/Reset`.
- [ ] Settings panel: "Keyboard" tab with rebind UI + conflict detection.
- [ ] Refactor `MainWindow.Window_KeyDown` to dispatch via service.
- [ ] Tests: `ShortcutService_ReturnsMappedGesture`, `ShortcutService_DetectsConflict`, `ShortcutService_Reset_RestoresDefaults`.

## Phase 17 — Theme System

### 17.1 Built-in Themes
- [x] Dark theme exists.
- [ ] Light, OLED Black, Nord, Catppuccin Mocha, Solarized Dark.
- [ ] `IThemeService.Apply(name)` — same merged-dict swap pattern as `LanguageManager`.
- [ ] Persist active theme.
- [ ] Tests: `ThemeService_MergesDictionary_OnApply`, `ThemeService_ThrowsOnUnknownTheme`.

### 17.2 Custom Accent Color
- [ ] `AccentColor` in settings, applied via `DynamicResource`.
- [ ] Color picker in settings.
- [ ] Tests: `AccentColor_UpdatesDynamicResource`.

## Phase 18 — Network & Streaming

### 18.1 Open Network Stream
- [ ] Dialog accepts HTTP/HLS/RTSP/MMS/YouTube URLs.
- [ ] `INetworkService.OpenStream(url) → bool`.
- [ ] YouTube URLs → invoke bundled `yt-dlp.exe` (auto-downloaded to `%AppData%`).
- [ ] Add to recent on success.
- [ ] Tests: `OpenStream_AddsToRecentFiles_OnSuccess`, `OpenStream_ResolvesYouTubeUrl_ViaYtDlp`.

### 18.2 Stream Recording
- [ ] `StartRecording(path) / StopRecording()` in `MediaService` via VLC `--sout`.
- [ ] "● REC" indicator in title bar.
- [ ] Tests: `Recording_SetsVlcSoutOption`, `Recording_StopsCorrectly`.

## Phase 19 — Mini Player & System Integration

### 19.1 System Tray
- [ ] `Hardcodet.NotifyIcon.Wpf` NuGet.
- [ ] Tray menu: Play/Pause, Next, Previous, Restore, Exit.
- [ ] Setting: `MinimizeToTray`.
- [ ] Double-click restores.
- [ ] Tests: `TrayIcon_ShowsOnMinimize_WhenSettingEnabled`, `TrayIcon_RestoresWindow_OnDoubleClick`.

### 19.2 Windows Taskbar Integration
- [x] SMTC already works.
- [x] `TaskbarItemInfo` progress bar reflecting position. *(`ProgressValue` bound to `Position`; `ProgressState` = None/Normal/Paused from `TaskbarProgressState` computed prop.)*
- [x] Thumbnail toolbar buttons (`ThumbButtonInfo`). *(Prev/Play/Next `ThumbButtonInfo` in `TaskbarItemInfo` with `DismissWhenClicked="False"`.)*
- [x] Jump List — recent files visible on taskbar right-click. (`JumpList` + `JumpTask` WPF API; populate from `RecentFiles` collection.) *(JumpListService; JumpTasks not JumpPaths, which Windows hides unless the app is the default handler. Process AUMID now matches the Velopack shortcut so the list attaches.)*
- [ ] Tests: `Taskbar_Progress_UpdatesWithPosition`, `SMTC_UpdatesOnTrackChange`.

### 19.4 Default Player Registration
- [x] "Set as Default Player" button in Settings — writes `HKCU\Software\Microsoft\Windows\Shell\Associations\UrlAssociations` + triggers Windows Default Apps dialog via `LaunchUriAsync("ms-settings:defaultapps")`. *(Done differently: UrlAssociations is for URL protocols, not file types. ShellRegistration writes ProgIDs, OpenWithProgids, Applications\SupportedTypes, Capabilities + RegisteredApplications under HKCU, from Velopack install/update hooks and a startup self-repair; the button and a menu item open `ms-settings:defaultapps?registeredAppUser=DarshanPlayer`.)*
- [ ] Protocol handler `darshan://` — register in registry so `darshan://play?path=...` opens and plays file directly. *(Not done: no current caller needs it, and a URL that opens local paths is attack surface a web page could trigger.)*
- [ ] Tests: `DefaultPlayer_RegistryKeysWritten`, `ProtocolHandler_ParsesUrl`. *(Registry half done - ShellIntegrationTests; protocol half moot while the handler is not built.)*

### 19.3 Discord Rich Presence
- [ ] `DiscordRPC` NuGet integration.
- [ ] Setting `DiscordRichPresence` (default false).
- [ ] Show track title + elapsed + "darshan" large image.
- [ ] Tests: `DiscordRpc_UpdatesPresence_WhenEnabled`, `DiscordRpc_ClearsPresence_OnStop`.

## Phase 20 — Settings System Expansion

- [ ] Add every property listed in the spec's `SettingsModel` block.
- [ ] Migration: missing keys → defaults; clamp/validate on load.
- [x] Use `System.Text.Json` (already done).
- [x] **Atomic save** — tmp file + replace (done).
- [x] **Debounced save** — 500ms coalesce (done).
- [ ] Tests: `Settings_Save_WritesJson`, `Settings_Load_ReturnsDefaults_OnMissingKeys`, `Settings_Migrate_DoesNotThrow_OnOldFormat`.

## Phase 21 — Update System

- [ ] `IUpdateService.CheckForUpdateAsync` → GitHub releases API.
- [ ] `UpdateInfo { Version, ReleaseNotes, DownloadUrl }`.
- [ ] Auto-check on startup (if `AutoCheckForUpdates`).
- [ ] "Help → Check for Updates" menu.
- [ ] Tests: `UpdateService_ReturnsNull_WhenAlreadyLatest`, `UpdateService_ParsesGitHubApiResponse`.

## Phase 22 — Accessibility

- [ ] `AutomationProperties.Name` on every control.
- [ ] Logical tab order.
- [ ] `SystemParameters.HighContrast` detection.
- [ ] `AutomationPeer` announcements on play/pause.
- [ ] Tests: `AutomationProperties_AreSet_OnAllControls`.

## Phase 23 — Toast Notification Foundation

- [x] Toast host (bottom-right) implemented as a click-through `Popup` (so toasts float above the LibVLC video HWND, escaping airspace). `ToastItem` model + DataTemplate.
- [x] `INotificationService` + `NotificationService` with bounded queue (max 4) and per-toast auto-dismiss timer.
- [x] Types: Info/Success/Warning/Error with colour stripe (DataTriggers) + glyph.
- [x] Wired for: screenshot saved/failed, subtitle loaded, playback errors, file-not-found, video-adjust reset, settings save failure (`SettingsService.SaveFailed`). Replaced the corresponding `MessageBox` calls in `MainViewModel`.
- [ ] TODO: toast message strings are still English-only (not yet through `LanguageManager`); update available / recording / sleep-timer toasts pending those features.

## Phase 24 — Test Project Setup

- [x] Create `DarshanPlayer.Tests` (xUnit). Plain xUnit for now — Moq/FluentAssertions not yet added (no mock-dependent suites exist yet). Solution file `DarshanPlayer.sln` added so `dotnet test` works.
- [x] Wrap `System.IO` behind `IFileSystem` for mockability (needed before SettingsService disk tests). *(Done more narrowly: SettingsService takes an injectable directory, which is what the tests actually needed.)*
- [ ] Wrap `HttpClient` with mockable `HttpMessageHandler`.
- [ ] Coverage goal: ≥ 80% on services and ViewModels (current: PlaylistService, NotificationService, VideoAdjustment, AppSettings defaults).
- [x] Suites added: **PlaylistService** (dedup, MoveItem, remove/current, repeat/next, shuffle), **NotificationService** (queue, types, cap), **VideoAdjustment** (clamping/neutral), **AppSettings defaults**.
- [ ] Suites still needed: MediaService, SubtitleService, EqualizerService, SettingsService, ShortcutService, UpdateService, WatchHistory, MainViewModel, PlaylistViewModel.

## Phase 25 — NuGet Additions (deferred until needed)

- [ ] `CommunityToolkit.Mvvm` (8.x) — `[ObservableProperty]` source generators.
- [ ] `Hardcodet.NotifyIcon.Wpf` — system tray.
- [ ] `FFMpegCore` — GIF export.
- [ ] `TagLibSharp` — metadata extraction.
- [ ] `DiscordRichPresence` — Discord RPC.
- [ ] Compatibility caveat: project targets `net10.0-windows10.0.19041.0`. Confirm each package multi-targets or build will fail.

---

# Appendix C — Suggested Implementation Order (from spec)

1. Settings model expansion + migration
2. Toast notification system
3. Shortcut service + refactor `Window_KeyDown`
4. Theme system (Light + OLED + accent)
5. Video adjustments panel
6. Audio: EQ, delay, channel, normalize
7. Subtitle system (load, select, customize, delay)
8. Playback speed + frame step + A-B loop + chapters
9. Playlist: shuffle, repeat, persist, sort/filter, drag-reorder, metadata
10. Watch history + resume
11. Screenshot + GIF export
12. Network stream + recording
13. System tray + taskbar + SMTC enhancements
14. Discord Rich Presence
15. Update checker
16. Accessibility pass
17. Tests alongside each feature

---

# Appendix D — Non-negotiable Quality Bars (from spec)

- App compiles without warnings on `dotnet build -c Release`.
- All xUnit tests pass: `dotnet test`.
- No `NullReferenceException` during normal usage.
- `MediaPlayer` properly disposed on close and on track change.
- All `ObservableCollection` mutations on UI thread via `Application.Current.Dispatcher.Invoke`.
- Every new UI string goes through `LanguageManager` resource files (en + hi minimum).
- All commands use `RelayCommand` / `AsyncRelayCommand` with proper `CanExecute`.
- All I/O is `async Task`; never block UI thread.
- All external calls wrapped in try/catch; errors → `INotificationService.ShowError`.
