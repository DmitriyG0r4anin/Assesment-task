namespace DataProcessor.Application.Queries.GetEnergy;

public record GetEnergyQuery(
    string EnergyId
) : IRequest<Result<EnergyModel>>;
