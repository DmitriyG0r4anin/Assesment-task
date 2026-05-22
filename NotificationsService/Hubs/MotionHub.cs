using Microsoft.AspNetCore.SignalR;

namespace NotificationsService.Hubs;

public class MotionHub(ILogger<MotionHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Connected {ConnectionId}", Context.ConnectionId);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
