using ArcadeOrchestrator.Core.Domain.Entities;
using System.Text.Json;

namespace ArcadeOrchestrator.Infrastructure.Logging;

/// <summary>
/// Persiste o histórico de rotações em JSONL (uma linha JSON por sessão).
/// O arquivo cresce incrementalmente — nunca é sobrescrito.
/// </summary>
public sealed class RotationHistoryWriter
{
    private readonly string _filePath;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public RotationHistoryWriter(string filePath)
    {
        _filePath = filePath;
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
    }

    public async Task RecordAsync(Game game, string strategy, TimeSpan duration)
    {
        var entry = new
        {
            timestamp = DateTime.UtcNow,
            game = game.DisplayName,
            rom = game.Rom,
            strategy,
            duration_seconds = (int)duration.TotalSeconds
        };

        var json = JsonSerializer.Serialize(entry);

        await _lock.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(_filePath, json + Environment.NewLine);
        }
        finally
        {
            _lock.Release();
        }
    }
}
