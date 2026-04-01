using DataProcessor.Application.DTOs;
using DataProcessor.Application.Queries.GetParameter;
using DataProcessor.Application.Queries.GetParameters;
using DataProcessor.Presentation.Protos;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;

namespace DataProcessor.Presentation.Services;

public class ParameterGrpcService(
    IMediator mediator,
    ILogger<ParameterGrpcService> logger)
    : ParameterService.ParameterServiceBase
{
    public override async Task<GetParametersResponse> GetParameters(
        GetParametersRequest request,
        ServerCallContext context)
    {
        logger.LogInformation("GetParameters called. Room: {Room}, Timestamp: {Timestamp}",
            request.HasRoom ? request.Room : "all",
            request.Timestamp is not null ? request.Timestamp.ToString() : "any");

        var query = new GetParametersQuery(
            Timestamp: request.Timestamp is not null ? request.Timestamp.ToDateTime() : null,
            Room: request.HasRoom ? request.Room : null);

        var result = await mediator.Send(query, context.CancellationToken);

        if (result.IsFailure)
        {
            return new GetParametersResponse
            {
                Error = new ErrorResponse
                {
                    Code = result.Error.Code,
                    Message = result.Error.Message
                }
            };
        }

        var parameterList = new ParameterList();
        parameterList.Parameters.AddRange(
            result.Value!.Select(MapToGrpcMessage));

        return new GetParametersResponse { Data = parameterList };
    }

    public override async Task<GetParameterResponse> GetParameter(
        GetParameterRequest request,
        ServerCallContext context)
    {
        logger.LogInformation("GetParameter called. Id: {Id}, Room: {Room}, Timestamp: {Timestamp}",
            request.ParameterId,
            request.HasRoom ? request.Room : "any",
            request.Timestamp is not null ? request.Timestamp.ToString() : "any");

        var query = new GetParameterQuery(
            ParameterId: request.ParameterId,
            Timestamp: request.Timestamp is not null ? request.Timestamp.ToDateTime() : null,
            Room: request.HasRoom ? request.Room : null);

        var result = await mediator.Send(query, context.CancellationToken);

        if (result.IsFailure)
        {
            return new GetParameterResponse
            {
                Error = new ErrorResponse
                {
                    Code = result.Error.Code,
                    Message = result.Error.Message
                }
            };
        }

        return new GetParameterResponse
        {
            Data = MapToGrpcMessage(result.Value!)
        };
    }

    private static ParameterMessage MapToGrpcMessage(ParameterDto dto)
    {
        var msg = new ParameterMessage
        {
            Id = dto.Id,
            Type = dto.Type,
            RoomName = dto.RoomName,
            Timestamp = Timestamp.FromDateTime(DateTime.SpecifyKind(dto.Timestamp, DateTimeKind.Utc))
        };

        if (dto.Pm25.HasValue) msg.Pm25 = dto.Pm25.Value;
        if (dto.Co2.HasValue) msg.Co2 = dto.Co2.Value;
        if (dto.Humidity.HasValue) msg.Humidity = dto.Humidity.Value;
        if (dto.Amount.HasValue) msg.Amount = dto.Amount.Value;
        if (dto.IsDetected.HasValue) msg.IsDetected = dto.IsDetected.Value;

        return msg;
    }
}
