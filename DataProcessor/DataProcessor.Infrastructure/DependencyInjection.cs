using DataProcessor.Application.Abstractions.Repositories;
using DataProcessor.Application.Abstractions.Repositories.Base;
using DataProcessor.Infrastructure.Configuration;
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
        services.Configure<MongoDbOptions>(
            configuration.GetSection(MongoDbOptions.SectionName));

        services.AddSingleton<MongoDbContext>();

        // Repositories
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped(typeof(IMetricBaseRepository<>),typeof(MetricBaseRepository<>));

        // Kafka
        services.Configure<KafkaConfig>(
            configuration.GetSection(KafkaConfig.SectionName));

        services.AddHostedService<KafkaConsumerService>();

        return services;
    }
}
