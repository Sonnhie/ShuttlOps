/**
 * ShuttlOps - Change Password JS Module
 */
(() => {
    function getAntiForgeryToken() {
        return $('input[name="__RequestVerificationToken"]').val() || '';
    }

    $(function () {
        // Toggle password visibility buttons
        $('.btn-toggle-pw').on('click', function () {
            const targetId = $(this).data('target');
            const input = $('#' + targetId);
            const icon = $(this).find('i');

            if (input.attr('type') === 'password') {
                input.attr('type', 'text');
                icon.removeClass('bi-eye').addClass('bi-eye-slash');
            } else {
                input.attr('type', 'password');
                icon.removeClass('bi-eye-slash').addClass('bi-eye');
            }
        });

        // Form submit
        $('#changePasswordForm').on('submit', function (e) {
            e.preventDefault();

            const currentPassword = $('#currentPassword').val() || '';
            const newPassword = $('#newPassword').val() || '';
            const confirmPassword = $('#confirmPassword').val() || '';

            if (!currentPassword) {
                showToast('warning', 'Please enter your current password.');
                return;
            }

            if (!newPassword || newPassword.length < 6) {
                showToast('warning', 'New password must be at least 6 characters.');
                return;
            }

            if (newPassword !== confirmPassword) {
                showToast('warning', 'New password and confirmation do not match.');
                return;
            }

            const payload = {
                CurrentPassword: currentPassword,
                NewPassword: newPassword,
                ConfirmPassword: confirmPassword
            };

            showConfirm(
                'Confirm Password Change',
                'Are you sure you want to update your account password?',
                'Yes, update password',
                'Cancel',
                function () {
                    showLoading('Updating password...');
                    $.ajax({
                        url: '/Account/ChangePassword',
                        type: 'POST',
                        contentType: 'application/json; charset=utf-8',
                        headers: {
                            'RequestVerificationToken': getAntiForgeryToken()
                        },
                        data: JSON.stringify(payload),
                        success: function (res) {
                            hideLoading();
                            if (res.success) {
                                showToast('success', res.message || 'Password changed successfully!');
                                $('#changePasswordForm')[0].reset();
                                setTimeout(function () {
                                    window.location.href = '/User/Settings';
                                }, 1500);
                            } else {
                                showToast('error', res.message || 'Failed to update password.');
                            }
                        },
                        error: function (xhr, status, error) {
                            hideLoading();
                            let message = 'An error occurred while changing password.';
                            if (xhr.responseJSON && xhr.responseJSON.message) {
                                message = xhr.responseJSON.message;
                            }
                            showToast('error', message);
                        }
                    });
                }
            );
        });
    });
})();
