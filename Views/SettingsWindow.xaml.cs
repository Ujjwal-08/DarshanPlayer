using DarshanPlayer.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace DarshanPlayer.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow(MainViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            ShortcutList.ItemsSource = new[]
            {
                new { Key = "Space",           Action = "Play / Pause" },
                new { Key = "Left",            Action = "Skip back 5 seconds" },
                new { Key = "Right",           Action = "Skip forward 5 seconds" },
                new { Key = "Up",              Action = "Volume +5" },
                new { Key = "Down",            Action = "Volume -5" },
                new { Key = "M",               Action = "Toggle Mute" },
                new { Key = "F  /  F11",       Action = "Toggle Fullscreen" },
                new { Key = "N",               Action = "Next Track" },
                new { Key = "P",               Action = "Previous Track" },
                new { Key = "S",               Action = "Stop" },
                new { Key = ". (period)",      Action = "Next Frame" },
                new { Key = ", (comma)",       Action = "Previous Frame (paused only)" },
                new { Key = "]",               Action = "Speed +0.25×" },
                new { Key = "[",               Action = "Speed −0.25×" },
                new { Key = "\\",              Action = "Reset Speed to 1×" },
                new { Key = "G",               Action = "Subtitle delay -50 ms" },
                new { Key = "H",               Action = "Subtitle delay +50 ms" },
                new { Key = "A",               Action = "Set A-B loop point A" },
                new { Key = "B",               Action = "Set A-B loop point B" },
                new { Key = "Ctrl + A",        Action = "Clear A-B loop" },
                new { Key = "R",               Action = "Set A-B loop point (legacy cycle)" },
                new { Key = "Ctrl + Shift + S",Action = "Take Screenshot" },
                new { Key = "Ctrl + L",        Action = "Toggle Playlist" },
                new { Key = "Ctrl + O",        Action = "Open File" },
                new { Key = "Escape",          Action = "Exit Fullscreen / PiP" },
            };
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
    }
}
