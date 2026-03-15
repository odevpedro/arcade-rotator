namespace ArcadeOrchestrator.Core.Application.Interfaces;

/// <summary>
/// Estratégia de detecção de fim de sessão arcade.
/// Implementações: TimeoutStrategy, ProcessWatchStrategy, LogParserStrategy.
/// O CompositeDetector executa múltiplas estratégias em paralelo.
/// </summary>
public interface IDetectionStrategy
{
    string StrategyName { get; }

    /// <summary>
    /// Observa o processo do emulador e chama <paramref name="onSessionEnd"/>
    /// quando detectar fim de sessão. Respeita o CancellationToken para
    /// permitir cancelamento quando outra estratégia vencer primeiro.
    /// </summary>
    Task WatchAsync(EmulatorProcess process, Action onSessionEnd, CancellationToken ct);
}
