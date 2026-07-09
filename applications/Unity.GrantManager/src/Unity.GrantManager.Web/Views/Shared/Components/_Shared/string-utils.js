// Mirrors Unity.Modules.Shared.Utils.StringExtensions.CompareStrings (bigram Dice coefficient)
function wordLetterPairs(str) {
    let pairs = [];
    let words = str.split(/\s+/);
    for (const word of words) {
        if (!word) continue;
        for (let j = 0; j < word.length - 1; j++) {
            pairs.push(word[j] + word[j + 1]);
        }
    }
    return pairs;
}

// Returns a match percentage (0–100), mirroring C# StringExtensions.CompareStrings.
// pairs1 is a plain array (duplicates kept); pairs2 is deduplicated (mirrors HashSet).
// intersection = count of pairs1 items found in set2, each match deletes the key.
// union = pairs1.length + remaining set2 size after deletions.
function compareStrings(str1, str2) {
    if (!str1 || !str2) return 0;
    let pairs1 = wordLetterPairs(str1.toUpperCase());
    let rawPairs2 = wordLetterPairs(str2.toUpperCase());
    let set2 = {};
    for (const p of rawPairs2) {
        set2[p] = true;
    }
    let intersection = 0;
    for (const p of pairs1) {
        if (Object.hasOwn(set2, p)) {
            intersection++;
            delete set2[p];
        }
    }
    let union = pairs1.length + Object.keys(set2).length;
    if (union === 0) return 0;
    return Math.round(Math.min(2 * intersection * 100 / union, 100) * 100) / 100;
}

/**
 * Strips HTML tags from a string using textContent (safe DOM API)
 * Safely extracts plain text content without interpreting HTML meta-characters
 * @param {string} html - HTML string to strip
 * @returns {string} Plain text content (safe for all contexts)
 */
function stripHtml(html) {
    if (!html) return '';
    // Use textContent which is inherently safe - never interprets HTML
    if (typeof document !== 'undefined') {
        try {
            const temp = document.createElement('div');
            temp.textContent = String(html);
            return temp.textContent;
        } catch {
            // Fall through to regex fallback if DOM is unavailable
        }
    }
    
    // Server-side or DOM unavailable: remove HTML tag delimiters directly.
    // Character-level replacement avoids incomplete multi-character sanitization bypasses.
    return String(html).replace(/[<>]/g, '');
}

/**
 * Escapes HTML meta-characters in a string for safe use in HTML attributes
 * @param {string} value - String to escape
 * @returns {string} Escaped string safe for use in HTML attributes
 */
function escapeHtmlAttribute(value) {
    return String(value ?? '')
        .replaceAll('&', '&amp;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#39;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;');
}

/**
 * Validates GUID format
 * @param {string} textString - String to validate as GUID
 * @returns {boolean} True if valid GUID format
 */
function validateGuid(textString) {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(String(textString ?? '').trim());
}
