$(document).ready(function () {
    const urlParams = new URLSearchParams(window.location.search);
    const isExpanded = urlParams.get('Render') === 'Expanded';
    const exportTitle = `${abp.currentTenant.name}_${(new Date()).toISOString().slice(0, 10)}_Permission-Role Matrix`;

    const userRoles = abp.currentUser.roles;
    const userPolicies = abp.auth.grantedPolicies;

    let _permissionsModal = new abp.ModalManager(
        abp.appPath + 'AbpPermissionManagement/PermissionManagementModal'
    );
    _permissionsModal.onClose(function () {
        // Refresh the page to show updated permissions
        window.location.reload();
    });

    const roleColumnIndexes = [];
    $('#permissionTable th[data-role-header]').each(function (index) {
        roleColumnIndexes.push($(this).index());
    });

    const columnDefs = [
        {
            targets: 0,
            orderable: false,
            className: 'notexport'
        }
    ];

    if (roleColumnIndexes.length > 0) {
        columnDefs.push({
            targets: roleColumnIndexes,
            orderable: false
        });
    }

    $.fn.dataTable.Buttons.defaults.dom.button.className = 'btn flex-none';
    let localTable = $('#permissionTable').DataTable({
        paging: false,
        searching: true,
        ordering: true,
        fixedHeader: {
            header: true,
            footer: false,
            headerOffset: 0
        },
        scrollX: true,
        scrollCollapse: true,
        dom: 'Blfrtip',
        columnDefs: columnDefs,
        buttons: [
            {
                text: isExpanded
                    ? '<i class="fl fl-back-to-window align-middle"></i> <span>View Simple</span>'
                    : '<i class="fl fl-fullscreen align-middle"></i> <span>View Expanded</span>',
                className: 'btn-light rounded-1',
                action: function (e, dt, button, config) {
                    window.location = isExpanded
                        ? '/Identity/Roles/PermissionRoleMatrix'
                        : '/Identity/Roles/PermissionRoleMatrix?Render=Expanded';
                }
            },
            {
                extend: 'copy',
                text: 'Copy',
                title: exportTitle,
                className: 'custom-table-btn flex-none btn btn-secondary',
                exportOptions: {
                    columns: ':visible:not(.notexport)'
                }
            },
            {
                extend: 'csv',
                text: 'Export',
                title: exportTitle,
                className: 'custom-table-btn flex-none btn btn-secondary',
                exportOptions: {
                    columns: ':visible:not(.notexport)'
                }
            }
        ],
        createdRow: function (row, data, dataIndex) {
            $('td', row).each(function () {
                let cellText = $(this).text().trim();

                // Check if the cell text matches a key in userPolicies
                if (userPolicies[cellText]) {
                    $(this).addClass('text-decoration-underline');
                }

                if (cellText === "TRUE") {
                    $(this).addClass('bg-success text-dark bg-opacity-25 fw-bold border border-success dt-center');
                }
            });
        },
        initComplete: function () {
            const table = this.api();

            // Required for DataTable 1.x - Disable sorting for role name columns after table initialization
            // Explicitly disable ordering for role columns using the API
            $('th[data-role-header]').removeClass('sorting').addClass('sorting_disabled');

            const headers = table.columns().header().toArray().map(header => $(header).text().trim());

            // Highlight columns where the header matches a user role
            headers.forEach((header, index) => {
                if (userRoles.includes(header.toLowerCase())) {
                    $(table.column(index).header()).addClass('text-decoration-underline');
                    table.column(index).nodes().to$().filter(function () {
                        return !$(this).text().trim();
                    }).addClass('bg-light');
                }
            });
        }
    });

    localTable.buttons().container().prependTo('#dynamicButtonContainerId');
    $("#search").on('input', function () {
        localTable.search($(this).val()).draw();
    });

    // Hide spinner and show table after initialization
    $('.loading-spinner').hide();
    $('#permissionTable').show();

    // Add click handlers to role column headers using data-role-header attribute
    $(document).on('click', 'th[data-role-header]', function () {
        const roleName = $(this).attr('data-role-header');
        _permissionsModal.open({
            providerName: 'R',
            providerKey: roleName,
            providerKeyDisplayName: roleName
        });
    });

    // Add styles to all headers, including dynamically created ones
    $(document).on('mouseover', 'th[data-role-header]', function () {
        $(this).css('cursor', 'pointer');
        if (!$(this).attr('title')) {
            $(this).attr('title', 'Click to manage permissions for this role');
        }
    });

    init(localTable);
    // Workaround - required until Datatables.net 2.x
    $('th[data-role-header]').removeClass('sorting').addClass('sorting_disabled');
});
