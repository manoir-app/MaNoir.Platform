using MaNoir.Core.Contracts.Models.Users;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.DataPublication;

internal static class UserMobileNotificationPublisher
{
    internal static Task PublishAsync(string userId, UserNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}