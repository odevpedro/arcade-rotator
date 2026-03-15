using ArcadeOrchestrator.Core.Application.Interfaces;

namespace ArcadeOrchestrator.Core.Detection;

/// <summary>
/// Detecta fim de sessão monitorando o ciclo de vida do processo do emulador.
/// É a estratégia mais rápida — event-driven, sem polling.
/// </summary>
public sealed class ProcessWatchStrategy : IDetectionStrategy
{
    public string StrategyName => "process_watch";

    public async Task WatchAsync(EmulatorProcess process, Action onSessionEnd, CancellationToken ct)
    {
        try
        {
            await process.Handle.WaitForExitAsync(ct);

            if (!ct.IsCancellationRequested)
                onSessionEnd();
        }
        catch (OperationCanceledException)
        {
            // Normal — outra estratégia venceu ou sessão foi cancelada externamente
        }
        catch (InvalidOperationException)
        {
            // Processo já havia encerrado antes do Watch começar
            if (!ct.IsCancellationRequested)
                onSessionEnd();
        }
    }
}
