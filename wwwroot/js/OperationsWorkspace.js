(function () {
    const workspaceHashes = new Set(['ga-overview', 'fleet-vehicles', 'fleet-drivers', 'ga-calendar', 'app-settings']);
    const state = { vehicles: [], drivers: [], trips: [], calendarDate: new Date() };

    const escapeHtml = (value) => String(value ?? '').replace(/[&<>'"]/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' }[char]));
    const activeHash = () => window.location.hash.replace('#', '');
    const formatDate = (value) => {
        if (!value) return '—';
        const date = new Date(`${value}T00:00:00`);
        return Number.isNaN(date.valueOf()) ? value : date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
    };
    const formatTime = (value) => {
        if (!value) return '';
        const [hours, minutes] = String(value).split(':').map(Number);
        if (Number.isNaN(hours)) return value;
        return new Date(2000, 0, 1, hours, minutes || 0).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
    };

    function updateWorkspaceVisibility() {
        const target = activeHash();
        const isWorkspace = workspaceHashes.has(target);
        $('#ga-workspace').toggleClass('d-none', !isWorkspace);
        $('#requestor-workspace').toggleClass('d-none', isWorkspace);

        if (!isWorkspace) return;
        $('[data-workspace-panel]').addClass('d-none');
        $(`#${target}`).removeClass('d-none');
        $('[data-workspace-link], [data-ga-nav]').removeClass('active');
        $(`[data-workspace-link="${target}"], [data-ga-nav="${target}"]`).addClass('active');

        if (target === 'ga-calendar') renderCalendar();
    }

    function loadWorkspaceData() {
       // const vehiclesRequest = $.getJSON('/GA/GetVehicle').then(data => { state.vehicles = Array.isArray(data) ? data : []; });
       // const driversRequest = $.getJSON('/GA/GetDrivers').then(data => { state.drivers = Array.isArray(data) ? data : []; });
       // const tripsRequest = $.getJSON('/GA/GetApprovedRequest').then(response => { state.trips = Array.isArray(response) ? response : (response.data || []); });

        $.when(vehiclesRequest, driversRequest, tripsRequest)
            .done(() => { renderWorkspace(); })
            .fail(() => {
                $('#gaVehicleTableBody').html('<tr><td colspan="5" class="text-center text-muted py-5">Vehicle list is unavailable.</td></tr>');
                $('#gaDriverTableBody').html('<tr><td colspan="4" class="text-center text-muted py-5">Driver list is unavailable.</td></tr>');
                $('#gaUpcomingTrips').html('<div class="text-center text-muted py-5">Schedule data is unavailable.</div>');
            });
    }

    function renderWorkspace() {
        //$('#gaVehicleCount').text(state.vehicles.length);
        //$('#gaDriverCount').text(state.drivers.length);
        //$('#gaPendingCount').text(state.trips.filter(trip => trip.ApprovalStatus === 'Pending GA Approve').length);
        //const now = new Date();
        //$('#gaMonthTripCount').text(state.trips.filter(trip => {getFullYear();
        //}).length);
        //    const date = new Date(`${trip.TripDate}T00:00:00`);
        //    return date.getMonth() === now.getMonth() && date.getFullYear() === now.
        renderVehicleRows();
        renderDriverRows();
        renderUpcomingTrips();
        renderCalendar();
    }

    function renderVehicleRows() {
        if (!state.vehicles.length) {
            $('#gaVehicleTableBody').html('<tr><td colspan="5" class="text-center text-muted py-5">No vehicles found.</td></tr>');
            return;
        }
        //$('#gaVehicleTableBody').html(state.vehicles.map(vehicle => `
        //    <tr>
        //        <td><div class="fw-semibold">${escapeHtml(vehicle.VehicleModel)}</div><small class="text-muted">Vehicle ID: ${escapeHtml(vehicle.Id)}</small></td>
        //        <td><span class="font-monospace">${escapeHtml(vehicle.PlateNumber)}</span></td>
        //        <td><i class="bi bi-people text-muted me-1"></i>${escapeHtml(vehicle.Capacity)} seats</td>
        //        <td><span class="badge bg-success bg-opacity-10 text-success border border-success border-opacity-25">Registered</span></td>
        //        <td class="text-end"><div class="btn-group btn-group-sm"><button class="btn btn-light" data-fleet-action="view" data-record-type="vehicle" data-record-id="${vehicle.Id}" title="View vehicle"><i class="bi bi-eye"></i></button><button class="btn btn-light" data-fleet-action="edit" data-record-type="vehicle" data-record-id="${vehicle.Id}" title="Edit mockup"><i class="bi bi-pencil"></i></button></div></td>
        //    </tr>`).join(''));

        createDataTable(
            "#VehicleTable",
            {
                url: UB + "/GA/GetVehicle",
                searchPlaceholder: "Search Vehicle Model...",
                order: [[2, 'desc']],
                columns: [
                    TableColumnsConfig.Text("Id"),
                    TableColumnsConfig.Text("VehicleModel"),
                    TableColumnsConfig.Text("PlateNumber"),
                    TableColumnsConfig.VehicleStatus("Status"),
                    TableColumnsConfig.Actions(function (data, type, row) {

                        return `
                        <div class="dropdown">
                            <button class="btn btn-sm btn-light border-0" type="button" data-bs-toggle="dropdown" aria-expanded="false" title="Actions">
                                <i class="bi bi-three-dots-vertical"></i>
                            </button>
                            <ul class="dropdown-menu dropdown-menu-end shadow-sm border-0">
                                <li>
                                    <a class="dropdown-item text-success btn-section-approve-ticket" href="#" data-id="${row.Id || row.VehicleModel}">
                                        <i class="bi bi-pencil text-warning me-2"></i> Edit
                                    </a>
                                </li>
                                <li><hr class="dropdown-divider"></li>
                                <li>
                                    <a class="dropdown-item text-danger btn-delete-ticket" href="#" data-id="${row.Id || row.VehicleModel}">
                                        <i class="bi bi-trash text-danger me-2"></i> Delete Record
                                    </a>
                                </li>
                            </ul>
                        </div>
                       `;
                    })
                ]
            })
    }

    function renderDriverRows() {
        if (!state.drivers.length) {
            $('#gaDriverTableBody').html('<tr><td colspan="4" class="text-center text-muted py-5">No drivers found.</td></tr>');
            return;
        }
        $('#gaDriverTableBody').html(state.drivers.map(driver => {
            const active = String(driver.Status || '').toLowerCase() === 'active';
            return `<tr>
                <td><div class="fw-semibold">${escapeHtml(driver.DriverName)}</div><small class="text-muted">Driver ID: ${escapeHtml(driver.Id)}</small></td>
                <td><span class="font-monospace">${escapeHtml(driver.LicenseNumber || 'Not recorded')}</span></td>
                <td><span class="badge ${active ? 'bg-success bg-opacity-10 text-success border border-success border-opacity-25' : 'bg-secondary bg-opacity-10 text-secondary border border-secondary border-opacity-25'}">${escapeHtml(driver.Status || 'Not specified')}</span></td>
                <td class="text-end"><div class="btn-group btn-group-sm"><button class="btn btn-light" data-fleet-action="view" data-record-type="driver" data-record-id="${driver.Id}" title="View driver"><i class="bi bi-eye"></i></button><button class="btn btn-light" data-fleet-action="edit" data-record-type="driver" data-record-id="${driver.Id}" title="Edit mockup"><i class="bi bi-pencil"></i></button></div></td>
            </tr>`;
        }).join(''));
    }

    function renderUpcomingTrips() {
        const today = new Date(); today.setHours(0, 0, 0, 0);
        const upcoming = state.trips.filter(trip => new Date(`${trip.TripDate}T00:00:00`) >= today)
            .sort((a, b) => `${a.TripDate}${a.DepartureTime}`.localeCompare(`${b.TripDate}${b.DepartureTime}`)).slice(0, 5);
        if (!upcoming.length) {
            $('#gaUpcomingTrips').html('<div class="text-center text-muted py-5"><i class="bi bi-calendar-check fs-4 d-block mb-2"></i>No upcoming trips scheduled.</div>');
            return;
        }
        $('#gaUpcomingTrips').html(upcoming.map(trip => `<div class="ga-upcoming-item"><div class="ga-upcoming-date"><span>${new Date(`${trip.TripDate}T00:00:00`).toLocaleDateString('en-US', { month: 'short' })}</span><strong>${new Date(`${trip.TripDate}T00:00:00`).getDate()}</strong></div><div class="flex-grow-1 min-w-0"><div class="fw-semibold text-truncate">${escapeHtml(trip.PickupLocation)} <i class="bi bi-arrow-right text-muted mx-1"></i> ${escapeHtml(trip.DropLocation)}</div><small class="text-muted">${formatTime(trip.DepartureTime)} · ${escapeHtml(trip.TicketNumber)}</small></div><span class="badge bg-primary bg-opacity-10 text-primary">${escapeHtml(trip.ApprovalStatus || 'Pending')}</span></div>`).join(''));
    }

    function renderCalendar() {
        const year = state.calendarDate.getFullYear();
        const month = state.calendarDate.getMonth();
        $('#gaCalendarTitle').text(new Date(year, month, 1).toLocaleDateString('en-US', { month: 'long', year: 'numeric' }));
        const firstDay = new Date(year, month, 1).getDay();
        const lastDate = new Date(year, month + 1, 0).getDate();
        const tripMap = state.trips.reduce((map, trip) => {
            const key = String(trip.TripDate || '').slice(0, 10);
            (map[key] ||= []).push(trip);
            return map;
        }, {});
        const cells = [];
        for (let index = 0; index < firstDay + lastDate; index++) {
            if (index < firstDay) { cells.push('<div class="ga-calendar-day is-empty"></div>'); continue; }
            const day = index - firstDay + 1;
            const key = `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
            const trips = tripMap[key] || [];
            const isToday = new Date().toDateString() === new Date(year, month, day).toDateString();
            const events = trips.slice(0, 2).map(trip => `<span class="ga-calendar-event" title="${escapeHtml(trip.PickupLocation)} to ${escapeHtml(trip.DropLocation)}">${formatTime(trip.DepartureTime)} ${escapeHtml(trip.TicketNumber)}</span>`).join('');
            const overflow = trips.length > 2 ? `<span class="ga-calendar-more">+${trips.length - 2} more</span>` : '';
            cells.push(`<div class="ga-calendar-day ${isToday ? 'is-today' : ''}"><span class="ga-calendar-date">${day}</span><div class="ga-calendar-events">${events}${overflow}</div></div>`);
        }
        $('#gaCalendarGrid').html(cells.join(''));
    }

    function openMockForm(kind) {
        const isVehicle = kind.startsWith('vehicle');
        const mode = kind.includes('-edit') ? 'Edit' : 'Add';
        const recordId = kind.split(':')[1];
        const record = recordId ? (isVehicle ? state.vehicles : state.drivers).find(item => String(item.Id) === String(recordId)) : {};
        $('#fleetMockModalTitle').text(`${mode} ${isVehicle ? 'Vehicle' : 'Driver'}`);
        $('#fleetMockFields').html(isVehicle ? `
            <div class="col-12"><label class="form-label">Vehicle model</label><input class="form-control" value="${escapeHtml(record.VehicleModel || '')}" placeholder="e.g. Toyota Hiace"></div>
            <div class="col-md-7"><label class="form-label">Plate number</label><input class="form-control" value="${escapeHtml(record.PlateNumber || '')}" placeholder="ABC-1234"></div>
            <div class="col-md-5"><label class="form-label">Capacity</label><input class="form-control" value="${escapeHtml(record.Capacity || '')}" type="number" placeholder="Seats"></div>` : `
            <div class="col-12"><label class="form-label">Driver name</label><input class="form-control" value="${escapeHtml(record.DriverName || '')}" placeholder="Full name"></div>
            <div class="col-md-7"><label class="form-label">License number</label><input class="form-control" value="${escapeHtml(record.LicenseNumber || '')}" placeholder="License number"></div>
            <div class="col-md-5"><label class="form-label">Status</label><select class="form-select"><option>Active</option><option>Inactive</option></select></div>`);
        if (!isVehicle && record.Status) $('#fleetMockFields select').val(record.Status);
        bootstrap.Modal.getOrCreateInstance(document.getElementById('fleetMockModal')).show();
    }

    function openRecord(recordType, recordId) {
        const record = (recordType === 'vehicle' ? state.vehicles : state.drivers).find(item => String(item.Id) === String(recordId));
        if (!record) return;
        const fields = recordType === 'vehicle'
            ? [['Vehicle', record.VehicleModel], ['Plate number', record.PlateNumber], ['Capacity', `${record.Capacity} seats`], ['Record ID', record.Id]]
            : [['Driver', record.DriverName], ['License number', record.LicenseNumber || 'Not recorded'], ['Status', record.Status || 'Not specified'], ['Record ID', record.Id]];
        $('#fleetViewModalTitle').text(`${recordType === 'vehicle' ? 'Vehicle' : 'Driver'} details`);
        $('#fleetViewContent').html(`<div class="record-detail-list">${fields.map(([label, value]) => `<div><span>${escapeHtml(label)}</span><strong>${escapeHtml(value)}</strong></div>`).join('')}</div><p class="alert alert-info small mb-0 mt-3"><i class="bi bi-info-circle me-1"></i>Read-only data from the current backend endpoint.</p>`);
        bootstrap.Modal.getOrCreateInstance(document.getElementById('fleetViewModal')).show();
    }

    function applySettings() {
        const saved = JSON.parse(localStorage.getItem('shuttlopsWorkspaceSettings') || '{}');
        $('#settingDensity').val(saved.density || 'comfortable');
        $('#settingAccent').val(saved.accent || 'blue');
        $('#settingReducedMotion').prop('checked', Boolean(saved.reducedMotion));
        $('#settingApprovalAlerts').prop('checked', saved.approvalAlerts !== false);
        $('#settingScheduleReminders').prop('checked', saved.scheduleReminders !== false);
        document.documentElement.dataset.density = saved.density || 'comfortable';
        document.documentElement.dataset.accent = saved.accent || 'blue';
        document.documentElement.classList.toggle('reduce-motion', Boolean(saved.reducedMotion));
    }

    $(function () {
        updateWorkspaceVisibility();
        loadWorkspaceData();
        applySettings();
        $(window).on('hashchange', updateWorkspaceVisibility);
        $('#calendarPrevious').on('click', () => { state.calendarDate.setMonth(state.calendarDate.getMonth() - 1); renderCalendar(); });
        $('#calendarNext').on('click', () => { state.calendarDate.setMonth(state.calendarDate.getMonth() + 1); renderCalendar(); });
        $('#calendarToday').on('click', () => { state.calendarDate = new Date(); renderCalendar(); });
        $(document).on('click', '[data-mock-form]', function () { openMockForm($(this).data('mock-form')); });
        $(document).on('click', '[data-fleet-action]', function () {
            const button = $(this);
            const type = button.data('record-type');
            const id = button.data('record-id');
            button.data('fleet-action') === 'view' ? openRecord(type, id) : openMockForm(`${type}-edit:${id}`);
        });
        $('#fleetMockForm').on('submit', function (event) { event.preventDefault(); bootstrap.Modal.getInstance(document.getElementById('fleetMockModal')).hide(); showToast('info', 'This is a frontend-only mockup. No record was saved.'); });
        $('#saveWorkspaceSettings').on('click', function () {
            const saved = { density: $('#settingDensity').val(), accent: $('#settingAccent').val(), reducedMotion: $('#settingReducedMotion').is(':checked'), approvalAlerts: $('#settingApprovalAlerts').is(':checked'), scheduleReminders: $('#settingScheduleReminders').is(':checked') };
            localStorage.setItem('shuttlopsWorkspaceSettings', JSON.stringify(saved));
            applySettings();
            showToast('success', 'Browser preferences saved.');
        });
    });
})();
