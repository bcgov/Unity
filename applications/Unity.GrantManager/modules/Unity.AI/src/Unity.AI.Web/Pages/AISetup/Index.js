$(function () {
    if ($('#AISetupModelsTable').length === 0) {
        return;
    }

    const l = abp.localization.getResource('AI');
    // Same Actions width in all three tables so the buttons line up down the page.
    const actionsColumnWidth = '9.5rem';
    const modelService = unity.aI.setup.aIModel;
    const operationService = unity.aI.setup.aIOperation;

    const modelModal = new abp.ModalManager(abp.appPath + 'AISetup/ModelModal');
    const operationModal = new abp.ModalManager(abp.appPath + 'AISetup/OperationModal');

    // Escapes for both element text and quoted attribute values (" and ' included),
    // since results also go into title="..." and data-*="...".
    const escapeHtml = (value) => String(value ?? '')
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#39;');

    // IT Administrator manages models; IT Operations edits operations and AI Prompts.
    // Both can see the tables and Test a model.
    const canManageModels = abp.auth.isGranted('ITAdministrator');
    const canEditOperations = abp.auth.isGranted('ITOperations');

    // Yellow badge for something that needs setting up; the tooltip says why.
    const warningBadge = (text, help) => help
        ? `<span class="badge bg-warning text-dark" data-bs-toggle="tooltip" title="${escapeHtml(help)}">${escapeHtml(text)}</span>`
        : `<span class="badge bg-warning text-dark">${escapeHtml(text)}</span>`;

    const statusBadge = (isActive) => isActive
        ? `<span class="badge bg-success">${escapeHtml(l('AISetup:Active'))}</span>`
        : `<span class="badge bg-secondary">${escapeHtml(l('AISetup:Inactive'))}</span>`;

    // AI Reporting's two AI Setup rows, shown by what they are rather than their row names.
    const reportingLabels = {
        AIReporting: l('AISetup:AIReportingChatUsage'),
        AIReportingEmbedding: l('AISetup:AIReportingEmbeddingUsage')
    };
    const operationLabel = (name) => reportingLabels[name] ?? name;

    const listResponse = (result) => ({
        recordsTotal: result.items.length,
        recordsFiltered: result.items.length,
        data: result.items
    });

    // ── Models ───────────────────────────────────────────────────────────────
    // Fixed widths (rem) on every column; the browser spreads the space left over.
    // Leaving one column without a width lets DataTables size it a few pixels too
    // wide, and width '1%' on several columns pushes the table past its container.
    const modelColumns = [
        {
            width: '12rem', className: 'text-nowrap', title: l('AISetup:DeploymentName'), name: 'name', data: 'name', index: 0,
            render: (data) => escapeHtml(data)
        },
        {
            width: '7rem', className: 'text-nowrap', title: l('AISetup:Type'), name: 'type', data: 'type', index: 1,
            render: (data) => escapeHtml(data)
        },
        {
            width: '8rem', className: 'text-start text-nowrap', title: l('AISetup:ApiVersion'), name: 'apiVersion', data: 'apiVersion', index: 2,
            render: (data) => escapeHtml(data)
        },
        {
            width: '6rem', className: 'text-nowrap', title: l('AISetup:Status'), name: 'isActive', data: 'isActive', index: 3,
            render: (data) => statusBadge(data)
        },
        {
            title: l('AISetup:UsedBy'), name: 'usedByOperations', data: 'usedByOperations', index: 4,
            orderable: false,
            className: 'text-wrap',
            render: (data) => data?.length
                ? data.map((name) => escapeHtml(operationLabel(name))).join(', ')
                : escapeHtml(l('AISetup:None'))
        },
        {
            title: l('Actions'), data: 'id', orderable: false, width: actionsColumnWidth, className: 'text-center text-nowrap',
            name: 'rowActions', index: 5,
            rowAction: {
                items: [
                    {
                        text: l('AISetup:Test'),
                        action: (data) => testModel(data.record)
                    },
                    {
                        text: l('AISetup:Edit'),
                        visible: () => canManageModels,
                        action: (data) => modelModal.open({ id: data.record.id })
                    },
                    {
                        text: l('AISetup:Delete'),
                        visible: (record) => canManageModels
                            && !record.usedByOperations?.length,
                        confirmMessage: (data) => l('AISetup:DeleteConfirm', data.record.name),
                        action: (data) => modelService.delete(data.record.id).then(reloadAll)
                    }
                ]
            }
        }
    ];

    const modelsTable = initializeDataTable({
        dt: $('#AISetupModelsTable'),
        defaultVisibleColumns: modelColumns.map(column => column.name),
        listColumns: modelColumns,
        maxRowsPerPage: 25,
        defaultSortColumn: 0,
        dataEndpoint: () => modelService.getList(),
        data: {},
        responseCallback: listResponse,
        actionButtons: canManageModels ? [
            {
                text: `<i class="fa-solid fa-plus"></i> ${escapeHtml(l('AISetup:NewModel'))}`,
                titleAttr: l('AISetup:NewModel'),
                id: 'CreateAIModelButton',
                className: 'btn btn-light rounded-1',
                action: (e) => { e.preventDefault(); modelModal.open(); }
            }
        ] : [],
        pagingEnabled: false,
        reorderEnabled: false,
        languageSetValues: {},
        dataTableName: 'AISetupModelsTable',
        dynamicButtonContainerId: 'ai-setup-models-dynamic-buttons',
        useNullPlaceholder: true
    });

    // ── Operations ───────────────────────────────────────────────────────────
    const operationColumns = [
        {
            width: '12rem', className: 'text-nowrap', title: l('PromptName'), name: 'name', data: 'name', index: 0,
            // Links to the operation's prompt in AI Prompts (the prompt has the same name).
            render: (data) => {
                const name = escapeHtml(data);
                if (!canEditOperations) {
                    // AI Prompts is shown only to IT Operations, so there is nothing to link to.
                    return name;
                }
                const label = escapeHtml(l('AISetup:OpenInAIPrompts', data));
                return `<a href="#" class="ai-setup-open-prompt" data-prompt-name="${name}" title="${label}">${name}</a>`;
            }
        },
        {
            width: '6rem', className: 'text-nowrap', title: l('AISetup:Status'), name: 'isActive', data: 'isActive', index: 1,
            render: (data, type, row) => {
                if (!row.isConfigured) {
                    return warningBadge(l('AISetup:NotConfigured'));
                }
                if (!row.promptVersion) {
                    // Every version of the prompt is off, so the operation would fail.
                    return warningBadge(l('AISetup:NoPrompt'), l('AISetup:NoPromptHelp'));
                }
                return statusBadge(data);
            }
        },
        {
            width: '10rem', className: 'text-nowrap', title: l('AISetup:Model'), name: 'aiModelName', data: 'aiModelName', index: 2,
            defaultContent: '',
            render: (data, type, row) => {
                if (!row.isConfigured) {
                    return '';
                }
                const name = escapeHtml(data);
                return row.aiModelIsActive
                    ? name
                    : `${name} <span class="badge bg-danger">${escapeHtml(l('AISetup:Inactive'))}</span>`;
            }
        },
        {
            // No width: this column takes the spare space.
            className: 'text-nowrap', title: l('AISetup:ExecutionMode'), name: 'executionMode', data: 'executionMode', index: 3,
            render: (data, type, row) => row.isConfigured ? escapeHtml(data) : ''
        },
        {
            title: l('Actions'), data: 'name', orderable: false, width: actionsColumnWidth, className: 'text-center text-nowrap',
            name: 'rowActions', index: 4,
            rowAction: {
                items: [
                    {
                        text: l('AISetup:Edit'),
                        visible: () => canEditOperations,
                        action: (data) => operationModal.open({ name: data.record.name })
                    }
                ]
            }
        }
    ];

    const operationsTable = initializeDataTable({
        dt: $('#AISetupOperationsTable'),
        defaultVisibleColumns: operationColumns.map(column => column.name),
        listColumns: operationColumns,
        maxRowsPerPage: 25,
        defaultSortColumn: 0,
        dataEndpoint: () => operationService.getList(),
        data: {},
        responseCallback: listResponse,
        actionButtons: [],
        pagingEnabled: false,
        reorderEnabled: false,
        languageSetValues: {},
        dataTableName: 'AISetupOperationsTable',
        dynamicButtonContainerId: 'ai-setup-operations-dynamic-buttons',
        useNullPlaceholder: true
    });

    // ── AI Reporting ─────────────────────────────────────────────────────────
    const reportingColumns = [
        {
            width: '21rem', className: 'text-nowrap', title: l('AISetup:Setting'), name: 'name', data: 'name', index: 0,
            render: (data) => escapeHtml(data === 'AIReportingEmbedding'
                ? l('AISetup:AIReportingEmbeddingModel')
                : l('AISetup:AIReportingChatModel'))
        },
        {
            className: 'text-nowrap', title: l('AISetup:Model'), name: 'aiModelName', data: 'aiModelName', index: 1,
            defaultContent: '',
            render: (data, type, row) => {
                if (!row.isConfigured) {
                    // Not chosen yet: AI Reporting is unavailable until both models are chosen.
                    return warningBadge(l('AISetup:NotConfigured'), l('AISetup:AIReportingNotConfiguredHelp'));
                }
                const name = escapeHtml(data);
                return row.aiModelIsActive
                    ? name
                    : `${name} <span class="badge bg-danger">${escapeHtml(l('AISetup:Inactive'))}</span>`;
            }
        },
        {
            title: l('Actions'), data: 'name', orderable: false, width: actionsColumnWidth, className: 'text-center text-nowrap',
            name: 'rowActions', index: 2,
            rowAction: {
                items: [
                    {
                        text: l('AISetup:Edit'),
                        visible: () => canEditOperations,
                        action: (data) => operationModal.open({ name: data.record.name })
                    }
                ]
            }
        }
    ];

    const reportingTable = initializeDataTable({
        dt: $('#AISetupReportingTable'),
        defaultVisibleColumns: reportingColumns.map(column => column.name),
        listColumns: reportingColumns,
        maxRowsPerPage: 25,
        defaultSortColumn: 0,
        dataEndpoint: () => operationService.getAIReporting(),
        data: {},
        responseCallback: listResponse,
        actionButtons: [],
        pagingEnabled: false,
        reorderEnabled: false,
        languageSetValues: {},
        dataTableName: 'AISetupReportingTable',
        dynamicButtonContainerId: 'ai-setup-reporting-dynamic-buttons',
        useNullPlaceholder: true
    });

    // Helpers are const arrow functions, not function declarations: the bundle
    // minifier moves declarations out of the block that holds `l`, breaking them.
    const reloadAll = () => {
        modelsTable.ajax.reload();
        operationsTable.ajax.reload();
        reportingTable.ajax.reload();
    };

    modelModal.onResult(reloadAll);
    operationModal.onResult(reloadAll);

    // ── Endpoint status: UGM calls the endpoint's /healthz from inside the cluster ──
    const endpointStatus = $('#ai-setup-endpoint-status');
    const showEndpointStatus = (cssClass, text) => {
        endpointStatus.removeClass('text-muted text-success text-danger').addClass(cssClass).text(text);
    };
    modelService.getEndpointStatus()
        .then((status) => {
            switch (status.status) {
                case 'Healthy':
                    showEndpointStatus('text-success', status.message || l('AISetup:EndpointHealthy'));
                    break;
                case 'Unhealthy':
                    showEndpointStatus('text-danger', l('AISetup:EndpointUnhealthy', status.httpStatusCode, status.message));
                    break;
                case 'NoHealthCheck':
                    showEndpointStatus('text-muted', l('AISetup:EndpointNoHealthCheck', status.httpStatusCode));
                    break;
                case 'Unreachable':
                    showEndpointStatus('text-danger', l('AISetup:EndpointUnreachable', status.message));
                    break;
                default:
                    showEndpointStatus('text-danger', l('AISetup:EndpointNotSet'));
            }
        })
        .catch(() => showEndpointStatus('text-danger', l('AISetup:EndpointStatusFailed')));

    // Tooltips for cells drawn later by DataTables, delegated from the section.
    const setupSection = $('#AISetupOperationsTable').closest('.container-fluid')[0];
    if (setupSection) {
        bootstrap.Tooltip.getOrCreateInstance(setupSection, { selector: '[data-bs-toggle="tooltip"]' });
    }

    // Each operation name opens AI Prompts filtered to its prompt, where Status shows
    // the version in use.
    $(setupSection).on('click', '.ai-setup-open-prompt', (e) => {
        e.preventDefault();
        const promptName = $(e.currentTarget).data('prompt-name');
        $('#ai-prompts-menu-item').trigger('click');
        $('#search-prompts').val(promptName).trigger('input');
    });

    // ── Test ─────────────────────────────────────────────────────────────────
    const testResult = $('#ai-setup-test-result');

    const showTestResult = (kind, text) => {
        testResult
            .removeClass('d-none alert-info alert-success alert-danger')
            .addClass(`alert-${kind}`)
            .text(text);
    };

    const describeFailure = (result) => {
        if (result.errorCode && l('AISetup:' + result.errorCode) !== 'AISetup:' + result.errorCode) {
            return l('AISetup:' + result.errorCode);
        }

        const parts = [];
        if (result.httpStatusCode) {
            parts.push(l('AISetup:TestFailedStatus', result.httpStatusCode));
        }
        if (result.errorCode && result.errorCode !== String(result.httpStatusCode)) {
            parts.push(result.errorCode);
        }
        if (result.message) {
            parts.push(result.message);
        }
        return parts.join(' - ');
    };

    const testModel = (record) => {
        showTestResult('info', l('AISetup:Testing', record.name));
        modelService.test(record.id)
            .then((result) => {
                if (result.success) {
                    showTestResult('success', l('AISetup:TestSucceeded', record.name, result.latencyMilliseconds));
                } else {
                    showTestResult('danger', l('AISetup:TestFailed', record.name, describeFailure(result)));
                }
            })
            .catch(() => testResult.addClass('d-none'));
    };
});
