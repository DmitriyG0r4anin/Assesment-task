using DataProcessor.Application.Models;
using DataProcessor.Domain.Common;
using MediatR;

namespace DataProcessor.Application.Queries.GetRooms;

public record GetRoomsQuery(
    DateTime? TimestampStart,
    DateTime? TimestampEnd
) : IRequest<Result<List<RoomModel>>>;
