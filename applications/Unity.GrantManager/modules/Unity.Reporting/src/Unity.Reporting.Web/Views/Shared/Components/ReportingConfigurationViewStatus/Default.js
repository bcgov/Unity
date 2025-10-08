$(function () {
    let refreshInterval = null;
    let lastKnownStatus = null;
    
    // Subscribe to view status refresh events
    PubSub.subscribe('refresh_view_status', function (msg, data) {
        // Refresh this specific widget when needed
        refreshViewStatusWidget();
    });

    // Function to refresh the view status widget
    function refreshViewStatusWidget() {
        let widgetWrapper = $('[data-widget-name="ReportingConfigurationViewStatus"]');
        if (widgetWrapper.length > 0) {
            let widgetManager = new abp.WidgetManager({
                wrapper: widgetWrapper,
                filterCallback: function () {
                    return {
                        versionId: widgetWrapper.attr('data-version-id'),
                        provider: widgetWrapper.attr('data-provider')
                    };
                }
            });
            widgetManager.refresh();
        }
    }

    // Function to get current status from the DOM
    function getCurrentStatus() {
        if ($('.view-status-compact .status-text-only').length > 0 && 
            $('.view-status-compact .status-text-only').text().includes('Generating')) {
            return 'GENERATING';
        } else if ($('.view-status-compact .fl-checkmark').length > 0) {
            return 'SUCCESS';
        } else if ($('.view-status-compact .fl-cross').length > 0) {
            return 'FAILED';
        }
        return null;
    }

    // Function to check if status is currently generating
    function isGeneratingStatus() {
        return getCurrentStatus() === 'GENERATING';
    }

    // Function to start polling for generating status
    function startGeneratingPoll() {
        if (refreshInterval) {
            clearInterval(refreshInterval);
        }
        
        console.log('Starting auto-refresh for view status (every 5 seconds)...');
        refreshInterval = setInterval(function() {
            // Only refresh if we're still in generating state
            if (isGeneratingStatus()) {
                console.log('Auto-refreshing view status (generating state)...');
                refreshViewStatusWidget();
            } else {
                // Stop polling if no longer generating
                console.log('View generation completed, stopping auto-refresh');
                stopGeneratingPoll();
                
                // Publish completion event
                PubSub.publish('view_generation_completed', { 
                    finalStatus: getCurrentStatus() 
                });
            }
        }, 5000); // 5 seconds
    }

    // Function to stop polling
    function stopGeneratingPoll() {
        if (refreshInterval) {
            clearInterval(refreshInterval);
            refreshInterval = null;
            console.log('Stopped auto-refresh for view status');
        }
    }

    // Monitor for status changes to start/stop polling
    function checkStatusAndManagePolling() {
        const currentStatus = getCurrentStatus();
        
        // If status changed from non-generating to generating, start polling
        if (currentStatus === 'GENERATING' && !refreshInterval) {
            console.log('Detected generating status, starting auto-refresh...');
            lastKnownStatus = currentStatus;
            startGeneratingPoll();
        } 
        // If status changed from generating to something else, stop polling
        else if (lastKnownStatus === 'GENERATING' && currentStatus !== 'GENERATING' && refreshInterval) {
            console.log('Status changed from generating to', currentStatus, ', stopping auto-refresh');
            stopGeneratingPoll();
            
            // Publish completion event
            PubSub.publish('view_generation_completed', { 
                finalStatus: currentStatus 
            });
        }
        
        lastKnownStatus = currentStatus;
    }

    // Preview functionality
    function bindPreviewEvents() {
        $(document).off('click', '.preview-view-btn').on('click', '.preview-view-btn', function(e) {
            e.preventDefault();
            
            const button = $(this);
            const versionId = button.data('version-id');
            const provider = button.data('provider');
            
            // Show loading state
            button.prop('disabled', true);
            button.html('<i class="fa fa-spinner fa-spin"></i> Loading...');
            
            // Call the preview endpoint
            $.ajax({
                url: '/ApplicationForms/ReportingConfigurationViewStatus/PreviewData',
                type: 'GET',
                data: {
                    versionId: versionId,
                    provider: provider
                },
                success: function(response) {
                    if (response.success) {
                        showViewDataModal(response);
                    } else {
                        abp.notify.error(response.message || 'Failed to load view data');
                    }
                },
                error: function(xhr, status, error) {
                    console.error('Error loading view data:', error);
                    abp.notify.error('Error loading view data: ' + error);
                },
                complete: function() {
                    // Reset button state
                    button.prop('disabled', false);
                    button.html('<i class="fa fa-eye"></i> Preview');
                }
            });
        });
    }

    // Function to show view data in modal
    function showViewDataModal(response) {
        const modal = $('#viewDataPreviewModal');
        const content = $('#viewDataPreviewContent');
        
        // Update modal title to reflect preview nature
        $('#viewDataPreviewModalLabel').text(`View Data Preview: ${response.viewName} (Sample Data)`);
        
        // Build table HTML
        let tableHtml = '<div class="table-responsive">';
        
        if (response.data && response.data.length > 0) {
            tableHtml += `
                <div class="mb-3">
                    <div class="alert alert-info">
                        <strong><i class="fa fa-info-circle"></i> Preview Mode:</strong> 
                        Showing sample data from the first application ID found in the view (${response.totalCount} record${response.totalCount !== 1 ? 's' : ''} from this application).
                    </div>
                </div>
                <table class="table table-striped table-hover">
                    <thead class="table-dark">
                        <tr>`;
            
            // Add column headers
            response.columns.forEach(function(column) {
                tableHtml += `<th>${column}</th>`;
            });
            
            tableHtml += '</tr></thead><tbody>';
            
            // Add data rows
            response.data.forEach(function(row) {
                tableHtml += '<tr>';
                response.columns.forEach(function(column) {
                    let value = row[column];
                    if (value === null || value === undefined) {
                        value = '';
                    }
                    // Escape HTML and truncate long values
                    value = $('<div>').text(value.toString()).html();
                    if (value.length > 100) {
                        value = value.substring(0, 100) + '...';
                    }
                    tableHtml += `<td>${value}</td>`;
                });
                tableHtml += '</tr>';
            });
            
            tableHtml += '</tbody></table>';
        } else {
            tableHtml += '<div class="alert alert-warning"><i class="fa fa-exclamation-triangle"></i> No preview data available. The view may be empty or the first application may not have data.</div>';
        }
        
        tableHtml += '</div>';
        
        // Set content and show modal
        content.html(tableHtml);
        
        // Show the modal using Bootstrap 5 syntax
        const modalInstance = new bootstrap.Modal(modal[0]);
        modalInstance.show();
    }

    // Initialize tooltips for status elements
    $(document).ready(function() {
        $('[data-bs-toggle="tooltip"]').tooltip();
        bindPreviewEvents();
        
        // Initial check for generating status
        setTimeout(checkStatusAndManagePolling, 1000);
    });

    // Monitor for changes in the widget content using MutationObserver
    if (window.MutationObserver) {
        const observer = new MutationObserver(function(mutations) {
            mutations.forEach(function(mutation) {
                if (mutation.type === 'childList' || mutation.type === 'characterData') {
                    // Re-initialize tooltips when content changes
                    $('[data-bs-toggle="tooltip"]').tooltip();
                    // Re-bind preview events when content changes
                    bindPreviewEvents();
                    
                    // Check if we need to start/stop polling based on new content
                    setTimeout(checkStatusAndManagePolling, 100);
                }
            });
        });

        // Observe changes in the view status container
        const statusContainer = document.querySelector('.view-status-compact');
        if (statusContainer) {
            observer.observe(statusContainer, {
                childList: true,
                subtree: true,
                characterData: true
            });
        }

        // Also observe for when the container is added/removed from DOM
        const bodyObserver = new MutationObserver(function(mutations) {
            mutations.forEach(function(mutation) {
                mutation.addedNodes.forEach(function(node) {
                    if (node.nodeType === 1) { // Element node
                        const statusElement = node.querySelector ? node.querySelector('.view-status-compact') : null;
                        if (statusElement || (node?.classList?.contains('view-status-compact'))) {
                            // New status element added, start observing it
                            observer.observe(statusElement || node, {
                                childList: true,
                                subtree: true,
                                characterData: true
                            });
                            
                            // Re-bind events and check status after a brief delay
                            setTimeout(() => {
                                bindPreviewEvents();
                                checkStatusAndManagePolling();
                            }, 100);
                        }
                    }
                });
            });
        });

        bodyObserver.observe(document.body, {
            childList: true,
            subtree: true
        });
    }

    // Cleanup on page unload
    $(window).on('beforeunload', function() {
        stopGeneratingPoll();
    });

    // Subscribe to global PubSub events that might indicate status changes
    PubSub.subscribe('view_generation_started', function() {
        setTimeout(checkStatusAndManagePolling, 500);
    });
    
    PubSub.subscribe('view_generation_completed', function() {
        setTimeout(function() {
            stopGeneratingPoll();
            // Refresh one final time to get the completed status
            refreshViewStatusWidget();
        }, 1000);
    });
});