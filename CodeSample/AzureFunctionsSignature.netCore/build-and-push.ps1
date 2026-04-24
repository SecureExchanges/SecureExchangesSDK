# build-and-push.ps1
# Builds the Docker image via Azure Container Registry Tasks (cloud-side) and pushes it to ACR.
#
# Prerequisites:
#   - Azure CLI installed + az login
#   - Existing Azure Container Registry
#
# Does NOT require local Docker Desktop — ACR Tasks builds server-side.
#
# Usage:
#   pwsh .\build-and-push.ps1 `
#       -AcrName "AzureFunctionsContainer"
#
# With a specific image name / tag:
#   pwsh .\build-and-push.ps1 `
#       -AcrName "AzureFunctionsContainer" `
#       -ImageName "signature" `
#       -ImageTag "v42"

param(
    [Parameter(Mandatory)][string]$AcrName,
    [Parameter(Mandatory)][string]$ImageName,
    [string]$ImageTag = "v$(Get-Date -Format 'yyyyMMdd-HHmm')",
    [switch]$SkipLatestTag,
    [string]$DockerfilePath = "Dockerfile"
)

$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot

$taggedImage = "${ImageName}:${ImageTag}"
$latestImage = "${ImageName}:latest"
$fullImage   = "${AcrName}.azurecr.io/${taggedImage}"

Write-Host "=== Configuration ===" -ForegroundColor Cyan
Write-Host "ACR         : $AcrName"
Write-Host "Image name  : $ImageName"
Write-Host "Tag         : $ImageTag"
Write-Host "Dockerfile  : $DockerfilePath"
Write-Host "Full image  : $fullImage"
if (-not $SkipLatestTag) {
    Write-Host "Also tagged : $latestImage"
}
Write-Host ""

# ---------------------------------------------------------------
# Build + push via ACR Tasks
# ---------------------------------------------------------------
Write-Host "`n=== Building and pushing image ===" -ForegroundColor Cyan

$acrBuildArgs = @(
    '--registry', $AcrName,
    '--image', $taggedImage,
    '--file', $DockerfilePath
)

if (-not $SkipLatestTag) {
    $acrBuildArgs += @('--image', $latestImage)
}

# Pass the build context (current dir) as the last positional arg.
$acrBuildArgs += '.'

az acr build @acrBuildArgs
if ($LASTEXITCODE -ne 0) { throw "az acr build failed" }

# ---------------------------------------------------------------
# Done
# ---------------------------------------------------------------
Write-Host "`n=== Done ===" -ForegroundColor Green
Write-Host "Image pushed : $fullImage"
if (-not $SkipLatestTag) {
    Write-Host "Also tagged  : ${AcrName}.azurecr.io/${latestImage}"
}

Pop-Location