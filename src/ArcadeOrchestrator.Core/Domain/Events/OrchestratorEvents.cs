using ArcadeOrchestrator.Core.Domain.Entities;

namespace ArcadeOrchestrator.Core.Domain.Events;

public sealed record SessionEndedEvent(
    Game Game,
    string TriggeredByStrategy,
    DateTime OccurredAt
);

public sealed record GameLaunchedEvent(
    Game Game,
    DateTime OccurredAt
);

public sealed record RotationPausedEvent(DateTime OccurredAt);
public sealed record RotationResumedEvent(DateTime OccurredAt);
