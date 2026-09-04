$content = [System.IO.File]::ReadAllText("Services/MainServices/RequestService.cs")

$nl = [char]13 + [char]10
$searchBlock = "                isreqExist.ApprovalStatus = status;" + $nl + "                await dbContext.SaveChangesAsync();"

$replaceLines = @()
$replaceLines += "                isreqExist.ApprovalStatus = status;"
$replaceLines += ""
$replaceLines += "                // Release assigned driver/vehicle when trip is denied or cancelled"
$replaceLines += "                if (status == \"Denied\" || status == \"Cancelled\")"
$replaceLines += "                {"
$replaceLines += "                    var dispatch = await dbContext.DispatchDetails"
$replaceLines += "                        .Include(d => d.Driver)"
$replaceLines += "                        .Include(d => d.Vehicle)"
$replaceLines += "                        .FirstOrDefaultAsync(d => d.TicketId == id);"
$replaceLines += ""
$replaceLines += "                    if (dispatch != null)"
$replaceLines += "                    {"
$replaceLines += "                        if (dispatch.Driver != null)"
$replaceLines += "                        {"
$replaceLines += "                            dispatch.Driver.Status = \"Active\";"
$replaceLines += "                        }"
$replaceLines += "                        if (dispatch.Vehicle != null)"
$replaceLines += "                        {"
$replaceLines += "                            dispatch.Vehicle.Status = \"Available\";"
$replaceLines += "                        }"
$replaceLines += "                        dispatch.VehicleStatus = \"Released\";"
$replaceLines += "                    }"
$replaceLines += "                }"
$replaceLines += ""
$replaceLines += "                await dbContext.SaveChangesAsync();"

$replaceBlock = $replaceLines -join $nl

$content = $content.Replace($searchBlock, $replaceBlock)
$has = $content.Contains("dispatch.Driver.Status")
Write-Host "Has trigger3: $has"
[System.IO.File]::WriteAllText("Services/MainServices/RequestService.cs", $content)
Write-Host "Done"
