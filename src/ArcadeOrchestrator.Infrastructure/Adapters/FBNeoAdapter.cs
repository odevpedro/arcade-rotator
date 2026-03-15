using ArcadeOrchestrator.Core.Application.Interfaces;
using ArcadeOrchestrator.Core.Domain.Entities;
using ArcadeOrchestrator.Infrastructure.Config.Models;
using ArcadeOrchestrator.Infrastructure.Win32;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ArcadeOrchestrator.Infrastructure.Adapters;

/// <summary>
/// Adapter concreto para o FinalBurn Neo (FBNeo standalone).
/// CLI: fbneo64.exe rom_name
/// </summary>
public sealed class FBNeoAdapter : IEmulatorAdapter
{
    private readonly EmulatorConfig _config;
    private readonly ILogger<FBNeoAdapter> _logger;
    private const int GracefulShutdownTimeoutMs = 3_000;

    public FBNeoAdapter(EmulatorConfig config, ILogger<FBNeoAdapter> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<EmulatorProcess> LaunchAsync(Game game, CancellationToken ct)
    {
        // FBNeo recebe o nome da ROM sem extensão
        var romName = Path.GetFileNameWithoutExtension(game.Rom);
        var args = $"\"{romName}\"";

        if (!string.IsNullOrWhiteSpace(_config.ExtraArgs))
            args += $" {_config.ExtraArgs}";

        _logger.LogInformation(
            "Iniciando FBNeo: {Exe} {Args}", _config.ExecutablePath, args);

        var psi = new ProcessStartInfo
        {
            FileName = _config.ExecutablePath,
            Arguments = args,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(_config.ExecutablePath)
        };

        var process = Process.Start(psi)
            ?? throw new InvalidOperationException(
                $"Falha ao iniciar FBNeo para ROM: {game.Rom}");

        await Task.Delay(2_000, ct);

        _logger.LogInformation(
            "FBNeo iniciado. PID={Pid} | Jogo={Game}", process.Id, game.DisplayName);

        return new EmulatorProcess(process.Id, process);
    }

    public async Task StopAsync(EmulatorProcess emulatorProcess, CancellationToken ct)
    {
        var process = emulatorProcess.Handle;
        if (process.HasExited) return;

        process.CloseMainWindow();

        try
        {
            await process.WaitForExitAsync(ct)
                         .WaitAsync(TimeSpan.FromMilliseconds(GracefulShutdownTimeoutMs), ct);
        }
        catch (TimeoutException)
        {
            ProcessHelper.KillProcessTree(process.Id);
        }
    }

    public bool IsRunning(EmulatorProcess emulatorProcess)
    {
        try { return !emulatorProcess.Handle.HasExited; }
        catch { return false; }
    }

    // FBNeo não gera log em arquivo por padrão
    public string? GetLogFilePath() => null;
}
