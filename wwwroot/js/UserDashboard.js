/**
 * ShuttlOps - User / Requestor Dashboard JS
 */
(() => {
    const state = { trips: [] };

    const valueOf = (record, name) => record?.[name] ?? record?.[name.charAt(0).toLowerCase() + name.slice(1)];
    const escapeHtml = (value) => String(value ?? '').replace(/[&<>'"]/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' }[char]));
    const formatDate = value => value ? new Date(`${value}T00:00:00`).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }) : '—';
    const formatTime = (value) => {
        if (!value) return '';
        const [hours, minutes] = String(value).split(':').map(Number);
        if (Number.isNaN(hours)) return value;
        return new Date(2000, 0, 1, hours, minutes || 0).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
    };

    function loadDashboardData() {
        $.getJSON(UB + '/Request/GetAllRequests')
            .done(res => {
                state.trips = Array.isArray(res) ? res : (res.data || []);
                renderMetrics();
                renderUpcomingTrips();
            })
            .fail(() => {
                $('#userTotalCount, #userPendingCount, #userApprovedCount, #userCompletedCount').text('—');
                $('#userUpcomingTrips').html('<div class="text-center text-muted py-5">Unable to load trip requests.</div>');
            });
    }

    function renderMetrics() {
        const total = state.trips.length;
        const pending = state.trips.filter(t => {
            const s = String(valueOf(t, 'ApprovalStatus') || '');
            return s === 'Pending Section' || s === 'Pending GA Approve' || s === 'Pending';
        }).length;
        const approved = state.trips.filter(t => {
            const s = String(valueOf(t, 'ApprovalStatus') || '');
            return s === 'GA Approved' || s === 'Ready for Dispatch' || s === 'In Transit' || s === 'Approved';
        }).length;
        const completed = state.trips.filter(t => valueOf(t, 'ApprovalStatus') === 'Completed').length;

        $('#userTotalCount').text(total);
        $('#userPendingCount').text(pending);
        $('#userApprovedCount').text(approved);
        $('#userCompletedCount').text(completed);
    }

    function renderUpcomingTrips() {
        if (!state.trips.length) {
            $('#userUpcomingTrips').html(`
                <div class="text-center text-muted py-5">
                    <i class="bi bi-ticket-perforated fs-3 d-block mb-2 text-muted"></i>
                    No trip requests found.
                    <div class="mt-3">
                        <a href="/User/CreateTicketPage" class="btn btn-sm btn-primary">
                            <i class="bi bi-plus-lg me-1"></i>Create First Trip Ticket
                        </a>
                    </div>
                </div>
            `);
            return;
        }

        const today = new Date();
        today.setHours(0, 0, 0, 0);

        // Sort upcoming first, then recent
        const sorted = [...state.trips].sort((left, right) => {
            const leftDate = new Date(`${valueOf(left, 'TripDate')}T00:00:00`);
            const rightDate = new Date(`${valueOf(right, 'TripDate')}T00:00:00`);
            return rightDate - leftDate;
        }).slice(0, 6);

        const html = sorted.map(trip => {
            const tripDate = valueOf(trip, 'TripDate');
            const date = new Date(`${tripDate}T00:00:00`);
            const status = valueOf(trip, 'ApprovalStatus') || 'Pending';
            const depTime = valueOf(trip, 'DepartureTime');

            let badgeClass = 'bg-secondary bg-opacity-10 text-secondary border-secondary';
            if (status.includes('Approved') || status.includes('Ready')) {
                badgeClass = 'bg-success bg-opacity-10 text-success border-success';
            } else if (status.includes('Pending')) {
                badgeClass = 'bg-warning bg-opacity-10 text-warning border-warning';
            } else if (status.includes('Transit')) {
                badgeClass = 'bg-primary bg-opacity-10 text-primary border-primary';
            } else if (status === 'Completed') {
                badgeClass = 'bg-info bg-opacity-10 text-info border-info';
            }

            return `<div class="ga-upcoming-item">
                <div class="ga-upcoming-date">
                    <span>${date.toLocaleDateString('en-US', { month: 'short' })}</span>
                    <strong>${date.getDate()}</strong>
                </div>
                <div class="flex-grow-1 min-w-0">
                    <div class="fw-semibold text-truncate">
                        ${escapeHtml(valueOf(trip, 'PickupLocation') || 'Gate')} 
                        <i class="bi bi-arrow-right text-muted mx-1"></i> 
                        ${escapeHtml(valueOf(trip, 'DropLocation') || 'Destination')}
                    </div>
                    <small class="text-muted">
                        ${escapeHtml(valueOf(trip, 'TicketNumber') || 'Trip ticket')} · 
                        ${formatDate(tripDate)} ${depTime ? '· ' + formatTime(depTime) : ''}
                    </small>
                </div>
                <span class="badge ${badgeClass} border border-opacity-25">${escapeHtml(status)}</span>
            </div>`;
        }).join('');

        $('#userUpcomingTrips').html(html);
    }

    $(function () {
        loadDashboardData();
    });
})();
