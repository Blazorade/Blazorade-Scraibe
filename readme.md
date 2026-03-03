# Blazorade Scraibe

A publishing framework that combines [Blazor WebAssembly](https://learn.microsoft.com/aspnet/core/blazor/hosting-models#blazor-webassembly) with GitHub Copilot-assisted content authoring. Write content in Markdown, embed live Blazor components via shortcodes, and publish to SEO-friendly static HTML — with no app server, no runtime, and no database required. Sites are designed to run on [Azure Static Web Apps](https://azure.microsoft.com/products/app-service/static) or [GitHub Pages](https://pages.github.com/).

> **There are no build scripts, no CLI commands, and no pipelines to configure.** To publish your site, you open a Copilot chat and say: *"Please publish my site."* Copilot reads your Markdown, generates the static HTML, updates the sitemap, and regenerates the navigation — all through conversation.

> **Note:** This repository is under active development and should be considered a beta offering. Expect breaking changes, incomplete features, and evolving conventions as the framework matures.

## Quick Start

The recommended way to get started is to create a new repository from this template using the **Use this template** button on GitHub. You can also clone or fork the repository if you prefer.

1. Create a new repository from this template (or clone/fork).
2. Open it in VS Code with GitHub Copilot enabled.
3. Copilot will detect the missing configuration and run first-time setup automatically.
4. Add Markdown files to `/content` and run the publish workflow.

## Key Features

- **[Markdown Authoring](content/scraibe-docs/content-authoring.md)** — Write pages as plain Markdown files with YAML frontmatter. No admin UI, no database, no proprietary format.
- **[Shortcodes](content/scraibe-docs/shortcodes/home.md)** — Embed fully interactive Blazor components directly in Markdown using a simple bracket syntax.
- **[AI-Driven Publishing](content/scraibe-docs/publishing.md)** — Copilot reads your Markdown, resolves shortcodes, generates semantic HTML, updates the sitemap, and auto-generates the site navigation — no scripts or CLI tools required.
- **[Page Layouts](content/scraibe-docs/page-layouts.md)** — Choose named layouts for different page types (default, landing, custom). Define shared content parts as `_name.md` scoped files or inline `[Part]` shortcodes, and rely on the auto-generated navbar when no custom nav is provided.
- **[Styling](content/scraibe-docs/styling.md)** — Global styles in `app.css`, component-scoped CSS isolation, and a customisable page shell template.
- **[Todo Items](content/scraibe-docs/home.md#todo-items)** — Track tasks and ideas for your site directly in the repository. Copilot creates, updates, and closes todo items through conversation — no external tool required.
- **[Playbooks](content/scraibe-docs/home.md#playbooks)** — Define custom, repeatable procedures for your site in plain language. Copilot triggers and follows any playbook on request, from content audits to pre-launch checklists.
- **Static HTML Output** — Every page is a static `.html` bootstrapper. Crawlers, search engines, and AI bots see fully-formed HTML.
- **Interactive Blazor Runtime** — The Blazor WebAssembly app takes over at runtime, rendering pages and activating embedded components. Static for bots, interactive for humans.
- **Zero-Config First Run** — Copilot detects the missing configuration, walks you through setup, and scaffolds the Blazor projects — all through conversation.

## About the Name

**Blazorade Scraibe** (/skraɪb/) is a blend of *Blazorade* and *Scribe* — with a deliberate twist: the spelling embeds **AI** in the middle of the word (scr-**AI**-be), reflecting the central role GitHub Copilot plays in the authoring and publishing workflow.

A [scribe](https://en.wikipedia.org/wiki/Scribe) was a professional trained to produce, copy, and distribute written knowledge. Before the printing press, scribes were the backbone of civilisation's information infrastructure — turning thought into published form with craft and precision. Blazorade Scraibe carries that same purpose into the modern web: taking your Markdown content and giving it a published, accessible, search-engine-visible form, with an AI agent as the intermediary between author and output.

## Documentation

Full documentation — setup, content authoring, shortcodes, publishing, styling, and everything else you need to get started — is in [`/content/scraibe-docs/`](content/scraibe-docs/home.md).

## License

See [LICENSE](LICENSE) for details.