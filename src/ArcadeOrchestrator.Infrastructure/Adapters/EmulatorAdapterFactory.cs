using ArcadeOrchestrator.Core.Application.Interfaces;
using ArcadeOrchestrator.Infrastructure.Config.Models;
using Microsoft.Extensions.Logging;

namespace ArcadeOrchestrator.Infrastructure.Adapters;

/// <summary>
/// Fábrica que resolve o IEmulatorAdapter concreto com base em config.yaml.
/// Permite adicionar novos emuladores sem alterar o Core.
/// </summary>
public static class EmulatorAdapterFactory
{
    public static IEmulatorAdapter Create(
        EmulatorConfig config,
        ILoggerFactory loggerFactory) =>

        config.Type.ToLowerInvariant() switch
        {
            "retroarch" => new RetroArchAdapter(
                config, loggerFactory.CreateLogger<RetroArchAdapter>()),

            "fbneo" => new FBNeoAdapter(
                config, loggerFactory.CreateLogger<FBNeoAdapter>()),

            _ => throw new NotSupportedException(
                $"Emulador '{config.Type}' não é suportado. " +
                $"Valores válidos: retroarch, fbneo")
        };
}
