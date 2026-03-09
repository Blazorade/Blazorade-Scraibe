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

## How to publish

**Publishing is fully automated. Run the PowerShell script and wait for the summary output:**

```powershell
.\tools\Invoke-Publish.ps1
```

The script reads `blazorade.config.md`, builds the component library, then hands everything off to `tools/Scraibe.Publisher` — a .NET console tool that owns the complete pipeline: content walking, shortcode processing, Markdown→HTML conversion, template application, nav generation, sitemap generation, static asset sync, staticwebapp route/exclude updates, and stale-HTML cleanup. No page HTML is generated in the chat context.

If the script reports errors, read the output, fix the problem in the relevant source file, and re-run. Do **not** attempt to replicate the pipeline manually.

### Optional parameters

```powershell
.\tools\Invoke-Publish.ps1 -Configuration Release   # build the component library in Release
```

### Partial publish

Use partial publish when a content author asks to publish one or a few specific pages rather than the whole site. Only the specified pages' HTML files are regenerated and their sitemap entries updated — all other files are untouched. No stale cleanup is performed in a partial run.

**Trigger phrases** — use partial publish when the request matches one of:
- "publish the current page" / "publish this page"
- "publish attached documents" / "publish these files"
- "publish [explicit path or filename]"
- "publish the pages I have open" (use attached files or the active editor file; if neither is available, ask the user to specify)

A plain "publish" or "run publish" with no page qualifier always triggers a **full publish** — no change to existing behaviour.

**Resolving the page list** — before invoking the script, resolve which pages to publish from one of three forms:

1. **"Publish the current page"** — use the active file path from the editor context.
2. **Attached files** — one or more `.md` files attached to the chat; use their file paths.
3. **Explicit path in chat** — the author mentions a path directly (e.g. `content/scraibe-docs/mermaid.md`); parse and use it.

**Validation rule (mandatory for all three forms):** every resolved path must be inside the `/content` folder of the repository. If any path falls outside `/content`, reject the entire request with a clear error and do not publish anything. Perform this check before invoking the script.

Convert each absolute path to a content-relative `.md` path for the `-Pages` argument (e.g. `C:\...\content\scraibe-docs\mermaid.md` → `scraibe-docs/mermaid.md`).

**Invocation:**

```powershell
.\tools\Invoke-Publish.ps1 -Pages home.md,scraibe-docs/mermaid.md
```

Paths in `-Pages` are relative to `/content` and must include the `.md` extension. The publisher performs a pre-flight check: if any of the specified pages has never been published (its `.html` file does not exist in `wwwroot/`), the run aborts with a clear error before any work is done. Instruct the author to run a full publish first.

---

## Pipeline reference

The sections below document the rules implemented by `tools/Scraibe.Publisher`. Read them when updating or debugging the publish tool — they are **not** instructions for the agent to follow manually.

### Overview

1. Walk all `.md` files recursively under `/content`.
2. For each file, parse the YAML frontmatter and the Markdown body.
3. **Process shortcodes** in the Markdown body: scan line by line, resolve known components from `{ComponentLibraryName}.ShortCodes`, and replace shortcode lines with `<x-shortcode>` sentinel elements. This step must run before Markdown-to-HTML conversion. See the **Shortcode processing** section below for full rules.
4. Convert the processed Markdown body to well-structured HTML.
5. Wrap it in the standard HTML page template (see below).
6. Write the output to `{WebAppPath}/wwwroot/{relative-path-without-extension}.html`, preserving subdirectory structure. Always write the complete file content in a single full overwrite — never patch or partially update an existing file.
7. **Delete stale HTML files** — remove any `.html` files under `{WebAppPath}/wwwroot/` that do not correspond to a page in the current publish set.
8. After all pages are processed, generate `{WebAppPath}/wwwroot/sitemap.xml`.
9. Copy eligible static assets from `/content` to `{WebAppPath}/wwwroot/` preserving relative paths.
10. Update `{WebAppPath}/wwwroot/staticwebapp.config.json` route rewrites and merge navigationFallback excludes.

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
layout: default                    # Optional. Page layout name. Resolved case-insensitively and normalised to
                                   # PascalCase (e.g. "default" → "Default", "two-column" → "Two-Column").
                                   # Absent field falls back to "Default".
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

- **Per-page tokens** (substituted fresh for every page): `{title}`, `{description}`, `{slug}`, `{HostName}` (from `blazorade.config.md`), `{layout}`, `{layout_html}`, `{cleanSlug}`, `{blazor_script}`. Optional tags — `{keywords}`, `{author}` — must be **omitted entirely** (whole line removed) when the corresponding frontmatter field is absent. The `{date}` token is **never omitted**: if `date` is set in frontmatter use that value verbatim; if absent, use the source file's last-modified timestamp formatted as `YYYY-MM-DD`; if the filesystem cannot provide a timestamp, fall back to today's date. This derived date must be used consistently in both the `<meta name="date">` tag and the sitemap `<lastmod>` entry for the same page. The `{layout}` token is **never omitted**: use the PascalCase-normalised layout name resolved from frontmatter, falling back to `Default` when the field is absent.

The `{layout_html}` placeholder is replaced with the fully composed layout HTML for the page (converted body + resolved parts).

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
- **Internal link rewriting:** After Markdown-to-HTML conversion, scan every `<a href="...">` in the generated body HTML. Any relative `href` ending in `.md` must be rewritten to the clean URL form by stripping `.md` and any trailing `/home`, preserving query/fragment suffixes (e.g. `./about.md` → `/about`, `./products/home.md#specs` → `/products#specs`). Absolute URLs and non-`.md` relative links are left unchanged.

### Tag-balancing rule for shortcode sentinels

`ContentSegmentParser` in the web app splits the final HTML on `<x-shortcode>` tag boundaries. Each resulting HTML fragment is injected into the live DOM as a raw `MarkupString`. If a fragment contains an **unclosed** block-level HTML tag (e.g. an opening `<div>` without its matching `</div>`), the browser will auto-close it before the next fragment is injected, corrupting the DOM structure.

**Rule: every HTML fragment that precedes or follows a `<x-shortcode>` sentinel must be fully tag-balanced.**

In practice this means: if any block-level wrapper would straddle a shortcode boundary, **close the wrapper before the `<x-shortcode>` sentinel and, if the block continues after the shortcode, reopen it immediately after the closing `</x-shortcode>` tag**. If the shortcode is the last item in the block, simply close the wrapper before it and do not reopen it.

Note: the outer `<article>` element that wraps the entire page body is handled by `ContentSegmentParser` as an `ElementNode` — its opening and closing tags are emitted via Blazor's `OpenElement`/`CloseElement` calls rather than as raw HTML strings, so the article wrapper itself does not trigger tag-balancing issues.

Example — a wrapping div that ends with a shortcode: the publisher must emit:

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

- Shortcode detection is **token-based**, not line-based. Tags may appear back-to-back on one line and may be indented.
- Back-to-back nested tags are valid when balanced, for example: `[Part Name="hero"][Carousel][Slide]...[/Slide][/Carousel][/Part]`.
- **Inline wrapping:** `[Name Params]inner text[/Name]` is valid.
- **Multi-line wrapping:** opening and closing tags may appear on separate lines with mixed Markdown and nested shortcodes between them.
- The opening tag may contain two kinds of tokens, both whitespace-separated. Closing tags carry no tokens.
- **Named parameters** — `Key=value` or `Key="value"` pairs. Names are matched to `[Parameter]` properties on the component using `OrdinalIgnoreCase`; the canonical property name (as declared on the component) is always used as the key in `data-params`. Duplicate names after case normalisation are a fatal error — report the file, line number, and duplicated name.
- **CSS class tokens** — any token that is not a `Key=value` pair. Both bare unquoted words (`rounded`) and quoted strings (`"rounded text-danger"`) are CSS class tokens. All CSS class tokens are collected in order and joined with a single space to form the `CssClasses` value.
- String parameter values are quoted (`Param="value"`); bool and numeric values are unquoted (`Flag=true Count=5`).
- Text that is not a valid shortcode tag is emitted as plain Markdown text.

### Wrapping-content normalization

- Before Markdown conversion, wrapping shortcode inner content is normalized with a **dedent-before-Markdown** step.
- Tabs in leading indentation are normalised consistently (tab width 4) for indentation measurement.
- The parser removes the minimum common leading indentation across non-empty lines.
- Relative indentation beyond the shared baseline is preserved.
- Visual indentation alone must not create accidental Markdown block semantics (for example blockquotes or code blocks).
- Intentional Markdown block semantics must be explicit (for example `>` for a blockquote).

### Code spans and code blocks — skip shortcode detection

- Do not scan for shortcodes inside Markdown code contexts. Anything that the Markdown parser treats as code must remain literal and bypass shortcode detection entirely.
- **Inline code spans** (single backticks) are emitted verbatim; `[Badge ui="pill"]` inside backticks stays as text.
- **Fenced code blocks** (with or without a language hint) are passed through unchanged; shortcode-like text is not tokenised there.
- For portable literal shortcode examples, prefer fenced blocks over indented-code syntax.
- Only non-code regions of the Markdown body are eligible for shortcode parsing.

### Mermaid fenced block detection

Fenced code blocks whose language hint is exactly `mermaid` (case-insensitive) are intercepted **before** shortcode parsing and converted to `Mermaid` shortcode sentinels. This happens inside the fenced-block tracking pass.

**Detection rule:** When a fenced-block opening line is matched and the language hint equals `mermaid` (OrdinalIgnoreCase):

1. Do **not** emit the fence-open line to output.
2. Accumulate all subsequent body lines into a separate `mermaidBody` buffer. Do **not** emit them to output.
3. On the closing fence line, take `mermaidBody.ToString().Trim()` as the diagram definition and emit the following sentinel:

```html
<x-shortcode name="Mermaid" data-params='{}'>
graph TD
    A --> B
</x-shortcode>
```

4. Do **not** emit the closing fence line. Reset mermaid state.

The inner content of the sentinel is the raw diagram definition — it is **not** run through Markdig. All other fenced blocks (any other language hint, or no hint) are left completely untouched by this step.

### Component resolution

- **Before reflecting over the component library, build the project** by running `dotnet build` on `{ComponentLibraryPath}/{ComponentLibraryName}.csproj`. This ensures any components added or modified since the last build are compiled into the assembly before the registry is populated. If the build fails, abort the publish run and report the build errors.
- Build a registry by reflecting over the compiled `{ComponentLibraryName}` assembly and collecting all types in the `{ComponentLibraryName}.ShortCodes` namespace.
- The component name in the shortcode must match a type name in that namespace exactly (pascal case).
- If the component name is **not found** in the registry, the shortcode line is treated as plain text and passes through unchanged.

### CSS class token processing

After the component type is resolved and tokens have been classified, process CSS class tokens as follows:

1. Collect all CSS class tokens in order and join them with a single space to form the `CssClasses` value.
2. Set `CssClasses` in the named parameter set to this string. If `CssClasses` was also written explicitly as a named parameter by the author, the token-derived value takes precedence and the explicit value is discarded (emit a warning).
3. If no CSS class tokens were found, skip this step entirely.

CSS class tokens are used verbatim — content authors write the actual CSS class names (e.g. Bootstrap utility classes) directly. No translation or reflection is involved.

CSS class token processing always runs before named parameters are serialised to `data-params`.

### Parser state machine

The parser processes the Markdown body using a stack of open wrapping shortcodes. Input is read line-by-line, but shortcode detection within each line is token-based.

Processing model:

- Scan each eligible line left-to-right.
- Emit plain text segments directly to the current accumulator.
- For a known self-closing tag, emit a sentinel immediately.
- For a known opening tag, push a frame onto the stack.
- For a closing tag, validate it matches the top stack frame, then pop and materialize the wrapping shortcode.
- Unknown opening/self-closing tags pass through as plain text.
- A closing tag with no matching open frame is fatal.
- End-of-file with non-empty stack is fatal (unclosed shortcode).

Processing a popped frame:

- Normalize inner markdown with dedent-before-Markdown.
- Convert the normalized content to HTML.
- For normal wrapping shortcodes, emit a sentinel with static inner HTML.
- For `[Part]`, store `{ name, elementName, innerHtml }` and emit nothing into main content.

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

### `[Part]` shortcode special handling

`Part` is a known wrapping shortcode with special output behaviour, processed within the same stack-based parser but producing no sentinel in the main content accumulator:

- `[Part]` is only valid at **root level** (stack depth 0). A `[Part]` appearing inside another wrapping shortcode is a fatal publish error.
- When the parser pops a `[Part Name="..."]...[/Part]` frame, instead of emitting an `<x-shortcode>` sentinel into the root accumulator, it:
  1. Converts the frame's accumulated inner content to HTML (same as any wrapping shortcode).
  2. Stores it as a named part entry `{ name, elementName, innerHtml }`. `name` is taken from the `Name` parameter (lowercased). `elementName` is taken from the `ElementName` parameter if provided, otherwise derived from `name` per the element name convention in **Content parts** below.
  3. Emits **nothing** into the root content accumulator — the block is fully removed from the primary content flow.
- After all lines are processed, stored `[Part]` entries are merged with parts from `_name.md` files and serialised into `{parts_html}`.

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
- `data-params` contains the named parameters serialised as a compact JSON object. Use the **canonical** property name (as declared on the component) for each key — never the raw casing written by the author. CSS class tokens are never emitted directly into `data-params`; they are joined and stored as the `CssClasses` parameter value (see **CSS class token processing** above). Use `'{}'` when there are no parameters.
- The inner static content (wrapping form only) is the accumulated inner Markdown lines converted to HTML following the same HTML quality rules as the rest of the body.
- `<x-shortcode>` is a custom element — it is transparent to browsers and crawlers, which read the inner content normally. The Blazor `ContentRenderer` component replaces it with the live component at runtime.

### Error handling

All shortcode errors are fatal — the publish run must stop and report a clear error message identifying the file and line number. Do not silently skip or corrupt content.

## Content parts

Content parts are named HTML fragments gathered per page and serialised into the `{parts_html}` token. At runtime, `ContentPage.razor` splices each part's `InnerHtml` into the matching `x-part` slot in the layout.

### Sources

Parts come from three sources, resolved and merged per page:

1. **`_name.md` scoped files** — A Markdown file whose name starts with `_` is a *shared part file*. It is never published as a standalone page and never listed in the sitemap. The stem (without the `_` prefix) is the part name. Scope resolution: starting from the page's own directory, walk upward to `/content/`; the deepest matching file wins. For a page at `content/products/widget.md`, the pipeline checks `content/products/_footer.md` first, then `content/_footer.md`. The element name is derived from the part name per the **Element name convention** below; the file's frontmatter may override it via an `element_name` field.

2. **`[Part]` shortcode** — Described in the **`[Part]` shortcode special handling** subsection above. The `Name` parameter becomes the part name; `ElementName` (if set) overrides the element name convention.

3. **Auto-generated nav** — If no `_nav.md` resolves at any scope level for a given page, the pipeline auto-generates a Bootstrap navbar as the `nav` part. The generated HTML follows the same navigation structure rules previously used for `NavMenu.razor` generation: brand link to `/`, flat top-level pages as `<li class="nav-item">` items (excluding the home page), subdirectory dropdowns as `<li class="nav-item dropdown">` items with a non-navigating toggle button. The `DisplayName` value from `blazorade.config.md` is used as the `navbar-brand` text.

   **Critical**: the generated HTML must be the **inner content** of the `<nav>` slot only — do **not** wrap it in another `<nav>` element. The layout's `<nav x-part="nav">` element is the sole root; the pipeline fills its children. Wrapping in a second `<nav>` produces two nested `<nav>` elements in the final page.

### Element name convention

The HTML element used to wrap a part in the generated HTML follows this rule, applied uniformly for all part sources:

| Part name | Element |
|---|---|
| `header` | `<header>` |
| `nav` | `<nav>` |
| `main` | `<main>` |
| `footer` | `<footer>` |
| anything else | `<aside>` |

### Generated HTML structure

All parts are serialised as sibling elements immediately after `</main>`, each carrying `hidden` and `x-part`:

```html
<nav hidden x-part="nav">
  <!-- Bootstrap navbar inner content only — NO wrapping <nav> element.
       The layout slot <nav x-part="nav"> is the root; only its children go here.
       Correct:   <div class="container"><a class="navbar-brand" ...> ... </div>
       Wrong:     <nav class="navbar ..."><div class="container"> ... </div></nav> -->
</nav>
<footer hidden x-part="footer">...from _footer.md...</footer>
<aside hidden x-part="right-panel">...from [Part Name="right-panel"]...</aside>
```

The `<main>` element itself carries `x-part="main"` and `hidden`. When a page has no parts beyond `main`, `{parts_html}` is replaced with an empty string.

### Part verification (fatal errors)

1. A referenced layout file does not exist. The pipeline looks for `{ComponentLibraryPath}/wwwroot/Layouts/{Name}.html` using the PascalCase-normalised name; the lookup is case-insensitive to tolerate OS differences.
2. A layout file has more than one root element.
3. A layout file has no elements with an `x-part` attribute (at least one slot is required).
4. The same `x-part` name appears more than once in a layout file.
5. The same part name is defined more than once for a given page (across `_name.md` files and `[Part]` blocks combined).

### Not errors

- A page defines a part that has no matching slot in the layout — the part is still emitted in the generated HTML (for crawlers) but is silently ignored at runtime.
- A layout slot has no matching part — replaced at runtime with an HTML comment: `<!-- x-part="name": no content found -->`.

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

`{WebAppPath}/wwwroot/staticwebapp.config.json` is managed by the publish pipeline.

- **Full publish**: rebuild from `templates/web-app/wwwroot/staticwebapp.config.json` as base, regenerate all page rewrite routes, and merge existing plus discovered asset-folder excludes.
- **Partial publish**: update only affected routes and merge newly discovered asset-folder excludes additively; never remove existing excludes.

This keeps clean-URL route rewrites aligned with published pages while preserving manual or historical exclusions.

## Summary of what to report after publishing

When the publishing run is complete, report:

- Number of pages successfully published.
- List of any skipped/blocked files and the reason.
- List of any stale `.html` files deleted during cleanup (or confirmation that none needed to be deleted).
- Confirmation that `sitemap.xml` was updated.
- Confirmation that `staticwebapp.config.json` was updated (routes and/or excludes) when applicable.
- The full output path of every file written.
