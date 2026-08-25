const LoadTripSchedule = () => {
    const isSectionApprover = hasRole("Section Approver");
    const isAdmin = hasRole("Admin");
    const isGA = hasRole("GA");


    return createDataTable(
        "#TripScheduleTable",
        {
            url: "/Request/GetAllRequests",
            searchPlaceholder: "Search Ticket number...",
            order: [[2, 'desc']],
            columns: [
                TableColumnsConfig.Text("TicketNumber"),
                TableColumnsConfig.Text("Requestor"),
                TableColumnsConfig.Text("RequestDepartment"),
                TableColumnsConfig.Status("ApprovalStatus"),
                TableColumnsConfig.Date("TripDate"),
                TableColumnsConfig.Time("DepartureTime"),
                TableColumnsConfig.Text("PickupLocation"),
                TableColumnsConfig.Text("DropLocation"),
                TableColumnsConfig.Passenger("Passengers"),
                TableColumnsConfig.Text("Purpose"),
                TableColumnsConfig.Actions(function (data, type, row) {

                    const canSectionApprove = (isSectionApprover || isAdmin) && row.ApprovalStatus === 'Pending Section Head';
                    const canGAApprove = isGA && row.ApprovalStatus === 'Pending GA Approve';
                    const canAssign = isGA && row.ApprovalStatus === 'GA Approved';
                    const canRequestDelete = (isSectionApprover || isAdmin) && (row.ApprovalStatus === 'Pending GA Approve' || row.ApprovalStatus === 'Pending GA Approve');

                    return `
                    <div class="dropdown">
                        <button class="btn btn-sm btn-light border-0" type="button" data-bs-toggle="dropdown" aria-expanded="false" title="Actions">
                            <i class="bi bi-three-dots-vertical"></i>
                        </button>
                        <ul class="dropdown-menu dropdown-menu-end shadow-sm border-0">
                            ${canSectionApprove ? `
                            <li>
                                <a class="dropdown-item text-success btn-section-approve-ticket" href="#" data-id="${row.TicketId || row.TicketNumber}">
                                    <i class="bi bi-check-circle text-success me-2"></i> Approve
                                </a>
                            </li>
                            ` : ''}
                            <li><hr class="dropdown-divider"></li>
                            <li>
                            ${canSectionApprove ? `
                            <li>
                                <a class="dropdown-item text-danger btn-delete-ticket" href="#" data-id="${row.TicketId || row.TicketNumber}">
                                    <i class="bi bi-trash text-danger me-2"></i> Delete Request
                                </a>
                            </li>
                            ` : ` `}



                           ${canGAApprove ? `
                           <li>
                                <a class="dropdown-item text-success btn-ga-approve-ticket" href="#"  data-id="${row.TicketId || row.TicketNumber}">
                                    <i class="bi bi-check-circle text-success me-2"></i> Approve
                                </a>
                            </li>
                            ` : ` `}

                           ${canAssign ? `
                           <li>
                                <a class="dropdown-item text-success btn-ga-approve-ticket" href="#" data-bs-toggle="modal" data-bs-target="#AssignDriverModal"  data-id="${row.TicketId || row.TicketNumber}">
                                    <i class="bi bi-check-circle text-success me-2"></i> Assign Vehicle
                                </a>
                            </li>
                            ` : ` `}


                            ${canRequestDelete ? `
                            <li>
                                <a class="dropdown-item text-danger btn-delete-ticket" href="#" data-id="${row.TicketId || row.TicketNumber}">
                                    <i class="bi bi-trash text-danger me-2"></i> Delete Request
                                </a>
                            </li>
                            ` : ` `}

                        </ul>
                    </div>
                `;
                })
            ]
        }
    )
}

const SectionApprove = (id) => {
    showConfirm(
        "Confirm Submission",
        "Are you sure you want to approve this request?",
        "Yes, Approve it!",
        "Cancel",
        () => {
            showLoading("Validating approval...");
            $.ajax({
                url: "/Request/SectionRequestApproval",
                type: "POST",
                data: {
                    status: "Pending GA Approve",
                    id: id
                },
                success: function (response) {
                    hideLoading();
                    if (response.success) {
                        showAlertWithCallback("success", response.message || "Request approved by section head successfully!", () => {
                            $("#TripScheduleTable").DataTable().ajax.reload();
                        });
                    } else {
                        showAlert("error", response.message || "An error occurred while approving the request.");
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
    );
};

const GAapprove = (id) => {
    showConfirm(
        "Confirm Submission",
        "Are you sure you want to approve this request?",
        "Yes, Approve it!",
        "Cancel",
        () => {
            showLoading("Validating approval...");
            $.ajax({
                url: "/Request/GARequestApproval",
                type: "POST",
                data: {
                    status: "GA Approved",
                    id: id
                },
                success: function (response) {
                    hideLoading();
                    if (response.success) {
                        showAlertWithCallback("success", response.message || "Request approved by GA PIC successfully!", () => {
                            $("#TripScheduleTable").DataTable().ajax.reload();
                        });
                    } else {
                        showAlert("error", response.message || "An error occurred while approving the request.");
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
    );
}

const DeleteRequest = (id) => {
    showConfirm(
        "Confirm Submission",
        "Are you sure you want to delete this request?",
        "Yes, Delete it!",
        "Cancel",
        () => {
            showLoading("Deleting Request...");
            $.ajax({
                url: "/Request/DeleteRequestApproval",
                type: "POST",
                data: {
                    id: id
                },
                success: function (response) {
                    hideLoading();
                    if (response.success) {
                        showAlertWithCallback("success", response.message || "Request delete by section head successfully!", () => {
                            $("#TripScheduleTable").DataTable().ajax.reload();
                        });
                    } else {
                        showAlert("error", response.message || "An error occurred while approving the request.");
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
    );
};

const RequestDelete = (id) => {
    showConfirm(
        "Confirm Submission",
        "Are you sure you want to request cancellation of this request?",
        "Yes, cancel it!",
        "Cancel",
        () => {
            showLoading("Deleting Request...");
            $.ajax({
                url: "/Request/RequestDelete",
                type: "POST",
                data: {
                    status: "Request for cancellation",
                    id: id
                },
                success: function (response) {
                    hideLoading();
                    if (response.success) {
                        showAlertWithCallback("success", response.message || "Request cancellation by section head successfully submitted!", () => {
                            $("#TripScheduleTable").DataTable().ajax.reload();
                        });
                    } else {
                        showAlert("error", response.message || "An error occurred while requesting the cancellation.");
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
    );
};



$.fn.dataTable.ext.search.push(function (settings, data, dataIndex, rowData) {
    if (settings.nTable.id !== 'TripScheduleTable') {
        return true;
    }

    const selectedStatus = $('#statusFilter').val()?.toLowerCase().trim();
    if (!selectedStatus) {
        return true; 
    }
    let rowStatus = (rowData.ApprovalStatus || data[1] || '').toString().toLowerCase().trim();
    return rowStatus === selectedStatus;
});

const DriverSelection = () => {
    createSelectOptions(
        "#driverName",
        {
            url: "/Request/GetDrivers",
            placeholder: "Driver",
            valueField: "Id",
            textField: "DriverName"
        }
    )
}

const VehicleSelection = () => {
    createSelectOptions(
        "#vehicleName",
        {
            url: "/Request/GetVehicle",
            placeholder: "Vehicle",
            valueField: "Id",
            textField: "VehicleModel"
        }
    )
}


const PlateNumberTxt = (vehicleid) => {
    $.ajax({
        url: "/Request/GetPlatenumber",
        type: "GET",
        data: {
            id: vehicleid
        },
        success: function (res) {
            $("#plateNumber").val(res);
        },
        error: function (xhr, status, error) {
            App.showAjaxError(xhr, status, error);
        }
    });
}

const CapacityTxt = (vehicleid) => {
    $.ajax({
        url: "/Request/GetCapacity",
        type: "GET",
        data: {
            id: vehicleid
        },
        success: function (res) {
            $("#capacity").val(res);
        },
        error: function (xhr, status, error) {
            App.showAjaxError(xhr, status, error);
        }
    });
}




$(function () {
    const table = LoadTripSchedule();
    $('#statusFilter').on('change', function () {
        table.draw();
    });

    $("#AssignDriverModal").on('show.bs.modal', function () {
        //console.log("clicked")
        DriverSelection();
        VehicleSelection();
    });

    $('#vehicleName').on('change', function () {
        let id = $(this).val();
        console.log(id);
        PlateNumberTxt(id);
        CapacityTxt(id);
    });

    $(document).on('click', '.btn-section-approve-ticket', function () {
        let id = $(this).data("id");
        console.log(id);
        SectionApprove(id);
    });

    $(document).on('click', '.btn-delete-ticket', function () {
        let id = $(this).data("id");
        console.log(id);
        DeleteRequest(id);
    });

    $(document).on('click', '.btn-cancel-ticket', function () {
        let id = $(this).data("id");
        console.log(id);
        RequestDelete(id);
    });

    $(document).on('click', '.btn-ga-approve-ticket', function () {
        let id = $(this).data("id");
        console.log(id);
        GAapprove(id);
    });

});