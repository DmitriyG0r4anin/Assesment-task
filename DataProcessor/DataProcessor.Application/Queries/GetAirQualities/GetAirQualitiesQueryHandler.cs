using DataProcessor.Application.Abstractions.Repositories;
using DataProcessor.Application.Abstractions.Repositories.Base;
using DataProcessor.Application.Models;
using DataProcessor.Domain.Common;
using DataProcessor.Domain.Entities;
using Mapster;
using MediatR;

namespace DataProcessor.Application.Queries.GetAirQualities;

public class GetAirQualitiesQueryHandler(
    IMetricBaseRepository<AirQuality> airQualityRepository)
    : IRequestHandler<GetAirQualitiesQuery, Result<List<AirQualityModel>>>
{
    public async Task<Result<List<AirQualityModel>>> Handle(
        GetAirQualitiesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var entities = await airQualityRepository.GetAllAsync(
                request.RoomId, request.TimestampStart, request.TimestampEnd, cancellationToken);

            return entities.Select(entity => entity.Adapt<AirQualityModel>()).ToList();
        }
        catch (Exception)
        {
            return Error.InternalError;
        }
    }
}
