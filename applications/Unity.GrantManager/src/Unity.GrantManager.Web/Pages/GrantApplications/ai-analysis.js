/**
 * AI Analysis module for Grant Applications
 * Renders a stable sectioned view for AI-generated analysis results.
 */

const dismissedSectionVisibility = {
    error: false,
    warning: false,
    summary: false,
    recommendation: false
};

// Preserve the user's collapse choice across analysis rerenders.
const sectionCollapseState = {
    error: false,
    warning: false,
    summary: false,
    recommendation: false
};

let aiAnalysisMonitor = null;

function getAnalysisLabels() {
    const labels = document.getElementById('aiAnalysisLabels')?.dataset ?? {};

    return {
        errors: labels.errors || 'Errors',
        warnings: labels.warnings || 'Warnings',
        summaries: labels.summaries || 'Summaries',
        recommendation: labels.recommendation || labels.recommendations || 'Recommendation',
        proceed: labels.proceed || 'Proceed',
        hold: labels.hold || 'Hold',
        noErrors: labels.noErrors || 'No errors',
        noWarnings: labels.noWarnings || 'No warnings',
        showDismissed: labels.showDismissed || 'Show dismissed items',
        hideDismissed: labels.hideDismissed || 'Hide dismissed items',
        dismiss: labels.dismiss || 'Dismiss',
        restore: labels.restore || 'Restore',
        dismissTitle: labels.dismissTitle || 'Dismiss this item',
        restoreTitle: labels.restoreTitle || 'Restore this item',
        collapseTitle: labels.collapseTitle || 'Collapse section',
        expandTitle: labels.expandTitle || 'Expand section'
    };
}

function bindTemplateAction($element, actionData) {
    $element.attr('data-id', actionData.id).attr('data-type', actionData.type);
}

function bindTemplateValue($item, key, value) {
    const $element = $item.find(`[data-element="${key}"]`);
    if ($element.length === 0) {
        return;
    }

    if (key === 'dismiss-btn' || key === 'restore-btn') {
        bindTemplateAction($element, value);
        return;
    }

    $element.text(value);
}

function createItemFromTemplate(templateName, data) {
    const $template = $(`[data-template="${templateName}"]`).first();
    const $item = $template.clone().removeAttr('data-template');

    Object.entries(data).forEach(([key, value]) => bindTemplateValue($item, key, value));

    return $item;
}

function getFindingDetailText(item) {
    if (!item || typeof item !== 'object') {
        return '';
    }

    const raw = item.detail ?? '';
    if (typeof raw === 'string') {
        return raw;
    }

    try {
        return JSON.stringify(raw);
    } catch {
        return String(raw);
    }
}

function normalizeDecision(decision) {
    if (typeof decision !== 'string') {
        return '';
    }

    const normalized = decision.trim().toUpperCase();
    return normalized === 'PROCEED' || normalized === 'HOLD'
        ? normalized
        : '';
}

function normalizeFindings(items, fallbackType) {
    const fallbackTitles = {
        error: 'Error',
        warning: 'Warning',
        summary: 'Summary',
        recommendation: 'Recommendation'
    };

    return (items || [])
        .filter(Boolean)
        .map((item, index) => ({
            ...item,
            id: item.id || `${fallbackType}-${index}`,
            dismissed: item.dismissed === true,
            title: item.title || item.category || fallbackTitles[fallbackType] || 'Item',
            detail: item.detail || item.message || ''
        }));
}

function createFindingItem(item, type, hidden) {
    const labels = getAnalysisLabels();
    const templateName = hidden ? 'dismissed-item' : 'active-item';
    const actionKey = hidden ? 'restore-btn' : 'dismiss-btn';
    const $item = createItemFromTemplate(templateName, {
        category: item.title,
        message: getFindingDetailText(item),
        [actionKey]: { id: item.id, type: type }
    });

    if (hidden) {
        $item.find('[data-element="restore-text"]').text(labels.restore);
        $item.find('[data-element="restore-btn"]').attr('title', labels.restoreTitle);
    } else {
        $item.find('[data-element="dismiss-text"]').text(labels.dismiss);
        $item.find('[data-element="dismiss-btn"]').attr('title', labels.dismissTitle);
    }

    return $item;
}

function updateVisibleItemLayout($items) {
    const $allItems = $items.children('.ai-analysis-detail-item');
    const $visibleItems = getVisibleAnalysisItems($items);

    $allItems.removeClass('last-visible');
    $visibleItems.last().addClass('last-visible');
}

function getVisibleAnalysisItems($items) {
    return $items
        .children('.ai-analysis-detail-item')
        .filter(function() {
            return this.style.display !== 'none';
        });
}

function formatSectionTitle(title, count) {
    return `${title} (${count})`;
}

function configureSectionStatus($status, text, statusClass) {
    if (!text) {
        return;
    }

    $status
        .removeClass('proceed hold')
        .addClass('ai-analysis-status-chip');

    if (statusClass) {
        $status.addClass(statusClass);
    }

    $status
        .text(text)
        .show();
}

function setSectionCollapsed($section, $collapseToggle, isCollapsed, itemType) {
    const labels = getAnalysisLabels();
    const $icon = $collapseToggle.find('i');

    if (itemType) {
        sectionCollapseState[itemType] = isCollapsed;
    }

    $section.toggleClass('collapsed', isCollapsed);
    $collapseToggle
        .attr('aria-expanded', (!isCollapsed).toString())
        .attr('title', isCollapsed ? labels.expandTitle : labels.collapseTitle);

    $icon
        .toggleClass('fa-chevron-down', !isCollapsed)
        .toggleClass('fa-chevron-up', isCollapsed);
}

function setSectionAutoCollapsed($section, $collapseToggle, isCollapsed) {
    const labels = getAnalysisLabels();
    const $icon = $collapseToggle.find('i');

    $section.toggleClass('collapsed', isCollapsed);
    $collapseToggle
        .attr('aria-expanded', (!isCollapsed).toString())
        .attr('title', isCollapsed ? labels.expandTitle : labels.collapseTitle);

    $icon
        .toggleClass('fa-chevron-down', !isCollapsed)
        .toggleClass('fa-chevron-up', isCollapsed);
}

function applySectionCollapseState($section, $items, $collapseToggle, itemType) {
    const hasVisibleItems = getVisibleAnalysisItems($items).length > 0;
    const isCollapsed = hasVisibleItems ? sectionCollapseState[itemType] === true : true;

    $collapseToggle.prop('disabled', !hasVisibleItems);
    if (hasVisibleItems) {
        setSectionCollapsed($section, $collapseToggle, isCollapsed, itemType);
    } else {
        setSectionAutoCollapsed($section, $collapseToggle, true);
    }
}

function configureCollapseToggle($section, $collapseToggle, itemType) {
    $collapseToggle
        .off('click')
        .on('click', function() {
            if ($(this).prop('disabled')) {
                return;
            }

            const isCollapsed = $section.toggleClass('collapsed').hasClass('collapsed');
            setSectionCollapsed($section, $(this), isCollapsed, itemType);
        });
}

function createAnalysisSection(config) {
    const groups = splitFindingsByVisibility(config.items);
    const hasItems = config.items.length > 0;

    return {
        ...config,
        activeItems: groups.activeItems,
        allItems: config.items,
        hiddenItems: groups.hiddenItems,
        hasItems
    };
}

function appendSectionItems($items, section, isDismissedVisible) {
    if (section.activeItems.length === 0 && section.hiddenItems.length === 0) {
        return;
    }

    section.allItems.forEach(item => {
        const isHidden = item.dismissed === true;
        const $item = createFindingItem(item, section.itemType, isHidden);

        if (isHidden && !isDismissedVisible) {
            $item.hide();
        }

        $items.append($item);
    });

    updateVisibleItemLayout($items);
}

function configureDismissedItemsToggle($section, $items, $toggle, $collapseToggle, section, isDismissedVisible) {
    const labels = getAnalysisLabels();
    const hiddenCount = section.hiddenItems.length;

    if (hiddenCount === 0) {
        dismissedSectionVisibility[section.itemType] = false;
        $toggle
            .text(labels.showDismissed)
            .css('visibility', 'hidden')
            .prop('disabled', true)
            .show();
        return;
    }

    $toggle
        .css('visibility', 'visible')
        .text(isDismissedVisible ? labels.hideDismissed : labels.showDismissed)
        .prop('disabled', false)
        .show()
        .off('click')
        .on('click', function() {
            const shouldShow = dismissedSectionVisibility[section.itemType] !== true;
            dismissedSectionVisibility[section.itemType] = shouldShow;
            $items.find('.dismissed-item').toggle(shouldShow);
            updateVisibleItemLayout($items);
            if (shouldShow) {
                // Showing dismissed items should reveal the section body.
                setSectionCollapsed($section, $collapseToggle, false, section.itemType);
            }
            applySectionCollapseState($section, $items, $collapseToggle, section.itemType);
            $toggle.text(shouldShow ? labels.hideDismissed : labels.showDismissed);
        });
}

function renderSection(section) {
    const $section = createItemFromTemplate('section', {
        title: section.title
    });

    $section
        .addClass(section.sectionClass)
        .toggleClass('header-only', !section.hasItems);

    const $items = $section.find('[data-element="items"]');
    const $status = $section.find('[data-element="status-chip"]');
    const $toggle = $section.find('[data-element="hidden-toggle"]');
    const $collapseToggle = $section.find('[data-element="collapse-toggle"]');
    const isDismissedVisible = dismissedSectionVisibility[section.itemType] === true;

    configureSectionStatus($status, section.statusText, section.statusClass);
    configureCollapseToggle($section, $collapseToggle, section.itemType);
    $collapseToggle.toggle(section.hasItems);

    appendSectionItems($items, section, isDismissedVisible);
    configureDismissedItemsToggle($section, $items, $toggle, $collapseToggle, section, isDismissedVisible);
    applySectionCollapseState($section, $items, $collapseToggle, section.itemType);

    return $section;
}

function splitFindingsByVisibility(items) {
    return {
        activeItems: items.filter(item => item.dismissed !== true),
        hiddenItems: items.filter(item => item.dismissed === true)
    };
}

function buildAnalysisSections(analysisData) {
    const labels = getAnalysisLabels();
    const decision = normalizeDecision(analysisData.decision);
    const errors = normalizeFindings(analysisData.errors, 'error');
    const warnings = normalizeFindings(analysisData.warnings, 'warning');
    const summaries = normalizeFindings(analysisData.summaries, 'summary');
    const recommendations = normalizeFindings(analysisData.recommendations, 'recommendation');
    let recommendationStatusText = '';
    if (decision === 'PROCEED') {
        recommendationStatusText = labels.proceed;
    } else if (decision === 'HOLD') {
        recommendationStatusText = labels.hold;
    }

    return {
        sections: [
            createAnalysisSection({
                title: formatSectionTitle(labels.errors, errors.length),
                sectionClass: 'error',
                itemType: 'error',
                items: errors
            }),
            createAnalysisSection({
                title: formatSectionTitle(labels.warnings, warnings.length),
                sectionClass: 'warning',
                itemType: 'warning',
                items: warnings
            }),
            createAnalysisSection({
                title: formatSectionTitle(labels.summaries, summaries.length),
                sectionClass: 'summary',
                itemType: 'summary',
                items: summaries
            }),
            createAnalysisSection({
                title: labels.recommendation,
                sectionClass: 'recommendation',
                itemType: 'recommendation',
                statusText: recommendationStatusText,
                statusClass: decision ? decision.toLowerCase() : '',
                items: recommendations
            })
        ]
    };
}

function bindAnalysisItemActions($sections) {
    $sections.off('click');
    $sections.on('click', '[data-element="dismiss-btn"]', function(e) {
        e.preventDefault();
        const itemId = $(this).data('id');
        dismissAnalysisItem(itemId);
    });

    $sections.on('click', '[data-element="restore-btn"]', function(e) {
        e.preventDefault();
        const itemId = $(this).data('id');
        restoreAnalysisItem(itemId);
    });
}

function renderRealAIAnalysis(analysisData) {
    const { sections } = buildAnalysisSections(analysisData);
    const $sections = $('#aiAnalysisSections');
    $sections.empty();

    sections.forEach(section => {
        $sections.append(renderSection(section));
    });

    const $noDataMessage = $('#aiAnalysisNoData');
    if ($sections.children().length === 0) {
        $noDataMessage.show();
        $sections.hide();
    } else {
        $noDataMessage.hide();
        $sections.show();
    }

    bindAnalysisItemActions($sections);
}

globalThis.dismissAnalysisItem = function(itemId) {
    const applicationId = $('#DetailsViewApplicationId').val();

    unity.grantManager.grantApplications.grantApplication
        .dismissAIAnalysisItem(applicationId, itemId)
        .then(function() {
            loadAIAnalysis();
        })
        .catch(function() {
            abp.message.error('Failed to dismiss the item. Please try again.');
        });
}

globalThis.restoreAnalysisItem = function(itemId) {
    const applicationId = $('#DetailsViewApplicationId').val();

    unity.grantManager.grantApplications.grantApplication
        .restoreAIAnalysisItem(applicationId, itemId)
        .then(function() {
            loadAIAnalysis();
        })
        .catch(function() {
            abp.message.error('Failed to restore the item. Please try again.');
        });
}

function resetAnalysisView() {
    $('#aiAnalysisSections').empty().hide();
    $('#aiAnalysisNoData').show();
}

function tryParseRawAnalysis(analysisJson) {
    if (!analysisJson) {
        return null;
    }

    try {
        let cleaned = String(analysisJson).trim();
        if (cleaned.startsWith('```json') || cleaned.startsWith('```')) {
            const firstBreak = cleaned.indexOf('\n');
            if (firstBreak >= 0) {
                cleaned = cleaned.substring(firstBreak + 1);
            }
        }

        if (cleaned.endsWith('```')) {
            cleaned = cleaned.substring(0, cleaned.lastIndexOf('```'));
        }

        cleaned = cleaned.replaceAll(/,(\s*[}\]])/g, '$1').trim();
        return JSON.parse(cleaned);
    } catch (parseError) {
        console.warn('Failed to parse aiAnalysis JSON fallback:', parseError);
        return null;
    }
}

globalThis.queueApplicationAnalysis = function(triggerButton = null) {
    const applicationId = $('#DetailsViewApplicationId').val();
    const $button = triggerButton ? $(triggerButton) : $('#regenerateApplicationAnalysis');
    const existingHtml = $button.html();

    if (!applicationId || $button.prop('disabled')) {
        return;
    }

    globalThis.AIGenerationButtonState?.setGenerating($button);

    abp.ajax({
        url: `/api/app/ai/generation/application-analysis?applicationId=${encodeURIComponent(applicationId)}`,
        type: 'POST'
    })
        .done(function(generationStatus) {
            const request = generationStatus?.generationRequest;
            const status = globalThis.AIGenerationButtonState?.resolveStatus(request?.status) ?? '';

            if (status === 'Completed') {
                globalThis.AIGenerationButtonState?.restoreForCooldownCheck($button, existingHtml);
                globalThis.AIGenerationButtonState?.applyStatusState(generationStatus);
                loadAIAnalysis();
                return;
            }

            monitorAIAnalysisGeneration(applicationId, $button, existingHtml);
        })
        .fail(function(error) {
            console.error('Failed to queue AI analysis.', error);
            aiAnalysisMonitor?.stop();
            globalThis.AIGenerationButtonState?.restore($button);
            $button.html(existingHtml).prop('disabled', false);
            globalThis.syncAIRateLimitButtons?.();
            abp.message.error('Failed to queue AI analysis. Please try again.');
        });
}

function monitorAIAnalysisGeneration(applicationId, $button, existingHtml) {
    aiAnalysisMonitor?.stop();
    aiAnalysisMonitor = globalThis.AIGenerationButtonState.monitor({
        $button,
        originalHtml: existingHtml,
        getStatus: () => abp.ajax({
            url: `/api/app/ai/generation/status?applicationId=${encodeURIComponent(applicationId)}&operationType=application-analysis`,
            type: 'GET'
        }),
        onComplete: loadAIAnalysis,
        onFailed: (request) => {
            loadAIAnalysis();
            abp.message.error(request?.failureReason || 'AI analysis failed.');
        },
        onPollFailed: (error) => {
            console.warn('Failed to poll AI analysis status.', error);
            abp.message.error('Unable to load AI analysis status. Please try again.');
        }
    });
}

function loadAIAnalysis() {
    if ($('#AIAnalysisFeatureEnabled').val() === 'False') {
        return;
    }

    const urlParams = new URL(globalThis.location.toLocaleString()).searchParams;
    const applicationId = urlParams.get('ApplicationId');
    if (!applicationId) {
        return;
    }

    unity.grantManager.grantApplications.grantApplication.get(applicationId)
        .done(function(application) {
            const aiAnalysisData = application?.aiAnalysisData ?? tryParseRawAnalysis(application?.aiAnalysis);

            if (aiAnalysisData) {
                try {
                    renderRealAIAnalysis(aiAnalysisData);
                } catch (error) {
                    console.warn('Failed to render AI analysis data:', error);
                    resetAnalysisView();
                }
            } else {
                resetAnalysisView();
            }
        })
        .fail(function(error) {
            console.warn('Failed to load application data', error);
            resetAnalysisView();
        });
}

$(function() {
    const $regenerateButton = $('#regenerateApplicationAnalysis');
    if ($regenerateButton.length > 0) {
        $regenerateButton.on('click', function() {
            queueApplicationAnalysis();
        });

        const applicationId = $('#DetailsViewApplicationId').val();
        if (!applicationId) {
            return;
        }

        abp.ajax({
            url: `/api/app/ai/generation/status?applicationId=${encodeURIComponent(applicationId)}&operationType=application-analysis`,
            type: 'GET'
        })
            .done(function(generationStatus) {
                const request = generationStatus?.generationRequest;
                if (request?.isActive !== true) {
                    return;
                }

                const existingHtml = $regenerateButton.html();
                globalThis.AIGenerationButtonState?.setGenerating($regenerateButton);
                monitorAIAnalysisGeneration(applicationId, $regenerateButton, existingHtml);
            });
    }
});
