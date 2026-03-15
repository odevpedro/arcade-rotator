using ArcadeOrchestrator.Core.Application.Interfaces;
using ArcadeOrchestrator.Core.Domain.Entities;

namespace ArcadeOrchestrator.Core.Application.Services;

/// <summary>
/// Sorteia o próximo jogo com pesos ponderados,
/// anti-repeat e bias de franquia configuráveis.
/// </summary>
public sealed class RotationEngine : IRotationEngine
{
    private readonly IGameCatalog _catalog;
    private readonly RotationConfig _config;
    private readonly Queue<string> _recentGameIds = new();
    private readonly Random _rng = new();
    private string? _lastFranchiseId;

    public RotationEngine(IGameCatalog catalog, RotationConfig config)
    {
        _catalog = catalog;
        _config = config;
    }

    public Game PickNext(string? currentGameId)
    {
        var allFranchises = _catalog.GetAllFranchises();

        if (!allFranchises.Any())
            throw new InvalidOperationException("Catálogo vazio. Adicione jogos em catalog.yaml.");

        // Tenta manter a mesma franquia com probabilidade FranchiseBias
        Franchise? franchise = null;
        if (_lastFranchiseId != null && _rng.NextDouble() < _config.FranchiseBias)
            franchise = allFranchises.FirstOrDefault(f => f.Id == _lastFranchiseId);

        franchise ??= PickWeighted(allFranchises);

        // Exclui jogos recentes (anti-repeat)
        var candidates = franchise.Games
            .Where(g => !_recentGameIds.Contains(g.Id))
            .ToList();

        // Se todos estão na memória, reseta e tenta novamente
        if (!candidates.Any())
        {
            _recentGameIds.Clear();
            candidates = franchise.Games.ToList();
        }

        var picked = PickWeighted(candidates);

        _recentGameIds.Enqueue(picked.Id);
        if (_recentGameIds.Count > _config.AntiRepeatCount)
            _recentGameIds.Dequeue();

        _lastFranchiseId = franchise.Id;
        return picked;
    }

    private T PickWeighted<T>(IList<T> items) where T : IWeighted
    {
        var total = items.Sum(i => i.Weight);
        if (total <= 0) return items[_rng.Next(items.Count)];

        var roll = _rng.NextDouble() * total;
        double acc = 0;
        foreach (var item in items)
        {
            acc += item.Weight;
            if (roll <= acc) return item;
        }
        return items.Last();
    }
}

public sealed class RotationConfig
{
    public int AntiRepeatCount { get; init; } = 3;
    public double FranchiseBias { get; init; } = 0.4;
}
