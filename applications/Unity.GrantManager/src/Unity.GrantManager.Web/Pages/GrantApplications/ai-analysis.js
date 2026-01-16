/**
 * AI Analysis module for Grant Applications
 * Handles rendering and management of AI-generated analysis results
 */

// Simple hash function to create stable IDs from content
function simpleHash(str) {
    let hash = 0;
    for (let i = 0; i < str.length; i++) {
        const char = str.codePointAt(i);
        hash = ((hash << 5) - hash) + char;
        hash = hash & hash; // Convert to 32bit integer
    }
    return Math.abs(hash).toString(36);
}

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

function renderRealAIAnalysis(analysisData) {
    // Generate STABLE IDs based on content hash
    const warnings = (analysisData.warnings || []).map((w, i) => ({
        ...w,
        id: w.id || 'warning-' + simpleHash((w.category || '') + (w.message || ''))
    }));
    const errors = (analysisData.errors || []).map((e, i) => ({
        ...e,
        id: e.id || 'error-' + simpleHash((e.category || '') + (e.message || ''))
    }));
    const recommendations = analysisData.recommendations || [];
    const dismissedItems = new Set((analysisData.dismissed_items || []).filter(Boolean));

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
            const errorId = error.id || 'error-unknown-' + Date.now();
            return createItemFromTemplate('dismissible-item', {
                category: error.category || 'Error',
                message: error.message || '',
                'dismiss-btn': { id: errorId, type: 'error' }
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
            const warningId = warning.id || 'warning-unknown-' + Date.now();
            return createItemFromTemplate('dismissible-item', {
                category: warning.category || 'Warning',
                message: warning.message || '',
                'dismiss-btn': { id: warningId, type: 'warning' }
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

    // Add recommendations section if there are any
    if (recommendations.length > 0) {
        const recommendationItems = recommendations.map(rec => {
            const category = typeof rec === 'object' ? (rec.category || 'Recommendation') : 'Recommendation';
            const message = typeof rec === 'object' ? (rec.message || rec) : rec;
            
            return createItemFromTemplate('recommendation-item', {
                category: category,
                message: message
            });
        });

        const $recommendationsContainer = $('<div>');
        recommendationItems.forEach(item => $recommendationsContainer.append(item));
        
        const accordionItem = createAccordionGroup(
            'recommendations',
            'info',
            'fl-info-circle',
            `Recommendations (${recommendations.length})`,
            $recommendationsContainer
        );
        $accordionList.append(accordionItem);
    }

    // Add dismissed items section if there are any
    if (dismissedErrors.length > 0 || dismissedWarnings.length > 0) {
        const dismissedItems = [
            ...dismissedErrors.map(error => {
                const errorId = error.id || 'error-dismissed-' + Date.now();
                return createItemFromTemplate('dismissed-item', {
                    category: error.category || 'Error',
                    message: error.message || '',
                    'restore-btn': { id: errorId, type: 'error' }
                });
            }),
            ...dismissedWarnings.map(warning => {
                const warningId = warning.id || 'warning-dismissed-' + Date.now();
                return createItemFromTemplate('dismissed-item', {
                    category: warning.category || 'Warning',
                    message: warning.message || '',
                    'restore-btn': { id: warningId, type: 'warning' }
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

    // If no items, show the no-data message; otherwise hide it
    const $noDataMessage = $('#aiAnalysisNoData');
    if (activeErrors.length === 0 && activeWarnings.length === 0 && recommendations.length === 0 && dismissedErrors.length === 0 && dismissedWarnings.length === 0) {
        $noDataMessage.show();
        $accordionList.hide();
    } else {
        $noDataMessage.hide();
        $accordionList.show();
    }

    // Update tab badge with total count
    const totalIssues = activeWarnings.length + activeErrors.length + recommendations.length;
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

function toggleAccordionItem($header) {
    const targetId = $header.attr('data-target');
    const $body = $('#' + targetId);

    // Toggle active class on header
    $header.toggleClass('active');

    // Toggle body visibility
    $body.slideToggle(300);
}

function renderDemoAIAnalysis() {
    console.log('Using demo AI analysis data');

    // Demo data
    const demoData = {
        errors: [
            {
                category: "Missing Required Documentation",
                message: "The application is missing the required financial statements for the last fiscal year. This is a mandatory requirement for grant eligibility."
            },
            {
                category: "Budget Calculation Error",
                message: "The total project budget does not match the sum of individual line items. There is a discrepancy of $15,000 that needs to be resolved."
            }
        ],
        warnings: [
            {
                category: "Incomplete Project Timeline",
                message: "The project timeline section is missing specific milestone dates. While not mandatory, providing detailed timelines strengthens the application."
            },
            {
                category: "Limited Partnership Details",
                message: "Partnership letters are provided but lack specific commitment amounts or resource contributions from partners."
            },
            {
                category: "Vague Impact Metrics",
                message: "The expected outcomes section lacks specific, measurable impact metrics. Consider adding quantifiable targets and KPIs."
            }
        ],
        recommendations: []
    };

    renderRealAIAnalysis(demoData);
}

function loadAIAnalysis() {
    if($('#AIAnalysisFeatureEnabled') == 'False') {
        return;
    }
    const urlParams = new URL(globalThis.location.toLocaleString()).searchParams;
    const applicationId = urlParams.get('ApplicationId');

    if (!applicationId) {
        renderDemoAIAnalysis();
        return;
    }

    // Get the application data including AI analysis
    unity.grantManager.grantApplications.grantApplication.get(applicationId)
        .done(function(application) {
            console.log('Application data received:', application);
            console.log('AI Analysis field:', application.aiAnalysis);

            // Use the camelCase version that should come from the API
            const aiAnalysis = application.aiAnalysis;

            if (application && aiAnalysis) {
                try {
                    console.log('Raw AI analysis:', aiAnalysis);

                    // Clean the JSON response (remove markdown code blocks if present)
                    let cleanedJson = aiAnalysis.trim();
                    if (cleanedJson.startsWith('```json') || cleanedJson.startsWith('```')) {
                        const startIndex = cleanedJson.indexOf('\n');
                        if (startIndex >= 0) {
                            cleanedJson = cleanedJson.substring(startIndex + 1);
                        }
                    }
                    if (cleanedJson.endsWith('```')) {
                        const lastIndex = cleanedJson.lastIndexOf('```');
                        if (lastIndex > 0) {
                            cleanedJson = cleanedJson.substring(0, lastIndex);
                        }
                    }
                    cleanedJson = cleanedJson.trim();

                    const analysisData = JSON.parse(cleanedJson);
                    console.log('Parsed analysis data:', analysisData);
                    renderRealAIAnalysis(analysisData);
                } catch (e) {
                    console.warn('Failed to parse AI analysis JSON, showing demo data:', e);
                }
            } else {
                console.log('No AI analysis found, showing demo');             
            }
        })
        .fail(function(error) {
            console.warn('Failed to load application data, showing demo AI analysis', error);
        });
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}