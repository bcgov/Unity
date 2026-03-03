$(function () {
    const nullPlaceholder = '—';
    const formatter = typeof createNumberFormatter === 'function' ? createNumberFormatter() : null;
    const formatCurrency = (val) => formatter ? formatter.format(val) : (val ?? nullPlaceholder);

    // ── Save button / dirty tracking ──────────────────────────────────────────
    const form = $('#ApplicantHistoryNotesForm');
    const saveBtn = $('#saveApplicantHistoryBtn');
    let zoneForm = null;

    if (form.length && saveBtn.length && typeof UnityZoneForm === 'function') {
        zoneForm = new UnityZoneForm(form, { saveButtonSelector: '#saveApplicantHistoryBtn' });
        zoneForm.init();
    }

    saveBtn.on('click', function (e) {
        e.preventDefault();
        if (!zoneForm || zoneForm.modifiedFields.size === 0) return;

        const applicantId = $('#ApplicantHistory_ApplicantId').val();
        if (!applicantId) {
            abp.notify.warn('Applicant identifier is missing.');
            return;
        }

        unity.grantManager.applicantProfile.applicantHistory
            .saveNotes(applicantId, {
                fundingHistoryComments: $('#FundingHistoryComments').val(),
                issueTrackingComments: $('#IssueTrackingComments').val(),
                auditComments: $('#AuditComments').val()
            })
            .done(function () {
                abp.notify.success('History notes saved.');
                zoneForm.resetTracking();
            })
            .fail(function () {
                abp.notify.error('Failed to save history notes.');
            });
    });

    // ── Column definitions ────────────────────────────────────────────────────

    function getFundingHistoryColumns() {
        return [
            { title: 'Grant Category', data: 'grantCategory', name: 'grantCategory', className: 'data-table-header', render: (d) => d ?? nullPlaceholder },
            { title: 'Funding Year', data: 'fundingYear', name: 'fundingYear', className: 'data-table-header', width: '80px', render: (d) => d ?? nullPlaceholder },
            { title: 'Renewed Funding', data: 'renewedFunding', name: 'renewedFunding', className: 'data-table-header', width: '110px', render: (d) => d === true ? 'Yes' : 'No' },
            { title: 'Approved Amount', data: 'approvedAmount', name: 'approvedAmount', className: 'data-table-header currency-display', render: (d) => formatCurrency(d) },
            { title: 'Reconsideration Amount', data: 'reconsiderationAmount', name: 'reconsiderationAmount', className: 'data-table-header currency-display', render: (d) => formatCurrency(d) },
            { title: 'Total Grant Amount', data: 'totalGrantAmount', name: 'totalGrantAmount', className: 'data-table-header currency-display', render: (d) => formatCurrency(d) },
            {
                title: 'Notes', data: 'fundingNotes', name: 'fundingNotes', className: 'data-table-header', width: '200px',
                createdCell: function (td) { $(td).css('min-width', '200px'); },
                render: (d) => d ?? nullPlaceholder
            },
            {
                title: 'Actions', data: null, name: 'actions', orderable: false, className: 'data-table-header', width: '70px',
                render: function (data, type, row) {
                    let $wrapper = $('<div>').addClass('d-flex align-items-center gap-2');

                    let $editBtn = $('<button>')
                        .addClass('btn btn-sm edit-button px-0 funding-edit-btn')
                        .attr({ 'aria-label': 'Edit', 'title': 'Edit' })
                        .append($('<i>').addClass('fl fl-edit'));

                    let $deleteBtn = $('<button>')
                        .addClass('btn btn-link p-0 funding-delete-btn')
                        .attr({ 'title': 'Delete Funding History', 'data-id': row.id })
                        .css({ 'color': '#0066cc', 'text-decoration': 'none' })
                        .append($('<i>').addClass('fa fa-times'));

                    $wrapper.append($editBtn).append($deleteBtn);
                    return $wrapper.prop('outerHTML');
                }
            }
        ].map(function (col, i) { col.index = i; col.targets = [i]; return col; });
    }

    function getIssueTrackingColumns() {
        return [
            { title: 'Year', data: 'year', name: 'year', className: 'data-table-header', width: '80px', render: (d) => d ?? nullPlaceholder },
            { title: 'Issue Heading', data: 'issueHeading', name: 'issueHeading', className: 'data-table-header', render: (d) => d ?? nullPlaceholder },
            { title: 'Issue Description', data: 'issueDescription', name: 'issueDescription', className: 'data-table-header', render: (d) => d ?? nullPlaceholder },
            { title: 'Resolved', data: 'resolved', name: 'resolved', className: 'data-table-header', width: '80px', render: (d) => d === true ? 'Yes' : 'No' },
            {
                title: 'Resolution Note', data: 'resolutionNote', name: 'resolutionNote', className: 'data-table-header', width: '200px',
                createdCell: function (td) { $(td).css('min-width', '200px'); },
                render: (d) => d ?? nullPlaceholder
            },
            {
                title: 'Actions', data: null, name: 'actions', orderable: false, className: 'data-table-header', width: '70px',
                render: function (data, type, row) {
                    let $wrapper = $('<div>').addClass('d-flex align-items-center gap-2');

                    let $editBtn = $('<button>')
                        .addClass('btn btn-sm edit-button px-0 issue-edit-btn')
                        .attr({ 'aria-label': 'Edit', 'title': 'Edit' })
                        .append($('<i>').addClass('fl fl-edit'));

                    let $deleteBtn = $('<button>')
                        .addClass('btn btn-link p-0 issue-delete-btn')
                        .attr({ 'title': 'Delete Issue Tracking', 'data-id': row.id })
                        .css({ 'color': '#0066cc', 'text-decoration': 'none' })
                        .append($('<i>').addClass('fa fa-times'));

                    $wrapper.append($editBtn).append($deleteBtn);
                    return $wrapper.prop('outerHTML');
                }
            }
        ].map(function (col, i) { col.index = i; col.targets = [i]; return col; });
    }

    function getAuditHistoryColumns() {
        return [
            { title: 'Tracking #', data: 'auditTrackingNumber', name: 'auditTrackingNumber', className: 'data-table-header', render: (d) => d ?? nullPlaceholder },
            {
                title: 'Audit Date', data: 'auditDate', name: 'auditDate', className: 'data-table-header',
                render: function (d) {
                    if (!d) return nullPlaceholder;
                    try {
                        return luxon.DateTime.fromISO(d, {
                            locale: abp.localization.currentCulture.name,
                        }).toUTC().toLocaleString();
                    } catch (e) { console.warn('Audit date parse error:', e); return d; }
                }
            },
            { title: 'Audit Note', data: 'auditNote', name: 'auditNote', className: 'data-table-header', render: (d) => d ?? nullPlaceholder },
            {
                title: 'Actions', data: null, name: 'actions', orderable: false, className: 'data-table-header', width: '70px',
                render: function (data, type, row) {
                    let $wrapper = $('<div>').addClass('d-flex align-items-center gap-2');

                    let $editBtn = $('<button>')
                        .addClass('btn btn-sm edit-button px-0 audit-edit-btn')
                        .attr({ 'aria-label': 'Edit', 'title': 'Edit' })
                        .append($('<i>').addClass('fl fl-edit'));

                    let $deleteBtn = $('<button>')
                        .addClass('btn btn-link p-0 audit-delete-btn')
                        .attr({ 'title': 'Delete Audit History', 'data-id': row.id })
                        .css({ 'color': '#0066cc', 'text-decoration': 'none' })
                        .append($('<i>').addClass('fa fa-times'));

                    $wrapper.append($editBtn).append($deleteBtn);
                    return $wrapper.prop('outerHTML');
                }
            }
        ].map(function (col, i) { col.index = i; col.targets = [i]; return col; });
    }

    // ── Modals ────────────────────────────────────────────────────────────────

    const getApplicantId = () => $('#ApplicantHistory_ApplicantId').val();

    const createFundingModal = new abp.ModalManager(abp.appPath + 'ApplicantHistory/CreateFundingHistoryModal');
    const editFundingModal = new abp.ModalManager(abp.appPath + 'ApplicantHistory/EditFundingHistoryModal');

    const createIssueModal = new abp.ModalManager(abp.appPath + 'ApplicantHistory/CreateIssueTrackingModal');
    const editIssueModal = new abp.ModalManager(abp.appPath + 'ApplicantHistory/EditIssueTrackingModal');

    const createAuditModal = new abp.ModalManager(abp.appPath + 'ApplicantHistory/CreateAuditHistoryModal');
    const editAuditModal = new abp.ModalManager(abp.appPath + 'ApplicantHistory/EditAuditHistoryModal');

    // ── DataTables ────────────────────────────────────────────────────────────

    const fundingHistoryTable = initializeDataTable({
        dt: $('#FundingHistoryTable'),
        defaultVisibleColumns: ['grantCategory', 'fundingYear', 'renewedFunding', 'approvedAmount', 'reconsiderationAmount', 'totalGrantAmount', 'fundingNotes', 'actions'],
        listColumns: getFundingHistoryColumns(),
        dataEndpoint: () => unity.grantManager.applicantProfile.applicantHistory.getFundingHistoryList(getApplicantId()),
        data: () => ({}),
        responseCallback: function (r) { return { recordsTotal: r.length, recordsFiltered: r.length, data: r }; },
        actionButtons: [
            {
                text: 'ADD',
                className: 'custom-table-btn flex-none btn btn-secondary',
                action: function () { createFundingModal.open({ applicantId: getApplicantId() }); }
            },
            {
                extend: 'csv',
                text: 'Export',
                className: 'custom-table-btn flex-none btn btn-secondary',
                exportOptions: { columns: ':visible:not(.notexport)' }
            }
        ],
        serverSideEnabled: false,
        pagingEnabled: true,
        dataTableName: 'FundingHistoryTable',
        dynamicButtonContainerId: 'fundingHistoryDynamicButtons'
    });

    if (fundingHistoryTable && typeof fundingHistoryTable.externalSearch === 'function') {
        fundingHistoryTable.externalSearch('#funding-history-search', { delay: 300 });
    }

    const issueTrackingTable = initializeDataTable({
        dt: $('#IssueTrackingTable'),
        defaultVisibleColumns: ['year', 'issueHeading', 'issueDescription', 'resolved', 'resolutionNote', 'actions'],
        listColumns: getIssueTrackingColumns(),
        dataEndpoint: () => unity.grantManager.applicantProfile.applicantHistory.getIssueTrackingList(getApplicantId()),
        data: () => ({}),
        responseCallback: function (r) { return { recordsTotal: r.length, recordsFiltered: r.length, data: r }; },
        actionButtons: [
            {
                text: 'ADD',
                className: 'custom-table-btn flex-none btn btn-secondary',
                action: function () { createIssueModal.open({ applicantId: getApplicantId() }); }
            },
            {
                extend: 'csv',
                text: 'Export',
                className: 'custom-table-btn flex-none btn btn-secondary',
                exportOptions: { columns: ':visible:not(.notexport)' }
            }
        ],
        serverSideEnabled: false,
        pagingEnabled: true,
        dataTableName: 'IssueTrackingTable',
        dynamicButtonContainerId: 'issueTrackingDynamicButtons'
    });

    if (issueTrackingTable && typeof issueTrackingTable.externalSearch === 'function') {
        issueTrackingTable.externalSearch('#issue-tracking-search', { delay: 300 });
    }

    const auditHistoryTable = initializeDataTable({
        dt: $('#AuditHistoryTable'),
        defaultVisibleColumns: ['auditTrackingNumber', 'auditDate', 'auditNote', 'actions'],
        listColumns: getAuditHistoryColumns(),
        dataEndpoint: () => unity.grantManager.applicantProfile.applicantHistory.getAuditHistoryList(getApplicantId()),
        data: () => ({}),
        responseCallback: function (r) { return { recordsTotal: r.length, recordsFiltered: r.length, data: r }; },
        actionButtons: [
            {
                text: 'ADD',
                className: 'custom-table-btn flex-none btn btn-secondary',
                action: function () { createAuditModal.open({ applicantId: getApplicantId() }); }
            },
            {
                extend: 'csv',
                text: 'Export',
                className: 'custom-table-btn flex-none btn btn-secondary',
                exportOptions: { columns: ':visible:not(.notexport)' }
            }
        ],
        serverSideEnabled: false,
        pagingEnabled: true,
        dataTableName: 'AuditHistoryTable',
        dynamicButtonContainerId: 'auditHistoryDynamicButtons'
    });

    if (auditHistoryTable && typeof auditHistoryTable.externalSearch === 'function') {
        auditHistoryTable.externalSearch('#audit-history-search', { delay: 300 });
    }

    // ── Modal result callbacks ─────────────────────────────────────────────────

    createFundingModal.onResult(function () {
        fundingHistoryTable.ajax.reload();
        abp.notify.success('Funding history record added.');
    });

    editFundingModal.onResult(function () {
        fundingHistoryTable.ajax.reload();
        abp.notify.success('Funding history record updated.');
    });

    createIssueModal.onResult(function () {
        issueTrackingTable.ajax.reload();
        abp.notify.success('Issue tracking record added.');
    });

    editIssueModal.onResult(function () {
        issueTrackingTable.ajax.reload();
        abp.notify.success('Issue tracking record updated.');
    });

    createAuditModal.onResult(function () {
        auditHistoryTable.ajax.reload();
        abp.notify.success('Audit history record added.');
    });

    editAuditModal.onResult(function () {
        auditHistoryTable.ajax.reload();
        abp.notify.success('Audit history record updated.');
    });

    // ── Edit / Delete — delegated handlers on table containers ────────────────

    $('#FundingHistoryTable').on('click', 'td button.funding-edit-btn', function (event) {
        event.stopPropagation();
        let rowData = fundingHistoryTable.row(event.target.closest('tr')).data();
        editFundingModal.open({ id: rowData.id });
    });

    $('#FundingHistoryTable').on('click', '.funding-delete-btn', function (e) {
        e.preventDefault();
        let id = $(this).data('id');
        abp.message.confirm(
            'Are you sure you want to delete this funding history record?',
            'Delete Funding History',
            function (isConfirmed) {
                if (isConfirmed) {
                    unity.grantManager.applicantProfile.applicantHistory.deleteFundingHistory(id)
                        .then(function () {
                            abp.notify.success('Record deleted.');
                            fundingHistoryTable.ajax.reload();
                        })
                        .catch(function () {
                            abp.notify.error('Failed to delete record.');
                            fundingHistoryTable.ajax.reload();
                        });
                }
            }
        );
    });

    $('#IssueTrackingTable').on('click', 'td button.issue-edit-btn', function (event) {
        event.stopPropagation();
        let rowData = issueTrackingTable.row(event.target.closest('tr')).data();
        editIssueModal.open({ id: rowData.id });
    });

    $('#IssueTrackingTable').on('click', '.issue-delete-btn', function (e) {
        e.preventDefault();
        let id = $(this).data('id');
        abp.message.confirm(
            'Are you sure you want to delete this issue tracking record?',
            'Delete Issue Tracking',
            function (isConfirmed) {
                if (isConfirmed) {
                    unity.grantManager.applicantProfile.applicantHistory.deleteIssueTracking(id)
                        .then(function () {
                            abp.notify.success('Record deleted.');
                            issueTrackingTable.ajax.reload();
                        })
                        .catch(function () {
                            abp.notify.error('Failed to delete record.');
                            issueTrackingTable.ajax.reload();
                        });
                }
            }
        );
    });

    $('#AuditHistoryTable').on('click', 'td button.audit-edit-btn', function (event) {
        event.stopPropagation();
        let rowData = auditHistoryTable.row(event.target.closest('tr')).data();
        editAuditModal.open({ id: rowData.id });
    });

    $('#AuditHistoryTable').on('click', '.audit-delete-btn', function (e) {
        e.preventDefault();
        let id = $(this).data('id');
        abp.message.confirm(
            'Are you sure you want to delete this audit history record?',
            'Delete Audit History',
            function (isConfirmed) {
                if (isConfirmed) {
                    unity.grantManager.applicantProfile.applicantHistory.deleteAuditHistory(id)
                        .then(function () {
                            abp.notify.success('Record deleted.');
                            auditHistoryTable.ajax.reload();
                        })
                        .catch(function () {
                            abp.notify.error('Failed to delete record.');
                            auditHistoryTable.ajax.reload();
                        });
                }
            }
        );
    });
});
