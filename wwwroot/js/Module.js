

const LoadModulePage = () => {
    return createDataTable(
        "#moduleTable",
        {
            url: UB + "/Admin/GetModules",
            searchPlaceholder: "Search Module ID...",
            order: [[0, 'asc']],
            columns: [
                TableColumnsConfig.Text("Id"),
                TableColumnsConfig.Text("ModuleName"),
                TableColumnsConfig.Text("ModuleDescription"),
                TableColumnsConfig.StatusActive("IsActive"),
                TableColumnsConfig.StatusToggle("IsActive"),
                TableColumnsConfig.Actions(function (data, type, row) {
                    return `
                    <div class="dropdown">
                        <button class="btn btn-sm btn-light border-0" type="button" data-bs-toggle="dropdown" aria-expanded="false" title="Actions">
                            <i class="bi bi-three-dots-vertical"></i>
                        </button>
                        <ul class="dropdown-menu dropdown-menu-end shadow-sm border-0">
                            <li>
                                <a class="dropdown-item btn-edit-module" href="#" data-id="${row.Id || row.ModuleName}">
                                    <i class="bi bi-pencil-square text-primary me-2"></i> Edit
                                </a>
                            </li>
                            <li><hr class="dropdown-divider"></li>
                            <li>
                                <a class="dropdown-item text-danger btn-delete-module" href="#" data-id="${row.Id || row.ModuleName}">
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

const LoadModuleSelection = () => {
    createSelectOptions(
        "#moduleFilter",
        {
            url: UB + "/Admin/GetModulesSelection",
            placeholder: "Module",
            valueField: "ModuleId",
            textField: "ModuleName"
        }
    )
};

const ChangeStatus = (toggleSwitch) => {

    let moduleId = toggleSwitch.data("id");
    let isNowActive = toggleSwitch.is(":checked");

    let actionText = isNowActive ? "ON" : "OFF";
    let warningText = isNowActive ? "User with permission will immediately see this module."
        : "This will instanly hide the module from ALL users!";

    showConfirm(
        `Are you sure you want to turn ${actionText} this module?`,
        warningText,
        `Yes, turn ${actionText}`,
        "Cancel",
        () => {
            showLoading(`Modult turning ${actionText}...`);
            $.ajax({
                url: UB + "/Admin/UpdateModuleStatus",
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify({
                    Id: moduleId,
                    IsActive: isNowActive,
                    __RequestVerificationToken: token
                }),
                success: function (response) {
                    hideLoading();
                    if (response.success) {
                        showAlertWithCallback("success", response.message, () => {
                            toggleSwitch.prop('checked', isNowActive);
                            $("#moduleTable").DataTable().ajax.reload();
                        });
                    } else {
                        showAlert("error", response.message || "An error occurred while switching status.");
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
    toggleSwitch.prop("checked", !isNowActive);
}

const CreateNewModule = (form) => {
    showConfirm(
        "Confirm Submission",
        "Are you sure you want to create this module?",
        "Yes, Create",
        "Cancel",
        () => {
            showLoading("Creating Module...");
            $.ajax({
                url: UB + "/Admin/CreateModule",
                type: "POST",
                contentType: "application/json; charset=utf-8",
                data: JSON.stringify(form),
                success: function (response) {
                    hideLoading();
                    if (response.success) {
                        showAlertWithCallback("success", response.message || "Module created successfully!", () => {
                            $("#moduleTable").DataTable().ajax.reload();
                        });
                    } else {
                        showAlert("error", response.message || "An error occurred while creating the module.");
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
    if (settings.nTable.id !== 'moduleTable') {
        return true;
    }

    const selectedVal = $('#moduleFilter').val();
    if (!selectedVal || selectedVal === "") {
        return true;
    }

    const selectedModule = $('#moduleFilter option:selected').text().toLowerCase().trim();

    let rowModule = (rowData.ModuleName || data[2] || '').toString().toLowerCase().trim();
    return rowModule === selectedModule;
});

$(function () {
    LoadModuleSelection();

    const table = LoadModulePage();
    $('#moduleFilter').on('change', function () {
        table.draw();
    });

    $("#moduleTable").on("change", ".module-toggle-btn", function () {
        let toggleSwitch = $(this);
        console.log(toggleSwitch);
        ChangeStatus(toggleSwitch);
    });

    $('#moduleModal').on('show.bs.modal', function () {
        $('#creatModuleForm').on('submit', function (e) {
            e.preventDefault();

            const formData = {
                ModuleName: $("#module_name").val(),
                ModuleDescription: $("#description").val()
            };

           CreateNewModule(formData);
        });
    });
});