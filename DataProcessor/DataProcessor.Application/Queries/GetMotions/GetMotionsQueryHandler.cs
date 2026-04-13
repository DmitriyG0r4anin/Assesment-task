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

        return motions.Adapt<List<MotionModel>>();
    }
}
