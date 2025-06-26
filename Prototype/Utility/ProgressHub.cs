using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Prototype.Utility;

/// <summary>
/// SignalR Hub for real-time progress updates during bulk operations.
/// Requires JWT authentication - the client must provide access_token via accessTokenFactory.
/// </summary>
[Authorize]
public class ProgressHub(ILogger<ProgressHub> logger) : Hub
{
    private readonly ILogger<ProgressHub> _logger = logger;

    public async Task JoinProgressGroup(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"progress_{jobId}");
    }

    public async Task LeaveProgressGroup(string jobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"progress_{jobId}");
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}