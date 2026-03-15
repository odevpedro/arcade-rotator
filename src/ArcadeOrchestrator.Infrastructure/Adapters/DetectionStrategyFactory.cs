using ArcadeOrchestrator.Core.Application.Interfaces;
using ArcadeOrchestrator.Core.Detection;
using ArcadeOrchestrator.Core.Domain.Entities;
using ArcadeOrchestrator.Infrastructure.Config.Models;

namespace ArcadeOrchestrator.Infrastructure.Adapters;

/// <summary>
/// Resolve o conjunto de estratégias de detecção para um jogo específico,
/// consultando os overrides do YAML e o default configurado.
/// </summary>
public static class DetectionStrategyFactory
{
    public static IReadOnlyList<IDetectionStrategy> BuildFor(
        Game game,
        DetectionConfig config,
        string? logFilePath)
    {
        // Tenta override específico do jogo, fallback para default
        var overrideCfg = config.Overrides.TryGetValue(game.Rom, out var ov)
            ? ov
            : config.Default;

        var strategies = new List<IDetectionStrategy>();

        var strategyType = overrideCfg.Strategy.ToLowerInvariant();

        // ProcessWatch sempre entra em modo composite ou quando habilitado
        if (strategyType == "composite" || overrideCfg.ProcessWatchEnabled)
            strategies.Add(new ProcessWatchStrategy());

        switch (strategyType)
        {
            case "timeout":
            case "composite":
                strategies.Add(new TimeoutStrategy(
                    TimeSpan.FromSeconds(overrideCfg.TimeoutSeconds)));
                break;

            case "log_parser":
                if (!string.IsNullOrWhiteSpace(logFilePath) &&
                    !string.IsNullOrWhiteSpace(overrideCfg.LogPattern))
                {
                    strategies.Add(new LogParserStrategy(logFilePath, overrideCfg.LogPattern));
                }
                // Fallback para timeout se configurado
                if (overrideCfg.FallbackToTimeout)
                    strategies.Add(new TimeoutStrategy(
                        TimeSpan.FromSeconds(overrideCfg.TimeoutSeconds)));
                break;
        }

        // Garante que sempre tem ao menos uma estratégia (nunca trava o ciclo)
        if (!strategies.Any())
            strategies.Add(new TimeoutStrategy(TimeSpan.FromSeconds(90)));

        return strategies.AsReadOnly();
    }
}
