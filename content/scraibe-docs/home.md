---
title: Blazorade Scraibe
description: Start here for Blazorade Scraibe. Learn what it is, how the runtime model works, and how to use the authoring, publishing, and site-building documentation.
keywords: Blazorade, Scraibe, documentation, runtime model, publishing, shortcodes, glossary
changefreq: weekly
priority: 0.9
---

# Blazorade Scraibe

Blazorade Scraibe is a content publishing framework for static Blazor WebAssembly sites. You author content in Markdown, publish it into static HTML files, and let the Blazor runtime enhance pages with live components where needed.

This section is the canonical documentation set for understanding Scraibe's model, writing content, and operating the publish workflow.

[Alert alert-info]
## Removing these docs from your site

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

## Who this documentation is for

This documentation primarily targets two audience roles:

- **Content authors**: people who write and maintain content pages under `/content`.
- **Site builders**: people who shape site structure, styling, layouts, hosting, and publish behavior.

If you are a content author, start with:

1. [Content authoring](authoring/content-authoring.md)
2. [Shortcodes](authoring/shortcodes/home.md)
3. [Mermaid diagrams](authoring/mermaid.md)
4. [Publishing](operations/publishing.md)

If you are a site builder, start with:

1. [Architecture positioning](core/architecture-positioning.md)
2. [Page layouts](site-building/page-layouts.md)
3. [Styling](site-building/styling.md)
4. [Hosting](site-building/hosting.md)
5. [Publishing](operations/publishing.md)

## Start here

If you are new to Scraibe, read these pages first and in order:

1. [Core concepts](core/architecture-positioning.md)
2. [What Scraibe is and is not](core/what-scraibe-is-and-is-not.md)
3. [Constraints and rationale](core/constraints-and-rationale.md)
4. [Operations](operations/publishing.md)
5. [Runtime glossary](core/runtime-glossary.md)

The next publish run will skip all pages in this section. The source files remain in `/content/scraibe-docs/` and can be re-included at any time by removing the entry.

## Core concepts

- [Architecture positioning](core/architecture-positioning.md) - The runtime model and execution contexts in one page.
- [What Scraibe is and is not](core/what-scraibe-is-and-is-not.md) - Product boundaries and non-goals.
- [Constraints and rationale](core/constraints-and-rationale.md) - Plain-language explanation of core architecture constraints.
- [Runtime glossary](core/runtime-glossary.md) - Shared terminology for authoring, publishing, and runtime behavior.

## Authoring guides

- [Content authoring](authoring/content-authoring.md) - Frontmatter, structure, routing, assets, and content rules.
- [Folder configuration](authoring/folder-configuration.md) - local and scoped configuration inheritance.
- [Shortcodes](authoring/shortcodes/home.md) - Embed live Blazor components in Markdown.
- [Mermaid diagrams](authoring/mermaid.md) - Author and render Mermaid diagrams in content.

## Site builder guides

- [Page layouts](site-building/page-layouts.md) - Layout slots and content-part composition.
- [Styling](site-building/styling.md) - Bootstrap SCSS pipeline and theme customization.
- [Hosting](site-building/hosting.md) - Hosting choices and why routing support matters.
- [Prerequisites](site-building/prerequisites.md) - Tools required for setup and local work.

## Operational reference

- [Publishing](operations/publishing.md) - How publish runs, what it generates, and what it does not do.
- [Runtime glossary](core/runtime-glossary.md) - Definitions for core terms used across docs.
- [Hosting](site-building/hosting.md) - Routing support implications for SEO and crawler visibility.

## How it works

Scraibe pages move through three execution contexts:

1. Authoring time: write Markdown and frontmatter in `/content`.
2. Publish time: convert content to static HTML and supporting artifacts.
3. Runtime: fetch static HTML and progressively enhance with Blazor components.

Read [Architecture positioning](core/architecture-positioning.md) for the full model.

## Related repository workflows

- todo/home.md - Active tasks, backlog, and completed task flow.

## Project structure

```text
content/                  Markdown source files
todo/                     Task-tracking documents
templates/                Scaffolding templates
tools/                    Publish script and supporting tools
.github/instructions/     Copilot instruction files
src/                      App and component library projects
```

## Getting started

If you are creating a new site from the template, start from [Prerequisites](site-building/prerequisites.md), then ask Copilot to run first-run setup.

[LinkButton href="https://github.com/new?template_name=Blazorade-Scraibe&template_owner=Blazorade" OpenInNewTab="true" btn-success btn-lg my-4]Create your new repository here[/LinkButton]

## About the name

Blazorade Scraibe (/skraib/) combines Blazorade and scribe, with the letters AI embedded in the name to reflect the framework's AI-assisted authoring and publishing workflow.

