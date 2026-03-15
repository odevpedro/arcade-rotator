using ArcadeOrchestrator.Core.Application.Interfaces;
using ArcadeOrchestrator.Core.Domain.Entities;
using ArcadeOrchestrator.Infrastructure.Config;
using ArcadeOrchestrator.Infrastructure.Config.Models;

namespace ArcadeOrchestrator.Infrastructure.Catalog;

/// <summary>
/// Implementação de IGameCatalog que lê de catalog.yaml via ConfigManager.
/// O catálogo é carregado uma vez e mantido em memória.
/// </summary>
public sealed class YamlGameCatalogRepository : IGameCatalog
{
    private readonly IReadOnlyList<Franchise> _franchises;
    private readonly Dictionary<string, Game> _byRomFileName;

    public YamlGameCatalogRepository(ConfigManager configManager)
    {
        var catalogConfig = configManager.LoadCatalogConfig();
        _franchises = MapFranchises(catalogConfig);
        _byRomFileName = _franchises
            .SelectMany(f => f.Games)
            .ToDictionary(g => g.Rom, g => g, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<Franchise> GetAllFranchises() => _franchises;

    public IReadOnlyList<Game> GetAllGames() =>
        _franchises.SelectMany(f => f.Games).ToList();

    public Game? FindByRomFileName(string romFileName) =>
        _byRomFileName.TryGetValue(romFileName, out var game) ? game : null;

    private static IReadOnlyList<Franchise> MapFranchises(CatalogConfig config) =>
        config.Franchises
            .Select(f => new Franchise(
                f.Id,
                f.Name,
                f.Weight,
                f.Games.Select(g => new Game(
                    g.Id,
                    g.DisplayName,
                    g.Rom,
                    g.RomPath,
                    g.Weight,
                    g.Tags.AsReadOnly()
                )).ToList().AsReadOnly()
            ))
            .ToList()
            .AsReadOnly();
}
