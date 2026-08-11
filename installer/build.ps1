# Builds the self-contained app and packages it into installer-output\LifeBucketList.msi.
# Requires: .NET SDK, and the WiX CLI (`dotnet tool install --global wix --version 5.0.2`
# plus `wix extension add WixToolset.UI.wixext/5.0.2`, both one-time setup steps).

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $repoRoot "publish"
$outputDir = Join-Path $repoRoot "installer-output"

Write-Host "Cleaning previous publish/installer output..."
Remove-Item -Recurse -Force $publishDir -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force $outputDir -ErrorAction SilentlyContinue

Write-Host "Publishing self-contained app..."
dotnet publish (Join-Path $repoRoot "src\LifeBucketList.App") `
    -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

Write-Host "Building MSI..."
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
wix build (Join-Path $PSScriptRoot "Product.wxs") `
    -d "PublishDir=$publishDir" `
    -arch x64 `
    -ext WixToolset.UI.wixext `
    -bindpath $PSScriptRoot `
    -o (Join-Path $outputDir "LifeBucketList.msi")
if ($LASTEXITCODE -ne 0) { throw "wix build failed" }

Write-Host "Done: $outputDir\LifeBucketList.msi"
