using Home.Common;
using Home.Common.Messages;
using System;

namespace MaNoir.Core.Users;

public static class UserAuthenticationInterprocessPublisher
{
    public static void TryPublishFailedLogin(UserFailedLoginState state)
    {
        if (state == null)
            return;

        try
        {
            NatsInterprocess.Push(new UserLoginFailedMessage()
            {
                UserId = state.UserId,
                FailedCount = state.FailedCount,
                FailedAtUtc = state.LastFailedAtUtc,
                WindowStartedAtUtc = state.WindowStartedAtUtc,
                RemoteAddress = state.LastRemoteAddress,
                UserAgent = state.LastUserAgent,
            });
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("The failed login event could not be published over NATS: " + exception.Message);
        }
    }

    public static void TryPublishPasswordChanged(string userId, DateTimeOffset changedAtUtc, string remoteAddress, string userAgent)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        try
        {
            NatsInterprocess.Push(new UserPasswordChangedMessage()
            {
                UserId = userId,
                ChangedAtUtc = changedAtUtc,
                RemoteAddress = remoteAddress,
                UserAgent = userAgent,
            });
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("The password changed event could not be published over NATS: " + exception.Message);
        }
    }
}