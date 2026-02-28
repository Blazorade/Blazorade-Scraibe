# Publishing Instructions

These instructions apply when you are asked to **publish** content for this website. Publishing means converting markdown documents in the `/content` folder into static HTML pages in the `{WebAppPath}/wwwroot` folder, and generating a `sitemap.xml` that lists all published pages.

**Notation used in these instructions:**
- `{WebAppPath}` — the value of `WebAppPath` from `blazorade.config.md` (e.g. `src/MyCompany.Web`).
- `{ComponentLibraryPath}` — the value of `ComponentLibraryPath` from `blazorade.config.md` (e.g. `src/MyCompany.Components`).
- `{ComponentLibraryName}` — the last path segment of `ComponentLibraryPath` (e.g. `MyCompany.Components`). Used as the assembly name and root namespace.
- `{HostName}` — the value of `HostName` from `blazorade.config.md` (e.g. `www.mysite.com`).
- `{DisplayName}` — the value of `DisplayName` from `blazorade.config.md` (e.g. `My Awesome Site`).

## Before you begin

**Read `blazorade.config.md` from the repository root before starting any publish run.** The following values from that file are required during publishing:

- `DisplayName` — used as the site brand name in the navbar.
- `HostName` — used to construct canonical URLs and Open Graph URLs in the form `https://{HostName}/{slug}.html`.
- `WebAppPath` — used to resolve the output path for generated files (e.g. `{WebAppPath}/wwwroot/`).
- `ComponentLibraryPath` — used to resolve component namespaces for shortcode processing.

Do not hardcode any of these values. Always read them from `blazorade.config.md`.

## Overview

1. Walk all `.md` files recursively under `/content`.
2. **Always re-read every `.md` file from disk before processing it.** Never assume a file is unchanged based on prior context or memory of a previous publish run. The content must be read fresh every time.
3. For each file, parse the YAML frontmatter and the markdown body.
4. **Process shortcodes** in the Markdown body: scan line by line, resolve known components from `{ComponentLibraryName}.ShortCodes`, and replace shortcode lines with `<x-shortcode>` sentinel elements. This step must run before Markdown-to-HTML conversion. See the **Shortcode processing** section below for full rules.
5. Convert the processed Markdown body to well-structured HTML.
6. Wrap it in the standard HTML page template (see below).
7. Write the output to `{WebAppPath}/wwwroot/{relative-path-without-extension}.html`, preserving subdirectory structure. **Always write the complete file content in a single full overwrite — never patch or partially update an existing file.** This applies to every generated file: `.html` pages, `sitemap.xml`, and `NavMenu.razor`. Partial replacement is fragile and risks corrupting the output.
8. **Delete stale HTML files** — remove any `.html` files under `{WebAppPath}/wwwroot/` that do not correspond to a page in the current publish set. See the **Stale file cleanup** section below.
9. After all pages are processed, generate `{WebAppPath}/wwwroot/sitemap.xml`.

## Excluded content

Before walking the `/content` tree, read the `## Excluded Content` section of `blazorade.config.md`. Any bullet-list entries in that section are paths relative to `/content` that must be skipped entirely — no files under those paths are processed and no output is written for them. Apply exclusions silently; do not emit warnings for excluded files.

Example: if `scraibe-docs` is listed, skip all files under `/content/scraibe-docs/` without processing or reporting them.

## Stale file cleanup

After writing all output files for the current publish run, delete any `.html` files under `{WebAppPath}/wwwroot/` that are **not** in the set of pages just published. This covers pages that were deleted from `/content`, pages whose source folder was added to the exclusion list, and any other previously published file that no longer has a corresponding source document.

**Rules:**
- Never delete `index.html` — it is the Blazor application shell and is not managed by the publish pipeline.
- After deleting `.html` files, delete any subdirectories under `{WebAppPath}/wwwroot/` that are now empty (recursively, bottom-up). Never delete the `wwwroot/` root itself.
- `sitemap.xml`, `staticwebapp.config.json`, and everything under `css/`, `js/`, `lib/`, and `_framework/` are not managed by publishing — never touch them during cleanup.
- Report the list of deleted files in the post-publish summary.

## Blocked files

**Never process `/content/index.md`.**  
The file `{WebAppPath}/wwwroot/index.html` is the Blazor application shell fallback and must never be overwritten. If `index.md` exists anywhere under `/content/`, it is also blocked — generating `index.html` anywhere in `wwwroot/` would shadow the application shell. Reject it with a warning.

**`home` is a reserved slug at every level.**  
A subdirectory named `home` is blocked for the same reason as `index`: `home.md` is always the designated landing page for its containing directory, so a folder named `home` would create an unresolvable ambiguity. Reject any subdirectory named `home` (case-insensitive) with a clear error and abort the publish run.

**A flat `.md` file and a same-named subdirectory cannot coexist at the same level.**  
If `/content/products.md` and `/content/products/` both exist, the publish run must **fail** with a clear error identifying the conflict. This is fatal — do not skip and continue. The author must resolve the conflict by either removing the flat file (if the section has grown into a folder) or removing the subdirectory. This rule enables a clean content evolution model: a path in the URL space is either a leaf page or a folder entry point, never both simultaneously.

Emit a clear warning message for any non-fatal blocked file and skip it; abort the entire publishing run for fatal conflicts.

## Frontmatter

Each markdown document should have a YAML frontmatter block at the top. All fields are optional unless noted.

```yaml
---
title: Page Title                  # Required. Used in <title>, <h1 class="page-title">, og:title
description: Short description     # Used in <meta name="description"> and og:description
slug: about                        # Overrides the filename-derived URL slug if present
keywords: keyword1, keyword2       # Injected into <meta name="keywords">
author: Jane Smith                 # Injected into <meta name="author">
date: 2026-02-20                   # Optional. When present, used verbatim on every publish run.
                                   # When absent, the file's last-modified timestamp on disk is used instead.
                                   # The pipeline never writes back to frontmatter. Injected as <meta name="date">.
ai_instructions: |                 # Free-form instructions for this page's HTML generation.
  Include a <nav aria-label="On this page"> TOC if there are more than 3 headings.
  Highlight the first paragraph with a <p class="lead"> class.
---
```

If `title` is missing, derive it from the first `# Heading` in the markdown body. If no heading exists either, derive it from the filename.

## Output path and slug derivation

- The output path mirrors the `/content` directory structure relative to its root.
- `/content/about.md` → `{WebAppPath}/wwwroot/about.html`
- `/content/contacts/team.md` → `{WebAppPath}/wwwroot/contacts/team.html`
- If the frontmatter specifies a `slug` field, use that as the filename instead of the original filename, but preserve the directory path.
- The canonical URL for a page is `https://{HostName}/{relative-path-without-extension}.html`.

### Folder home pages

`home.md` (case-insensitive) is the designated landing page for its containing directory. It follows these special rules:

- `/content/home.md` is the root home page. It generates `/wwwroot/home.html`. The clean URL is `/`.
- `/content/{path}/home.md` (at any nesting depth) is the landing page for its containing directory. It generates `/wwwroot/{path}/home.html`. The clean URL is `/{path}` (e.g. `content/contacts/finland/home.md` → URL `/contacts/finland`).
- Folder `home.md` files follow all the same rules as other pages (shortcodes, HTML template, metadata, sitemap entry) except for the routing convention above.

## HTML page template

Each generated page is built from the single page shell template at `{WebAppPath}/page-template.html`. **Always read this file** — do not hardcode the template structure in the publish pipeline.

The template uses two kinds of tokens:

- **Per-page tokens** (substituted fresh for every page): `{title}`, `{description}`, `{body_html}`, `{slug}`, `{HostName}` (from `blazorade.config.md`). Optional tags — `{keywords}`, `{author}` — must be **omitted entirely** (whole line removed) when the corresponding frontmatter field is absent. The `{date}` token is **never omitted**: if `date` is set in frontmatter use that value verbatim; if absent, use the source file's last-modified timestamp formatted as `YYYY-MM-DD`; if the filesystem cannot provide a timestamp, fall back to today's date. This derived date must be used consistently in both the `<meta name="date">` tag and the sitemap `<lastmod>` entry for the same page.

The `{body_html}` placeholder is replaced with the fully converted and shortcode-processed HTML content of the page.

## HTML quality requirements for the `<main>` body

The HTML inside `<main>` must be semantic, accessible, and optimised for comprehension by both search engines and AI crawlers:

- Use correct heading hierarchy (`<h1>` for the page title, `<h2>` for sections, etc.).
- Include only one `<h1>` per page.
- Wrap the entire page body in a single `<article>` element. Headings and paragraphs flow directly inside `<article>` without intermediate `<section>` wrappers.
- Use `<nav aria-label="...">` for any navigation lists within the content.
- Prefer `<ul>` / `<ol>` for lists, `<figure>` + `<figcaption>` for images.
- Do **not** include any `<style>` or `<script>` tags inside `<main>`.
- Do **not** include any Blazor-specific attributes or class names — the HTML must be standalone.
- **External links:** Any `<a>` element whose `href` starts with `http://` or `https://` is external and must have `target="_blank" rel="noopener noreferrer"` added. This applies uniformly to links generated from Markdown `[text](https://example.com)` syntax, bare URL autolinking, and any other source. Since all intra-site links are relative paths, an absolute URL unambiguously identifies an external resource.
- **Bare URL autolinking:** Any bare absolute URL (`http://` or `https://`) that appears as plain text in non-code Markdown content must be converted to an `<a href="...">...</a>` element using the URL itself as both the `href` and the link text. Apply external-link treatment as defined above.
- **Internal link rewriting:** After Markdown-to-HTML conversion, scan every `<a href="...">` in the generated body HTML. Any `href` that is a relative path ending in `.md` must be rewritten to end in `.html` instead, preserving any `#fragment` suffix (e.g. `./about.md` → `./about.html`, `./about.md#section` → `./about.html#section`). Absolute URLs and non-`.md` relative links are left unchanged. Do not condense `home.html` paths — `../products/home.html` is the correct canonical URL for that page and must be preserved as-is.
- Any page-level `ai_instructions` from frontmatter take precedence over these defaults.

### Tag-balancing rule for shortcode sentinels

`ContentSegmentParser` in the web app splits the final HTML on `<x-shortcode>` tag boundaries. Each resulting HTML fragment is injected into the live DOM as a raw `MarkupString`. If a fragment contains an **unclosed** block-level HTML tag (e.g. an opening `<div>` without its matching `</div>`), the browser will auto-close it before the next fragment is injected, corrupting the DOM structure.

**Rule: every HTML fragment that precedes or follows a `<x-shortcode>` sentinel must be fully tag-balanced.**

In practice this means: if any block-level wrapper (e.g. one introduced by `ai_instructions`) would straddle a shortcode boundary, **close the wrapper before the `<x-shortcode>` sentinel and, if the block continues after the shortcode, reopen it immediately after the closing `</x-shortcode>` tag**. If the shortcode is the last item in the block, simply close the wrapper before it and do not reopen it.

Note: the outer `<article>` element that wraps the entire page body is handled by `ContentSegmentParser` as an `ElementNode` — its opening and closing tags are emitted via Blazor's `OpenElement`/`CloseElement` calls rather than as raw HTML strings, so the article wrapper itself does not trigger tag-balancing issues.

Example — a custom wrapper added via `ai_instructions` that ends with a shortcode: the publisher must emit:

```html
<div class="callout">
  <p>Intro text.</p>
</div>
<x-shortcode name="Alert" data-params='{}'>
  ...
</x-shortcode>
```

**Not:**

```html
<div class="callout">
  <p>Intro text.</p>
  <x-shortcode name="Alert" data-params='{}'>
    ...
  </x-shortcode>
</div>
```

## Shortcode processing

Before converting Markdown to HTML, the publisher must scan the Markdown body line by line and process any shortcodes. Shortcodes are a mechanism for embedding Blazor components from `{ComponentLibraryName}.ShortCodes` into published pages.

### Shortcode syntax

**Self-closing** — no static fallback content:
```
[ComponentName Param1="value" Param2=true Param3=500 /]
```

**Wrapping (inline)** — opening tag, inner text, and closing tag all on one line. Use when the inner content is short. Only plain text and inline Markdown are permitted as inner content in this form:
```
[ComponentName Param1="value"]Inline inner content[/ComponentName]
```

**Wrapping (multi-line)** — inner content (Markdown text and/or nested shortcodes) becomes the component's child content:
```
[ComponentName Param1="value"]
## Static heading
Some static descriptive text visible to crawlers.
[/ComponentName]
```

**Nested** — block shortcodes may nest to arbitrary depth. Any line that is not a shortcode line is treated as plain Markdown regardless of nesting depth, so Markdown text may appear freely alongside nested shortcodes at the same level:
```
[Carousel]
Below are some slides.
[Slide Title="First Slide"]
## Slide 1
This is the contents of slide 1.
[/Slide]

> This is a standard markdown blockquote that sits in between child shortcodes.

[Slide Title="Second Slide"]
## Slide 2
This is the content of the second slide.
[/Slide]
[/Carousel]
```

### Syntax rules

- The self-closing shortcode `[Name Params /]` must be alone on its line.
- **Multi-line wrapping:** The opening tag `[Name Params]` and the closing tag `[/Name]` must each be alone on their own lines, with inner content on the lines in between.
- **Inline wrapping:** If the opening tag, inner text, and closing tag all appear on a single line — `[Name Params]inner text[/Name]` — this is treated as an inline wrapping shortcode. Only plain text and inline Markdown (no nested shortcodes) are permitted in inline form.
- The opening tag may contain two kinds of tokens, both whitespace-separated. Closing tags carry no tokens.
- **Named parameters** — `Key=value` or `Key="value"` pairs. Names are matched to `[Parameter]` properties on the component using `OrdinalIgnoreCase`; the canonical property name (as declared on the component) is always used as the key in `data-params`. Duplicate names after case normalisation are a fatal error — report the file, line number, and duplicated name.
- **Hint tokens** — any token that is not a `Key=value` pair. Both bare unquoted words (`round`) and quoted strings (`"round pill"`) are hint tokens. All hint tokens are collected in order and joined with a single space to form the **hint string**.
- String parameter values are quoted (`Param="value"`); bool and numeric values are unquoted (`Flag=true Count=5`).
- A line that does not match any shortcode pattern is never treated as a shortcode — it passes through as plain text.

### Code spans and code blocks — skip shortcode detection

- Do not scan for shortcodes inside Markdown code contexts. Anything that the Markdown parser treats as code must remain literal and bypass shortcode detection entirely.
- **Inline code spans** (single backticks) are emitted verbatim; `[Badge ui="pill"]` inside backticks stays as text.
- **Fenced code blocks** (with or without a language hint) and **indented code blocks** are passed through unchanged; shortcode-like text is not tokenised there.
- Only non-code regions of the Markdown body are eligible for shortcode parsing.

### Component resolution

- **Before reflecting over the component library, build the project** by running `dotnet build` on `{ComponentLibraryPath}/{ComponentLibraryName}.csproj`. This ensures any components added or modified since the last build are compiled into the assembly before the registry is populated. If the build fails, abort the publish run and report the build errors.
- Build a registry by reflecting over the compiled `{ComponentLibraryName}` assembly and collecting all types in the `{ComponentLibraryName}.ShortCodes` namespace.
- The component name in the shortcode must match a type name in that namespace exactly (pascal case).
- If the component name is **not found** in the registry, the shortcode line is treated as plain text and passes through unchanged.

### Hint token processing (`[AgentInstructions]`)

After the component type is resolved and tokens have been classified, process hint tokens as follows:

1. Reflect over the resolved component class and check for a class-level `[AgentInstructions]` attribute (in the `{ComponentLibraryName}.Annotations` namespace).
2. If the attribute is present **and** at least one hint token was found:
   a. Use the `AgentInstructions` string as reasoning context to translate the joined hint string into one or more Bootstrap CSS class names.
   b. Add `CssClasses` to the named parameter set with the translated class string as its value. If `CssClasses` was also written explicitly as a named parameter by the author, the hint-derived value takes precedence and the explicit value is discarded (emit a warning).
3. If `[AgentInstructions]` is absent but hint tokens were found, emit a warning and discard the hint tokens — this is a content authoring mistake, not a fatal error.
4. If no hint tokens were found, skip this step entirely.

Hint token processing always runs before named parameters are serialised to `data-params`.

### Parser state machine

The parser processes the Markdown body line by line using a **stack** that tracks open wrapping shortcodes. The stack starts empty (root level).

Detection must be attempted **in the order listed below** — earlier patterns take priority.

**On every line:**

| Line matches | Stack empty (root) | Stack non-empty (inside block) |
|---|---|---|
| Regular text line | Emit to root Markdown accumulator | Append to top-of-stack Markdown accumulator |
| `[Name Params /]` — self-closing, **known** component | Replace line with sentinel; stay at root | Replace line with sentinel; append sentinel HTML to top-of-stack accumulator |
| `[Name Params /]` — self-closing, **unknown** component | Pass through as plain text | Append verbatim to top-of-stack accumulator |
| `[Name Params]text[/Name]` — inline wrapping, **known** component | Convert inner text as inline Markdown; emit sentinel with converted text as body; stay at root | Convert inner text as inline Markdown; emit sentinel; append to top-of-stack accumulator |
| `[Name Params]text[/Name]` — inline wrapping, **unknown** component | Pass through as plain text | Append verbatim to top-of-stack accumulator |
| `[Name Params]` — opening only, **known** component | Push new frame onto stack (name, params, empty accumulator). Do **not** emit the opening tag line. | Push new frame onto stack. Do **not** emit the opening tag line. |
| `[Name Params]` — opening only, **unknown** component | Pass through as plain text | Append verbatim to top-of-stack accumulator |
| `[/Name]` — closing, matches top of stack | Pop frame; process its accumulated content (see below); emit wrapping sentinel into root accumulator | Pop frame; process its accumulated content; append resulting sentinel HTML to new top-of-stack accumulator |
| `[/Name]` — closing, **does not** match top of stack | **Error**: unexpected `[/Name]` with no matching open. Publish fails. | **Error**: expected `[/FrameName]`, found `[/Name]`. Publish fails. |
| End of file | Normal completion | **Error**: unclosed `[FrameName]`. Publish fails. |

**Processing a popped frame:**

When a wrapping frame is popped its accumulated content may contain a mix of plain Markdown lines and already-emitted `<x-shortcode>` sentinel HTML strings (from nested shortcodes that were processed and injected back). Run the entire accumulated content through the Markdown-to-HTML converter. The Markdown processor treats raw HTML blocks as pass-through, so the nested sentinel elements are preserved verbatim. The result becomes the inner body of the wrapping sentinel element for the popped frame.

For example, the mixed-content `[Carousel]` frame above accumulates:
```
Below are some slides.
<x-shortcode name="Slide" data-params='{}'></x-shortcode>

> This is a standard markdown blockquote that sits in between child shortcodes.

<x-shortcode name="Slide" data-params='{}'></x-shortcode>
```
After Markdown conversion this becomes:
```html
<p>Below are some slides.</p>
<x-shortcode name="Slide" data-params='{}'></x-shortcode>
<blockquote><p>This is a standard markdown blockquote that sits in between child shortcodes.</p></blockquote>
<x-shortcode name="Slide" data-params='{}'></x-shortcode>
```
This full HTML block then becomes the inner body of the `<x-shortcode name="Carousel" ...>` sentinel.

**Root completion:**

After all lines are processed, convert the root accumulator (which may also contain a mix of Markdown and injected sentinel HTML) through the Markdown-to-HTML converter. The result is the `{body_html}` value for the page template.

### Sentinel element format

> **SENTINEL CONTRACT** — The exact attribute names, quote style, and element name below form a contract between the publish pipeline and `ContentRenderer` at runtime. If this format changes, both the publisher and `ContentRenderer`/`ContentSegmentParser` in the component library **must** be updated together.

Self-closing shortcode → empty sentinel:
```html
<x-shortcode name="ComponentName" data-params='{"Param1":"value","Param2":true}'></x-shortcode>
```

Wrapping shortcode → sentinel with inner content:
```html
<x-shortcode name="ComponentName" data-params='{...}'>
  <!-- static content for crawlers; replaced by live Blazor component at runtime -->
  <p>Inner Markdown converted to HTML goes here.</p>
</x-shortcode>
```

Nested wrapping shortcodes produce naturally nested sentinel elements:
```html
<x-shortcode name="Carousel" data-params='{}'>
  <x-shortcode name="Slide" data-params='{"Title":"First Slide"}'>
    <h2>Slide 1</h2>
    <p>This is the contents of slide 1.</p>
  </x-shortcode>
  <x-shortcode name="Slide" data-params='{"Title":"Second Slide"}'>
    <h2>Slide 2</h2>
    <p>This is the content of the second slide.</p>
  </x-shortcode>
</x-shortcode>
```

- `name` attribute uses double quotes. `data-params` attribute uses **single quotes** so that the JSON value can contain unescaped double quotes without HTML encoding.
- `data-params` contains the named parameters serialised as a compact JSON object. Use the **canonical** property name (as declared on the component) for each key — never the raw casing written by the author. Hint tokens are never emitted directly; they are translated to `CssClasses` via `[AgentInstructions]` processing (see above) and appear in `data-params` only as `"CssClasses":"..."`. Use `'{}'` when there are no parameters.
- The inner static content (wrapping form only) is the accumulated inner Markdown lines converted to HTML following the same HTML quality rules as the rest of the body.
- `<x-shortcode>` is a custom element — it is transparent to browsers and crawlers, which read the inner content normally. The Blazor `ContentRenderer` component replaces it with the live component at runtime.

### Error handling

All shortcode errors are fatal — the publish run must stop and report a clear error message identifying the file and line number. Do not silently skip or corrupt content.

## Sitemap generation

After all pages are written, generate `{WebAppPath}/wwwroot/sitemap.xml` listing every successfully published page.

Use `/templates/web-app/wwwroot/sitemap.xml` as the structural template. It contains a single `<url>` block with per-page tokens. Repeat that block once for each published page, substituting all tokens, and wrap the result in the `<urlset>` element shown in the template.

- `<loc>` uses the canonical HTTPS URL: `https://{HostName}/{slug}.html` (no `/pages/` prefix).
- `<lastmod>` is determined as follows:
  - If `date` is set in frontmatter, use that value verbatim.
  - If `date` is absent, read the source `.md` file's last-modified timestamp from disk (e.g. `File.GetLastWriteTime` or equivalent) and format it as `YYYY-MM-DD`.
  - If neither is available (e.g. the filesystem cannot return a timestamp), fall back to today's date in `YYYY-MM-DD` format.
  - The pipeline never stores a derived date back into the frontmatter.
- `<changefreq>` defaults to `monthly` unless overridden in frontmatter with a `changefreq` field.
- `<priority>` defaults to `0.8` unless overridden in frontmatter with a `priority` field (0.0–1.0).
- The `sitemap.xml` itself is **not** listed in the sitemap.
- `index.html` is **never** listed in the sitemap.

## Content to output mapping examples

The following examples illustrate how source documents map to generated output files:

| Source document | Generated page | Address bar URL |
|---|---|---|
| `content/about.md` | `wwwroot/about.html` | `/about` |
| `content/blog/my-post.md` | `wwwroot/blog/my-post.html` | `/blog/my-post` |
| `content/home.md` | `wwwroot/home.html` | `/` |
| `content/products/home.md` | `wwwroot/products/home.html` | `/products` |
| `content/products/widget.md` | `wwwroot/products/widget.html` | `/products/widget` |

## staticwebapp.config.json

`{WebAppPath}/wwwroot/staticwebapp.config.json` is **not managed by the publish pipeline**. It contains only the `navigationFallback` block (falling back to `/index.html`) and the asset `exclude` list. Do not modify this file during publishing.

## NavMenu.razor generation

After `sitemap.xml` is written, also regenerate `{WebAppPath}/Components/NavMenu.razor` so the top navbar stays in sync with the published pages. **Always overwrite the existing file — regenerate it unconditionally on every publish run, regardless of whether any page titles or structure changed.**

### Navigation structure rules

Walk the same page list used for the sitemap and apply these rules:

- **Flat pages** (no subdirectory) become a plain `<li class="nav-item">` with a `<NavLink>`. Use the frontmatter `title` property as the link text. **Exclude the Home page** (the page whose slug resolves to `""` / `/`) from the nav items — the `navbar-brand` already links to `/` so including it would duplicate the link.
- **Subdirectories** become a dropdown `<li class="nav-item dropdown">`. The dropdown label is taken from the frontmatter `title` property of the `Home.md` (case-insensitive) file inside that subdirectory. Each page inside the subdirectory (including `Home`) becomes a `<li>` with a `<NavLink class="dropdown-item">` inside the dropdown menu. The dropdown button itself does **not** navigate; it only opens/closes the menu via `@onclick`.
- Preserve the order pages appear in the sitemap.

### Home slug convention

The Home page at `/home.html` must render its link with `href="/"` (not `/home`). All other pages use `href="/{slug}"` where the slug is the path-without-extension relative to `wwwroot/` (e.g. `/about.html` → `/about`; `/products/widget.html` → `/products/widget`).

### Generated file structure

Use `/templates/web-app/Components/NavMenu.razor` as the scaffold. It contains the full component structure with comment markers showing where flat-page `<li>` items and subdirectory dropdown `<li>` items are inserted. Generate the nav items dynamically from the published page list, then write the result to `{WebAppPath}/Components/NavMenu.razor`.

Omit the dropdown helper methods (`ToggleDropdown`, `CloseDropdown`, `IsDropdownOpen`) and the `_openDropdownKey` field entirely if no subdirectory pages are present.

## Summary of what to report after publishing

When the publishing run is complete, report:

- Number of pages successfully published.
- List of any skipped/blocked files and the reason.
- List of any stale `.html` files deleted during cleanup (or confirmation that none needed to be deleted).
- Confirmation that `sitemap.xml` was updated.
- Confirmation that `Components/NavMenu.razor` was regenerated.
- The full output path of every file written.
