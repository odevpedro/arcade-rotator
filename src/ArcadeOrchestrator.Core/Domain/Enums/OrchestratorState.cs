namespace ArcadeOrchestrator.Core.Domain.Enums;

public enum OrchestratorState
{
    Idle,
    Launching,
    InjectingCredits,  // ← novo
    WaitingAttract,    // ← novo
    Playing,
    DetectingEnd,
    Cooldown,
    Paused
}
