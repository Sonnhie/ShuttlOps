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
        data: form.serialize(),
        success: function (response) {
            if (response.success) {
                toastr.success(response.message, "Authenticate successfully.");
                setTimeout(function () {
                    window.location.href = response.redirectUrl || "/";
                }, 1500);
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
});