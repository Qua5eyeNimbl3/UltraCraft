# Builds the desktop programs with the .NET Framework compiler that ships with Windows:
#   UltraCraft.exe               starts the Java companion, then ULTRAKILL
#   Reset UltraCraft World.exe   backs up and removes the world (after confirmation) so the next launch makes a new seed
# The icon is taken from the locally installed ULTRAKILL.exe at build time and is not stored in the project.
param([string]$Folder = [Environment]::GetFolderPath('Desktop'))
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$icon = Join-Path $env:TEMP 'ultracraft-launcher.ico'
Add-Type -AssemblyName System.Drawing
$extracted = [System.Drawing.Icon]::ExtractAssociatedIcon('C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL\ULTRAKILL.exe')
$stream = [System.IO.File]::Create($icon); $extracted.Save($stream); $stream.Close()
$launcher = Join-Path $Folder 'UltraCraft.exe'
$reset = Join-Path $Folder 'Reset UltraCraft World.exe'
& $csc /nologo /target:winexe /optimize+ "/win32icon:$icon" "/out:$launcher" /r:System.Windows.Forms.dll (Join-Path $here 'UltraCraftLauncher.cs')
if ($LASTEXITCODE -ne 0) { throw 'Launcher build failed' }
& $csc /nologo /target:winexe /optimize+ "/win32icon:$icon" "/out:$reset" /r:System.Windows.Forms.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll (Join-Path $here 'ResetWorld.cs')
if ($LASTEXITCODE -ne 0) { throw 'Reset tool build failed' }
# The all-in-one player program for releases (installs and starts everything), with the mod files embedded.
$mod = Split-Path (Split-Path $here -Parent) -Parent
$release = Join-Path (Split-Path $mod -Parent) 'release'
New-Item -ItemType Directory -Force $release | Out-Null
$setup = Join-Path $release 'UltraCraft.exe'
$dll = Join-Path $mod 'host\bin\Release\netstandard2.1\UltraCraft.dll'
$shader = Join-Path $mod 'shaders\built\ultracraft-shaders'
$jar = Get-ChildItem (Join-Path $mod 'java\build\libs') -Filter *.jar | Where-Object { $_.Name -notlike '*sources*' } | Select-Object -First 1
& $csc /nologo /target:winexe /optimize+ "/win32icon:$icon" "/out:$setup" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll "/resource:$dll,UltraCraft.dll" "/resource:$shader,ultracraft-shaders" "/resource:$($jar.FullName),ultracraft-java.jar" (Join-Path $here 'UltraCraftSetup.cs')
if ($LASTEXITCODE -ne 0) { throw 'Player program build failed' }
Remove-Item $icon -Force
Get-Item $launcher, $reset, $setup | Select-Object FullName, Length
