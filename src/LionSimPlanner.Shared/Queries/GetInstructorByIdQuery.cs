using LionSimPlanner.Shared.Dtos;
using MediatR;

namespace LionSimPlanner.Shared.Queries;

/// <summary>
/// Issued by the Scheduling module's Validation Gate to fetch a single instructor's
/// data without directly referencing the Personnel module.
/// Defined in Shared so Personnel.Infrastructure can handle it without depending on Scheduling.
/// </summary>
public record GetInstructorByIdQuery(Guid InstructorId)
    : IRequest<InstructorValidationData?>;
