using ArcadeOrchestrator.Core.Application.Interfaces;
using ArcadeOrchestrator.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ArcadeOrchestrator.Infrastructure.Logging;

/// <summary>
/// Implementação de IOrchestrationLogger usando ILogger (Serilog por baixo).
/// </summary>
public sealed class SerilogOrchestrationLogger : IOrchestrationLogger
{
    private readonly ILogger<SerilogOrchestrationLogger> _logger;

    public SerilogOrchestrationLogger(ILogger<SerilogOrchestrationLogger> logger)
        => _logger = logger;

    public void LogSessionStarted(Game game) =>
        _logger.LogInformation(
            "[SESSION_START] {Game} | ROM={Rom}", game.DisplayName, game.Rom);

    public void LogSessionEnded(Game game, string strategy, TimeSpan duration) =>
        _logger.LogInformation(
            "[SESSION_END] {Game} | Strategy={Strategy} | Duration={Duration:mm\\:ss}",
            game.DisplayName, strategy, duration);

    public void LogRotation(Game from, Game to) =>
        _logger.LogInformation(
            "[ROTATION] {From} → {To}", from.DisplayName, to.DisplayName);

    public void LogError(string context, Exception ex) =>
        _logger.LogError(ex, "[ERROR] Context={Context}", context);
}
