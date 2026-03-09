---
title: Publishing
description: How the Blazorade Scraibe publish workflow turns Markdown source files into static HTML pages using GitHub Copilot.
keywords: publishing, publish, static HTML, GitHub Copilot, sitemap, navigation, wwwroot
changefreq: monthly
priority: 0.8
---

# Publishing

Publishing is the process of converting your Markdown source files in `/content` into static HTML files in `wwwroot/`. The entire workflow is driven by GitHub Copilot — there are no build scripts, CLI commands, or manual steps required.

## How It Works

When you ask Copilot to publish, it follows the structured instructions in `.github/instructions/publish.instructions.md`. The process for each page is:

1. **Read** the Markdown source file from disk.
2. **Parse** the YAML frontmatter to extract metadata (title, description, slug, date, etc.).
3. **Process shortcodes** — scan the body for `[ComponentName ...]` syntax and replace matched shortcodes with `<x-shortcode>` sentinel elements that the Blazor runtime will activate later.
4. **Convert** the processed Markdown body to semantic HTML.
5. **Rewrite relative URLs** in the generated HTML to root-relative form (for example `product1.jpg` becomes `/products/product1.jpg` when published from `content/products/home.md`). Relative links to Markdown files are also resolved to clean URLs (for example `product2.md` becomes `/products/product2`).
6. **Wrap** the HTML in the page shell template from `{WebAppPath}/page-template.html`, substituting per-page tokens.
7. **Write** the complete file to `{WebAppPath}/wwwroot/{path}.html`.

After all pages are processed:

8. **Delete stale files** — any `.html` files in `wwwroot/` that no longer have a corresponding source document are removed (except `index.html`, which is the Blazor app shell and is never touched).
9. **Regenerate `sitemap.xml`** with an entry for every published page.
10. **Sync static assets** — eligible non-Markdown files from `/content` are copied to `wwwroot/` at the same relative paths.
11. **Update `staticwebapp.config.json`** — publish updates clean-URL rewrite routes and merges navigation fallback excludes additively.

Content authors should write normal relative links and image references in Markdown so they work in local preview. The publisher rewrites them during publish; no manual root-relative URL maintenance is needed.

Static assets are synced in the same publish run, so image and download links continue to resolve without manual copy steps.

Navigation is not a separate generated file — it is embedded as a block of HTML inside each page during step 6 above. Every static `.html` file contains the full site navigation, so crawlers and AI bots see all the links immediately without running any JavaScript.

## Running a Publish

There are no build scripts, no CLI commands, and no pipelines to configure. Publishing is a conversation.

Open a Copilot chat in VS Code (`Ctrl+Alt+I` / `Cmd+Alt+I`) and simply describe what you want to publish. Copilot reads the structured instructions in `.github/instructions/publish.instructions.md`, loads the site configuration from `blazorade.config.md`, and does the rest autonomously.

Some examples of things you can say:

- *"Please publish my site."*
- *"Publish all content."*
- *"Publish content/about.md."*

Copilot will tell you what it is about to do before it starts, process each file, and give you a summary of everything that was written, updated, or deleted when it is done.

### What Copilot Does Autonomously

You do not need to tell Copilot *how* to publish — only *what* to publish. It already knows:

- How to parse frontmatter and derive titles, slugs, and dates.
- How to resolve shortcodes against your component library.
- Which files are excluded based on `blazorade.config.md`.
- How to build the page shell from `page-template.html`.
- How to embed the site navigation into each static HTML page.
- How to regenerate the sitemap.
- Which stale HTML files to clean up.
- Which content-derived static assets to copy.
- How `staticwebapp.config.json` is updated during full and partial publish runs.

The instructions that govern all of this are version-controlled in `.github/instructions/publish.instructions.md`. You can read and modify them if you need to change how publishing behaves for your site.

## Excluded Content

You can prevent specific files or directories from being published by adding them to `blazorade.config.md`:

```markdown
## Excluded Content

- scraibe-docs
- drafts
```

Any path listed there (relative to `/content`) is skipped entirely on every publish run. The source files remain on disk and can be re-included at any time by removing the entry.

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

## URL Structure

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

## What Publishing Does Not Do

- It does not run `dotnet build` or `dotnet publish`. The Blazor app is a separate concern.
- It does not deploy to Azure Static Web Apps. Deployment is handled by your CI/CD pipeline or manually via the Azure CLI / SWA CLI.
- It does not modify your Markdown source files. Frontmatter values (including `date`) are never written back by the pipeline.
