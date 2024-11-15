$(function () {
    $('body').on('click', '#saveFundingAgreementInfoBtn', function () {
        let applicationId = document.getElementById('FundingAgreementInfoViewApplicationId').value;
        let formData = $("#fundingAgreementInfoForm").serializeArray();
        let fundingAgreementInfoObj = {};

        $.each(formData, function (_, input) {
             buildFormData(fundingAgreementInfoObj, input)
        });

        updateFundingAgreementInfo(applicationId, fundingAgreementInfoObj);
    });

    function buildFormData(fundingAgreementInfoObj, input) {
        fundingAgreementInfoObj[input.name.split(".")[1]] = input.value;

        if (fundingAgreementInfoObj[input.name.split(".")[1]] == '') {
            fundingAgreementInfoObj[input.name.split(".")[1]] = null;
        }
    }

    function updateFundingAgreementInfo(applicationId, fundingAgreementInfoObj) {
        try {
            unity.grantManager.grantApplications.grantApplication
                .updateFundingAgreementInfo(applicationId, fundingAgreementInfoObj)
                .done(function () {
                    abp.notify.success(
                        'The funding agreement has been updated.'
                    );
                    $('#saveFundingAgreementInfoBtn').prop('disabled', true);
                    PubSub.publish('funding_agreement_info_saved', fundingAgreementInfoObj);
                    PubSub.publish('refresh_detail_panel_summary');
                });
        }
        catch (error) {
            console.log(error);
            $('#saveFundingAgreementInfoBtn').prop('disabled', false);
        }
    }

    PubSub.subscribe(
        'fields_fundingAgreementInfo',
        () => {
            enableFundingAgreementInfoSaveBtn();
        }
    );

});

function enableFundingAgreementInfoSaveBtn(inputText) {
    if (!$("#fundingAgreementInfoForm").valid() || formHasInvalidCurrencyCustomFields("fundingAgreementInfoForm")) {
        $('#saveFundingAgreementInfoBtn').prop('disabled', true);
        return;
    }

    $('#saveFundingAgreementInfoBtn').prop('disabled', false);
}
