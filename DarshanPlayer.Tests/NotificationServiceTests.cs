using DarshanPlayer.Services;
using Xunit;

namespace DarshanPlayer.Tests
{
    public class NotificationServiceTests
    {
        [Fact]
        public void Show_AddsToastWithMessage()
        {
            var svc = new NotificationService();

            svc.Show("hello");

            Assert.Single(svc.Toasts);
            Assert.Equal("hello", svc.Toasts[0].Message);
            Assert.Equal(ToastType.Info, svc.Toasts[0].Type);
        }

        [Fact]
        public void Show_IgnoresEmptyMessage()
        {
            var svc = new NotificationService();

            svc.Show("");
            svc.Show("   ");

            Assert.Empty(svc.Toasts);
        }

        [Fact]
        public void ShowError_UsesErrorType()
        {
            var svc = new NotificationService();
            svc.ShowError("boom");
            Assert.Equal(ToastType.Error, Assert.Single(svc.Toasts).Type);
        }

        [Fact]
        public void ShowSuccess_UsesSuccessType()
        {
            var svc = new NotificationService();
            svc.ShowSuccess("done");
            Assert.Equal(ToastType.Success, Assert.Single(svc.Toasts).Type);
        }

        [Fact]
        public void Show_CapsVisibleToastCount()
        {
            var svc = new NotificationService();
            for (int i = 0; i < 8; i++)
                svc.Show($"toast {i}");

            // MaxVisible is 4; oldest are evicted.
            Assert.Equal(4, svc.Toasts.Count);
            Assert.Equal("toast 7", svc.Toasts[^1].Message);
        }

        [Theory]
        [InlineData(ToastType.Success, "✔")]
        [InlineData(ToastType.Warning, "⚠")]
        [InlineData(ToastType.Error, "✖")]
        [InlineData(ToastType.Info, "ℹ")]
        public void ToastItem_IconMatchesType(ToastType type, string expected)
        {
            var item = new ToastItem("x", type);
            Assert.Equal(expected, item.Icon);
        }
    }
}
