param([string]$Configuration = "Release")
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $repoRoot "src\SureType\SureType.csproj"
[xml]$projectXml = Get-Content -LiteralPath $project -Raw
$version = [string]$projectXml.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+([-.][A-Za-z0-9.]+)?$') { throw "Invalid version: $version" }
$localDotnet = Join-Path $repoRoot ".dotnet\dotnet.exe"
$dotnet = if (Test-Path -LiteralPath $localDotnet) { $localDotnet } else { (Get-Command dotnet -ErrorAction Stop).Source }
$env:DOTNET_CLI_HOME = Join-Path $repoRoot ".dotnet-home"
$staging = Join-Path $repoRoot ("artifacts\publish-staging-" + [guid]::NewGuid().ToString("N"))
$output = Join-Path $repoRoot "artifacts\SureType-$version-win-x64"
$buildOutput = Join-Path $repoRoot "artifacts\build\"
& $dotnet publish $project -c $Configuration -r win-x64 --self-contained true --no-restore `
    -p:BaseOutputPath=$buildOutput -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true `
    -p:PublishReadyToRun=false -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=none -p:DebugSymbols=false -o $staging
if ($LASTEXITCODE -ne 0) { throw "Publish failed ($LASTEXITCODE). Existing packages were not changed." }
$stagedExe = Join-Path $staging "SureType.exe"
if (!(Test-Path -LiteralPath $stagedExe) -or (Get-Item -LiteralPath $stagedExe).Length -eq 0) {
    throw "Publish did not produce a non-empty executable."
}
$informationVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($stagedExe).ProductVersion
if (!$informationVersion.StartsWith($version)) { throw "Unexpected product version: $informationVersion" }
New-Item -ItemType Directory -Force -Path $output | Out-Null
Copy-Item -LiteralPath $stagedExe -Destination (Join-Path $output "SureType.exe") -Force
Copy-Item -LiteralPath (Join-Path $repoRoot "README.md") -Destination (Join-Path $output "README.md") -Force
$hash = (Get-FileHash -LiteralPath (Join-Path $output "SureType.exe") -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $output "SHA256SUMS.txt"), "$hash  SureType.exe" + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
$packageDocs = Join-Path $output "docs"
New-Item -ItemType Directory -Force -Path $packageDocs | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot "docs\validation.md") -Destination (Join-Path $packageDocs "validation.md") -Force
$zip = Join-Path $repoRoot "artifacts\SureType-$version-win-x64.zip"
Compress-Archive -LiteralPath (Join-Path $output "SureType.exe"),(Join-Path $output "README.md"),(Join-Path $output "SHA256SUMS.txt"),$packageDocs -DestinationPath $zip -Force
$zipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText("$zip.sha256", "$zipHash  $([IO.Path]::GetFileName($zip))" + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
Write-Host "Published candidate: $output"
Write-Host "Archive: $zip"
Write-Host "SHA256 (exe): $hash"
Write-Host "Run packaged lifecycle checks before updating download/SureType.exe."
