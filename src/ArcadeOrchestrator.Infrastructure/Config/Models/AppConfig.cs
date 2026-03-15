using YamlDotNet.Serialization;

namespace ArcadeOrchestrator.Infrastructure.Config.Models;

public sealed class AppConfig
{
    public OrchestratorConfig Orchestrator { get; set; } = new();
    public EmulatorConfig Emulator { get; set; } = new();
    public HotkeyConfig Hotkeys { get; set; } = new();
    public RotationConfig Rotation { get; set; } = new();
    public DetectionConfig Detection { get; set; } = new();
}

public sealed class OrchestratorConfig
{
    public string Mode { get; set; } = "festa";

    [YamlMember(Alias = "cooldown_between_games_seconds")]
    public int CooldownBetweenGamesSeconds { get; set; } = 5;

    public OverlayConfig Overlay { get; set; } = new();
}

public sealed class OverlayConfig
{
    public bool Enabled { get; set; } = true;
    public string Position { get; set; } = "top_right";
    public double Opacity { get; set; } = 0.85;

    [YamlMember(Alias = "font_size")]
    public int FontSize { get; set; } = 14;

    [YamlMember(Alias = "show_next_game")]
    public bool ShowNextGame { get; set; } = true;

    [YamlMember(Alias = "countdown_seconds")]
    public int CountdownSeconds { get; set; } = 10;
}

public sealed class EmulatorConfig
{
    public string Type { get; set; } = "retroarch";

    [YamlMember(Alias = "executable_path")]
    public string ExecutablePath { get; set; } = "";

    [YamlMember(Alias = "core_path")]
    public string CorePath { get; set; } = "";

    [YamlMember(Alias = "extra_args")]
    public string ExtraArgs { get; set; } = "";

    [YamlMember(Alias = "window_mode")]
    public string WindowMode { get; set; } = "borderless";

    [YamlMember(Alias = "log_file_path")]
    public string? LogFilePath { get; set; }
}

public sealed class HotkeyConfig
{
    [YamlMember(Alias = "skip_game")]
    public string SkipGame { get; set; } = "Ctrl+Alt+N";

    [YamlMember(Alias = "restart_game")]
    public string RestartGame { get; set; } = "Ctrl+Alt+R";

    [YamlMember(Alias = "pause_rotation")]
    public string PauseRotation { get; set; } = "Ctrl+Alt+P";

    [YamlMember(Alias = "open_config")]
    public string OpenConfig { get; set; } = "Ctrl+Alt+C";

    [YamlMember(Alias = "force_quit")]
    public string ForceQuit { get; set; } = "Ctrl+Alt+Q";
}

public sealed class RotationConfig
{
    [YamlMember(Alias = "anti_repeat_count")]
    public int AntiRepeatCount { get; set; } = 3;

    [YamlMember(Alias = "franchise_bias")]
    public double FranchiseBias { get; set; } = 0.4;
}

public sealed class DetectionConfig
{
    public DetectionOverrideConfig Default { get; set; } = new();
    public Dictionary<string, DetectionOverrideConfig> Overrides { get; set; } = new();
}

public sealed class DetectionOverrideConfig
{
    public string Strategy { get; set; } = "timeout";

    [YamlMember(Alias = "timeout_seconds")]
    public int TimeoutSeconds { get; set; } = 90;

    [YamlMember(Alias = "fallback_action")]
    public string FallbackAction { get; set; } = "next_game";

    [YamlMember(Alias = "process_watch_enabled")]
    public bool ProcessWatchEnabled { get; set; } = true;

    [YamlMember(Alias = "log_pattern")]
    public string? LogPattern { get; set; }

    [YamlMember(Alias = "fallback_to_timeout")]
    public bool FallbackToTimeout { get; set; } = true;
}
