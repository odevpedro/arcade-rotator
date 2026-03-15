using ArcadeOrchestrator.Core.Domain.Entities;

namespace ArcadeOrchestrator.Core.Application.Interfaces;

public interface IGameCatalog
{
    IReadOnlyList<Franchise> GetAllFranchises();
    Game? FindByRomFileName(string romFileName);
    IReadOnlyList<Game> GetAllGames();
}
