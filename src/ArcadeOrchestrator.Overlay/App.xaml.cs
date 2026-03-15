using ArcadeOrchestrator.Infrastructure.Config;
using ArcadeOrchestrator.Infrastructure.Config.Models;
using ArcadeOrchestrator.Overlay.Services;
using ArcadeOrchestrator.Overlay.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;
using System.Windows;

namespace ArcadeOrchestrator.Overlay;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configura Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine("logs", "orchestrator-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .CreateLogger();

        var configPath  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "config.yaml");
        var catalogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "catalog.yaml");

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton(new ConfigManager(configPath, catalogPath));
                services.AddSingleton<OverlayService>();
                services.AddSingleton<OverlayWindow>();
            })
            .Build();

        await _host.StartAsync();

        var overlay = _host.Services.GetRequiredService<OverlayWindow>();
        overlay.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
