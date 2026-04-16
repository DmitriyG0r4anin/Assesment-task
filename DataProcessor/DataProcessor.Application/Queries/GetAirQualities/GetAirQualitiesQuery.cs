namespace DataProcessor.Application.Queries.GetAirQualities;

public record GetAirQualitiesQuery(
    DateTime? TimestampStart,
    DateTime? TimestampEnd,
    string? RoomId
) : IRequest<Result<List<AirQualityModel>>>;
