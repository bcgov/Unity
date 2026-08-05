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

    global.AIGenerationApi = {
        queueApplicationAnalysis(applicationId) {
            return request(
                `/api/app/ai/generation/application-analysis?applicationId=${encodeURIComponent(applicationId)}`,
                'POST'
            );
        },
        queueApplicationScoring(applicationId) {
            return request(
                `/api/app/ai/generation/application-scoring?applicationId=${encodeURIComponent(applicationId)}`,
                'POST'
            );
        },
        queueAttachmentSummary(input) {
            return request(
                '/api/app/ai/generation/attachment-summary',
                'POST',
                {
                    applicationId: input.applicationId,
                    attachmentIds: input.attachmentIds || [],
                    promptVersion: input.promptVersion || null,
                },
                'application/json'
            );
        },
        getStatus(applicationId, operationType) {
            return request(
                `/api/app/ai/generation/status?applicationId=${encodeURIComponent(applicationId)}&operationType=${encodeURIComponent(operationType)}`,
                'GET'
            );
        },
    };
})(globalThis);
