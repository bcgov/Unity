(function () {
    $('.custom-currency-input').maskMoney({
        thousands: ',',
        decimal: '.'
    }).maskMoney('mask');

    $('.custom-currency-input').on('blur', function () {
        if ($(this).val() === '' || $(this).val() === '0') {
            $(this).val('0.00');
        }
    });
})();
