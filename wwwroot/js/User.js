const LoadUserTable = () => {
    return createDataTable(
        "#UserManagementTable",
        {
            url: "/Admin/GetUser",
            searchPlaceholder: "Search Employee ID...",
            order: [[1, 'desc']],
            columns: [
                TableColumnsConfig.Text("EmployeeID"),
                TableColumnsConfig.Text("EmployeeName"),
                TableColumnsConfig.Text("Email"),
                TableColumnsConfig.Text("Department"),
                TableColumnsConfig.Text("Role"),
                TableColumnsConfig.Actions(function (data, type, row) {
                    return `
                    <div class="dropdown">
                        <button class="btn btn-sm btn-light border-0" type="button" data-bs-toggle="dropdown" aria-expanded="false" title="Actions">
                            <i class="bi bi-three-dots-vertical"></i>
                        </button>
                        <ul class="dropdown-menu dropdown-menu-end shadow-sm border-0">
                            <li>
                                <a class="dropdown-item btn-edit-user" href="#" data-id="${row.Id || row.EmployeeID}">
                                    <i class="bi bi-pencil-square text-primary me-2"></i> Edit
                                </a>
                                <a class="dropdown-item btn-reset-password" href="#" data-id="${row.Id || row.EmployeeID}">
                                    <i class="bi bi-arrow-repeat text-primary me-2"></i> Reset password
                                </a>
                            </li>
                            <li><hr class="dropdown-divider"></li>
                            <li>
                                <a class="dropdown-item text-danger btn-delete-ticket" href="#" data-id="${row.Id || row.EmployeeID}">
                                    <i class="bi bi-trash text-danger me-2"></i> Delete
                                </a>
                            </li>
                        </ul>
                    </div>
                `;
                })
            ]
        }

    );
}


const LoadDepartmentSelection = () => {
    createSelectOptions(
        "#departmentFilter",
        {
            url: "/Admin/GetDepartments",
            placeholder: "Department",
            valueField: "DepartmentId",
            textField: "DepartmentName"
        }
    )
}

const LoadModalSelection = () => {
    createSelectOptions(
        "#department_select",
        {
            url: "/Admin/GetDepartments",
            placeholder: "Department",
            valueField: "DepartmentId",
            textField: "DepartmentName"
        }
    )

    createSelectOptions(
        "#role_select",
        {
            url: "/Admin/GetRoles",
            placeholder: "Role",
            valueField: "RoleId",
            textField: "RoleName"
        }
    )
}

const CreateUser = (forms) => {
    let form = forms;
    showConfirm(
        "Confirm Submission",
        "Are you sure you want to create this user?", 
        "Yes, Create",
        "Cancel",
        () => {
            showLoading("Creating user...");
            $.ajax({
                url: "/Admin/CreateUser",
                type: "POST",
                contentType: "application/json; charset=utf-8",
                data: JSON.stringify(form),
                success: function (response) {
                    hideLoading();
                    if (response.success) {
                        showAlertWithCallback("success", response.message || "User created successfully!", () => {
                            window.location.href = response.redirectUrl || "/Admin/Users";
                        });
                    } else {
                        showAlert("error", response.message || "An error occurred while creating the user.");
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

const DeleteUser = (rowid) => {
    showConfirm(
        "Confirm Delete",
        "Are you sure you want to delete this user?",
        "Yes, Delete",
        "Cancel",
        () => {
            showLoading("Deleting User...");
            $.ajax({
                url: "/Admin/DeleteUser",
                type: "POST",
                data: {
                    id: rowid,
                    __RequestVerificationToken: token
                },
                success: function (response) {
                    hideLoading();
                    if (response.success) {
                        showAlertWithCallback("success", response.message || "User deleted successfully!", () => {
                            window.location.href = response.redirectUrl || "/Admin/Users";
                        });
                    } else {
                        showAlert("error", response.message || "An error occurred while deleting the user.");
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

const ResetPassword = (rowid) => {
    showConfirm(
        "Confirm Reset",
        "Are you sure you want to reset the password of this user?",
        "Yes, Reset",
        "Cancel",
        () => {
            showLoading("Resetting password...");
            $.ajax({
                url: "/Admin/ResetPassword",
                type: "POST",
                data: {
                    id: rowid,
                    __RequestVerificationToken: token
                },
                success: function (response) {
                    hideLoading();
                    if (response.success) {
                        showAlertWithCallback("success", response.message || "Password reset successfully!", () => {
                            window.location.href = response.redirectUrl || "/Admin/Users";
                        });
                    } else {
                        showAlert("error", response.message || "An error occurred while resetting the password.");
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

$.fn.dataTable.ext.search.push(function (settings, data, dataIndex, rowData) {
    if (settings.nTable.id !== 'UserManagementTable') {
        return true;
    }

    const selectedVal = $('#departmentFilter').val();
    if (!selectedVal || selectedVal === "") {
        return true;
    }

    const selectedDepartment = $('#departmentFilter option:selected').text().toLowerCase().trim();

    let rowDepartment = (rowData.Department || data[3] || '').toString().toLowerCase().trim();
    return rowDepartment === selectedDepartment;
});

$(function () {
    LoadDepartmentSelection();
   
    const table = LoadUserTable();
    $('#departmentFilter').on('change', function () {
        table.draw();
    });

    $('#AddUserModal').on('show.bs.modal', function () {
        LoadModalSelection();

        $('#AddUserForm').on('submit', function (e) {
            e.preventDefault();
            const formData = {
                EmployeeID: $('#id_input').val(),
                EmployeeName: $('#name_input').val(),
                Email: $('#email_input').val(),
                Department: $('#department_select').val(),
                Role: $('#role_select').val()
            };
            CreateUser(formData);
        });
    });

    $(document).on('click', '.btn-delete-ticket', function () {
        let dataId = $(this).data("id");
        console.log(dataId);
        DeleteUser(dataId);
    });

    $(document).on('click', '.btn-reset-password', function () {
        let dataId = $(this).data("id");
        console.log(dataId);
        ResetPassword(dataId);
    });

});