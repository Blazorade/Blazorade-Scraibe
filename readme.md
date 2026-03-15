# Blazorade Scraibe

**Your Blazor site — published, SEO-ready, and free to host — with GitHub Copilot as your site builder.**

## The problem with Blazor and SEO

Blazor WebAssembly ships an empty HTML shell. Crawlers and AI bots see nothing. You fix that one of two ways:

- **[Blazor Server with SSR](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes)** — an always-on app server, a paid hosting plan, SignalR latency overhead, and a deployment pipeline to maintain. For a content site, that is enormous overhead.
- **You write all the HTML yourself** — which defeats the point.

There has never been a good middle ground. Until now.

Blazorade Scraibe solves a real gap that mainstream Blazor workflows still leave open: crawler-visible static content plus live Blazor component enhancement, without forcing you into server-side rendering infrastructure.

## What Blazorade Scraibe does

Write your content in Markdown. Open a Copilot chat. Say *"publish my site"*. Every page becomes a fully-formed, crawler-visible static HTML file — served for free on [Azure Static Web Apps](https://azure.microsoft.com/products/app-service/static). No app server. No pipeline engineering. No custom JavaScript required from content authors or site builders.

**The hard parts you run into have already been solved and captured as structured instructions for GitHub Copilot.** Content authoring, shortcode resolution, page layouts, navigation, sitemap generation, styling conventions, first-run setup, repeatable procedures — all of it is version-controlled with your site and followed autonomously by Copilot every time you ask.

You do not configure tools. You have a conversation.

## Copilot as your site builder — on steroids

Most teams use AI to generate boilerplate faster. Blazorade Scraibe is built around a different idea: let the scripts handle the deterministic work, and let the AI handle everything that requires judgement.

Copilot can draft your page content, suggest structure, pick a layout, generate a [Mermaid](https://mermaid.js.org) diagram from a plain-language description, advise on styling, write a shortcode component, set up a new content section, run a content audit, close a task, or onboard a new contributor — all through conversation, all within VS Code.

It is not a generator. It is a collaborator that already knows how your site works. Some things you can just say:

- *"How can you help me with my site?"*
- *"Write a compact, SEO-friendly description for this page."*
- *"I need a new Products section with a landing page and three sub-pages."*
- *"Generate a sequence diagram showing our checkout flow."*
- *"Publish the page I just finished writing."*
- *"What tasks are still open for this site?"*

## What you get

- **SEO and AIO out of the box** — Static HTML on every page. Crawlers and AI bots see real content, not a JavaScript shell. No [Blazor Server](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-server) required.
- **Content-linked static assets** — Place images and files next to Markdown in `/content`; publish copies them to matching paths in `wwwroot`.
- **Shortcodes — [WordPress power](https://codex.wordpress.org/Shortcode_API), Blazor quality** — Embed fully interactive [Razor components](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/) directly in Markdown using a simple bracket syntax. Pure .NET, no sandboxing.
- **Zero custom JavaScript. Fully responsive. Fully interactive.** — No npm app stack, no bundlers to maintain, no JS authoring burden for content authors or site builders. You write C# and Markdown. Blazorade handles the rest.
- **Free hosting** — Designed for [Azure Static Web Apps](https://azure.microsoft.com/products/app-service/static): clean-URL routing, custom domains, and HTTPS on the free tier.

## Configuration

Blazorade Scraibe uses `.config.json` files for machine-readable settings.

- The root `.config.json` defines site identity (`scraibe.site.*`), publish exclusions (`scraibe.publish.excludedContent`), and the default layout (`scraibe.layout.default`).
- Nested `.config.json` files support folder-level inheritance using `scoped` and folder-only overrides using `local`.
- Effective settings are resolved from repository root to the target folder; nearest matching key wins.

See [content/scraibe-docs/authoring/content-authoring.md](content/scraibe-docs/authoring/content-authoring.md) for full configuration rules and examples.

## Convinced? Start here

**[Read the full documentation →](content/scraibe-docs/home.md)** — every feature, every concept, and every reason you should build your next site with Blazorade Scraibe instead of anything else.

Getting started is as easy as 1-2-3:

1. Create a new repository from this template using the **Use this template** button on GitHub.
2. Open it in VS Code with GitHub Copilot enabled.
3. Add Markdown files to `/content` and say *"publish my site"*.

**[Create your new repository here](https://github.com/new?template_name=Blazorade-Scraibe&template_owner=Blazorade)**

## About the name

**Blazorade Scraibe** (/skraɪb/) blends *Blazorade* and *Scribe* — with a deliberate twist: the spelling embeds **AI** in the middle of the word (scr-**AI**-be). A [scribe](https://en.wikipedia.org/wiki/Scribe) was the professional who turned thought into published form. Blazorade Scraibe does the same thing — with an AI agent as the intermediary between author and output.

> **Beta:** This repository is under active development. Expect breaking changes, incomplete features, and evolving conventions.

## License

See [LICENSE](LICENSE) for details.