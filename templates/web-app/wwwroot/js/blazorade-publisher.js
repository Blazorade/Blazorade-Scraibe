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
    },

    /**
     * Shows all .navigation-indicator elements by adding the is-navigating class.
     * Called by ContentPage.razor when a new page starts loading (overlays the old content).
     */
    showNavigationIndicator: function () {
        document.querySelectorAll('.navigation-indicator').forEach(function (el) {
            el.classList.add('is-navigating');
        });
    },

    /**
     * Hides all .navigation-indicator elements by removing the is-navigating class.
     * Called by ContentPage.razor after a page's content has finished loading.
     */
    hideNavigationIndicator: function () {
        document.querySelectorAll('.navigation-indicator').forEach(function (el) {
            el.classList.remove('is-navigating');
        });
    }
};
