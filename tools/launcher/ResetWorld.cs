// Reset UltraCraft World: after the player confirms, backs the current Java world up to a zip and
// removes it, so the next UltraCraft launch creates a brand-new world with a new random seed.
// Build: tools\launcher\build.ps1 (C# 5, .NET Framework compiler built into Windows).
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

internal static class ResetWorld
{
    const string Runtime = @"C:\Users\homer\Documents\Codex\2026-10-05\files-pasted-by-the-user-help-2\work\runtime";

    [STAThread]
    static void Main()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string save = Path.Combine(appData, @"PrismLauncher\instances\UltraCraft-Lab\.minecraft\saves\UltraCraft");
        string backups = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @".universal-modder\backups\ultracraft-java-save");
        if (Process.GetProcessesByName("ULTRAKILL").Length > 0 || CompanionRunning())
        {
            Show("Close UltraCraft (ULTRAKILL) first, wait a few seconds for the world to save, then run the reset again.", MessageBoxIcon.Information);
            return;
        }
        if (!Directory.Exists(save))
        {
            Show("There is no UltraCraft world to reset. The next launch will create a new one with a random seed.", MessageBoxIcon.Information);
            return;
        }
        var answer = MessageBox.Show(
            "Reset your UltraCraft world?\n\nYour current Minecraft world, buildings and inventory will be removed. " +
            "A backup zip is saved first in:\n" + backups + "\n\nThe next time you start UltraCraft, a brand-new world with a new random seed is generated.",
            "Reset UltraCraft World", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes)
        {
            return;
        }
        try
        {
            Directory.CreateDirectory(backups);
            string zip = Path.Combine(backups, DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-before-reset.zip");
            ZipFile.CreateFromDirectory(save, zip, CompressionLevel.Optimal, false);
            Directory.Delete(save, true);
            Show("Done. Your old world is backed up at:\n" + zip + "\n\nStart UltraCraft to generate a new world.", MessageBoxIcon.Information);
        }
        catch (Exception e)
        {
            Show("The reset did not finish, and your world was not removed unless the backup was complete:\n\n" + e.Message, MessageBoxIcon.Error);
        }
    }

    static bool CompanionRunning()
    {
        foreach (var p in Process.GetProcessesByName("javaw"))
        {
            try
            {
                if (p.MainModule.FileName.StartsWith(Runtime, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch (Exception)
            {
                // Not ours.
            }
        }
        return false;
    }

    static void Show(string message, MessageBoxIcon icon)
    {
        MessageBox.Show(message, "Reset UltraCraft World", MessageBoxButtons.OK, icon);
    }
}
