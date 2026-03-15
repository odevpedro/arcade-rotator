using ArcadeOrchestrator.Core.Domain.Entities;

namespace ArcadeOrchestrator.Core.Application.Interfaces;

public interface IRotationEngine
{
    /// <summary>
    /// Sorteia o próximo jogo respeitando pesos, anti-repeat e bias de franquia.
    /// </summary>
    Game PickNext(string? currentGameId);
}
