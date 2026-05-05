using DataProcessor.Application.Queries.GetMotion;
using DataProcessor.Application.Queries.GetMotions;
using DataProcessor.Domain.Common;

namespace DataProcessor.Presentation.Services;

public class MotionGrpcService(
    IMediator mediator,
    ILogger<MotionGrpcService> logger)
    : MotionService.MotionServiceBase
{
    public override async Task<GetMotionsResponse> GetMotions(
        GetMotionsRequest request,
        ServerCallContext context)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "GetMotions called. RoomId: {RoomId}, TimestampStart: {TimestampStart}, TimestampEnd: {TimestampEnd}",
                request.HasRoomId ? request.RoomId : "all",
                request.TimestampStart is not null ? request.TimestampStart.ToString() : "any",
                request.TimestampEnd is not null ? request.TimestampEnd.ToString() : "any");
        }

        var query = new GetMotionsQuery(
            TimestampStart: request.TimestampStart?.ToDateTime(),
            TimestampEnd: request.TimestampEnd?.ToDateTime(),
            RoomId: request.HasRoomId ? request.RoomId : null);

        var result = await mediator.Send(query, context.CancellationToken);

        var list = new MotionList();
        list.Motions.AddRange(result.Value!.Select(m => m.Adapt<MotionMessage>()));

        return new GetMotionsResponse { Data = list };
    }

    public override async Task<GetMotionResponse> GetMotion(
        GetMotionRequest request,
        ServerCallContext context)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("GetMotion called. Id: {Id}", request.MotionId);
        }

        if (string.IsNullOrEmpty(request.MotionId))
        {
            var error = Error.Validation("Id field is empty");

            return new GetMotionResponse
            {
                Error = error.Adapt<ErrorResponse>()
            };
        }

        var query = new GetMotionQuery(MotionId: request.MotionId);

        var result = await mediator.Send(query, context.CancellationToken);

        if (!result.IsSuccess)
        {
            return new GetMotionResponse
            {
                Error = new ErrorResponse
                {
                    Code = result.Error.Code,
                    Message = result.Error.Message
                }
            };
        }

        return new GetMotionResponse { Data = result.Value!.Adapt<MotionMessage>() };
    }
}
