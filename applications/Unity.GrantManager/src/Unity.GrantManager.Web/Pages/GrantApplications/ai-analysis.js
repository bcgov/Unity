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
            hidden: item.hidden === true,
            title: item.title || item.category || fallbackTitles[fallbackType] || 'Item',
            detail: item.detail || item.message || ''
        }));
}

function createFindingItem(item, type, hidden) {
    const templateName = hidden ? 'hidden-item' : 'active-item';
    const actionKey = hidden ? 'restore-btn' : 'dismiss-btn';
    return createItemFromTemplate(templateName, {
        category: item.title,
        message: getFindingDetailText(item),
        [actionKey]: { id: item.id, type: type }
    });
}

function updateVisibleItemLayout($items) {
    const $allItems = $items.children('.ai-analysis-detail-item');
    const $visibleItems = $allItems.filter(function() {
        return this.style.display !== 'none';
    });

    $allItems.removeClass('last-visible');
    $visibleItems.last().addClass('last-visible');
}

function configureSectionDecision($status, decision) {
    if (!decision) {
        return;
    }

    $status
        .addClass('ai-analysis-status-chip')
        .addClass(decision.toLowerCase())
        .text(decision === 'PROCEED' ? 'Proceed' : 'Hold')
        .show();
}

function configureCollapseToggle($section, $collapseToggle) {
    $collapseToggle
        .off('click')
        .on('click', function() {
            const isCollapsed = $section.toggleClass('collapsed').hasClass('collapsed');
            const $icon = $(this).find('i');

            $(this)
                .attr('aria-expanded', (!isCollapsed).toString())
                .attr('title', isCollapsed ? 'Expand section' : 'Collapse section');

            $icon
                .toggleClass('fa-chevron-down', !isCollapsed)
                .toggleClass('fa-chevron-up', isCollapsed);
        });
}

function renderHeaderOnlySection($section, $status, $toggle, $collapseToggle, section) {
    $section.addClass('header-only');

    if (section.decision) {
        $status.show();
    } else {
        $status
            .addClass('ai-analysis-status-chip')
            .text(section.headerOnlyText)
            .show();
    }

    $toggle.hide();
    $collapseToggle.hide();
    return $section;
}

function appendSectionItems($items, section, isDismissedVisible) {
    if (section.activeItems.length === 0 && section.hiddenItems.length === 0) {
        return;
    }

    section.allItems.forEach(item => {
        const isHidden = item.hidden === true;
        const $item = createFindingItem(item, section.itemType, isHidden);

        if (isHidden && !isDismissedVisible) {
            $item.hide();
        }

        $items.append($item);
    });

    updateVisibleItemLayout($items);
}

function configureDismissedItemsToggle($items, $toggle, section, isDismissedVisible) {
    const hiddenCount = section.hiddenItems.length;

    if (hiddenCount === 0) {
        dismissedSectionVisibility[section.itemType] = false;
        $toggle
            .text('Show dismissed items')
            .css('visibility', 'hidden')
            .prop('disabled', true)
            .show();
        return;
    }

    $toggle
        .css('visibility', 'visible')
        .text(isDismissedVisible ? 'Hide dismissed items' : 'Show dismissed items')
        .prop('disabled', false)
        .show()
        .off('click')
        .on('click', function() {
            const shouldShow = dismissedSectionVisibility[section.itemType] !== true;
            dismissedSectionVisibility[section.itemType] = shouldShow;
            $items.find('.hidden-item').toggle(shouldShow);
            updateVisibleItemLayout($items);
            $toggle.text(shouldShow ? 'Hide dismissed items' : 'Show dismissed items');
        });
}

function renderSection(section) {
    const $section = createItemFromTemplate('section', {
        title: section.title
    });

    $section.addClass(section.sectionClass);
    if (section.activeItems.length === 0) {
        $section.addClass('compact');
    }

    const $items = $section.find('[data-element="items"]');
    const $status = $section.find('[data-element="status-chip"]');
    const $toggle = $section.find('[data-element="hidden-toggle"]');
    const $collapseToggle = $section.find('[data-element="collapse-toggle"]');
    const isDismissedVisible = dismissedSectionVisibility[section.itemType] === true;

    configureSectionDecision($status, section.decision);
    configureCollapseToggle($section, $collapseToggle);

    if (section.headerOnlyText) {
        return renderHeaderOnlySection($section, $status, $toggle, $collapseToggle, section);
    }

    appendSectionItems($items, section, isDismissedVisible);
    configureDismissedItemsToggle($items, $toggle, section, isDismissedVisible);

    return $section;
}

function splitFindingsByVisibility(items) {
    return {
        activeItems: items.filter(item => item.hidden !== true),
        hiddenItems: items.filter(item => item.hidden === true)
    };
}

function buildAnalysisSections(analysisData) {
    const decision = normalizeDecision(analysisData.decision);
    const errors = normalizeFindings(analysisData.errors, 'error');
    const warnings = normalizeFindings(analysisData.warnings, 'warning');
    const summaries = normalizeFindings(analysisData.summaries, 'summary');
    const recommendations = normalizeFindings(analysisData.recommendations, 'recommendation');
    const errorGroups = splitFindingsByVisibility(errors);
    const warningGroups = splitFindingsByVisibility(warnings);
    const summaryGroups = splitFindingsByVisibility(summaries);
    const recommendationGroups = splitFindingsByVisibility(recommendations);

    return {
        sections: [
            {
                title: 'Errors',
                sectionClass: 'error',
                itemType: 'error',
                activeItems: errorGroups.activeItems,
                allItems: errors,
                hiddenItems: errorGroups.hiddenItems
            },
            {
                title: 'Warnings',
                sectionClass: 'warning',
                itemType: 'warning',
                activeItems: warningGroups.activeItems,
                allItems: warnings,
                hiddenItems: warningGroups.hiddenItems
            },
            {
                title: 'Summary',
                sectionClass: 'summary',
                itemType: 'summary',
                headerOnlyText: summaryGroups.activeItems.length === 0 && summaryGroups.hiddenItems.length === 0 ? 'No summary' : null,
                activeItems: summaryGroups.activeItems,
                allItems: summaries,
                hiddenItems: summaryGroups.hiddenItems
            },
            {
                title: 'Recommendations',
                sectionClass: 'recommendations',
                itemType: 'recommendation',
                decision: decision,
                headerOnlyText: recommendationGroups.activeItems.length === 0 && recommendationGroups.hiddenItems.length === 0 ? 'No recommendations' : null,
                activeItems: recommendationGroups.activeItems,
                allItems: recommendations,
                hiddenItems: recommendationGroups.hiddenItems
            }
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
        if ((section.itemType === 'error' || section.itemType === 'warning') && section.allItems.length === 0) {
            return;
        }

        $sections.append(renderSection(section));
    });

    const $noDataMessage = $('#aiAnalysisNoData');
    $noDataMessage.hide();
    $sections.show();

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
    const promptVersion = globalThis.getSelectedPromptVersion?.() || null;

    if (!applicationId || $button.prop('disabled')) {
        return;
    }

    $button
        .html('<span class="ai-button-content"><span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span><span>Queueing...</span></span>')
        .prop('disabled', true);

    unity.grantManager.grantApplications.applicationAnalysis
        .generateApplicationAnalysis(applicationId, promptVersion)
        .then(function() {
            abp.notify.success('AI analysis queued. Refresh later to see updated results.');
        })
        .catch(function() {
            abp.message.error('Failed to queue AI analysis. Please try again.');
        })
        .always(function() {
            $button.html(existingHtml).prop('disabled', false);
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
    }
});
