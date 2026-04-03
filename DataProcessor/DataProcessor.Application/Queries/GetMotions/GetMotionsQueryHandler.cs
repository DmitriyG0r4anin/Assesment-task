using DataProcessor.Application.Abstractions.Repositories.Base;
using DataProcessor.Application.Models;
using DataProcessor.Domain.Common;
using DataProcessor.Domain.Entities;
using Mapster;
using MediatR;

namespace DataProcessor.Application.Queries.GetMotions;

public class GetMotionsQueryHandler(
    IMetricBaseRepository<Motion> motionRepository)
    : IRequestHandler<GetMotionsQuery, Result<List<MotionModel>>>
{
    public async Task<Result<List<MotionModel>>> Handle(
        GetMotionsQuery request,
        CancellationToken cancellationToken)
    {
        var motions = await motionRepository.GetAllAsync(
            request.RoomId, request.TimestampStart, request.TimestampEnd, cancellationToken);

        return motions.Select(motion => motion.Adapt<MotionModel>()).ToList();
    }
}
