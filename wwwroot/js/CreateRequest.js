const CreateRequest = (forms) => {
    let form = forms;
    showConfirm("Confirm Submission",
        "Are you sure you want to submit this trip ticket?",
        "Yes, Submit",
        "Cancel",
        () => {
            showLoading("Submitting trip ticket...");
            $.ajax({
                url: "/Request/CreateRequest",
                type: "POST",
                contentType: "application/json; charset=utf-8",
                headers: {
                    "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
                },
                data: JSON.stringify(form),
                success: function (response) {
                    hideLoading();
                    if (response.success) {
                        showAlertWithCallback("success", response.message || "Trip ticket created successfully!", () => {
                            window.location.href = response.redirectUrl || "/User/TripSchedule";
                        });
                    } else {
                        showAlert("error", response.message || "An error occurred while creating the trip ticket.");
                    }
                },
                error: function (xhr, status, error) {
                    hideLoading();
                    let errorMessage = "An unknown error occurred.";
                    if (xhr.responseText) {
                        try {
                            let response = JSON.parse(xhr.responseText);
                            errorMessage = response.error || response.message || errorMessage;
                        } catch (ex) {
                            console.error("Could not parse error response", ex);
                        }
                    }
                    showAlert("error", errorMessage);
                }
            });
        });
};

$(function () {
    $("#createticket").on("submit", function (e) {
        e.preventDefault();

        const formatTimeOnly = (val) => {
            if (!val) return null;
            const parts = val.split(":");
            if (parts.length === 2) return `${val}:00`;
            return val;
        };

        const rawPassengerNames = $('textarea[name="passengerNamesRaw"]').val() || "";
        const passengersList = rawPassengerNames
            .split(",")
            .map(name => name.trim())
            .filter(name => name.length > 0)
            .map(name => ({ passengerName: name }));

        const depTime = formatTimeOnly($('#EstDepartureTime').val());
        const arrTime = formatTimeOnly($('#EstArrivalTime').val());

        const formData = {
            RequestDate: $('#DateRequested').val(),
            TripDate: $('#DateOfTrip').val(),
            DepartureTime: depTime,
            ArrivalTime: arrTime,
            PickupLocation: $('#PickupLocation').val(),
            DropLocation: $('#DropoffLocation').val(),
            Purpose: $('#Purpose').val(),
            Passengers: passengersList
        };

        CreateRequest(formData);
    });
});

