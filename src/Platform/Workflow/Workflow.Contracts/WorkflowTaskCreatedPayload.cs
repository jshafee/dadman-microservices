namespace Workflow.Contracts;

public sealed record WorkflowTaskCreatedPayload(Guid TaskId, Guid WorkflowInstanceId, string TaskType, string Name, IReadOnlyCollection<Guid>? CandidatePrincipals, IReadOnlyCollection<Guid>? CandidateGroups, IReadOnlyCollection<string>? CandidateRoles, IReadOnlyCollection<Guid>? CandidateOrgUnits, IReadOnlyCollection<Guid>? CandidatePositions, IReadOnlyCollection<Guid>? ResolvedCandidatePrincipals, DateTimeOffset? DueAtUtc);
