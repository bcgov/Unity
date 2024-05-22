(function () {
    abp.widgets.ApplicationActionWidget = function ($wrapper) {
        let widgetManager = $wrapper.data('abp-widget-manager');
        console.log(widgetManager);
    };
})();

function notifyFieldChange(event, field) {
    let value = document.getElementById(field.id).value;
    if (PubSub) {
        PubSub.publish('fields_' + event, value);
    }
}

const Flex = class {
    static isCustomField(input) {
        return input.name.startsWith('Custom_');
    }

    static includeCustomFieldObj(formObject, input) {
        if (!formObject.CustomFields) {            
            formObject.CustomFields = {};
        }

        formObject.CustomFields[input.name.replace('Custom_','')] = input.value;
    }
}
