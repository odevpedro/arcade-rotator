using ArcadeOrchestrator.Core.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ArcadeOrchestrator.Core.Application.Services;

/// <summary>
/// Executa múltiplas IDetectionStrategy em paralelo (Task.WhenAny).
/// A primeira que disparar cancela as demais.
/// </summary>
public sealed class SessionEndDetector
{
    private readonly IReadOnlyList<IDetectionStrategy> _strategies;
    private readonly ILogger<SessionEndDetector> _logger;
    private CancellationTokenSource? _manualCts;

    public string? LastTriggeredStrategy { get; private set; }

    public SessionEndDetector(
        IReadOnlyList<IDetectionStrategy> strategies,
        ILogger<SessionEndDetector> logger)
    {
        if (!strategies.Any())
            throw new ArgumentException("Ao menos uma estratégia de detecção é obrigatória.", nameof(strategies));

        _strategies = strategies;
        _logger = logger;
    }

    /// <summary>
    /// Aguarda até que qualquer estratégia detecte fim de sessão.
    /// Retorna quando a primeira disparar ou o token externo for cancelado.
    /// </summary>
    public async Task WaitForSessionEndAsync(EmulatorProcess process, CancellationToken externalCt)
    {
        LastTriggeredStrategy = null;
        _manualCts = new CancellationTokenSource();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            externalCt, _manualCts.Token);

        var triggered = false;
        var winnerLock = new object();

        void OnSessionEnd(string strategyName)
        {
            lock (winnerLock)
            {
                if (triggered) return;
                triggered = true;
                LastTriggeredStrategy = strategyName;
            }

            _logger.LogInformation(
                "Fim de sessão detectado por: {Strategy}", strategyName);

            try { linkedCts.Cancel(); }
            catch (ObjectDisposedException) { }
        }

        var tasks = _strategies
            .Select(s => s.WatchAsync(
                process,
                () => OnSessionEnd(s.StrategyName),
                linkedCts.Token))
            .ToList();

        try
        {
            await Task.WhenAny(tasks);
        }
        catch (OperationCanceledException) { }

        // Aguarda todas finalizarem antes de retornar
        await Task.WhenAll(tasks.Select(t => t.ContinueWith(
            _ => { }, TaskContinuationOptions.None)));

        _manualCts.Dispose();
        _manualCts = null;
    }

    /// <summary>Força o fim de sessão manualmente (hotkey skip).</summary>
    public void TriggerManualEnd()
    {
        LastTriggeredStrategy = "manual_skip";
        try { _manualCts?.Cancel(); }
        catch (ObjectDisposedException) { }
    }
}
