---
title: Page Layouts
description: How to use named layouts to control page chrome — navbar, footer, and additional content parts — in Blazorade Scraibe.
keywords: page layout, layout, content parts, _name.md, Part shortcode, nav, footer, x-part
changefreq: monthly
priority: 0.8
---

# Page Layouts

Every published page is rendered inside a **layout** — a static HTML file that defines the page chrome: navbar, footer, content area, and any additional structural elements. The layout system lets you choose different structures for different pages, such as a landing page without a nav or a two-column page with a sidebar.

## Choosing a Layout

Set the `layout` frontmatter field to the name of the layout you want. The value is case-insensitive; omit the field entirely to use the default layout.

```yaml
---
title: My Page
layout: default
---
```

Available layouts are the `.html` files in the `Layouts/` folder of the component library. The two built-in layouts are:

- **Default** — Bootstrap navbar, a main content area, and a footer. This is the layout used when no `layout` field is set.
- **Landing** — Full-width main content only. No navbar, no footer. Useful for home pages and marketing pages.

To use the landing layout:

```yaml
---
title: Home
layout: landing
---
```

## How Layouts Work

A layout file contains named slots marked with an `x-part` attribute. The publish pipeline collects all content parts for the page and serialises them as sibling `<main>`, `<nav>`, `<footer>`, and `<aside>` elements in the static HTML — visible to search engines and AI crawlers. At runtime, the Blazor app splices each part's content into its matching layout slot before rendering.

If a layout slot has no matching part, a placeholder comment is inserted. If a page defines a part that has no slot in the layout, the part is still present in the static HTML for crawlers but is not placed in the visual output.

## Content Parts

Content parts are the named HTML fragments that fill a layout's slots. They come from three sources.

### Shared Part Files (`_name.md`)

A Markdown file whose name starts with `_` is a shared part file. It is never published as a standalone page. The file stem (without the `_` prefix) becomes the part name.

Shared parts are resolved by scope: starting from the page's own directory, the pipeline walks upward to `/content/` and uses the deepest matching file. This lets you define a global footer at `content/_footer.md` and override it for a specific section with `content/products/_footer.md`.

```
content/
  _footer.md             ← footer for all pages that don't override it
  _nav.md                ← nav for all pages that don't override it
  products/
    _footer.md           ← overrides the global footer for pages under products/
    widget.md
```

The element used to wrap the part in the static HTML follows this naming convention:

| Part name | HTML element |
|---|---|
| `header` | `<header>` |
| `nav` | `<nav>` |
| `main` | `<main>` |
| `footer` | `<footer>` |
| anything else | `<aside>` |

To override the element, add `element_name: div` (or any tag you prefer) to the part file's frontmatter.

### The `[Part]` Shortcode

Use the `[Part]` shortcode to define a named part inline in a page body. The block is completely extracted from the primary content flow — nothing remains in its place in the page body.

```
[Part Name="right-panel"]
## Related Links

- [Getting Started](./home.md)
- [Content Authoring](./content-authoring.md)
[/Part]
```

`[Part]` only works at the root level of a page body — it cannot be nested inside another shortcode.

For full parameter reference, see the [Part shortcode](shortcodes/part.md) page.

### Auto-Generated Nav

If no `_nav.md` file resolves for a page at any scope level, the publish pipeline auto-generates a Bootstrap navbar as the `nav` part. The generated navbar includes the site brand link and links to all published top-level pages, with subdirectory dropdowns where applicable.

To customise the navigation for your whole site, create `content/_nav.md` and write any HTML or Markdown you need. To override it for a specific section, place `_nav.md` in that subdirectory.
