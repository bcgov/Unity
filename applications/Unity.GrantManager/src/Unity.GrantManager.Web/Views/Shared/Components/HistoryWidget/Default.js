$(function () {
    const UIElements = {
        historyLength: $('#historyLength')[0],
        expandedDiv: $('#expanded-div'),
        cardHeaderDiv: $('#card-header-div'),
        historyTab: $('#history-tab')
    }

    UIElements.historyTab.on('click', function (e) {
        if(UIElements.historyLength+"" !== "undefined" && UIElements.historyLength.value > 0) {
            UIElements.expandedDiv.removeClass('hidden');
            UIElements.cardHeaderDiv.addClass('custom-active');
        }
    });
});