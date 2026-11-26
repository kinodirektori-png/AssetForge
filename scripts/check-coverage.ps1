# Fails when line coverage in the newest Cobertura report is below the threshold (claude.md: at least 80 %).
# PowerShell version of check-coverage.py, for Windows machines without Python.
param([double]$Threshold = 80)

$reports = Get-ChildItem -Path TestResults -Recurse -Filter coverage.cobertura.xml -ErrorAction SilentlyContinue
if (-not $reports) { Write-Error "No coverage report found under TestResults/."; exit 1 }

$newest = $reports | Sort-Object LastWriteTime -Descending | Select-Object -First 1
[xml]$xml = Get-Content $newest.FullName
$covered = [int]$xml.coverage.'lines-covered'
$valid = [int]$xml.coverage.'lines-valid'
$rate = if ($valid -gt 0) { 100.0 * $covered / $valid } else { 0 }

Write-Output "Report: $($newest.FullName)"
foreach ($package in $xml.coverage.packages.package) {
    Write-Output ("  {0}: {1:N1} %" -f $package.name, (100 * [double]$package.'line-rate'))
}
Write-Output ("Line coverage: {0:N1} % ({1}/{2}), threshold {3} %" -f $rate, $covered, $valid, $Threshold)
if ($rate -lt $Threshold) { Write-Error ("Line coverage {0:N1} % is below {1} %." -f $rate, $Threshold); exit 1 }
