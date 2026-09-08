const LoadDepartmentTable = () => {
    return createDataTable(
        "#departmentTable",
        {
            url: UB + "/Admin/GetAllDepartments",
            searchPlaceholder: "Search Department ID...",
            order: [[0, 'asc']],
            columns: [
                TableColumnsConfig.Text("Id"),
                TableColumnsConfig.Text("DepartmentName"),
                TableColumnsConfig.Text("ManagerId"),
                TableColumnsConfig.Text("MembersCount"),
                TableColumnsConfig.Actions(function (data, type, row) {
                    return `
                    <div class="dropdown">
                        <button class="btn btn-sm btn-light border-0" type="button" data-bs-toggle="dropdown" aria-expanded="false" title="Actions">
                            <i class="bi bi-three-dots-vertical"></i>
                        </button>
                        <ul class="dropdown-menu dropdown-menu-end shadow-sm border-0">
                            <li>
                                <a class="dropdown-item btn-assign-manager" href="#" data-bs-toggle="modal" data-bs-target="#AssignManagerModal" data-id="${row.Id || row.DepartmentName}">
                                    <i class="bi bi-pencil-square text-primary me-2"></i> Assign Section Head
                                </a>
                            </li>
                            <li><hr class="dropdown-divider"></li>
                            <li>
                                <a class="dropdown-item text-danger btn-delete-department" href="#" data-id="${row.Id || row.DepartmentName}">
                                    <i class="bi bi-trash text-danger me-2"></i> Delete
                                </a>
                            </li>
                        </ul>
                    </div>
                `;
                })
            ]
        }
    )
}

const CreateDepartment = (forms) => {
    let form = forms;
    showConfirm(
        "Confirm Submission",
        "Are you sure you want to create this department?",
        "Yes, Create",
        "Cancel",
        () => {
            showLoading("Creating department...");
            $.ajax({
                url: UB + "/Admin/CreateDepartment",
                type: "POST",
                contentType: "application/json; charset=utf-8",
                data: JSON.stringify(form),
                success: function (response) {
                    hideLoading();
                    if (response.success) {
                        showAlertWithCallback("success", response.message || "Department created successfully!", () => {
                            window.location.href = UB + (response.redirectUrl || "/Admin/Groups");
                        });
                    } else {
                        showAlert("error", response.message || "An error occurred while creating the department.");
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

const DeleteDepartment = (rowid) => {
    showConfirm(
        "Confirm Delete",
        "Are you sure you want to delete this department?",
        "Yes, Delete",
        "Cancel",
        () => {
            showLoading("Deleting department...");
            $.ajax({
                url: UB + "/Admin/DeleteDepartment",
                type: "POST",
                data: {
                    id: rowid,
                    __RequestVerificationToken: token
                },
                success: function (response) {
                    hideLoading();
                    if (response.success) {
                        showAlertWithCallback("success", response.message || "Department successfully!", () => {
                            window.location.href = UB + (response.redirectUrl || "/Admin/Groups");
                        });
                    } else {
                        showAlert("error", response.message || "An error occurred while deleting the department.");
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

const UpdateDeptHead = (id, deptid) => {
    showConfirm(
        "Confirm Assign",
        "Are you sure you want to assign this user?",
        "Yes, Assign",
        "Cancel",
        () => {
            showLoading("Updating department...");
            $.ajax({
                url: UB + "/Admin/UpdateDeptHead",
                type: "POST",
                data: {
                    id: id,
                    deptid: deptid,
                    __RequestVerificationToken: token
                },
                success: function (response) {
                    hideLoading();
                    if (response.success) {
                        showAlertWithCallback("success", response.message || "Department update successfully!", () => {
                            window.location.href = UB + (response.redirectUrl || "/Admin/Groups");
                        });
                    } else {
                        showAlert("error", response.message || "An error occurred while updating the department.");
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


const LoadDepartmentSelection = () => {
    createSelectOptions(
        "#departmentFilter",
        {
            url: UB + "/Admin/GetDepartments",
            placeholder: "Department",
            valueField: "DepartmentId",
            textField: "DepartmentName"
        }
    )
}


const LoadManagerSelection = () => {
    createSelectOptions(
        "#managerId",
        {
            url: UB + "/Admin/GetSectionHeadId",
            placeholder: "Employee name",
            valueField: "Id",
            textField: "Name"
        }
    )
}

$.fn.dataTable.ext.search.push(function (settings, data, dataIndex, rowData) {
    if (settings.nTable.id !== 'departmentTable') {
        return true;
    }

    const selectedVal = $('#departmentFilter').val();
    if (!selectedVal || selectedVal === "") {
        return true;
    }

    const selectedDepartment = $('#departmentFilter option:selected').text().toLowerCase().trim();

    let rowDepartment = (rowData.DepartmentName || data[2] || '').toString().toLowerCase().trim();
    return rowDepartment === selectedDepartment;
});

$(function () {
    LoadDepartmentSelection();
    const table = LoadDepartmentTable();
    $('#departmentFilter').on('change', function () {
        table.draw();
    });

    $(document).on('click', '.btn-delete-department', function () {
        let dataId = $(this).data("id");
        console.log(dataId);
        DeleteDepartment(dataId);
    });

    $(document).on('click', '.btn-assign-manager', function () {
        const deptid = $(this).data("id");

        $('#AssignManagerModal').data("id", deptid);
    });

    $('#AddDepartmentModal').on('show.bs.modal', function () {
        $('#AddDepartmentForm').on('submit', function (e) {
            e.preventDefault();

            const formData = {
                DepartmentName: $("#department_name").val(),
            };

            CreateDepartment(formData);
        });
    });

    $('#AssignManagerModal').on('show.bs.modal', function () {
        LoadManagerSelection();

        $('#AssignManagerForm').on('submit', function (e) {
            e.preventDefault();
            const deptid = $("#AssignManagerModal").data("id");
            const id = parseInt($("#managerId").val(), 10);

            console.log(deptid, id);

            UpdateDeptHead(id, deptid);
        });
    });
});