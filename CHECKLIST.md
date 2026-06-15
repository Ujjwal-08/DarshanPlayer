# Darshan Player Full Checklist

This checklist turns the current audit into an execution plan for making Darshan Player fast, reliable, modern, and ready for Indian audiences.

## Phase 0: Immediate Stabilization

- [x] Make the project build cleanly from the current local environment.
- [ ] Verify package restore and document the supported .NET SDK and Windows target.
- [ ] Add a repeatable local build command for contributors.
- [ ] Add startup diagnostics for LibVLC initialization failures.
- [ ] Add structured logging for playback, window-state changes, subtitle loading, and crashes.
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
- [ ] Keep fullscreen idle actions and playlist interactions clickable while fullscreen controls are present.
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

- [ ] Create a dedicated test project.
- [ ] Add unit tests for playlist behavior.
- [ ] Add unit tests for settings persistence.
- [ ] Add unit tests for repeat/shuffle/session restore behavior.
- [ ] Add manual QA checklists for fullscreen, PiP, minimize/restore, multi-monitor, and DPI.
- [ ] Build a media sample pack for regression testing.
- [ ] Add release validation steps before packaging MSI builds.

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
- [ ] **A21** No structured logging anywhere. Add Serilog or `Microsoft.Extensions.Logging`.
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
- [ ] `ChapterList` ObservableCollection from `MediaPlayer.ChapterDescription()`.
- [ ] Keyboard: `Ctrl+Right`/`Ctrl+Left` next/prev chapter.
- [ ] Tick marks on seek bar.
- [ ] Model: `ChapterInfo { Index, Title, Start, Duration }`.
- [ ] Tests: `ChapterList_IsPopulated_OnMediaLoaded`, `NextChapter_WrapsAround`.

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
- [ ] Spec keys: `H` shift −100ms, `J` shift +100ms, `Ctrl+H` reset. Current code uses `G`/`H` ±50ms.
- [x] Reset on new media load. *(`SubtitleDelay = 0` in `PlayRequested` handler.)*
- [ ] Tests: `SubtitleDelay_AppliedToMediaPlayer`, `SubtitleDelay_Reset_OnNewMedia`.

## Phase 11 — Audio System

### 11.1 Audio Track Selection
- [x] Working via `AudioTracks` + menu.
- [x] Reload every media change. *(`RefreshTracks()` called on `Playing` event.)*
- [x] Controls-bar ComboBox added. *(`HasMultipleAudioTracks`-gated ComboBox in controls bar.)*
- [ ] Tests: `AudioTracks_Repopulated_OnMediaChange`.

### 11.2 Audio Delay
- [x] `AudioDelay` property (−2000 to +2000ms) via `MediaPlayer.SetAudioDelay()`. Resets to 0 on new media. `AdjustAudioDelayCommand` accepts delta string.
- [ ] Keyboard: conflicts with subtitle delay keys — deferred.
- [ ] Tests: `AudioDelay_AppliedToMediaPlayer`.

### 11.3 Equalizer
- [ ] Model `EqualizerProfile { Name, PreAmp, Bands[10] }`.
- [ ] Presets: Flat, Bass Boost, Vocal Boost, Classical, Dance, Pop, Rock, Podcast.
- [ ] `IEqualizerService.Apply/Reset/SaveCustom`.
- [ ] WPF panel: 10 vertical sliders (−20 to +20 dB), PreAmp slider.
- [ ] Apply via `LibVLC.Equalizer` API.
- [ ] Persist active preset + custom presets.
- [ ] Tests: `Equalizer_Apply_SetsAllBandValues`, `SaveCustomPreset_PersistsToSettings`, `LoadPreset_RestoresBandValues`.

### 11.4 Audio Normalization
- [ ] `NormalizeAudio` bool → VLC `--audio-filter=compressor`.
- [ ] Toggle button in audio menu.
- [ ] Tests: `AudioNormalization_SetsVlcAudioFilter`.

### 11.5 Stereo / Mono / Surround Mix
- [ ] `AudioChannelMode` enum → `MediaPlayer.AudioChannel`.
- [ ] Picker in audio menu.
- [ ] Tests: `AudioChannel_SetCorrectly_ForEachMode`.

## Phase 12 — Video Adjustments

### 12.1 Adjustments Panel
- [x] Brightness (0–2), Contrast (0–2), Saturation (0–3), Gamma (0.01–10), Hue (−180 to +180). Sharpness deferred (separate `sharpen` module, not part of the `adjust` filter).
- [x] Apply via VLC `adjust` filter (`SetAdjustInt(Enable,1)` + `SetAdjustFloat(...)`), re-applied on the `Playing` event since VLC resets it per media. Values persisted in `AppSettings` and clamped in `Models/VideoAdjustment`.
- [x] "Reset All" button (`ResetVideoAdjustmentsCommand`).
- [x] Tests: `ClampBrightness/Saturation/Gamma/Hue_BoundsToRange`, `IsNeutral_*` (in `VideoAdjustmentTests`). `VideoAdjust_AppliedToMediaPlayer` integration test deferred (needs a mockable IMediaService).

### 12.2 Aspect Ratio
- [x] Existing menu (Default/16:9/4:3/1:1/21:9/Fill).
- [ ] Add `Stretch` mode per spec.
- [ ] Keyboard: `A` cycles ratios (conflicts with A-B loop key — pick one).
- [ ] Tests: `AspectRatio_SetsMediaPlayerAspectRatioString`.

### 12.3 Crop
- [ ] `CropMode` enum (12 values).
- [ ] Apply via `MediaPlayer.CropGeometry`.
- [ ] Tests: `Crop_SetsMediaPlayerCropGeometry`.

### 12.4 Zoom
- [x] `Zoom` 0.25–4.0 via `MediaPlayer.Scale`. Persisted to `AppSettings.VideoScale`. `VideoScale` added to `IMediaService`.
- [x] Keyboard: `+` zoom in, `-` zoom out, `*` reset (numpad).
- [ ] Tests: `Zoom_ClampsAtBounds`.

### 12.5 Rotation
- [ ] 0/90/180/270 via VLC `transform` filter.
- [ ] Tests: `Rotation_SetsTransformFilter`.

### 12.6 Deinterlace
- [ ] Modes: Off/Discard/Blend/Mean/Bob/Linear/X/Yadif/Yadif2x.
- [ ] Apply via `MediaPlayer.Deinterlace`.
- [ ] Tests: `Deinterlace_SetsMode`.

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
- [ ] Auto-save last playlist on close, restore on startup.
- [ ] Tests: `SavePlaylist_WritesM3U8Format`, `LoadPlaylist_ParsesM3U8_AndPopulatesItems`.

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
- [ ] Show duration in playlist panel.
- [ ] Tests: `MetadataExtraction_PopulatesDuration`.

## Phase 14 — Session & History

### 14.1 Resume Playback (Watch History)
- [x] Basic session resume exists.
- [x] `WatchHistory<path, long>` dict in `AppSettings`. *(Saves position every 10 ticks; evicts when >100 entries; checked on session restore.)*
- [ ] Prompt only if position < 95% of duration.
- [ ] LRU eviction at 100 entries.
- [ ] Tests: `WatchHistory_SavesPosition_OnStop`, `WatchHistory_PromptResume_IfPositionUnder95Percent`, `WatchHistory_LruEvicts_WhenOver100`.

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
- [ ] Jump List — recent files visible on taskbar right-click. (`JumpList` + `JumpTask` WPF API; populate from `RecentFiles` collection.)
- [ ] Tests: `Taskbar_Progress_UpdatesWithPosition`, `SMTC_UpdatesOnTrackChange`.

### 19.4 Default Player Registration
- [ ] "Set as Default Player" button in Settings — writes `HKCU\Software\Microsoft\Windows\Shell\Associations\UrlAssociations` + triggers Windows Default Apps dialog via `LaunchUriAsync("ms-settings:defaultapps")`.
- [ ] Protocol handler `darshan://` — register in registry so `darshan://play?path=...` opens and plays file directly.
- [ ] Tests: `DefaultPlayer_RegistryKeysWritten`, `ProtocolHandler_ParsesUrl`.

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
- [ ] Wrap `System.IO` behind `IFileSystem` for mockability (needed before SettingsService disk tests).
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
