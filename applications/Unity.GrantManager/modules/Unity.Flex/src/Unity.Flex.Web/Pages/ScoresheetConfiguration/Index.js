$(function () {
    $('#scoresheet_import_upload_btn').click(function () {
        $('#scoresheet_import_upload').trigger('click');
    });

    // --- Column resizer ---
    const resizer = document.getElementById('scoresheet-column-resizer');
    const leftCol = document.getElementById('scoresheet-left-col');
    const rightCol = document.getElementById('scoresheet-right-col');

    if (resizer && leftCol && rightCol) {
        let isResizing = false;
        let startX = 0;
        let startLeftWidth = 0;

        resizer.addEventListener('mousedown', function (e) {
            isResizing = true;
            startX = e.clientX;
            startLeftWidth = leftCol.getBoundingClientRect().width;
            resizer.classList.add('dragging');
            document.body.style.cursor = 'col-resize';
            document.body.style.userSelect = 'none';
            e.preventDefault();
        });

        document.addEventListener('mousemove', function (e) {
            if (!isResizing) return;
            const container = resizer.parentElement;
            const containerWidth = container.getBoundingClientRect().width;
            const resizerWidth = resizer.getBoundingClientRect().width;
            const dx = e.clientX - startX;
            let newLeftWidth = startLeftWidth + dx;
            newLeftWidth = Math.max(200, Math.min(containerWidth - resizerWidth - 200, newLeftWidth));
            leftCol.style.flex = `0 0 ${newLeftWidth}px`;
            rightCol.style.flex = '1 1 0';
        });

        document.addEventListener('mouseup', function () {
            if (!isResizing) return;
            isResizing = false;
            resizer.classList.remove('dragging');
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
        });
    }

    // --- Scoresheet name filter & published toggle ---
    $(document).on('input', '#scoresheet-name-filter', applyScoresheetFilters);

    $(document).on('click', '#scoresheet-published-toggle button', function () {
        $('#scoresheet-published-toggle button').removeClass('active');
        $(this).addClass('active');
        applyScoresheetFilters();
    });

    applyScoresheetFilters();
});

function applyScoresheetFilters() {
    const searchText = $('#scoresheet-name-filter').val().toLowerCase();
    const publishedFilter = $('#scoresheet-published-toggle .active').data('filter');

    let visibleCount = 0;
    $('#scoresheet-accordion .accordion-item').each(function () {
        const $item = $(this);
        const title = $item.find('.scoresheet-title').text().toLowerCase();
        const name = $item.find('.scoresheet-name').text().toLowerCase();
        const isPublished = !$item.find('.scoresheet-published-icon').hasClass('hidden');

        const matchesText = !searchText || title.includes(searchText) || name.includes(searchText);
        const matchesFilter =
            publishedFilter === 'all' ||
            (publishedFilter === 'published' && isPublished) ||
            (publishedFilter === 'unpublished' && !isPublished);

        const visible = matchesText && matchesFilter;
        $item.toggle(visible);
        if (visible) visibleCount++;
    });

    $('#scoresheet-no-results').toggle(visibleCount === 0);
}

function importScoresheetFile(inputId) {
    importFlexFile(inputId, "/api/app/scoresheet/import", "Scoresheet", 'refresh_scoresheet_list');
}


let scoresheetModal = new abp.ModalManager({
    viewUrl: 'ScoresheetConfiguration/ScoresheetModal'
});

let cloneScoresheetModal = new abp.ModalManager({
    viewUrl: 'ScoresheetConfiguration/CloneScoresheetModal'
});

let publishScoresheetModal = new abp.ModalManager({
    viewUrl: 'ScoresheetConfiguration/PublishScoresheetModal'
});

let scoresheetToEditId = null;

scoresheetModal.onResult(function (response) {
    const actionType = $(response.currentTarget).find('#ActionType').val();
    if (actionType.startsWith('Delete')) {
        PubSub.publish('refresh_scoresheet_list', { scoresheetId: null });
    } else {
        PubSub.publish('refresh_scoresheet_list', { scoresheetId: scoresheetToEditId });
    }
    abp.notify.success(
        actionType + ' is successful.', 
        'Scoresheet'
    );
});

cloneScoresheetModal.onResult(function (response) {
    PubSub.publish('refresh_scoresheet_list', { scoresheetId: null });
    abp.notify.success(
        'Scoresheet cloning is successful.',
        'Scoresheet'
    );
});

publishScoresheetModal.onResult(function (response) {
    PubSub.publish('refresh_scoresheet_list', { scoresheetId: scoresheetToEditId });
    abp.notify.success(
        'Scoresheet publishing is successful.',
        'Scoresheet'
    );
});

function openScoresheetModal(scoresheetId, actionType) {
    scoresheetToEditId = scoresheetId;
    scoresheetModal.open({
        scoresheetId: scoresheetId,
        actionType: actionType
    });
}

function openCloneScoresheetModal(scoresheetId) {
    scoresheetToEditId = scoresheetId;
    cloneScoresheetModal.open({
        scoresheetId: scoresheetId
    });
}

function openPublishScoresheetModal(scoresheetId) {
    scoresheetToEditId = scoresheetId;
    publishScoresheetModal.open({
        scoresheetId: scoresheetId
    });
}

PubSub.subscribe(
    'refresh_scoresheet_list',
    (msg, data) => {
        refreshScoresheetInfoWidget(data.scoresheetId);
    }
);

function showAccordion(scoresheetId) {
    if (!scoresheetId) {
        return;
    }
    const accordionId = 'collapse-' + scoresheetId;
    const accordion = document.getElementById(accordionId);
    accordion.classList.add('show');

    const buttonId = 'accordion-button-' + scoresheetId;
    const accordionButton = document.getElementById(buttonId);
    accordionButton.classList.remove('collapsed');
}

function refreshScoresheetInfoWidget(scoresheetId) {
    const url = `../Flex/Widget/Scoresheet/Refresh`;
    fetch(url)
        .then(response => response.text())
        .then(data => {
            document.getElementById('scoresheet-info-widget').innerHTML = data;
            showAccordion(scoresheetId);
            applyScoresheetFilters();
            PubSub.publish('refresh_scoresheet_configuration_page');
        })
        .catch(error => {
            console.error('Error refreshing scoresheet-info-widget:', error);
        });
}

