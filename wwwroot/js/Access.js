const LoadPermissionTable = () => {
    return createDataTable(
        "#permissionTable",
        {
            url: "/Admin/GetPermissionList",
            searchPlaceholder: "Search Module ID...",
            order: [[0, 'asc']],
            columns: [
                TableColumnsConfig.Text("Role_name"),
                TableColumnsConfig.Text("Module_name"),
                TableColumnsConfig.Checkbox("Can_view"),
                TableColumnsConfig.Checkbox("Can_create"),
                TableColumnsConfig.Checkbox("Can_edit"),
                TableColumnsConfig.Checkbox("Can_delete"),
                TableColumnsConfig.Checkbox("Can_approve"),
                TableColumnsConfig.Actions(function (data, type, row) {
                    return `
                    <div class="dropdown">
                        <button class="btn btn-sm btn-light border-0" type="button" data-bs-toggle="dropdown" aria-expanded="false" title="Actions">
                            <i class="bi bi-three-dots-vertical"></i>
                        </button>
                        <ul class="dropdown-menu dropdown-menu-end shadow-sm border-0">
                            <li>
                                <a class="dropdown-item btn-save-permission" href="#" data-id="${row.Permission_id || row.Module_name}">
                                    <i class="bi bi-save text-primary me-2"></i> Edit
                                </a>
                            </li>
                            <li><hr class="dropdown-divider"></li>
                            <li>
                                <a class="dropdown-item text-danger btn-delete-permission" href="#" data-id="${row.Permission_id || row.Module_name}">
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

const ChangeStatus = (checkBox) => {

    let permissionId = checkBox.data("id");
    let fieldName = checkBox.data("field");
    let isNowActive = checkBox.is(":checked");

    let actionText = isNowActive ? "Enable" : "Disable";
    let warningText = isNowActive
        ? "User with permission will grant permission to this action."
        : "This user cannot perform this action!";

    showConfirm(
        `Are you sure you want to ${actionText} this action?`,
        warningText,
        `Yes, turn ${actionText}`,
        "Cancel",
        () => {
            showLoading(`${actionText} action...`);

            $.ajax({
                url: "/Admin/UpdatePermissionAction",
                type: "POST",
                data: {
                    id: permissionId,
                    action: fieldName,
                    isActive: isNowActive,
                    __RequestVerificationToken: token
                },
                success: function (response) {
                    hideLoading();
                    if (response.success) {
                        showAlertWithCallback("success", response.message, () => {
                            checkBox.prop('checked', isNowActive);
                            $("#permissionTable").DataTable().ajax.reload(null, false); // Keep paging position
                        });
                    } else {
                        checkBox.prop("checked", !isNowActive);
                        showAlert("error", response.message || "An error occurred while switching status.");
                    }
                },
                error: function (xhr, status, error) {
                    hideLoading();
                    checkBox.prop("checked", !isNowActive);

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
        },
        () => {
            checkBox.prop("checked", !isNowActive);
        }
    );
    checkBox.prop("checked", !isNowActive);
};

const LoadRoleSelection = () => {
    createSelectOptions(
        "#role_select",
        {
            url: "/Admin/GetRoles",
            placeholder: "Role",
            valueField: "RoleId",
            textField: "RoleName"
        }
    )
};

const CreateNewPermission = (form) => {
    showConfirm(
        "Confirm Submission",
        "Are you sure you want to create this permission?",
        "Yes, Create",
        "Cancel",
        () => {
            showLoading("Creating permission...");
            $.ajax({
                url: "/Admin/CreatePermission",
                type: "POST",
                contentType: "application/json; charset=utf-8",
                data: JSON.stringify(form),
                success: function (response) {
                    hideLoading();
                    if (response.success) {
                        showAlertWithCallback("success", response.message || "Permission created successfully!", () => {
                            $("#permissionTable").DataTable().ajax.reload();
                            $("#permissionForm")[0].reset();
                        });
                    } else {
                        showAlert("error", response.message || "An error occurred while creating the permission.");
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

const LoadSelection = () => {
    createSelectOptions(
        "#roleSelect",
        {
            url: "/Admin/GetRoles",
            placeholder: "Role",
            valueField: "RoleId",
            textField: "RoleName"
        }
    )

    createSelectOptions(
        "#moduleSelect",
        {
            url: "/Admin/GetModulesSelection",
            placeholder: "Module",
            valueField: "ModuleId",
            textField: "ModuleName"
        }
    )
}

$.fn.dataTable.ext.search.push(function (settings, data, dataIndex, rowData) {
    if (settings.nTable.id !== 'permissionTable') {
        return true;
    }

    const selectedVal = $('#role_select').val();
    if (!selectedVal || selectedVal === "") {
        return true;
    }

    const selectedDepartment = $('#role_select option:selected').text().toLowerCase().trim();

    let rowDepartment = (rowData.Role_name || data[0] || '').toString().toLowerCase().trim();
    return rowDepartment === selectedDepartment;
});


$(function () {
    LoadRoleSelection();
    
    const table = LoadPermissionTable();
    $('#role_select').on('change', function () {
        table.draw();
    });

    $("#permissionTable").on("change", ".permission-checkbox", function () {
        let checkBox = $(this);
        console.log(checkBox.data("id"));
        console.log(checkBox.data("field"));
        ChangeStatus(checkBox);
    });

    $('#permissionModal').on('show.bs.modal', function () {
        LoadSelection();

        $('#permissionForm').on('submit', function (e) {
            e.preventDefault();
            const formdata = {
                Role_id: $('#roleSelect').val(),
                Module_id: $('#moduleSelect').val(),
                Can_view: $('#canView').is(":checked"),
                Can_edit: $('#canEdit').is(":checked"),
                Can_delete: $('#canDelete').is(":checked"),
                Can_approve: $('#canApprove').is(":checked"),
                Can_create: $('#canCreate').is(":checked"),
                SelectAll: $('#toggleAllPermissions').is(":checked")
            };
            CreateNewPermission(formdata);
        });

    });
});