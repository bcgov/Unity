$(function () {
    $('body').on('click', '#saveFundingAgreementInfoBtn', function () {
        let applicationId = document.getElementById('FundingAgreementInfoViewApplicationId').value;
        let formVersionId = $("#FundingAgreementInfoView_FormVersionId").val();
        let formData = $("#fundingAgreementInfoForm").serializeArray();
        let fundingAgreementInfoObj = {};
        // Check for both multiple and single worksheet ID formats for compatibility
        let worksheetIds = $("#FundingAgreementInfo_WorksheetIds").val() || $("#FundingAgreementInfo_WorksheetId").val();

        $.each(formData, function (_, input) {
            if (typeof Flex === 'function' && Flex?.isCustomField(input)) {
                Flex.includeCustomFieldObj(fundingAgreementInfoObj, input);
            } else {
                buildFormData(fundingAgreementInfoObj, input)
            }
        });

        // Update checkboxes which are serialized if unchecked
        $(`#fundingAgreementInfoForm input:checkbox`).each(function () {
            fundingAgreementInfoObj[this.name] = (this.checked).toString();
        });

        // Make sure all the custom fields are set in the custom fields object
        if (typeof Flex === 'function') {
            Flex?.setCustomFields(fundingAgreementInfoObj);
        }

        fundingAgreementInfoObj['correlationId'] = formVersionId;
        if (worksheetIds) {
            // Handle both single and multiple worksheet IDs - always send as array
            if (worksheetIds.includes(',')) {
                // Multiple IDs - split and clean
                fundingAgreementInfoObj['worksheetIds'] = worksheetIds.split(',').map(id => id.trim());
            } else {
                // Single ID - convert to array for backend compatibility
                fundingAgreementInfoObj['worksheetIds'] = [worksheetIds.trim()];
            }
        }

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
        'fields_fundingagreementinfo',
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
