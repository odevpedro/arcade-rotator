using Microsoft.Extensions.Logging;

namespace ArcadeOrchestrator.Infrastructure.Win32;

/// <summary>
/// Registra hotkeys globais via Win32 RegisterHotKey.
/// Deve ser instanciado na thread da UI (WPF) pois precisa de um HWND.
/// </summary>
public sealed class HotkeyManager : IDisposable
{
    private readonly IntPtr _hwnd;
    private readonly ILogger<HotkeyManager> _logger;
    private readonly Dictionary<int, Action> _handlers = new();
    private int _nextId = 1;
    private bool _disposed;

    public HotkeyManager(IntPtr hwnd, ILogger<HotkeyManager> logger)
    {
        _hwnd = hwnd;
        _logger = logger;
    }

    /// <summary>
    /// Registra uma hotkey global. Retorna o ID do registro.
    /// </summary>
    public int Register(uint modifiers, uint vk, Action callback)
    {
        var id = _nextId++;
        if (!NativeMethods.RegisterHotKey(_hwnd, id, modifiers, vk))
        {
            var err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            _logger.LogWarning(
                "Falha ao registrar hotkey (VK={Vk}, MOD={Mod}). Win32Error={Err}", vk, modifiers, err);
            return -1;
        }

        _handlers[id] = callback;
        _logger.LogDebug("Hotkey registrada. ID={Id} VK={Vk}", id, vk);
        return id;
    }

    /// <summary>
    /// Deve ser chamado ao receber WM_HOTKEY na janela principal do WPF.
    /// </summary>
    public void HandleWmHotkey(int hotkeyId)
    {
        if (_handlers.TryGetValue(hotkeyId, out var action))
            action();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var id in _handlers.Keys)
            NativeMethods.UnregisterHotKey(_hwnd, id);

        _handlers.Clear();
    }
}
