Import-Module Pester
$version =

Write-Warning "Pester Version: $(Get-Module Pester | Select-Object -ExpandProperty Version)"
Write-Warning "Pester Path: $(Get-Module Pester | Select-Object -ExpandProperty Path)"

[PesterConfiguration] $configuration = New-PesterConfiguration
$configuration.Run.Path = "${PSScriptRoot}\..\tests"
$configuration.Run.Exit = $true
$configuration.Output.CIFormat = 'GithubActions'
$psEditor = 'fuck'
Invoke-Pester -Configuration $configuration
