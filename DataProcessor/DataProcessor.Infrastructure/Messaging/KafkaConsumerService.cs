using Confluent.Kafka;
using DataProcessor.Domain.Constants;
using DataProcessor.Domain.Entities;
using DataProcessor.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace DataProcessor.Infrastructure.Messaging;

public class KafkaConsumerService(
    IServiceScopeFactory scopeFactory,
    ILogger<KafkaConsumerService> logger,
    IOptions<KafkaConfig> settings) : BackgroundService
{
    private readonly KafkaConfig _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Kafka consumer starting. Brokers: {Brokers}, Topic: {Topic}, Group: {GroupId}",
            _settings.Brokers, _settings.Topic, _settings.GroupId);

        var config = new ConsumerConfig
        {
            BootstrapServers = _settings.Brokers,
            GroupId = _settings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(_settings.Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);

                    if (result?.Message?.Value is null)
                        continue;

                    logger.LogInformation("Received Kafka message: {Message}", result.Message.Value);

                    await ProcessMessageAsync(result.Message.Value, stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Error consuming Kafka message");
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Kafka consumer stopping");
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ProcessMessageAsync(string messageJson, CancellationToken ct)
    {
        try
        {
            var message = JsonSerializer.Deserialize<KafkaMessage>(messageJson);
            if (message is null)
            {
                logger.LogWarning("Failed to deserialize Kafka message: {Message}", messageJson);
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var roomRepository = scope.ServiceProvider.GetRequiredService<IRoomRepository>();

            var room = await roomRepository.GetOrCreateAsync(message.Name, ct);

            switch (message.Type)
            {
                case MetricTypes.AirQuality:
                    await ProcessAirQualityAsync(scope, message, room.Id, ct);
                    break;
                case MetricTypes.Energy:
                    await ProcessEnergyAsync(scope, message, room.Id, ct);
                    break;
                case MetricTypes.Motion:
                    await ProcessMotionAsync(scope, message, room.Id, ct);
                    break;
                default:
                    logger.LogWarning("Unknown message type: {Type}", message.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Kafka message");
        }
    }

    private async Task ProcessAirQualityAsync(
        IServiceScope scope, KafkaMessage message, string roomId, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<AirQualityPayload>(message.Payload.GetRawText());
        if (payload is null) return;

        var repository = scope.ServiceProvider.GetRequiredService<IMetricBaseRepository<AirQuality>>();
        var entity = new AirQuality
        {
            RoomId = roomId,
            Co2 = payload.Co2,
            Pm25 = payload.Pm25,
            Humidity = payload.Humidity,
            Timestamp = message.Timestamp
        };

        await repository.InsertAsync(entity, cancellationToken);
        logger.LogInformation("Saved AirQuality data for room {RoomId}", roomId);
    }

    private async Task ProcessEnergyAsync(
        IServiceScope scope, KafkaMessage message, string roomId, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<EnergyPayload>(message.Payload.GetRawText());
        if (payload is null) return;

        var repository = scope.ServiceProvider.GetRequiredService<IMetricBaseRepository<Energy>>();
        var entity = new Energy
        {
            RoomId = roomId,
            Amount = payload.Energy,
            Timestamp = message.Timestamp
        };

        await repository.InsertAsync(entity, ct);
        logger.LogInformation("Saved Energy data for room {RoomId}", roomId);
    }

    private async Task ProcessMotionAsync(
        IServiceScope scope, KafkaMessage message, string roomId, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<MotionPayload>(message.Payload.GetRawText());
        if (payload is null) return;

        var repository = scope.ServiceProvider.GetRequiredService<IMetricBaseRepository<Motion>>();
        var entity = new Motion
        {
            RoomId = roomId,
            IsDetected = payload.MotionDetected,
            Timestamp = message.Timestamp
        };

        await repository.InsertAsync(entity, ct);
        logger.LogInformation("Saved Motion data for room {RoomId}", roomId);
    }
}
