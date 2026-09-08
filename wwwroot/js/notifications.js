/**
 * ShuttlOps - Real-Time Notification & SignalR Client
 */
(() => {
    let unreadCount = 0;
    const notificationsHistory = [];

    function initSignalR() {
        if (typeof signalR === 'undefined') {
            console.warn('SignalR library is not loaded. Real-time updates disabled.');
            return;
        }

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(UB + '/Hubs/NotificationsHubs')
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        connection.on('ReceiveNotification', function (data) {
            handleIncomingNotification(data);
        });

        connection.start()
            .then(function () {
                console.log('SignalR Notification Hub connected.');
            })
            .catch(function (err) {
                console.error('SignalR Connection Error: ', err);
            });
    }

    function loadNotificationsFromDB() {
        $.getJSON(UB + '/Notification/GetNotifications')
            .done(function (res) {
                if (res && res.success && Array.isArray(res.data)) {
                    notificationsHistory.length = 0;
                    res.data.forEach(function (n) {
                        notificationsHistory.push({
                            id: n.NotificationId,
                            title: n.Title || 'System Notification',
                            message: n.Message || '',
                            category: n.Category || 'General',
                            ticketNumber: n.TicketNumber || '',
                            url: n.Url || '#',
                            isRead: n.IsRead,
                            timestamp: new Date(n.CreatedAt)
                        });
                    });
                    unreadCount = res.unreadCount || 0;
                    renderNotificationBadge();
                    renderNotificationDropdownList();
                }
            })
            .fail(function () {
                console.warn('Failed to load notifications from DB.');
            });
    }

    function handleIncomingNotification(data) {
        if (!data) return;

        var notifId = data.NotificationId || data.notificationId;
        var notifUrl = data.Url || data.url || '#';

        // Deduplicate: skip if already in history (DB save may arrive before SignalR)
        if (notifId && notificationsHistory.some(function (n) { return n.id === notifId; })) return;

        notificationsHistory.unshift({
            id: notifId || null,
            title: data.Title || data.title || 'System Notification',
            message: data.Message || data.message || '',
            category: data.Category || data.category || 'General',
            ticketNumber: data.TicketNumber || data.ticketNumber || '',
            url: notifUrl,
            isRead: false,
            timestamp: new Date()
        });

        unreadCount++;
        renderNotificationBadge();
        renderNotificationDropdownList();

        var title = data.Title || data.title || 'Notification';
        var msg = data.Message || data.message || '';
        var category = data.Category || data.category;

        if (typeof showToast === 'function') {
            var toastType = category === 'Security' ? 'warning' : 'info';
            showToast(toastType, msg);
        } else if (typeof toastr !== 'undefined') {
            toastr.info(msg, title);
        }

        triggerPageAutoRefresh(data);
    }

    function markAllAsRead() {
        $.ajax({
            url: UB + '/Notification/MarkAllAsRead',
            type: 'POST',
            headers: { 'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val() }
        }).done(function () {
            unreadCount = 0;
            notificationsHistory.forEach(function (n) { n.isRead = true; });
            renderNotificationBadge();
        });
    }

    function renderNotificationBadge() {
        var badge = $('#notifBadge');
        if (!badge.length) return;

        if (unreadCount > 0) {
            badge.text(unreadCount > 99 ? '99+' : unreadCount).removeClass('d-none');
        } else {
            badge.text('').addClass('d-none');
        }
    }

    function renderNotificationDropdownList() {
        var listContainer = $('#notifDropdownList');
        if (!listContainer.length) return;

        if (notificationsHistory.length === 0) {
            listContainer.html(
                '<div class="p-4 text-center text-muted small">' +
                    '<i class="bi bi-bell-slash fs-4 d-block mb-2 text-muted"></i>' +
                    'No new notifications' +
                '</div>'
            );
            return;
        }

        var itemsHtml = notificationsHistory.slice(0, 10).map(function (item) {
            var timeAgo = formatTimeAgo(item.timestamp);
            var iconClass = 'bi-bell-fill text-primary';
            var bgClass = 'bg-primary bg-opacity-10';

            if (item.category === 'Security') {
                iconClass = 'bi-shield-check text-warning';
                bgClass = 'bg-warning bg-opacity-10';
            } else if (item.category === 'Approval') {
                iconClass = 'bi-check2-circle text-success';
                bgClass = 'bg-success bg-opacity-10';
            } else if (item.category === 'Dispatch') {
                iconClass = 'bi-bus-front text-info';
                bgClass = 'bg-info bg-opacity-10';
            }

            var readStyle = item.isRead ? 'opacity: 0.7;' : '';

            return '<a href="' + (item.url || '#') + '" class="dropdown-item p-3 border-bottom d-flex align-items-start gap-3 text-wrap" style="' + readStyle + '">' +
                '<div class="rounded-circle ' + bgClass + ' d-flex align-items-center justify-content-center p-2 flex-shrink-0" style="width: 36px; height: 36px;">' +
                    '<i class="bi ' + iconClass + ' fs-6"></i>' +
                '</div>' +
                '<div class="flex-grow-1 min-w-0">' +
                    '<div class="d-flex justify-content-between align-items-center mb-1">' +
                        '<strong class="text-dark small">' + escapeHtml(item.title) + '</strong>' +
                        '<span class="text-muted" style="font-size: 0.68rem;">' + timeAgo + '</span>' +
                    '</div>' +
                    '<p class="mb-0 text-muted small lh-sm">' + escapeHtml(item.message) + '</p>' +
                '</div>' +
            '</a>';
        }).join('');

        listContainer.html(itemsHtml);
    }

    function triggerPageAutoRefresh(data) {
        if ($.fn.DataTable && $.fn.DataTable.isDataTable('#SecurityLogsTable')) {
            $('#SecurityLogsTable').DataTable().ajax.reload(null, false);
        }
        if ($.fn.DataTable && $.fn.DataTable.isDataTable('#TripScheduleTable')) {
            $('#TripScheduleTable').DataTable().ajax.reload(null, false);
        }
        if ($.fn.DataTable && $.fn.DataTable.isDataTable('#ReportsTable')) {
            $('#ReportsTable').DataTable().ajax.reload(null, false);
        }
    }

    function formatTimeAgo(date) {
        var seconds = Math.floor((new Date() - date) / 1000);
        if (seconds < 60) return 'Just now';
        var minutes = Math.floor(seconds / 60);
        if (minutes < 60) return minutes + 'm ago';
        var hours = Math.floor(minutes / 60);
        if (hours < 24) return hours + 'h ago';
        return date.toLocaleDateString();
    }

    function escapeHtml(value) {
        return String(value ?? '').replace(/[&<>'"]/g, function (char) {
            return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' })[char];
        });
    }

    $(function () {
        loadNotificationsFromDB();
        initSignalR();

        $('#notifDropdownBtn').on('show.bs.dropdown', function () {
            if (unreadCount > 0) markAllAsRead();
        });

        $('#btnClearAllNotifs').on('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            markAllAsRead();
            notificationsHistory.length = 0;
            unreadCount = 0;
            renderNotificationBadge();
            renderNotificationDropdownList();
        });

        // Poll DB every 30 seconds for missed SignalR messages
        setInterval(function () {
            loadNotificationsFromDB();
        }, 30000);

        // Refresh notifications when user returns to tab
        window.addEventListener("focus", function () {
            loadNotificationsFromDB();
        });
    });
})();