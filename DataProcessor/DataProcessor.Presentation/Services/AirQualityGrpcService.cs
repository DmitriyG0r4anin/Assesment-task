using DataProcessor.Application.Queries.GetAirQualities;
using DataProcessor.Application.Queries.GetAirQuality;
using DataProcessor.Domain.Common;

namespace DataProcessor.Presentation.Services;

public class AirQualityGrpcService(
    IMediator mediator,
    ILogger<AirQualityGrpcService> logger)
    : AirQualityService.AirQualityServiceBase
{
    public override async Task<GetAirQualitiesResponse> GetAirQualities(
        GetAirQualitiesRequest request,
        ServerCallContext context)
    {
        logger.LogInformation(
            "GetAirQualities called. RoomId: {RoomId}, TimestampStart: {TimestampStart}, TimestampEnd: {TimestampEnd}",
            request.HasRoomId ? request.RoomId : "all",
            request.TimestampStart is not null ? request.TimestampStart.ToString() : "any",
            request.TimestampEnd is not null ? request.TimestampEnd.ToString() : "any");

        var query = new GetAirQualitiesQuery(
            TimestampStart: request.TimestampStart?.ToDateTime(),
            TimestampEnd: request.TimestampEnd?.ToDateTime(),
            RoomId: request.HasRoomId ? request.RoomId : null);

        var result = await mediator.Send(query, context.CancellationToken);

        if (!result.IsSuccess)
        {
            return new GetAirQualitiesResponse
            {
                Error = new ErrorResponse
                {
                    Code = result.Error.Code,
                    Message = result.Error.Message
                }
            };
        }

        var list = new AirQualityList();
        list.AirQualities.AddRange(result.Value!.Select(m => m.Adapt<AirQualityMessage>()));

        return new GetAirQualitiesResponse { Data = list };
    }

    public override async Task<GetAirQualityResponse> GetAirQuality(
        GetAirQualityRequest request,
        ServerCallContext context)
    {
        logger.LogInformation("GetAirQuality called. Id: {Id}", request.AirQualityId);

        if (string.IsNullOrEmpty(request.AirQualityId))
        {
            var error = Error.Validation("Id field is empty");

            return new GetAirQualityResponse
            {
                Error = error.Adapt<ErrorResponse>()
            };
        }


        var query = new GetAirQualityQuery(AirQualityId: request.AirQualityId);

        var result = await mediator.Send(query, context.CancellationToken);

        if (!result.IsSuccess)
        {
            return new GetAirQualityResponse
            {
                Error = new ErrorResponse
                {
                    Code = result.Error.Code,
                    Message = result.Error.Message
                }
            };
        }

        return new GetAirQualityResponse { Data = result.Value!.Adapt<AirQualityMessage>() };
    }
}
