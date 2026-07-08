param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "src\SureType\SureType.csproj"
$output = Join-Path $root "artifacts\SureType-win-x64-self-contained"

dotnet publish $project `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishReadyToRun=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -o $output

$exe = Join-Path $output "SureType.exe"
if (Test-Path $exe) {
    $size = (Get-Item $exe).Length
    Write-Host "Published: $exe"
    Write-Host ("Size: {0:N2} MB" -f ($size / 1MB))
}