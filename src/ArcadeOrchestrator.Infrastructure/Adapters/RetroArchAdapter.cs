using ArcadeOrchestrator.Core.Application.Interfaces;
using ArcadeOrchestrator.Core.Domain.Entities;
using ArcadeOrchestrator.Infrastructure.Config.Models;
using ArcadeOrchestrator.Infrastructure.Win32;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ArcadeOrchestrator.Infrastructure.Adapters;

/// <summary>
/// Adapter concreto para o RetroArch.
/// Controla o emulador via CLI: retroarch.exe -L core.dll rom.zip
/// </summary>
public sealed class RetroArchAdapter : IEmulatorAdapter
{
    private readonly EmulatorConfig _config;
    private readonly ILogger<RetroArchAdapter> _logger;
    private const int GracefulShutdownTimeoutMs = 3_000;

    public RetroArchAdapter(EmulatorConfig config, ILogger<RetroArchAdapter> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<EmulatorProcess> LaunchAsync(Game game, CancellationToken ct)
    {
        var args = BuildArguments(game);
        _logger.LogInformation(
            "Iniciando RetroArch: {Exe} {Args}", _config.ExecutablePath, args);

        var psi = new ProcessStartInfo
        {
            FileName = _config.ExecutablePath,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = false
        };

        var process = Process.Start(psi)
            ?? throw new InvalidOperationException(
                $"Falha ao iniciar RetroArch para ROM: {game.Rom}");

        // Aguarda o processo estabilizar
        await Task.Delay(1_500, ct);

        _logger.LogInformation(
            "RetroArch iniciado. PID={Pid} | Jogo={Game}", process.Id, game.DisplayName);

        return new EmulatorProcess(process.Id, process);
    }

    public async Task StopAsync(EmulatorProcess emulatorProcess, CancellationToken ct)
    {
        var process = emulatorProcess.Handle;

        if (process.HasExited)
        {
            _logger.LogDebug("PID={Pid} já encerrado.", process.Id);
            return;
        }

        _logger.LogInformation("Encerrando RetroArch PID={Pid}...", process.Id);

        // Tentativa graciosa: envia WM_CLOSE
        process.CloseMainWindow();

        try
        {
            await process.WaitForExitAsync(ct)
                         .WaitAsync(TimeSpan.FromMilliseconds(GracefulShutdownTimeoutMs), ct);

            _logger.LogInformation("RetroArch encerrado graciosamente.");
        }
        catch (TimeoutException)
        {
            _logger.LogWarning(
                "Timeout graceful ({Ms}ms). Forçando KillProcessTree.", GracefulShutdownTimeoutMs);
            ProcessHelper.KillProcessTree(process.Id);
        }
    }

    public bool IsRunning(EmulatorProcess emulatorProcess)
    {
        try { return !emulatorProcess.Handle.HasExited; }
        catch { return false; }
    }

    public string? GetLogFilePath() => _config.LogFilePath;

    private string BuildArguments(Game game)
    {
        var parts = new List<string>
        {
            $"-L \"{_config.CorePath}\"",
            $"\"{game.RomPath}\""
        };

        if (string.Equals(_config.WindowMode, "borderless", StringComparison.OrdinalIgnoreCase))
            parts.Add("--windowed");
        else
            parts.Add("--fullscreen");

        if (!string.IsNullOrWhiteSpace(_config.ExtraArgs))
            parts.Add(_config.ExtraArgs);

        return string.Join(" ", parts);
    }
}
