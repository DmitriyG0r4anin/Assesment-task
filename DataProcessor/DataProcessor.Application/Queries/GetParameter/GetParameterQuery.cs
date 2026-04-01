using DataProcessor.Application.DTOs;
using DataProcessor.Domain.Common;
using MediatR;

namespace DataProcessor.Application.Queries.GetParameter;

public record GetParameterQuery(
    string ParameterId,
    DateTime? Timestamp,
    string? Room
) : IRequest<Result<ParameterDto>>;
