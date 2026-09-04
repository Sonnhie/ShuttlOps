$content = [System.IO.File]::ReadAllText('C:\Users\00723\Desktop\Files\WebProjects\STMS\ShuttlOps\Services\MainServices\RequestService.cs')

# Find ProcessApproval notification block - use unique markers
$marker1 = 'var notifyTitle = (status == "Approved" || status == "GA Approved") ? "Trip Approved" : "Trip " + status;'
$idx1 = $content.IndexOf($marker1)
if ($idx1 -lt 0) { Write-Host "MARKER1 NOT FOUND"; exit 1 }

# Find the start of the if block (go backwards to find "if (notificationService != null)")
$searchStart = $idx1 - 200
$blockStart = $content.LastIndexOf('if (notificationService != null)', $idx1, 200)
if ($blockStart -lt 0) { Write-Host "BLOCK START NOT FOUND"; exit 1 }

# Find the end - look for "return (true, `"$`" after the block
$afterBlock = $content.IndexOf('return (true, $"Trip ticket successfully updated', $idx1)
if ($afterBlock -lt 0) { $afterBlock = $content.IndexOf('return (true, $"Trip ticket successfully updated', $idx1) }
if ($afterBlock -lt 0) { Write-Host "BLOCK END NOT FOUND"; exit 1 }

# Find the closing of the if notificationService block - count braces
$braceCount = 0
$blockEnd = -1
for ($i = $blockStart; $i -lt $content.Length -and $i -lt $afterBlock + 200; $i++) {
    if ($content[$i] -eq '{') { $braceCount++ }
    if ($content[$i] -eq '}') { 
        $braceCount--
        if ($braceCount -eq 0) { 
            $blockEnd = $i + 1
            break 
        }
    }
}

Write-Host "BlockStart: $blockStart, BlockEnd: $blockEnd, Idx1: $idx1"
Write-Host "Current block content (first 100 chars): $($content.Substring($blockStart, [Math]::Min(100, $blockEnd - $blockStart)))"

# Extract ticket number variable reference
$oldBlock = $content.Substring($blockStart, $blockEnd - $blockStart)

# New block - ProcessApproval notifications per AGENTS.md
$newBlock = @"
                if (notificationService != null)
                {
                    var approverRole = userinfo?.Role?.RoleName ?? "";
                    var dept = isreqExist.RequestedDepartment ?? "";
                    var ticketNum = isreqExist.TicketNumber ?? "";

                    if (status == "Cancelled" || status == "Denied")
                    {
                        if (!string.IsNullOrEmpty(isreqExist.RequestorId))
                        {
                            await notificationService.SendToUserAsync(isreqExist.RequestorId, new NotificationMessageDTO
                            {
                                Title = `"Trip ${status}`",
                                Message = `"Your ticket ${ticketNum} has been ${status.ToLower()} by ${userinfo?.EmployeeName ?? "Unknown"}.`",
                                Category = "Approval",
                                TicketNumber = ticketNum,
                                Url = "/User/TripSchedule"
                            });
                        }
                        if (status == "Cancelled")
                        {
                            await notificationService.SendToRoleAsync("GA", new NotificationMessageDTO
                            {
                                Title = "Trip Cancelled",
                                Message = `"Ticket ${ticketNum} has been cancelled by ${userinfo?.EmployeeName ?? "Unknown"}.`",
                                Category = "Approval",
                                TicketNumber = ticketNum,
                                Url = "/User/TripSchedule"
                            });
                            await notificationService.SendToRoleInDepartmentAsync("Section Approver", dept, new NotificationMessageDTO
                            {
                                Title = `"Trip ${status}`",
                                Message = `"Ticket ${ticketNum} has been ${status.ToLower()} by ${userinfo?.EmployeeName ?? "Unknown"}.`",
                                Category = "Approval",
                                TicketNumber = ticketNum,
                                Url = "/User/TripSchedule"
                            });
                        }
                    }
                    else if (approverRole == "Section Approver" || approverRole == "Requestor")
                    {
                        await notificationService.SendToRoleAsync("GA", new NotificationMessageDTO
                        {
                            Title = "Section Head Approved",
                            Message = `"Ticket ${ticketNum} approved by Section Head ${userinfo?.EmployeeName ?? "Unknown"}.`",
                            Category = "Approval",
                            TicketNumber = ticketNum,
                            Url = "/User/TripSchedule"
                        });
                        if (!string.IsNullOrEmpty(isreqExist.RequestorId))
                        {
                            await notificationService.SendToUserAsync(isreqExist.RequestorId, new NotificationMessageDTO
                            {
                                Title = "Section Head Approved",
                                Message = `"Your ticket ${ticketNum} has been approved by your Section Head.`",
                                Category = "Approval",
                                TicketNumber = ticketNum,
                                Url = "/User/TripSchedule"
                            });
                        }
                    }
                    else if (approverRole == "GA")
                    {
                        if (!string.IsNullOrEmpty(isreqExist.RequestorId))
                        {
                            await notificationService.SendToUserAsync(isreqExist.RequestorId, new NotificationMessageDTO
                            {
                                Title = "GA Approved",
                                Message = `"Your ticket ${ticketNum} has been approved by GA ${userinfo?.EmployeeName ?? "Unknown"}.`",
                                Category = "Approval",
                                TicketNumber = ticketNum,
                                Url = "/User/TripSchedule"
                            });
                        }
                        await notificationService.SendToRoleInDepartmentAsync("Section Approver", dept, new NotificationMessageDTO
                        {
                            Title = "GA Approved",
                            Message = `"Ticket ${ticketNum} has been approved by GA ${userinfo?.EmployeeName ?? "Unknown"}.`",
                            Category = "Approval",
                            TicketNumber = ticketNum,
                            Url = "/User/TripSchedule"
                        });
                        await notificationService.SendToRoleAsync("Security", new NotificationMessageDTO
                        {
                            Title = "GA Approved Trip",
                            Message = `"Ticket ${ticketNum} has been approved by GA and is ready for dispatch.`",
                            Category = "Approval",
                            TicketNumber = ticketNum,
                            Url = "/Security/SecurityLogs"
                        });
                    }
                }
"@

$content = $content.Remove($blockStart, $blockEnd - $blockStart).Insert($blockStart, $newBlock)
[System.IO.File]::WriteAllText('C:\Users\00723\Desktop\Files\WebProjects\STMS\ShuttlOps\Services\MainServices\RequestService.cs', $content)
Write-Host "ProcessApproval notifications updated."