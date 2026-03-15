using ArcadeOrchestrator.Infrastructure.Config.Models;

namespace ArcadeOrchestrator.Infrastructure.Config.Validators;

/// <summary>
/// Validações de negócio aplicadas sobre a AppConfig após o parse do YAML.
/// </summary>
public static class AppConfigValidator
{
    public static IReadOnlyList<string> Validate(AppConfig config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.Emulator.ExecutablePath))
            errors.Add("emulator.executable_path é obrigatório.");

        if (config.Detection.Default.TimeoutSeconds <= 0)
            errors.Add("detection.default.timeout_seconds deve ser maior que zero.");

        if (config.Rotation.AntiRepeatCount < 0)
            errors.Add("rotation.anti_repeat_count não pode ser negativo.");

        if (config.Rotation.FranchiseBias is < 0 or > 1)
            errors.Add("rotation.franchise_bias deve estar entre 0.0 e 1.0.");

        if (config.Orchestrator.CooldownBetweenGamesSeconds < 0)
            errors.Add("orchestrator.cooldown_between_games_seconds não pode ser negativo.");

        return errors.AsReadOnly();
    }

    public static void ValidateOrThrow(AppConfig config)
    {
        var errors = Validate(config);
        if (errors.Any())
            throw new InvalidOperationException(
                $"Erros de configuração:\n{string.Join("\n", errors.Select(e => $"  • {e}"))}");
    }
}
