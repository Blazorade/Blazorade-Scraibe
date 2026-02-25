# Blazorade AI Publisher

A static site generator and publishing framework that combines Blazor WebAssembly with GitHub Copilot-assisted content authoring and publishing. Write your content in Markdown, enhance it with Razor component shortcodes, and publish to SEO-friendly static HTML while preserving the interactive capabilities of Blazor components at runtime.

## What It Does

Blazorade AI Publisher transforms Markdown content into modern, interactive websites:

- **Write in Markdown** — Author content with YAML frontmatter in the `/content` folder
- **GitHub Copilot-Powered** — Use GitHub Copilot to generate, publish, and maintain your content with intelligent assistance
- **Component Shortcodes** — Embed live Blazor components directly in your Markdown using a simple shortcode syntax
- **Hybrid Rendering** — Static HTML for crawlers and bots, dynamic Blazor WASM for users
- **SEO-Optimized** — Generates sitemap, meta tags, Open Graph tags, and canonical URLs automatically
- **Zero Backend** — Deploy to Azure Static Web Apps, GitHub Pages, or any static hosting service

## Key Features

- **First-Run Setup** — Automated project scaffolding creates the complete site structure from templates
- **Smart Instructions** — Context-aware instructions guide GitHub Copilot through content authoring and publishing workflows
- **Template-Based** — All code files are maintained as templates with token substitution for multi-site reuse
- **Component Library** — Build reusable shortcode components in a separate Razor Class Library
- **Navigation Generation** — Top navbar with dropdowns is automatically generated from content structure

## Project Structure

```
├── content/                    # Markdown source files with frontmatter
├── templates/                  # Reusable templates for new projects
│   ├── component-library/      # Razor component templates
│   └── web-app/               # Blazor WASM app templates
├── .github/
│   └── instructions/          # GitHub Copilot instruction files
└── src/                       # Generated during first-run (not in repo)
    ├── {AppName}.Components/  # Razor Class Library for shortcodes
    └── {AppName}.Web/         # Blazor WebAssembly application
```

## Getting Started

This repository serves as both the framework and a template for new sites. When you start working in a clone or fork of this repo, GitHub Copilot will detect the missing configuration and guide you through an automated first-run setup that:

1. Collects your site identity (display name, app name, hostname)
2. Creates the Blazor projects under `/src`
3. Copies and configures all template files with your site's values
4. Sets up the instruction bridges for scoped GitHub Copilot guidance
5. Prepares the `/content` folder for your pages

After setup, simply add Markdown files to `/content` and use the GitHub Copilot-powered publishing workflow to generate static HTML.

## Documentation

Detailed documentation for using Blazorade AI Publisher, including content authoring guidelines, shortcode creation, and publishing workflows, is maintained in `/content/blazorade-docs/`. This documentation serves as both:

- A complete reference for authors using the framework
- Example content demonstrating best practices for Markdown authoring with Blazorade AI Publisher

Authors can choose to include this documentation in their published site or use it purely as a reference.

## License


See [LICENSE](LICENSE) for details.
