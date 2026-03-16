window.blazoradePublisher = {
    /**
     * Parses an HTML string and returns the published layout container HTML.
     * The publisher writes one layout root element in <body> with a class that starts
     * with "blz-layout-". Returning this root preserves nav/footer/parts at runtime.
     * Falls back to the previous <main> extraction behavior for compatibility.
     * @param {string} html - Full HTML page content
     * @returns {string|null} outerHTML of layout root, or main.innerHTML as fallback
     */
    extractPageContent: function (html) {
        const parser = new DOMParser();
        const doc = parser.parseFromString(html, 'text/html');

        for (const el of doc.body.children) {
            for (const cls of el.classList) {
                if (cls.startsWith('blz-layout-')) {
                    return el.outerHTML;
                }
            }
        }

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

if (!window.__blazoradeNestedDropdownInit) {
    window.__blazoradeNestedDropdownInit = true;

    // Use delegated events so newly rendered navigation trees work without re-initialization.
    document.addEventListener('click', function (event) {
        const toggle = event.target.closest('.dropdown-submenu > .dropdown-toggle');
        if (!toggle) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        if (typeof event.stopImmediatePropagation === 'function') {
            event.stopImmediatePropagation();
        }

        const submenu = toggle.nextElementSibling;
        if (!submenu || !submenu.classList.contains('dropdown-menu')) {
            return;
        }

        const parentMenu = toggle.closest('.dropdown-menu');
        if (parentMenu) {
            parentMenu.querySelectorAll(':scope > .dropdown-submenu > .dropdown-menu.show').forEach(function (openMenu) {
                if (openMenu !== submenu) {
                    openMenu.classList.remove('show');
                    const openToggle = openMenu.previousElementSibling;
                    if (openToggle && openToggle.classList.contains('dropdown-toggle')) {
                        openToggle.setAttribute('aria-expanded', 'false');
                    }
                }
            });
        }

        const willOpen = !submenu.classList.contains('show');
        submenu.classList.toggle('show', willOpen);
        toggle.setAttribute('aria-expanded', willOpen ? 'true' : 'false');
    }, true);

    // When a top-level dropdown closes, clear all nested submenu state.
    document.addEventListener('hidden.bs.dropdown', function (event) {
        if (!(event.target instanceof Element)) {
            return;
        }

        const dropdown = event.target.classList.contains('dropdown')
            ? event.target
            : event.target.closest('.dropdown');
        if (!dropdown) {
            return;
        }

        dropdown.querySelectorAll('.dropdown-submenu > .dropdown-menu.show').forEach(function (openMenu) {
            openMenu.classList.remove('show');
            const openToggle = openMenu.previousElementSibling;
            if (openToggle && openToggle.classList.contains('dropdown-toggle')) {
                openToggle.setAttribute('aria-expanded', 'false');
            }
        });
    });
}
