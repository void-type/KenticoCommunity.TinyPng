$originalLocation = Get-Location
$projectRoot = "$PSScriptRoot/../"

try {
  Set-Location -Path $projectRoot
  . ./build/buildSettings.ps1

  ./build/updateTools.ps1
  dotnet outdated -u --exclude "Kentico.Xperience.Libraries"

  if ($LASTEXITCODE -ne 0) {
    Write-Warning "You need to update the Directory.build.props files by hand to update the following packages."
    dotnet outdated --exclude "Kentico.Xperience.Libraries"
  }

} finally {
  Set-Location -Path $originalLocation
}
