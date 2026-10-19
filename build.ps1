param([string]$GamePath='W:\Games\Welcome to the Game II', [string]$OutputPath="$PSScriptRoot\bin")
$ErrorActionPreference='Stop'
$managed=Join-Path $GamePath 'WTTG2_Data\Managed'
$core=Join-Path $GamePath 'BepInEx\core'
New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null
$refs=@('mscorlib.dll','System.dll','System.Core.dll','Assembly-CSharp.dll','UnityEngine.dll','UnityEngine.CoreModule.dll') | ForEach-Object { '/r:'+(Join-Path $managed $_) }
$refs+='/r:'+(Join-Path $core 'BepInEx.dll')
$refs+='/r:'+(Join-Path $core '0Harmony.dll')
& (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe') /nologo /noconfig /nostdlib+ /target:library /optimize+ ('/out:'+(Join-Path $OutputPath 'WTTG2.SilentMicrophone.dll')) @refs (Join-Path $PSScriptRoot 'src\SilentMic.cs')
if($LASTEXITCODE -ne 0){throw 'Build failed'}
