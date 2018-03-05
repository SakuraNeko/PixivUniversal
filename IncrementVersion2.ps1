param([string]$workingDirectory = $PSScriptRoot)
Write-Host 'Executing Powershell script IncrementVersion.ps1 with working directory set to: ' $workingDirectory
Set-Location $workingDirectory

$inputFileName = 'Package.appxmanifest'
$outputFileName = $PSScriptRoot + '/Package.appxmanifest';

$versionRevision = 0;

$content = (gc $inputFileName) -join "`r`n"

$callback = {
  param($match)
    [string]$versionMajor = $match.Groups[2].Value
    $match.Groups[1].Value + 'Version="' + $appveyor_build_version + '.' + $versionRevision + '"'
}

$identityRegex = [regex]'(\<Identity[^\>]*)Version=\"([0-9])+\.([0-9]+)\.([0-9]+)\.([0-9]+)\.*\"'
$newContent = $identityRegex.Replace($content, $callback)

[io.file]::WriteAllText($outputFileName, $newContent)