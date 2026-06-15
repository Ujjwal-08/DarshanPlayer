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
            ActivityDetected?.Invoke(this, EventArgs.Empty);
        }

        private void RootSurface_MouseMove(object sender, MouseEventArgs e)
        {
            ActivityDetected?.Invoke(this, EventArgs.Empty);
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
