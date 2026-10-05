// UltraCraft launcher: starts the hidden Minecraft Java companion (Prism instance UltraCraft-Lab),
// then ULTRAKILL. The UltraCraft plugin links the two; Java saves and quits when ULTRAKILL closes.
// Build: tools\launcher\build.ps1 (uses the .NET Framework compiler built into Windows).
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

internal static class UltraCraftLauncher
{
    const string Game = @"C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL\ULTRAKILL.exe";
    const string Plugin = @"C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL\BepInEx\plugins\UltraCraft\UltraCraft.dll";
    const string Prism = @"C:\Users\homer\Documents\Codex\2026-10-05\files-pasted-by-the-user-help-2\work\runtime\Prism\prismlauncher.exe";
    const string Instance = "UltraCraft-Lab";

    [STAThread]
    static void Main()
    {
        string prismData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PrismLauncher");
        string missing = new[] { Game, Plugin, Prism, Path.Combine(prismData, "instances", Instance) }
            .FirstOrDefault(p => !File.Exists(p) && !Directory.Exists(p));
        if (missing != null)
        {
            Fail("UltraCraft can't start because this is missing:\n\n" + missing);
            return;
        }
        if (Process.GetProcessesByName("ULTRAKILL").Length > 0)
        {
            MessageBox.Show("ULTRAKILL is already running. Close it first, then start UltraCraft again.", "UltraCraft",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            // The Java companion only for this instance: other Minecraft games are left alone.
            if (!CompanionRunning())
            {
                Process.Start(new ProcessStartInfo(Prism, "--dir \"" + prismData + "\" --launch " + Instance)
                {
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = Path.GetDirectoryName(Prism),
                });
            }
            Process.Start(new ProcessStartInfo(Game)
            {
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(Game),
            });
        }
        catch (Exception e)
        {
            Fail("UltraCraft couldn't start:\n\n" + e.Message);
        }
    }

    static bool CompanionRunning()
    {
        string runtime = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Prism), ".."));
        foreach (var p in Process.GetProcessesByName("javaw"))
        {
            try
            {
                if (p.MainModule.FileName.StartsWith(runtime, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch (Exception)
            {
                // Another user's or a protected process: not ours.
            }
        }
        return false;
    }

    static void Fail(string message)
    {
        MessageBox.Show(message, "UltraCraft", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
