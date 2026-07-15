(function () {
    function decodeHtmlEntities(value) {
        if (!value || typeof value !== 'string') {
            return '';
        }

        const parser = new DOMParser();
        const doc = parser.parseFromString(value, 'text/html');
        return doc.documentElement.textContent || '';
    }

    function decodeHtmlEntitiesRecursively(value, maxPasses = 3) {
        let current = value || '';

        for (let pass = 0; pass < maxPasses; pass++) {
            const decoded = decodeHtmlEntities(current);
            if (!decoded || decoded === current) {
                break;
            }
            current = decoded;

            if (/<[a-z][\s\S]*>/i.test(current)) {
                break;
            }
        }

        return current;
    }

    function resolveTemplateBodyHtml(template) {
        const rawBody =
            template.body ||
            template.bodyHtml ||
            template.bodyHTML ||
            template.Body ||
            template.BodyHtml ||
            template.BodyHTML ||
            '';

        if (/<[a-z][\s\S]*>/i.test(rawBody)) {
            return rawBody;
        }

        if (
            rawBody.includes('&lt;') ||
            rawBody.includes('&#60;') ||
            rawBody.includes('&amp;lt;') ||
            rawBody.includes('&amp;#60;')
        ) {
            return decodeHtmlEntitiesRecursively(rawBody);
        }

        return rawBody;
    }

    function resolveTemplateSubject(template) {
        return (
            template.subject ||
            template.Subject ||
            ''
        );
    }

    function renderNotificationTemplatePreview(previewElement, template) {
        if (!previewElement) {
            return;
        }

        previewElement.replaceChildren();

        if (!template) {
            return;
        }

        const subjectRow = document.createElement('div');
        const subjectLabel = document.createElement('strong');
        subjectLabel.textContent = 'Subject: ';
        const subjectValue = document.createElement('span');
        subjectValue.textContent = resolveTemplateSubject(template);
        subjectRow.append(subjectLabel, subjectValue);

        const bodyRow = document.createElement('div');
        bodyRow.innerHTML = resolveTemplateBodyHtml(template);

        previewElement.append(subjectRow, document.createElement('br'), bodyRow);
    }

    globalThis.renderNotificationTemplatePreview = renderNotificationTemplatePreview;
})();