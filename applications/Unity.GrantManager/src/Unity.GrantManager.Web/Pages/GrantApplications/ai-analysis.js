/**
 * AI Analysis module for Grant Applications
 * Renders a stable sectioned view for AI-generated analysis results.
 */

const hiddenSectionVisibility = {
    error: false,
    warning: false,
    summary: false,
    nextStep: false
};

function bindTemplateAction($element, actionData) {
    $element.attr('data-id', actionData.id).attr('data-type', actionData.type);
}

function bindTemplateValue($item, key, value) {
    const $element = $item.find(`[data-element="${key}"]`);
    if ($element.length === 0) {
        return;
    }

    if (key === 'hide-btn' || key === 'show-btn') {
        bindTemplateAction($element, value);
        return;
    }

    if (key === 'icon') {
        $element.addClass(value);
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

function updateAnalysisTabStatus(recommendation) {
    let status = '';
    if (recommendation) {
        status = recommendation.decision === 'PROCEED' ? 'proceed' : 'hold';
    }

    PubSub.publish('update_ai_analysis_status', {
        status: status
    });
}

function normalizeFindings(items, fallbackType) {
    const fallbackTitles = {
        error: 'Error',
        warning: 'Warning',
        summary: 'Summary',
        nextStep: 'Next step'
    };

    return (items || [])
        .filter(item => item)
        .map((item, index) => ({
            ...item,
            id: item.id || `${fallbackType}-${index}`,
            hidden: item.hidden === true,
            title: item.title || item.category || fallbackTitles[fallbackType] || 'Item',
            detail: item.detail || item.message || ''
        }));
}

function normalizeRecommendation(recommendation) {
    if (!recommendation || typeof recommendation !== 'object') {
        return null;
    }

    const decision = typeof recommendation.decision === 'string'
        ? recommendation.decision.trim().toUpperCase()
        : '';
    const rationale = typeof recommendation.rationale === 'string'
        ? recommendation.rationale.trim()
        : '';

    if (decision !== 'PROCEED' && decision !== 'HOLD') {
        return null;
    }

    return {
        decision: decision,
        rationale: rationale
    };
}

function createFindingItem(item, type, hidden) {
    const templateName = hidden ? 'hidden-item' : 'active-item';
    const actionKey = hidden ? 'show-btn' : 'hide-btn';
    return createItemFromTemplate(templateName, {
        category: item.title,
        message: getFindingDetailText(item),
        [actionKey]: { id: item.id, type: type }
    });
}

function renderSection(section) {
    const $section = createItemFromTemplate('section', {
        icon: section.icon,
        title: section.title
    });

    $section.addClass(section.sectionClass);
    if (section.activeItems.length === 0) {
        $section.addClass('compact');
    }

    const $items = $section.find('[data-element="items"]');
    const $status = $section.find('[data-element="status-chip"]');
    const $toggle = $section.find('[data-element="hidden-toggle"]');
    const hiddenCount = section.hiddenItems.length;
    const isHiddenVisible = hiddenSectionVisibility[section.itemType] === true;

    if (section.headerOnlyText) {
        $section.addClass('header-only');
        $status
            .addClass('ai-analysis-status-chip')
            .text(section.headerOnlyText)
            .show();
        $toggle.hide();
        return $section;
    }

    if (section.activeItems.length > 0 || hiddenCount > 0) {
        section.allItems.forEach(item => {
            const isHidden = item.hidden === true;
            const $item = createFindingItem(item, section.itemType, isHidden);
            if (isHidden && !isHiddenVisible) {
                $item.hide();
            }

            $items.append($item);
        });
    }

    if (hiddenCount > 0) {
        $toggle
            .css('visibility', 'visible')
            .text(isHiddenVisible
                ? 'Hide hidden items'
                : 'Show hidden items')
            .prop('disabled', false)
            .show()
            .off('click')
            .on('click', function() {
                const shouldShow = hiddenSectionVisibility[section.itemType] !== true;
                hiddenSectionVisibility[section.itemType] = shouldShow;
                $items.find('.hidden-item').toggle(shouldShow);
                $toggle.text(
                    shouldShow
                        ? 'Hide hidden items'
                        : 'Show hidden items'
                );
            });
    } else {
        hiddenSectionVisibility[section.itemType] = false;
        $toggle
            .text('Show hidden items')
            .css('visibility', 'hidden')
            .prop('disabled', true)
            .show();
    }

    return $section;
}

function renderRecommendationSection(recommendation) {
    if (!recommendation) {
        return null;
    }

    const shouldProceed = recommendation.decision === 'PROCEED';
    const $section = createItemFromTemplate('section', {
        icon: 'fl-info-circle',
        title: 'Recommendation'
    });

    $section.addClass('recommendation compact');
    $section.find('[data-element="status-chip"]')
        .addClass('ai-analysis-status-badge')
        .addClass(shouldProceed ? 'proceed' : 'hold')
        .text(shouldProceed ? 'Proceed' : 'Hold')
        .show();
    $section.find('[data-element="hidden-toggle"]').remove();
    $section.find('[data-element="items"]').append(
        $('<div class="ai-analysis-recommendation-rationale"></div>')
            .text(recommendation.rationale || 'No rationale provided.')
    );

    return $section;
}

function splitFindingsByVisibility(items) {
    return {
        activeItems: items.filter(item => item.hidden !== true),
        hiddenItems: items.filter(item => item.hidden === true)
    };
}

function buildAnalysisSections(analysisData) {
    const recommendation = normalizeRecommendation(analysisData.recommendation);
    const errors = normalizeFindings(analysisData.errors, 'error');
    const warnings = normalizeFindings(analysisData.warnings, 'warning');
    const summaries = normalizeFindings(analysisData.summaries || analysisData.recommendations, 'summary');
    const nextSteps = normalizeFindings(analysisData.nextSteps, 'nextStep');
    const errorGroups = splitFindingsByVisibility(errors);
    const warningGroups = splitFindingsByVisibility(warnings);
    const summaryGroups = splitFindingsByVisibility(summaries);
    const nextStepGroups = splitFindingsByVisibility(nextSteps);

    return {
        recommendation,
        sections: [
            {
                title: 'Errors',
                icon: 'fl-times-circle',
                sectionClass: 'error',
                itemType: 'error',
                headerOnlyText: errorGroups.activeItems.length === 0 && errorGroups.hiddenItems.length === 0 ? 'No errors' : null,
                activeItems: errorGroups.activeItems,
                allItems: errors,
                hiddenItems: errorGroups.hiddenItems
            },
            {
                title: 'Warnings',
                icon: 'fl-exclamation-triangle',
                sectionClass: 'warning',
                itemType: 'warning',
                headerOnlyText: warningGroups.activeItems.length === 0 && warningGroups.hiddenItems.length === 0 ? 'No warnings' : null,
                activeItems: warningGroups.activeItems,
                allItems: warnings,
                hiddenItems: warningGroups.hiddenItems
            },
            {
                title: 'Summary',
                icon: 'fl-info-circle',
                sectionClass: 'summary',
                itemType: 'summary',
                headerOnlyText: summaryGroups.activeItems.length === 0 && summaryGroups.hiddenItems.length === 0 ? 'No summary' : null,
                activeItems: summaryGroups.activeItems,
                allItems: summaries,
                hiddenItems: summaryGroups.hiddenItems
            },
            {
                title: 'Next Steps',
                icon: 'fl-check-square',
                sectionClass: 'next-steps',
                itemType: 'nextStep',
                headerOnlyText: nextStepGroups.activeItems.length === 0 && nextStepGroups.hiddenItems.length === 0 ? 'No next steps' : null,
                activeItems: nextStepGroups.activeItems,
                allItems: nextSteps,
                hiddenItems: nextStepGroups.hiddenItems
            }
        ]
    };
}

function hasAnyAnalysisContent(recommendation, sections) {
    if (recommendation) {
        return true;
    }

    return sections.some(section => section.allItems.length > 0);
}

function bindAnalysisItemActions($sections) {
    $sections.off('click');
    $sections.on('click', '[data-element="hide-btn"]', function(e) {
        e.preventDefault();
        const itemId = $(this).data('id');
        hideAnalysisItem(itemId);
    });

    $sections.on('click', '[data-element="show-btn"]', function(e) {
        e.preventDefault();
        const itemId = $(this).data('id');
        showAnalysisItem(itemId);
    });
}

function renderRealAIAnalysis(analysisData) {
    const { recommendation, sections } = buildAnalysisSections(analysisData);
    const $sections = $('#aiAnalysisSections');
    $sections.empty();
    const $recommendationSection = renderRecommendationSection(recommendation);
    const hasRecommendation = $recommendationSection !== null;
    if ($recommendationSection) {
        $sections.append($recommendationSection);
    }

    sections.forEach(section => {
        $sections.append(renderSection(section));
    });

    updateAnalysisTabStatus(recommendation);

    const $noDataMessage = $('#aiAnalysisNoData');
    if (!hasRecommendation && !hasAnyAnalysisContent(recommendation, sections)) {
        $noDataMessage.show();
        $sections.hide();
    } else {
        $noDataMessage.hide();
        $sections.show();
    }

    bindAnalysisItemActions($sections);
}

globalThis.hideAnalysisItem = function(itemId) {
    const applicationId = $('#DetailsViewApplicationId').val();

    unity.grantManager.grantApplications.grantApplication
        .hideAIAnalysisItem(applicationId, itemId)
        .then(function() {
            loadAIAnalysis();
        })
        .catch(function() {
            abp.message.error('Failed to hide the item. Please try again.');
        });
}

globalThis.showAnalysisItem = function(itemId) {
    const applicationId = $('#DetailsViewApplicationId').val();

    unity.grantManager.grantApplications.grantApplication
        .showAIAnalysisItem(applicationId, itemId)
        .then(function() {
            loadAIAnalysis();
        })
        .catch(function() {
            abp.message.error('Failed to show the item. Please try again.');
        });
}

function resetAnalysisView() {
    $('#aiAnalysisSections').empty().hide();
    $('#aiAnalysisNoData').show();
    updateAnalysisTabStatus(null);
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

globalThis.regenerateAIAnalysis = function(capturePromptIo = false, triggerButton = null) {
    const applicationId = $('#DetailsViewApplicationId').val();
    const $button = triggerButton ? $(triggerButton) : $('#regenerateAiAnalysis');
    const existingHtml = $button.html();
    const promptVersion = globalThis.getSelectedPromptVersion?.() || null;

    if (!applicationId || $button.prop('disabled')) {
        return;
    }

    if (!capturePromptIo && globalThis.hideAIPromptCapture) {
        globalThis.hideAIPromptCapture('#aiAnalysisPromptCaptureContainer', '#aiAnalysisPromptCaptureOutput');
    }

    $button
        .html('<span class="ai-button-content"><span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span><span>Generating...</span></span>')
        .prop('disabled', true);

    unity.grantManager.grantApplications.applicationAIAnalysis
        .generateAIAnalysis(applicationId, promptVersion, capturePromptIo)
        .then(function() {
            abp.notify.success('AI analysis refreshed successfully.');
            loadAIAnalysis();
            if (capturePromptIo && globalThis.loadAIPromptCapture) {
                return globalThis.loadAIPromptCapture(
                    applicationId,
                    'ApplicationAnalysis',
                    promptVersion,
                    '#aiAnalysisPromptCaptureContainer',
                    '#aiAnalysisPromptCaptureOutput'
                );
            }
        })
        .catch(function() {
            abp.message.error('Failed to refresh AI analysis. Please try again.');
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
    const $regenerateButton = $('#regenerateAiAnalysis');
    if ($regenerateButton.length > 0) {
        $regenerateButton.on('click', function() {
            regenerateAIAnalysis();
        });
    }
});
