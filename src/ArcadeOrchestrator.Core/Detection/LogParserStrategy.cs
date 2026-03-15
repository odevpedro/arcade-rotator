using ArcadeOrchestrator.Core.Application.Interfaces;
using System.Text.RegularExpressions;

namespace ArcadeOrchestrator.Core.Detection;

/// <summary>
/// Detecta fim de sessão monitorando o arquivo de log do emulador.
/// Usa FileSystemWatcher para ser reativo, sem polling pesado.
/// O padrão regex é configurável por jogo no YAML.
/// </summary>
public sealed class LogParserStrategy : IDetectionStrategy
{
    public string StrategyName => "log_parser";

    private readonly string _logFilePath;
    private readonly Regex _pattern;

    public LogParserStrategy(string logFilePath, string regexPattern)
    {
        _logFilePath = logFilePath;
        _pattern = new Regex(regexPattern,
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public async Task WatchAsync(EmulatorProcess process, Action onSessionEnd, CancellationToken ct)
    {
        if (!File.Exists(_logFilePath))
            return; // Sem log, sem problema — outra estratégia assume

        // Posiciona no final atual para não reprocessar entradas antigas
        long lastPosition = new FileInfo(_logFilePath).Length;

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        using var watcher = new FileSystemWatcher(
            Path.GetDirectoryName(_logFilePath)!,
            Path.GetFileName(_logFilePath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        watcher.Changed += (_, _) =>
        {
            if (tcs.Task.IsCompleted) return;

            try
            {
                using var fs = new FileStream(
                    _logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                fs.Seek(lastPosition, SeekOrigin.Begin);
                using var reader = new StreamReader(fs);

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (_pattern.IsMatch(line))
                    {
                        tcs.TrySetResult(true);
                        return;
                    }
                }
                lastPosition = fs.Position;
            }
            catch (IOException) { /* Log sendo escrito — tenta no próximo evento */ }
        };

        using var reg = ct.Register(() => tcs.TrySetCanceled());

        try
        {
            var matched = await tcs.Task;
            if (matched && !ct.IsCancellationRequested)
                onSessionEnd();
        }
        catch (OperationCanceledException) { }
    }
}
