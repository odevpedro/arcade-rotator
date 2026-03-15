using ArcadeOrchestrator.Core.Application.Interfaces;
using ArcadeOrchestrator.Core.Domain.Entities;
using ArcadeOrchestrator.Core.Domain.Enums;
using ArcadeOrchestrator.Core.Domain.Events;
using Microsoft.Extensions.Logging;

namespace ArcadeOrchestrator.Core.Application.Services;

/// <summary>
/// Orquestra o ciclo completo do Modo Festa.
/// Estados: IDLE → LAUNCHING → PLAYING → DETECTING_END → COOLDOWN → IDLE
/// </summary>
public sealed class StateMachine
{
    private readonly IEmulatorAdapter _adapter;
    private readonly IRotationEngine _rotation;
    private readonly SessionEndDetector _detector;
    private readonly IOrchestrationLogger _log;
    private readonly ILogger<StateMachine> _logger;
    private readonly int _cooldownMs;

    private OrchestratorState _state = OrchestratorState.Idle;
    private Game? _currentGame;

    public event Action<OrchestratorState>? StateChanged;
    public event Action<Game>? GameLaunched;
    public event Action<SessionEndedEvent>? SessionEnded;

    public OrchestratorState CurrentState => _state;
    public Game? CurrentGame => _currentGame;

    public StateMachine(
        IEmulatorAdapter adapter,
        IRotationEngine rotation,
        SessionEndDetector detector,
        IOrchestrationLogger log,
        ILogger<StateMachine> logger,
        int cooldownMs = 5000)
    {
        _adapter = adapter;
        _rotation = rotation;
        _detector = detector;
        _log = log;
        _logger = logger;
        _cooldownMs = cooldownMs;
    }

    /// <summary>
    /// Inicia o loop do Modo Festa. Roda até o CancellationToken ser cancelado.
    /// </summary>
    public async Task RunAsync(CancellationToken ct)
    {
        _logger.LogInformation("Modo Festa iniciado.");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var next = _rotation.PickNext(_currentGame?.Id);
                await RunCycleAsync(next, ct);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Modo Festa encerrado pelo usuário.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no ciclo de rotação. Aguardando antes de tentar novamente.");
                _log.LogError("StateMachine.RunAsync", ex);
                await Task.Delay(3000, ct);
            }
        }

        Transition(OrchestratorState.Idle);
    }

    private async Task RunCycleAsync(Game game, CancellationToken ct)
    {
        // LAUNCHING
        Transition(OrchestratorState.Launching);
        _currentGame = game;
        _logger.LogInformation("Lançando: {Game}", game.DisplayName);

        var process = await _adapter.LaunchAsync(game, ct);
        _log.LogSessionStarted(game);
        GameLaunched?.Invoke(game);

        var sessionStart = DateTime.UtcNow;

        // PLAYING
        Transition(OrchestratorState.Playing);
        await _detector.WaitForSessionEndAsync(process, ct);

        var duration = DateTime.UtcNow - sessionStart;
        var strategyName = _detector.LastTriggeredStrategy ?? "unknown";

        // DETECTING_END
        Transition(OrchestratorState.DetectingEnd);
        await _adapter.StopAsync(process, ct);

        _log.LogSessionEnded(game, strategyName, duration);

        var ended = new SessionEndedEvent(game, strategyName, DateTime.UtcNow);
        SessionEnded?.Invoke(ended);

        // COOLDOWN
        Transition(OrchestratorState.Cooldown);
        await Task.Delay(_cooldownMs, ct);
    }

    /// <summary>Força a troca imediata para o próximo jogo (hotkey skip).</summary>
    public void RequestSkip() => _detector.TriggerManualEnd();

    private void Transition(OrchestratorState next)
    {
        _state = next;
        _logger.LogDebug("Estado: {State}", next);
        StateChanged?.Invoke(next);
    }
}
