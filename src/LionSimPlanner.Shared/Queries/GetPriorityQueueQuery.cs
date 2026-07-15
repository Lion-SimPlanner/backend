using LionSimPlanner.Shared.Dtos;

namespace LionSimPlanner.Shared.Queries;

/// <summary>
/// Issued by the Scheduling module when the Admin loads the Session Pairing Builder.
/// The Personnel module resolves this by querying hr.pilots, ordered by NextTrainingDue ASC
/// so the most urgently overdue crew members appear first.
/// </summary>
/// <param name="SyllabusFilter">Optional: restrict results to pilots needing a specific syllabus type.</param>
/// <param name="TypeRatingFilter">Optional: restrict results to pilots with a specific type rating.</param>
public record GetPriorityQueueQuery(
    string? SyllabusFilter = null,
    string? TypeRatingFilter = null)
    : MediatR.IRequest<IReadOnlyList<PilotPriorityDto>>;
