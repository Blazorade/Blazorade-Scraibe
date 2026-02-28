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
5. **Wrap** the HTML in the page shell template from `{WebAppPath}/page-template.html`, substituting per-page tokens.
6. **Write** the complete file to `{WebAppPath}/wwwroot/{path}.html`.

After all pages are processed:

7. **Delete stale files** — any `.html` files in `wwwroot/` that no longer have a corresponding source document are removed (except `index.html`, which is the Blazor app shell and is never touched).
8. **Regenerate `sitemap.xml`** with an entry for every published page.
9. **Regenerate `staticwebapp.config.json`** with rewrite rules mapping clean URLs to the corresponding `.html` files.
10. **Regenerate `NavMenu.razor`** with a link for every top-level page and a dropdown for every subdirectory.

## Running a Publish

There are no build scripts, no CLI commands, and no pipelines to configure. Publishing is a conversation.

Open a Copilot chat in VS Code (`Ctrl+Alt+I` / `Cmd+Alt+I`) and simply describe what you want to publish. Copilot reads the structured instructions in `.github/instructions/publish.instructions.md`, loads the site configuration from `blazorade.config.md`, and does the rest autonomously.

Some examples of things you can say:

- *"Please publish my site."*
- *"Publish all content."*
- *"Publish content/about.md."*
- *"Publish everything under content/blog/."*
- *"I've updated the homepage and the about page — please publish those."*

Copilot will tell you what it is about to do before it starts, process each file, and give you a summary of everything that was written, updated, or deleted when it is done.

### What Copilot Does Autonomously

You do not need to tell Copilot *how* to publish — only *what* to publish. It already knows:

- How to parse frontmatter and derive titles, slugs, and dates.
- How to resolve shortcodes against your component library.
- Which files are excluded based on `blazorade.config.md`.
- How to build the page shell from `page-template.html`.
- How to regenerate the sitemap, route rewrites, and navigation menu.
- Which stale HTML files to clean up after a partial publish.

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
| `{redirect_to}` | Clean URL the SWA rewrite rule points to |
| `{body_html}` | The converted HTML body of the page |
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

The top navigation bar is regenerated from scratch on every publish run. Top-level pages get a direct link. Subdirectories get a dropdown whose label comes from the `title` field in the subdirectory's `home.md` frontmatter.

The generated file is `{WebAppPath}/Components/NavMenu.razor`. Do not edit it manually — it will be overwritten the next time you publish.

## What Publishing Does Not Do

- It does not run `dotnet build` or `dotnet publish`. The Blazor app is a separate concern.
- It does not deploy to Azure or GitHub Pages. Deployment is handled by your CI/CD pipeline or manually via the Azure CLI / SWA CLI.
- It does not modify your Markdown source files. Frontmatter values (including `date`) are never written back by the pipeline.
