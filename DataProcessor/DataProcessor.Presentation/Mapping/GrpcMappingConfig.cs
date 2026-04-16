using DataProcessor.Application.Models;
using Google.Protobuf.WellKnownTypes;

namespace DataProcessor.Presentation.Mapping;

public static class GrpcMappingConfig
{
    public static void RegisterMappings()
    {
        TypeAdapterConfig<AirQualityModel, AirQualityMessage>.NewConfig()
            .Map(dest => dest.Timestamp, src => ToUtcTimestamp(src.Timestamp));

        TypeAdapterConfig<EnergyModel, EnergyMessage>.NewConfig()
            .Map(dest => dest.Timestamp, src => ToUtcTimestamp(src.Timestamp));

        TypeAdapterConfig<MotionModel, MotionMessage>.NewConfig()
            .Map(dest => dest.Timestamp, src => ToUtcTimestamp(src.Timestamp));
    }

    private static Timestamp ToUtcTimestamp(DateTime dt) =>
        Timestamp.FromDateTime(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
}
