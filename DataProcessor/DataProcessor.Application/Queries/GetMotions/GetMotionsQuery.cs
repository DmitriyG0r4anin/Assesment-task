namespace DataProcessor.Application.Queries.GetMotions;

public record GetMotionsQuery(
    DateTime? TimestampStart,
    DateTime? TimestampEnd,
    string? RoomId
) : IRequest<Result<List<MotionModel>>>;
