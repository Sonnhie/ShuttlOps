const Logout = () => {
    showConfirm(
        "Are you sure you want to logout?",
        "You will redirect to login page.",
        `Yes, logout`,
        "Cancel",
        () => {
            showLoading(`Logout user...`);
            $.ajax({
                url: UB + "/Auth/Logout",
                type: "POST",
                headers: {
                    "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
                },
                success: function (res) {
                    if (res.success) {
                        showAlertWithCallback("success", res.message || "Logout successfully!", () => {
                            window.location.href = UB + (res.redirectUrl || "/Account/Login");
                        });
                    } else {
                        showAlert("error", res.message || "An error occurred while Logout the user.");
                    }
                },
                error: function (xhr, status, error) {
                    let errorMessage = "An unknown error occurred.";
                    if (xhr.responseText) {
                        try {
                            let response = JSON.parse(xhr.responseText);
                            errorMessage = response.error || response.message || errorMessage;
                        } catch (e) {
                            console.error("Could not parse error response");
                        }
                    }
                    showAlert("error", errorMessage);
                }
            });
        }
    )
}

$(function () {
    $(document).on('click', '#logout', function () {
        console.log("clicked");
        Logout();
    });
});