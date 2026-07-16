using Microsoft.AspNetCore.SignalR;

namespace LionSimPlanner.Shared.Hubs;

public sealed class SimPlannerHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
