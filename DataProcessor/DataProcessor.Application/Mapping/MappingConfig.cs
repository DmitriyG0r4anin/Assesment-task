using DataProcessor.Application.DTOs;
using DataProcessor.Domain.Entities;
using Mapster;

namespace DataProcessor.Application.Mapping;

public static class MappingConfig
{
#pragma warning disable CS8603 // Possible null reference return (Mapster Ignore API)
    public static void RegisterMappings()
    {
        TypeAdapterConfig<AirQuality, ParameterDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Pm25, src => src.Pm25)
            .Map(dest => dest.Co2, src => src.Co2)
            .Map(dest => dest.Humidity, src => src.Humidity)
            .Map(dest => dest.Timestamp, src => src.Timestamp)
            .Ignore(dest => dest.Amount)
            .Ignore(dest => dest.IsDetected)
            .Ignore(dest => dest.Type)
            .Ignore(dest => dest.RoomName);

        TypeAdapterConfig<Energy, ParameterDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Amount, src => src.Amount)
            .Map(dest => dest.Timestamp, src => src.Timestamp)
            .Ignore(dest => dest.Pm25)
            .Ignore(dest => dest.Co2)
            .Ignore(dest => dest.Humidity)
            .Ignore(dest => dest.IsDetected)
            .Ignore(dest => dest.Type)
            .Ignore(dest => dest.RoomName);

        TypeAdapterConfig<Motion, ParameterDto>.NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.IsDetected, src => src.IsDetected)
            .Map(dest => dest.Timestamp, src => src.Timestamp)
            .Ignore(dest => dest.Pm25)
            .Ignore(dest => dest.Co2)
            .Ignore(dest => dest.Humidity)
            .Ignore(dest => dest.Amount)
            .Ignore(dest => dest.Type)
            .Ignore(dest => dest.RoomName);
    }
#pragma warning restore CS8603
}
