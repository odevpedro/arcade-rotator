using ArcadeOrchestrator.Infrastructure.Config;
using FluentAssertions;
using Xunit;

namespace ArcadeOrchestrator.Infrastructure.Tests;

public sealed class ConfigManagerTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public ConfigManagerTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private string WriteConfig(string yaml)
    {
        var path = Path.Combine(_tempDir, "config.yaml");
        File.WriteAllText(path, yaml);
        return path;
    }

    private string WriteCatalog(string yaml)
    {
        var path = Path.Combine(_tempDir, "catalog.yaml");
        File.WriteAllText(path, yaml);
        return path;
    }

    [Fact]
    public void LoadAppConfig_ShouldParseValidYaml()
    {
        var configPath = WriteConfig("""
            orchestrator:
              mode: festa
              cooldown_between_games_seconds: 5
            emulator:
              type: retroarch
              executable_path: C:\RetroArch\retroarch.exe
              core_path: C:\RetroArch\cores\fbneo_libretro.dll
            detection:
              default:
                strategy: timeout
                timeout_seconds: 90
            """);
        var catalogPath = WriteCatalog("""
            franchises:
              - id: kof
                name: King of Fighters
                weight: 10
                games:
                  - id: kof99
                    rom: kof99.zip
                    rom_path: C:\roms\kof99.zip
                    display_name: "KOF '99"
                    weight: 10
            """);

        var manager = new ConfigManager(configPath, catalogPath);
        var config = manager.LoadAppConfig();

        config.Orchestrator.Mode.Should().Be("festa");
        config.Emulator.Type.Should().Be("retroarch");
        config.Detection.Default.TimeoutSeconds.Should().Be(90);
    }

    [Fact]
    public void LoadAppConfig_ShouldThrow_WhenExecutablePathIsEmpty()
    {
        var configPath = WriteConfig("""
            emulator:
              executable_path: ""
            detection:
              default:
                timeout_seconds: 90
            """);
        var catalogPath = WriteCatalog("""
            franchises:
              - id: kof
                name: KOF
                weight: 10
                games:
                  - id: kof99
                    rom: kof99.zip
                    rom_path: C:\roms\kof99.zip
                    display_name: KOF99
                    weight: 10
            """);

        var manager = new ConfigManager(configPath, catalogPath);
        var act = () => manager.LoadAppConfig();

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*executable_path*");
    }

    [Fact]
    public void LoadAppConfig_ShouldThrow_WhenFileNotFound()
    {
        var manager = new ConfigManager("/nao/existe.yaml", "/nao/existe.yaml");
        var act = () => manager.LoadAppConfig();

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void LoadCatalogConfig_ShouldParseFranchisesAndGames()
    {
        var configPath = WriteConfig("""
            emulator:
              executable_path: C:\RetroArch\retroarch.exe
            detection:
              default:
                timeout_seconds: 90
            """);
        var catalogPath = WriteCatalog("""
            franchises:
              - id: kof
                name: King of Fighters
                weight: 40
                games:
                  - id: kof99
                    rom: kof99.zip
                    rom_path: C:\roms\kof99.zip
                    display_name: "KOF '99"
                    weight: 25
                    tags: [neogeo, fighter]
                  - id: kof2002
                    rom: kof2002.zip
                    rom_path: C:\roms\kof2002.zip
                    display_name: "KOF 2002"
                    weight: 50
            """);

        var manager = new ConfigManager(configPath, catalogPath);
        var catalog = manager.LoadCatalogConfig();

        catalog.Franchises.Should().HaveCount(1);
        catalog.Franchises[0].Id.Should().Be("kof");
        catalog.Franchises[0].Games.Should().HaveCount(2);
        catalog.Franchises[0].Games[0].Tags.Should().Contain("fighter");
    }
}
