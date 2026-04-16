using DataProcessor.Infrastructure.Configuration;
using DataProcessor.Infrastructure.Messaging;
using DataProcessor.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DataProcessor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MongoDbOptions>(
            configuration.GetSection(MongoDbOptions.SectionName));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoDbOptions>>();
            var client = new MongoClient(options.Value.ConnectionString);
            return client.GetDatabase(options.Value.DatabaseName);
        });

        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped(typeof(IMetricBaseRepository<>), typeof(MetricBaseRepository<>));

        services.Configure<KafkaConfig>(
            configuration.GetSection(KafkaConfig.SectionName));

        services.AddHostedService<KafkaConsumerService>();

        return services;
    }
}
