using ScheduleCentral.Models;

namespace ScheduleCentral.Models.ViewModels
{
    public class NotificationsIndexViewModel
    {
        public int UnreadCount { get; set; }
        public List<UserNotification> Notifications { get; set; } = new();
    }
}
