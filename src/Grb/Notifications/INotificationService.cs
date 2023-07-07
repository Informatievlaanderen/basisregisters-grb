namespace Grb.Notifications
{
    using System.Threading.Tasks;

    public interface INotificationService
    {
        Task PublishToTopicAsync(NotificationMessage message);
    }
}
