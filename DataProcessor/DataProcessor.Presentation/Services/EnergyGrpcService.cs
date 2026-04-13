using DataProcessor.Application.Queries.GetEnergies;
using DataProcessor.Application.Queries.GetEnergy;
using DataProcessor.Domain.Common;

namespace DataProcessor.Presentation.Services;

public class EnergyGrpcService(
    IMediator mediator,
    ILogger<EnergyGrpcService> logger)
    : EnergyService.EnergyServiceBase
{
    public override async Task<GetEnergiesResponse> GetEnergies(
        GetEnergiesRequest request,
        ServerCallContext context)
    {
        logger.LogInformation(
            "GetEnergies called. RoomId: {RoomId}, TimestampStart: {TimestampStart}, TimestampEnd: {TimestampEnd}",
            request.HasRoomId ? request.RoomId : "all",
            request.TimestampStart is not null ? request.TimestampStart.ToString() : "any",
            request.TimestampEnd is not null ? request.TimestampEnd.ToString() : "any");

        var query = new GetEnergiesQuery(
            TimestampStart: request.TimestampStart?.ToDateTime(),
            TimestampEnd: request.TimestampEnd?.ToDateTime(),
            RoomId: request.HasRoomId ? request.RoomId : null);

        var result = await mediator.Send(query, context.CancellationToken);

        if (!result.IsSuccess)
        {
            return new GetEnergiesResponse
            {
                Error = new ErrorResponse
                {
                    Code = result.Error.Code,
                    Message = result.Error.Message
                }
            };
        }

        var list = new EnergyList();
        list.Energies.AddRange(result.Value!.Select(m => m.Adapt<EnergyMessage>()));

        return new GetEnergiesResponse { Data = list };
    }

    public override async Task<GetEnergyResponse> GetEnergy(
        GetEnergyRequest request,
        ServerCallContext context)
    {
        logger.LogInformation("GetEnergy called. Id: {Id}", request.EnergyId);

        if (string.IsNullOrEmpty(request.EnergyId))
        {
            var error = Error.Validation("Id field is empty");

            return new GetEnergyResponse
            {
                Error = error.Adapt<ErrorResponse>()
            };
        }

        var query = new GetEnergyQuery(EnergyId: request.EnergyId);

        var result = await mediator.Send(query, context.CancellationToken);

        if (!result.IsSuccess)
        {
            return new GetEnergyResponse
            {
                Error = new ErrorResponse
                {
                    Code = result.Error.Code,
                    Message = result.Error.Message
                }
            };
        }

        return new GetEnergyResponse { Data = result.Value!.Adapt<EnergyMessage>() };
    }
}
