---
applyTo: "content/**"
---

# Content Authoring Instructions

The `/content` folder is the source of truth for all site content. Each Markdown file in this folder represents one page on the published website. When the site is published, these files are converted to static HTML bootstrappers in `{WebAppPath}/wwwroot/`, which the Blazor app fetches and renders at runtime. Editing a file here and running publish is the only way to update the content of a page — the generated HTML files must never be edited directly.

These instructions cover the frontmatter schema and the shortcode syntax available to content authors.

## Frontmatter

Every content file should begin with a YAML frontmatter block. All fields are optional unless noted.

```yaml
---
title: Page Title                  # Required. Used in <title>, <h1 class="page-title">, og:title.
description: Short description     # Used in <meta name="description"> and og:description.
slug: about                        # Overrides the filename-derived URL slug if present.
keywords: keyword1, keyword2       # Injected into <meta name="keywords">.
author: Jane Smith                 # Injected into <meta name="author">.
date: 2026-02-20                   # Optional. Publication date in YYYY-MM-DD format.
                                   # When set, this value is used verbatim on every publish run until
                                   # you explicitly change or remove it. The pipeline never overwrites it.
                                   # When omitted, the publish pipeline uses the file's last-modified
                                   # timestamp on disk. That derived date is never written back here.
layout: default                    # Optional. The name of the page layout to use. Resolved
                                   # case-insensitively; "default" and "Default" both resolve to
                                   # Default.html in the component library. Absent field uses "Default".
changefreq: monthly                # Sitemap change frequency. Defaults to monthly.
priority: 0.8                      # Sitemap priority (0.0–1.0). Defaults to 0.8.
---
```

If `title` is missing, it is derived from the first `# Heading` in the body. If no heading exists either, it is derived from the filename.

## Reserved filenames and slugs

- **`index.md`** is blocked at every level — it would overwrite the Blazor app shell (`index.html`).
- **`home.md`** is the designated landing page for its containing directory:
  - `content/home.md` — root home page. Routes to `/`.
  - `content/{a}/home.md` — folder landing page. Routes to `/{a}`, **not** `/{a}/home`.
  - This applies at any nesting depth: `content/{a}/{b}/home.md` routes to `/{a}/{b}`, `content/{a}/{b}/{c}/home.md` routes to `/{a}/{b}/{c}`, and so on.
- A flat `.md` file and a same-named subdirectory cannot coexist at the same level (e.g. `products.md` and `products/` together is an error).

## Shortcode syntax

Shortcodes embed live Blazor components into a published page. They are resolved against components in the `{ComponentLibraryName}.ShortCodes` namespace.

### Self-closing shortcode

Use when the component needs no inner content. The trailing `/]` is **required** when there is no separate closing tag — a shortcode with neither `/]` nor a matching `[/ComponentName]` is a parse error:

```
[ComponentName Param1="value" Param2=true Param3=500 /]
```

### Wrapping shortcode

Use when the component wraps inner content. **The component must have child content enabled** (i.e. declare a `[Parameter] public RenderFragment? ChildContent { get; set; }` property) — using a wrapping shortcode with a component that does not support child content is an error.

**Inline form** — opening tag, inner text, and closing tag all on one line. Use this when the inner content is short. Only plain text and inline Markdown (bold, italic, links) are permitted; nested shortcodes are not allowed in inline form:

```
[ComponentName Param1="value"]This is the inner text[/ComponentName]
```

**Multi-line form** — opening tag on its own line, inner content on the lines in between, closing tag on its own line. Inner content may be Markdown, other shortcodes, or any combination of both — all mixed freely at any nesting depth:

```
[ComponentName Param1="value"]
## Static heading visible to crawlers
Some descriptive text.
[/ComponentName]
```

Nested example — shortcodes may nest to arbitrary depth, and plain Markdown may appear freely alongside nested shortcodes at any level:

```
[Carousel]
These are the slides.
[Slide Title="First Slide"]
## Slide 1
Content of slide 1.
[/Slide]

> A blockquote between shortcodes is fine.

[Slide Title="Second Slide"]
## Slide 2
Content of slide 2.
[/Slide]
[/Carousel]
```

### Parameter syntax rules

- The self-closing shortcode `[Name /]` must be alone on its line.
- **Multi-line wrapping:** The opening tag and the closing tag `[/ComponentName]` must each be alone on their own lines.
- **Inline wrapping:** The opening tag, inner text, and closing tag may all appear on a single line: `[Name]inner text[/Name]`. Only plain text and inline Markdown are permitted as inner content — nested shortcodes are not allowed inline.
- Parameters are whitespace-separated key=value pairs on the opening tag only. Closing tags have no parameters.
- Parameter names use PascalCase, matching the component's `[Parameter]` property names.
- String values are quoted: `Param="value"`.
- Boolean and numeric values are unquoted: `Flag=true`, `Count=5`.
- A `[...]` expression that doesn't match any shortcode pattern is never treated as a shortcode — it passes through as plain text.
- Shortcode detection is skipped inside Markdown code contexts: inline code spans, fenced code blocks (with or without a language hint), and indented code blocks all remain literal.

## Page layouts and content parts

Each published page is rendered inside a **layout** — a static HTML file in the component library (`{ComponentLibraryPath}/wwwroot/Layouts/`) that defines named `x-part` slots. The layout controls the page chrome: navbar, footer, columns, and any other structural elements.

### Choosing a layout

Set the `layout` frontmatter field to the layout name (case-insensitive). Omitting the field uses `Default`.

```yaml
layout: default   # uses Default.html
layout: landing   # uses Landing.html
```

Available layouts are the `.html` files in `{ComponentLibraryPath}/wwwroot/Layouts/`. The two built-in layouts are:

- **Default** — Bootstrap navbar (`x-part="nav"`), content area (`x-part="main"`), and footer (`x-part="footer"`).
- **Landing** — Full-width content only (`x-part="main"`). No nav, no footer.

### `_name.md` shared part files

A Markdown file whose name starts with `_` is a *shared part file* and is never published as a standalone page. The stem (without `_`) is the part name that it provides. The pipeline resolves them by scope: starting from the page's directory, walking up to `/content/`, the deepest matching file wins.

Examples:
- `content/_footer.md` — provides the `footer` part for every page that doesn't override it.
- `content/products/_footer.md` — provides the `footer` part exclusively for pages under `content/products/`.

The element used to wrap the part in the generated HTML follows the **element name convention** (e.g. `_footer.md` → `<footer>`, `_nav.md` → `<nav>`, anything else → `<aside>`). Add `element_name: div` to the file's frontmatter to override.

### `[Part]` shortcode

The `[Part]` shortcode adds a named part inline in a content page body. The part is extracted entirely from the primary content flow — nothing appears in its place in the page body. It is only valid at the top level of a page (not nested inside other shortcodes).

```
[Part Name="right-panel"]
## Related Links
- [Item one](/item-one)
[/Part]
```

Parameters:
- `Name` (required) — the part name; must match an `x-part` slot in the layout, or the part is emitted in the static HTML for crawlers but not rendered visually.
- `ElementName` (optional) — overrides the element name convention for the generated HTML wrapper.

### Auto-generated nav

If no `_nav.md` resolves for a page, the publish pipeline auto-generates a Bootstrap navbar as the `nav` part, using the full published page list (same structure as the previous `NavMenu.razor` generation: brand link, flat-page links, subdirectory dropdowns).

## Content quality guidelines

- Use correct heading hierarchy: `#` for the page title (one per page), `##` for sections, `###` for subsections.
- Do not skip heading levels.
- Write clear, descriptive section headings — they are used as `aria-label` values on `<section>` elements.
- Prefer short paragraphs and lists over long prose blocks for scannability by both humans and AI crawlers.
