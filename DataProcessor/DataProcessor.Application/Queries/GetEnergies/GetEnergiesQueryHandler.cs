using DataProcessor.Application.Abstractions.Repositories.Base;
using DataProcessor.Application.Models;
using DataProcessor.Domain.Common;
using DataProcessor.Domain.Entities;
using Mapster;
using MediatR;

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

        return energies.Select(energy => energy.Adapt<EnergyModel>()).ToList();
    }
}
