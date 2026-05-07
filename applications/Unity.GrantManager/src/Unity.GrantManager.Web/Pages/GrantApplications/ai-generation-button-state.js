(function (global) {
    const generatingStyles = {
        'background-color': '#f1f3f5',
        'border-color': '#adb5bd',
        color: '#495057',
        opacity: '1',
    };

    const completedStyles = {
        'border-color': '#2e7d32',
        color: '#2e7d32',
        opacity: '1',
    };

    function applyStyles($button, styles) {
        $button.css(styles);
    }

    global.AIGenerationButtonState = {
        resolveStatus(status) {
            switch (Number(status)) {
                case 0:
                    return 'Queued';
                case 1:
                    return 'Running';
                case 2:
                    return 'Completed';
                case 3:
                    return 'Failed';
                default:
                    return '';
            }
        },
        setGenerating($button) {
            applyStyles($button, generatingStyles);
        },
        setCompleted($button) {
            applyStyles($button, completedStyles);
        },
        restore($button) {
            $button.css({
                'background-color': '',
                'border-color': '',
                color: '',
                opacity: '',
            }).removeClass('disabled');
        },
    };
})(globalThis);
