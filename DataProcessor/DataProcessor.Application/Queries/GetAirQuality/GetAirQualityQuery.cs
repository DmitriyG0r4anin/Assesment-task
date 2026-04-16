namespace DataProcessor.Application.Queries.GetAirQuality;

public record GetAirQualityQuery(
    string AirQualityId
) : IRequest<Result<AirQualityModel>>;
