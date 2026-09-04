$content = Get-Content 'Services/MainServices/RequestService.cs' -Raw
$nl = [Environment]::NewLine
$search = '                d.VehicleStatus = "In Transit";' + $nl + '            }'
$replace = '                d.VehicleStatus = "In Transit";' + $nl + '                d.Ticket.ApprovalStatus = "On Trip";' + $nl + '            }'
$content = $content.Replace($search, $replace)
Set-Content 'Services/MainServices/RequestService.tmp.cs' $content -NoNewline
Write-Host 'Written to temp'
