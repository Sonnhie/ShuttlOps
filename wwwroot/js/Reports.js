/**
 * ShuttlOps - Transportation Reports Module
 */
(() => {
    let dataTableInstance = null;
    let rawRecords = [];

    const valueOf = (record, name) => record?.[name] ?? record?.[name.charAt(0).toLowerCase() + name.slice(1)];

    function initReportsTable() {
        dataTableInstance = createDataTable('#ReportsTable', {
            url: '/Request/GetAllRequests',
            searchPlaceholder: 'Search reports…',
            order: [[1, 'desc']],
            columns: [
                TableColumnsConfig.Text('TicketNumber'),
                TableColumnsConfig.Date('TripDate'),
                TableColumnsConfig.Time('DepartureTime'),
                TableColumnsConfig.Text('Requestor'),
                TableColumnsConfig.Text('RequestDepartment'),
                TableColumnsConfig.Text('PickupLocation'),
                TableColumnsConfig.Text('DropLocation'),
                TableColumnsConfig.Passenger('Passengers'),
                TableColumnsConfig.Text('DriverName'),
                TableColumnsConfig.Status('ApprovalStatus')
            ]
        });

        // Custom filtering logic for DataTables
        $.fn.dataTable.ext.search.push(function (settings, data, dataIndex) {
            if (settings.sTableId !== 'ReportsTable') return true;

            const row = rawRecords[dataIndex];
            if (!row) return true;

            const startDateStr = $('#reportDateStart').val();
            const endDateStr = $('#reportDateEnd').val();
            const selectedDept = $('#reportDepartmentFilter').length ? $('#reportDepartmentFilter').val() : '';
            const selectedStatus = $('#reportStatusFilter').val();

            const rowDate = valueOf(row, 'TripDate');
            const rowDept = valueOf(row, 'RequestDepartment') || '';
            const rowStatus = valueOf(row, 'ApprovalStatus') || '';

            if (startDateStr) {
                if (!rowDate || rowDate < startDateStr) return false;
            }
            if (endDateStr) {
                if (!rowDate || rowDate > endDateStr) return false;
            }
            if (selectedDept && rowDept !== selectedDept) {
                return false;
            }
            if (selectedStatus && rowStatus !== selectedStatus) {
                return false;
            }

            return true;
        });

        // Listen for table draw to update summary stats
        $('#ReportsTable').on('draw.dt', function () {
            updateSummaryStats();
        });

        // Fetch raw data to hold for filtering
        $.getJSON('/Request/GetAllRequests').done(res => {
            rawRecords = Array.isArray(res) ? res : (res.data || []);
            updateSummaryStats();
        });
    }

    function updateSummaryStats() {
        if (!dataTableInstance) return;

        const info = dataTableInstance.page.info();
        const visibleRows = dataTableInstance.rows({ search: 'applied' }).data().toArray();

        $('#reportCountFiltered').text(info.recordsDisplay || 0);

        let totalPax = 0;
        let pending = 0;
        let completed = 0;

        visibleRows.forEach(row => {
            const pax = valueOf(row, 'Passengers');
            if (Array.isArray(pax)) totalPax += pax.length;
            else totalPax += 1;

            const st = valueOf(row, 'ApprovalStatus');
            if (st === 'Pending Section Head' || st === 'Pending GA Approve') pending++;
            if (st === 'Completed' || st === 'GA Approved' || st === 'Ready for Dispatch') completed++;
        });

        $('#reportCountPassengers').text(totalPax);
        $('#reportCountPending').text(pending);
        $('#reportCountCompleted').text(completed);
    }

    function loadDepartmentsDropdown() {
        if ($('#reportDepartmentFilter').length) {
            createSelectOptions('#reportDepartmentFilter', {
                url: '/Admin/GetAllDepartments',
                placeholder: 'Department',
                valueField: 'DepartmentName',
                textField: 'DepartmentName'
            });
        }
    }

    $(function () {
        initReportsTable();
        loadDepartmentsDropdown();

        // Trigger table redraw on filter changes
        $('#reportDateStart, #reportDateEnd, #reportDepartmentFilter, #reportStatusFilter').on('change', function () {
            if (dataTableInstance) {
                dataTableInstance.draw();
            }
        });

        $('#btnResetFilters').on('click', function () {
            $('#reportDateStart').val('');
            $('#reportDateEnd').val('');
            if ($('#reportDepartmentFilter').length) {
                $('#reportDepartmentFilter').val('');
            }
            $('#reportStatusFilter').val('');
            if (dataTableInstance) {
                dataTableInstance.search('').draw();
            }
        });
    });
})();
