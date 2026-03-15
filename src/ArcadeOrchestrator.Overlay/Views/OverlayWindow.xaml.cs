using ArcadeOrchestrator.Infrastructure.Win32;
using System.Windows;
using System.Windows.Interop;

namespace ArcadeOrchestrator.Overlay.Views;

public partial class OverlayWindow : Window
{
    public OverlayWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Posiciona no canto superior direito
        var screen = SystemParameters.WorkArea;
        Left = screen.Right - ActualWidth - 16;
        Top = screen.Top + 16;

        // Aplica WS_EX_TRANSPARENT para ser click-through
        var hwnd = new WindowInteropHelper(this).Handle;
        var extStyle = NativeWindowHelper.GetWindowLong(hwnd, NativeWindowHelper.GWL_EXSTYLE);
        NativeWindowHelper.SetWindowLong(hwnd, NativeWindowHelper.GWL_EXSTYLE,
            extStyle | NativeWindowHelper.WS_EX_TRANSPARENT);

        // Re-asserta Topmost a cada 500ms (garante ficar sobre o emulador)
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        timer.Tick += (_, _) =>
        {
            NativeMethods.SetWindowPos(
                hwnd,
                NativeMethods.HWND_TOPMOST,
                0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
        };
        timer.Start();
    }

    public void UpdateCurrentGame(string gameName)
        => Dispatcher.Invoke(() => CurrentGameText.Text = gameName);

    public void UpdateNextGame(string gameName)
        => Dispatcher.Invoke(() => NextGameText.Text = gameName);

    public void UpdateSessionTime(TimeSpan elapsed)
        => Dispatcher.Invoke(() =>
            SessionTimeText.Text = elapsed.ToString(@"mm\:ss"));

    public void UpdateRotationCount(int count)
        => Dispatcher.Invoke(() =>
            RotationCountText.Text = $"{count} rotações");
}

/// <summary>P/Invoke para aplicar WS_EX_TRANSPARENT (click-through).</summary>
internal static class NativeWindowHelper
{
    internal const int GWL_EXSTYLE = -20;
    internal const int WS_EX_TRANSPARENT = 0x00000020;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
