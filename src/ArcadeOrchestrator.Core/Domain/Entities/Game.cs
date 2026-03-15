namespace ArcadeOrchestrator.Core.Domain.Entities;

public sealed record Game(
    string Id,
    string DisplayName,
    string Rom,
    string RomPath,
    int Weight,
    IReadOnlyList<string> Tags
) : IWeighted;

public interface IWeighted
{
    int Weight { get; }
}
