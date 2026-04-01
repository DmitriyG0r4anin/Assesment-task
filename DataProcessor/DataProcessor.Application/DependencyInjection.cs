using DataProcessor.Application.Mapping;
using Microsoft.Extensions.DependencyInjection;

namespace DataProcessor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        MappingConfig.RegisterMappings();

        return services;
    }
}
