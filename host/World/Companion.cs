using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
namespace UltraCraft.World;
/// <summary>
/// Keeps UltraCraft's Minecraft (the bundled Prism instance in %LOCALAPPDATA%\UltraCraft) running
/// and tells the player what is happening while ULTRAKILL waits for it. Melty normally starts it
/// alongside the game; when it isn't there (ULTRAKILL started from Steam, Minecraft closed or
/// crashed) it is started again, a few times at most, and a Minecraft crash is shown on screen.
/// </summary>
internal sealed class Companion {
    readonly string prismDir=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"UltraCraft","Prism");
    readonly DateTime startedUtc=DateTime.UtcNow;
    float lastAlive,lastStart=-1,lastCheck;
    int starts;
    string? crash;
    string PrismExe=>Path.Combine(prismDir,"prismlauncher.exe");
    string GameDir=>Path.Combine(prismDir,"instances","UltraCraft",".minecraft");
    public string Status {get;private set;}="";
    public Companion(){lastAlive=Time.realtimeSinceStartup;}

    public void Update(bool guestAlive,bool inWorld){
      float now=Time.realtimeSinceStartup;
      if(guestAlive){lastAlive=now;crash=null;Status=inWorld?"":"Minecraft is connected: opening your world...";return;}
      if(now-lastCheck<2)return;
      lastCheck=now;
      bool bundled=File.Exists(PrismExe);
      crash??=RecentCrash();
      bool signedIn=!bundled||File.Exists(Path.Combine(prismDir,"accounts.json"));
      if(crash!=null)Status="Minecraft stopped with an error: "+crash+"\nUltraCraft is starting it again. Details: "+Path.Combine(GameDir,"crash-reports");
      else if(!signedIn)Status="Sign in to Minecraft in the Prism Launcher window\n(the Microsoft account that owns Minecraft: Java Edition).";
      else if(!bundled)Status="Minecraft's launcher is missing. Return to Melty and reinstall UltraCraft to repair it.";
      else if(starts>=3)Status="Minecraft could not start after three attempts. Check the Prism Launcher window for a sign-in or error, then press Play again in Melty.";
      else Status=$"Starting Minecraft in the background... {now-lastAlive:F0}s\n(its window stays hidden on purpose; the first start downloads Minecraft and takes a few minutes)";
      if(!bundled||starts>=3)return;
      if(now-lastAlive<(starts==0?35:20))return;              // Melty starts it first; give it time to connect
      if(lastStart>=0&&now-lastStart<120)return;               // a start in progress (downloads, sign-in)
      if(MinecraftRunning())return;
      lastStart=now;starts++;crash=null;
      try{
        Process.Start(new ProcessStartInfo(PrismExe,"--launch UltraCraft"){UseShellExecute=true,WorkingDirectory=prismDir});
        Plugin.Log.LogWarning($"UltraCraft: Minecraft wasn't running; started it (attempt {starts})");
      }catch(Exception e){Plugin.Log.LogError("UltraCraft: couldn't start Minecraft: "+e.Message);starts=3;}
    }

    /// <summary>Prism or a Java from its folder is running, or Minecraft wrote its log moments ago.</summary>
    bool MinecraftRunning(){
      foreach(var name in new[]{"prismlauncher","javaw","java"}){
        foreach(var p in Process.GetProcessesByName(name)){
          try{if(p.MainModule!=null&&p.MainModule.FileName.StartsWith(prismDir,StringComparison.OrdinalIgnoreCase))return true;}
          catch(Exception){} // another user's or a protected process
        }
      }
      var log=new FileInfo(Path.Combine(GameDir,"logs","latest.log"));
      return log.Exists&&(DateTime.UtcNow-log.LastWriteTimeUtc).TotalSeconds<20;
    }

    /// <summary>The first line of a Minecraft crash report written during this session, if any.</summary>
    string? RecentCrash(){
      try{
        var dir=new DirectoryInfo(Path.Combine(GameDir,"crash-reports"));
        if(!dir.Exists)return null;
        var report=dir.GetFiles("crash-*.txt").Where(f=>f.LastWriteTimeUtc>startedUtc).OrderByDescending(f=>f.LastWriteTimeUtc).FirstOrDefault();
        if(report==null)return null;
        var line=File.ReadLines(report.FullName).FirstOrDefault(l=>l.StartsWith("Description:"))??report.Name;
        Plugin.Log.LogError("UltraCraft: Minecraft crash report "+report.FullName+" - "+line);
        return line.Replace("Description:","").Trim();
      }catch(Exception){return null;}
    }
}
