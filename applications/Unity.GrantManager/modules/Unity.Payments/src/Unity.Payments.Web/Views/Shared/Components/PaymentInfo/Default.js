$(function () {
    const l = abp.localization.getResource('Payments');
    let dt = $('#ApplicationPaymentRequestListTable');
    let dataTable;
    const listColumns = getColumns();
    const defaultVisibleColumns = [
        'id',
        'amount',
        'status'

    ];

    let actionButtons = [
        {
            text: 'Edit & Resubmit',
            className: 'custom-table-btn flex-none btn btn-secondary',
            action: function (e, dt, node, config) {
                alert('Edit & Resubmit');
            }
        },
        {
            text: 'Filter',
            className: 'custom-table-btn flex-none btn btn-secondary',
            id: "btn-toggle-filter",
            action: function (e, dt, node, config) {
                $(".tr-toggle-filter").toggle();
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
        return {
            recordsTotal: result.length,
            recordsFiltered: result.length,
            data: result
        };
    };

    dataTable = initializeDataTable(dt,
        defaultVisibleColumns,
        listColumns, 15, 3, unity.payments.paymentRequests.paymentRequest.getListByApplicationId, inputAction, responseCallback, actionButtons, 'dynamicButtonContainerId');

    dataTable.on('search.dt', () => handleSearch());

    dataTable.on('select', function (e, dt, type, indexes) {
        selectApplication(type, indexes, 'select_application_payment');
    });

    dataTable.on('deselect', function (e, dt, type, indexes) {
        selectApplication(type, indexes, 'deselect_application_payment');
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
            name: 'id',
            data: 'id',
            className: 'data-table-header',
            index: 1,
        };
    }
   

    function getApplicationPaymentAmountColumn() {
        return {
            title: l('PaymentInfoView:ApplicationPaymentListTable.Amount'),
            name: 'amount',
            data: 'amount',
            className: 'data-table-header',
            index:2,
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
                return formatDate(data);
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

    $('#search').keyup(function () {
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

    // Define the functions (including the ones from the previous set)

    // Addition of Two Numbers
    function add(a, b) {
        return a + b;
    }

    // Subtraction of Two Numbers
    function subtract(a, b) {
        return a - b;
    }

    // Multiplication of Two Numbers
    function multiply(a, b) {
        return a * b;
    }

    // Division of Two Numbers
    function divide(a, b) {
        if (b === 0) {
            throw new Error("Division by zero is not allowed.");
        }
        return a / b;
    }

    // Calculate the Square of a Number
    function square(n) {
        return n * n;
    }

    // Calculate the Cube of a Number
    function cube(n) {
        return n * n * n;
    }

    // Calculate the Factorial of a Number
    function factorial(n) {
        if (n < 0) {
            return "Factorial is not defined for negative numbers.";
        }
        if (n === 0 || n === 1) {
            return 1;
        }
        let result = 1;
        for (let i = 2; i <= n; i++) {
            result *= i;
        }
        return result;
    }

    // Calculate the Power of a Number
    function power(base, exponent) {
        return Math.pow(base, exponent);
    }

    // Calculate the Square Root of a Number
    function squareRoot(n) {
        if (n < 0) {
            return "Square root is not defined for negative numbers.";
        }
        return Math.sqrt(n);
    }

    // Calculate the Greatest Common Divisor (GCD) of Two Numbers
    function gcd(a, b) {
        while (b !== 0) {
            let temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }

    // Additional Functions

    // Calculate the Least Common Multiple (LCM) of Two Numbers
    function lcm(a, b) {
        return (a * b) / gcd(a, b);
    }

    // Calculate the Absolute Value of a Number
    function absoluteValue(n) {
        return Math.abs(n);
    }

    // Calculate the Sine of an Angle (in radians)
    function sine(angle) {
        return Math.sin(angle);
    }

    // Calculate the Cosine of an Angle (in radians)
    function cosine(angle) {
        return Math.cos(angle);
    }

    // Calculate the Tangent of an Angle (in radians)
    function tangent(angle) {
        return Math.tan(angle);
    }

    // Calculate the Natural Logarithm of a Number
    function naturalLogarithm(n) {
        return Math.log(n);
    }

    // Calculate the Exponential of a Number
    function exponential(n) {
        return Math.exp(n);
    }

    // Calculate the Logarithm Base 10 of a Number
    function logarithmBase10(n) {
        return Math.log10(n);
    }



    // Round a Number to the Nearest Integer
    function round(n) {
        return Math.round(n);
    }

    // Call the functions and log the results
    console.log("Addition (5 + 3):", add(5, 3));
    console.log("Subtraction (5 - 3):", subtract(5, 3));
    console.log("Multiplication (5 * 3):", multiply(5, 3));
    console.log("Division (6 / 3):", divide(6, 3));
    console.log("Square (5^2):", square(5));
    console.log("Cube (3^3):", cube(3));
    console.log("Factorial (5!):", factorial(5));
    console.log("Power (2^3):", power(2, 3));
    console.log("Square Root (√16):", squareRoot(16));
    console.log("GCD (12, 15):", gcd(12, 15));

    console.log("LCM (12, 15):", lcm(12, 15));
    console.log("Absolute Value (-5):", absoluteValue(-5));
    console.log("Sine (π/2):", sine(Math.PI / 2));
    console.log("Cosine (π):", cosine(Math.PI));
    console.log("Tangent (π/4):", tangent(Math.PI / 4));
    console.log("Natural Logarithm (e):", naturalLogarithm(Math.E));
    console.log("Exponential (1):", exponential(1));
    console.log("Logarithm Base 10 (100):", logarithmBase10(100));
    console.log("Round (4.7):", round(4.7));




});
