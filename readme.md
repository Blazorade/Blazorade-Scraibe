# Blazorade Scraibe

A publishing framework that combines Blazor WebAssembly with GitHub Copilot-assisted content authoring. Write content in Markdown, embed live Blazor components via shortcodes, and publish to SEO-friendly static HTML.

> **Note:** This repository is under active development and should be considered a beta offering. Expect breaking changes, incomplete features, and evolving conventions as the framework matures.

Because every page is a static file, hosting requires nothing beyond basic file serving — no app server, no runtime, no database. Yet thanks to Blazor WebAssembly, you can still embed fully interactive applications in your content using shortcodes, much like WordPress plugins but with the full power of .NET. Sites built with Blazorade Scraibe are designed to run on Azure Static Web Apps — fast, globally distributed, and affordable. GitHub Pages is also supported as a free hosting alternative.

## Quick Start

The recommended way to get started is to create a new repository from this template using the **Use this template** button on GitHub. You can also clone or fork the repository if you prefer.

1. Create a new repository from this template (or clone/fork).
2. Open it in VS Code with GitHub Copilot enabled.
3. Copilot will detect the missing configuration and run first-time setup automatically.
4. Add Markdown files to `/content` and run the publish workflow.

## Prerequisites

Before you can run Blazorade Scraibe on your own machine, make sure the following software is installed.

- [Visual Studio Code](https://code.visualstudio.com/) — the editor the entire workflow is built around.
- [GitHub Copilot extension for VS Code](https://marketplace.visualstudio.com/items?itemName=GitHub.copilot) — drives publishing, first-run setup, and all AI-assisted authoring. Requires an active GitHub Copilot subscription.
- [.NET SDK](https://dotnet.microsoft.com/download) — needed to build and run the Blazor WebAssembly application. Version 10.0 or later is required.
- [Git](https://git-scm.com/downloads) — needed to clone the repository and manage your content history.
- [GitHub account](https://github.com/join) — required to use GitHub Copilot and to create a repository from this template.

For detailed setup instructions, system requirements, and optional tooling, see [Prerequisites](content/blazorade-docs/prerequisites) in the documentation.

## Key Features

### Markdown Authoring
Content is written as plain Markdown files with YAML frontmatter. No admin UI, no database, no proprietary format — just files in a folder that any editor can open.

### Shortcodes
Embed fully interactive Blazor components directly in Markdown using a simple bracket syntax. Components are defined once in a Razor Class Library and reused across any number of pages — from simple callout boxes to complex data-driven widgets.

### AI-Driven Publishing
The publish workflow is driven entirely by GitHub Copilot following structured instruction files. Copilot reads your Markdown, resolves shortcodes, generates semantic HTML, updates the sitemap, and regenerates the navigation menu — no build scripts or CLI tools required.

### Static HTML Output
Every page is published as a static `.html` bootstrapper. Crawlers, search engines, and AI bots see fully-formed HTML. No app server, no runtime, no database needed to serve the site.

### Interactive Blazor Runtime
When a user visits the site in a browser, the Blazor WebAssembly app takes over — fetching the static HTML, rendering the page, and activating any embedded Blazor components. Static for bots, interactive for humans.

### Zero-Config First Run
Open the repository in VS Code with GitHub Copilot enabled and Copilot automatically detects the missing configuration, walks you through setup, and scaffolds the Blazor projects — all through conversation, no manual scaffolding required.

### Affordable Hosting
Because the output is pure static files, sites are a natural fit for Azure Static Web Apps — fast, globally distributed, and inexpensive to run. GitHub Pages is also supported for fully free hosting. Either way, there is no server infrastructure to manage.

## About the Name

**Blazorade Scraibe** (/skraɪb/) is a blend of *Blazorade* and *Scribe* — with a deliberate twist: the spelling embeds **AI** in the middle of the word (scr-**AI**-be), reflecting the central role GitHub Copilot plays in the authoring and publishing workflow.

A [scribe](https://en.wikipedia.org/wiki/Scribe) was a professional trained to produce, copy, and distribute written knowledge. Before the printing press, scribes were the backbone of civilisation's information infrastructure — turning thought into published form with craft and precision. Blazorade Scraibe carries that same purpose into the modern web: taking your Markdown content and giving it a published, accessible, search-engine-visible form, with an AI agent as the intermediary between author and output.

## Documentation

Full documentation — content authoring, shortcodes, publishing, and styling — is in [`/content/blazorade-docs/`](content/blazorade-docs/home).

## License

See [LICENSE](LICENSE) for details.