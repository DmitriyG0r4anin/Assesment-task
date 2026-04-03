using DataProcessor.Application.Models;
using DataProcessor.Domain.Common;
using MediatR;

namespace DataProcessor.Application.Queries.GetEnergy;

public record GetEnergyQuery(
    string EnergyId
) : IRequest<Result<EnergyModel>>;
