using DataProcessor.Application.Abstractions.Repositories;
using DataProcessor.Application.DTOs;
using DataProcessor.Domain.Common;
using Mapster;
using MediatR;

namespace DataProcessor.Application.Queries.GetParameter;

public class GetParameterQueryHandler(
    IRoomRepository roomRepository,
    IAirQualityRepository airQualityRepository,
    IEnergyRepository energyRepository,
    IMotionRepository motionRepository)
    : IRequestHandler<GetParameterQuery, Result<ParameterDto>>
{
    public async Task<Result<ParameterDto>> Handle(
        GetParameterQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Try to find in AirQuality
            var airQuality = await airQualityRepository.GetByIdAsync(request.ParameterId, cancellationToken);
            if (airQuality is not null)
            {
                if (!await ValidateFilters(airQuality.RoomId, airQuality.Timestamp, request, cancellationToken))
                    return Error.NotFound;

                var dto = airQuality.Adapt<ParameterDto>();
                dto.Type = "air_quality";
                dto.RoomName = await GetRoomNameAsync(airQuality.RoomId, cancellationToken);
                return dto;
            }

            // Try to find in Energy
            var energy = await energyRepository.GetByIdAsync(request.ParameterId, cancellationToken);
            if (energy is not null)
            {
                if (!await ValidateFilters(energy.RoomId, energy.Timestamp, request, cancellationToken))
                    return Error.NotFound;

                var dto = energy.Adapt<ParameterDto>();
                dto.Type = "energy";
                dto.RoomName = await GetRoomNameAsync(energy.RoomId, cancellationToken);
                return dto;
            }

            // Try to find in Motion
            var motion = await motionRepository.GetByIdAsync(request.ParameterId, cancellationToken);
            if (motion is not null)
            {
                if (!await ValidateFilters(motion.RoomId, motion.Timestamp, request, cancellationToken))
                    return Error.NotFound;

                var dto = motion.Adapt<ParameterDto>();
                dto.Type = "motion";
                dto.RoomName = await GetRoomNameAsync(motion.RoomId, cancellationToken);
                return dto;
            }

            return Error.NotFound;
        }
        catch (Exception)
        {
            return Error.InternalError;
        }
    }

    private async Task<bool> ValidateFilters(
        string roomId, DateTime entityTimestamp,
        GetParameterQuery request, CancellationToken ct)
    {
        if (request.Timestamp.HasValue && entityTimestamp != request.Timestamp.Value)
            return false;

        if (!string.IsNullOrEmpty(request.Room))
        {
            var room = await roomRepository.GetByNameAsync(request.Room, ct);
            if (room is null || room.Id != roomId)
                return false;
        }

        return true;
    }

    private async Task<string> GetRoomNameAsync(string roomId, CancellationToken ct)
    {
        var room = await roomRepository.GetByIdAsync(roomId, ct);
        return room?.Name ?? "Unknown";
    }
}
