param (
    [string]$Version = "1.1.0"
)

$ErrorActionPreference = "Stop"

Write-Host "`nStarting Release Automation: v$Version..." -ForegroundColor Cyan

# 1. Build WPF App
Write-Host "`n1. Compiling self-contained C# WPF app in Release mode..." -ForegroundColor Yellow
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true
if ($LASTEXITCODE -ne 0) {
    Write-Error "WPF App Compilation Failed."
    exit $LASTEXITCODE
}

# 2. Get hash
$exePath = "bin\Release\net10.0-windows\win-x64\publish\ApexDebloater.exe"
if (-not (Test-Path $exePath)) {
    Write-Error "Executable not found at $exePath"
    exit 1
}
Write-Host "`n2. Calculating SHA-256 hash..." -ForegroundColor Yellow
$hashResult = Get-FileHash -Algorithm SHA256 $exePath
$hash = $hashResult.Hash.ToLower()
Write-Host "Checksum: $hash" -ForegroundColor Green

# 3. Update Astro page
Write-Host "`n3. Updating website/src/pages/index.astro with version & hash..." -ForegroundColor Yellow
$astroPath = "website\src\pages\index.astro"
if (Test-Path $astroPath) {
    $content = Get-Content $astroPath -Raw
    
    # Replace version in download header
    $content = $content -replace '<h2>v\d+\.\d+\.\d+</h2>', "<h2>v$Version</h2>"
    # Replace version in download link path
    $content = $content -replace '/download/v\d+\.\d+\.\d+/', "/download/v$Version/"
    # Replace SHA-256 hash
    $content = $content -replace '<code>[a-f0-9]{64}</code>', "<code>$hash</code>"
    
    Set-Content $astroPath -Value $content -NoNewline
    Write-Host "index.astro updated successfully." -ForegroundColor Green
} else {
    Write-Warning "index.astro not found; skipping update."
}

# 4. Build Astro website
Write-Host "`n4. Rebuilding Astro static assets..." -ForegroundColor Yellow
Push-Location website
npm run build
if ($LASTEXITCODE -ne 0) {
    Write-Error "Astro Build Failed."
    Pop-Location
    exit $LASTEXITCODE
}
Pop-Location

# 5. Commit and Push to Git
Write-Host "`n5. Committing and pushing source updates to GitHub..." -ForegroundColor Yellow
git add .
git commit -m "release: v$Version updates"
git push origin main

# 6. Create Git tag and Release
Write-Host "`n6. Creating and pushing tag v$Version..." -ForegroundColor Yellow
git tag "v$Version"
git push origin "v$Version"

Write-Host "`n7. Creating GitHub Release and uploading compiled binary..." -ForegroundColor Yellow
# Unset GITHUB_TOKEN overrides to use keyring credentials
if ($env:GITHUB_TOKEN) {
    Remove-Item env:GITHUB_TOKEN
}
gh release create "v$Version" $exePath --title "v$Version" --notes "Automated release version $Version"

Write-Host "`nRelease v$Version successfully built, tagged, and published to GitHub!" -ForegroundColor Green
