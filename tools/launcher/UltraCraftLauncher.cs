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
                SyncWithMelty(Path.Combine(prismData, "instances", Instance));
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

    /// <summary>
    /// Keeps this instance on the release Melty installed: the ULTRAKILL plugin folder is shared, so the
    /// Java mod must be Melty's too. Copies Melty's mod jars (UltraCraft and Fabric API) over the lab's and
    /// adds Melty's launch flags and memory to the lab's settings. The lab's world and its own extra flags
    /// (such as -Dskycraft.showWindow=true) stay. Without Melty's copy nothing changes.
    /// </summary>
    static void SyncWithMelty(string lab)
    {
        string melty = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"UltraCraft\Prism\instances\UltraCraft");
        string meltyMods = Path.Combine(melty, @".minecraft\mods");
        string labMods = Path.Combine(lab, @".minecraft\mods");
        try
        {
            string[] jars = Directory.Exists(meltyMods) ? Directory.GetFiles(meltyMods, "*.jar") : new string[0];
            if (!jars.Any(j => Path.GetFileName(j).StartsWith("ultracraft-", StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }
            Directory.CreateDirectory(labMods);
            foreach (string old in Directory.GetFiles(labMods, "*.jar"))
            {
                string name = Path.GetFileName(old);
                if (name.StartsWith("ultracraft-", StringComparison.OrdinalIgnoreCase) || name.StartsWith("fabric-api", StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(old);
                }
            }
            foreach (string jar in jars)
            {
                File.Copy(jar, Path.Combine(labMods, Path.GetFileName(jar)), true);
            }
            if (Process.GetProcessesByName("prismlauncher").Length == 0)
            {
                MergeSettings(Path.Combine(melty, "instance.cfg"), Path.Combine(lab, "instance.cfg"));
            }
        }
        catch (Exception e)
        {
            Fail("UltraCraft couldn't update its Minecraft mod from Melty's copy; it will start with the previous one.\n\n" + e.Message);
        }
    }

    static void MergeSettings(string from, string to)
    {
        if (!File.Exists(from) || !File.Exists(to))
        {
            return;
        }
        string[] source = File.ReadAllLines(from);
        string[] lines = File.ReadAllLines(to);
        string meltyArgs = Value(source, "JvmArgs");
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("JvmArgs=") && meltyArgs != null)
            {
                string mine = Value(lines, "JvmArgs") ?? "";
                var tokens = mine.Split(' ').Where(t => t.Length > 0).ToList();
                foreach (string t in meltyArgs.Split(' '))
                {
                    if (t.Length > 0 && !tokens.Contains(t)) tokens.Add(t);
                }
                lines[i] = "JvmArgs=\"" + string.Join(" ", tokens) + "\"";
            }
            else if (lines[i].StartsWith("MaxMemAlloc=") && Value(source, "MaxMemAlloc") != null)
            {
                lines[i] = "MaxMemAlloc=" + Value(source, "MaxMemAlloc");
            }
            else if (lines[i].StartsWith("MinMemAlloc=") && Value(source, "MinMemAlloc") != null)
            {
                lines[i] = "MinMemAlloc=" + Value(source, "MinMemAlloc");
            }
        }
        File.WriteAllLines(to, lines);
    }

    static string Value(string[] lines, string key)
    {
        string line = lines.FirstOrDefault(l => l.StartsWith(key + "="));
        return line == null ? null : line.Substring(key.Length + 1).Trim('"');
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
