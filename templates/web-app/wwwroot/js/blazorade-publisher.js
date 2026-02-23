window.blazoradePublisher = {
    /**
     * Parses an HTML string and returns the innerHTML of the first <main> element.
     * Used by Blazor content pages to extract body content from generated HTML files
     * while ignoring the redirect script and head metadata.
     * @param {string} html - Full HTML page content
     * @returns {string|null} innerHTML of <main>, or null if not found
     */
    extractMain: function (html) {
        const parser = new DOMParser();
        const doc = parser.parseFromString(html, 'text/html');
        const main = doc.querySelector('main');
        return main ? main.innerHTML : null;
    },

    /**
     * Parses an HTML string and returns the serialized head metadata tags suitable
     * for injection into the Blazor page <head> via HeadContent.
     * Includes: <meta name|property> (except charset/viewport), <link rel="canonical">.
     * Excludes: <title>, <script>, <style>, <meta charset>, <meta name="viewport">.
     * @param {string} html - Full HTML page content
     * @returns {string} Serialized HTML string of the extracted head tags
     */
    extractHeadMeta: function (html) {
        const parser = new DOMParser();
        const doc = parser.parseFromString(html, 'text/html');
        const results = [];

        for (const el of doc.head.children) {
            const tag = el.tagName.toLowerCase();

            if (tag === 'meta') {
                const name = (el.getAttribute('name') || '').toLowerCase();
                const prop = (el.getAttribute('property') || '').toLowerCase();
                // Skip charset (has no name/property) and viewport
                if (!name && !prop) continue;
                if (name === 'viewport') continue;
                results.push(el.outerHTML);
            } else if (tag === 'link') {
                const rel = (el.getAttribute('rel') || '').toLowerCase();
                if (rel === 'canonical') {
                    results.push(el.outerHTML);
                }
            }
        }

        return results.join('\n');
    }
};
