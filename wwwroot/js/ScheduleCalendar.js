/**
 * ShuttlOps - Schedule Calendar Module
 */
(() => {
    const state = {
        allTrips: [],
        currentDate: new Date(),
        selectedDateKey: null
    };

    const valueOf = (record, name) => record?.[name] ?? record?.[name.charAt(0).toLowerCase() + name.slice(1)];
    const escapeHtml = (value) => String(value ?? '').replace(/[&<>'"]/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' }[char]));
    let polltimer = null;

    function loadScheduleData() {
        $.getJSON('/Request/GetScheduledTrip')
            .done(res => {
                state.allTrips = Array.isArray(res) ? res : (res.data || []);
                const todayKey = formatKey(new Date());
                state.selectedDateKey = todayKey;
                renderCalendar();
                renderDailyItinerary(todayKey);
            })
            .fail(() => {
                showToast('error', 'Failed to load schedule calendar data.');
            });
    }

    function StartPolling(intervalMs = 3000) {
        StopPolling();
        polltimer = setInterval(() => {
            if (document.hidden) return; // Skip polling if the page is not visible
            $.getJSON('/Request/GetScheduledTrip')
                .done(res => {
                    const trips = Array.isArray(res) ? res : (res.data || []);
                    if (JSON.stringify(trips) !== JSON.stringify(state.allTrips)) {
                        state.allTrips = trips;
                        renderCalendar();
                        renderDailyItinerary(state.selectedDateKey);
                    };
                })
                .fail(() => {
                    showToast('error', 'Failed to load schedule calendar data.');
                });
        }, intervalMs);
    }

    function StopPolling() {
        if (polltimer) {
            clearInterval(polltimer);
            polltimer = null;
        }
    }

    function formatKey(date) {
        const y = date.getFullYear();
        const m = String(date.getMonth() + 1).padStart(2, '0');
        const d = String(date.getDate()).padStart(2, '0');
        return `${y}-${m}-${d}`;
    }

    function formatTime(val) {
        if (!val) return 'Time pending';
        const [hours, minutes] = String(val).split(':').map(Number);
        return Number.isNaN(hours) ? val : new Date(2000, 0, 1, hours, minutes || 0).toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' });
    }

    function renderCalendar() {
        const year = state.currentDate.getFullYear();
        const month = state.currentDate.getMonth();

        $('#calendarMonthHeading').text(new Date(year, month, 1).toLocaleDateString('en-US', { month: 'long', year: 'numeric' }));

        const tripMap = {};
        state.allTrips.forEach(trip => {
            const raw = valueOf(trip, 'TripDate');
            if (!raw) return;
            const key = String(raw).slice(0, 10);
            (tripMap[key] ||= []).push(trip);
        });

        const firstDay = new Date(year, month, 1).getDay();
        const lastDate = new Date(year, month + 1, 0).getDate();
        const cells = [];

        for (let i = 0; i < firstDay + lastDate; i++) {
            if (i < firstDay) {
                cells.push('<div class="ga-calendar-day is-empty"></div>');
                continue;
            }

            const day = i - firstDay + 1;
            const key = `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
            const trips = tripMap[key] || [];
            const isToday = new Date().toDateString() === new Date(year, month, day).toDateString();
            const isSelected = state.selectedDateKey === key;

            const events = trips.slice(0, 2).map(t => {
                const status = valueOf(t, 'ApprovalStatus') || '';
                let bgClass = 'bg-primary text-white';
                if (status.includes('Pending GA')) bgClass = 'bg-info text-dark';
                else if (status.includes('Approved')) bgClass = 'bg-warning text-dark'
                else if (status.includes('In Transit')) bgClass = 'bg-primary text-white'                    ;
                else if (status.includes('Completed')) bgClass = 'bg-success text-white'


                return `
                    <div class="ga-calendar-event p-1 mb-1 rounded small ${bgClass}" title="${escapeHtml(valueOf(t, 'DropLocation'))}">
                        <span class="fw-bold">${formatTime(valueOf(t, 'DepartureTime'))}</span>
                        <span class="d-inline-block text-truncate" style="max-width: 100px;">${escapeHtml(valueOf(t, 'DropLocation') || 'Destination')}</span>
                    </div>
                `;
            }).join('');

            const overflow = trips.length > 2 ? `<span class="ga-calendar-more badge bg-light text-dark border">+ ${trips.length - 2} more</span>` : '';

            cells.push(`
                <div class="ga-calendar-day ${isToday ? 'is-today' : ''} ${isSelected ? 'border border-2 border-primary' : ''}" data-date="${key}" style="cursor: pointer; min-height: 85px;">
                    <div class="d-flex justify-content-between align-items-center mb-1">
                        <span class="ga-calendar-date fw-bold">${day}</span>
                        ${trips.length > 0 ? `<span class="badge bg-light text-primary border" style="font-size: 0.65rem;">${trips.length}</span>` : ''}
                    </div>
                    <div class="ga-calendar-events">${events}${overflow}</div>
                </div>
            `);
        }

        $('#fullCalendarGrid').html(cells.join(''));
    }

    function renderDailyItinerary(dateKey) {
        if (!dateKey) return;
        state.selectedDateKey = dateKey;

        const dateObj = new Date(`${dateKey}T00:00:00`);
        const formattedTitle = dateObj.toLocaleDateString('en-US', { weekday: 'long', month: 'short', day: 'numeric', year: 'numeric' });
        $('#selectedDateLabel').text(formattedTitle);

        const trips = state.allTrips.filter(t => {
            const raw = valueOf(t, 'TripDate');
            return raw && String(raw).slice(0, 10) === dateKey;
        });

        if (!trips.length) {
            $('#dailyItineraryContainer').html(`
                <div class="text-center py-5 text-muted">
                    <i class="bi bi-calendar-x fs-3 d-block mb-2"></i>
                    No trips scheduled for this date.
                </div>
            `);
            return;
        }

        const html = trips.map(t => {
            const passengers = valueOf(t, 'Passengers') || [];
            const paxText = Array.isArray(passengers) && passengers.length > 0
                ? passengers.map(p => valueOf(p, 'PassengerName') || p).join(', ')
                : 'No passengers listed';

            const status = valueOf(t, 'TripStatus') || 'Not Started';
            let badgeClass = 'bg-primary text-primary';
            if (status.includes('Not Started')) badgeClass = 'bg-warning text-warning';
            else if (status.includes('In Transit')) badgeClass = 'bg-primary text-primary';
            else if (status === 'Completed') badgeClass = 'bg-success text-success';

            return `
                <div class="card p-3 mb-3 border shadow-none bg-light">
                    <div class="d-flex justify-content-between align-items-start mb-2">
                        <span class="badge bg-white text-dark border fw-bold">${escapeHtml(valueOf(t, 'TicketNumber'))}</span>
                        <span class="badge ${badgeClass} bg-opacity-10 border border-opacity-25 px-2 py-1">${escapeHtml(status)}</span>
                    </div>
                    <div class="fw-bold text-dark fs-6 mb-1">
                        ${escapeHtml(valueOf(t, 'PickupLocation'))} <i class="bi bi-arrow-right text-muted mx-1"></i> ${escapeHtml(valueOf(t, 'DropLocation'))}
                    </div>
                    <div class="small text-muted mb-2">
                        <i class="bi bi-clock me-1 text-primary"></i> ${formatTime(valueOf(t, 'DepartureTime'))}
                        ${valueOf(t, 'DriverName') ? `<span class="ms-2"><i class="bi bi-person-badge me-1"></i>${escapeHtml(valueOf(t, 'DriverName'))}</span>` : ''}
                    </div>
                    <div class="small bg-white p-2 rounded border">
                        <strong class="text-muted d-block" style="font-size: 0.7rem; text-transform: uppercase;">Passenger Manifest</strong>
                        <span class="text-dark">${escapeHtml(paxText)}</span>
                    </div>
                </div>
            `;
        }).join('');

        $('#dailyItineraryContainer').html(html);
    }

    document.addEventListener('visibilitychange', () => {
        if (document.hidden) {
            StopPolling();
        } else {
            StartPolling();
        }
    });

    $(function () {
        loadScheduleData();
        StartPolling();
        $('#calPrevMonth').on('click', () => {
            state.currentDate.setMonth(state.currentDate.getMonth() - 1);
            renderCalendar();
        });

        $('#calNextMonth').on('click', () => {
            state.currentDate.setMonth(state.currentDate.getMonth() + 1);
            renderCalendar();
        });

        $('#calTodayBtn').on('click', () => {
            state.currentDate = new Date();
            const todayKey = formatKey(new Date());
            state.selectedDateKey = todayKey;
            renderCalendar();
            renderDailyItinerary(todayKey);
        });

        $(document).on('click', '.ga-calendar-day[data-date]', function () {
            const date = $(this).data('date');
            renderDailyItinerary(date);
            $('.ga-calendar-day').removeClass('border border-2 border-primary');
            $(this).addClass('border border-2 border-primary');
        });
    });
})();

