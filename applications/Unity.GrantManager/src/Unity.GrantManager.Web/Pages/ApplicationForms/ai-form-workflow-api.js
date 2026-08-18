(function (global) {
    function request(url, type, data = null, contentType = null) {
        const options = { url, type };
        if (data !== null) {
            options.data = contentType === 'application/json' && typeof data !== 'string'
                ? JSON.stringify(data)
                : data;
        }
        if (contentType) {
            options.contentType = contentType;
        }

        return abp.ajax(options);
    }

    function workflowUrl(path, formVersionId, query = '') {
        return `/api/app/application-form-version/${path}?formVersionId=${encodeURIComponent(formVersionId)}${query}`;
    }

    global.AIFormWorkflowApi = {
        startWorksheetReviewPhase(formVersionId) {
            return request(workflowUrl('mapping-review-phase', formVersionId, '&phase=WorksheetReview'), 'POST');
        },
        completeMappingReviewPhase(formVersionId) {
            return request(workflowUrl('mapping-review-phase', formVersionId, '&phase=Completed'), 'POST');
        },
        getMappingReview(formVersionId) {
            return request(workflowUrl('mapping-review', formVersionId), 'GET');
        },
        finalizeMappingReview(formVersionId) {
            return request(workflowUrl('finalize-mapping-review', formVersionId), 'POST');
        },
        acceptMappingSuggestions(formVersionId, suggestionIds) {
            return request(
                workflowUrl('accept-mapping-suggestions', formVersionId),
                'POST',
                { suggestionIds },
                'application/json'
            );
        },
        discardMappingSuggestions(formVersionId) {
            return request(workflowUrl('discard-mapping-suggestions', formVersionId), 'POST');
        },
        resetAiFlow(formVersionId) {
            return request(workflowUrl('reset-ai-flow', formVersionId), 'POST');
        },
        getPendingWorksheet(formVersionId) {
            return request(workflowUrl('pending-ai-worksheet', formVersionId), 'GET');
        },
        createWorksheetDraft(formVersionId, input) {
            return request(
                workflowUrl('create-ai-worksheet-draft', formVersionId),
                'POST',
                {
                    sessionId: input.sessionId,
                    title: input.title,
                    selectedFieldIds: input.selectedFieldIds
                },
                'application/json'
            );
        },
        discardWorksheetSuggestions(formVersionId) {
            return request(workflowUrl('discard-ai-worksheet-suggestions', formVersionId), 'POST');
        },
        getPendingScoresheet(formVersionId) {
            return request(workflowUrl('pending-ai-scoresheet', formVersionId), 'GET');
        },
        createScoresheetDraft(formVersionId, input) {
            return request(
                workflowUrl('create-ai-scoresheet-draft', formVersionId),
                'POST',
                {
                    sessionId: input.sessionId,
                    title: input.title,
                    selectedQuestionIds: input.selectedQuestionIds
                },
                'application/json'
            );
        },
        discardScoresheetSuggestions(formVersionId) {
            return request(workflowUrl('discard-ai-scoresheet-suggestions', formVersionId), 'POST');
        }
    };
})(globalThis);
