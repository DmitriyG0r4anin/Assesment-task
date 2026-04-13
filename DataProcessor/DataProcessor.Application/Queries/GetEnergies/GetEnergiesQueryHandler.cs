namespace DataProcessor.Application.Queries.GetEnergies;

public class GetEnergiesQueryHandler(
    IMetricBaseRepository<Energy> energyRepository)
    : IRequestHandler<GetEnergiesQuery, Result<List<EnergyModel>>>
{
    public async Task<Result<List<EnergyModel>>> Handle(
        GetEnergiesQuery request,
        CancellationToken cancellationToken)
    {
        var energies = await energyRepository.GetAllAsync(
            request.RoomId, request.TimestampStart, request.TimestampEnd, cancellationToken);

        return energies.Adapt<List<EnergyModel>>();
    }
}
