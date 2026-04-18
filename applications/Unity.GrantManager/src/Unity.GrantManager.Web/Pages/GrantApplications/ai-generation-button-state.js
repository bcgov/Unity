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
