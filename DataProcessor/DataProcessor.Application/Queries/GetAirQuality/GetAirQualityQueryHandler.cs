namespace DataProcessor.Application.Queries.GetAirQuality;

public class GetAirQualityQueryHandler(IMetricBaseRepository<AirQuality> airQualityRepository)
    : IRequestHandler<GetAirQualityQuery, Result<AirQualityModel>>
{
    public async Task<Result<AirQualityModel>> Handle(
        GetAirQualityQuery request,
        CancellationToken cancellationToken)
    {
        var airQuality = await airQualityRepository.GetByIdAsync(request.AirQualityId, cancellationToken);

        if (airQuality is null)
            return Error.NotFound;

        return airQuality.Adapt<AirQualityModel>();
    }
}
