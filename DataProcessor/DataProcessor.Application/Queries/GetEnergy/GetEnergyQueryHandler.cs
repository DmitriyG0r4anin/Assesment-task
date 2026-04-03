using DataProcessor.Application.Abstractions.Repositories.Base;
using DataProcessor.Application.Models;
using DataProcessor.Domain.Common;
using DataProcessor.Domain.Entities;
using Mapster;
using MediatR;

namespace DataProcessor.Application.Queries.GetEnergy;

public class GetEnergyQueryHandler(IMetricBaseRepository<Energy> energyRepository)
    : IRequestHandler<GetEnergyQuery, Result<EnergyModel>>
{
    public async Task<Result<EnergyModel>> Handle(
        GetEnergyQuery request,
        CancellationToken cancellationToken)
    {
        var energy = await energyRepository.GetByIdAsync(request.EnergyId, cancellationToken);

        if (energy is null)
            return Error.NotFound;

        return energy.Adapt<EnergyModel>();
    }
}
