const LoadTripSchedule = () => {
    return createDataTable(
        "#TripScheduleTable",
        {
            url: "/Request/GetAllRequests",
            searchPlaceholder: "Search Ticket number...",
            order: [[2, 'desc']],
            columns: [
                TableColumnsConfig.Text("TicketNumber"),
                TableColumnsConfig.Status("ApprovalStatus"),
                TableColumnsConfig.Date("TripDate"),
                TableColumnsConfig.Time("DepartureTime"),
                TableColumnsConfig.Text("PickupLocation"),
                TableColumnsConfig.Text("DropLocation"),
                TableColumnsConfig.Text("Purpose"),
                TableColumnsConfig.Actions(function (data, type, row) {
                    return `
                    <div class="dropdown">
                        <button class="btn btn-sm btn-light border-0" type="button" data-bs-toggle="dropdown" aria-expanded="false" title="Actions">
                            <i class="bi bi-three-dots-vertical"></i>
                        </button>
                        <ul class="dropdown-menu dropdown-menu-end shadow-sm border-0">
                            <li>
                                <a class="dropdown-item btn-view-ticket" href="#" data-id="${row.id || row.TicketNumber}">
                                    <i class="bi bi-eye text-primary me-2"></i> View Details
                                </a>
                            </li>
                            <li><hr class="dropdown-divider"></li>
                            <li>
                                <a class="dropdown-item text-danger btn-delete-ticket" href="#" data-id="${row.id || row.TicketNumber}">
                                    <i class="bi bi-trash text-danger me-2"></i> Delete
                                </a>
                            </li>
                        </ul>
                    </div>
                `;
                })
            ]
        }
    )
}


$.fn.dataTable.ext.search.push(function (settings, data, dataIndex, rowData) {
    if (settings.nTable.id !== 'TripScheduleTable') {
        return true;
    }

    const selectedStatus = $('#statusFilter').val()?.toLowerCase().trim();
    if (!selectedStatus) {
        return true; 
    }
    let rowStatus = (rowData.ApprovalStatus || data[1] || '').toString().toLowerCase().trim();
    return rowStatus === selectedStatus;
});


$(function () {
    const table = LoadTripSchedule();
    $('#statusFilter').on('change', function () {
        table.draw();
    });
});