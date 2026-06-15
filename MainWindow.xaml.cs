using DarshanPlayer.Services;
using DarshanPlayer.ViewModels;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using DragEventArgs = System.Windows.DragEventArgs;
using Brushes = System.Windows.Media.Brushes;
using ColorConverter = System.Windows.Media.ColorConverter;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
using System.Windows.Controls.Primitives;
using System.Windows.Shell;
using System.Windows.Interop;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using Windows.Media;

namespace DarshanPlayer
{
    public partial class MainWindow : Window
    {
        private MainViewModel _vm = null!;
        private DispatcherTimer _hideControlsTimer;
        private SystemMediaTransportControls? _smtc;
        private HwndHost? _vlcHost; // host inside LibVLCSharp VideoView
        private FullscreenControlsWindow? _fullscreenControlsWindow;
        
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_NCMOUSEMOVE = 0x00A0;
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int WM_HSCROLL = 0x0114;
        private const int WM_VSCROLL = 0x0115;
        private const int VK_ESCAPE = 0x1B;
        private const int MONITOR_DEFAULTTONEAREST = 2;
        private const int HTCAPTION = 2;

        private WindowState _lastWindowState;
        private WindowState _restoreWindowState = WindowState.Normal;
        private double _preWidth;
        private double _preHeight;
        private double _preLeft;
        private double _preTop;
        private bool _wasAlwaysOnTop;
        private WindowChrome? _savedChrome;
        private bool _isApplyingFullscreenTransition;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left; public int Top; public int Right; public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetMessageTime();

        [DllImport("user32.dll")]
        private static extern uint GetDoubleClickTime();

        private delegate bool EnumWindowProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_FRAMECHANGED = 0x0020;

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8) return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else return SetWindowLong32(hWnd, nIndex, dwNewLong);
        }

        private const int GWL_WNDPROC = -4;
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private DispatcherTimer? _hookRetryTimer;
        private uint _lastClickTime = 0;
        private IntPtr _lastClickHwnd = IntPtr.Zero;

        [Flags]
        private enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS        = 0x80000000,
            ES_DISPLAY_REQUIRED  = 0x00000002,
            ES_SYSTEM_REQUIRED   = 0x00000001
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        [ComImport, Guid("ddb0472d-c911-4a1f-86d9-dc3d71a95f5a"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ISystemMediaTransportControlsInterop
        {
            SystemMediaTransportControls GetForWindow(IntPtr appWindow, [In] ref Guid riid);
        }

        [DllImport("combase.dll", PreserveSig = false)]
        private static extern void RoGetActivationFactory([MarshalAs(UnmanagedType.HString)] string activatableClassId, [In] ref Guid iid, [MarshalAs(UnmanagedType.IUnknown, IidParameterIndex = 1)] out object factory);

        public MainWindow()
        {
            InitializeComponent();

            var media    = ServiceLocator.MediaService!;
            var playlist = ServiceLocator.PlaylistService!;
            var settings = ServiceLocator.SettingsService!;
            var lang     = ServiceLocator.LanguageManager!;

            var notifications = ServiceLocator.Notifications!;
            _vm = new MainViewModel(media, playlist, settings, lang, notifications);
            _vm.PlaylistVM = new PlaylistViewModel(playlist, settings);

            DataContext = _vm;
            VideoPlayer.MediaPlayer = media.MediaPlayer;
            media.MediaPlayerRecreated += (_, _) =>
                Dispatcher.Invoke(() => VideoPlayer.MediaPlayer = media.MediaPlayer);

            // Hide VideoPlayer + show dark overlay during subtitle reload, restore after resume
            _vm.SubtitleReloadRequested += (_, _) => Dispatcher.Invoke(ShowSubtitleApplyOverlay);
            media.PlaybackResumedAfterRecreation += (_, _) =>
                Dispatcher.Invoke(HideSubtitleApplyOverlay);

            ApplySavedWindowPlacement(settings.Current);
            UseLayoutRounding = true;
            lang.Apply(settings.Current.Language);

            _vm.PropertyChanged += VmOnPropertyChanged;

            _hideControlsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            _hideControlsTimer.Tick += HideControlsTimer_Tick;

            _hookRetryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _hookRetryTimer.Tick += (s, e) => { if (_vm.IsPlaying && _vlcHost == null) HookHwndHost(); };
            
            VideoPlayer.Loaded += (s, e) => HookHwndHost();

            this.SourceInitialized += (s, e) =>
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                HwndSource.FromHwnd(hwnd)?.AddHook(HwndHook);

                try
                {
                    Guid iidInterop = typeof(ISystemMediaTransportControlsInterop).GUID;
                    RoGetActivationFactory("Windows.Media.SystemMediaTransportControls", ref iidInterop, out object factory);
                    var interop = (ISystemMediaTransportControlsInterop)factory;
                    
                    Guid iidSmtc = typeof(SystemMediaTransportControls).GetInterface("ISystemMediaTransportControls")!.GUID;
                    _smtc = interop.GetForWindow(hwnd, ref iidSmtc);

                    _smtc.IsPlayEnabled = true;
                    _smtc.IsPauseEnabled = true;
                    _smtc.IsNextEnabled = true;
                    _smtc.IsPreviousEnabled = true;
                    _smtc.ButtonPressed += Smtc_ButtonPressed;
                }
                catch (Exception ex)
                {
                    // SMTC unavailable (older Windows, no media runtime, COM activation failed).
                    // Not fatal — the app still works without lock-screen media controls.
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] SMTC init failed: {ex.Message}");
                }
            };

            this.SizeChanged += MainWindow_SizeChanged;
            this.LocationChanged += MainWindow_LocationChanged;
            _lastWindowState = WindowState;

            this.Loaded += MainWindow_Loaded;

            // initialize alias to the VideoPlayer host after loaded
            Loaded += (s, e) =>
            {
                _vlcHost = FindVisualChild<HwndHost>(VideoPlayer);
            };

            Closed += (_, _) =>
            {
                _fullscreenControlsWindow?.Close();
                _fullscreenControlsWindow = null;
            };
        }

        private void VideoArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_vm.IsPipMode && e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    ExitPipMode();
                    e.Handled = true;
                    return;
                }

                if (!IsOverInteractiveControl(e.OriginalSource as DependencyObject))
                {
                    BeginNativeWindowDrag();
                    e.Handled = true;
                    return;
                }
            }

            TryToggleFullscreenFromDoubleClick(e, e.OriginalSource as DependencyObject);
        }

        private void HookHwndHost()
        {
            if (_vlcHost != null) return;
            _vlcHost = FindVisualChild<HwndHost>(VideoPlayer);
            if (_vlcHost != null)
            {
                _vlcHost.MessageHook += VideoHost_MessageHook;
            }
        }

        private IntPtr VideoHost_MessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_LBUTTONDOWN = 0x0201;

            if (msg == WM_LBUTTONDBLCLK || msg == WM_LBUTTONDOWN)
            {
                uint currentTime = GetMessageTime();
                bool isDoubleClick = (msg == WM_LBUTTONDBLCLK);

                if (!isDoubleClick && hwnd == _lastClickHwnd && (currentTime - _lastClickTime) < GetDoubleClickTime())
                {
                    isDoubleClick = true;
                }

                if (isDoubleClick)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (_vm.IsPipMode)
                        {
                            ExitPipMode();
                        }
                        else
                        {
                            ToggleFullscreen();
                        }
                    }));
                    _lastClickTime = 0;
                    handled = true;
                    return IntPtr.Zero;
                }

                if (msg == WM_LBUTTONDOWN)
                {
                    // A17: IsPipMode is a simple bool field read — safe here; all writes go through Dispatcher
                    if (_vm.IsPipMode)
                    {
                        BeginNativeWindowDrag();
                        handled = true;
                        return IntPtr.Zero;
                    }

                    _lastClickTime = currentTime;
                    _lastClickHwnd = hwnd;
                }
            }
            else if (msg == WM_MOUSEMOVE)
            {
                Dispatcher.BeginInvoke(new Action(WakeControls));
            }

            return IntPtr.Zero;
        }

        private T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child is T t) return t;
                T? childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null) return childOfChild;
            }
            return null;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var workArea = SystemParameters.WorkArea;
            if (this.Width > workArea.Width) this.Width = workArea.Width * 0.9;
            if (this.Height > workArea.Height) this.Height = workArea.Height * 0.9;
            this.Left = workArea.Left + (workArea.Width - this.Width) / 2;
            this.Top = workArea.Top + (workArea.Height - this.Height) / 2;

            if (Application.Current.Properties["Args"] is string[] args && args.Length > 0)
            {
                Application.Current.Properties.Remove("Args");

                Action playAction = () =>
                {
                    try { _vm.AddDroppedFiles(args); }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not open file:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                };

                if (VideoPlayer.IsLoaded)
                    Dispatcher.InvokeAsync(playAction, DispatcherPriority.ContextIdle);
                else
                    VideoPlayer.Loaded += (s, ev) => Dispatcher.InvokeAsync(playAction, DispatcherPriority.ContextIdle);
            }

            ApplyPlaylistColumns();
        }



        private void Smtc_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            Dispatcher.InvokeAsync(() =>
            {
                switch (args.Button)
                {
                    case SystemMediaTransportControlsButton.Play:
                    case SystemMediaTransportControlsButton.Pause:
                        if (_vm.PlayPauseCommand.CanExecute(null)) _vm.PlayPauseCommand.Execute(null);
                        break;
                    case SystemMediaTransportControlsButton.Next:
                        if (_vm.NextTrackCommand.CanExecute(null)) _vm.NextTrackCommand.Execute(null);
                        break;
                    case SystemMediaTransportControlsButton.Previous:
                        if (_vm.PrevTrackCommand.CanExecute(null)) _vm.PrevTrackCommand.Execute(null);
                        break;
                }
            });
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // ESC Handling
            if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
            {
                if (wParam.ToInt32() == VK_ESCAPE)
                {
                    if (_vm.IsPipMode || _vm.IsFullscreen)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (_vm.IsPipMode) ExitPipMode();
                            else if (_vm.IsFullscreen) _vm.IsFullscreen = false;
                        }));
                        handled = true;
                    }
                }
            }
            // Mouse Wake Handling
            else if (msg == WM_MOUSEMOVE || msg == WM_NCMOUSEMOVE)
            {
                Dispatcher.BeginInvoke(new Action(WakeControls));
            }
            return IntPtr.Zero;
        }

        private void WakeControls()
        {
            StopCursorPolling();
            Mouse.OverrideCursor = null;

            if (!_vm.IsFullscreen && !_vm.IsPipMode)
            {
                HideFullscreenControlsWindow();
                PipPopup.IsOpen = false;
                PipOverlay.Visibility = Visibility.Collapsed;
                PipCloseButton.Visibility = Visibility.Collapsed;
                PipHoverHandle.Visibility = Visibility.Collapsed;
                ControlsBar.Visibility = Visibility.Visible;
                return;
            }

            if (_vm.IsFullscreen)
            {
                PipPopup.IsOpen = false;
                PipOverlay.Visibility = Visibility.Collapsed;
                PipCloseButton.Visibility = Visibility.Collapsed;
                PipHoverHandle.Visibility = Visibility.Collapsed;
                ControlsBar.Visibility = Visibility.Collapsed;
                ShowFullscreenControls();
            }
            else
            {
                HideFullscreenControlsWindow();
                PipPopup.HorizontalOffset = 0;
                PipPopup.VerticalOffset = 0;
                PipPopup.IsOpen = true;
                PipOverlay.Visibility = Visibility.Visible;
                PipCloseButton.Visibility = Visibility.Visible;
                PipHoverHandle.Visibility = Visibility.Visible;
                ControlsBar.Visibility = Visibility.Collapsed;
            }

            _hideControlsTimer.Stop();
            _hideControlsTimer.Start();
        }

        private void ShowFullscreenControls(bool forceReopen = false)
        {
            if (!_vm.IsFullscreen)
                return;

            EnsureFullscreenControlsWindow();
            // Show first so the window enters the z-order, then SetWindowPos(HWND_TOPMOST) works
            if (_fullscreenControlsWindow is { IsVisible: false })
                _fullscreenControlsWindow.Show();
            UpdateFullscreenControlsWindowBounds(); // calls SetWindowPos(HWND_TOPMOST) to float above main window
            _fullscreenControlsWindow?.SetControlsVisible(true);
        }

        private void EnsureFullscreenControlsWindow()
        {
            if (_fullscreenControlsWindow != null)
                return;

            _fullscreenControlsWindow = new FullscreenControlsWindow(_vm)
            {
                Owner = this
            };
            _fullscreenControlsWindow.ActivityDetected += (_, _) => Dispatcher.BeginInvoke(new Action(WakeControls));
        }

        private void HideFullscreenControlsWindow()
        {
            if (_fullscreenControlsWindow?.IsVisible == true)
            {
                _fullscreenControlsWindow.Hide();
            }
        }

        private void UpdateFullscreenControlsWindowBounds()
        {
            if (_fullscreenControlsWindow == null || !_vm.IsFullscreen)
                return;

            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
                return;

            var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            MONITORINFO mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
            if (!GetMonitorInfo(monitor, ref mi))
                return;

            Rect monitorBounds = GetMonitorBoundsInDip(mi.rcMonitor);
            double windowWidth = Math.Min(1080, Math.Max(520, monitorBounds.Width - 80));
            windowWidth = Math.Min(windowWidth, Math.Max(420, monitorBounds.Width - 16));
            double windowHeight = 150;
            _fullscreenControlsWindow.Width = windowWidth;
            _fullscreenControlsWindow.Height = windowHeight;
            _fullscreenControlsWindow.Left = monitorBounds.Left + ((monitorBounds.Width - windowWidth) / 2);
            _fullscreenControlsWindow.Top = monitorBounds.Bottom - windowHeight - 28;

            var overlayHwnd = new WindowInteropHelper(_fullscreenControlsWindow).Handle;
            if (overlayHwnd != IntPtr.Zero)
            {
                SetWindowPos(
                    overlayHwnd,
                    HWND_TOPMOST,
                    0,
                    0,
                    0,
                    0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW | SWP_FRAMECHANGED);
            }
        }

        private void ApplyNativeFullscreenZOrder()
        {
            if (!_vm.IsFullscreen)
                return;

            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
                return;

            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }

        private void ApplyNativeMonitorBounds(RECT monitorRect, bool topmost)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
                return;

            SetWindowPos(
                hwnd,
                topmost ? HWND_TOPMOST : HWND_NOTOPMOST,
                monitorRect.Left,
                monitorRect.Top,
                monitorRect.Right - monitorRect.Left,
                monitorRect.Bottom - monitorRect.Top,
                SWP_SHOWWINDOW | SWP_FRAMECHANGED);
        }

        private void HideControlsTimer_Tick(object? sender, EventArgs e)
        {
            _hideControlsTimer.Stop();

            if (_vm.IsFullscreen)
            {
                if (_vm.IsPlaying)
                {
                    // Don't hide controls if the cursor is currently hovering over them — restart the timer instead
                    if (_fullscreenControlsWindow != null && IsCursorOverWindow(_fullscreenControlsWindow))
                    {
                        _hideControlsTimer.Start();
                        return;
                    }
                    // Fade out via opacity rather than Hide() so z-order is preserved for the next Show
                    _fullscreenControlsWindow?.SetControlsVisible(false);
                    Mouse.OverrideCursor = Cursors.None;
                    StartCursorPolling(); // VideoHost_MessageHook never gets mouse moves from LibVLC's deep child HWND
                }
                else
                {
                    ShowFullscreenControls();
                    Mouse.OverrideCursor = null;
                }
            }
            else
            {
                HideFullscreenControlsWindow();
            }

            if (_vm.IsPipMode)
            {
                PipPopup.IsOpen = true;
                PipOverlay.Visibility = Visibility.Collapsed;
                PipCloseButton.Visibility = Visibility.Collapsed;
                PipHoverHandle.Visibility = Visibility.Collapsed;
            }
            else
            {
                PipPopup.IsOpen = false;
                PipOverlay.Visibility = Visibility.Collapsed;
                PipCloseButton.Visibility = Visibility.Collapsed;
                PipHoverHandle.Visibility = Visibility.Collapsed;
            }

        }
        private void Window_MouseEnter(object sender, MouseEventArgs e) => WakeControls();
        private void Root_MouseEnter(object sender, MouseEventArgs e) => WakeControls();
        
        private void VideoArea_MouseEnter(object sender, MouseEventArgs e) => WakeControls();

        private void Root_MouseMove(object sender, MouseEventArgs e) => WakeControls();
        private void Root_PreviewKeyDown(object sender, KeyEventArgs e) => WakeControls();
        private void VideoArea_MouseMove(object sender, MouseEventArgs e) => WakeControls();

      
        
        private void OverlayEventGrid_MouseMove(object sender, MouseEventArgs e) 
        {
            // If we are moving inside the popup, keep it awake
            WakeControls();
        }
        
        private void OverlayEventGrid_Click(object sender, MouseButtonEventArgs e) => VideoArea_Click(sender, e);
        private void VmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsFullscreen))
                ApplyFullscreen(_vm.IsFullscreen);
            else if (e.PropertyName == nameof(MainViewModel.IsPlaying))
            {
                UpdateExecutionState(_vm.IsPlaying);
                if (_vm.IsPlaying) _hookRetryTimer?.Start();
                else _hookRetryTimer?.Stop();

                if (_vm.IsFullscreen)
                {
                    if (_vm.IsPlaying)
                    {
                        WakeControls();
                    }
                    else
                    {
                        ShowFullscreenControls();
                        Mouse.OverrideCursor = null;
                    }
                }

                if (_smtc != null)
                {
                    _smtc.PlaybackStatus = _vm.IsPlaying ? MediaPlaybackStatus.Playing : MediaPlaybackStatus.Paused;
                }
            }
            else if (e.PropertyName == nameof(MainViewModel.AlwaysOnTop))
            {
                UpdateEffectiveTopmost();
            }
            else if (e.PropertyName == nameof(MainViewModel.CurrentTitle))
            {
                HookHwndHost();
                if (_smtc != null)
                {
                    var updater = _smtc.DisplayUpdater;
                    updater.Type = MediaPlaybackType.Video;
                    updater.VideoProperties.Title = _vm.CurrentTitle;
                    updater.Update();
                }
            }
            else if (e.PropertyName is nameof(MainViewModel.ABPointAFraction)
                                    or nameof(MainViewModel.ABPointBFraction)
                                    or nameof(MainViewModel.Duration))
            {
                UpdateAbMarkers();
            }
            else if (e.PropertyName == nameof(MainViewModel.IsPlaylistVisible))
            {
                Dispatcher.Invoke(ApplyPlaylistColumns);
            }
        }

        private void ApplyFullscreen(bool full)
        {
            _isApplyingFullscreenTransition = true;

            try
            {
            if (full)
            {
                if (_vm.IsPipMode) ExitPipMode();
                _restoreWindowState = WindowState == WindowState.Minimized ? WindowState.Normal : WindowState;
                _preWidth = Width; _preHeight = Height; _preLeft = Left; _preTop = Top;

                _savedChrome = WindowChrome.GetWindowChrome(this);
                WindowChrome.SetWindowChrome(this, null);

                // Set Background to pure black for immersive fullscreen
                this.Background = Brushes.Black;

                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Normal;

                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

                // Clear WPF size constraints BEFORE repositioning so they never fight Win32
                MaxWidth = double.PositiveInfinity;
                MaxHeight = double.PositiveInfinity;

                // GetMonitorInfo.rcMonitor gives guaranteed raw physical-pixel bounds (no DPI scaling).
                // Screen.Bounds from WinForms can be DPI-adjusted and mis-matches SetWindowPos units.
                var hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                MONITORINFO mi = new MONITORINFO { cbSize = Marshal.SizeOf(typeof(MONITORINFO)) };
                GetMonitorInfo(hMonitor, ref mi);
                int monX = mi.rcMonitor.Left;
                int monY = mi.rcMonitor.Top;
                int monW = mi.rcMonitor.Right  - mi.rcMonitor.Left;
                int monH = mi.rcMonitor.Bottom - mi.rcMonitor.Top;

                // Sync WPF properties (DIPs) so layout engine agrees with Win32 size
                var source = PresentationSource.FromVisual(this);
                double scaleX = source?.CompositionTarget?.TransformFromDevice.M11 ?? 1.0;
                double scaleY = source?.CompositionTarget?.TransformFromDevice.M22 ?? 1.0;
                Left   = monX * scaleX;
                Top    = monY * scaleY;
                Width  = monW * scaleX;
                Height = monH * scaleY;

                // Drive Win32 with raw physical-pixel values — covers taskbar, no rounding gap
                SetWindowPos(hwnd, HWND_TOPMOST,
                    monX, monY, monW, monH,
                    SWP_SHOWWINDOW | SWP_FRAMECHANGED);

                UpdateCompactLayout(isCompact: true);
                UpdateEffectiveTopmost();
                TitleRow.Height = ControlsRow.Height = new GridLength(0);

                UpdateLayout();

                ShowFullscreenControls(forceReopen: true);
                WakeControls();

                this.Activate();
                this.Focus();
                Keyboard.Focus(this);

                _hideControlsTimer.Start();
                Dispatcher.InvokeAsync(() =>
                {
                    UpdateEffectiveTopmost();
                    Activate();
                    RefreshOverlayPopups();
                    ShowFullscreenControls(forceReopen: true);
                    UpdateFullscreenControlsWindowBounds();
                }, DispatcherPriority.ApplicationIdle);
            }
            else
            {
                // Restore dimensions to allow normal window behavior
                MaxWidth = MaxHeight = double.PositiveInfinity;
                
                // Reset Style before State to prevent Taskbar visual bugs
                if (_savedChrome != null) WindowChrome.SetWindowChrome(this, _savedChrome);
                
                // Restore safe background color directly (avoids "White Screen" resource lookup bug)
                this.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111111"));
                
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.CanResize;
                WindowState = WindowState.Normal;

                RestoreWindowPlacement();
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW | SWP_FRAMECHANGED);
                }
                UpdateCompactLayout(isCompact: false);
                StopCursorPolling();
                HideFullscreenControlsWindow();
                PipPopup.IsOpen = false;
                PipHoverHandle.Visibility = Visibility.Collapsed;
                Mouse.OverrideCursor = null;
                _hideControlsTimer.Stop();
                UpdateEffectiveTopmost();
            }
            }
            finally
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    _isApplyingFullscreenTransition = false;
                }), DispatcherPriority.ApplicationIdle);
            }
        }
        private void EnterPipMode()
        {
            if (_vm.IsPipMode) return;

            _restoreWindowState = WindowState == WindowState.Minimized ? WindowState.Normal : WindowState;
            _preWidth = Width;
            _preHeight = Height;
            _preLeft = Left;
            _preTop = Top;

            _vm.IsPipMode = true;
            _wasAlwaysOnTop = _vm.AlwaysOnTop;

            WindowState = WindowState.Normal;
            // A18: scale PiP to ~22% of work area so it looks proportional on all screen sizes/DPIs
            var wa = SystemParameters.WorkArea;
            Width  = Math.Max(320, wa.Width  * 0.22);
            Height = Math.Max(180, wa.Height * 0.22);
            Left = wa.Right - Width - 20;
            Top = wa.Bottom - Height - 20;

            UpdateCompactLayout(isCompact: true);
            _vm.EnterPipScale(); // Scale=0 → LibVLC auto-fits video to PiP window size

            // Refresh the popup/overlay layout after shrinking the window.
            UpdateLayout();
            WakeControls();

            UpdateEffectiveTopmost();
            Activate();
            Keyboard.Focus(this);
            _hideControlsTimer.Start();
        }

        private void ExitPipMode()
        {
            if (!_vm.IsPipMode) return;

            _vm.IsPipMode = false;
            _vm.AlwaysOnTop = _wasAlwaysOnTop;

            WindowState = WindowState.Normal;

            RestoreWindowPlacement();
            UpdateCompactLayout(isCompact: false);
            _vm.ExitPipScale(); // restore user's zoom setting

            HideFullscreenControlsWindow();
            PipPopup.IsOpen = false;
            PipHoverHandle.Visibility = Visibility.Collapsed;

            Mouse.OverrideCursor = null;
            UpdateEffectiveTopmost();
            Activate();
            _hideControlsTimer.Stop();
        }

        private void RestoreWindowPlacement()
        {
            if (_restoreWindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Maximized;
                return;
            }

            if (_preWidth > 0) Width = _preWidth;
            if (_preHeight > 0) Height = _preHeight;

            if (_preLeft > 0 || _preTop > 0)
            {
                Left = _preLeft;
                Top = _preTop;
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        private void ApplySavedWindowPlacement(Models.AppSettings settings)
        {
            Width = Math.Max(1100, settings.WindowWidth);
            Height = Math.Max(700, settings.WindowHeight);

            if (settings.WindowLeft.HasValue && settings.WindowTop.HasValue)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                // Clamp to WorkArea so an off-screen saved position always restores on-screen (A16)
                var wa = SystemParameters.WorkArea;
                Left = Math.Max(wa.Left, Math.Min(settings.WindowLeft.Value, wa.Right - 200));
                Top  = Math.Max(wa.Top,  Math.Min(settings.WindowTop.Value,  wa.Bottom - 100));
            }
            else
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            if (settings.StartMaximized)
            {
                Loaded += (_, _) => WindowState = WindowState.Maximized;
            }
        }

        // ── Subtitle reload overlay — hides VideoView during LibVLC recreation to prevent green screen ──
        private bool _isReloadingSubtitles = false;

        // ── Cursor-position polling (used in fullscreen when controls are hidden) ──
        private System.Drawing.Point _lastCursorPos;
        private DispatcherTimer? _cursorPollTimer;

        private void ShowSubtitleApplyOverlay()
        {
            if (_isReloadingSubtitles) return;  // guard: ignore if already in progress
            _isReloadingSubtitles = true;
            VideoPlayer.Visibility = Visibility.Collapsed;
            SubtitleApplyOverlay.Visibility = Visibility.Visible;
        }

        private void StartCursorPolling()
        {
            _lastCursorPos = System.Windows.Forms.Cursor.Position;
            if (_cursorPollTimer == null)
            {
                _cursorPollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
                _cursorPollTimer.Tick += (_, _) =>
                {
                    var pos = System.Windows.Forms.Cursor.Position;
                    if (pos != _lastCursorPos)
                    {
                        _lastCursorPos = pos;
                        WakeControls();
                    }
                };
            }
            _cursorPollTimer.Start();
        }

        private void StopCursorPolling() => _cursorPollTimer?.Stop();

        private void HideSubtitleApplyOverlay()
        {
            // Short delay so new MediaPlayer has time to attach to HWND before we show it
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                VideoPlayer.Visibility = Visibility.Visible;
                SubtitleApplyOverlay.Visibility = Visibility.Collapsed;
                _isReloadingSubtitles = false;
            };
            timer.Start();
        }

        // ── Playlist column management ──────────────────────────────────────
        private GridLength _savedPlaylistWidth = new GridLength(260);

        private void ApplyPlaylistColumns()
        {
            bool show = _vm.IsPlaylistVisible && !_vm.IsPipMode && !_vm.IsFullscreen;
            var col1 = VideoPlaylistGrid.ColumnDefinitions[1]; // splitter
            var col2 = VideoPlaylistGrid.ColumnDefinitions[2]; // playlist
            if (show)
            {
                col1.Width = new GridLength(5);
                col2.Width = _savedPlaylistWidth;
                col2.MinWidth = 220;
                col2.MaxWidth = 500;
            }
            else
            {
                if (col2.Width.Value > 0) _savedPlaylistWidth = col2.Width;
                col1.Width = new GridLength(0);
                col2.Width = new GridLength(0);
                col2.MinWidth = 0;
                col2.MaxWidth = double.PositiveInfinity;
            }
        }

        private void UpdateCompactLayout(bool isCompact)
        {
            TitleBar.Visibility = isCompact ? Visibility.Collapsed : Visibility.Visible;
            ControlsBar.Visibility = isCompact ? Visibility.Collapsed : Visibility.Visible;
            TitleRow.Height = isCompact ? new GridLength(0) : new GridLength(40);
            ControlsRow.Height = isCompact ? new GridLength(0) : GridLength.Auto;
            ApplyPlaylistColumns();
        }

        private void UpdateEffectiveTopmost()
        {
            bool wantTopmost = _vm.IsFullscreen || _vm.AlwaysOnTop || _vm.IsPipMode;
            Topmost = wantTopmost;
            // Explicitly demote Z-order so apps launched while we were topmost aren't stuck behind us
            if (!wantTopmost)
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                    SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
        }
        private void BtnCloseMetadata_Click(object sender, RoutedEventArgs e)
        {
            _vm.IsMetadataVisible = false;
        }
        private void UpdateExecutionState(bool isPlaying)
        {
            SetThreadExecutionState(isPlaying 
                ? (EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_DISPLAY_REQUIRED | EXECUTION_STATE.ES_SYSTEM_REQUIRED)
                : EXECUTION_STATE.ES_CONTINUOUS);
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateFullscreenControlsWindowBounds();
            RefreshOverlayPopups();

            // Update the viewmodel so we don't destroy the WPF UI binding
            if (ActualWidth < 900 && _vm.IsPlaylistVisible)
            {
                _vm.IsPlaylistVisible = false;
            }
        }

        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            UpdateFullscreenControlsWindowBounds();
            RefreshOverlayPopups();
        }

        private void RefreshOverlayPopups()
        {
            RefreshPopupPlacement(PipPopup);
        }

        private Rect GetMonitorBoundsInDip(RECT monitorRect)
        {
            var source = PresentationSource.FromVisual(this);
            var transform = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;

            Point topLeft = transform.Transform(new Point(monitorRect.Left, monitorRect.Top));
            Point bottomRight = transform.Transform(new Point(monitorRect.Right, monitorRect.Bottom));

            return new Rect(topLeft, bottomRight);
        }

        private static void RefreshPopupPlacement(Popup popup)
        {
            if (!popup.IsOpen)
                return;

            popup.HorizontalOffset += 0.1;
            popup.HorizontalOffset -= 0.1;
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnPip_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.IsPipMode)
            {
                ExitPipMode();
                return;
            }

            if (_vm.IsFullscreen)
            {
                _vm.IsFullscreen = false;
            }

            EnterPipMode();
        }

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.IsPipMode) 
            {
                ExitPipMode();
            }
            else if (_vm.IsFullscreen)
            {
                _vm.IsFullscreen = false;
            }
            else 
            {
                WindowState = (WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
            }
        }

        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.IsPipMode) ExitPipMode();
            else if (_vm.IsFullscreen) _vm.IsFullscreen = false;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            ServiceLocator.SettingsService?.Save();
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _hookRetryTimer?.Stop();
            if (_vlcHost != null) _vlcHost.MessageHook -= VideoHost_MessageHook;

            var s = ServiceLocator.SettingsService!;
            SaveWindowPlacement(s.Current);

            // Save final playback position before closing
            if (_vm.IsPlaying && !string.IsNullOrEmpty(s.Current.LastPlayedFile))
            {
                s.Current.LastPlaybackPosition = _vm.CurrentMs;
            }

            s.Save();
            _vm.Dispose();
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
            base.OnClosing(e);
        }

        private void SaveWindowPlacement(Models.AppSettings settings)
        {
            if (_vm.IsFullscreen || _vm.IsPipMode)
            {
                settings.WindowWidth = _preWidth > 0 ? _preWidth : settings.WindowWidth;
                settings.WindowHeight = _preHeight > 0 ? _preHeight : settings.WindowHeight;
                settings.WindowLeft = (_preLeft > 0 || _preTop > 0) ? _preLeft : settings.WindowLeft;
                settings.WindowTop = (_preLeft > 0 || _preTop > 0) ? _preTop : settings.WindowTop;
                settings.StartMaximized = _restoreWindowState == WindowState.Maximized;
                return;
            }

            if (WindowState == WindowState.Maximized)
            {
                settings.WindowWidth = Math.Max(1100, RestoreBounds.Width);
                settings.WindowHeight = Math.Max(700, RestoreBounds.Height);
                settings.WindowLeft = RestoreBounds.Left;
                settings.WindowTop = RestoreBounds.Top;
                settings.StartMaximized = true;
                return;
            }

            Rect bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, ActualWidth, ActualHeight) : RestoreBounds;
            settings.WindowWidth = Math.Max(1100, bounds.Width);
            settings.WindowHeight = Math.Max(700, bounds.Height);
            settings.WindowLeft = bounds.Left;
            settings.WindowTop = bounds.Top;
            settings.StartMaximized = false;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                if (_vm.IsPipMode)
                {
                    // Aero Snap fired while in PiP — exit PiP and restore to pre-PiP state instead of maximizing
                    ExitPipMode();
                    return;
                }

                if (!_vm.IsFullscreen)
                {
                    MaxHeight = SystemParameters.WorkArea.Height + 16;
                    MaxWidth  = SystemParameters.WorkArea.Width  + 16;
                }
            }

            if (_isApplyingFullscreenTransition)
            {
                _lastWindowState = WindowState;
                return;
            }

            UpdateEffectiveTopmost();
            _lastWindowState = WindowState;
        }

        private void PipPopup_MouseEnter(object sender, MouseEventArgs e) => WakeControls();

        private void PipPopup_MouseMove(object sender, MouseEventArgs e) => WakeControls();

        private void PipPopup_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_vm.IsPipMode)
            {
                _hideControlsTimer.Stop();
                _hideControlsTimer.Start();
            }
        }

        private void PipPopup_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_vm.IsPipMode || e.ChangedButton != MouseButton.Left)
                return;

            var source = e.OriginalSource as DependencyObject;
            if (IsOverInteractiveControl(source) && !IsWithinPipDragHandle(source))
                return;

            BeginNativeWindowDrag();
            e.Handled = true;
        }

        private static bool IsCursorOverWindow(Window window)
        {
            var cursor = System.Windows.Forms.Cursor.Position;
            return cursor.X >= window.Left && cursor.X <= window.Left + window.Width &&
                   cursor.Y >= window.Top  && cursor.Y <= window.Top  + window.Height;
        }

        private bool IsWithinPipDragHandle(DependencyObject? source)
        {
            var current = source;
            while (current != null)
            {
                if (ReferenceEquals(current, PipHoverHandle))
                    return true;

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
        }

        private void BeginNativeWindowDrag()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
                return;

            ReleaseCapture();
            SendMessage(hwnd, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
        }

        private void VideoArea_Click(object sender, MouseButtonEventArgs e)
        {
            TryToggleFullscreenFromDoubleClick(e, e.OriginalSource as DependencyObject);
        }

        private bool IsOverInteractiveControl(DependencyObject? source)
        {
            var current = source;
            while (current != null)
            {
                if (current is ButtonBase || current is Slider || current is ScrollViewer || current is TextBox || 
                    current is RepeatButton || current is Thumb || current is ComboBox || current is ListBox || current is MenuItem)
                    return true;
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space:  _vm.PlayPauseCommand.Execute(null);   e.Handled = true; break;
                case Key.Left:   _vm.SkipBackCommand.Execute(null);    e.Handled = true; break;
                case Key.Right:  _vm.SkipForwardCommand.Execute(null); e.Handled = true; break;
                case Key.Up:     _vm.Volume = Math.Min(100, _vm.Volume + 5); e.Handled = true; break;
                case Key.Down:   _vm.Volume = Math.Max(0,   _vm.Volume - 5); e.Handled = true; break;
                case Key.M:      _vm.ToggleMuteCommand.Execute(null);  e.Handled = true; break;
                case Key.I:      _vm.ToggleMetadataCommand.Execute(null); e.Handled = true; break;
                case Key.F:
                case Key.F11:    _vm.ToggleFullscreenCommand.Execute(null); e.Handled = true; break;
                case Key.N:      _vm.NextTrackCommand.Execute(null);   e.Handled = true; break;
                case Key.P:      _vm.PrevTrackCommand.Execute(null);   e.Handled = true; break;
                case Key.S:
                    if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                        _vm.TakeScreenshotCommand.Execute(null);
                    else if (Keyboard.Modifiers == ModifierKeys.Control)
                        _vm.PlaylistVM?.ToggleShuffleCommand.Execute(null);
                    else
                        _vm.StopCommand.Execute(null);
                    e.Handled = true; break;
                case Key.OemPeriod: _vm.NextFrameCommand.Execute(null); e.Handled = true; break;
                case Key.OemComma:  _vm.FrameBackCommand.Execute(null);  e.Handled = true; break;
                case Key.G:      _vm.AdjustSubtitleDelayCommand.Execute("-50"); e.Handled = true; break;
                case Key.H:      _vm.AdjustSubtitleDelayCommand.Execute("50");  e.Handled = true; break;
                case Key.R:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        _vm.SetABPointCommand.Execute(null);
                    else
                        _vm.PlaylistVM?.ToggleRepeatCommand.Execute(null);
                    e.Handled = true; break;
                case Key.A:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                        _vm.ClearABRepeatCommand.Execute(null);
                    else if (Keyboard.Modifiers == ModifierKeys.None)
                        _vm.SetAPointCommand.Execute(null);
                    e.Handled = true; break;
                case Key.B:      _vm.SetBPointCommand.Execute(null); e.Handled = true; break;
                case Key.OemCloseBrackets:
                    _vm.Rate = Math.Min(4.0f, (float)Math.Round(_vm.Rate + 0.25f, 2));
                    e.Handled = true; break;
                case Key.OemOpenBrackets:
                    _vm.Rate = Math.Max(0.25f, (float)Math.Round(_vm.Rate - 0.25f, 2));
                    e.Handled = true; break;
                case Key.OemPipe:
                    _vm.Rate = 1.0f;
                    e.Handled = true; break;
                case Key.OemPlus:
                case Key.Add:
                    _vm.Zoom = Math.Clamp((float)Math.Round(_vm.Zoom + 0.25f, 2), 0.25f, 4.0f);
                    e.Handled = true; break;
                case Key.OemMinus:
                case Key.Subtract:
                    _vm.Zoom = Math.Clamp((float)Math.Round(_vm.Zoom - 0.25f, 2), 0.25f, 4.0f);
                    e.Handled = true; break;
                case Key.Multiply:
                    _vm.Zoom = 0f; // reset to auto-fit
                    e.Handled = true; break;
                case Key.L:
                    if (Keyboard.Modifiers == ModifierKeys.Control) { _vm.TogglePlaylistCommand.Execute(null); e.Handled = true; }
                    break;
            }
        }

        private void TryToggleFullscreenFromDoubleClick(MouseButtonEventArgs e, DependencyObject? source)
        {
            if (e.ClickCount != 2 || e.ChangedButton != MouseButton.Left)
                return;

            if (_vm.IsPipMode || IsOverInteractiveControl(source))
                return;

            ToggleFullscreen();
            e.Handled = true;
        }

        private void ToggleFullscreen()
        {
            if (_vm.IsPipMode)
                return;

            _vm.IsFullscreen = !_vm.IsFullscreen;
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            bool isFile = e.Data.GetDataPresent(DataFormats.FileDrop);
            e.Effects = isFile ? DragDropEffects.Copy : DragDropEffects.None;
            DropHintOverlay.Visibility = isFile ? Visibility.Visible : Visibility.Collapsed;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            DropHintOverlay.Visibility = Visibility.Collapsed;
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files) _vm.AddDroppedFiles(files);
        }

        private void SeekBar_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            _vm.IsDraggingSeek = true;
            (sender as Slider)?.CaptureMouse();
        }

        private void SeekBar_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            if (_vm.IsDraggingSeek)
            {
                _vm.IsDraggingSeek = false;
                // Commit the seek based on the current slider value
                if (sender is Slider sl && _vm.Duration > 0)
                    _vm.Position = sl.Value; // This triggers the real seek now that IsDraggingSeek=false
                (sender as Slider)?.ReleaseMouseCapture();
            }
        }

        // ─── Seek bar hover tooltip ───────────────────────────────────────
        private void SeekSlider_MouseMove(object sender, MouseEventArgs e)
        {
            if (_vm.Duration <= 0) return;
            var sl = (Slider)sender;
            var x = e.GetPosition(sl).X;
            var fraction = Math.Clamp(x / sl.ActualWidth, 0, 1);
            var ms = (long)(fraction * _vm.Duration);
            SeekHoverText.Text = FormatMs(ms);
            SeekHoverPopup.IsOpen = true;
        }

        private void SeekSlider_MouseLeave(object sender, MouseEventArgs e)
            => SeekHoverPopup.IsOpen = false;

        private static string FormatMs(long ms)
        {
            var t = TimeSpan.FromMilliseconds(ms);
            return t.Hours > 0 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");
        }

        // ─── A-B loop seek bar markers ────────────────────────────────────
        private void AbMarkersCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateAbMarkers();

        private void UpdateAbMarkers()
        {
            var w = AbMarkersCanvas.ActualWidth;
            if (w <= 0) return;
            var aFrac = _vm.ABPointAFraction;
            var bFrac = _vm.ABPointBFraction;
            AbMarkerA.Visibility = aFrac >= 0 ? Visibility.Visible : Visibility.Collapsed;
            AbMarkerB.Visibility = bFrac >= 0 ? Visibility.Visible : Visibility.Collapsed;
            if (aFrac >= 0) Canvas.SetLeft(AbMarkerA, aFrac * w - 1);
            if (bFrac >= 0) Canvas.SetLeft(AbMarkerB, bFrac * w - 1);
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)        => _vm.OpenFileCommand.Execute(null);
        private void MenuOpenFolder_Click(object sender, RoutedEventArgs e)  => _vm.OpenFolderCommand.Execute(null);

        // ─── Playlist drag-drop reorder ───────────────────────────────────
        private Models.PlaylistItem? _draggedPlaylistItem;

        private void PlaylistBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed &&
                sender is ListBox lb &&
                lb.SelectedItem is Models.PlaylistItem item)
            {
                _draggedPlaylistItem = item;
                DragDrop.DoDragDrop(lb, item, DragDropEffects.Move);
            }
        }

        private void PlaylistBox_Drop(object sender, DragEventArgs e)
        {
            if (_draggedPlaylistItem == null || sender is not ListBox) return;
            var target = (e.OriginalSource as FrameworkElement)?.DataContext as Models.PlaylistItem;
            if (target == null || target == _draggedPlaylistItem) return;
            var svc = _vm.Playlist;
            var from = svc.Items.IndexOf(_draggedPlaylistItem);
            var to = svc.Items.IndexOf(target);
            if (from >= 0 && to >= 0) svc.MoveItem(from, to);
            _draggedPlaylistItem = null;
        }

        private void MenuRecentFolders_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as UIElement ?? RecentFoldersBtn;
            var menu = new ContextMenu();
            if (!_vm.RecentFolders.Any()) menu.Items.Add(new MenuItem { Header = "No recent folders", IsEnabled = false });
            else
            {
                foreach (var d in _vm.RecentFolders.ToList())
                {
                    if (string.IsNullOrEmpty(d)) continue;
                    var item = new MenuItem { Header = d, Tag = d };
                    item.Click += (s, _) =>
                    {
                        var dir = ((MenuItem)s).Tag as string;
                        if (!string.IsNullOrEmpty(dir))
                        {
                            if (System.IO.Directory.Exists(dir)) _vm.OpenRecentFolderCommand.Execute(dir);
                            else MessageBox.Show("Folder no longer exists: " + dir, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    };
                    menu.Items.Add(item);
                }
            }
            menu.Style = (Style)Application.Current.Resources[typeof(ContextMenu)];
            menu.PlacementTarget = btn; menu.Placement = PlacementMode.Bottom; menu.IsOpen = true;
        }

        private void MenuRecent_Click(object sender, RoutedEventArgs e)
        {
            var btn  = RecentBtn;
            var menu = new ContextMenu();
            if (!_vm.RecentFiles.Any()) menu.Items.Add(new MenuItem { Header = "No recent files", IsEnabled = false });
            else
            {
                foreach (var f in _vm.RecentFiles.ToList()) // Use ToList to avoid concurrent modification issues
                {
                    if (string.IsNullOrEmpty(f)) continue;
                    var item = new MenuItem { Header = System.IO.Path.GetFileName(f), Tag = f };
                    item.Click += (s, args) => 
                    {
                        var path = ((MenuItem)s).Tag as string;
                        if (!string.IsNullOrEmpty(path))
                        {
                            if (System.IO.File.Exists(path))
                            {
                                _vm.PlayRecentCommand.Execute(path);
                            }
                            else
                            {
                                MessageBox.Show("File no longer exists: " + path, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    };
                    menu.Items.Add(item);
                }
            }
            menu.Style = (Style)Application.Current.Resources[typeof(ContextMenu)];
            menu.PlacementTarget = btn; menu.Placement = PlacementMode.Bottom; menu.IsOpen = true;
        }

        private void MenuSleepTimer_Click(object sender, RoutedEventArgs e)
        {
            var menu = (ContextMenu)FindResource("SleepMenu");
            menu.PlacementTarget = sender as UIElement; menu.Placement = PlacementMode.Bottom; menu.IsOpen = true;
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            var menu = (ContextMenu)FindResource("SettingsMenu");
            menu.PlacementTarget = sender as UIElement; menu.Placement = PlacementMode.Bottom; menu.IsOpen = true;
        }

        private void OpenSettingsDialog_Click(object sender, RoutedEventArgs e)
        {
            var win = new Views.SettingsWindow(_vm) { Owner = this };
            win.ShowDialog();
        }

        private void SleepTimer_5(object sender, RoutedEventArgs e)   => _vm.SetSleepTimerCommand.Execute(5);
        private void SleepTimer_10(object sender, RoutedEventArgs e)  => _vm.SetSleepTimerCommand.Execute(10);
        private void SleepTimer_15(object sender, RoutedEventArgs e)  => _vm.SetSleepTimerCommand.Execute(15);
        private void SleepTimer_30(object sender, RoutedEventArgs e)  => _vm.SetSleepTimerCommand.Execute(30);
        private void SleepTimer_60(object sender, RoutedEventArgs e)  => _vm.SetSleepTimerCommand.Execute(60);
        private void SleepTimer_Cancel(object sender, RoutedEventArgs e) => _vm.CancelSleepTimerCommand.Execute(null);

        private void Playlist_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_vm.PlaylistVM?.SelectedItem == null) return;
            var item = _vm.PlaylistVM.SelectedItem;
            if (!System.IO.File.Exists(item.FilePath))
            {
                MessageBox.Show($"File no longer exists:\n{item.FilePath}", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                ServiceLocator.PlaylistService!.PlayItem(item);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not play file:\n{ex.Message}", "Playback Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSupportPatreon_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://www.patreon.com/cw/BABU_ISHU");
        }

        private void BtnSupportPaypal_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://www.paypal.com/ncp/payment/SECBQ62TRZZ6Y");
        }

        private void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                // Could be: no browser registered for the protocol, sandboxed environment,
                // or a malformed URL. Log so devs can diagnose; non-fatal for the user.
                System.Diagnostics.Debug.WriteLine($"[MainWindow] OpenUrl('{url}') failed: {ex.Message}");
            }
        }
    }
}
