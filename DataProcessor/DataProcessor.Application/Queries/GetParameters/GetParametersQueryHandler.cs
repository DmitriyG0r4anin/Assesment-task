using DataProcessor.Application.Abstractions.Repositories;
using DataProcessor.Application.DTOs;
using DataProcessor.Domain.Common;
using DataProcessor.Domain.Entities;
using Mapster;
using MediatR;

namespace DataProcessor.Application.Queries.GetParameters;

public class GetParametersQueryHandler(
    IRoomRepository roomRepository,
    IAirQualityRepository airQualityRepository,
    IEnergyRepository energyRepository,
    IMotionRepository motionRepository)
    : IRequestHandler<GetParametersQuery, Result<List<ParameterDto>>>
{
    public async Task<Result<List<ParameterDto>>> Handle(
        GetParametersQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            string? roomId = null;
            string? roomName = request.Room;

            if (!string.IsNullOrEmpty(request.Room))
            {
                var room = await roomRepository.GetByNameAsync(request.Room, cancellationToken);
                if (room is null)
                    return Error.NotFound;

                roomId = room.Id;
                roomName = room.Name;
            }

            var parameters = new List<ParameterDto>();

            var airQualities = await airQualityRepository.GetAllAsync(roomId, request.Timestamp, cancellationToken);
            foreach (var aq in airQualities)
            {
                var dto = aq.Adapt<ParameterDto>();
                dto.Type = "air_quality";
                dto.RoomName = roomName ?? await GetRoomNameAsync(aq.RoomId, cancellationToken);
                parameters.Add(dto);
            }

            var energies = await energyRepository.GetAllAsync(roomId, request.Timestamp, cancellationToken);
            foreach (var e in energies)
            {
                var dto = e.Adapt<ParameterDto>();
                dto.Type = "energy";
                dto.RoomName = roomName ?? await GetRoomNameAsync(e.RoomId, cancellationToken);
                parameters.Add(dto);
            }

            var motions = await motionRepository.GetAllAsync(roomId, request.Timestamp, cancellationToken);
            foreach (var m in motions)
            {
                var dto = m.Adapt<ParameterDto>();
                dto.Type = "motion";
                dto.RoomName = roomName ?? await GetRoomNameAsync(m.RoomId, cancellationToken);
                parameters.Add(dto);
            }

            return parameters;
        }
        catch (Exception)
        {
            return Error.InternalError;
        }
    }

    private async Task<string> GetRoomNameAsync(string roomId, CancellationToken ct)
    {
        var room = await roomRepository.GetByIdAsync(roomId, ct);
        return room?.Name ?? "Unknown";
    }
}
