param([Parameter(Mandatory=$true)][string]$Editor)
$ErrorActionPreference='Stop'
$taskModRoot=Split-Path $PSScriptRoot -Parent
python (Join-Path $PSScriptRoot 'preflight.py') --generate
if ($LASTEXITCODE -ne 0) { throw 'Shader preflight failed' }
$taskWorkspace=Split-Path (Split-Path $taskModRoot -Parent) -Parent
$taskProject=Join-Path $taskWorkspace 'work\shader-project'
New-Item -ItemType Directory -Path "$taskProject\Assets\Editor","$taskProject\ProjectSettings","$taskProject\Packages" -Force | Out-Null
Copy-Item -LiteralPath "$taskModRoot\shaders\World.shader" -Destination "$taskProject\Assets\World.shader"
Copy-Item -LiteralPath "$PSScriptRoot\BuildShaders.cs" -Destination "$taskProject\Assets\Editor\BuildShaders.cs"
Set-Content -LiteralPath "$taskProject\Packages\manifest.json" -Value '{"dependencies":{"com.unity.modules.assetbundle":"1.0.0"}}'
Set-Content -LiteralPath "$taskProject\ProjectSettings\ProjectVersion.txt" -Value "m_EditorVersion: 2022.3.29f1`nm_EditorVersionWithRevision: 2022.3.29f1 (8d510ca76d2b)"
$env:ULTRACRAFT_SHADER_OUTPUT=Join-Path $taskModRoot 'shaders\built'
$taskBuildLog=Join-Path $taskWorkspace 'work\shader-build.log'
$taskArguments=@('-batchmode','-nographics','-quit','-projectPath',('"{0}"' -f $taskProject),'-executeMethod','BuildShaders.Run','-logFile',('"{0}"' -f $taskBuildLog))
$taskCompiler=Start-Process -FilePath (Resolve-Path -LiteralPath $Editor).Path -ArgumentList $taskArguments -WindowStyle Hidden -Wait -PassThru
if ($taskCompiler.ExitCode -ne 0) { throw 'Shader build failed; see work/shader-build.log' }
if (-not (Test-Path -LiteralPath "$env:ULTRACRAFT_SHADER_OUTPUT\ultracraft-shaders")) { throw 'Shader bundle missing' }
