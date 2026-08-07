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
                        data: JSON.stringify(form),
                        success: function (response) {
                            hideLoading();
                            if (response.success) {
                                //showAlert("success", response.message || "Trip ticket created successfully!");
                                showAlertWithCallback("success", response.message || "Trip ticket created successfully!", () => {
                                    window.location.href = response.redirectUrl || "/User/CreateTicketPage";
                                });
                                
                            } else {
                                showAlert("error", response.message || "An error occurred while creating the trip ticket.");
                            }
                        },
                        error: function (xhr, status, error) {
                            let errorMessage = "An unknown error occurred.";
                            if (xhr.responseText) {
                                try {
                                    let response = JSON.parse(xhr.responseText);
                                    errorMessage = response.error || response.message || errorMessage;
                                } catch (ex) {
                                    console.error("Could not parse error response");
                                }
                            }
                            showAlert("error", errorMessage);
                        }
                    })
                });   
}

$(function () {
    $("#createticket").on("submit", function (e) {
        e.preventDefault();
        const RawPassengerNames = $('textarea[name="passengerNamesRaw"]').val();
        const passengersList = RawPassengerNames.split(",").map(name => name.trim()).filter(name => name.length > 0).map(name => ({ passengerName: name }));
        const formData = {
            // 1. Fixed: Included missing required field 'RequestedBy'
            RequestedBy: $('#RequestedBy').val() || "System User", // Pass user ID or employee name

            RequestDate: $('#DateRequested').val(),
            TripDate: $('#DateOfTrip').val(),

            // 2. Fixed: Format times as 'HH:mm:ss' so .NET TimeOnly parses correctly
            DepartureTime: $('#EstDepartureTime').val() ? $('#EstDepartureTime').val() + ":00" : null,
            ArrivalTime: $('#EstArrivalTime').val() ? $('#EstArrivalTime').val() + ":00" : null,

            PickupLocation: $('#PickupLocation').val(),
            DropLocation: $('#DropoffLocation').val(),
            Purpose: $('#Purpose').val(),
            Remarks: $('#Remarks').val() || null,
            Passengers: passengersList
        };
        CreateRequest(formData);
    });


});