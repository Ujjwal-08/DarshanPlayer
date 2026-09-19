using DarshanPlayer.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace DarshanPlayer
{
    public partial class FullscreenControlsWindow : Window
    {
        private readonly MainViewModel _vm;

        public event EventHandler? ActivityDetected;

        // Last physical cursor position that produced an activity signal. Showing this layered
        // window under a motionless cursor synthesises MouseEnter/MouseMove, which used to
        // re-wake the controls in a loop and latch the bar on screen permanently.
        private System.Drawing.Point _lastPointerPos;

        private void RaiseActivityIfPointerMoved()
        {
            var pos = System.Windows.Forms.Cursor.Position;
            if (pos == _lastPointerPos)
                return;

            _lastPointerPos = pos;
            ActivityDetected?.Invoke(this, EventArgs.Empty);
        }

        public FullscreenControlsWindow(MainViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;
        }

        public void SetControlsVisible(bool visible)
        {
            // Visibility.Collapsed (not just Opacity=0) ensures the layered window renders pure
            // alpha=0 so Win32 routes mouse events through to the window below (LibVLC HWND).
            OverlayChrome.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            IsHitTestVisible = visible;
        }

        private void RootSurface_MouseEnter(object sender, MouseEventArgs e)
        {
            RaiseActivityIfPointerMoved();
        }

        private void RootSurface_MouseMove(object sender, MouseEventArgs e)
        {
            RaiseActivityIfPointerMoved();
        }

        private void SeekSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _vm.IsDraggingSeek = true;
            ActivityDetected?.Invoke(this, EventArgs.Empty);
        }

        private void SeekSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _vm.IsDraggingSeek = false;
            ActivityDetected?.Invoke(this, EventArgs.Empty);
        }
    }
}
