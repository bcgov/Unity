// Note: File depends on Unity.GrantManager.Web\Views\Shared\Components\_Shared\Attachments.js
// Move variables to global scope
let applicationAttachmentsDataTable;
let applicationAttachmentsInitialized = false;

// Global initialization function for lazy loading
window.initializeApplicationAttachments = function (
    containerSelector = 'body'
) {
    console.log('Initializing ApplicationAttachments component');

    // Use container selector to scope the search
    const $container = $(containerSelector);

    const l = abp.localization.getResource('GrantManager');
    const nullPlaceholder = '—';

    let inputAction = function (requestData, dataTableSettings) {
        const urlParams = new URL(window.location.toLocaleString())
            .searchParams;
        const applicationId =
            urlParams.get('ApplicationId') ||
            document.getElementById('DetailsViewApplicationId')?.value;
        return {
            attachmentType: 'APPLICATION',
            attachedResourceId: applicationId,
        };
    };

    let responseCallback = function (result) {
        if (result) {
            setTimeout(function () {
                PubSub.publish('update_application_attachment_count', {
                    files: result.length,
                });
            }, 10);
        }

        return {
            data: result,
        };
    };

    // Find the table within the container
    const $table =
        $container.find('#ApplicationAttachmentsTable').length > 0
            ? $container.find('#ApplicationAttachmentsTable')
            : $('#ApplicationAttachmentsTable');

    if ($table.length === 0) {
        console.error('ApplicationAttachments: Table not found');
        return;
    }

    applicationAttachmentsDataTable = $table.DataTable(
        abp.libs.datatables.normalizeConfiguration({
            serverSide: false,
            order: [[2, 'asc']],
            searching: false,
            paging: false,
            select: false,
            info: false,
            scrollX: true,
            ajax: abp.libs.datatables.createAjax(
                unity.grantManager.attachments.attachment.getAttachments,
                inputAction,
                responseCallback
            ),
            columnDefs: [
                {
                    title: '<i class="fl fl-paperclip" ></i>',
                    render: function (data) {
                        return '<i class="fl fl-paperclip" ></i>';
                    },
                    orderable: false,
                },
                {
                    title: l('AssessmentResultAttachments:DocumentName'),
                    data: 'fileName',
                    className: 'data-table-header',
                },
                {
                    title: 'Label',
                    data: 'displayName',
                    className: 'data-table-header',
                    render: function (data) {
                        return data ?? nullPlaceholder;
                    },
                },
                {
                    title: l('AssessmentResultAttachments:UploadedDate'),
                    data: 'time',
                    className: 'data-table-header',
                    render: function (data, type) {
                        if (type === 'display') {
                            return new Date(data).toDateString();
                        }
                        return data;
                    },
                },
                {
                    title: l('AssessmentResultAttachments:AttachedBy'),
                    data: 'attachedBy',
                    className: 'data-table-header',
                },
                {
                    title: '',
                    data: 's3ObjectKey',
                    render: function (data, type, full, meta) {
                        return generateAttachmentButtonContent(
                            data,
                            type,
                            full,
                            meta,
                            'Application'
                        );
                    },
                    orderable: false,
                },
            ],
        })
    );

    // Remove existing handlers to prevent duplicates
    applicationAttachmentsDataTable.off(
        'click.applicationAttachments',
        'tbody tr'
    );

    // Add new handler with namespace
    applicationAttachmentsDataTable.on(
        'click.applicationAttachments',
        'tbody tr',
        function (e) {
            e.currentTarget.classList.toggle('selected');
        }
    );

    // Setup PubSub subscription (only once)
    if (!applicationAttachmentsInitialized) {
        PubSub.subscribe('refresh_application_attachment_list', (msg, data) => {
            if (applicationAttachmentsDataTable) {
                applicationAttachmentsDataTable.ajax.reload();
            }
        });
        applicationAttachmentsInitialized = true;
    }

    // Handle tab click for column adjustment
    const $attachmentsTab =
        $container.find('#attachments-tab').length > 0
            ? $container.find('#attachments-tab')
            : $('#attachments-tab');

    $attachmentsTab.off('click.applicationAttachments');
    $attachmentsTab.one('click.applicationAttachments', function () {
        if (applicationAttachmentsDataTable) {
            applicationAttachmentsDataTable.columns.adjust();
        }
    });

    console.log('ApplicationAttachments initialized successfully');
};

// Global refresh function
window.refreshApplicationAttachments = function () {
    if (applicationAttachmentsDataTable) {
        applicationAttachmentsDataTable.ajax.reload();
    }
};

// Global cleanup function
window.cleanupApplicationAttachments = function () {
    if (applicationAttachmentsDataTable) {
        applicationAttachmentsDataTable.off('click.applicationAttachments');
        applicationAttachmentsDataTable.destroy();
        applicationAttachmentsDataTable = null;
    }
    $('#attachments-tab').off('click.applicationAttachments');
    console.log('ApplicationAttachments cleaned up');
};

// For debugging
// window.ApplicationAttachmentsDebug = {
//     isInitialized: () => applicationAttachmentsInitialized,
//     getDataTable: () => applicationAttachmentsDataTable,
//     forceRefresh: () => window.refreshApplicationAttachments(),
//     forceReinitialize: () => {
//         window.cleanupApplicationAttachments();
//         window.initializeApplicationAttachments('body');
//     },
// };

// Original initialization for backward compatibility
$(function () {
    // Check if we're in a lazy loading context by looking for specific elements
    const hasLazyContainer = $('.lazy-component-container').length > 0;
    const isDetailsV2 = window.location.pathname.includes('DetailsV2');

    // Only auto-initialize if NOT in lazy loading context
    if (!hasLazyContainer && !isDetailsV2) {
        console.log(
            'Auto-initializing ApplicationAttachments for non-lazy context'
        );
        window.initializeApplicationAttachments('body');
    } else {
        console.log(
            'Skipping auto-initialization - lazy loading context detected'
        );
    }
});
