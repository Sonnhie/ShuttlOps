param([string]$FilePath, [string]$OldB64, [string]$NewB64)
$old = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($OldB64))
$new = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($NewB64))
$c = [System.IO.File]::ReadAllText($FilePath, [System.Text.Encoding]::UTF8)
$c = $c.Replace($old, $new)
[System.IO.File]::WriteAllText($FilePath, $c, [System.Text.Encoding]::UTF8)
Write-Host "Replacement done"