$(function () {
    const l = abp.localization.getResource('Payments');
    $('.unity-currency-input').maskMoney({});
    $('.unity-currency-input').each(function () {
        $(this).maskMoney('mask', this.value);
    });
    const formatter = createNumberFormatter();
    let dt = $('#ApplicationPaymentRequestListTable');
    let dataTable;
    const listColumns = getColumns();
    const defaultVisibleColumns = [
        'id',
        'amount',
        'status'
    ];

    $('body').on('click', '#savePaymentInfoBtn', function () {
        let applicationId = document.getElementById('PaymentInfoViewApplicationId').value; 
        let formData = $("#paymentInfoForm").serializeArray();
        let paymentInfoObj = {};
        let formVersionId = $("#ApplicationFormVersionId").val();
        let worksheetId = $("#WorksheetId").val();

        $.each(formData, function (_, input) {
            if (typeof Flex === 'function' && Flex?.isCustomField(input)) {
                Flex.includeCustomFieldObj(paymentInfoObj, input);
            }
            else {
                buildFormData(paymentInfoObj, input)
            }
        });

        // Update checkboxes which are serialized if unchecked
        $(`#paymentInfoForm input:checkbox`).each(function () {
            paymentInfoForm[this.name] = (this.checked).toString();
        });

        paymentInfoObj['correlationId'] = formVersionId;
        paymentInfoObj['worksheetId'] = worksheetId;
        updatePaymentInfo(applicationId, paymentInfoObj);
    });

    function buildFormData(paymentInfoObj, input) {

        let inputElement = $('[name="' + input.name + '"]');
        // This will not work if the culture is different and uses a different decimal separator
        if (inputElement.hasClass('unity-currency-input') || inputElement.hasClass('numeric-mask')) {
            paymentInfoObj[input.name.split(".")[1]] = input.value.replace(/,/g, '');
        }
        else {
            paymentInfoObj[input.name.split(".")[1]] = input.value;
        }
        if (isNumberField(input)) {
            if (paymentInfoObj[input.name.split(".")[1]] == '') {
                paymentInfoObj[input.name.split(".")[1]] = 0;
            } else if (paymentInfoObj[input.name.split(".")[1]] > getMaxNumberField(input)) {
                paymentInfoObj[input.name.split(".")[1]] = getMaxNumberField(input);
            }
        }
        else if (paymentInfoObj[input.name.split(".")[1]] == '') {
            paymentInfoObj[input.name.split(".")[1]] = null;
        }
    }

    function updatePaymentInfo(applicationId, paymentInfoObj) {
        try {
            unity.payments.paymentInfo.paymentInfo
                .update(applicationId, paymentInfoObj)
                .done(function () {
                    abp.notify.success(
                        'The payment info has been updated.'
                    );
                    $('#savePaymentInfoBtn').prop('disabled', true);                    
                });
        }
        catch (error) {
            console.log(error);
            $('#savePaymentInfoBtn').prop('disabled', false);
        }
    }

    let actionButtons = [
        {
            text: 'Filter',
            className: 'custom-table-btn flex-none btn btn-secondary',
            id: "btn-toggle-filter",
            action: function (e, dt, node, config) {},
            attr: {
                id: 'btn-toggle-filter'
            }
        },
        {
            extend: 'csv',
            text: 'Export',
            title: 'Payment Requests',
            className: 'custom-table-btn flex-none btn btn-secondary',
            exportOptions: {
                columns: ':visible:not(.notexport)',
                orthogonal: 'fullName',
            }
        },
    ];

    let appId = document.getElementById('DetailsViewApplicationId').value;
    let inputAction = function (requestData, dataTableSettings) {
        const applicationId = appId
        return applicationId;
    }

    let responseCallback = function (result) {
        if (result.length <= 15) {
            $('.dataTables_paginate').hide();
        }
        return {
            recordsTotal: result.length,
            recordsFiltered: result.length,
            data: formatItems(result)
        };
    };

    let formatItems = function (items) {
        const newData = items.map((item, index) => {
            return {
                ...item,
                rowCount: index
            };
        });
        return newData;
    }

    dataTable = initializeDataTable(dt,
        defaultVisibleColumns,
        listColumns, 10, 3, unity.payments.paymentRequests.paymentRequest.getListByApplicationId, inputAction, responseCallback, actionButtons, 'dynamicButtonContainerId');

    dataTable.on('search.dt', () => handleSearch());

    dataTable.on('select', function (e, dt, type, indexes) {

        if (indexes?.length) {
            indexes.forEach(index => {
                $("#row_" + index).prop("checked", true);
                if ($(".chkbox:checked").length == $(".chkbox").length) {
                    $(".select-all-application-payments").prop("checked", true);
                }
                selectApplication(type, index, 'select_application_payment');
            });
        }
    });

    dataTable.on('deselect', function (e, dt, type, indexes) {
        if (indexes?.length) {
            indexes.forEach(index => {
                $("#row_" + index).prop("checked", false);
                if ($(".chkbox:checked").length != $(".chkbox").length) {
                    $(".select-all-application-payments").prop("checked", false);
                }
                selectApplication(type, index, 'deselect_application_payment');
            });
        }
    });

    function selectApplication(type, indexes, action) {
        if (type === 'row') {
            let data = dataTable.row(indexes).data();
            PubSub.publish(action, data);
        }
    }

    function handleSearch() {
        let filterValue = $('.dataTables_filter input').val();
        if (filterValue.length > 0) {

            Array.from(document.getElementsByClassName('selected')).forEach(
                function (element, index, array) {
                    element.classList.toggle('selected');
                }
            );
            PubSub.publish("deselect_application_payment", "reset_data");
        }
    }

    function getColumns() {
        return [
            getSelectColumn('Select Application', 'rowCount', 'application-payments'),
            getApplicationPaymentIdColumn(),
            getApplicationPaymentAmountColumn(),
            getApplicationPaymentStatusColumn(),
            getApplicationPaymentRequestedonColumn(),
            getApplicationPaymentUpdatedOnColumn(),
            getApplicationPaymentPaidOnColumn(),
            getApplicationPaymentDescriptionColumn(),
            getApplicationPaymentCASResponseColumn(),
        ]
    }

    function getApplicationPaymentIdColumn() {
        return {
            title: l('PaymentInfoView:ApplicationPaymentListTable.PaymentID'),
            name: 'referenceNumber',
            data: 'referenceNumber',
            className: 'data-table-header',
            index: 1,
        };
    }

    function getApplicationPaymentAmountColumn() {
        return {
            title: l('PaymentInfoView:ApplicationPaymentListTable.Amount'),
            name: 'amount',
            data: 'amount',
            className: 'data-table-header currency-display',
            index: 2,
            render: function (data) {
                return formatter.format(data);
            },
        };
    }

    function getApplicationPaymentStatusColumn() {
        return {
            title: l('PaymentInfoView:ApplicationPaymentListTable.Status'),
            name: 'status',
            data: 'status',
            className: 'data-table-header',
            index: 3,
            render: function (data) {
                return getStatusText(data);
            }
        };
    }

    function getApplicationPaymentRequestedonColumn() {
        return {
            title: l('PaymentInfoView:ApplicationPaymentListTable.RequestedOn'),
            name: 'requestedOn',
            data: 'creationTime',
            className: 'data-table-header',
            index: 5,
            render: function (data) {
                return formatDate(data);
            }
        };
    }

    function getApplicationPaymentUpdatedOnColumn() {
        return {
            title: l('PaymentInfoView:ApplicationPaymentListTable.UpdatedOn'),
            name: 'updatedOn',
            data: 'lastModificationTime',
            className: 'data-table-header',
            index: 6,
            render: function (data) {
                return formatDate(data);
            }
        };
    }

    function getApplicationPaymentPaidOnColumn() {
        return {
            title: l('PaymentInfoView:ApplicationPaymentListTable.PaidOn'),
            name: 'paidOn',
            data: 'paidOn',
            className: 'data-table-header',
            index: 7,
            render: function (data) {
                return formatDate(data);
            }
        };
    }

    function getApplicationPaymentDescriptionColumn() {
        return {
            title: l('PaymentInfoView:ApplicationPaymentListTable.Description'),
            name: 'description',
            data: 'description',
            className: 'data-table-header',
            index: 4,
        };
    }

    function getApplicationPaymentCASResponseColumn() {
        return {
            title: l('PaymentInfoView:ApplicationPaymentListTable.CASResponse'),
            name: 'cASResponse',
            data: 'casResponse',
            className: 'data-table-header',
            index: 8,
            render: function (data) {
                if(data+"" !== "undefined" && data?.length > 0) {
                    return '<button id="cas-response-btn" class="btn btn-light info-btn cas-response-btn" type="button" onclick="openCasResponseModal(\'' + data + '\');">View Response<i class="fl fl-mapinfo"></i></button>';
                }
                return  '{Not Available}';
            }
        };
    }

    function formatDate(data) {
        return data != null ? luxon.DateTime.fromISO(data, {
            locale: abp.localization.currentCulture.name,
        }).toUTC().toLocaleString() : '{Not Available}';
    }

    /* the resizer needs looking at again after ux2 refactor 
     window.addEventListener('resize', setTableHeighDynamic('PaymentRequestListTable'));
    */

    PubSub.subscribe(
        'refresh_application_list',
        (msg, data) => {
            dataTable.ajax.reload(null, false);
            PubSub.publish('clear_payment_application');
        }
    );

    $('#nav-payment-info-tab').one('click', function () {
        dataTable.columns.adjust();
    });

    $('#search').on('input', function () {
        let table = $('#ApplicationPaymentRequestListTable').DataTable();
        table.search($(this).val()).draw();
    });

    function getStatusText(data) {
        switch (data) {
            case 1:
                return "Created";
            case 2:
                return "Submitted";
            case 3:
                return "Approved";
            case 4:
                return "Declined";
            case 5:
                return "Awaiting Approval"
            default:
                return "Created";
        }
    }

    $('.select-all-application-payments').click(function () {
        if ($(this).is(':checked')) {
            dataTable.rows({ 'page': 'current' }).select();
        }
        else {
            dataTable.rows({ 'page': 'current' }).deselect();
        }
    });

    PubSub.subscribe(
        'fields_paymentinfo',
        () => {
            enableSaveBtn();
        }
    );
});

let casPaymentResponseModal = new abp.ModalManager({
    viewUrl: '../PaymentRequests/CasPaymentRequestResponse'
});

function openCasResponseModal(casResponse) {
    casPaymentResponseModal.open({
        casResponse: casResponse
    });
}

function enableSaveBtn() {
    if (!$("#paymentInfoForm").valid() || formHasInvalidCurrencyCustomFields("paymentInfoForm")) {
        $('#savePaymentInfoBtn').prop('disabled', true);
        return;
    }
    $('#savePaymentInfoBtn').prop('disabled', false);
}
