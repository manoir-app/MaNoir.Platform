namespace MaNoir.Core.Users;

/// <summary>
/// Implements the user business logic layer on top of persistence helpers.
/// </summary>
/// <remarks>
/// <para>Example:</para>
/// <code>
/// UserLogic logic = new UserLogic();
/// User user = await logic.UpsertUserAsync("michael", new User()
/// {
///     FirstName = "Michael",
///     Name = "Carbenay",
///     MainEmail = "michael@example.net"
/// }, cancellationToken);
///
/// await logic.SetPasswordAsync("michael", "P@ssw0rd!", cancellationToken);
/// </code>
/// </remarks>
public sealed partial class UserLogic
{
}