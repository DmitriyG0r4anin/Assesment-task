using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using NotificationsService.Models;

namespace NotificationsService.Hubs;

public class MotionHub(ILogger<MotionHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();

        await base.Clients.All.SendAsync($"Connected {Context.ConnectionId}");

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
