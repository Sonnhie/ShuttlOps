/**
 * ShuttlOps - Portal Settings Module
 */
(() => {
    function loadSavedSettings() {
        const saved = JSON.parse(localStorage.getItem('shuttlopsWorkspaceSettings') || '{}');
        $('#userSettingDensity').val(saved.density || 'comfortable');
        $('#userSettingAccent').val(saved.accent || 'blue');
        $('#userSettingReducedMotion').prop('checked', Boolean(saved.reducedMotion));
        $('#userSettingApprovalAlerts').prop('checked', saved.approvalAlerts !== false);
        $('#userSettingScheduleReminders').prop('checked', saved.scheduleReminders !== false);
        $('#userSettingSecurityAlerts').prop('checked', saved.securityAlerts !== false);

        applySettingsToDom(saved);
    }

    function applySettingsToDom(settings) {
        document.documentElement.dataset.density = settings.density || 'comfortable';
        document.documentElement.dataset.accent = settings.accent || 'blue';
        document.documentElement.classList.toggle('reduce-motion', Boolean(settings.reducedMotion));
    }

    function saveSettings() {
        const settings = {
            density: $('#userSettingDensity').val(),
            accent: $('#userSettingAccent').val(),
            reducedMotion: $('#userSettingReducedMotion').is(':checked'),
            approvalAlerts: $('#userSettingApprovalAlerts').is(':checked'),
            scheduleReminders: $('#userSettingScheduleReminders').is(':checked'),
            securityAlerts: $('#userSettingSecurityAlerts').is(':checked')
        };

        localStorage.setItem('shuttlopsWorkspaceSettings', JSON.stringify(settings));
        applySettingsToDom(settings);
        showToast('success', 'Portal preferences successfully saved.');
    }

    $(function () {
        loadSavedSettings();

        $('#btnSaveAllSettings').on('click', function () {
            saveSettings();
        });

        $('#userSettingDensity, #userSettingAccent, #userSettingReducedMotion').on('change', function () {
            const tempSettings = {
                density: $('#userSettingDensity').val(),
                accent: $('#userSettingAccent').val(),
                reducedMotion: $('#userSettingReducedMotion').is(':checked')
            };
            applySettingsToDom(tempSettings);
        });
    });
})();

