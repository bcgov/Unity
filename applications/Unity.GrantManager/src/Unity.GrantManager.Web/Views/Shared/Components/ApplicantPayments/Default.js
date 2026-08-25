function getPaymentIdColumn() {
    return {
        title: 'Payment ID',
        name: 'referenceNumber',
        data: 'referenceNumber',
        className: 'data-table-header',
        index: 0,
    };
}

function getSubmissionIdColumn() {
    return {
        title: 'Submission ID',
        name: 'applicationReferenceNo',
        data: 'applicationReferenceNo',
        className: 'data-table-header',
        index: 1,
        render: function (data, type, row) {
            if (type === 'display') {
                return (
                    '<a href="/GrantApplications/Details?ApplicationId=' +
                    row.applicationId +
                    '">' +
                    (data || '') +
                    '</a>'
                );
            }
            return data;
        },
    };
}

function getPaidDateColumn() {
    return {
        title: 'Paid Date',
        name: 'paymentDate',
        data: 'paymentDate',
        className: 'data-table-header',
        index: 2,
        render: function (data, type) {
            if (type !== 'display' && type !== 'filter') return data || '';
            return data || '';
        },
    };
}

function getCasPaymentStatusColumn() {
    return {
        title: 'CAS Payment Status',
        name: 'paymentStatus',
        data: 'paymentStatus',
        className: 'data-table-header',
        index: 5,
    };
}

function getSupplierNumberColumn() {
    return {
        title: 'Supplier #',
        name: 'supplierNumber',
        data: 'supplierNumber',
        className: 'data-table-header',
        index: 9,
    };
}

function getSupplierNameColumn() {
    return {
        title: 'Supplier Name',
        name: 'supplierName',
        data: 'supplierName',
        className: 'data-table-header',
        index: 10,
    };
}

function getSiteNumberColumn() {
    return {
        title: 'Site #',
        name: 'siteNumber',
        data: 'site.number',
        className: 'data-table-header',
        defaultContent: '',
        index: 11,
    };
}

let applicantCasPaymentResponseModal = new abp.ModalManager({
    viewUrl: '../PaymentRequests/CasPaymentRequestResponse',
});

function openApplicantCasResponseModal(casResponse) {
    applicantCasPaymentResponseModal.open({ casResponse: casResponse });
}

function getPaymentStatusTextColor(status) {
    switch (status) {
        case 'L1Pending':
        case 'L2Pending':
        case 'L3Pending':
            return '#053662';
        case 'L1Declined':
        case 'L2Declined':
        case 'L3Declined':
        case 'Failed':
            return '#CE3E39';
        case 'Submitted':
            return '#5595D9';
        case 'Paid':
        case 'HistoricalPayment':
            return '#42814A';
        default:
            return '#053662';
    }
}

function getInvoiceStatusColumn() {
    return {
        title: 'Invoice Status',
        name: 'invoiceStatus',
        data: 'invoiceStatus',
        className: 'data-table-header',
        defaultContent: '',
        index: 6,
    };
}

function getCasResponseColumn() {
    return {
        title: 'CAS Response',
        name: 'casResponse',
        data: 'casResponse',
        className: 'data-table-header notexport',
        index: 7,
        render: function (data) {
            if (data + '' !== 'undefined' && data?.length > 0) {
                return '<button class="btn btn-light info-btn" type="button" onclick="openApplicantCasResponseModal(\'' + data + '\');">View Response<i class="fl fl-mapinfo"></i></button>';
            }
            return null;
        },
    };
}

function getCategoryColumn() {
    return {
        title: 'Category',
        name: 'category',
        data: 'category',
        className: 'data-table-header',
        defaultContent: '',
        index: 8,
    };
}

$(function () {
    const l = abp.localization.getResource('Payments');
    $('.unity-currency-input').maskMoney({});
    $('.unity-currency-input').each(function () {
        $(this).maskMoney('mask', this.value);
    });

    const formatter = createNumberFormatter();
    let dt = $('#ApplicantPaymentRequestListTable');
    let dataTable;
    const listColumns = getColumns();
    const defaultVisibleColumns = [
        'referenceNumber',
        'applicationReferenceNo',
        'paymentDate',
        'status',
        'amount',
        'paymentStatus',
        'invoiceStatus',
        'casResponse',
        'category',
        'supplierNumber',
        'supplierName',
        'siteNumber',
    ];

    let actionButtons = [
        {
            text: 'Filter',
            className: 'custom-table-btn flex-none btn btn-secondary',
            action: function () {},
            attr: { id: 'btn-toggle-filter-applicant-payments' },
        },
        {
            extend: 'csv',
            text: 'Export',
            title: 'Applicant Payments',
            className: 'custom-table-btn flex-none btn btn-secondary',
            exportOptions: {
                rows: { search: 'applied' },
                columns: ':visible:not(.notexport)',
                orthogonal: 'fullName',
            },
        },
    ];

    let applicantId = document.getElementById('ApplicantPaymentsApplicantId').value;
    let inputAction = function () {
        return applicantId;
    };

    let responseCallback = function (result) {
        if (result.length <= 15) {
            $('.dataTables_paginate').hide();
        }
        return {
            recordsTotal: result.length,
            recordsFiltered: result.length,
            data: result,
        };
    };

    if (abp.auth.isGranted('Unity.GrantManager.ApplicationManagement.Payment.PaymentList')) {
        dataTable = initializeDataTable({
            dt,
            defaultVisibleColumns,
            listColumns,
            maxRowsPerPage: 10,
            defaultSortColumn: { name: 'paymentDate', dir: 'desc' },
            dataEndpoint:
                unity.grantManager.applicantProfile.applicantPayments
                    .getPaymentListByApplicantId,
            data: inputAction,
            responseCallback,
            actionButtons,
            serverSideEnabled: false,
            pagingEnabled: true,
            reorderEnabled: true,
            languageSetValues: {},
            dataTableName: 'ApplicantPaymentRequestListTable',
            externalFilterButtonId: 'btn-toggle-filter-applicant-payments',
            dynamicButtonContainerId: 'applicantPaymentsDynamicButtonContainerId',
            lengthMenu: [10, 25, 50, -1],
        });

        dataTable.externalSearch('#applicant-payments-search', { delay: 300 });

        dataTable.on('draw', function () {
            dataTable.rows().every(function () {
                let data = this.data();
                let $row = $(this.node());
                $row.removeClass('error-row');
                if (data.casResponse && data.casResponse !== '' &&
                    data.casResponse.toUpperCase() !== 'SUCCEEDED') {
                    $row.addClass('error-row');
                }
            });
        });

        // Reposition the COLUMNS dropdown to open upward when there is not enough space below. Opens upward if there is no ample space in either direction.
        $('#applicantPaymentsDynamicButtonContainerId').on('click', '.buttons-collection', function () {
            const $btn = $(this);
            setTimeout(function () {
                const $collection = $('.dt-button-collection').filter(':visible').first();
                if (!$collection.length) return;
                const btnRect = $btn[0].getBoundingClientRect();
                const collHeight = $collection.outerHeight();
                const rightOffset = window.innerWidth - btnRect.right;
                if (btnRect.bottom + collHeight > window.innerHeight) {
                    $collection[0].style.setProperty('position', 'fixed', 'important');
                    $collection[0].style.setProperty('bottom', (window.innerHeight - btnRect.top) + 'px', 'important');
                    $collection[0].style.setProperty('top', '', 'important');
                } else {
                    $collection[0].style.setProperty('position', 'fixed', 'important');
                    $collection[0].style.setProperty('top', btnRect.bottom + 'px', 'important');
                    $collection[0].style.setProperty('bottom', '', 'important');
                }
                $collection[0].style.setProperty('left', 'auto', 'important');
                $collection[0].style.setProperty('right', rightOffset + 'px', 'important');
            }, 0);
        });

        $('#nav-payments-tab').one('click', function () {
            dataTable.columns.adjust();
        });
    }

    function getColumns() {
        return [
            getPaymentIdColumn(),
            getSubmissionIdColumn(),
            getPaidDateColumn(),
            getStatusColumn(),
            getAmountColumn(),
            getCasPaymentStatusColumn(),
            getInvoiceStatusColumn(),
            getCasResponseColumn(),
            getCategoryColumn(),
            getSupplierNumberColumn(),
            getSupplierNameColumn(),
            getSiteNumberColumn(),
        ];
    }

    function getStatusColumn() {
        return {
            title: 'Status',
            name: 'status',
            data: 'status',
            className: 'data-table-header',
            index: 3,
            render: function (data) {
                let statusColor = getPaymentStatusTextColor(data);
                return (
                    '<span style="color:' +
                    statusColor +
                    ';">' +
                    l('Enum:PaymentRequestStatus.' + data) +
                    '</span>'
                );
            },
        };
    }

    function getAmountColumn() {
        return {
            title: 'Amount',
            name: 'amount',
            data: 'amount',
            className: 'data-table-header currency-display',
            index: 4,
            render: function (data) {
                return formatter.format(data);
            },
        };
    }
});
