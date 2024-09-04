dotnet build

Remove-Item "..\ModBuild\Konfig" -Recurse

New-Item "..\ModBuild\Konfig" -ItemType "directory"
New-Item "..\ModBuild\Konfig\BepInEx" -ItemType "directory"

New-Item "..\ModBuild\Konfig\BepInEx\plugins" -ItemType "directory"
New-Item "..\ModBuild\Konfig\BepInEx\plugins\Konfig" -ItemType "directory"

Copy-Item ".\bin\Debug\netstandard2.1\Konfig.dll" "..\ModBuild\Konfig\BepInEx\plugins\Konfig"
Copy-Item ".\bin\Debug\netstandard2.1\Konfig.pdb" "..\ModBuild\Konfig\BepInEx\plugins\Konfig"

Copy-Item "..\ModBuild\Konfig\BepInEx\*" "..\Game\BepInEx" -Recurse -Force

Start-Process "..\Game\KSP2_x64.exe"