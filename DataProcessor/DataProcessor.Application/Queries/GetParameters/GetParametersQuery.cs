using DataProcessor.Application.DTOs;
using DataProcessor.Domain.Common;
using MediatR;

namespace DataProcessor.Application.Queries.GetParameters;

public record GetParametersQuery(
    DateTime? Timestamp,
    string? Room
) : IRequest<Result<List<ParameterDto>>>;
