using System.Diagnostics;

namespace ArcadeOrchestrator.Infrastructure.Win32;

/// <summary>
/// Utilitários para gerenciamento de processos no Windows.
/// </summary>
public static class ProcessHelper
{
    /// <summary>
    /// Termina o processo e todos os seus filhos recursivamente.
    /// Necessário quando o emulador spawna processos auxiliares.
    /// </summary>
    public static void KillProcessTree(int pid)
    {
        try
        {
            // No .NET 5+ podemos usar Kill(entireProcessTree: true)
            var process = Process.GetProcessById(pid);
            process.Kill(entireProcessTree: true);
        }
        catch (ArgumentException)
        {
            // Processo já encerrou — OK
        }
        catch (Exception ex)
        {
            // Log e segue — melhor um processo zumbi do que travar o ciclo
            Console.Error.WriteLine(
                $"[ProcessHelper] Falha ao encerrar árvore do PID {pid}: {ex.Message}");
        }
    }

    /// <summary>
    /// Retorna true se o processo com o PID dado ainda está ativo.
    /// </summary>
    public static bool IsAlive(int pid)
    {
        try
        {
            var p = Process.GetProcessById(pid);
            return !p.HasExited;
        }
        catch { return false; }
    }
}
