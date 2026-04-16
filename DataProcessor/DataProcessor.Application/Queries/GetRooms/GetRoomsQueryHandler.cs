namespace DataProcessor.Application.Queries.GetRooms;

public class GetRoomsQueryHandler(IRoomRepository roomRepository)
    : IRequestHandler<GetRoomsQuery, Result<List<RoomModel>>>
{
    public async Task<Result<List<RoomModel>>> Handle(
        GetRoomsQuery request,
        CancellationToken cancellationToken)
    {
        var rooms = await roomRepository.GetAllAsync(cancellationToken);

        return rooms.Adapt<List<RoomModel>>();
    }
}
