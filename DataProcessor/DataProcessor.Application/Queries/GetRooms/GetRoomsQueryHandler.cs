using DataProcessor.Application.Abstractions.Repositories;
using DataProcessor.Application.Models;
using DataProcessor.Domain.Common;
using Mapster;
using MediatR;

namespace DataProcessor.Application.Queries.GetRooms;

public class GetRoomsQueryHandler(IRoomRepository roomRepository)
    : IRequestHandler<GetRoomsQuery, Result<List<RoomModel>>>
{
    public async Task<Result<List<RoomModel>>> Handle(
        GetRoomsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var rooms = await roomRepository.GetAllAsync(cancellationToken);
            return rooms.Select(r => r.Adapt<RoomModel>()).ToList();
        }
        catch (Exception)
        {
            return Error.InternalError;
        }
    }
}
