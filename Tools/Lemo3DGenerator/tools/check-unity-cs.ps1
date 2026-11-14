# Compila os scripts C# do Lemo3DGenerator FORA do Unity (Roslyn embarcado no Editor), usando as mesmas
# referÃªncias do DungeonCrawler.Editor.csproj gerado pelo Unity. Serve para garantir que copiar os
# scripts para Assets/ nÃ£o introduz erro de compilaÃ§Ã£o no projeto aberto.
#   powershell -ExecutionPolicy Bypass -File tools\check-unity-cs.ps1
param(
  [string]$UnityData = "C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Data",
  [string]$Project = (Resolve-Path "$PSScriptRoot\..\..\..").Path
)
$ErrorActionPreference = 'Stop'
$lab = Resolve-Path "$PSScriptRoot\.."
$csproj = Join-Path $Project 'DungeonCrawler.Editor.csproj'
[xml]$x = Get-Content $csproj
$refs = $x.Project.ItemGroup.Reference | Where-Object { $_.HintPath } | ForEach-Object { $_.HintPath } | Where-Object { Test-Path $_ }
$ns = Get-ChildItem "$UnityData\NetStandard\ref\2.1.0\netstandard.dll" -ErrorAction SilentlyContinue
if ($ns) { $refs = @($ns.FullName) + $refs }
$dotnet = "$UnityData\DotNetSdk\dotnet.exe"
$csc = (Get-ChildItem "$UnityData\DotNetSdk\sdk" -Recurse -Filter csc.dll | Select-Object -First 1).FullName
$out = Join-Path $env:TEMP 'Lemo3DGeneratorCheck'; New-Item -ItemType Directory -Force $out | Out-Null
$common = @('/nologo', '/noconfig', '/nostdlib+', '/langversion:9.0', '/target:library', '/nowarn:1701,1702', '/define:UNITY_EDITOR') + ($refs | ForEach-Object { "/r:$_" })

function Invoke-Csc([string]$name, [string[]]$extra, [string[]]$files) {
  $rsp = Join-Path $out "$name.rsp"
  ($common + $extra + "/out:$out\$name.dll" + $files) | ForEach-Object { if ($_ -match '\s') { '"' + $_ + '"' } else { $_ } } | Set-Content $rsp -Encoding UTF8
  & $dotnet $csc "@$rsp"
  if ($LASTEXITCODE) { throw "${name}: falha de compilaÃ§Ã£o" }
}
# assemblies do próprio jogo e pacotes (já compilados pelo Unity) usados pela ponte GameBridge
$game = Get-ChildItem "$Project\Library\ScriptAssemblies\*.dll" | Where-Object { $_.Name -match '^(DungeonCrawler\.Runtime|Unity\.Cinemachine|Unity\.InputSystem|Unity\.AI\.Navigation|UnityEngine\.UI)\.dll$' } | ForEach-Object { "/r:$($_.FullName)" }
Invoke-Csc 'DungeonCrawler.CharacterArt' @() (Get-ChildItem "$lab\unity\Runtime\*.cs").FullName
Invoke-Csc 'DungeonCrawler.CharacterArt.GameBridge' (@("/r:$out\DungeonCrawler.CharacterArt.dll") + $game) (Get-ChildItem "$lab\unity\GameBridge\*.cs").FullName
Invoke-Csc 'DungeonCrawler.CharacterArt.Editor' (@("/r:$out\DungeonCrawler.CharacterArt.dll", "/r:$out\DungeonCrawler.CharacterArt.GameBridge.dll") + $game) (Get-ChildItem "$lab\unity\Editor\*.cs").FullName
Write-Host "Compilacao OK (Runtime + GameBridge + Editor) com $($refs.Count) referencias do Unity."
