/**
 * AI Analysis module for Grant Applications
 * Handles rendering and management of AI-generated analysis results
 */

/**
 * Helper function to create an item from a template
 * @param {string} templateName - Name of the template to clone
 * @param {Object} data - Data to populate the template with
 * @returns {jQuery} Cloned and populated element
 */
function createItemFromTemplate(templateName, data) {
    const $template = $(`[data-template="${templateName}"]`).first();
    const $item = $template.clone().removeAttr('data-template');
    
    // Populate the template with data
    Object.keys(data).forEach(key => {
        const $element = $item.find(`[data-element="${key}"]`);
        if ($element.length) {
            if (key === 'dismiss-btn' || key === 'restore-btn') {
                $element.attr('data-id', data[key].id).attr('data-type', data[key].type);
                if (key === 'dismiss-btn') {
                    $element.attr('title', `Dismiss this ${data[key].type}`);
                } else {
                    $element.attr('title', `Restore this ${data[key].type}`);
                }
            } else if (key === 'icon') {
                $element.addClass(data[key]);
            } else {
                $element.text(data[key]);
            }
        }
    });
    
    return $item;
}

/**
 * Helper function to create an accordion group
 * @param {string} id - Unique ID for the accordion
 * @param {string} type - Type class to add
 * @param {string} iconClass - Icon class
 * @param {string} title - Title text
 * @param {jQuery} content - Content to append
 * @returns {jQuery} Complete accordion group element
 */
function createAccordionGroup(id, type, iconClass, title, content) {
    const $group = createItemFromTemplate('accordion-group', {
        icon: iconClass,
        title: title
    });
    
    const $header = $group.find('[data-element="header"]');
    const $body = $group.find('[data-element="body"]');
    
    $header.addClass(type).attr('data-target', id);
    $body.addClass(type).attr('id', id);
    $body.append(content);
    
    return $group;
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

function getVisibleIssueCount(activeWarnings, activeErrors, summaries) {
    return activeWarnings.length + activeErrors.length + summaries.length;
}

function renderRealAIAnalysis(analysisData) {
    const rawWarnings = analysisData.warnings || [];
    const rawErrors = analysisData.errors || [];
    const rawSummaries = analysisData.summaries || analysisData.recommendations || [];
    const warnings = rawWarnings
        .filter(w => w)
        .map((w, i) => ({
            ...w,
            id: w.id || `warning-${i}`,
            title: w.title || w.category || 'Warning',
            detail: w.detail || w.message || ''
        }));
    const errors = rawErrors
        .filter(e => e)
        .map((e, i) => ({
            ...e,
            id: e.id || `error-${i}`,
            title: e.title || e.category || 'Error',
            detail: e.detail || e.message || ''
        }));
    const summaries = rawSummaries.map((s) => ({
        title: s?.title || s?.category || 'Summary',
        detail: s?.detail || s?.message || s || ''
    }));
    const dismissedItems = new Set((analysisData.dismissed || analysisData.dismissed_items || []).filter(Boolean));

    // Get all valid IDs from current errors and warnings
    const allValidIds = new Set([...errors.map(e => e.id), ...warnings.map(w => w.id)]);

    // Filter dismissed items to only include IDs that exist in current errors/warnings
    const validDismissedItems = new Set(
        Array.from(dismissedItems).filter(id => allValidIds.has(id))
    );

    // Separate active and dismissed items
    const activeErrors = errors.filter(e => !validDismissedItems.has(e.id));
    const activeWarnings = warnings.filter(w => !validDismissedItems.has(w.id));
    const dismissedErrors = errors.filter(e => validDismissedItems.has(e.id));
    const dismissedWarnings = warnings.filter(w => validDismissedItems.has(w.id));

    // Clear existing accordion list and rebuild
    const $accordionList = $('#aiAnalysisAccordionList');
    $accordionList.empty();

    // Add errors section if there are any
    if (activeErrors.length > 0) {
        const errorItems = activeErrors.map(error => {
            return createItemFromTemplate('dismissible-item', {
                category: error.title || 'Error',
                message: getFindingDetailText(error),
                'dismiss-btn': { id: error.id, type: 'error' }
            });
        });

        const $errorsContainer = $('<div>');
        errorItems.forEach(item => $errorsContainer.append(item));
        
        const accordionItem = createAccordionGroup(
            'errors',
            'error',
            'fl-times-circle',
            `Errors (${activeErrors.length})`,
            $errorsContainer
        );
        $accordionList.append(accordionItem);
    }

    // Add warnings section if there are any
    if (activeWarnings.length > 0) {
        const warningItems = activeWarnings.map(warning => {
            return createItemFromTemplate('dismissible-item', {
                category: warning.title || 'Warning',
                message: getFindingDetailText(warning),
                'dismiss-btn': { id: warning.id, type: 'warning' }
            });
        });

        const $warningsContainer = $('<div>');
        warningItems.forEach(item => $warningsContainer.append(item));
        
        const accordionItem = createAccordionGroup(
            'warnings',
            'warning',
            'fl-exclamation-triangle',
            `Warnings (${activeWarnings.length})`,
            $warningsContainer
        );
        $accordionList.append(accordionItem);
    }

    // Add summary section if there are any
    if (summaries.length > 0) {
        const summaryItems = summaries.map(rec => {
            const category = typeof rec === 'object' ? (rec.title || 'Summary') : 'Summary';
            const message = typeof rec === 'object' ? getFindingDetailText(rec) : rec;
            
            return createItemFromTemplate('summary-item', {
                category: category,
                message: message
            });
        });

        const $summaryContainer = $('<div>');
        summaryItems.forEach(item => $summaryContainer.append(item));
        
        const accordionItem = createAccordionGroup(
            'summary',
            'info',
            'fl-info-circle',
            `Summary (${summaries.length})`,
            $summaryContainer
        );
        $accordionList.append(accordionItem);
    }

    // Add dismissed items section if there are any
    if (dismissedErrors.length > 0 || dismissedWarnings.length > 0) {
        const dismissedItems = [
            ...dismissedErrors.map(error => {
                return createItemFromTemplate('dismissed-item', {
                    category: error.title || 'Error',
                    message: getFindingDetailText(error),
                    'restore-btn': { id: error.id, type: 'error' }
                });
            }),
            ...dismissedWarnings.map(warning => {
                return createItemFromTemplate('dismissed-item', {
                    category: warning.title || 'Warning',
                    message: getFindingDetailText(warning),
                    'restore-btn': { id: warning.id, type: 'warning' }
                });
            })
        ];

        const $dismissedContainer = $('<div>');
        dismissedItems.forEach(item => $dismissedContainer.append(item));
        
        const accordionItem = createAccordionGroup(
            'dismissed',
            'dismissed',
            'fl-eye-slash',
            `Dismissed Items (${dismissedErrors.length + dismissedWarnings.length})`,
            $dismissedContainer
        );

        $accordionList.append(accordionItem);
    }

    const totalLength = getVisibleIssueCount(activeWarnings, activeErrors, summaries);
    PubSub.publish('update_ai_analysis_count', {
        itemCount: totalLength,
    });

    // If no items, show the no-data message; otherwise hide it
    const $noDataMessage = $('#aiAnalysisNoData');
    if (activeErrors.length === 0 && activeWarnings.length === 0 && summaries.length === 0 && dismissedErrors.length === 0 && dismissedWarnings.length === 0) {
        $noDataMessage.show();
        $accordionList.hide();
    } else {
        $noDataMessage.hide();
        $accordionList.show();
    }

    // Update tab badge with total count
    const totalIssues = getVisibleIssueCount(activeWarnings, activeErrors, summaries);
    $('#ai-analysis-tab').html(`<i class="fa-solid fa-wand-sparkles" aria-hidden="true"></i>(${totalIssues})`);

    // Remove all previous event handlers from accordion list
    $accordionList.off('click');

    // Use event delegation for dismiss buttons (highest priority)
    $accordionList.on('click', '.ai-analysis-dismiss-btn', function(e) {
        e.preventDefault();
        e.stopPropagation();
        e.stopImmediatePropagation();
        const issueId = $(this).data('id');
        dismissAIIssue(issueId);
        return false;
    });

    // Use event delegation for restore buttons (highest priority)
    $accordionList.on('click', '.ai-analysis-restore-btn', function(e) {
        e.preventDefault();
        e.stopPropagation();
        e.stopImmediatePropagation();
        const issueId = $(this).data('id');
        restoreAIIssue(issueId);
        return false;
    });

    // Add click event listener for accordion headers - check if target is not a button
    $accordionList.on('click', '.ai-analysis-accordion-header', function(e) {
        // Don't toggle if clicking on buttons or their children
        if ($(e.target).closest('.ai-analysis-dismiss-btn, .ai-analysis-restore-btn').length === 0) {
            toggleAccordionItem($(this));
        }
    });
}

globalThis.dismissAIIssue = function(issueId) {
    const applicationId = $('#DetailsViewApplicationId').val();

    unity.grantManager.grantApplications.grantApplication
        .dismissAIIssue(applicationId, issueId)
        .then(function(updatedAnalysis) {
            // Reload the AI analysis section with updated data
            loadAIAnalysis();
        })
        .catch(function(error) {
            abp.message.error('Failed to dismiss the issue. Please try again.');
        });
}

globalThis.restoreAIIssue = function(issueId) {
    const applicationId = $('#DetailsViewApplicationId').val();

    unity.grantManager.grantApplications.grantApplication
        .restoreAIIssue(applicationId, issueId)
        .then(function(updatedAnalysis) {
            // Reload the AI analysis section with updated data
            loadAIAnalysis();
        })
        .catch(function(error) {
            abp.message.error('Failed to restore the issue. Please try again.');
        });
}

globalThis.regenerateAIAnalysis = function() {
    const applicationId = $('#DetailsViewApplicationId').val();
    const $button = $('#regenerateAiAnalysis');
    const existingHtml = $button.html();

    if (!applicationId || $button.prop('disabled')) {
        return;
    }

    $button
        .html('<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span> Regenerating...')
        .prop('disabled', true);

    unity.grantManager.grantApplications.grantApplication
        .generateAIAnalysis(applicationId)
        .then(function() {
            abp.notify.success('AI analysis regenerated successfully.');
            loadAIAnalysis();
        })
        .catch(function() {
            abp.message.error('Failed to regenerate AI analysis. Please try again.');
        })
        .always(function() {
            $button.html(existingHtml).prop('disabled', false);
        });
}

function toggleAccordionItem($header) {
    const targetId = $header.attr('data-target');
    const $body = $('#' + targetId);

    // Toggle active class on header
    $header.toggleClass('active');

    // Toggle body visibility
    $body.slideToggle(300);
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

    // Get the application data including AI analysis
    unity.grantManager.grantApplications.grantApplication.get(applicationId)
        .done(function(application) {
            let aiAnalysisData = application.aiAnalysisData;
            if (!aiAnalysisData && application.aiAnalysis) {
                try {
                    let cleaned = String(application.aiAnalysis).trim();
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
                    aiAnalysisData = JSON.parse(cleaned);
                } catch (parseError) {
                    console.warn('Failed to parse aiAnalysis JSON fallback:', parseError);
                }
            }

            if (application && aiAnalysisData) {
                try {
                    renderRealAIAnalysis(aiAnalysisData);
                } catch (e) {
                    console.warn('Failed to render AI analysis data:', e);
                }
            }
        })
        .fail(function(error) {
            console.warn('Failed to load application data', error);
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
