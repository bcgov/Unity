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
