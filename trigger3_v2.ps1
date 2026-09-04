$content = Get-Content 'Services/MainServices/RequestService.cs' -Raw
$nl = [Environment]::NewLine
$search = '                isreqExist.ApprovalStatus = status;' + $nl + '                await dbContext.SaveChangesAsync();'
$r = @()
$r += '                isreqExist.ApprovalStatus = status;'
$r += ''
$r += '                if (status == "Denied" || status == "Cancelled")'
$r += '                {'
$r += '                    var dispatch = await dbContext.DispatchDetails'
$r += '                        .Include(d => d.Driver)'
$r += '                        .Include(d => d.Vehicle)'
$r += '                        .FirstOrDefaultAsync(d => d.TicketId == id);'
$r += ''
$r += '                    if (dispatch != null)'
$r += '                    {'
$r += '                        if (dispatch.Driver != null)'
$r += '                        {'
$r += '                            dispatch.Driver.Status = "Active";'
$r += '                        }'
$r += '                        if (dispatch.Vehicle != null)'
$r += '                        {'
$r += '                            dispatch.Vehicle.Status = "Available";'
$r += '                        }'
$r += '                        dispatch.VehicleStatus = "Released";'
$r += '                    }'
$r += '                }'
$r += ''
$r += '                await dbContext.SaveChangesAsync();'
$replace = $r -join $nl
$content = $content.Replace($search, $replace)
Write-Host ('Has: ' + $content.Contains('dispatch.Driver.Status'))
Set-Content 'Services/MainServices/RequestService.cs' $content -NoNewline
Write-Host 'Done'
