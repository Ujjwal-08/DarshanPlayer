namespace DarshanPlayer.Services
{
    public static class ServiceLocator
    {
        public static IMediaService? MediaService { get; set; }
        public static PlaylistService? PlaylistService { get; set; }
        public static SettingsService? SettingsService { get; set; }
        public static LanguageManager? LanguageManager { get; set; }
        public static INotificationService? Notifications { get; set; }
        public static UpdateService? UpdateService { get; set; }
    }
}
