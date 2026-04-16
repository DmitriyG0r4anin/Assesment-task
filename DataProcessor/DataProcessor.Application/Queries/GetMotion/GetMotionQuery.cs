namespace DataProcessor.Application.Queries.GetMotion;

public record GetMotionQuery(string MotionId) : IRequest<Result<MotionModel>>;
