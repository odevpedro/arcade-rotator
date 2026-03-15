using ArcadeOrchestrator.Core.Application.Interfaces;
using ArcadeOrchestrator.Core.Application.Services;
using Microsoft.Extensions.Logging;

namespace ArcadeOrchestrator.Core.Application.UseCases;

public sealed class StartFestaModeUseCase
{
    private readonly StateMachine _stateMachine;
    private readonly IGameCatalog _catalog;
    private readonly ILogger<StartFestaModeUseCase> _logger;
    private CancellationTokenSource? _cts;

    public bool IsRunning => _cts is { IsCancellationRequested: false };

    public StartFestaModeUseCase(
        StateMachine stateMachine,
        IGameCatalog catalog,
        ILogger<StartFestaModeUseCase> logger)
    {
        _stateMachine = stateMachine;
        _catalog = catalog;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        if (IsRunning)
        {
            _logger.LogWarning("Modo Festa já está em execução.");
            return;
        }

        if (!_catalog.GetAllGames().Any())
            throw new InvalidOperationException(
                "Nenhum jogo encontrado no catálogo. Verifique catalog.yaml.");

        _cts = new CancellationTokenSource();
        _logger.LogInformation("Iniciando Modo Festa...");

        await _stateMachine.RunAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _logger.LogInformation("Modo Festa encerrado.");
    }
}
