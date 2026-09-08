param([string]$B64, [string]$Target)
$bytes = [System.Convert]::FromBase64String($B64)
$text = [System.Text.Encoding]::UTF8.GetString($bytes)
[System.IO.File]::WriteAllText($Target, $text, [System.Text.Encoding]::UTF8)
Write-Host "Written successfully"
