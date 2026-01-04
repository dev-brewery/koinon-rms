using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Koinon.Api.Hubs;

/// <summary>
/// SignalR hub for real-time notifications.
/// Users are automatically added to a group based on their PersonIdKey claim.
/// </summary>
[Authorize]
public class NotificationHub(ILogger<NotificationHub> logger) : Hub
{
    private const string PersonIdKeyClaimType = "idKey";

    /// <summary>
    /// Called when a client connects to the hub.
    /// Adds the user to their personal notification group based on PersonIdKey.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var personIdKey = GetPersonIdKey();

        if (string.IsNullOrEmpty(personIdKey))
        {
            logger.LogWarning(
                "Client {ConnectionId} connected without PersonIdKey claim. Connection: {UserIdentifier}",
                Context.ConnectionId,
                Context.User?.Identity?.Name ?? "Unknown");

            await base.OnConnectedAsync();
            return;
        }

        // Add user to their personal notification group
        await Groups.AddToGroupAsync(Context.ConnectionId, personIdKey);

        logger.LogInformation(
            "Client {ConnectionId} connected and added to group {PersonIdKey}",
            Context.ConnectionId,
            personIdKey);

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// SignalR automatically handles group cleanup.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var personIdKey = GetPersonIdKey();

        if (exception != null)
        {
            logger.LogWarning(
                exception,
                "Client {ConnectionId} disconnected with error. PersonIdKey: {PersonIdKey}",
                Context.ConnectionId,
                personIdKey ?? "Unknown");
        }
        else
        {
            logger.LogInformation(
                "Client {ConnectionId} disconnected. PersonIdKey: {PersonIdKey}",
                Context.ConnectionId,
                personIdKey ?? "Unknown");
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Extracts the PersonIdKey from the user's claims.
    /// </summary>
    /// <returns>The PersonIdKey if found, otherwise null.</returns>
    private string? GetPersonIdKey()
    {
        return Context.User?.FindFirst(PersonIdKeyClaimType)?.Value;
    }
}
