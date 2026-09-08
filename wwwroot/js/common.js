/**
 * Reusable DataTables Builder
 * @param {string} tableId - Target table selector (e.g., "#TripScheduleTable")
 * @param {object} options - Custom configuration overrides
 */

/**
 * Dynamic Select Options Generator
 * @param {string} selectId - Selector for the dropdown (e.g., "#DriverId")
 * @param {object} options - Configuration options
 */

/**
 * Checks if the current authenticated user has a specific role.
 * @param {string} roleName - The exact name of the role to check.
 * @returns {boolean}
 */

const createDataTable = (tableId, options = {}) => {
    const searchPlaceholder = options.searchPlaceholder || "Search...";
    const defaultConfig = {
        destroy: true,
        searching: true,
        order: [[1, 'desc']],
        pageLength: 10,
        lengthChange: true,
        responsive: true,
        dom: "<'row mb-3 align-items-center'<'col-md-3'l><'col-md-5'B><'col-md-4'f>>" +
            "<'row'<'col-sm-12'tr>>" +
            "<'row mt-3 align-items-center'<'col-md-5'i><'col-md-7'p>>",
        buttons: [
            {
                extend: 'excel', className: 'btn btn-sm btn-success', text: '<i class="bi bi-file-earmark-excel me-1"></i> Export Excel', exportOptions: {
                    columns: ':not(:last-child)'
                } },
            {
                extend: 'csv', className: 'btn btn-sm btn-secondary', text: '<i class="bi bi-filetype-csv me-1"></i> CSV', exportOptions: {
                    columns: ':not(:last-child)'
                }
            }
        ],
        ajax: typeof options.url === 'string' ? {
            url: options.url,
            dataSrc: (json) => Array.isArray(json) ? json : (json.data || []),
            error: (xhr) => {
                let errorMessage = "An unknown error occurred.";
                if (xhr.responseText) {
                    try {
                        let res = JSON.parse(xhr.responseText);
                        errorMessage = res.error || res.message || errorMessage;
                    } catch (ex) {
                        console.error("Could not parse error response", ex);
                    }
                }
                showAlert("error", errorMessage);
            }
        } : options.ajax,
        language: {
            search: "",
            searchPlaceholder: searchPlaceholder,
            lengthMenu: "Show _MENU_ entries",
            emptyTable: "<div class='py-4 text-muted'><i class='bi bi-inbox display-6 d-block mb-2'></i>No records found.</div>",
            zeroRecords: "No matching records found.",
            loadingRecords: "<div class='py-4'><div class='spinner-border text-primary' role='status'></div><p class='mt-2'>Loading requests...</p></div>"
        },
        initComplete: function () {
            const api = this.api();
            $(`${tableId}_filter input`).off('.DT').on('input.DT', function () {
                api.search(this.value).draw();
            });
        },
    };

    const finalConfig = $.extend(true, {}, defaultConfig, options);
    return $(tableId).DataTable(finalConfig);
}

const App = {
    initModernDatepicker: function () {
        const datepickerElement = document.querySelectorAll(".modern-datepicker");
        if (datepickerElement) {
            flatpickr(datepickerElement, {
                dateFormat: "Y-m-d", // Formats the output (e.g., 2026-06-16)
                allowInput: true,
                //mode: "range" // Uncomment if you need date ranges
            });
        }
    },

    configureToast: function () {
        toastr.options = {
            closeButton: true,
            debug: false,
            newestOnTop: true,
            progressBar: true,
            positionClass: "toast-top-center",
            preventDuplicates: true,
            onclick: null,
            showDuration: "2000",
            hideDuration: "1000",
            timeOut: "3000"
        }
    },

    showAjaxError: function (xhr, error, code) {
        let errormessage = "An Unknown error occured.";
        if (xhr.responseText) {
            try {
                let response = JSON.parse(xhr.responseText);
                errormessage = response.error || response.message || errormessage;
            } catch (ex) {
                console.error("Could not parse error response");
            }
        }

        toastr.error(errormessage, "System error");
    },
}

const showToast = (type, message) => {
    switch (type) {
        case 'success':
            toastr.success(message, "Success");
            break;
            case 'error':
            toastr.error(message, "Error");
            break;
            case 'info':
            toastr.info(message, "Info");
            break;
            case 'warning':
            toastr.warning(message, "Warning");
            break;
            default:
            toastr.info(message, "Info");
    }
};

const showAlert = (type, message) => {
    Swal.fire({
        title: type.charAt(0).toUpperCase() + type.slice(1),
        text: message,
        icon: type,
        confirmButtonColor: '#0d6efd'
    });
}

const showAlertWithCallback = (type, message, callback) => {
    Swal.fire({
        title: type.charAt(0).toUpperCase() + type.slice(1),
        text: message,
        icon: type,
        confirmButtonColor: '#0d6efd'
    }).then(() => {
        callback();
    });
}

const showConfirm = (title, message, confirmButtonText, cancelButtonText, confirmCallback) => {
    Swal.fire({
        title: title,
        text: message,
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: '#0d6efd',
        cancelButtonColor: '#dc3545',
        confirmButtonText: confirmButtonText || 'Yes',
        cancelButtonText: cancelButtonText || 'No',
        backdrop: `rgba(0,0,12,0.4)`
        }).then((result) => {
            if (result.isConfirmed) {
                confirmCallback();
            }
        }
    );
}

const showLoading = (message) => {
    Swal.fire({
        title: message || 'Loading...',
        allowOutsideClick: false,
        didOpen: () => {
            Swal.showLoading();
        }
        });
}

const hideLoading = () => {
    Swal.close();
}

const showToastFromServer = (type, message) => {
    if (type && message) {
        showToast(type, message);
    }
}

const formattedDateshort = (dateString) => {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
}

const TableColumnsConfig = {
    Text: (field) => {
        return {
            data: field,
            className: 'text-center fw-medium',
        }
    },
    Passenger: (field) => {
        return {
            data: field,
            defaultContent: 'N/A',
            className: 'text-center fw-medium',
            render: function (data, type, row) {
                if (!data) return 'N/A';
                if (typeof data === 'object' && !Array.isArray(data)) {
                    return data.PassengerName || 'N/A';
                }

                if (Array.isArray(data) && data.length > 0) {
                    return data.map(p => p.PassengerName).join(', ');
                }

                return 'N/A';
            }
        }
    },
    Number: (field) => {
        return {
            data: field,
            render: $.fn.dataTable.render.number(),
        }
    },
    Date: (field) => {
        return {
            data: field,
            render: function (data, type, row) {
                if (!data) return '<span class="badge bg-secondary">No Data</span>';
                return formattedDateshort(data);
            }
        }
    },
    Time: (field) => {
        return {
            data: field,
            render: function (data, type, row) {
                if (!data) return '<span class="badge bg-secondary">No Data</span>';
                const time = new Date(`1970-01-01T${data}`);
                return time.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
            }
        }
    },
    Status: (field) => {
        return {
            data: field,
            render: function (data) {
                if (!data) return `<span class="badge bg-secondary-subtle text-secondary px-2 py-1">N/A</span>`;

                // Map statuses directly to Bootstrap color schemes & human-readable labels
                const statusMap = {
                    'Pending Section': { color: 'warning', label: 'Pending Section' },
                    'Pending GA Approve': { color: 'info', label: 'Pending GA Approve' },
                    'GA Approved': { color: 'success', label: 'GA Approved' },
                    'Ready for Dispatch': { color: 'warning', label: 'Ready for Dispatch' }, // Distinct primary blue
                    'Rejected': { color: 'danger', label: 'Rejected' },
                    'Completed': { color: 'success', label: 'Completed' },
                    'In Transit': { color: 'primary', label: 'In Transit' }
                };

                const config = statusMap[data] || { color: 'secondary', label: data };

                return `
                    <span class="badge bg-${config.color} bg-opacity-10 text-${config.color} border border-${config.color} border-opacity-25 px-2 py-1">
                        ${config.label}
                    </span>
                `;
            }
        }
    },
    VehicleStatus: (field) => {
        return {
            data: field,
            render: function (data) {
                if (data === 'Available') {
                    return `<span class="badge bg-success bg-opacity-10 text-success border border-success border-opacity-25 px-2 py-1">Available</span>`;
                }
                else if (data === 'In Transit') {
                    return `<span class="badge bg-warning bg-opacity-10 text-warning border border-warning border-opacity-25 px-2 py-1">In Transit</span>`;
                }
                else if (data === 'Assigned') {
                    return `<span class="badge bg-info bg-opacity-10 text-info border border-info border-opacity-25 px-2 py-1">Assigned</span>`;
                }
                else if (data === 'Under Maintenance') {
                    return `<span class="badge bg-danger bg-opacity-10 text-danger border border-danger border-opacity-25 px-2 py-1">Under Maintenance</span>`;
                }
                else if (data === 'Decommissioned') {
                    return `<span class="badge bg-danger bg-opacity-10 text-danger border border-danger border-opacity-25 px-2 py-1">Decommissioned</span>`;
                }
                return `<span class="badge bg-secondary px-2 py-1">${data}</span>`;
            }
        }
    },
    DriverStatus: (field) => {
        return {
            data: field,
            render: function (data) {
                const status = String(data || 'Not specified');
                const isActive = status.toLowerCase() === 'active' || status.toLowerCase() === 'available';
                const badgeClass = isActive
                    ? 'bg-success bg-opacity-10 text-success border border-success border-opacity-25'
                    : 'bg-secondary bg-opacity-10 text-secondary border border-secondary border-opacity-25';
                return `<span class="badge ${badgeClass} px-2 py-1">${status}</span>`;
            }
        };
    },
    Actions: (renderFunction) => {
        return {
            data: null,
            defaultContent: "",
            searchable: false,
            orderable: false,
            className: 'text-center',
            render: renderFunction || function () {
                return '';
            }
        };
    },
    StatusToggle: (field) => {
        return {
            data: field,
            className: 'text-center align-middle',
            render: function (data, type, row) {
                let isActive = (data === true || data === 1 || data === 'true');
                let checkedAttribute = isActive ? 'checked' : '';

                return `
                    <div class="form-check form-switch d-flex justify-content-center">
                        <input class="form-check-input module-toggle-btn shadow-sm" 
                               type="checkbox" 
                               role="switch" 
                               data-id="${row.Id}" 
                               ${checkedAttribute} 
                               style="cursor: pointer; transform: scale(1.3);">
                    </div>
                `;
            }
        };
    },
    StatusActive: function (field) {
        return {
            data: field,
            render: function (data) {
                let isActive = (data === true || data === 1 || data === 'true');
                let badgeClass = isActive ? 'bg-success' : 'bg-secondary';
                let statusText = isActive ? 'Active' : 'Inactive';
                return `<span class="badge ${badgeClass}">${statusText}</span>`;
            }
        };
    },
    Checkbox: function (dataField) {
        return {
            data: dataField,
            className: 'text-center align-middle', // Keeps the checkbox perfectly centered
            searchable: false, // You don't want users searching for "true" or "false"
            render: function (data, type, row) {

                if (type === 'display') {
         
                    let isChecked = data ? 'checked' : '';
                    return `
                        <input class="form-check-input permission-checkbox border-secondary" 
                               type="checkbox" 
                               ${isChecked} 
                               data-id="${row.Permission_id}" 
                               data-field="${dataField}" 
                               style="cursor: pointer; transform: scale(1.2);">
                    `;
                }
                return data;
            }
        };
    }
}

//const createSelectOptions = (selectId, options = {}) => {
//    const selectElement = $(selectId);
//    if (!selectElement.length) return;

//    const settings = {
//        url: "",
//        placeholder: "Option",
//        valueField: "id",
//        textField: "name",
//        selectedValue: null,
//        dataSrc: (res) => res.data || res,
//        onChange: null
//    };

//    $.extend(settings, options);

//    if (!settings.url) {
//        console.error("populateSelect error: 'url' parameter is required.");
//        return;
//    }

//    selectElement.html(`<option value="">Loading ${settings.placeholder}s...</option>`).prop("disabled", true);

//    return $.ajax({
//        type: "GET",
//        url: settings.url,
//        success: function (response) {
//            const items = settings.dataSrc(response) || [];

//            selectElement.empty().prop("disabled", false);
//            selectElement.append(`<option value="">-- Select ${settings.placeholder}</option>`);

//            $.each(items, function (index, item) {
//                const val = item[settings.valueField];
//                const text = item[settings.textField];
//                const isSelected = settings.selectedValue && val == settings.selectedValue ? "selected" : "";
//                selectElement.append(`<option value="${val}" ${isSelected}>${text}</option>`);
//            });

//            if (typeof settings.onChange === "function") {
//                selectElement.off("change.populate").on("change.populate", settings.onChange);
//            }
//        },
//        error: function (xhr) {
//            selectElement.html(`<option value="">Failed to load ${settings.placeholder}s</option>`).prop("disabled", false);
//            console.error(`Failed to populate ${selectId}:`, xhr);
//        }
//    });
//}


const createSelectOptions = (selectId, options = {}) => {
    const selectElement = $(selectId);
    if (!selectElement.length) return;

    const settings = {
        url: "",
        placeholder: "Option",
        valueField: "id",
        textField: "name",
        selectedValue: null,
        dataSrc: (res) => res.data || res,
        // Smart adapter: Automatically handles List vs Dictionary
        formatItem: (keyOrIndex, item, settings) => {
            // Case 1: Simple Dictionary { "101": "Apple", "102": "Banana" }
            if (typeof item !== "object" || item === null) {
                return { value: keyOrIndex, text: item };
            }
            // Case 2: Array List or Nested Dictionary [{ id: 1, name: "Apple" }]
            return {
                value: item[settings.valueField],
                text: item[settings.textField]
            };
        },
        onChange: null
    };

    $.extend(settings, options);

    if (!settings.url) {
        console.error("populateSelect error: 'url' parameter is required.");
        return;
    }

    selectElement.html(`<option value="">Loading ${settings.placeholder}s...</option>`).prop("disabled", true);

    return $.ajax({
        type: "GET",
        url: settings.url,
        success: function (response) {
            const items = settings.dataSrc(response) || [];

            selectElement.empty().prop("disabled", false);
            selectElement.append(`<option value="">-- Select ${settings.placeholder} --</option>`);

            $.each(items, function (keyOrIndex, item) {
                const { value, text } = settings.formatItem(keyOrIndex, item, settings);
                const isSelected = settings.selectedValue && value == settings.selectedValue ? "selected" : "";

                selectElement.append(`<option value="${value}" ${isSelected}>${text}</option>`);
            });

            if (typeof settings.onChange === "function") {
                selectElement.off("change.populate").on("change.populate", settings.onChange);
            }
        },
        error: function (xhr) {
            selectElement.html(`<option value="">Failed to load ${settings.placeholder}s</option>`).prop("disabled", false);
            console.error(`Failed to populate ${selectId}:`, xhr);
        }
    });
};


const token = $('input[name="__RequestVerificationToken"]').val();

const hasRole = (roleName) => {
    const rolesAttr = document.body.dataset.userRoles || "";

    // Split by comma and trim whitespace to create a clean array of roles
    const userRoles = rolesAttr.split(',').map(role => role.trim());

    return userRoles.includes(roleName);
};



document.addEventListener('DOMContentLoaded', function () {
    App.initModernDatepicker();
    App.configureToast();
});
