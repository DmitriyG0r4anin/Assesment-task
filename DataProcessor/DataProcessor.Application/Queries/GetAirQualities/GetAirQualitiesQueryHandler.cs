namespace DataProcessor.Application.Queries.GetAirQualities;

public class GetAirQualitiesQueryHandler(
    IMetricBaseRepository<AirQuality> airQualityRepository)
    : IRequestHandler<GetAirQualitiesQuery, Result<List<AirQualityModel>>>
{
    public async Task<Result<List<AirQualityModel>>> Handle(
        GetAirQualitiesQuery request,
        CancellationToken cancellationToken)
    {
        var entities = await airQualityRepository.GetAllAsync(
            request.RoomId, request.TimestampStart, request.TimestampEnd, cancellationToken);

        return entities.Adapt<List<AirQualityModel>>();
    }
}
