using ArcadeOrchestrator.Core.Domain.Entities;

namespace ArcadeOrchestrator.Core.Application.Interfaces;

public interface IOrchestrationLogger
{
    void LogSessionStarted(Game game);
    void LogSessionEnded(Game game, string strategy, TimeSpan duration);
    void LogRotation(Game from, Game to);
    void LogError(string context, Exception ex);
}
