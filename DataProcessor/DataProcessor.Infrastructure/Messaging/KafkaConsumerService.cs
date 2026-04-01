using System.Text.Json;
using Confluent.Kafka;
using DataProcessor.Application.Abstractions.Repositories;
using DataProcessor.Domain.Entities;
using DataProcessor.Infrastructure.Messaging.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataProcessor.Infrastructure.Messaging;

public class KafkaConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly KafkaSettings _settings;

    public KafkaConsumerService(
        IServiceScopeFactory scopeFactory,
        ILogger<KafkaConsumerService> logger,
        IOptions<KafkaSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
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

                    _logger.LogInformation("Received Kafka message: {Message}", result.Message.Value);

                    await ProcessMessageAsync(result.Message.Value, stoppingToken);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming Kafka message");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumer stopping");
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
                _logger.LogWarning("Failed to deserialize Kafka message: {Message}", messageJson);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var roomRepository = scope.ServiceProvider.GetRequiredService<IRoomRepository>();

            var room = await roomRepository.GetOrCreateAsync(message.Name, ct);

            switch (message.Type)
            {
                case "air_quality":
                    await ProcessAirQualityAsync(scope, message, room.Id, ct);
                    break;
                case "energy":
                    await ProcessEnergyAsync(scope, message, room.Id, ct);
                    break;
                case "motion":
                    await ProcessMotionAsync(scope, message, room.Id, ct);
                    break;
                default:
                    _logger.LogWarning("Unknown message type: {Type}", message.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Kafka message");
        }
    }

    private async Task ProcessAirQualityAsync(
        IServiceScope scope, KafkaMessage message, string roomId, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<AirQualityPayload>(message.Payload.GetRawText());
        if (payload is null) return;

        var repository = scope.ServiceProvider.GetRequiredService<IAirQualityRepository>();
        var entity = new AirQuality
        {
            RoomId = roomId,
            Co2 = payload.Co2,
            Pm25 = payload.Pm25,
            Humidity = payload.Humidity,
            Timestamp = message.Timestamp
        };

        await repository.InsertAsync(entity, ct);
        _logger.LogInformation("Saved AirQuality data for room {RoomId}", roomId);
    }

    private async Task ProcessEnergyAsync(
        IServiceScope scope, KafkaMessage message, string roomId, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<EnergyPayload>(message.Payload.GetRawText());
        if (payload is null) return;

        var repository = scope.ServiceProvider.GetRequiredService<IEnergyRepository>();
        var entity = new Energy
        {
            RoomId = roomId,
            Amount = payload.Energy,
            Timestamp = message.Timestamp
        };

        await repository.InsertAsync(entity, ct);
        _logger.LogInformation("Saved Energy data for room {RoomId}", roomId);
    }

    private async Task ProcessMotionAsync(
        IServiceScope scope, KafkaMessage message, string roomId, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<MotionPayload>(message.Payload.GetRawText());
        if (payload is null) return;

        var repository = scope.ServiceProvider.GetRequiredService<IMotionRepository>();
        var entity = new Motion
        {
            RoomId = roomId,
            IsDetected = payload.MotionDetected,
            Timestamp = message.Timestamp
        };

        await repository.InsertAsync(entity, ct);
        _logger.LogInformation("Saved Motion data for room {RoomId}", roomId);
    }
}
