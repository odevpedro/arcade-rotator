using ArcadeOrchestrator.Core.Domain.Entities;

namespace ArcadeOrchestrator.Core.Application.Interfaces;

/// <summary>
/// Abstrai o controle de qualquer emulador externo.
/// Implementações concretas ficam em Infrastructure/Adapters.
/// </summary>
public interface IEmulatorAdapter
{
    /// <summary>
    /// Lança o emulador para o ROM especificado e retorna o processo iniciado.
    /// </summary>
    Task<EmulatorProcess> LaunchAsync(Game game, CancellationToken ct);

    /// <summary>
    /// Encerra o emulador. Tenta gracioso (WM_CLOSE) antes de forçar kill.
    /// </summary>
    Task StopAsync(EmulatorProcess process, CancellationToken ct);

    /// <summary>
    /// Retorna true enquanto o processo do emulador estiver ativo.
    /// </summary>
    bool IsRunning(EmulatorProcess process);

    /// <summary>
    /// Caminho do arquivo de log gerado pelo emulador.
    /// Null se o emulador não suportar log em arquivo.
    /// </summary>
    string? GetLogFilePath();
}

/// <summary>Representa um processo de emulador em execução.</summary>
public sealed record EmulatorProcess(
    int ProcessId,
    System.Diagnostics.Process Handle
);
