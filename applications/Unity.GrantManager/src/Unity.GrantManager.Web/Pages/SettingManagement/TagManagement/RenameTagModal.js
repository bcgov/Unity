$(function () {
    abp.modals.renameTag = function () {
        let _modalPublicApi = null;

        let initModal = function (modalManager, args) {
            let $modal = modalManager.getModal();
            let $form = modalManager.getForm();
            _modalPublicApi = modalManager;
            let _modalOptions = modalManager.getOptions();

            // DEVELOPMENT IN PROGRESS - STILL NEEDS WORK

            let _tagTypes = _modalOptions.registeredTagTypes;
            
            debugger;
            console.log('initialized the modal...');
            $form.on('submit', function (e) {
                // $('[href="#url"]').tab('show');
                debugger;
                e.preventDefault();

                if (!validateForm()) {
                    return;
                }

                const originalTagInput = $('input[name="ViewModel.OriginalTag"]');
                const replacementTagInput = $('input[name="ViewModel.ReplacementTag"]');
                const selectedTagTypeInput = $('input[name="SelectedTagType"]');

                const originalTag = originalTagInput.val();
                const replacementTag = replacementTagInput.val();
                const selectedTagType = selectedTagTypeInput.val();

                let tagService;

                // Call the appropriate service based on the selected tag type
                if (selectedTagType === 'Application' && _tagTypes.APPLICATIONS !== undefined) {
                    // Call the application tags service
                    tagService = _tagTypes.APPLICATIONS.service;
                } else if (selectedTagType === 'Payment' && _tagTypes.PAYMENTS !== undefined) {
                    // Call the payment tags service
                    tagService = _tagTypes.PAYMENTS.service;
                } else {
                    abp.notify.error('Unknown tag type: ' + selectedTagType);
                }

                tagService.renameTag(
                    originalTag,
                    replacementTag
                ).then(function () {
                    abp.notify.success(`The tag "${originalTag}" has been renamed to "${replacementTag}".`);
                    _modalPublicApi.close();
                }).catch(function (error) {
                    abp.notify.error('Tag rename failed: ' + (error.message || 'Unknown error'));
                    console.error(error);
                    _modalPublicApi.close();
                });
            });
        };

        return {
            initModal: initModal
        };
    };



    // Cache DOM elements
    const form = $('#renameTagForm');
    const saveButton = $('#SaveButton');
    const originalTagInput = $('input[name="ViewModel.OriginalTag"]');
    const replacementTagInput = $('input[name="ViewModel.ReplacementTag"]');
    const selectedTagTypeInput = $('input[name="SelectedTagType"]');
    
    // Function to validate the form
    function validateForm() {
        const originalTag = originalTagInput.val();
        const replacementTag = replacementTagInput.val();
        
        // Check if the field is empty
        if (!replacementTag || replacementTag.trim() === '') {
            return false;
        }
        
        // Check for spaces or commas
        if (/[\s,]/.test(replacementTag)) {
            return false;
        }
        
        // Check if tags are the same
        if (replacementTag === originalTag) {
            return false;
        }
        
        return true;
    }
    
    // Function to update save button state
    function updateSaveButtonState() {
        saveButton.prop('disabled', !validateForm());
    }
    
    // Set up event listeners
    replacementTagInput.on('input', updateSaveButtonState);
    
    // Initial validation on page load
    updateSaveButtonState();
    
    // Add custom validation to the jQuery validation
    if ($.validator) {
        $.validator.addMethod('validTag', function(value, element) {
            return !(/[\s,]/.test(value));
        }, 'Tag cannot contain spaces or commas');
        
        $.validator.addMethod('notEqual', function(value, element) {
            return value !== originalTagInput.val();
        }, 'New tag cannot be the same as the original tag');
        
        form.validate({
            rules: {
                'ViewModel.ReplacementTag': {
                    required: true,
                    validTag: true,
                    notEqual: true
                }
            },
            errorPlacement: function(error, element) {
                error.insertAfter(element);
            }
        });
    }
});
