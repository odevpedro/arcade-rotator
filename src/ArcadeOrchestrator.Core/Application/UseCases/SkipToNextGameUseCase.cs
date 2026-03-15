using ArcadeOrchestrator.Core.Application.Services;
using Microsoft.Extensions.Logging;

namespace ArcadeOrchestrator.Core.Application.UseCases;

public sealed class SkipToNextGameUseCase
{
    private readonly StateMachine _stateMachine;
    private readonly ILogger<SkipToNextGameUseCase> _logger;

    public SkipToNextGameUseCase(StateMachine stateMachine, ILogger<SkipToNextGameUseCase> logger)
    {
        _stateMachine = stateMachine;
        _logger = logger;
    }

    public void Execute()
    {
        _logger.LogInformation("Skip solicitado pelo usuário.");
        _stateMachine.RequestSkip();
    }
}
