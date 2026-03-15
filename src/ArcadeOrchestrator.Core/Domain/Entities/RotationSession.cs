namespace ArcadeOrchestrator.Core.Domain.Entities;

public sealed record RotationSession(
    Guid Id,
    Game Game,
    DateTime StartedAt,
    DateTime? EndedAt,
    string? TriggeredByStrategy
);
