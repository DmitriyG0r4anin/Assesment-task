using DataProcessor.Application.Models;
using DataProcessor.Domain.Common;
using MediatR;

namespace DataProcessor.Application.Queries.GetMotions;

public record GetMotionsQuery(
    DateTime? TimestampStart,
    DateTime? TimestampEnd,
    string? RoomId
) : IRequest<Result<List<MotionModel>>>;
