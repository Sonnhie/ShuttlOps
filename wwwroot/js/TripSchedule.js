const isSectionApprover = hasRole("Section Approver");
const isAdmin = hasRole("Admin");
const isGA = hasRole("GA");
const isRequestor = hasRole("Requestor");

const LoadTripSchedule = () => {
    console.log(isSectionApprover);
    return createDataTable(
        "#TripScheduleTable",
        {
            url: UB + "/Request/GetAllRequests",
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
                    const ticketId = row.TicketId || row.TicketNumber;
                    const currentStatus = row.ApprovalStatus; // e.g., "Pending Section Approval", "Pending GA Approve", etc.

                    // 1. Combine Role AND Status checks
                    const canSectionApprove = isSectionApprover && currentStatus === "Pending Section Head";
                    const canGAApprove = isGA && currentStatus === "Pending GA Approve";
                    const canAssign = isGA && currentStatus === "GA Approved";
                    const canDelete = (isSectionApprover || isRequestor) && currentStatus === "Pending Section Head";
                    const canGADelete = isGA && currentStatus === "Request for cancellation";
                    const canSectionDelete = (isSectionApprover || isRequestor) && currentStatus === "GA Approved";


                    let menuItems = [];

                    // 2. Push valid items dynamically
                    if (canSectionApprove) {
                        menuItems.push(`
                            <li>
                                <a class="dropdown-item text-success btn-section-approve-ticket" href="#" data-id="${ticketId}" data-status="Pending GA Approve">
                                    <i class="bi bi-check-circle text-success me-2"></i> Approve (Section)
                                </a>
                            </li>
                        `);
                    }

                    if (canGAApprove) {
                        menuItems.push(`
                            <li>
                                <a class="dropdown-item text-success btn-ga-approve-ticket" href="#" data-id="${ticketId}" data-status="GA Approved">
                                    <i class="bi bi-check-circle text-success me-2"></i> Approve (GA)
                                </a>
                            </li>
                        `);
                                    }

                    if (canAssign) {
                        menuItems.push(`
                            <li>
                                <a class="dropdown-item text-primary btn-assign-vehicle" href="#" data-bs-toggle="modal" data-bs-target="#AssignDriverModal" data-id="${ticketId}">
                                    <i class="bi bi-truck me-2"></i> Assign Vehicle
                                </a>
                            </li>
                        `);
                    }

                    if (canDelete) {
                        menuItems.push(`
                            ${menuItems.length > 0 ? '<li><hr class="dropdown-divider"></li>' : ''}
                            <li>
                                <a class="dropdown-item text-danger btn-delete-ticket" href="#" data-id="${ticketId}">
                                    <i class="bi bi-trash me-2"></i> Delete Request
                                </a>
                            </li>
                        `);
                    }

                    if (canGADelete) {
                        menuItems.push(`
                            ${menuItems.length > 0 ? '<li><hr class="dropdown-divider"></li>' : ''}
                            <li>
                                <a class="dropdown-item text-danger btn-delete-ticket" href="#" data-id="${ticketId}">
                                    <i class="bi bi-trash me-2"></i> Delete Request
                                </a>
                            </li>
                        `);
                    }

                    if (canSectionDelete) {
                        menuItems.push(`
                            ${menuItems.length > 0 ? '<li><hr class="dropdown-divider"></li>' : ''}
                            <li>
                                <a class="dropdown-item text-danger btn-cancel-ticket" href="#" data-id="${ticketId}">
                                    <i class="bi bi-trash me-2"></i> Request for Cancellation
                                </a>
                            </li>
                        `);
                    }

                    // If no actions available for the current role/status, show a disabled state
                    if (menuItems.length === 0) {
                          return `<span class="text-muted small">No actions</span>`;
                    }

                    return `
                        <div class="dropdown">
                            <button class="btn btn-sm btn-light border-0" type="button" data-bs-toggle="dropdown" aria-expanded="false" title="Actions">
                                <i class="bi bi-three-dots-vertical"></i>
                            </button>
                            <ul class="dropdown-menu dropdown-menu-end shadow-sm border-0">
                                ${menuItems.join('')}
                            </ul>
                        </div>
                    `;
                })
            ]
        }
    )
}

const ProcessApproval = (id, status) => {
    showConfirm(
        "Confirm Submission",
        "Are you sure you want to update status of this request?",
        "Yes, update it!",
        "Cancel",
        () => {
            showLoading("Validating approval...");
            $.ajax({
                url: UB + "/Request/ProcessApproval",
                type: "POST",
                data: {
                    status: status,
                    id: id,
                    __RequestVerificationToken: token
                },
                success: function (response) {
                    hideLoading();
                    if (response.success) {
                        showAlertWithCallback("success", response.message || "Status updated successfully!", () => {
                            $("#TripScheduleTable").DataTable().ajax.reload();
                        });
                    } else {
                        showAlert("error", response.message || "An error occurred while updating the status.");
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
                url: UB + "/Request/DeleteRequestApproval",
                type: "POST",
                data: {
                    id: id,
                    __RequestVerificationToken: token
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

$.fn.dataTable.ext.search.push(function (settings, data, dataIndex, rowData) {
    if (settings.nTable.id !== 'TripScheduleTable') {
        return true;
    }

    const selectedStatus = $('#scheduleStatusTabs .nav-link.active').data('status')?.toString().toLowerCase().trim();
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
            url: UB + "/Request/GetDrivers",
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
            url: UB + "/Request/GetVehicle",
            placeholder: "Vehicle",
            valueField: "Id",
            textField: "VehicleModel"
        }
    )
}

const PlateNumberTxt = (vehicleid) => {
    $.ajax({
        url: UB + "/Request/GetPlatenumber",
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

const VehicleStatus = (vehicleid) => {
    $.ajax({
        url: UB + "/Request/GetVehicleStatus",
        type: "GET",
        data: {
            id: vehicleid
        },
        success: function (res) {
            $("#Status").val(res);
        },
        error: function (xhr, status, error) {
            App.showAjaxError(xhr, status, error);
        }
    });
}

const CapacityTxt = (vehicleid) => {
    $.ajax({
        url: UB + "/Request/GetCapacity",
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

const AssignDriver = () => {
    const formData = {
        TicketId: parseInt($("#ticketId").val(), 10) || 0,
        DriverId: parseInt($("#driverId").val(), 10) || 0,
        VehicleId: parseInt($("#vehicleId").val(), 10) || 0,
        // Selected option text instead of ID value
        DriverName: $('#driverSelect').val(),
        VehicleStatus: $("#vehicleStatus").val() || $("#Status").val(),
        Remarks: $("#Remarks").val(),
        PlateNumber: $("#plateNumber").val(),
        Status: "Ready for Dispatch"
    };

    return showConfirm(
        "Confirm Submission",
        "Are you sure you want to assign this Driver?",
        "Yes, Assign",
        "Cancel",
        () => {
            showLoading("Processing...");

            $.ajax({
                url: UB + "/GA/AssignDriver",
                type: "POST",
                contentType: "application/json",
                // Pass Anti-Forgery Token via Headers instead of the JSON body
                headers: {
                    "RequestVerificationToken": typeof token !== 'undefined' ? token : $('input[name="__RequestVerificationToken"]').val()
                },
                // Send flat DTO payload directly
                data: JSON.stringify(formData),
                success: function (response) {
                    hideLoading();

                    // Close the modal cleanly
                    $('#AssignDriverModal').modal('hide');

                    if (response.success) {
                        showAlertWithCallback("success", response.message || "Successfully assigned.", () => {
                            $("#TripScheduleTable").DataTable().ajax.reload();
                        });
                    } else {
                        showAlert("error", response.message || "An error occurred while assigning driver.");
                    }
                },
                error: function (xhr) {
                    hideLoading();
                    let errorMessage = "An unknown error occurred.";
                    if (xhr.responseText) {
                        try {
                            let response = JSON.parse(xhr.responseText);
                            errorMessage = response.error || response.message || errorMessage;
                        } catch (e) {
                            console.error("Could not parse error response", e);
                        }
                    }
                    showAlert("error", errorMessage);
                }
            });
        }
    );
};
$(function () {
    const table = LoadTripSchedule();
    const tabDescriptions = {
        '': 'All submitted and scheduled trips',
        'Pending Section Head': 'Trips awaiting section-head approval',
        'Pending GA Approve': 'Trips awaiting General Affairs approval',
        'GA Approved': 'Approved trips ready for vehicle assignment or dispatch',
        'Request for cancellation': 'Trips with an active cancellation request',
        'Ready for dispatch': 'Trips ready for dispatch',
        'In Transit': 'Trips that already in transit.',
        'Completed' : 'Trips that already completed.'
    };

    $('#scheduleStatusTabs .nav-link').on('shown.bs.tab', function (event) {
        const selectedStatus = $(event.target).data('status') || '';
        $('#activeTripTabDescription').text(tabDescriptions[selectedStatus] || 'Filtered trip records');
        table.draw();
    });

    $("#AssignDriverModal").on('show.bs.modal', function(event) {
        //console.log("clicked")
        const button = $(event.relatedTarget);
        const ticketId = button.data('id');
        $(this).find('#ticketId').val(ticketId);
       // console.log("Modal opened for Ticket ID:", ticketId);
        DriverSelection();
        VehicleSelection();

        $("#AssignDriverForm").on('submit', function (e) {
            e.preventDefault();
            AssignDriver();
        });
    });

    $('#vehicleName').on('change', function () {
        let id = $(this).val();
      //  console.log(id);
        $("#vehicleId").val(id);
        PlateNumberTxt(id);
        CapacityTxt(id);
        VehicleStatus(id);
    });

    $('#driverName').on('change', function () {
        let id = $(this).val();
     //   console.log(id);
        const selectedOption = $(this).find('option:selected');
        $("#driverId").val(id);
        $('#driverSelect').val(selectedOption.text());
    });



    $(document).on('click', '.btn-section-approve-ticket', function () {
        let id = $(this).data("id");
      //  console.log(id);
        ProcessApproval(id, "Pending GA Approve")
    });

    $(document).on('click', '.btn-delete-ticket', function () {
        let id = $(this).data("id");
     //   console.log(id);
        DeleteRequest(id);
    });

    $(document).on('click', '.btn-cancel-ticket', function () {
        let id = $(this).data("id");
     //   console.log(id);
        ProcessApproval(id, "Request for cancellation")
    });

    $(document).on('click', '.btn-ga-approve-ticket', function () {
        let id = $(this).data("id");
     //   console.log(id);
        ProcessApproval(id, "GA Approved");
    });

  

});
