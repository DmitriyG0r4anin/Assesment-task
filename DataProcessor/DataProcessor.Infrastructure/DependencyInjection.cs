using DataProcessor.Application.Abstractions.Repositories;
using DataProcessor.Infrastructure.Messaging;
using DataProcessor.Infrastructure.Persistence;
using DataProcessor.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DataProcessor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // MongoDB
        services.Configure<MongoDbSettings>(
            configuration.GetSection("MongoDb"));
        services.AddSingleton<MongoDbContext>();

        // Repositories
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IAirQualityRepository, AirQualityRepository>();
        services.AddScoped<IEnergyRepository, EnergyRepository>();
        services.AddScoped<IMotionRepository, MotionRepository>();

        // Kafka
        services.Configure<KafkaSettings>(
            configuration.GetSection("Kafka"));
        services.AddHostedService<KafkaConsumerService>();

        return services;
    }
}
