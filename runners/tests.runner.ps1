Import-Module Pester

[PesterConfiguration] $configuration = New-PesterConfiguration
$configuration.Run.Path = "${PSScriptRoot}\..\tests"
$configuration.Run.Exit = $true
$configuration.Output.CIFormat = 'GitHubActions'

Invoke-Pester -Configuration $configuration
