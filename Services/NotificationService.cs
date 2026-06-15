using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace DarshanPlayer.Services
{
    /// <summary>
    /// Default <see cref="INotificationService"/>. Each toast auto-dismisses after a delay via a
    /// one-shot <see cref="DispatcherTimer"/>. All collection mutations are marshalled to the UI
    /// thread so callers (media events, background tasks) can raise toasts from anywhere.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private const int MaxVisible = 4;

        public ObservableCollection<ToastItem> Toasts { get; } = new();

        public void Show(string message, ToastType type = ToastType.Info, int durationMs = 3500)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(new Action(() => AddToast(message, type, durationMs)));
            }
            else
            {
                AddToast(message, type, durationMs);
            }
        }

        private void AddToast(string message, ToastType type, int durationMs)
        {
            var item = new ToastItem(message, type);
            Toasts.Add(item);

            // Keep the stack bounded so a burst of toasts can't fill the screen.
            while (Toasts.Count > MaxVisible)
                Toasts.RemoveAt(0);

            // One-shot timer to remove this specific toast. DispatcherTimer fires on the UI thread.
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Max(500, durationMs)) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                Toasts.Remove(item);
            };
            timer.Start();
        }

        public void ShowInfo(string message) => Show(message, ToastType.Info);
        public void ShowSuccess(string message) => Show(message, ToastType.Success);
        public void ShowWarning(string message) => Show(message, ToastType.Warning);
        public void ShowError(string message) => Show(message, ToastType.Error, 6000);
    }
}
