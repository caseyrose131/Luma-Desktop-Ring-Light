$ErrorActionPreference = 'Stop'
& "$PSScriptRoot\make-icon.ps1"
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /optimize+ /out:"$PSScriptRoot\LUMA.exe" /win32icon:"$PSScriptRoot\LUMA.ico" /reference:System.Drawing.dll /reference:System.Windows.Forms.dll "$PSScriptRoot\RingLight.cs" "$PSScriptRoot\Interface.cs" "$PSScriptRoot\AssemblyInfo.cs" "$PSScriptRoot\Settings.cs" "$PSScriptRoot\SingleInstance.cs"
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }


