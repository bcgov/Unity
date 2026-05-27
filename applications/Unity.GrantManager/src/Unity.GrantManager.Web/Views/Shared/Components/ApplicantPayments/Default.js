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
            getSupplierNumberColumn(),
            getSupplierNameColumn(),
            getSiteNumberColumn(),
        ];
    }

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
            index: 6,
        };
    }

    function getSupplierNameColumn() {
        return {
            title: 'Supplier Name',
            name: 'supplierName',
            data: 'supplierName',
            className: 'data-table-header',
            index: 7,
        };
    }

    function getSiteNumberColumn() {
        return {
            title: 'Site #',
            name: 'siteNumber',
            data: 'site.number',
            className: 'data-table-header',
            defaultContent: '',
            index: 8,
        };
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
});
