using DataProcessor.Application.Models;
using DataProcessor.Domain.Common;
using MediatR;

namespace DataProcessor.Application.Queries.GetAirQuality;

public record GetAirQualityQuery(
    string AirQualityId
) : IRequest<Result<AirQualityModel>>;
