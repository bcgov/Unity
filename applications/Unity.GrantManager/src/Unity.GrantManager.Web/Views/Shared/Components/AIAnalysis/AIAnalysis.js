$(function () {
    const aiAnalysisContent = $('#aiAnalysisContent');

    if (aiAnalysisContent.length > 0) {
        // Parse and color-code the AI analysis content
        formatAIAnalysis();
    }

    function formatAIAnalysis() {
        const content = aiAnalysisContent.text();
        const formattedContent = parseAndFormatAnalysis(content);
        aiAnalysisContent.html(formattedContent);
    }

    function parseAndFormatAnalysis(text) {
        if (!text) return '';

        // Split into lines
        const lines = text.split('\n');
        let formatted = '';
        let currentSection = '';
        let inList = false;

        lines.forEach((line, index) => {
            const trimmedLine = line.trim();

            // Skip empty lines
            if (trimmedLine === '') {
                if (inList) {
                    formatted += '</ul>';
                    inList = false;
                }
                formatted += '<br>';
                return;
            }

            // Check for section headers (ALL CAPS, ending with colon)
            if (trimmedLine.match(/^[A-Z\s]+:$/)) {
                if (inList) {
                    formatted += '</ul>';
                    inList = false;
                }
                currentSection = trimmedLine;
                formatted += `<h6 class="ai-section-header">${escapeHtml(trimmedLine)}</h6>`;
                return;
            }

            // Check for subsection headers (numbered or bulleted)
            if (trimmedLine.match(/^\d+\.\s+[A-Z]/)) {
                if (inList) {
                    formatted += '</ul>';
                    inList = false;
                }
                formatted += `<div class="ai-subsection-header">${escapeHtml(trimmedLine)}</div>`;
                return;
            }

            // Check for bullet points
            if (trimmedLine.match(/^[-*•]\s+/)) {
                if (!inList) {
                    formatted += '<ul class="ai-bullet-list">';
                    inList = true;
                }
                const bulletText = trimmedLine.replace(/^[-*•]\s+/, '');
                formatted += `<li>${formatLineWithColors(bulletText)}</li>`;
                return;
            }

            // Regular text
            if (inList) {
                formatted += '</ul>';
                inList = false;
            }
            formatted += `<p class="ai-paragraph">${formatLineWithColors(trimmedLine)}</p>`;
        });

        if (inList) {
            formatted += '</ul>';
        }

        return formatted;
    }

    function formatLineWithColors(text) {
        let formatted = escapeHtml(text);

        // Color-code based on keywords and patterns
        // HIGH / STRONG / EXCELLENT / APPROVED - Green
        formatted = formatted.replace(
            /\b(HIGH|STRONG|EXCELLENT|APPROVED|MEETS|COMPLETE|COMPREHENSIVE|WELL-PREPARED|LOW RISK)\b/gi,
            '<span class="ai-highlight-positive">$1</span>'
        );

        // MEDIUM / MODERATE / ACCEPTABLE - Yellow/Orange
        formatted = formatted.replace(
            /\b(MEDIUM|MODERATE|ACCEPTABLE|MINOR|SOME CONCERNS|PARTIAL)\b/gi,
            '<span class="ai-highlight-moderate">$1</span>'
        );

        // LOW / WEAK / POOR / REJECTED / MISSING - Red
        formatted = formatted.replace(
            /\b(LOW|WEAK|POOR|REJECTED|MISSING|INCOMPLETE|INSUFFICIENT|HIGH RISK|SIGNIFICANT CONCERNS)\b/gi,
            '<span class="ai-highlight-negative">$1</span>'
        );

        // Dollar amounts - Bold
        formatted = formatted.replace(
            /\$[\d,]+(?:\.\d{2})?/g,
            '<strong class="ai-amount">$&</strong>'
        );

        // Percentages - Bold
        formatted = formatted.replace(
            /\b\d+(\.\d+)?%/g,
            '<strong class="ai-percentage">$&</strong>'
        );

        return formatted;
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
});
