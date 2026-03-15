namespace ArcadeOrchestrator.Core.Domain.Entities;

public sealed record Franchise(
    string Id,
    string Name,
    int Weight,
    IReadOnlyList<Game> Games
) : IWeighted;
