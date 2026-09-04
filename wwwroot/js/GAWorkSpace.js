/**
 * ShuttlOps - General Affairs Fleet Workspace JS
 */
(() => {
    const workspacePanels = new Set(['ga-overview', 'fleet-vehicles', 'fleet-drivers']);
    const state = { vehicles: [], drivers: [], trips: [] };

    const valueOf = (record, name) => record?.[name] ?? record?.[name.charAt(0).toLowerCase() + name.slice(1)];
    const escapeHtml = (value) => String(value ?? '').replace(/[&<>'"]/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' }[char]));
    const activePanel = () => window.location.hash.replace('#', '') || 'ga-overview';
    const formatDate = value => value ? new Date(`${value}T00:00:00`).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }) : '—';

    function getAntiForgeryToken() {
        return $('input[name="__RequestVerificationToken"]').val() || '';
    }

    function showPanel() {
        const target = workspacePanels.has(activePanel()) ? activePanel() : 'ga-overview';
        $('[data-workspace-panel]').addClass('d-none');
        $(`#${target}`).removeClass('d-none');
        $('[data-workspace-link], [data-ga-nav]').removeClass('active');
        $(`[data-workspace-link="${target}"], [data-ga-nav="${target}"]`).addClass('active');

        if (target === 'fleet-vehicles' && $.fn.DataTable.isDataTable('#VehicleTable')) {
            $('#VehicleTable').DataTable().columns.adjust();
        }
        if (target === 'fleet-drivers' && $.fn.DataTable.isDataTable('#DriverTable')) {
            $('#DriverTable').DataTable().columns.adjust();
        }
    }

    function actionMenu(recordType, row) {
        const id = valueOf(row, 'Id');
        return `<div class="dropdown">
            <button class="btn btn-sm btn-light border-0" type="button" data-bs-toggle="dropdown" aria-expanded="false" title="Actions"><i class="bi bi-three-dots-vertical"></i></button>
            <ul class="dropdown-menu dropdown-menu-end shadow-sm border-0">
                <li><a class="dropdown-item ga-view-record" href="#" data-record-type="${recordType}" data-record-id="${id}"><i class="bi bi-eye text-primary me-2"></i>View details</a></li>
                <li><a class="dropdown-item" href="#" data-mock-form="${recordType}-edit:${id}"><i class="bi bi-pencil text-warning me-2"></i>Edit record</a></li>
                <li><a class="dropdown-item btn-delete-${recordType}" data-id="${id}" href="#"><i class="bi bi-trash3 text-danger me-2"></i>Delete record</a></li>
            </ul>
        </div>`;
    }

    function createFleetTables() {
        createDataTable('#VehicleTable', {
            url: '/GA/GetVehicle',
            searchPlaceholder: 'Search vehicles…',
            order: [[1, 'asc']],
            columns: [
                TableColumnsConfig.Text('Id'),
                TableColumnsConfig.Text('VehicleModel'),
                TableColumnsConfig.Text('PlateNumber'),
                TableColumnsConfig.Number('Capacity'),
                TableColumnsConfig.VehicleStatus('Status'),
                TableColumnsConfig.Actions((_, __, row) => actionMenu('vehicle', row))
            ]
        });

        createDataTable('#DriverTable', {
            url: '/GA/GetDrivers',
            searchPlaceholder: 'Search drivers…',
            order: [[1, 'asc']],
            columns: [
                TableColumnsConfig.Text('Id'),
                TableColumnsConfig.Text('DriverName'),
                TableColumnsConfig.Text('ContactNumber'),
                TableColumnsConfig.Text('LicenseNumber'),
                TableColumnsConfig.DriverStatus('Status'),
                TableColumnsConfig.Actions((_, __, row) => actionMenu('driver', row))
            ]
        });
    }

    function loadOverview() {
        const vehiclesRequest = $.getJSON('/GA/GetVehicle').then(data => { state.vehicles = Array.isArray(data) ? data : (data.data || []); });
        const driversRequest = $.getJSON('/GA/GetDrivers').then(data => { state.drivers = Array.isArray(data) ? data : (data.data || []); });
        const tripsRequest = $.getJSON('/GA/GetApprovedRequest').then(data => { state.trips = Array.isArray(data) ? data : (data.data || []); });

        $.when(vehiclesRequest, driversRequest, tripsRequest)
            .done(() => {
                $('#gaVehicleCount').text(state.vehicles.length);
                $('#gaDriverCount').text(state.drivers.length);
                $('#gaPendingCount').text(state.trips.length);
                const current = new Date();
                $('#gaMonthTripCount').text(state.trips.filter(trip => {
                    const date = new Date(`${valueOf(trip, 'TripDate')}T00:00:00`);
                    return date.getMonth() === current.getMonth() && date.getFullYear() === current.getFullYear();
                }).length);
                renderUpcomingTrips();
            })
            .fail(() => {
                $('#gaUpcomingTrips').html('<div class="text-center text-muted py-5">Unable to load the approved-trip schedule.</div>');
                $('#gaVehicleCount, #gaDriverCount, #gaPendingCount, #gaMonthTripCount').text('—');
            });
    }

    function renderUpcomingTrips() {
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const upcoming = state.trips
            .filter(trip => new Date(`${valueOf(trip, 'TripDate')}T00:00:00`) >= today)
            .sort((left, right) => String(valueOf(left, 'TripDate')).localeCompare(String(valueOf(right, 'TripDate'))))
            .slice(0, 5);

        if (!upcoming.length) {
            $('#gaUpcomingTrips').html('<div class="text-center text-muted py-5"><i class="bi bi-calendar-check fs-4 d-block mb-2"></i>No approved trips are scheduled for upcoming dates.</div>');
            return;
        }

        $('#gaUpcomingTrips').html(upcoming.map(trip => {
            const tripDate = valueOf(trip, 'TripDate');
            const date = new Date(`${tripDate}T00:00:00`);
            return `<div class="ga-upcoming-item">
                <div class="ga-upcoming-date"><span>${date.toLocaleDateString('en-US', { month: 'short' })}</span><strong>${date.getDate()}</strong></div>
                <div class="flex-grow-1 min-w-0"><div class="fw-semibold text-truncate">${escapeHtml(valueOf(trip, 'DropLocation') || 'Destination pending')}</div><small class="text-muted">${escapeHtml(valueOf(trip, 'TicketNumber') || 'Trip ticket')} · ${formatDate(tripDate)}</small></div>
                <span class="badge bg-success bg-opacity-10 text-success border border-success border-opacity-25">Approved</span>
            </div>`;
        }).join(''));
    }

    function openMockForm(kind) {
        const isVehicle = kind.startsWith('vehicle');
        const isEdit = kind.includes('-edit');
        const id = kind.split(':')[1];
        const records = isVehicle ? state.vehicles : state.drivers;
        const record = id ? records.find(item => String(valueOf(item, 'Id')) === String(id)) : {};
        const currentStatus = valueOf(record, 'Status') || 'Available';

        $('#fleetMockModalTitle').text(`${isEdit ? 'Edit' : 'Add'} ${isVehicle ? 'Vehicle' : 'Driver'}`);
        $('#fleetMockModal').data('record-id', id || '');
        $('#fleetMockFields').html(isVehicle
            ? `<div class="col-12">
                <label class="form-label fw-semibold small">Vehicle model</label>
                <input class="form-control" id="vehicleModel" value="${escapeHtml(valueOf(record, 'VehicleModel') || '')}" placeholder="e.g. Toyota Hiace" required>
               </div>
               <div class="col-md-7">
                <label class="form-label fw-semibold small">Plate number</label>
                <input class="form-control" id="plateNumber" value="${escapeHtml(valueOf(record, 'PlateNumber') || '')}" placeholder="e.g. ABC-1234" required>
               </div>
               <div class="col-md-5">
                <label class="form-label fw-semibold small">Capacity (Seats)</label>
                <input class="form-control" type="number" id="capacity" min="1" max="100" value="${escapeHtml(valueOf(record, 'Capacity') || '')}" placeholder="e.g. 14" required>
               </div>
               <div class="col-md-12">
                <label class="form-label fw-semibold small">Status</label>
                <select class="form-select" id="vehicleStatus">
                    <option value="Available" ${currentStatus === 'Available' ? 'selected' : ''}>Available</option>
                    <option value="Under Maintenance" ${currentStatus === 'Under Maintenance' ? 'selected' : ''}>Under Maintenance</option>
                    <option value="Assigned" ${currentStatus === 'Assigned' ? 'selected' : ''}>Assigned</option>
                    <option value="In Transit" ${currentStatus === 'In Transit' ? 'selected' : ''}>In Transit</option>
                    <option value="Decommissioned" ${currentStatus === 'Decommissioned' ? 'selected' : ''}>Decommissioned</option>
                </select>
               </div>`

            : `<div class="col-12">
                <label class="form-label fw-semibold small">Driver name</label>
                <input class="form-control" id="driverName" value="${escapeHtml(valueOf(record, 'DriverName') || '')}" placeholder="e.g. Juan Dela Cruz" required>
               </div>
               <div class="col-md-7">
                <label class="form-label fw-semibold small">License number</label>
                <input class="form-control" id="licenseNumber" value="${escapeHtml(valueOf(record, 'LicenseNumber') || '')}" placeholder="e.g. N01-23-456789" required>
               </div>
               <div class="col-md-5">
                <label class="form-label fw-semibold small">Status</label>
                <select class="form-select" id="driverStatus">
                    <option value="Available" ${currentStatus === 'Available' ? 'selected' : ''}>Available</option>
                    <option value="Active" ${currentStatus === 'Active' ? 'selected' : ''}>Active</option>
                    <option value="Assigned" ${currentStatus === 'Assigned' ? 'selected' : ''}>Assigned</option>
                    <option value="In Transit" ${currentStatus === 'In Transit' ? 'selected' : ''}>In Transit</option>
                    <option value="On Leave" ${currentStatus === 'On Leave' ? 'selected' : ''}>On Leave</option>
                    <option value="Inactive" ${currentStatus === 'Inactive' ? 'selected' : ''}>Inactive</option>
                </select>
               </div>
               <div class="col-md-12">
                <label class="form-label fw-semibold small">Contact number</label>
                <input class="form-control" id="contactNumber" value="${escapeHtml(valueOf(record, 'ContactNumber') || '')}" placeholder="e.g. +63 912 345 6789" required>
               </div>`
        );
        bootstrap.Modal.getOrCreateInstance(document.getElementById('fleetMockModal')).show();
    }

    function showRecord(type, id) {
        const record = (type === 'vehicle' ? state.vehicles : state.drivers).find(item => String(valueOf(item, 'Id')) === String(id));
        if (!record) return;
        const fields = type === 'vehicle'
            ? [['Vehicle', valueOf(record, 'VehicleModel')], ['Plate number', valueOf(record, 'PlateNumber')], ['Capacity', `${valueOf(record, 'Capacity')} seats`], ['Status', valueOf(record, 'Status')]]
            : [['Driver', valueOf(record, 'DriverName')], ['Contact number', valueOf(record, 'ContactNumber')], ['License number', valueOf(record, 'LicenseNumber')], ['Status', valueOf(record, 'Status')]];
        $('#fleetViewModalTitle').text(`${type === 'vehicle' ? 'Vehicle' : 'Driver'} details`);
        $('#fleetViewContent').html(`<div class="record-detail-list">${fields.map(([label, value]) => `<div><span>${escapeHtml(label)}</span><strong>${escapeHtml(value || 'Not recorded')}</strong></div>`).join('')}</div>`);
        bootstrap.Modal.getOrCreateInstance(document.getElementById('fleetViewModal')).show();
    }

    function updateRecord(type, id, updatedData) {
        const displayName = type.charAt(0).toUpperCase() + type.slice(1);
        showConfirm(
            "Confirm Update",
            `Are you sure you want to update this ${type}?`,
            "Yes, update it",
            "Cancel",
            () => {
                showLoading("Updating record...");
                $.ajax({
                    url: type === 'vehicle' ? '/GA/UpdateVehicle' : '/GA/UpdateDriver',
                    method: 'POST',
                    contentType: 'application/json',
                    headers: {
                        "RequestVerificationToken": getAntiForgeryToken()
                    },
                    data: JSON.stringify(updatedData),
                    success: (res) => {
                        hideLoading();
                        if (res.success) {
                            showToast('success', res.message || `${displayName} updated successfully.`);
                            bootstrap.Modal.getInstance(document.getElementById('fleetMockModal'))?.hide();
                            loadOverview();
                            if (type === 'vehicle') {
                                $('#VehicleTable').DataTable().ajax.reload(null, false);
                            } else {
                                $('#DriverTable').DataTable().ajax.reload(null, false);
                            }
                        } else {
                            showToast('error', res.message || `Failed to update ${type}.`);
                        }
                    },
                    error: (err) => {
                        hideLoading();
                        showToast('error', `An error occurred while updating the ${type}.`);
                        console.error(err);
                    }
                });
            }
        );
    }

    function addRecord(type, newRecord) {
        const displayName = type.charAt(0).toUpperCase() + type.slice(1);
        showConfirm(
            "Add New Record",
            `Are you sure you want to add this ${type}?`,
            "Yes, add it",
            "Cancel",
            () => {
                showLoading("Processing...");
                $.ajax({
                    url: type === 'vehicle' ? '/GA/CreateNewVehicle' : '/GA/CreateNewDriver',
                    method: 'POST',
                    contentType: 'application/json',
                    headers: {
                        "RequestVerificationToken": getAntiForgeryToken()
                    },
                    data: JSON.stringify(newRecord),
                    success: (res) => {
                        hideLoading();
                        if (res.success) {
                            showToast('success', res.message || `${displayName} added successfully.`);
                            bootstrap.Modal.getInstance(document.getElementById('fleetMockModal'))?.hide();
                            loadOverview();
                            if (type === 'vehicle') {
                                $('#VehicleTable').DataTable().ajax.reload(null, false);
                            } else {
                                $('#DriverTable').DataTable().ajax.reload(null, false);
                            }
                        } else {
                            showToast('error', res.message || `Failed to create new ${type} record.`);
                            console.error(res.message);
                        }
                    },
                    error: (err) => {
                        hideLoading();
                        showToast('error', `An error occurred while creating new ${type} record.`);
                        console.error(err);
                    }
                });
            }
        );
    }

    function deleteFunction(type, id) {
        const displayName = type.charAt(0).toUpperCase() + type.slice(1);
        showConfirm(
            "Confirm Deletion",
            "Are you sure you want to delete this record?",
            "Yes, delete it",
            "Cancel",
            () => {
                showLoading("Deleting record...");
                $.ajax({
                    url: type === 'driver' ? `/GA/DeleteDriverRecord/${id}` : `/GA/DeleteVehicleRecord/${id}`,
                    method: 'POST',
                    contentType: 'application/json',
                    headers: {
                        "RequestVerificationToken": getAntiForgeryToken()
                    },
                    data: JSON.stringify({ Id: id }),
                    success: (res) => {
                        hideLoading();
                        if (res.success) {
                            showToast('success', res.message || `${displayName} deleted successfully.`);
                            loadOverview();
                            if (type === 'vehicle') {
                                $('#VehicleTable').DataTable().ajax.reload(null, false);
                            } else if (type === 'driver') {
                                $('#DriverTable').DataTable().ajax.reload(null, false);
                            }
                        } else {
                            showToast('error', res.message || `Failed to delete ${type}.`);
                        }
                    },
                    error: (err) => {
                        hideLoading();
                        showToast('error', `An error occurred while deleting the ${type}.`);
                        console.error(err);
                    }
                });
            }
        );
    }

    $(function () {
        createFleetTables();
        loadOverview();
        showPanel();

        $(window).on('hashchange', showPanel);
        $(document).on('click', '[data-mock-form]', function (event) {
            event.preventDefault();
            openMockForm($(this).data('mock-form'));
        });
        $(document).on('click', '.ga-view-record', function (event) {
            event.preventDefault();
            showRecord($(this).data('record-type'), $(this).data('record-id'));
        });

        $('#fleetMockForm').on('submit', function (event) {
            event.preventDefault();

            const isAdd = $('#fleetMockModalTitle').text().includes('Add');
            const isVehicle = $('#fleetMockModalTitle').text().includes('Vehicle');

            if (isAdd) {
                if (isVehicle) {
                    const model = $('#vehicleModel').val()?.trim();
                    const plate = $('#plateNumber').val()?.trim();
                    const capacity = parseInt($('#capacity').val()) || 0;
                    const status = $('#vehicleStatus').val() || 'Available';

                    if (!model || !plate || capacity <= 0) {
                        showToast('warning', 'Please provide a valid vehicle model, plate number, and seating capacity.');
                        return;
                    }

                    addRecord('vehicle', {
                        VehicleModel: model,
                        PlateNumber: plate,
                        Capacity: capacity,
                        Status: status
                    });
                } else {
                    const name = $('#driverName').val()?.trim();
                    const license = $('#licenseNumber').val()?.trim();
                    const contact = $('#contactNumber').val()?.trim();
                    const status = $('#driverStatus').val() || 'Available';

                    if (!name || !license || !contact) {
                        showToast('warning', 'Please provide driver name, license number, and contact number.');
                        return;
                    }

                    addRecord('driver', {
                        DriverName: name,
                        LicenseNumber: license,
                        ContactNumber: contact,
                        Status: status
                    });
                }
            } else {
                const recordId = $('#fleetMockModal').data('record-id');
                if (isVehicle) {
                    const matched = state.vehicles.find(item => String(valueOf(item, 'Id')) === String(recordId));
                    const id = parseInt(valueOf(matched, 'Id') || recordId) || 0;
                    const model = $('#vehicleModel').val()?.trim();
                    const plate = $('#plateNumber').val()?.trim();
                    const capacity = parseInt($('#capacity').val()) || 0;
                    const status = $('#vehicleStatus').val() || 'Available';

                    if (!model || !plate || capacity <= 0) {
                        showToast('warning', 'Please provide a valid vehicle model, plate number, and seating capacity.');
                        return;
                    }

                    updateRecord('vehicle', id, {
                        Id: id,
                        VehicleModel: model,
                        PlateNumber: plate,
                        Capacity: capacity,
                        Status: status
                    });
                } else {
                    const matched = state.drivers.find(item => String(valueOf(item, 'Id')) === String(recordId));
                    const id = parseInt(valueOf(matched, 'Id') || recordId) || 0;
                    const name = $('#driverName').val()?.trim();
                    const license = $('#licenseNumber').val()?.trim();
                    const contact = $('#contactNumber').val()?.trim();
                    const status = $('#driverStatus').val() || 'Available';

                    if (!name || !license || !contact) {
                        showToast('warning', 'Please provide driver name, license number, and contact number.');
                        return;
                    }

                    updateRecord('driver', id, {
                        Id: id,
                        DriverName: name,
                        LicenseNumber: license,
                        ContactNumber: contact,
                        Status: status
                    });
                }
            }
        });

        $(document).on('click', '.btn-delete-vehicle', function (e) {
            e.preventDefault();
            const id = $(this).data("id");
            deleteFunction('vehicle', id);
        });

        $(document).on('click', '.btn-delete-driver', function (e) {
            e.preventDefault();
            const id = $(this).data("id");
            deleteFunction('driver', id);
        });
    });
})();
