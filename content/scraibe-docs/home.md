---
title: Blazorade Scraibe
description: Documentation for Blazorade Scraibe — a publishing framework for building static Blazor WebAssembly sites with GitHub Copilot-assisted content authoring and publishing.
keywords: Blazorade, Blazor, static site, GitHub Copilot, AI publishing, shortcodes, Markdown
changefreq: weekly
priority: 0.9
---

# Blazorade Scraibe

A publishing framework that combines [Blazor WebAssembly](https://learn.microsoft.com/aspnet/core/blazor/hosting-models#blazor-webassembly) with GitHub Copilot-assisted content authoring. Write content in Markdown, embed live Blazor components via shortcodes, and publish to SEO-friendly static HTML — with no server runtime and no database required. Sites are designed to run on [Azure Static Web Apps](https://azure.microsoft.com/products/app-service/static), which provides clean-URL routing, custom domains, and HTTPS — all on the free tier. Any static file host works too, but without routing rule support you lose the SEO and clean-URL benefits.

> **You do not need to run publish tools manually.** Ask Copilot to publish your site and it will execute the repository's publish workflow, generate static HTML, update the sitemap, and refresh navigation.

[Alert alert-info]
## Removing These Docs From Your Site

This documentation is published on your site by default so you have working content to view immediately after setup and a reference for all built-in features. When your own content is ready and you no longer want these docs published, add `scraibe-docs` to `scraibe.publish.excludedContent` in the repository-root `.config.json` file:

```json
{
  "local": {
    "scraibe.publish.excludedContent": [
      "scraibe-docs"
    ]
  }
}
```
[/Alert]

The next publish run will skip all pages in this section. The source files remain in `/content/scraibe-docs/` and can be re-included at any time by removing the entry.

## Key Features

- **[Markdown Authoring](content-authoring.md)** — Write pages as plain Markdown files with YAML frontmatter. No admin UI, no database, no proprietary format.
- **[Shortcodes](shortcodes/home.md)** — Embed fully interactive Blazor components directly in Markdown using a [WordPress-style shortcode syntax](https://codex.wordpress.org/Shortcode_API). Components are pure [Razor components](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/) — no JavaScript, no sandboxing, full .NET.
- **[AI as Intelligent Collaborator](publishing.md)** — Copilot drafts content, suggests structure, generates layouts, creates Mermaid diagrams, and advises on styling — while traditional scripts handle the mechanical work. A junior site builder, available on demand through conversation.
- **[Free SEO and AIO on Static Hosting](hosting.md)** — Full static HTML for every page, hosted free on Azure Static Web Apps. No [Blazor Server](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-server) app server required. Search engines and AI bots see real, indexable HTML — not an empty JavaScript shell.
- **[Page Layouts](page-layouts.md)** — Named layouts for different page types (default, landing, custom). Shared content parts defined as `_name.md` scoped files or inline `[Part]` shortcodes.
- **[Folder Configuration](folder-configuration.md)** — Define folder-level `.config.json` settings with `local` and `scoped` inheritance for publish and runtime behavior.
- **[Styling](styling.md)** — Bootstrap 5 compiled from SCSS. Customise design tokens in `_variables.scss`. No Node.js, no npm.
- **[Mermaid Diagrams](mermaid.md)** — Publish flowcharts, sequence diagrams, and more via a shortcode. The AI agent generates [Mermaid](https://mermaid.js.org) syntax from a plain-language description.
- **[Todo Items](#todo-items)** — Track tasks and ideas in the repository itself. Copilot creates, updates, and closes todo items through conversation — no external tool required.
- **[Playbooks](#playbooks)** — Define custom, repeatable procedures in plain language. Copilot discovers and runs any playbook on request.
- **Zero JavaScript — fully responsive, fully interactive** — No JavaScript required from you. Ever. Navigation, interactivity, Bootstrap behaviour, and Mermaid rendering are handled entirely by Blazorade libraries and .NET code.
- **Zero-Config First Run** — Copilot detects the missing configuration, walks you through setup, and scaffolds the Blazor projects — all through conversation.

## Why Blazorade Scraibe?

### AI as intelligent collaborator

Most tools treat AI as automation glue — a way to run the same steps faster. Blazorade Scraibe uses AI differently. The mechanical parts of publishing (HTML generation, sitemap updates, nav regeneration) are handled by traditional scripts and .NET tools, precisely so the AI agent can focus on where it adds real value: drafting and refining content, suggesting page structure, generating layouts, creating Mermaid diagrams, advising on styling — the work of a junior site builder, available on demand through conversation.

### Shortcodes — WordPress power, Blazor quality

[Shortcodes](https://codex.wordpress.org/Shortcode_API) are one of the most-loved features in WordPress: a simple bracket syntax that lets content authors embed rich, interactive components directly in Markdown — no HTML, no code-behind files required in the content itself. Blazorade Scraibe brings that same authoring experience to Blazor. The difference is that the components behind the shortcodes are pure [Razor components](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/) and .NET code — no JavaScript, no sandboxing, full access to the .NET ecosystem.

### Free SEO and AIO on static hosting

Until now, getting crawler-visible HTML from a Blazor application meant writing it as a [Blazor Server](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-server) app with server-side rendering — which requires an always-on app server, a paid hosting plan, and carries the latency cost of a persistent SignalR connection per connected user. Blazorade Scraibe delivers the same result — full static HTML for every page — hosted entirely for free on Azure Static Web Apps. Search engines and AI bots see real, indexable HTML, not an empty JavaScript shell. The upcoming [Schema.org JSON-LD](https://developers.google.com/search/docs/appearance/structured-data/intro-structured-data) support will take structured data beyond what any other Blazor publishing platform currently offers.

### Zero JavaScript — fully responsive, fully interactive

The Blazorade-wide promise extends to Blazorade Scraibe: you do not have to write a single line of JavaScript. Navigation, component interactivity, Bootstrap behaviour, and Mermaid diagram rendering are all handled by Blazorade libraries and .NET code. No npm, no bundlers, no JS configuration files. Just C# and Markdown — and a site that is fully responsive on any device and fully interactive in the browser.

### Mermaid diagrams, AI-assisted

[Mermaid](https://mermaid.js.org) lets you define flowcharts, sequence diagrams, entity-relationship diagrams, and more in plain text — but the syntax is not always intuitive. Blazorade Scraibe renders Mermaid diagrams directly in published pages via a shortcode, and the AI agent can generate and explain diagram syntax from a plain-language description. You describe the diagram; the agent writes the code.

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

## Getting Started

If you are setting up a new site from this repository, open it in VS Code with GitHub Copilot enabled. Copilot will detect missing setup configuration (or a legacy config that needs migration) and walk you through the first-run setup, which scaffolds the Blazor projects, copies and configures template files, and prepares the content folder.

[LinkButton href="https://github.com/new?template_name=Blazorade-Scraibe&template_owner=Blazorade" OpenInNewTab="true" btn-success btn-lg my-4]Create your new repository here[/LinkButton]

## About the Name

**Blazorade Scraibe** (/skraɪb/) is a blend of *Blazorade* and *Scribe* — with a deliberate twist: the spelling embeds **AI** in the middle of the word (scr-**AI**-be), reflecting the central role GitHub Copilot plays in the authoring and publishing workflow.

A [scribe](https://en.wikipedia.org/wiki/Scribe) was a professional trained to produce, copy, and distribute written knowledge. Before the printing press, scribes were the backbone of civilisation's information infrastructure — turning thought into published form with craft and precision. Blazorade Scraibe carries that same purpose into the modern web: taking your Markdown content and giving it a published, accessible, search-engine-visible form, with an AI agent as the intermediary between author and output.

