using ArcadeOrchestrator.Core.Application.Interfaces;

namespace ArcadeOrchestrator.Core.Detection;

/// <summary>
/// Detecta fim de sessão por inatividade.
/// Dispara após o timeout configurado sem atividade de input.
/// ReportActivity() deve ser chamado pelo RawInput hook ao detectar qualquer input.
/// </summary>
public sealed class TimeoutStrategy : IDetectionStrategy
{
    public string StrategyName => "timeout";

    private readonly TimeSpan _timeout;
    private volatile DateTime _lastActivity;

    public TimeoutStrategy(TimeSpan timeout)
    {
        _timeout = timeout;
        _lastActivity = DateTime.UtcNow;
    }

    /// <summary>
    /// Registra atividade de input. Reinicia o contador de inatividade.
    /// Thread-safe — pode ser chamado de qualquer thread.
    /// </summary>
    public void ReportActivity() => _lastActivity = DateTime.UtcNow;

    public async Task WatchAsync(EmulatorProcess process, Action onSessionEnd, CancellationToken ct)
    {
        _lastActivity = DateTime.UtcNow;

        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(5_000, ct); // Verifica a cada 5 segundos

            var elapsed = DateTime.UtcNow - _lastActivity;
            if (elapsed >= _timeout)
            {
                onSessionEnd();
                return;
            }
        }
    }
}
