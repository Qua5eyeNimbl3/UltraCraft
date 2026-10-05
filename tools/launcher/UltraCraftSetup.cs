// UltraCraft: one program that installs and starts everything for a player.
//  - Finds ULTRAKILL through Steam, installs BepInEx 5.4.23.5 if missing, and copies the UltraCraft plugin.
//  - Downloads a private portable Prism Launcher, Java 25 and Fabric API from their official releases,
//    and creates a Minecraft 26.3 + Fabric instance with the UltraCraft mod.
//  - The player signs in with their own Microsoft account in Prism (once); then Play starts Minecraft
//    hidden and ULTRAKILL through Steam. Reset world backs up and removes the world (new random seed).
// Everything else lives in %LOCALAPPDATA%\UltraCraft. The mod files are embedded in this program.
// Build: tools\launcher\build.ps1 (C# 5, the .NET Framework compiler built into Windows).
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

internal sealed class UltraCraftSetup : Form
{
    const string Version = "0.1";
    const string SteamAppId = "1229490";
    const string BepInExUrl = "https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_win_x64_5.4.23.5.zip";
    const string PrismUrl = "https://github.com/PrismLauncher/PrismLauncher/releases/download/11.1.1/PrismLauncher-Windows-MSVC-Portable-11.1.1.zip";
    const string FabricApiUrl = "https://github.com/FabricMC/fabric-api/releases/download/0.161.0%2B26.3/fabric-api-0.161.0%2B26.3.jar";
    const string JavaUrl = "https://api.adoptium.net/v3/binary/latest/25/ga/windows/x64/jre/hotspot/normal/eclipse";
    const string MinecraftVersion = "26.3";
    const string FabricLoaderVersion = "0.19.5";
    const string Instance = "UltraCraft";
    const string JvmArgs = "--enable-native-access=ALL-UNNAMED -Dskycraft.startHidden=true -Dskycraft.quitWithSkyrim=true -Dultracraft.hostMovement=true -Dskycraft.discordAppId=0";

    string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UltraCraft");
    readonly TextBox log = new TextBox();
    readonly Button play = new Button(), account = new Button(), reset = new Button();
    string PrismDir { get { return Path.Combine(root, "Prism"); } }
    string PrismExe { get { return Path.Combine(PrismDir, "prismlauncher.exe"); } }
    string InstanceDir { get { return Path.Combine(PrismDir, "instances", Instance); } }
    string SaveDir { get { return Path.Combine(InstanceDir, ".minecraft", "saves", "UltraCraft"); } }

    [STAThread]
    static void Main(string[] args)
    {
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2 for GitHub and Adoptium
        Application.EnableVisualStyles();
        if (args.Length == 2 && args[0] == "--selftest")
        {
            new UltraCraftSetup().SelfTest(args[1]);
            return;
        }
        Application.Run(new UltraCraftSetup());
    }

    /// <summary>
    /// Developer check into a scratch folder: downloads, extraction, Prism and instance files and the
    /// embedded mod files. Never touches ULTRAKILL, never opens Prism or signs in. Writes selftest.log.
    /// </summary>
    void SelfTest(string scratch)
    {
        root = scratch;
        Directory.CreateDirectory(root);
        try
        {
            string[] libraries = SteamLibraries();
            Say("Steam libraries: " + string.Join("; ", libraries));
            foreach (string library in libraries)
            {
                if (File.Exists(Path.Combine(library, "steamapps", "common", "ULTRAKILL", "ULTRAKILL.exe"))) { Say("ULTRAKILL found in " + library); }
            }
            string plugin = Path.Combine(root, "plugin-test");
            Directory.CreateDirectory(plugin);
            WriteResource("UltraCraft.dll", Path.Combine(plugin, "UltraCraft.dll"));
            WriteResource("ultracraft-shaders", Path.Combine(plugin, "ultracraft-shaders"));
            string java = EnsureJava();
            Say("Java: " + java);
            EnsurePrism(java);
            Say("Prism: " + File.Exists(PrismExe));
            EnsureInstance(java);
            Say("Account: " + HasAccount());
            Say("SELFTEST OK");
        }
        catch (Exception e)
        {
            Say("SELFTEST FAILED: " + e);
        }
        File.WriteAllText(Path.Combine(root, "selftest.log"), log.Text);
    }

    UltraCraftSetup()
    {
        Text = "UltraCraft " + Version;
        ClientSize = new Size(560, 360);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch (Exception) { }
        var title = new Label { Text = "UltraCraft " + Version + " - ULTRAKILL x Minecraft", Font = new Font(Font.FontFamily, 13, FontStyle.Bold), Location = new Point(14, 12), AutoSize = true };
        log.Multiline = true; log.ReadOnly = true; log.ScrollBars = ScrollBars.Vertical;
        log.Location = new Point(14, 48); log.Size = new Size(532, 248);
        play.Text = "Play"; play.Location = new Point(14, 308); play.Size = new Size(170, 38); play.Font = new Font(Font.FontFamily, 11, FontStyle.Bold);
        account.Text = "Minecraft account"; account.Location = new Point(195, 308); account.Size = new Size(170, 38);
        reset.Text = "Reset world"; reset.Location = new Point(376, 308); reset.Size = new Size(170, 38);
        Controls.AddRange(new Control[] { title, log, play, account, reset });
        play.Click += delegate { Run(Play); };
        account.Click += delegate { Run(OpenAccounts); };
        reset.Click += delegate { ResetWorld(); };
        Say("Press Play. The first time, UltraCraft downloads Prism Launcher, Java and Fabric (about 150 MB)");
        Say("and installs BepInEx and the UltraCraft plugin into ULTRAKILL.");
        Say("You need ULTRAKILL on Steam and a Minecraft Java account (you sign in once).");
    }

    // ---- UI helpers ----
    void Say(string line)
    {
        if (InvokeRequired) { BeginInvoke(new Action<string>(Say), line); return; }
        log.AppendText(line + Environment.NewLine);
    }

    void Run(Action work)
    {
        SetBusy(true);
        var thread = new Thread(delegate ()
        {
            try { work(); }
            catch (Exception e) { Say("Error: " + e.Message); Ask("UltraCraft ran into a problem:\n\n" + e.Message, MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { SetBusy(false); }
        });
        thread.IsBackground = true;
        thread.Start();
    }

    void SetBusy(bool busy)
    {
        if (InvokeRequired) { BeginInvoke(new Action<bool>(SetBusy), busy); return; }
        play.Enabled = account.Enabled = reset.Enabled = !busy;
        UseWaitCursor = busy;
    }

    DialogResult Ask(string message, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        if (InvokeRequired) { return (DialogResult)Invoke(new Func<string, MessageBoxButtons, MessageBoxIcon, DialogResult>(Ask), message, buttons, icon); }
        return MessageBox.Show(this, message, "UltraCraft", buttons, icon);
    }

    string PickFolder(string prompt)
    {
        if (InvokeRequired) { return (string)Invoke(new Func<string, string>(PickFolder), prompt); }
        using (var dialog = new FolderBrowserDialog { Description = prompt })
        {
            return dialog.ShowDialog(this) == DialogResult.OK ? dialog.SelectedPath : null;
        }
    }

    // ---- Play ----
    void Play()
    {
        if (Process.GetProcessesByName("ULTRAKILL").Length > 0)
        {
            Ask("ULTRAKILL is already running. Close it first, then press Play again.", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        string game = FindGame();
        if (game == null) { Say("ULTRAKILL was not found; nothing was changed."); return; }
        Say("ULTRAKILL: " + game);
        InstallBepInEx(game);
        InstallPlugin(game);
        string java = EnsureJava();
        EnsurePrism(java);
        EnsureInstance(java);
        if (!HasAccount())
        {
            Ask("Sign in to Minecraft once.\n\nPrism Launcher opens next on \"Add Microsoft account\": click that button and sign in with the account that owns Minecraft Java Edition, then click Finish and close Prism. UltraCraft continues by itself.\n\n(Later you can change accounts with the \"Minecraft account\" button.)",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            OpenAccountsAndWait();
            if (!HasAccount()) { Say("No Minecraft account yet: press Play again after signing in."); return; }
        }
        if (!CompanionRunning())
        {
            Say("Starting Minecraft (it stays hidden)...");
            Process.Start(new ProcessStartInfo(PrismExe, "--dir \"" + PrismDir + "\" --launch " + Instance) { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden, WorkingDirectory = PrismDir });
        }
        Say("Starting ULTRAKILL through Steam...");
        Process.Start(new ProcessStartInfo("steam://rungameid/" + SteamAppId) { UseShellExecute = true });
        Say("Done! After ULTRAKILL's intro you are put into your Minecraft world. Closing ULTRAKILL saves and closes Minecraft.");
        Say("The first start of Minecraft downloads the game itself, so it can take a few minutes.");
    }

    string FindGame()
    {
        foreach (string library in SteamLibraries())
        {
            string folder = Path.Combine(library, "steamapps", "common", "ULTRAKILL");
            if (File.Exists(Path.Combine(folder, "ULTRAKILL.exe"))) { return folder; }
        }
        string saved = Path.Combine(root, "ultrakill-path.txt");
        if (File.Exists(saved) && File.Exists(Path.Combine(File.ReadAllText(saved).Trim(), "ULTRAKILL.exe"))) { return File.ReadAllText(saved).Trim(); }
        Ask("ULTRAKILL wasn't found in your Steam libraries. Pick the ULTRAKILL game folder (the one with ULTRAKILL.exe).", MessageBoxButtons.OK, MessageBoxIcon.Information);
        string picked = PickFolder("Select the ULTRAKILL folder (contains ULTRAKILL.exe)");
        if (picked == null || !File.Exists(Path.Combine(picked, "ULTRAKILL.exe"))) { return null; }
        Directory.CreateDirectory(root);
        File.WriteAllText(saved, picked);
        return picked;
    }

    static string[] SteamLibraries()
    {
        var found = new System.Collections.Generic.List<string>();
        string steam = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string
            ?? Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null) as string;
        if (steam == null) { return found.ToArray(); }
        steam = steam.Replace('/', '\\');
        found.Add(steam);
        string vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
        if (File.Exists(vdf))
        {
            foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
            {
                string library = m.Groups[1].Value.Replace("\\\\", "\\");
                if (!found.Exists(f => string.Equals(f.TrimEnd('\\'), library.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))) { found.Add(library); }
            }
        }
        return found.ToArray();
    }

    void InstallBepInEx(string game)
    {
        if (!File.Exists(Path.Combine(game, "BepInEx", "core", "BepInEx.dll")))
        {
            Say("Installing BepInEx 5.4.23.5 into ULTRAKILL...");
            string zip = Download(BepInExUrl, "BepInEx.zip");
            Extract(zip, game);
        }
        // UltraCraft's bridge object must survive ULTRAKILL's scene loads.
        string config = Path.Combine(game, "BepInEx", "config", "BepInEx.cfg");
        Directory.CreateDirectory(Path.GetDirectoryName(config));
        string text = File.Exists(config) ? File.ReadAllText(config) : "";
        if (Regex.IsMatch(text, @"(?m)^HideManagerGameObject\s*=\s*true")) { return; }
        if (Regex.IsMatch(text, @"(?m)^HideManagerGameObject\s*="))
        {
            text = Regex.Replace(text, @"(?m)^HideManagerGameObject\s*=.*$", "HideManagerGameObject = true");
        }
        else if (Regex.IsMatch(text, @"(?m)^\[Chainloader\]"))
        {
            text = Regex.Replace(text, @"(?m)^\[Chainloader\]\s*$", "[Chainloader]\r\n\r\nHideManagerGameObject = true");
        }
        else
        {
            text += "\r\n[Chainloader]\r\n\r\nHideManagerGameObject = true\r\n";
        }
        File.WriteAllText(config, text);
    }

    void InstallPlugin(string game)
    {
        string plugin = Path.Combine(game, "BepInEx", "plugins", "UltraCraft");
        Directory.CreateDirectory(plugin);
        WriteResource("UltraCraft.dll", Path.Combine(plugin, "UltraCraft.dll"));
        WriteResource("ultracraft-shaders", Path.Combine(plugin, "ultracraft-shaders"));
        Say("UltraCraft plugin installed.");
    }

    string EnsureJava()
    {
        string dir = Path.Combine(root, "Java");
        string java = FindFile(dir, "javaw.exe");
        if (java != null) { return java; }
        Say("Downloading Java 25 (Eclipse Temurin)...");
        string zip = Download(JavaUrl, "java.zip");
        Extract(zip, dir);
        java = FindFile(dir, "javaw.exe");
        if (java == null) { throw new Exception("Java was downloaded but javaw.exe wasn't found."); }
        return java;
    }

    void EnsurePrism(string java)
    {
        if (!File.Exists(PrismExe))
        {
            Say("Downloading Prism Launcher 11.1.1...");
            string zip = Download(PrismUrl, "prism.zip");
            Extract(zip, PrismDir);
        }
        string cfg = Path.Combine(PrismDir, "prismlauncher.cfg");
        if (!File.Exists(cfg))
        {
            // Pre-filled so Prism's first-run wizard has nothing to ask.
            File.WriteAllText(cfg, "[General]\r\nConfigVersion=1.3\r\nLanguage=en_US\r\nApplicationTheme=dark\r\nIconTheme=pe_colored\r\nJavaPath=" + java.Replace('\\', '/') +
                "\r\nAutomaticJavaDownload=true\r\nAutomaticJavaSwitch=true\r\nIgnoreJavaWizard=true\r\nUserAskedAboutAutomaticJavaDownload=true\r\nMaxMemAlloc=4096\r\nShowConsole=false\r\nShowConsoleOnError=true\r\nAutoCloseConsole=false\r\n");
        }
    }

    void EnsureInstance(string java)
    {
        string mc = Path.Combine(InstanceDir, ".minecraft");
        string mods = Path.Combine(mc, "mods");
        Directory.CreateDirectory(mods);
        File.WriteAllText(Path.Combine(InstanceDir, "instance.cfg"),
            "[General]\r\nInstanceType=OneSix\r\nname=UltraCraft\r\niconKey=default\r\nOverrideJavaLocation=true\r\nJavaPath=" + java.Replace('\\', '/') +
            "\r\nOverrideMemory=true\r\nMaxMemAlloc=4096\r\nMinMemAlloc=512\r\nOverrideJavaArgs=true\r\nJvmArgs=" + JvmArgs +
            "\r\nOverrideWindow=true\r\nMinecraftWinWidth=1280\r\nMinecraftWinHeight=720\r\nShowConsole=false\r\nLaunchMaximized=false\r\n");
        string pack = Path.Combine(InstanceDir, "mmc-pack.json");
        if (!File.Exists(pack))
        {
            File.WriteAllText(pack, "{\n  \"components\": [\n" +
                "    { \"uid\": \"net.minecraft\", \"version\": \"" + MinecraftVersion + "\", \"important\": true },\n" +
                "    { \"uid\": \"net.fabricmc.intermediary\", \"version\": \"" + MinecraftVersion + "\", \"dependencyOnly\": true },\n" +
                "    { \"uid\": \"net.fabricmc.fabric-loader\", \"version\": \"" + FabricLoaderVersion + "\" }\n" +
                "  ],\n  \"formatVersion\": 1\n}\n");
        }
        string options = Path.Combine(mc, "options.txt");
        if (!File.Exists(options))
        {
            File.WriteAllText(options, "version:0\nlang:en_us\nrenderDistance:8\nsimulationDistance:6\nguiScale:2\nfullscreen:false\nonboardAccessibility:false\n");
        }
        WriteResource("ultracraft-java.jar", Path.Combine(mods, "ultracraft-java.jar"));
        string api = Path.Combine(mods, "fabric-api.jar");
        if (!File.Exists(api))
        {
            Say("Downloading Fabric API 0.161.0...");
            File.Copy(Download(FabricApiUrl, "fabric-api.jar"), api, true);
        }
        Say("Minecraft " + MinecraftVersion + " + Fabric instance ready.");
    }

    bool HasAccount()
    {
        string accounts = Path.Combine(PrismDir, "accounts.json");
        return File.Exists(accounts) && File.ReadAllText(accounts).Contains("\"profile\"");
    }

    void OpenAccounts()
    {
        string java = EnsureJava();
        EnsurePrism(java);
        EnsureInstance(java);
        OpenAccountsAndWait();
        Say(HasAccount() ? "Minecraft account found." : "No Minecraft account found yet.");
    }

    void OpenAccountsAndWait()
    {
        Say("Opening Prism Launcher: add your Microsoft account, then close Prism.");
        var prism = Process.Start(new ProcessStartInfo(PrismExe, "--dir \"" + PrismDir + "\"") { UseShellExecute = false, WorkingDirectory = PrismDir });
        prism.WaitForExit();
    }

    bool CompanionRunning()
    {
        string java = Path.Combine(root, "Java");
        foreach (var p in Process.GetProcessesByName("javaw"))
        {
            try { if (p.MainModule.FileName.StartsWith(java, StringComparison.OrdinalIgnoreCase)) { return true; } }
            catch (Exception) { }
        }
        return false;
    }

    // ---- Reset world ----
    void ResetWorld()
    {
        if (Process.GetProcessesByName("ULTRAKILL").Length > 0 || CompanionRunning())
        {
            Ask("Close UltraCraft (ULTRAKILL) first and wait a few seconds for the world to save.", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!Directory.Exists(SaveDir))
        {
            Ask("There is no world to reset yet. The next Play creates one with a random seed.", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        string backups = Path.Combine(root, "Backups");
        if (Ask("Reset your UltraCraft world?\n\nYour world, buildings and inventory are removed. A backup zip is kept in:\n" + backups +
            "\n\nThe next Play generates a brand-new world with a new random seed.", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }
        Directory.CreateDirectory(backups);
        string zip = Path.Combine(backups, DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".zip");
        ZipFile.CreateFromDirectory(SaveDir, zip, CompressionLevel.Optimal, false);
        Directory.Delete(SaveDir, true);
        Say("World reset. Backup: " + zip);
    }

    // ---- files ----
    string Download(string url, string name)
    {
        string dir = Path.Combine(root, "Downloads");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, name);
        string partial = path + ".part";
        using (var web = new WebClient())
        {
            web.Headers[HttpRequestHeader.UserAgent] = "UltraCraft-Setup/" + Version;
            web.DownloadFile(url, partial);
        }
        if (File.Exists(path)) { File.Delete(path); }
        File.Move(partial, path);
        Say("  downloaded " + name + " (" + (new FileInfo(path).Length >> 20) + " MB)");
        return path;
    }

    static void Extract(string zip, string destination)
    {
        string full = Path.GetFullPath(destination);
        Directory.CreateDirectory(full);
        using (ZipArchive archive = ZipFile.OpenRead(zip))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string target = Path.GetFullPath(Path.Combine(full, entry.FullName));
                if (!target.StartsWith(full, StringComparison.OrdinalIgnoreCase)) { continue; } // never outside the folder
                if (entry.Name.Length == 0) { Directory.CreateDirectory(target); continue; }
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                entry.ExtractToFile(target, true);
            }
        }
    }

    static string FindFile(string dir, string name)
    {
        if (!Directory.Exists(dir)) { return null; }
        string[] hits = Directory.GetFiles(dir, name, SearchOption.AllDirectories);
        return hits.Length > 0 ? hits[0] : null;
    }

    static void WriteResource(string name, string destination)
    {
        using (Stream source = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
        {
            if (source == null) { throw new Exception("This UltraCraft program is missing its " + name + "."); }
            using (FileStream output = File.Create(destination)) { source.CopyTo(output); }
        }
    }
}
