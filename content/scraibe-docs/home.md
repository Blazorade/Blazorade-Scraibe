---
title: Blazorade Scraibe
description: Documentation for Blazorade Scraibe — a publishing framework for building static Blazor WebAssembly sites with GitHub Copilot-assisted content authoring and publishing.
keywords: Blazorade, Blazor, static site, GitHub Copilot, AI publishing, shortcodes, Markdown
changefreq: weekly
priority: 0.9
---

# Blazorade Scraibe

A publishing framework that combines [Blazor WebAssembly](https://learn.microsoft.com/aspnet/core/blazor/hosting-models#blazor-webassembly) with GitHub Copilot-assisted content authoring. Write content in Markdown, embed live Blazor components via shortcodes, and publish to SEO-friendly static HTML — with no app server, no runtime, and no database required. Sites are designed to run on [Azure Static Web Apps](https://azure.microsoft.com/products/app-service/static) or [GitHub Pages](https://pages.github.com/).

> **There are no build scripts, no CLI commands, and no pipelines to configure.** To publish your site, you open a Copilot chat and say: *"Please publish my site."* Copilot reads your Markdown, generates the static HTML, updates the sitemap, and regenerates the navigation — all through conversation.

[Alert alert-info]
## Removing These Docs From Your Site

This documentation is published on your site by default so you have working content to view immediately after setup and a reference for all built-in features. When your own content is ready and you no longer want these docs published, open `blazorade.config.md` in the repository root and add `scraibe-docs` as a bullet point under the `## Excluded Content` section:

```
## Excluded Content

- scraibe-docs
```
[/Alert]

The next publish run will skip all pages in this section. The source files remain in `/content/scraibe-docs/` and can be re-included at any time by removing the entry.

## How It Works

Every page you write in `/content` goes through a two-step lifecycle:

1. **Publish** — GitHub Copilot reads the Markdown file, resolves frontmatter metadata and shortcodes, generates semantic HTML, and writes a static `.html` bootstrapper to `wwwroot/`. The navigation menu is regenerated at the same time.
2. **Runtime** — When a user visits the site, the Blazor WASM app fetches the relevant `.html` file, resolves the page's named layout, and composes the full page by splicing each content part into its layout slot before rendering — including any live Blazor components embedded as shortcodes.

Crawlers and AI bots see the full static HTML directly. Browser users get the interactive Blazor experience. No server required.

## Key Concepts

### Markdown and Frontmatter

Pages are plain Markdown files with a YAML frontmatter block at the top. The frontmatter controls the page title, description, Open Graph metadata, sitemap settings, and more. See the [Content Authoring](content-authoring.md) page for the full frontmatter reference.

### Shortcodes

Shortcodes let you embed live Blazor components directly in Markdown content using a simple bracket syntax — no HTML, no code-behind files needed in the content itself. Components are defined once in the component library and reused across any number of pages. See the [Shortcodes](shortcodes/home.md) page for syntax and examples.

### Page Layouts

Each page is rendered inside a named layout — a static HTML file in the component library that defines `x-part` slots for the navbar, content area, footer, and any other structural regions. Content parts are gathered at publish time from `_name.md` scoped files, inline `[Part]` shortcodes, and an auto-generated navbar, then spliced into the layout at runtime. See [Page Layouts](page-layouts.md) for the full guide.

### Publishing

The publish workflow is driven entirely by GitHub Copilot following a set of structured instruction files. Running a publish processes one or more content files, generates their static HTML bootstrappers, updates the sitemap, and auto-generates the site navigation where no custom nav part is provided. See the [Publishing](publishing.md) page for the full workflow.

### Todo Items

Blazorade Scraibe includes a lightweight task-tracking system built directly into the repository. The `/todo` folder holds an index of active tasks and a backlog of ideas, with each active task having its own detail document containing full context, decisions made, and next steps. A completed-task log is maintained as a permanent record. Copilot can create new tasks, update them, promote backlog ideas, and close completed ones — all through conversation. No external tools or project management software required.

### Playbooks

Playbooks are site-specific, repeatable procedures stored in `/playbooks` and written in plain language. A playbook describes how to carry out a recurring task — a content freshness audit, a pre-launch readiness review, an onboarding process for a new content section — anything the site owner wants Copilot to know how to run. Playbooks are authored by the site owner, not shipped by the framework, so they reflect how *your* site operates. Copilot discovers available playbooks from the `/playbooks/home.md` index and triggers the right one based on what you ask for.

### Project Structure

```
content/                  # Markdown source files — edit these to update the site
playbooks/                # Site-specific repeatable procedures authored in plain language
todo/                     # Task-tracking documents: active tasks, backlog, and completed log
templates/                # Reusable scaffolding templates for new projects
  component-library/      # Razor Class Library template
  web-app/                # Blazor WASM app template
tools/                    # Publish pipeline script (Invoke-Publish.ps1) and supporting tools
.github/instructions/     # Copilot instruction files that drive authoring and publishing
src/                      # Generated on first run — not committed to the template repo
  {AppName}.Components/   # Razor Class Library: shortcode components
  {AppName}.Web/          # Blazor WebAssembly application
```

## In This Section

- [Prerequisites](prerequisites.md) — Required and optional software before you begin
- [Content Authoring](content-authoring.md) — Markdown structure, frontmatter fields, and writing guidelines
- [Shortcodes](shortcodes/home.md) — Embedding Blazor components in content
- [Page Layouts](page-layouts.md) — Choosing a layout, content parts, and shared `_name.md` files
- [Publishing](publishing.md) — How the publish workflow generates static HTML
- [Styling](styling.md) — CSS conventions and how to customise the look of your site

## Getting Started

If you are setting up a new site from this repository, open it in VS Code with GitHub Copilot enabled. Copilot will detect the missing `blazorade.config.md` configuration file and walk you through the automated first-run setup, which scaffolds the Blazor projects, copies and configures all template files, and prepares the content folder.

## About the Name

**Blazorade Scraibe** (/skraɪb/) is a blend of *Blazorade* and *Scribe* — with a deliberate twist: the spelling embeds **AI** in the middle of the word (scr-**AI**-be), reflecting the central role GitHub Copilot plays in the authoring and publishing workflow.

A [scribe](https://en.wikipedia.org/wiki/Scribe) was a professional trained to produce, copy, and distribute written knowledge. Before the printing press, scribes were the backbone of civilisation's information infrastructure — turning thought into published form with craft and precision. Blazorade Scraibe carries that same purpose into the modern web: taking your Markdown content and giving it a published, accessible, search-engine-visible form, with an AI agent as the intermediary between author and output.

