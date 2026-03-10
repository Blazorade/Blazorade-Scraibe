---
title: Content Authoring
description: How to write and structure content for Blazorade Scraibe — frontmatter fields, reserved filenames, heading conventions, and shortcode syntax.
keywords: content authoring, Markdown, frontmatter, YAML, shortcodes, headings, slug
changefreq: monthly
priority: 0.8
---

# Content Authoring

All site content lives in the `/content` folder as plain Markdown files. Each file becomes one page on the published site. This page covers everything you need to know to write and structure content correctly.

## Frontmatter

Every content file should start with a YAML frontmatter block, delimited by `---` lines. All fields are optional unless noted.

```yaml
---
title: Page Title           # Required. Used in <title>, <h1>, and og:title.
description: Short text     # Used in <meta name="description"> and og:description.
slug: my-custom-url         # Overrides the filename-derived URL slug.
keywords: foo, bar, baz     # Injected into <meta name="keywords">.
author: Jane Smith          # Injected into <meta name="author">.
date: 2026-02-20            # Publication date (YYYY-MM-DD). Once set, never overwritten by the pipeline.
layout: default             # Page layout. Case-insensitive; falls back to "Default" when absent.
changefreq: monthly         # Sitemap change frequency. Defaults to monthly.
priority: 0.8               # Sitemap priority (0.0–1.0). Defaults to 0.8.
---
```

If `title` is absent, it is derived from the first `#` heading in the body. If no heading exists either, the filename is used.

### The `date` field

When `date` is set in frontmatter, the pipeline uses that value verbatim on every publish run. The pipeline never writes back to the source file, so the date you set is permanent until you change it manually. When `date` is omitted, the file's last-modified timestamp on disk is used instead — but that derived date is never saved to your frontmatter.

## Reserved Filenames

### `home.md`

`home.md` is the designated landing page for its containing directory. It has special routing behaviour:

| File | Published URL |
|------|--------------|
| `content/home.md` | `/` (site root) |
| `content/products/home.md` | `/products` |
| `content/products/widgets/home.md` | `/products/widgets` |

The `/home` segment is never shown in the URL. A `home.md` file always represents the directory it lives in, not a page named "home".

### `index.md`

`index.md` is **blocked at every level**. The file `wwwroot/index.html` is the Blazor application shell and must never be overwritten. Any `index.md` file encountered during publishing will be rejected with a warning and skipped.

### Name conflicts

A flat `.md` file and a same-named subdirectory cannot coexist at the same level. For example, having both `content/products.md` and `content/products/` is an error that aborts the entire publish run. A path in the URL space is either a leaf page or a section entry point — never both simultaneously.

## Directory Structure and URL Mapping

The published URL mirrors the `/content` folder structure exactly. Subdirectories become URL path segments:

| Source file | Published URL |
|-------------|--------------|
| `content/about.md` | `/about` |
| `content/blog/first-post.md` | `/blog/first-post` |
| `content/docs/api/reference.md` | `/docs/api/reference` |

If the `slug` frontmatter field is set, it overrides the filename (but not the directory path).

## Static Assets

You can place non-Markdown files next to your Markdown content in `/content` (for example images, PDF files, or downloadable attachments). During publish, eligible static assets are copied to `wwwroot/` at the same relative path.

Examples:

| Source file | Published path |
|-------------|----------------|
| `content/img/logo.png` | `wwwroot/img/logo.png` |
| `content/scraibe-docs/guide.pdf` | `wwwroot/scraibe-docs/guide.pdf` |

Eligibility rule:

- The filename must start with a letter or digit.
- The filename must not end with `.md`.

This means files like `_right-panel.md` and `.config.json` are excluded automatically.

Author links in Markdown using normal relative paths (for example `![Screenshot](screenshot.png)` or `[Download PDF](guide.pdf)`). The publish pipeline rewrites those links in generated HTML to root-relative URLs that match the copied asset location.

## Code Blocks

Use fenced code blocks with three backticks (or tildes) for all code examples.

```md
~~~csharp
var message = "Hello";
~~~
```

Indented code blocks (created by leading tabs or spaces) are intentionally not supported by the publish pipeline.

## Heading Conventions

- Use exactly one `#` heading per page. It becomes the `<h1>` and the page title if `title` is not set in frontmatter.
- Use `##` for top-level sections, `###` for subsections, and so on.
- Do not skip heading levels (e.g. jumping from `##` to `####`).
- Write descriptive headings — the publishing pipeline uses them as `aria-label` values on generated `<section>` elements.

## Shortcodes

Shortcodes let you embed live, interactive Blazor components directly in Markdown content using a simple bracket syntax. See the [Shortcodes](shortcodes/home.md) page for the full syntax reference and examples.

A quick overview:

**Self-closing** — use when the component needs no inner content:

```
[Alert Type="warning" Message="This is a warning." /]
```

**Wrapping (inline)** — use for short inner content on one line:

```
[Highlight Color="yellow"]important term[/Highlight]
```

**Wrapping (multi-line)** — use for rich inner content including nested Markdown or other shortcodes:

```
[CalloutBox Title="Note"]
This is a paragraph inside the component.

- It can include lists
- And other Markdown elements
[/CalloutBox]
```

Shortcodes are never processed inside code spans or fenced code blocks, so documenting them is always safe.

## Writing Tips

- Keep paragraphs short. Both search engines and AI crawlers favour scannable content.
- Use lists and tables for structured information rather than long prose.
- Prefer concrete, specific headings over vague ones — they anchor navigation and accessibility labels.
- The `description` frontmatter field appears in search result snippets; make it useful and accurate.
