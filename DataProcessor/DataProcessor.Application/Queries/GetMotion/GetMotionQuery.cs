using DataProcessor.Application.Models;
using DataProcessor.Domain.Common;
using MediatR;

namespace DataProcessor.Application.Queries.GetMotion;

public record GetMotionQuery(string MotionId) : IRequest<Result<MotionModel>>;
