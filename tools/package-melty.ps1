# Packs a Melty release into dist\ (one click: Melty installs BepInEx 5 for ULTRAKILL itself):
#   UltraCraft-<version>.zip            plugins/  -> {game}/BepInEx/plugins/UltraCraft
#   UltraCraft-Minecraft-<version>.zip  (root)    -> {localappdata}/UltraCraft
# The Minecraft package is a portable Prism Launcher with a ready "UltraCraft" instance (Minecraft
# 26.3, Fabric Loader 0.19.5, Fabric API, the UltraCraft mod and its launch flags). It holds no
# Minecraft and no account: Prism asks the player to sign in once, then downloads Minecraft and Java.
#   powershell -ExecutionPolicy Bypass -File tools\package-melty.ps1
param([string]$Version = "0.1.0")
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$root = Split-Path -Parent $PSScriptRoot

# Pinned downloads from their official sources (checked against these hashes).
$prismVersion = "11.1.1"
$prismZip = "PrismLauncher-Windows-MSVC-Portable-$prismVersion.zip"
$prismUrl = "https://github.com/PrismLauncher/PrismLauncher/releases/download/$prismVersion/$prismZip"
$prismSha256 = "ab35a770fb06d89d2ccc098079db5db329fb4e68f42b72babd8b095efde3d2d7"
$prismLicenseUrl = "https://raw.githubusercontent.com/PrismLauncher/PrismLauncher/$prismVersion/LICENSE"
$fabricApiJar = "fabric-api-0.161.0+26.3.jar"
$fabricApiUrl = "https://cdn.modrinth.com/data/P7dR8mSH/versions/bNnaTiuM/fabric-api-0.161.0%2B26.3.jar"
$fabricApiSha512 = "ed6b2586d6fde11fde8472f5a527c51e99b67026e46f94d4bfd85e7e28ce5ee299173ee16ad576ceb51f39f98d30a811086a6deb1a86a524859cc16e12da109d"
$fabricApiLicenseUrl = "https://raw.githubusercontent.com/FabricMC/fabric/HEAD/LICENSE"

function Get-Pinned([string]$url, [string]$path, [string]$algorithm, [string]$hash) {
    if (-not (Test-Path $path)) {
        New-Item -ItemType Directory (Split-Path $path) -Force | Out-Null
        Invoke-WebRequest -Uri $url -OutFile $path -UseBasicParsing
    }
    if ($hash -and (Get-FileHash $path -Algorithm $algorithm).Hash -ne $hash.ToUpper()) {
        Remove-Item $path
        throw "$path doesn't match its pinned $algorithm hash"
    }
}

# Zip entries with forward slashes, as the zip format expects.
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
function New-Zip([string]$path, [System.Collections.IDictionary]$entries) {
    if (Test-Path $path) { Remove-Item $path }
    $zip = [System.IO.Compression.ZipFile]::Open($path, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($name in $entries.Keys) {
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $entries[$name], $name, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } finally { $zip.Dispose() }
}
function Add-Folder([System.Collections.IDictionary]$entries, [string]$folder, [string]$prefix) {
    $base = (Resolve-Path $folder).Path.TrimEnd('\') + '\'
    Get-ChildItem $folder -Recurse -File | Sort-Object FullName | ForEach-Object {
        $entries[$prefix + $_.FullName.Substring($base.Length).Replace('\', '/')] = $_.FullName
    }
}

$dll = "$root\host\bin\Release\netstandard2.1\UltraCraft.dll"
$shader = "$root\shaders\built\ultracraft-shaders"
$jar = Get-ChildItem "$root\java\build\libs" -Filter *.jar | Where-Object { $_.Name -notlike '*sources*' } | Select-Object -First 1
foreach ($f in @($dll, $shader, $jar.FullName)) { if (-not (Test-Path $f)) { throw "missing $f (build both halves first)" } }

$cache = "$root\.tools\melty"
Get-Pinned $prismUrl "$cache\$prismZip" SHA256 $prismSha256
Get-Pinned $prismLicenseUrl "$cache\PrismLauncher-$prismVersion-LICENSE.txt" "" ""
Get-Pinned $fabricApiUrl "$cache\$fabricApiJar" SHA512 $fabricApiSha512
Get-Pinned $fabricApiLicenseUrl "$cache\FabricAPI-LICENSE.txt" "" ""

$dist = "$root\dist"
New-Item -ItemType Directory $dist -Force | Out-Null
$stage = "$dist\stage"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
$bundle = New-Item -ItemType Directory "$stage\minecraft" -Force
Expand-Archive "$cache\$prismZip" "$bundle\Prism" -Force
Copy-Item -Recurse "$root\tools\melty-bundle\*" $bundle -Force
Copy-Item "$cache\PrismLauncher-$prismVersion-LICENSE.txt" "$bundle\Prism\LICENSE-PrismLauncher.txt"
Copy-Item "$cache\FabricAPI-LICENSE.txt" "$bundle\Prism\LICENSE-FabricAPI.txt"
Copy-Item "$root\THIRD-PARTY\SkyCraft-PeakCraft-MIT.txt" "$bundle\Prism\LICENSE-SkyCraft-PeakCraft.txt"
Copy-Item "$root\LICENSE" "$bundle\Prism\LICENSE-UltraCraft.txt"
Copy-Item "$root\tools\melty-bundle-readme.txt" "$bundle\README.txt"
$mods = New-Item -ItemType Directory "$bundle\Prism\instances\UltraCraft\.minecraft\mods" -Force
Copy-Item "$cache\$fabricApiJar" $mods
Copy-Item $jar.FullName "$mods\ultracraft-$Version.jar"
Set-Content "$bundle\bundle-version.txt" "UltraCraft $Version, Prism Launcher $prismVersion, $fabricApiJar" -NoNewline

$mcEntries = [ordered]@{}
Add-Folder $mcEntries $bundle ""
New-Zip "$dist\UltraCraft-Minecraft-$Version.zip" $mcEntries

New-Zip "$dist\UltraCraft-$Version.zip" ([ordered]@{
    "plugins/UltraCraft.dll" = $dll
    "plugins/ultracraft-shaders" = $shader
    "plugins/THIRD-PARTY/SkyCraft-PeakCraft-MIT.txt" = "$root\THIRD-PARTY\SkyCraft-PeakCraft-MIT.txt"
    "plugins/THIRD-PARTY/Bridge-notices.md" = "$root\THIRD-PARTY\Bridge-notices.md"
    "plugins/README.txt" = "$root\tools\melty-bundle-readme.txt"
    "plugins/LICENSE.txt" = "$root\LICENSE"
})
Remove-Item $stage -Recurse -Force

Get-ChildItem $dist -File | ForEach-Object { "{0,-40} {1,12:N0} bytes  {2}" -f $_.Name, $_.Length, (Get-FileHash $_.FullName -Algorithm SHA256).Hash }
