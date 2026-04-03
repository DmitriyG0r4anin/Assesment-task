using DataProcessor.Application.Abstractions.Repositories.Base;
using DataProcessor.Application.Models;
using DataProcessor.Domain.Common;
using DataProcessor.Domain.Entities;
using Mapster;
using MediatR;

namespace DataProcessor.Application.Queries.GetMotion;

public class GetMotionQueryHandler(IMetricBaseRepository<Motion> motionRepository)
    : IRequestHandler<GetMotionQuery, Result<MotionModel>>
{
    public async Task<Result<MotionModel>> Handle(
        GetMotionQuery request,
        CancellationToken cancellationToken)
    {
        var motion = await motionRepository.GetByIdAsync(request.MotionId, cancellationToken);

        if (motion is null)
            return Error.NotFound;

        return motion.Adapt<MotionModel>();
    }
}
