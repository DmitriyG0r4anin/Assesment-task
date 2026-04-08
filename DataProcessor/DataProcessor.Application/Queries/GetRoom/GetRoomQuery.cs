namespace DataProcessor.Application.Queries.GetRoom;

public record GetRoomQuery(string RoomId) : IRequest<Result<RoomModel>>;
