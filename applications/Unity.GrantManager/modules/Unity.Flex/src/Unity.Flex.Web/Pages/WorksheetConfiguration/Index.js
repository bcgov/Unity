$(function () {
    $('.worksheet-import-btn').on('click', function () {
        $('#worksheet_import_upload').trigger('click');
    });

    // --- Column resizer ---
    const resizer = document.getElementById('column-resizer');
    const leftCol = document.getElementById('worksheet-left-col');
    const rightCol = document.getElementById('worksheet-right-col');

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

    // --- Worksheet name filter & published/archived toggle ---
    function applyWorksheetFilters() {
        const searchText = $('#worksheet-name-filter').val().toLowerCase();
        const publishedFilter = $('#worksheet-published-toggle .active').data('filter');

        let visibleCount = 0;
        $('#worksheet-accordion .accordion-item').each(function () {
            const $item = $(this);
            const title = $item.find('.worksheet-title').text().toLowerCase();
            const name  = $item.find('.worksheet-name').text().toLowerCase();
            const isPublished = !$item.find('.worksheet-published-icon').hasClass('hidden');
            const isArchived = $item.data('is-archived') === true || $item.data('is-archived') === 'true';

            const matchesText = !searchText || title.includes(searchText) || name.includes(searchText);
            const matchesFilter =
                publishedFilter === 'archived'
                    ? isArchived
                    : !isArchived && (
                        publishedFilter === 'all' ||
                        (publishedFilter === 'published' && isPublished) ||
                        (publishedFilter === 'unpublished' && !isPublished)
                    );

            const visible = matchesText && matchesFilter;
            $item.toggle(visible);
            if (visible) visibleCount++;
        });

        $('#worksheet-no-results').toggle(visibleCount === 0);
    }

    $(document).on('input', '#worksheet-name-filter', applyWorksheetFilters);

    $(document).on('click', '#worksheet-published-toggle button', function () {
        $('#worksheet-published-toggle button').removeClass('active');
        $(this).addClass('active');
        applyWorksheetFilters();
    });

    PubSub.subscribe('worksheet_list_refreshed', function () {
        applyWorksheetFilters();
    });

    applyWorksheetFilters();
});

function importWorksheetFile(inputId) {
    importFlexFile(inputId, "/api/app/worksheet/import", "Worksheet", 'refresh_worksheet_list');
}

function importFlexFile(inputId, urlStr, flexType, refreshChannel) {
    let input = document.getElementById(inputId);
    let file = input.files[0]; // Only get the first file
    let formData = new FormData();
    const maxFileSize = decodeURIComponent($("#MaxFileSize").val());

    if (!file) {
        return;
    }

    if ((file.size * 0.000001) > maxFileSize) {
        input.value = null;
        return abp.notify.error(
            'Error',
            'File size exceeds ' + maxFileSize + 'MB'
        );
    }

    formData.append("file", file);

    $.ajax({
        url: urlStr,
        data: formData,
        processData: false,
        contentType: false,
        type: "POST",
        success: function (data) {
            abp.notify.success(
                data.responseText,
                flexType + ' Import Is Successful'
            );
            PubSub.publish(refreshChannel, { scoresheetId: null });
            input.value = null;
        },
        error: function (data) {
            abp.notify.error(
                data.responseText,
                flexType + ' Import Not Successful'
            );
            PubSub.publish(refreshChannel, { scoresheetId: null });
            input.value = null;
        }
    });
}
