[CmdletBinding()]
param(
 [Parameter(Mandatory)][string]$Repository,
 [Parameter(Mandatory)][string]$Tag,
 [string]$OutputDirectory = "dist",
 [ValidateSet('Release','Beta')][string]$Channel = 'Release'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($Repository -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') { throw 'Invalid GitHub repository.' }
if ($Tag -notmatch '^v(\d+)\.(\d+)\.(\d+)\.(\d+)$') { throw 'Version tag must be vMAJOR.MINOR.PATCH.BUILD.' }
$version = $Tag.Substring(1)
$project = 'src/NINA.Plugin.ShutdownButton/NINA.Plugin.ShutdownButton.csproj'
& dotnet restore 'tests/NINA.Plugin.ShutdownButton.Tests/NINA.Plugin.ShutdownButton.Tests.csproj'
if ($LASTEXITCODE) { throw 'dotnet restore failed' }
& dotnet build $project -c Release --no-restore
if ($LASTEXITCODE) { throw 'dotnet build failed' }
& dotnet test 'tests/NINA.Plugin.ShutdownButton.Tests/NINA.Plugin.ShutdownButton.Tests.csproj' -c Release --no-restore
if ($LASTEXITCODE) { throw 'dotnet test failed' }
$source = 'src/NINA.Plugin.ShutdownButton/bin/Release/net8.0-windows'
$dll = Join-Path $source 'NINA.Plugin.ShutdownButton.dll'
if (!(Test-Path $dll)) { throw 'Plugin DLL missing after build' }
$actualVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo((Resolve-Path $dll).Path).FileVersion
if ($version -ne $actualVersion) { throw "Tag $version does not match DLL file version $actualVersion" }
$archive = "NINA.Plugin.ShutdownButton.$version.zip"
$manifestName = "NINA.Plugin.ShutdownButton.$version.manifest.json"
New-Item -Force -ItemType Directory $OutputDirectory | Out-Null
$fullOut = (Resolve-Path $OutputDirectory).Path
$zipPath = Join-Path $fullOut $archive
if (Test-Path $zipPath) { Remove-Item $zipPath }
Compress-Archive -LiteralPath $dll -DestinationPath $zipPath
$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
$v = $version.Split('.')
$manifest = [ordered]@{
 Name = 'Shutdown Button'
 Identifier = '87a0ca7c-d8e6-4f47-b718-c70270f86adb'
 Version = [ordered]@{ Major=$v[0]; Minor=$v[1]; Patch=$v[2]; Build=$v[3] }
 Author = 'grm'
 Homepage = "https://github.com/$Repository"
 Repository = "https://github.com/$Repository"
 License = 'MIT'
 LicenseURL = 'https://opensource.org/license/mit'
 ChangelogURL = "https://github.com/$Repository/releases/tag/$Tag"
 Tags = @('Shutdown','Sequencer','Telescope','REST','Automation')
 MinimumApplicationVersion = [ordered]@{ Major='3'; Minor='2'; Patch='0'; Build='9001' }
 Descriptions = [ordered]@{
  ShortDescription = 'Manual, confirmed shutdown of N.I.N.A. sequencer and telescope equipment.'
  LongDescription = 'Dockable Imaging shutdown button: stop the active sequence, stop guiding, verify mount parking, warm the camera, disconnect chosen equipment and optionally control selected REST power outlets. REST is disabled and dry-run by default. Configure and test carefully with simulators before real equipment.'
  FeaturedImageURL = ''
  ScreenshotURL = ''
  AltScreenshotURL = ''
 }
 Installer = [ordered]@{
  URL = "https://github.com/$Repository/releases/download/$Tag/$archive"
  Type = 'ARCHIVE'
  Checksum = $hash
  ChecksumType = 'SHA256'
 }
}
if ($Channel -eq 'Beta') { $manifest.Channel = 'Beta' }
$json = $manifest | ConvertTo-Json -Depth 12
$manifestPath = Join-Path $fullOut $manifestName
[IO.File]::WriteAllText($manifestPath,$json,[Text.UTF8Encoding]::new($false))
Write-Host "Prepared $zipPath"
Write-Host "Prepared $manifestPath"
