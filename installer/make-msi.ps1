<#
.SYNOPSIS
  boo-win (ButlerDeck) を Unpackaged で publish し、MSI インストーラーを作成する。

.DESCRIPTION
  - -p:BooUnpackaged=true を指定して publish する (csproj 内で WindowsPackageType=None に切り替わる)。
	指定しない通常ビルド (Visual Studio からの MSIX 作成 = Packaged 構成) は従来どおり利用可能。
  - MSI 作成には WiX Toolset v5 (dotnet tool "wix") を使用する。未インストールなら自動でインストールする。
  - 出力: installer\out\ButlerDeck-<Version>-<Arch>.msi

.EXAMPLE
  .\installer\build-msi.ps1
  .\installer\build-msi.ps1 -Arch arm64
#>
param(
	[ValidateSet('x64', 'x86', 'arm64')]
	[string]$Arch = 'x64',
	[string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$root       = Split-Path -Parent $PSScriptRoot
$project    = Join-Path $root 'boo-win\boo-win.csproj'
$wxs        = Join-Path $PSScriptRoot 'ButlerDeck.wxs'
$outDir     = Join-Path $PSScriptRoot 'out'
$platform   = if ($Arch -eq 'arm64') { 'ARM64' } else { $Arch }
$publishDir = Join-Path $outDir "publish\win-$Arch"

# --- バージョン取得 (csproj の <Version>) ---
[xml]$projXml = Get-Content $project -Raw
$version = $projXml.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ } | Select-Object -First 1
if (-not $version) { throw "<Version> が $project に見つかりません。" }
Write-Host "Version: $version / Arch: $Arch" -ForegroundColor Cyan

# --- WiX の準備 ---
if (-not (Get-Command wix -ErrorAction SilentlyContinue)) {
	$env:PATH += ";$env:USERPROFILE\.dotnet\tools"
}
if (-not (Get-Command wix -ErrorAction SilentlyContinue)) {
	Write-Host 'WiX Toolset (dotnet tool) をインストールします...' -ForegroundColor Cyan
	dotnet tool install --global wix --version 5.*
	if ($LASTEXITCODE -ne 0) { throw 'wix のインストールに失敗しました。' }
}

# --- Unpackaged publish ---
# RuntimeIdentifier / SelfContained は PublishProfile (win-<Platform>.pubxml) で指定される。
# (-r などでグローバル指定すると参照ライブラリにも伝播してエラーになるため指定しない)
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
Write-Host 'Unpackaged publish...' -ForegroundColor Cyan
dotnet publish $project `
	-c $Configuration `
	-o $publishDir `
	-p:Platform=$platform `
	-p:BooUnpackaged=true `
	-p:GenerateAppxPackageOnBuild=false
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish に失敗しました。' }

# --- MSI ビルド ---
$msi = Join-Path $outDir "ButlerDeck-$version-$Arch.msi"
Write-Host 'MSI build...' -ForegroundColor Cyan
wix build $wxs `
	-arch $Arch `
	-d "Version=$version" `
	-d "PublishDir=$publishDir\" `
	-o $msi
if ($LASTEXITCODE -ne 0) { throw 'wix build に失敗しました。' }

Write-Host "完了: $msi" -ForegroundColor Green
