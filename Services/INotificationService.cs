using System.Collections.ObjectModel;

namespace DarshanPlayer.Services
{
    /// <summary>Severity of a toast — drives the colour stripe in the UI template.</summary>
    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>A single transient toast notification (Phase 23).</summary>
    public class ToastItem
    {
        public ToastItem(string message, ToastType type)
        {
            Message = message;
            Type = type;
        }

        public string Message { get; }
        public ToastType Type { get; }

        /// <summary>Glyph shown to the left of the message. Plain Unicode so it renders without an icon font.</summary>
        public string Icon => Type switch
        {
            ToastType.Success => "✔", // ✔
            ToastType.Warning => "⚠", // ⚠
            ToastType.Error => "✖",   // ✖
            _ => "ℹ"                  // ℹ
        };
    }

    /// <summary>
    /// Bottom-right toast notifications. Replaces blocking <c>MessageBox</c> calls for
    /// non-critical feedback (screenshot saved, subtitle loaded, save failures, …).
    /// </summary>
    public interface INotificationService
    {
        /// <summary>Live collection of currently-visible toasts; bind an ItemsControl to this.</summary>
        ObservableCollection<ToastItem> Toasts { get; }

        void Show(string message, ToastType type = ToastType.Info, int durationMs = 3500);
        void ShowInfo(string message);
        void ShowSuccess(string message);
        void ShowWarning(string message);
        void ShowError(string message);
    }
}
