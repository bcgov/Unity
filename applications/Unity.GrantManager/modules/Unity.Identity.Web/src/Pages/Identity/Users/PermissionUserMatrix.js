$(document).ready(function () {
    const urlParams = new URLSearchParams(globalThis.location.search);
    const isExpanded = urlParams.get('Render') === 'Expanded';
    const exportTitle = `${abp.currentTenant.name}_${(new Date()).toISOString().slice(0, 10)}_Permission-User Matrix`;

    const currentUserName = abp.currentUser.userName;

    let _permissionsModal = new abp.ModalManager(
        abp.appPath + 'AbpPermissionManagement/PermissionManagementModal'
    );
    _permissionsModal.onClose(function () {
        globalThis.location.reload();
    });

    const userColumnIndexes = [];
    $('#permissionUserTable th[data-user-header]').each(function () {
        userColumnIndexes.push($(this).index());
    });

    const columnDefs = [
        {
            targets: 0,
            orderable: true,
            className: 'notexport'
        }
    ];

    if (userColumnIndexes.length > 0) {
        columnDefs.push({
            targets: userColumnIndexes,
            orderable: false
        });
    }

    $.fn.dataTable.Buttons.defaults.dom.button.className = 'btn flex-none';
    let localTable = $('#permissionUserTable').DataTable({
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
                action: function () {
                    globalThis.location = isExpanded
                        ? '/Identity/Users/PermissionUserMatrix'
                        : '/Identity/Users/PermissionUserMatrix?Render=Expanded';
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
        createdRow: function (row) {
            $('td', row).each(function () {
                const cellText = $(this).text().trim();

                if (cellText === 'TRUE') {
                    $(this).addClass('bg-success text-dark bg-opacity-25 fw-bold border border-success dt-center');
                }
            });
        },
        initComplete: function () {
            const table = this.api();

            // Disable sorting on user columns
            $('th[data-user-header]').removeClass('sorting').addClass('sorting_disabled');

            const headers = table.columns().header().toArray().map(header => $(header).text().trim());

            // Highlight the current user's column
            headers.forEach((header, index) => {
                const $th = $(table.column(index).header());
                const userName = $th.attr('data-user-name');
                if (userName && userName.toLowerCase() === currentUserName?.toLowerCase()) {
                    $th.addClass('text-decoration-underline');
                    table.column(index).nodes().to$().filter(function () {
                        return !$(this).text().trim();
                    }).addClass('bg-light');
                }
            });
        }
    });

    localTable.buttons().container().prependTo('#dynamicButtonContainerId');

    $('#search').on('input', function () {
        localTable.search($(this).val()).draw();
    });

    // Hide spinner and show table after initialization
    $('.loading-spinner').hide();
    $('#permissionUserTable').show();

    // Click on user column header → open ABP permissions modal for that user
    $(document).on('click', 'th[data-user-header]', function () {
        const userId = $(this).attr('data-user-header');
        const userName = $(this).attr('data-user-name');
        _permissionsModal.open({
            providerName: 'U',
            providerKey: userId,
            providerKeyDisplayName: userName
        });
    });

    $(document).on('mouseover', 'th[data-user-header]', function () {
        $(this).css('cursor', 'pointer');
        if (!$(this).attr('title')) {
            $(this).attr('title', 'Click to manage permissions for this user');
        }
    });

    // Workaround - required until Datatables.net 2.x
    $('th[data-user-header]').removeClass('sorting').addClass('sorting_disabled');
});
