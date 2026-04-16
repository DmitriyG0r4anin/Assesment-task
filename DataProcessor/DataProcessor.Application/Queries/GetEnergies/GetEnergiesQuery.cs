namespace DataProcessor.Application.Queries.GetEnergies;

public record GetEnergiesQuery(
    DateTime? TimestampStart,
    DateTime? TimestampEnd,
    string? RoomId
) : IRequest<Result<List<EnergyModel>>>;
