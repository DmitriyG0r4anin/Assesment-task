using DataProcessor.Application.Models;
using DataProcessor.Domain.Common;
using MediatR;

namespace DataProcessor.Application.Queries.GetRoom;

public record GetRoomQuery(string RoomId) : IRequest<Result<RoomModel>>;
