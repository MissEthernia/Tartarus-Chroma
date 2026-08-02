$ErrorActionPreference = "Stop"

$Project = Join-Path $PSScriptRoot "src\TartarusChroma\TartarusChroma.csproj"
$Output = Join-Path $PSScriptRoot "artifacts\win-x64"

dotnet restore $Project
dotnet publish $Project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $Output

Write-Host ""
Write-Host "Fertig: $Output\TartarusChroma.exe" -ForegroundColor Green
