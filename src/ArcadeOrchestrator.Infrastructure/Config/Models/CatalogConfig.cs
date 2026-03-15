using YamlDotNet.Serialization;

namespace ArcadeOrchestrator.Infrastructure.Config.Models;

public sealed class CatalogConfig
{
    public List<FranchiseConfig> Franchises { get; set; } = new();
}

public sealed class FranchiseConfig
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Weight { get; set; } = 10;
    public List<GameConfig> Games { get; set; } = new();
}

public sealed class GameConfig
{
    public string Id { get; set; } = "";
    public string Rom { get; set; } = "";

    [YamlMember(Alias = "rom_path")]
    public string RomPath { get; set; } = "";

    [YamlMember(Alias = "display_name")]
    public string DisplayName { get; set; } = "";

    public int Weight { get; set; } = 10;
    public List<string> Tags { get; set; } = new();
}
