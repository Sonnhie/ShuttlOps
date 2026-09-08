const AuthenticateUser = function (form) {
    let btn = form.find('button[type="submit"]');
    if (form.valid && !form.valid()) {
        toastr.error("Please fill in all required fields", "Validation Error");
        return false;
    }

    if (document.activeElement) document.activeElement.blur();
    let originalBtnText = btn.html();
    btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Authenticating...');

    $.ajax({
        type: form.attr('method'),
        url: form.attr('action'),
        contentType: 'application/json',
        headers: {
            "RequestVerificationToken": token
        },
        data: JSON.stringify({
            UsernameInput: form.find('[name="UsernameInput"]').val(),
            PasswordInput: form.find('[name="PasswordInput"]').val()
        }),
        success: function (response) {
            if (response.success) {
                toastr.success(response.message, "Authenticate successfully.");

                if (response.requiredChangePassword) {
                    setTimeout(function () {
                        window.location.href = UB + "/Account/ChangePassword";
                    }, 1500);
                } else {
                    setTimeout(function () {
                        window.location.href = UB + (response.redirectUrl || "/");
                    }, 1500);
                }
            } else {
                toastr.error(response.message, "Authentication failed");
            }
        },
        error: function (xhr, error, code) {
            App.showAjaxError(xhr, error, code);
        },
        complete: function () {
            btn.prop('disabled', false).html(originalBtnText);
        }
    })
}

$(function () {
    App.configureToast();

    $("#LoginForm").on("submit", function (e) {
        e.preventDefault();
        AuthenticateUser($(this));
    });

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
                    url: UB + '/Account/ChangePassword',
                    type: 'POST',
                    contentType: 'application/json; charset=utf-8',
                    headers: {
                        'RequestVerificationToken': token
                    },
                    data: JSON.stringify(payload),
                    success: function (res) {
                        hideLoading();
                        if (res.success) {
                            showToast('success', res.message || 'Password changed successfully!');
                            $('#changePasswordForm')[0].reset();
                            setTimeout(function () {
                                window.location.href = UB + '/Account/Login';
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