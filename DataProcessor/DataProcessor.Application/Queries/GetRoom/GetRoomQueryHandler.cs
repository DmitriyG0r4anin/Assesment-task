namespace DataProcessor.Application.Queries.GetRoom;

public class GetRoomQueryHandler(IRoomRepository roomRepository)
    : IRequestHandler<GetRoomQuery, Result<RoomModel>>
{
    public async Task<Result<RoomModel>> Handle(
        GetRoomQuery request,
        CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetByIdAsync(request.RoomId, cancellationToken);

        if (room is null)
            return Error.NotFound;

        return room.Adapt<RoomModel>();
    }
}
