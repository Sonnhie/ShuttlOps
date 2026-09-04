/**
 * ShuttlOps - Operations Analytics JS Module
 */
(() => {
    const state = {
        allRequests: [],
        filteredRequests: [],
        vehicles: [],
        drivers: [],
        timePeriod: 'month'
    };

    const valueOf = (record, name) => record?.[name] ?? record?.[name.charAt(0).toLowerCase() + name.slice(1)];
    const escapeHtml = (value) => String(value ?? '').replace(/[&<>'"]/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' }[char]));

    function loadData() {
        const reqPromise = $.getJSON('/Request/GetAllRequests').then(res => {
            state.allRequests = Array.isArray(res) ? res : (res.data || []);
        });

        const vehPromise = $.getJSON('/Request/GetVehicle').then(res => {
            state.vehicles = Array.isArray(res) ? res : (res.data || []);
        });

        const drvPromise = $.getJSON('/Request/GetDrivers').then(res => {
            state.drivers = Array.isArray(res) ? res : (res.data || []);
        });

        $.when(reqPromise, vehPromise, drvPromise)
            .done(() => {
                applyFilterAndRender();
            })
            .fail(() => {
                showToast('error', 'Failed to load analytics data.');
            });
    }

    function applyFilterAndRender() {
        const now = new Date();
        const startOfDay = new Date(now.getFullYear(), now.getMonth(), now.getDate());
        const startOfWeek = new Date(now.setDate(now.getDate() - now.getDay()));
        const startOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);
        const startOfYear = new Date(now.getFullYear(), 0, 1);

        state.filteredRequests = state.allRequests.filter(item => {
            const rawDate = valueOf(item, 'TripDate') || valueOf(item, 'RequestDate');
            if (!rawDate) return true;
            const tripDate = new Date(`${rawDate}T00:00:00`);
            if (Number.isNaN(tripDate.valueOf())) return true;

            if (state.timePeriod === 'today') return tripDate >= startOfDay;
            if (state.timePeriod === 'week') return tripDate >= startOfWeek;
            if (state.timePeriod === 'month') return tripDate >= startOfMonth;
            if (state.timePeriod === 'year') return tripDate >= startOfYear;
            return true;
        });

        renderKPIs();
        renderDepartmentBreakdown();
        renderStatusDistribution();
        renderPeakHours();
        renderFleetHealth();
    }

    function renderKPIs() {
        const totalTrips = state.filteredRequests.length;
        const dispatched = state.filteredRequests.filter(r => {
            const st = valueOf(r, 'ApprovalStatus');
            return st === 'Ready for Dispatch' || st === 'GA Approved' || st === 'Completed';
        }).length;

        let totalPassengers = 0;
        state.filteredRequests.forEach(r => {
            const pList = valueOf(r, 'Passengers');
            if (Array.isArray(pList)) totalPassengers += pList.length;
            else totalPassengers += 1;
        });

        const activeVehicles = state.vehicles.filter(v => {
            const st = valueOf(v, 'Status') || '';
            return st === 'Available' || st === 'In Transit' || st === 'Assigned';
        }).length;

        const utilizationPercent = state.vehicles.length > 0
            ? Math.round((activeVehicles / state.vehicles.length) * 100)
            : 0;

        $('#statTotalTrips').text(totalTrips);
        $('#statDispatchedTrips').text(dispatched);
        $('#statTotalPassengers').text(totalPassengers);
        $('#statFleetUtilization').text(`${utilizationPercent}%`);
    }

    function renderDepartmentBreakdown() {
        const counts = {};
        state.filteredRequests.forEach(r => {
            const dept = valueOf(r, 'RequestDepartment') || 'General / Unassigned';
            counts[dept] = (counts[dept] || 0) + 1;
        });

        const total = state.filteredRequests.length || 1;
        const sorted = Object.entries(counts).sort((a, b) => b[1] - a[1]);

        if (sorted.length === 0) {
            $('#deptBreakdownContainer').html('<div class="text-center py-4 text-muted"><i class="bi bi-inbox fs-4 d-block mb-2"></i>No department activity recorded in this period.</div>');
            return;
        }

        const html = sorted.map(([dept, count]) => {
            const pct = Math.round((count / total) * 100);
            return `
                <div class="mb-3">
                    <div class="d-flex justify-content-between align-items-center mb-1 small">
                        <span class="fw-semibold text-dark">${escapeHtml(dept)}</span>
                        <span class="text-muted">${count} trips (${pct}%)</span>
                    </div>
                    <div class="progress" style="height: 8px;">
                        <div class="progress-bar bg-primary" role="progressbar" style="width: ${pct}%;" aria-valuenow="${pct}" aria-valuemin="0" aria-valuemax="100"></div>
                    </div>
                </div>
            `;
        }).join('');

        $('#deptBreakdownContainer').html(html);
    }

    function renderStatusDistribution() {
        const statusMap = {
            'Ready for Dispatch': { count: 0, color: 'primary', label: 'Ready for Dispatch' },
            'GA Approved': { count: 0, color: 'success', label: 'GA Approved' },
            'Pending Section Head': { count: 0, color: 'warning', label: 'Pending Section' },
            'Pending GA Approve': { count: 0, color: 'info', label: 'Pending GA' },
            'Rejected': { count: 0, color: 'danger', label: 'Rejected' },
            'Completed': { count: 0, color: 'dark', label: 'Completed' }
        };

        state.filteredRequests.forEach(r => {
            const st = valueOf(r, 'ApprovalStatus') || 'Pending Section Head';
            if (statusMap[st]) {
                statusMap[st].count++;
            } else {
                statusMap['Pending Section Head'].count++;
            }
        });

        const total = state.filteredRequests.length || 1;
        const html = Object.values(statusMap).map(item => {
            const pct = Math.round((item.count / total) * 100);
            return `
                <div class="d-flex align-items-center justify-content-between py-2 border-bottom">
                    <div class="d-flex align-items-center gap-2">
                        <span class="badge bg-${item.color} bg-opacity-10 text-${item.color} border border-${item.color} border-opacity-25 px-2 py-1">
                            ${item.label}
                        </span>
                    </div>
                    <div class="d-flex align-items-center gap-3">
                        <span class="fw-bold text-dark">${item.count}</span>
                        <span class="text-muted small" style="min-width: 45px; text-align: right;">${pct}%</span>
                    </div>
                </div>
            `;
        }).join('');

        $('#statusDistributionContainer').html(html);
    }

    function renderPeakHours() {
        const timeWindows = {
            'Early Morning (06:00 - 09:00)': 0,
            'Mid-Day (09:00 - 13:00)': 0,
            'Afternoon (13:00 - 17:00)': 0,
            'Evening / Night (17:00 - 22:00)': 0
        };

        state.filteredRequests.forEach(r => {
            const depTime = valueOf(r, 'DepartureTime');
            if (!depTime) return;
            const [hStr] = String(depTime).split(':');
            const h = parseInt(hStr, 10);
            if (h >= 6 && h < 9) timeWindows['Early Morning (06:00 - 09:00)']++;
            else if (h >= 9 && h < 13) timeWindows['Mid-Day (09:00 - 13:00)']++;
            else if (h >= 13 && h < 17) timeWindows['Afternoon (13:00 - 17:00)']++;
            else timeWindows['Evening / Night (17:00 - 22:00)']++;
        });

        const total = state.filteredRequests.length || 1;
        const html = Object.entries(timeWindows).map(([windowLabel, count]) => {
            const pct = Math.round((count / total) * 100);
            return `
                <div class="mb-3">
                    <div class="d-flex justify-content-between align-items-center mb-1 small">
                        <span class="fw-medium text-dark"><i class="bi bi-clock me-1 text-primary"></i> ${windowLabel}</span>
                        <span class="fw-bold">${count} trips</span>
                    </div>
                    <div class="progress" style="height: 8px;">
                        <div class="progress-bar bg-info" role="progressbar" style="width: ${pct}%;" aria-valuenow="${pct}" aria-valuemin="0" aria-valuemax="100"></div>
                    </div>
                </div>
            `;
        }).join('');

        $('#peakHoursContainer').html(html);
    }

    function renderFleetHealth() {
        const vAvailable = state.vehicles.filter(v => (valueOf(v, 'Status') || 'Available') === 'Available').length;
        const vTransit = state.vehicles.filter(v => valueOf(v, 'Status') === 'In Transit').length;
        const vAssigned = state.vehicles.filter(v => valueOf(v, 'Status') === 'Assigned').length;
        const vMaint = state.vehicles.filter(v => valueOf(v, 'Status') === 'Under Maintenance').length;

        const dAvailable = state.drivers.filter(d => (valueOf(d, 'Status') || 'Available') === 'Available' || valueOf(d, 'Status') === 'Active').length;
        const dTotal = state.drivers.length;

        const html = `
            <div class="row g-3">
                <div class="col-6">
                    <div class="p-3 border rounded-3 bg-light">
                        <div class="text-muted small text-uppercase fw-semibold mb-1">Vehicles Ready</div>
                        <div class="fs-4 fw-bold text-success">${vAvailable} <span class="fs-6 fw-normal text-muted">/ ${state.vehicles.length}</span></div>
                    </div>
                </div>
                <div class="col-6">
                    <div class="p-3 border rounded-3 bg-light">
                        <div class="text-muted small text-uppercase fw-semibold mb-1">Drivers Ready</div>
                        <div class="fs-4 fw-bold text-primary">${dAvailable} <span class="fs-6 fw-normal text-muted">/ ${dTotal}</span></div>
                    </div>
                </div>
                <div class="col-6">
                    <div class="p-3 border rounded-3 bg-light">
                        <div class="text-muted small text-uppercase fw-semibold mb-1">In Transit / Active</div>
                        <div class="fs-4 fw-bold text-warning">${vTransit + vAssigned}</div>
                    </div>
                </div>
                <div class="col-6">
                    <div class="p-3 border rounded-3 bg-light">
                        <div class="text-muted small text-uppercase fw-semibold mb-1">Under Maintenance</div>
                        <div class="fs-4 fw-bold text-danger">${vMaint}</div>
                    </div>
                </div>
            </div>
        `;

        $('#fleetHealthContainer').html(html);
    }

    $(function () {
        loadData();

        $('#analyticsTimePeriod').on('change', function () {
            state.timePeriod = $(this).val();
            applyFilterAndRender();
        });
    });
})();

