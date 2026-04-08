using DataProcessor.Application.Models;
using Google.Protobuf.WellKnownTypes;

namespace DataProcessor.Presentation.Mapping;

public static class GrpcMappingConfig
{
    public static void RegisterMappings()
    {
        TypeAdapterConfig<AirQualityModel, AirQualityMessage>.NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.RoomId, src => src.RoomId)
            .Map(dest => dest.Timestamp, src => ToUtcTimestamp(src.Timestamp))
            .Map(dest => dest.Pm25, src => src.Pm25)
            .Map(dest => dest.Co2, src => src.Co2)
            .Map(dest => dest.Humidity, src => src.Humidity);

        TypeAdapterConfig<EnergyModel, EnergyMessage>.NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.RoomId, src => src.RoomId)
            .Map(dest => dest.Timestamp, src => ToUtcTimestamp(src.Timestamp))
            .Map(dest => dest.Amount, src => src.Amount);

        TypeAdapterConfig<MotionModel, MotionMessage>.NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.RoomId, src => src.RoomId)
            .Map(dest => dest.Timestamp, src => ToUtcTimestamp(src.Timestamp))
            .Map(dest => dest.IsDetected, src => src.IsDetected);

        TypeAdapterConfig<RoomModel, RoomMessage>.NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name);
    }

    private static Timestamp ToUtcTimestamp(DateTime dt) =>
        Timestamp.FromDateTime(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
}
