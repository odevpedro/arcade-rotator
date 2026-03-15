using ArcadeOrchestrator.Core.Application.Interfaces;
using ArcadeOrchestrator.Core.Application.Services;
using ArcadeOrchestrator.Core.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace ArcadeOrchestrator.Core.Tests.Application;

public sealed class RotationEngineTests
{
    private static Game MakeGame(string id, int weight = 10) =>
        new(id, $"Game {id}", $"{id}.zip", $"C:\\roms\\{id}.zip", weight, Array.Empty<string>());

    private static Franchise MakeFranchise(string id, params Game[] games) =>
        new(id, $"Franchise {id}", 10, games);

    private static IGameCatalog CatalogWith(params Franchise[] franchises)
    {
        var catalog = Substitute.For<IGameCatalog>();
        catalog.GetAllFranchises().Returns(franchises.ToList().AsReadOnly());
        catalog.GetAllGames().Returns(franchises.SelectMany(f => f.Games).ToList().AsReadOnly());
        return catalog;
    }

    [Fact]
    public void PickNext_ShouldReturnGame_WhenCatalogHasGames()
    {
        var game = MakeGame("kof99");
        var catalog = CatalogWith(MakeFranchise("kof", game));
        var engine = new RotationEngine(catalog, new RotationConfig());

        var result = engine.PickNext(null);

        result.Should().Be(game);
    }

    [Fact]
    public void PickNext_ShouldThrow_WhenCatalogIsEmpty()
    {
        var catalog = CatalogWith();
        var engine = new RotationEngine(catalog, new RotationConfig());

        var act = () => engine.PickNext(null);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*catálogo*");
    }

    [Fact]
    public void PickNext_ShouldNotRepeatGameImmediately_WhenAntiRepeatIsActive()
    {
        var g1 = MakeGame("kof99");
        var g2 = MakeGame("kof2002");
        var catalog = CatalogWith(MakeFranchise("kof", g1, g2));
        var engine = new RotationEngine(catalog, new RotationConfig { AntiRepeatCount = 1, FranchiseBias = 1.0 });

        var first  = engine.PickNext(null);
        var second = engine.PickNext(first.Id);

        second.Should().NotBe(first);
    }

    [Fact]
    public void PickNext_ShouldClearAntiRepeat_WhenAllGamesAreExhausted()
    {
        var g1 = MakeGame("kof99");
        var catalog = CatalogWith(MakeFranchise("kof", g1));
        var engine = new RotationEngine(catalog, new RotationConfig { AntiRepeatCount = 5 });

        // Mesmo com anti-repeat maior que o catálogo, nunca trava
        var results = Enumerable.Range(0, 5)
            .Select(_ => engine.PickNext(null))
            .ToList();

        results.Should().AllSatisfy(g => g.Should().Be(g1));
    }

    [Fact]
    public void PickNext_ShouldRespectWeights_StatisticallyOverManyRuns()
    {
        var heavy = MakeGame("heavy", weight: 90);
        var light = MakeGame("light", weight: 10);
        var catalog = CatalogWith(MakeFranchise("franchise", heavy, light));
        var engine = new RotationEngine(catalog, new RotationConfig { AntiRepeatCount = 0 });

        var counts = new Dictionary<string, int> { ["heavy"] = 0, ["light"] = 0 };
        for (var i = 0; i < 1000; i++)
        {
            var picked = engine.PickNext(null);
            counts[picked.Id]++;
        }

        // Heavy deveria ser escolhido ~90% das vezes — tolerância de 10pp
        counts["heavy"].Should().BeGreaterThan(750);
        counts["light"].Should().BeLessThan(250);
    }
}
