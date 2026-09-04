$content = Get-Content 'Services/MainServices/RequestService.cs' -Raw
$nl = [Environment]::NewLine
$search = '                d.VehicleStatus = "In Transit";' + $nl + '            }'
$replace = '                d.VehicleStatus = "In Transit";' + $nl + '                d.Ticket.ApprovalStatus = "On Trip";' + $nl + '            }'
$content = $content.Replace($search, $replace)
$has = $content.Contains('d.Ticket.ApprovalStatus')
Write-Host ('Has On Trip transition: ' + $has)
Set-Content 'Services/MainServices/RequestService.cs' $content -NoNewline
Write-Host 'Done'
