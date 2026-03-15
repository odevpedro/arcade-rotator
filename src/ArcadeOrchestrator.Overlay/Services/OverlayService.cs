using ArcadeOrchestrator.Core.Domain.Entities;
using ArcadeOrchestrator.Overlay.Views;
using Microsoft.Extensions.Logging;

namespace ArcadeOrchestrator.Overlay.Services;

/// <summary>
/// Serviço que atualiza a OverlayWindow com o estado atual da orquestração.
/// </summary>
public sealed class OverlayService
{
    private readonly OverlayWindow _window;
    private readonly ILogger<OverlayService> _logger;

    private int _rotationCount;
    private DateTime _sessionStart;

    public OverlayService(OverlayWindow window, ILogger<OverlayService> logger)
    {
        _window = window;
        _logger = logger;
    }

    public void NotifyGameLaunched(Game current, Game? next)
    {
        _rotationCount++;
        _sessionStart = DateTime.UtcNow;

        _window.UpdateCurrentGame(current.DisplayName);
        _window.UpdateNextGame(next?.DisplayName ?? "—");
        _window.UpdateRotationCount(_rotationCount);

        _logger.LogDebug("Overlay atualizado: {Game}", current.DisplayName);
    }

    public void NotifySessionTime(TimeSpan elapsed)
        => _window.UpdateSessionTime(elapsed);
}
