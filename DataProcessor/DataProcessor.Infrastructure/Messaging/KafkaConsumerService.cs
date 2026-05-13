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
    private IProducer<Null, string>? _producer;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Kafka consumer starting. Brokers: {Brokers}, Topic: {Topic}, Group: {GroupId}",
                _settings.Brokers, _settings.MetricTopic, _settings.GroupId);
        }

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _settings.Brokers,
            GroupId = _settings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _settings.Brokers
        };

        _producer = new ProducerBuilder<Null, string>(producerConfig).Build();
        using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
        consumer.Subscribe(_settings.MetricTopic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);

                    if (result?.Message?.Value is null)
                        continue;

                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("Received Kafka message: {Message}", result.Message.Value);
                    }

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
            _producer?.Dispose();
        }
    }

    private async Task ProcessMessageAsync(string messageJson, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<KafkaMessage>(messageJson);
        if (message is null)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Failed to deserialize Kafka message: {Message}", messageJson);
            }
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var roomRepository = scope.ServiceProvider.GetRequiredService<IRoomRepository>();

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Getting room {RoomName}", message.Name);
        }

        var room = await roomRepository.GetByNameAsync(message.Name, cancellationToken);
        if (room is null)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Room {RoomName} not found. Creating new room", message.Name);
            }
            room = await roomRepository.CreateByNameAsync(message.Name, cancellationToken);
        }

        switch (message.Type)
        {
            case MetricTypes.AirQuality:
                await ProcessAirQualityAsync(scope, message, room.Id, cancellationToken);
                break;
            case MetricTypes.Energy:
                await ProcessEnergyAsync(scope, message, room.Id, cancellationToken);
                break;
            case MetricTypes.Motion:
                await ProcessMotionAsync(scope, message, room.Id, cancellationToken);
                break;
            default:
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Unknown message type: {Type}", message.Type);
                }
                break;
        }
    }

    private async Task ProcessAirQualityAsync(
        IServiceScope scope, KafkaMessage message, string roomId, CancellationToken cancellationToken)
    {
        var payload = message.Payload.Deserialize<AirQualityPayload>();
        if (payload is null)
        {
            LogMetricDeserializationFailure(MetricTypes.AirQuality);
            return;
        }

        var repository = scope.ServiceProvider.GetRequiredService<IMetricBaseRepository<AirQuality>>();
        var entity = new AirQuality
        {
            RoomId = roomId,
            Co2 = payload.Co2,
            Pm25 = payload.Pm25,
            Humidity = payload.Humidity,
            Timestamp = message.Timestamp
        };

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Saving AirQuality data for room {RoomId}", roomId);
        }
        await repository.InsertAsync(entity, cancellationToken);
    }

    private async Task ProcessEnergyAsync(
        IServiceScope scope, KafkaMessage message, string roomId, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<EnergyPayload>(message.Payload.GetRawText());
        if (payload is null)
        {
            LogMetricDeserializationFailure(MetricTypes.Energy);
            return;
        }

        var repository = scope.ServiceProvider.GetRequiredService<IMetricBaseRepository<Energy>>();
        var entity = new Energy
        {
            RoomId = roomId,
            Amount = payload.Energy,
            Timestamp = message.Timestamp
        };

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Saving Energy data for room {RoomId}", roomId);
        }
        await repository.InsertAsync(entity, ct);
    }

    private async Task ProcessMotionAsync(
        IServiceScope scope, KafkaMessage message, string roomId, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<MotionPayload>(message.Payload.GetRawText());
        if (payload is null)
        {
            LogMetricDeserializationFailure(MetricTypes.Motion);
            return;
        }

        var repository = scope.ServiceProvider.GetRequiredService<IMetricBaseRepository<Motion>>();
        var entity = new Motion
        {
            RoomId = roomId,
            IsDetected = payload.MotionDetected,
            Timestamp = message.Timestamp
        };

        await repository.InsertAsync(entity, ct);
        if (!payload.MotionDetected)
        {
            return;
        }

        var motionMessage = new MotionDetectedMessage
        {
            RoomName = message.Name,
            IsDetected = payload.MotionDetected,
            Timestamp = message.Timestamp
        };

        var json = JsonSerializer.Serialize(motionMessage);
        await _producer!.ProduceAsync(
            _settings.MotionTopic,
            new Message<Null, string> { Value = json },
            ct);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Saved Motion data for room {RoomId} and published to {Topic}", roomId, _settings.MotionTopic);
        }
    }

    private void LogMetricDeserializationFailure(string metricType)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning("Failed to deserialize {Metric} payload", metricType);
        }
    }
}
