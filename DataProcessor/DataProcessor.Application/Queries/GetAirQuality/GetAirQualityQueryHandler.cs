using DataProcessor.Application.Abstractions.Repositories.Base;
using DataProcessor.Application.Models;
using DataProcessor.Domain.Common;
using DataProcessor.Domain.Entities;
using Mapster;
using MediatR;

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
