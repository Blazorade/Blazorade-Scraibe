---
title: Publishing
description: How the Blazorade Scraibe publish workflow turns Markdown source files into static HTML pages using a PowerShell script and Copilot skill.
keywords: publishing, publish, static HTML, GitHub Copilot, sitemap, navigation, wwwroot, skill
changefreq: monthly
priority: 0.8
---

# Publishing

Publishing converts Markdown source files in `/content` into static HTML files in `wwwroot/`. The workflow is run through Copilot using `tools/Invoke-Publish.ps1`, which delegates generation to the publish tool.

In day-to-day use, this is mostly Copilot-driven: you ask Copilot to publish, and the toolchain handles the pipeline. Under the hood, publishing still performs full content-to-output generation across pages, assets, routes, and sitemap files.

For architecture context around where publishing fits in the full lifecycle, see [Architecture positioning](../core/architecture-positioning.md), [Constraints and rationale](../core/constraints-and-rationale.md), and [Runtime glossary](../core/runtime-glossary.md).

## What publishing produces

Each publish run updates or generates:

- Page HTML files under `wwwroot/` (one per Markdown page)
- `sitemap.xml`
- Eligible static assets copied from `/content`
- `staticwebapp.config.json` routing updates

Published HTML is the crawler-readable baseline. Runtime interactivity is layered on by Blazor at runtime.

For layout composition, the runtime matches named content fragments to layout placeholders using the `x-slot` attribute contract.

For detailed pipeline internals, see the repository publish instructions and tool source. For layout and part behavior, see [Page layouts](../site-building/page-layouts.md). For authoring rules that affect publish output, see [Content authoring](../authoring/content-authoring.md).

## Running a publish

Open Copilot chat in VS Code and ask to publish.

For most content authors and site builders, this is the primary workflow.

- No specific pages mentioned: full publish.
- Specific pages mentioned: partial publish.

You can also run the script directly:

```powershell
.\tools\Invoke-Publish.ps1
```

### Full publish

A plain publish request with no specific page named always triggers a full publish.

Examples of full publish requests:

- *"Please publish my site."*
- *"Publish all content."*
- *"Run publish."*
- *"Regenerate the site."*

### Partial publish

When you name specific pages, Copilot runs a partial publish. Only those pages are regenerated. Partial publish does not perform stale file cleanup or full static asset sync.

Examples of partial publish requests:

- *"Publish the current page."*
- *"Publish content/about.md."*
- *"Publish these files."* (with files attached to chat)
- *"Publish the pages I have open."*

**Note:** partial publish requires that each page has been published at least once by a prior full publish run.

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

For complete `.config.json` behavior, see [Folder configuration](../authoring/folder-configuration.md).

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

For navigation structure controls and configuration keys, see [Folder configuration](../authoring/folder-configuration.md).

## What publishing does not do

- It does not deploy to Azure Static Web Apps. Deployment is handled by your CI/CD pipeline or manually via the Azure CLI or SWA CLI.
- It does not modify your Markdown source files. Frontmatter values (including `date`) are never written back by the pipeline.
- It does not manually generate HTML in chat context. All HTML generation is performed by `tools/Scraibe.Publisher`. If Copilot appears to be generating HTML manually, that is a guardrail violation — tell it to run the script instead.

