using DataProcessor.Application.Queries.GetRoom;
using DataProcessor.Application.Queries.GetRooms;
using DataProcessor.Domain.Common;

namespace DataProcessor.Presentation.Services;

public class RoomGrpcService(
    IMediator mediator,
    ILogger<RoomGrpcService> logger)
    : RoomService.RoomServiceBase
{
    public override async Task<GetRoomsResponse> GetRooms(
        GetRoomsRequest request,
        ServerCallContext context)
    {
        logger.LogInformation("GetRooms called. TimestampStart: {TimestampStart}, TimestampEnd: {TimestampEnd}",
            request.TimestampStart is not null ? request.TimestampStart.ToString() : "any",
            request.TimestampEnd is not null ? request.TimestampEnd.ToString() : "any");

        var query = new GetRoomsQuery(
            TimestampStart: request.TimestampStart?.ToDateTime(),
            TimestampEnd: request.TimestampEnd?.ToDateTime());

        var result = await mediator.Send(query, context.CancellationToken);

        if (!result.IsSuccess)
        {
            return new GetRoomsResponse
            {
                Error = new ErrorResponse
                {
                    Code = result.Error.Code,
                    Message = result.Error.Message
                }
            };
        }

        var list = new RoomList();
        list.Rooms.AddRange(result.Value!.Select(m => m.Adapt<RoomMessage>()));

        return new GetRoomsResponse { Data = list };
    }

    public override async Task<GetRoomResponse> GetRoom(
        GetRoomRequest request,
        ServerCallContext context)
    {
        logger.LogInformation("GetRoom called. Id: {Id}", request.RoomId);

        if (string.IsNullOrEmpty(request.RoomId))
        {
            var error = Error.Validation("Id field is empty");

            return new GetRoomResponse
            {
                Error = error.Adapt<ErrorResponse>()
            };
        }


        var query = new GetRoomQuery(RoomId: request.RoomId);

        var result = await mediator.Send(query, context.CancellationToken);

        if (!result.IsSuccess)
        {
            return new GetRoomResponse
            {
                Error = new ErrorResponse
                {
                    Code = result.Error.Code,
                    Message = result.Error.Message
                }
            };
        }

        return new GetRoomResponse { Data = result.Value!.Adapt<RoomMessage>() };
    }
}
