namespace ArcadeOrchestrator.Core.Domain.Enums;

public enum OrchestratorState
{
    Idle,
    Launching,
    Playing,
    DetectingEnd,
    Cooldown,
    Paused
}
