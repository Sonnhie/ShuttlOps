$lines = [System.Collections.ArrayList]@(Get-Content 'Services/MainServices/GAServices.cs')

# Remove old if block (lines 267-272, 0-indexed: 266-271)
$lines.RemoveRange(266, 6)

$newLines = @(
    '                // Block if driver is In Transit'
    '                if (driver.Status == "In Transit")'
    '                    return (false, $"Driver {driver.DriverName} is currently In Transit and cannot be assigned.");'
    ''
    '                // Block if vehicle is In Transit'
    '                if (vehicle.Status == "In Transit")'
    '                    return (false, $"Vehicle {vehicle.PlateNumber} is currently In Transit and cannot be assigned.");'
    ''
    '                // Prevent duplicate assignment on the same ticket'
    '                var existingDispatch = await dbContext.DispatchDetails'
    '                    .FirstOrDefaultAsync(d => d.TicketId == Dispatch.TicketId);'
    ''
    '                if (existingDispatch != null)'
    '                    return (false, "This ticket already has an assigned driver and vehicle.");'
    ''
    '                // Time-conflict: same driver on same date with overlapping time window'
    '                var driverConflict = await dbContext.DispatchDetails'
    '                    .Include(d => d.Ticket)'
    '                    .Where(d => d.DriverId == Dispatch.DriverId'
    '                             && d.TicketId != Dispatch.TicketId'
    '                             && d.Ticket.DateOfTrip == isTicketExist.DateOfTrip'
    '                             && d.Ticket.ApprovalStatus != "Completed"'
    '                             && d.Ticket.ApprovalStatus != "Cancelled"'
    '                             && d.Ticket.ApprovalStatus != "Denied"'
    '                             && d.Ticket.EstDepartureTime < isTicketExist.EstArrivalTime'
    '                             && d.Ticket.EstArrivalTime > isTicketExist.EstDepartureTime)'
    '                    .FirstOrDefaultAsync();'
    ''
    '                if (driverConflict != null)'
    '                    return (false, "Driver " + $driver.DriverName + " is already assigned to ticket " + $driverConflict.Ticket.TicketNumber + " on " + $isTicketExist.DateOfTrip + " (Time: " + $driverConflict.Ticket.EstDepartureTime.ToString("hh\:mm") + " - " + $driverConflict.Ticket.EstArrivalTime.ToString("hh\:mm") + ").");'
    ''
    '                // Time-conflict: same vehicle on same date with overlapping time window'
    '                var vehicleConflict = await dbContext.DispatchDetails'
    '                    .Include(d => d.Ticket)'
    '                    .Where(d => d.VehicleId == Dispatch.VehicleId'
    '                             && d.TicketId != Dispatch.TicketId'
    '                             && d.Ticket.DateOfTrip == isTicketExist.DateOfTrip'
    '                             && d.Ticket.ApprovalStatus != "Completed"'
    '                             && d.Ticket.ApprovalStatus != "Cancelled"'
    '                             && d.Ticket.ApprovalStatus != "Denied"'
    '                             && d.Ticket.EstDepartureTime < isTicketExist.EstArrivalTime'
    '                             && d.Ticket.EstArrivalTime > isTicketExist.EstDepartureTime)'
    '                    .FirstOrDefaultAsync();'
    ''
    '                if (vehicleConflict != null)'
    '                    return (false, "Vehicle " + $vehicle.PlateNumber + " is already assigned to ticket " + $vehicleConflict.Ticket.TicketNumber + " on " + $isTicketExist.DateOfTrip + " (Time: " + $vehicleConflict.Ticket.EstDepartureTime.ToString("hh\:mm") + " - " + $vehicleConflict.Ticket.EstArrivalTime.ToString("hh\:mm") + ").");'
    ''
    '                // All checks passed'
    '                driver.Status = "Assigned";'
    '                vehicle.Status = "Assigned";'
    ''
)

for ($i = 0; $i -lt $newLines.Count; $i++) {
    $lines.Insert(266 + $i, $newLines[$i])
}

$lines -join [Environment]::NewLine | Set-Content 'Services/MainServices/GAServices.cs' -NoNewline
Write-Output "SUCCESS: GAServices.cs updated with $($lines.Count) lines"
