namespace Jellyfin.ApiClient.Model.Notifications
{
    public class NotificationsSummary
    {
        public int UnreadCount { get; set; }
        public NotificationLevel MaxUnreadNotificationLevel { get; set; }
    }
}
