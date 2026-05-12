using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using NotificationsService.Configuration;
using NotificationsService.Hubs;
using NotificationsService.Models;
using NotificationsService.Serialization;
using System.Text.Json;

namespace NotificationsService.Services;

public class KafkaMotionConsumerService(
    IHubContext<MotionHub> hubContext,
    ILogger<KafkaMotionConsumerService> logger,
    IOptions<KafkaConfig> settings) : BackgroundService
{
    private readonly KafkaConfig _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Kafka motion consumer starting. Brokers: {Brokers}, Topic: {Topic}, Group: {GroupId}",
                _settings.Brokers, _settings.MotionTopic, _settings.GroupId);
        }

        var config = new ConsumerConfig
        {
            BootstrapServers = _settings.Brokers,
            GroupId = _settings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(_settings.MotionTopic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);

                    if (result?.Message?.Value is null)
                        continue;

                    await ProcessMessageAsync(result.Message.Value, stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Error consuming Kafka message");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Unexpected error while processing Kafka message");
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ProcessMessageAsync(string messageJson, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize(messageJson, MotionMessageJsonContext.Default.MotionDetectedMessage);
        if (message is null)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Failed to deserialize motion message: {Message}", messageJson);
            }
            return;
        }

        var motionEvent = new MotionEvent(
            RoomName: message.RoomName,
            IsDetected: message.IsDetected,
            Timestamp: message.Timestamp);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Motion event received from DataProcessor. Room={RoomName}, Detected={IsDetected}, Timestamp={Timestamp}",
                motionEvent.RoomName, motionEvent.IsDetected, motionEvent.Timestamp);
        }

        await hubContext.Clients.All.SendAsync(
            "MotionDetected",
            motionEvent,
            cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Motion event sent to MotionDetected");
        }
    }
}
