using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArcadeOrchestrator.Overlay.ViewModels;

public sealed partial class OverlayViewModel : ObservableObject
{
    [ObservableProperty] private string _currentGame = "—";
    [ObservableProperty] private string _nextGame    = "—";
    [ObservableProperty] private string _sessionTime = "00:00";
    [ObservableProperty] private int    _rotationCount;
    [ObservableProperty] private bool   _isPaused;
    [ObservableProperty] private string _statusMessage = "Aguardando...";

    public event Action? SkipRequested;
    public event Action? PauseRequested;
    public event Action? RestartRequested;

    [RelayCommand]
    private void Skip()    => SkipRequested?.Invoke();

    [RelayCommand]
    private void Pause()   => PauseRequested?.Invoke();

    [RelayCommand]
    private void Restart() => RestartRequested?.Invoke();
}
