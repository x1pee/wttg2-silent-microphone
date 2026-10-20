$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo 'bin\tests'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$sources=@((Join-Path $repo 'src\SilentMic.cs'),(Join-Path $PSScriptRoot 'StartupSafetyTests.cs'))
foreach($variant in @('compatible','incompatible')) {
    $executable=Join-Path $output ($variant+'.exe')
    $arguments=@('/nologo','/noconfig','/target:exe','/optimize+','/r:System.dll','/r:System.Core.dll','/nowarn:0169',('/out:'+$executable))
    if($variant -eq 'incompatible') { $arguments+='/define:BAD_SIGNATURE' }
    & $compiler @arguments @sources
    if($LASTEXITCODE -ne 0) {throw 'Test build failed'}
    & $executable
    if($LASTEXITCODE -ne 0) {throw 'Startup safety test failed'}
}
