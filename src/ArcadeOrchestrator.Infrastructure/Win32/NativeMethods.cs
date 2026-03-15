using System.Runtime.InteropServices;

namespace ArcadeOrchestrator.Infrastructure.Win32;

/// <summary>
/// Declarações P/Invoke para APIs Win32 usadas pelo orquestrador.
/// </summary>
internal static class NativeMethods
{
    // ── Hotkeys ──────────────────────────────────────────────────────────────
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // ── Window management ────────────────────────────────────────────────────
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter,
        int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool SetLayeredWindowAttributes(
        IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    // ── Process ──────────────────────────────────────────────────────────────
    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

    // ── Constants ────────────────────────────────────────────────────────────
    internal static readonly IntPtr HWND_TOPMOST = new(-1);
    internal const uint SWP_NOSIZE     = 0x0001;
    internal const uint SWP_NOMOVE     = 0x0002;
    internal const uint SWP_NOACTIVATE = 0x0010;
    internal const uint LWA_ALPHA      = 0x00000002;

    // Modificadores de hotkey
    internal const uint MOD_ALT   = 0x0001;
    internal const uint MOD_CTRL  = 0x0002;
    internal const uint MOD_SHIFT = 0x0004;

    // WM_HOTKEY
    internal const int WM_HOTKEY = 0x0312;
}
