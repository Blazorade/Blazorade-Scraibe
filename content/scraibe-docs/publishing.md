---
title: Publishing
description: How the Blazorade Scraibe publish workflow turns Markdown source files into static HTML pages using a PowerShell script and Copilot skill.
keywords: publishing, publish, static HTML, GitHub Copilot, sitemap, navigation, wwwroot, skill
changefreq: monthly
priority: 0.8
---

# Publishing

Publishing is the process of converting your Markdown source files in `/content` into static HTML files in `wwwroot/`. The workflow is driven by a combination of a GitHub Copilot skill (`.github/skills/publish/skill.md`) and a PowerShell script (`tools/Invoke-Publish.ps1`) that delegates to a .NET console tool. Copilot detects your publish request, chooses the correct mode (full or partial), and invokes the script — the entire HTML generation pipeline runs inside the tool, not in chat context.

## How it works

When you ask Copilot to publish, it reads `.github/skills/publish/skill.md` to determine the correct mode and invokes the script. The script reads `.config.json`, builds the component library, and hands everything off to `tools/Scraibe.Publisher` — a .NET console tool that owns the complete pipeline:

1. **Walk** all `.md` files recursively under `/content`.
2. **Parse** the YAML frontmatter to extract metadata (title, description, slug, date, etc.).
3. **Process shortcodes** — scan the body for `[ComponentName ...]` syntax and replace matched shortcodes with `<x-shortcode>` sentinel elements that the Blazor runtime activates later.
4. **Convert** the processed Markdown body to semantic HTML.
5. **Rewrite relative URLs** in the generated HTML to root-relative form (for example `product1.jpg` becomes `/products/product1.jpg` when published from `content/products/home.md`). Relative links to Markdown files are also resolved to clean URLs (for example `product2.md` becomes `/products/product2`).
6. **Compose** the HTML using the layout template from `{ComponentLibraryPath}/wwwroot/Layouts/`, resolving parts from `_name.md` files and `[Part]` shortcodes.
7. **Wrap** the composed layout in the page shell template from `{WebAppPath}/page-template.html`, substituting per-page tokens.
8. **Write** the complete file to `{WebAppPath}/wwwroot/{path}.html`.

After all pages are processed:

9. **Delete stale files** — any `.html` files in `wwwroot/` that no longer have a corresponding source document are removed (except `index.html`, which is the Blazor app shell and is never touched).
10. **Regenerate `sitemap.xml`** with an entry for every published page.
11. **Sync static assets** — eligible non-Markdown files from `/content` are copied to `wwwroot/` at the same relative paths.
12. **Update `staticwebapp.config.json`** — publish updates clean-URL rewrite routes and merges navigation fallback excludes additively.

Content authors should write normal relative links and image references in Markdown so they work in local preview. The publisher rewrites them during publish; no manual root-relative URL maintenance is needed.

## Running a publish

Open a Copilot chat in VS Code (`Ctrl+Alt+I` / `Cmd+Alt+I`) and describe what you want to publish. In Copilot skill mode, you can invoke the skill directly with `/publish` and include intent such as `/publish current page`, `/publish attached pages`, or `/publish content/about.md`. Copilot reads `.github/skills/publish/skill.md`, loads the site configuration from `.config.json`, and invokes the appropriate command.

### Full publish

A plain publish request with no specific page named always triggers a full publish.

Examples of full publish requests:

- *"Please publish my site."*
- *"Publish all content."*
- *"Run publish."*
- *"Regenerate the site."*

### Partial publish

When you name specific pages, Copilot runs a partial publish. Only the specified pages' HTML files are regenerated, and their entries in `sitemap.xml` are patched (creating `sitemap.xml` if it does not exist). Partial publish does not delete stale `.html` files or sync static assets, but it may update `staticwebapp.config.json` when clean-URL routes or navigation fallback excludes change.

Examples of partial publish requests:

- *"Publish the current page."*
- *"Publish content/about.md."*
- *"Publish these files."* (with files attached to chat)
- *"Publish the pages I have open."*

Copilot builds the `-Pages` argument from your request and runs:

```powershell
.\tools\Invoke-Publish.ps1 -Pages "about.md,scraibe-docs/mermaid.md"
```

**Note:** partial publish requires that each page has been published at least once by a prior full publish run. If a page's `.html` file does not yet exist in `wwwroot/`, the script aborts with a pre-flight error. Run a full publish first.

- How to parse frontmatter and derive titles, slugs, and dates.
- How to resolve shortcodes against your component library.
- Which files are excluded based on `.config.json`.
- How to build the page shell from `page-template.html`.
- How to embed the site navigation into each static HTML page.
- How to regenerate the sitemap.
- Which stale HTML files to clean up.
- Which content-derived static assets to copy.
- How `staticwebapp.config.json` is updated during full and partial publish runs.

To build the component library in Release mode:

```powershell
.\tools\Invoke-Publish.ps1 -Configuration Release
```

You can ask Copilot to use this by saying *"publish the site with a release build"* or similar.

## Excluded content

You can prevent specific files or directories from being published by adding them to `scraibe.publish.excludedContent` in `.config.json`:

```json
{
	"scoped": {
		"scraibe.publish.excludedContent": [
			"scraibe-docs",
			"drafts"
		]
	}
}
```

Any path listed there (relative to `/content`) is skipped entirely on every publish run. The source files remain on disk and can be re-included at any time by removing the entry.

## Folder configuration files

The publish pipeline consumes `.config.json` files from repository root and nested folders to resolve effective configuration for each page. These files are control metadata:

- They are parsed during publish.
- They are not published as pages.
- They are not copied to `wwwroot/` as static assets.

## The Page Template

Every published page is built from the shell template at `{WebAppPath}/page-template.html`. This file contains the full HTML document structure — `<head>`, Open Graph tags, Blazor script references — with placeholder tokens that the pipeline substitutes per page. You can edit this file to change the global HTML structure, add analytics scripts, or modify the `<head>` content that appears on every page.

The key tokens in the template:

| Token | Replaced with |
|-------|--------------|
| `{title}` | Page title from frontmatter or first heading |
| `{description}` | `description` frontmatter field |
| `{slug}` | URL-relative path without extension |
| `{cleanSlug}` | Canonical clean URL path used in canonical/og tags |
| `{layout_html}` | Fully composed layout HTML for the page |
| `{keywords}` | `keywords` frontmatter field (line omitted if absent) |
| `{author}` | `author` frontmatter field (line omitted if absent) |
| `{date}` | `date` from frontmatter, or file last-modified timestamp |

## URL structure

Published URLs mirror the `/content` directory structure. The `.html` extension is hidden from users by the rewrite rules in `staticwebapp.config.json`:

| Source file | Output file | Clean URL |
|-------------|-------------|-----------|
| `content/about.md` | `wwwroot/about.html` | `/about` |
| `content/blog/post.md` | `wwwroot/blog/post.html` | `/blog/post` |
| `content/home.md` | `wwwroot/home.html` | `/` |
| `content/products/home.md` | `wwwroot/products/home.html` | `/products` |

## Navigation

The top navigation bar is regenerated from scratch on every publish run and embedded directly inside each static `.html` file. Top-level pages get a direct link. Subdirectories get a dropdown whose label comes from the `title` field in the subdirectory's `home.md` frontmatter.

Because the navigation is part of each static file rather than a separate component, crawlers and AI bots see the full site structure on every page without executing any JavaScript.

## What publishing does not do

- It does not deploy to Azure Static Web Apps. Deployment is handled by your CI/CD pipeline or manually via the Azure CLI or SWA CLI.
- It does not modify your Markdown source files. Frontmatter values (including `date`) are never written back by the pipeline.
- It does not manually generate HTML in chat context. All HTML generation is performed by `tools/Scraibe.Publisher`. If Copilot appears to be generating HTML manually, that is a guardrail violation — tell it to run the script instead.

