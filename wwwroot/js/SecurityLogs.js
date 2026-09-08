/**
 * ShuttlOps - Security & Attendance Logs JS Module
 */
(() => {
    let securityDataTable = null;
    let requestsData = [];
    let currentFilterStatus = '';

    const valueOf = (record, name) => record?.[name] ?? record?.[name.charAt(0).toLowerCase() + name.slice(1)];
    const escapeHtml = (value) => String(value ?? '').replace(/[&<>'"]/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' }[char]));

    function initSecurityTable() {
        securityDataTable = createDataTable('#SecurityLogsTable', {
            url: UB + '/Request/GetScheduledOnTrip',
            searchPlaceholder: 'Search Security logs\u2026',
            order: [[1, 'desc']],
            columns: [
                TableColumnsConfig.Text('TicketNumber'),
                TableColumnsConfig.Date('TripDate'),
                TableColumnsConfig.Time('DepartureTime'),
                TableColumnsConfig.Text('DriverName'),
                {
                    data: null,
                    render: function (data, type, row) {
                        const from = escapeHtml(valueOf(row, 'PickupLocation') || 'Gate');
                        const to = escapeHtml(valueOf(row, 'DropLocation') || 'Destination');
                        return '<span class="small fw-semibold">' + from + '</span> <i class="bi bi-arrow-right text-muted mx-1"></i> <span class="small">' + to + '</span>';
                    }
                },
                TableColumnsConfig.Passenger('Passengers'),
                TableColumnsConfig.Status('ApprovalStatus'),
                TableColumnsConfig.Actions(function (data, type, row) {
                    const ticketId = valueOf(row, 'TicketId') || valueOf(row, 'TicketNumber');
                    const status = valueOf(row, 'ApprovalStatus');

                    if (status === 'Ready for Dispatch') {
                        return '<button type="button" class="btn btn-sm btn-outline-primary px-3 btn-dispatch-clearance" data-id="' + ticketId + '"><i class="bi bi-clipboard-check me-1"></i> Dispatch Clearance</button>';
                    } else if (status === 'Completed') {
                        return '<span class="badge bg-success bg-opacity-10 text-success border border-success border-opacity-25 px-2 py-1"><i class="bi bi-check-all me-1"></i> Cleared</span>';
                    } else if (status === 'In Transit') {
                        return '<button type="button" class="btn btn-sm btn-outline-success px-3 btn-gate-clearance" data-id="' + ticketId + '"><i class="bi bi-shield-check me-1"></i> Verify & Clear</button>';
                    } else {
                        return '<span class="badge bg-secondary bg-opacity-10 text-secondary border border-secondary border-opacity-25 px-2 py-1">' + (status || 'Pending') + '</span>';
                    }
                })
            ]
        });

        $('#secStatusTabs button[data-bs-toggle="tab"]').on('shown.bs.tab', function (e) {
            currentFilterStatus = $(e.target).data('status') || '';
            securityDataTable.column(6).search(currentFilterStatus).draw();
        });

        loadCounts();
    }

    function loadCounts() {
        $.getJSON(UB + '/Request/GetAllRequests').done(res => {
            requestsData = Array.isArray(res) ? res : (res.data || []);

            const awaiting = requestsData.filter(r => valueOf(r, 'ApprovalStatus') === 'Assigned').length;
            const active = requestsData.filter(r => valueOf(r, 'ApprovalStatus') === 'In Transit').length;
            const cleared = requestsData.filter(r => valueOf(r, 'ApprovalStatus') === 'Completed').length;

            let totalPax = 0;
            requestsData.forEach(r => {
                const p = valueOf(r, 'Passengers');
                if (Array.isArray(p)) totalPax += p.length;
            });

            $('#secAwaitingCount').text(awaiting);
            $('#secActiveCount').text(active);
            $('#secClearedCount').text(cleared);
            $('#secPaxCount').text(totalPax);
        });
    }

    function openClearanceModal(ticketId) {
        const row = requestsData.find(r => String(valueOf(r, 'TicketId')) === String(ticketId) || valueOf(r, 'TicketNumber') === ticketId);
        if (!row) return;

        $('#clearanceTicketId').val(valueOf(row, 'TicketId'));
        $('#clearanceTicketNumber').text(valueOf(row, 'TicketNumber'));
        $('#clearanceRoute').text(valueOf(row, 'PickupLocation') + ' \u2192 ' + valueOf(row, 'DropLocation'));
        $('#clearanceDriverVehicle').text('Driver: ' + (valueOf(row, 'DriverName') || 'Assigned Driver') + ' | Date: ' + valueOf(row, 'TripDate'));

        const passengers = valueOf(row, 'Passengers') || [];
        if (Array.isArray(passengers) && passengers.length > 0) {
            const paxHtml = passengers.map((p, idx) => {
                const name = valueOf(p, 'PassengerName') || p;
                return '<div class="form-check"><input class="form-check-input pax-check" type="checkbox" id="pax_' + idx + '" checked><label class="form-check-label small" for="pax_' + idx + '">' + escapeHtml(name) + '</label></div>';
            }).join('');
            $('#clearancePassengerList').html(paxHtml);
        } else {
            $('#clearancePassengerList').html('<div class="text-muted small">No specific passengers listed.</div>');
        }

        $('#guardNameInput').val('');
        $('#odometerInputStart').val('');
        $('#odometerInputEnd').val('');
        $('#guardRemarksInput').val('');

        // Pre-fill arrival time with the trip date + current local time
        var tripDate = valueOf(row, 'TripDate');
        if (tripDate) {
            var d = new Date(tripDate);
            if (!isNaN(d.getTime())) {
                var yyyy = d.getFullYear();
                var mm = String(d.getMonth() + 1).padStart(2, '0');
                var dd = String(d.getDate()).padStart(2, '0');
                var now = new Date();
                var hh = String(now.getHours()).padStart(2, '0');
                var min = String(now.getMinutes()).padStart(2, '0');
                $('#arrivalTimeInput').val(yyyy + '-' + mm + '-' + dd + 'T' + hh + ':' + min);
            } else {
                $('#arrivalTimeInput').val(new Date().toISOString().slice(0, 16));
            }
        } else {
            $('#arrivalTimeInput').val(new Date().toISOString().slice(0, 16));
        }

        const modalEl = document.getElementById('gateClearanceModal');
        bootstrap.Modal.getOrCreateInstance(modalEl).show();
    }

    function DispatchGateClearance(ticketId) {
        const row = requestsData.find(r => String(valueOf(r, 'TicketId')) === String(ticketId) || valueOf(r, 'TicketNumber') === ticketId);
        if (!row) return;
        const ticketNumber = valueOf(row, 'TicketNumber');

        showConfirm(
            "Dispatch Gate Clearance",
            "Are you sure you want to dispatch gate clearance for ticket " + ticketNumber + "?",
            "Yes, dispatch it!",
            "Cancel",
            function () {
                showLoading("Dispatching gate clearance for ticket " + ticketNumber + "...");
                $.post(UB + '/Request/DispatchConfirm', { TicketId: ticketId, __RequestVerificationToken: token })
                .done(res => {
                    hideLoading();
                    if (res.success) {
                        showToast('success', "Gate clearance dispatched for ticket " + ticketNumber + ".");
                        if (securityDataTable) {
                            securityDataTable.ajax.reload();
                        }
                    } else {
                        showToast('error', res.message || "Failed to dispatch gate clearance for ticket " + ticketNumber + ".");
                    }
                })
                .fail(err => {
                    hideLoading();
                    showToast('error', "Failed to dispatch gate clearance for ticket " + ticketNumber + ".");
                });
            }
        );
    }

    function VerifyGateClearance(formData) {
        const ticketId = formData.TicketId;
        const row = requestsData.find(r => String(valueOf(r, 'TicketId')) === String(ticketId));
        if (!row) return;
        const ticketNumber = valueOf(row, 'TicketNumber');

        showConfirm(
            'Verify Gate Clearance',
            'Are you sure you want to verify gate clearance for ticket ' + ticketNumber + '?',
            'Yes, verify it!',
            'Cancel',
            function () {
                showLoading('Verifying gate clearance for ticket ' + ticketNumber + '...');
                $.ajax({
                    url: UB + '/Request/SecurityLogs',
                    type: 'POST',
                    contentType: 'application/json',
                    headers: {
                        "RequestVerificationToken": token
                    },
                    data: JSON.stringify({
                        TicketId: parseInt(formData.TicketId) || 0,
                        ArrivalTime: formData.ArrivalTime,
                        OdometerStart: parseInt(formData.OdometerStart) || 0,
                        OdometerEnd: parseInt(formData.OdometerEnd) || 0,
                        GuardId: parseInt(formData.GuardId) || 0,
                        GuardName: formData.GuardName || '',
                        Remarks: formData.Remarks || '',
                        LoggedAt: new Date().toISOString()
                    }),
                    success: function (res) {
                        hideLoading();
                        if (res.success) {
                            bootstrap.Modal.getInstance(document.getElementById('gateClearanceModal')).hide();
                            showToast('success', 'Gate clearance verified for ticket ' + ticketNumber + '.');
                            securityDataTable.ajax.reload();
                            loadCounts();
                        } else {
                            showToast('error', res.message || 'Failed to verify gate clearance for ticket ' + ticketNumber + '.');
                        }
                    },
                    error: function (err) {
                        hideLoading();
                        showToast('error', 'Error occurred while verifying gate clearance for ticket ' + ticketNumber + '.');
                    }
                });
            }
        );
    }

    $(function () {
        initSecurityTable();

        $('#btnRefreshSecurityLogs').on('click', function () {
            if (securityDataTable) {
                securityDataTable.ajax.reload();
            }
            loadCounts();
            showToast('info', 'Security logs updated.');
        });

        $(document).on('click', '.btn-gate-clearance', function () {
            const id = $(this).data('id');
            openClearanceModal(id);
        });

        $(document).on('click', '.btn-dispatch-clearance', function () {
            const id = $(this).data('id');
            DispatchGateClearance(id);
        });

        $('#gateClearanceForm').on('submit', function (e) {
            e.preventDefault();

            const FormDataToObject = {
                TicketId: $('#clearanceTicketId').val(),
                OdometerStart: $('#odometerInputStart').val(),
                OdometerEnd: $('#odometerInputEnd').val(),
                ArrivalTime: $('#arrivalTimeInput').val(),
                GuardName: $('#guardNameInput').val(),
                Remarks: $('#guardRemarksInput').val(),
                GuardId: $('#guardIdInput').val()
            };

            if (!FormDataToObject.GuardName) {
                showToast('warning', 'Please enter guard signature/name.');
                return;
            }
            console.log('Submitting gate clearance data:', FormDataToObject);

            VerifyGateClearance(FormDataToObject);
        });
    });
})();
