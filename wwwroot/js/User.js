const LoadUserTable = () => {
    return createDataTable(
        "#UserManagementTable",
        {
            url: UB + "/Admin/GetUser",
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
                                <a class="dropdown-item btn-edit-user" href="#" data-bs-toggle="modal" data-bs-target="#UpdateUserModal" data-id="${row.Id}">
                                    <i class="bi bi-pencil-square text-primary me-2"></i> Edit
                                </a>
                                <a class="dropdown-item btn-reset-password" href="#" data-id="${row.Id}">
                                    <i class="bi bi-arrow-repeat text-primary me-2"></i> Reset password
                                </a>
                            </li>
                            <li><hr class="dropdown-divider"></li>
                            <li>
                                <a class="dropdown-item text-danger btn-delete-ticket" href="#" data-id="${row.Id}">
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
};

const LoadDepartmentSelection = () => {
    createSelectOptions(
        "#departmentFilter",
        {
            url: UB + "/Admin/GetDepartments",
            placeholder: "Department",
            valueField: "DepartmentId",
            textField: "DepartmentName"
        }
    );
};

const LoadModalSelection = (departmentSelect, roleSelect) => {
    const deptPromise = createSelectOptions(
        departmentSelect,
        {
            url: UB + "/Admin/GetDepartments",
            placeholder: "Department",
            valueField: "DepartmentId",
            textField: "DepartmentName"
        }
    );

    const rolePromise = createSelectOptions(
        roleSelect,
        {
            url: UB + "/Admin/GetRoles",
            placeholder: "Role",
            valueField: "RoleId",
            textField: "RoleName"
        }
    );

    return Promise.all([deptPromise, rolePromise]);
};

const selectOptionByText = (selectElement, targetText) => {
    if (!targetText) return;
    const cleanTarget = targetText.toString().trim().toLowerCase();
    selectElement.find('option').each(function () {
        if ($(this).text().trim().toLowerCase() === cleanTarget) {
            $(this).prop('selected', true);
            return false;
        }
    });
};

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
                url: UB + "/Admin/CreateUser",
                type: "POST",
                contentType: "application/json; charset=utf-8",
                headers: {
                    "RequestVerificationToken": token
                },
                data: JSON.stringify(form),
                success: function (response) {
                    hideLoading();
                    if (response.success) {
                        showAlertWithCallback("success", response.message || "User created successfully!", () => {
                            $("#UserManagementTable").DataTable.ajax.reload();
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
};

const UpdateUser = (forms) => {
    let form = forms;
    showConfirm(
        "Confirm Submission",
        "Are you sure you want to update this user?",
        "Yes, Update",
        "Cancel",
        () => {
            showLoading("Updating user details...");
            $.ajax({
                url: UB + "/Admin/UpdateUser",
                type: "POST",
                contentType: "application/json; charset=utf-8",
                headers: {
                    "RequestVerificationToken": token
                },
                data: JSON.stringify(form),
                success: function (response) {
                    hideLoading();
                    if (response.success) {
                        showAlertWithCallback("success", response.message || "User updated successfully!", () => {
                            $('#UpdateUserModal').modal('hide');
                            if (response.redirectUrl) {
                                window.location.href = UB + response.redirectUrl;
                            } else if ($.fn.DataTable.isDataTable("#UserManagementTable")) {
                                $("#UserManagementTable").DataTable().ajax.reload(null, false);
                            }
                        });
                    } else {
                        showAlert("error", response.message || "An error occurred while updating the user.");
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

const DeleteUser = (rowid) => {
    showConfirm(
        "Confirm Delete",
        "Are you sure you want to delete this user?",
        "Yes, Delete",
        "Cancel",
        () => {
            showLoading("Deleting User...");
            $.ajax({
                url: UB + "/Admin/DeleteUser",
                type: "POST",
                data: {
                    id: rowid,
                    __RequestVerificationToken: token
                },
                success: function (response) {
                    hideLoading();
                    if (response.success) {
                        showAlertWithCallback("success", response.message || "User deleted successfully!", () => {
                            window.location.href = UB + (response.redirectUrl || "/Admin/Users");
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
};

const ResetPassword = (rowid) => {
    showConfirm(
        "Confirm Reset",
        "Are you sure you want to reset the password of this user?",
        "Yes, Reset",
        "Cancel",
        () => {
            showLoading("Resetting password...");
            $.ajax({
                url: UB + "/Admin/ResetPassword",
                type: "POST",
                data: {
                    id: rowid,
                    __RequestVerificationToken: token
                },
                success: function (response) {
                    hideLoading();
                    if (response.success) {
                        showAlertWithCallback("success", response.message || "Password reset successfully!", () => {
                            window.location.href = UB + (response.redirectUrl || "/Admin/Users");
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
};

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
    let currentEditUser = null;

    LoadDepartmentSelection();
   
    const table = LoadUserTable();
    $('#departmentFilter').on('change', function () {
        table.draw();
    });

    $('#AddUserModal').on('show.bs.modal', function () {
        const department_select = $("#department_select");
        const role_select = $("#role_select"); 
        LoadModalSelection(department_select, role_select);
        $('#AddUserForm')[0]?.reset();
    });

    $('#AddUserForm').on('submit', function (e) {
        e.preventDefault();
        const formData = {
            EmployeeID: $('#id_input').val()?.trim(),
            EmployeeName: $('#name_input').val()?.trim(),
            Email: $('#email_input').val()?.trim(),
            Department: $('#department_select').val(),
            Role: $('#role_select').val()
        };
        CreateUser(formData);
    });

    $(document).on('click', '.btn-edit-user', function () {
        const tr = $(this).closest('tr');
        const rowData = table.row(tr).data();
        if (rowData) {
            currentEditUser = rowData;
        }
    });

    $('#UpdateUserModal').on('show.bs.modal', function (event) {
        const triggerBtn = $(event.relatedTarget);
        if (triggerBtn.length) {
            const tr = triggerBtn.closest('tr');
            const rowData = table.row(tr).data();
            if (rowData) {
                currentEditUser = rowData;
            }
        }

        if (!currentEditUser) return;

        $('#employeeName').val(currentEditUser.EmployeeName || '');
        $('#employeeId').val(currentEditUser.EmployeeID || '');
        $('#emailadd').val(currentEditUser.Email || '');

        const department_select = $("#departmentid");
        const role_select = $("#roleid");

        LoadModalSelection(department_select, role_select).then(() => {
            selectOptionByText(department_select, currentEditUser.Department);
            selectOptionByText(role_select, currentEditUser.Role);
        });
    });

    $('#UpdateUserForm').on('submit', function (e) {
        e.preventDefault();
        if (!currentEditUser || !currentEditUser.Id) {
            showAlert("error", "User identifier is missing. Please select a user to update.");
            return;
        }

        const formData = {
            Id: currentEditUser.Id,
            EmployeeID: $('#employeeId').val()?.trim(),
            EmployeeName: $('#employeeName').val()?.trim(),
            Email: $('#emailadd').val()?.trim(),
            Department: $('#departmentid').val(),
            Role: $('#roleid').val()
        };
        UpdateUser(formData);
    });

    $(document).on('click', '.btn-delete-ticket', function () {
        let dataId = $(this).data("id");
        DeleteUser(dataId);
    });

    $(document).on('click', '.btn-reset-password', function () {
        let dataId = $(this).data("id");
        ResetPassword(dataId);
    });
});
