using ArcadeOrchestrator.Infrastructure.Config.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ArcadeOrchestrator.Infrastructure.Config;

/// <summary>
/// Lê e valida os arquivos YAML de configuração.
/// Falha rápido (InvalidOperationException) se a config for inválida.
/// </summary>
public sealed class ConfigManager
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private readonly string _configPath;
    private readonly string _catalogPath;

    public ConfigManager(string configPath, string catalogPath)
    {
        _configPath = configPath;
        _catalogPath = catalogPath;
    }

    public AppConfig LoadAppConfig()
    {
        var yaml = ReadFile(_configPath);
        var config = Deserializer.Deserialize<AppConfig>(yaml);
        Validate(config);
        return config;
    }

    public CatalogConfig LoadCatalogConfig()
    {
        var yaml = ReadFile(_catalogPath);
        var catalog = Deserializer.Deserialize<CatalogConfig>(yaml);

        if (!catalog.Franchises.Any())
            throw new InvalidOperationException(
                $"Nenhuma franquia encontrada em '{_catalogPath}'.");

        var totalGames = catalog.Franchises.Sum(f => f.Games.Count);
        if (totalGames == 0)
            throw new InvalidOperationException(
                "O catálogo não possui nenhum jogo configurado.");

        return catalog;
    }

    private static string ReadFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Arquivo de configuração não encontrado: '{path}'");

        return File.ReadAllText(path);
    }

    private static void Validate(AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Emulator.ExecutablePath))
            throw new InvalidOperationException(
                "emulator.executable_path não pode estar vazio em config.yaml.");

        if (config.Detection.Default.TimeoutSeconds <= 0)
            throw new InvalidOperationException(
                "detection.default.timeout_seconds deve ser maior que zero.");
    }
}
