using DataProcessor.Application.Models;
using DataProcessor.Domain.Common;
using MediatR;

namespace DataProcessor.Application.Queries.GetAirQualities;

public record GetAirQualitiesQuery(
    DateTime? TimestampStart,
    DateTime? TimestampEnd,
    string? RoomId
) : IRequest<Result<List<AirQualityModel>>>;
