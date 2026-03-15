---
title: Hosting
description: Hosting model for Blazorade Scraibe, focused on Azure Static Web Apps.
keywords: hosting, Azure Static Web Apps, SWA, static hosting, Blazor WebAssembly, SSR
changefreq: monthly
priority: 0.7
---

# Hosting

A Blazorade Scraibe site is a Blazor WebAssembly application that serves pre-generated static HTML pages.

If you have not read the core model yet, start with [Architecture positioning](../core/architecture-positioning.md) and [Constraints and rationale](../core/constraints-and-rationale.md).

Scraibe is primarily targeted at Azure Static Web Apps, and that is the hosting model documented here.

Scraibe was originally created to address the SEO limitation commonly seen in Blazor WebAssembly applications. The same limitation does not apply to Blazor Server applications that use [server-side rendering (SSR)](../core/runtime-glossary.md#server-side-rendering-ssr).

## Azure Static Web Apps

Blazorade Scraibe generates a static HTML file for every content page (e.g. `scraibe-docs/authoring/mermaid.html`) and a `staticwebapp.config.json` that rewrites clean URLs to those files:

```
/scraibe-docs/authoring/mermaid  →  scraibe-docs/authoring/mermaid.html
```

Blazorade Scraibe is built and tested against **[Azure Static Web Apps](https://azure.microsoft.com/products/app-service/static)**. It natively reads `staticwebapp.config.json`, applies clean-URL rewrites, and aligns with Scraibe's static-first publishing model.

The free tier includes:

- Custom domains with automatic HTTPS
- Clean-URL routing via `staticwebapp.config.json`
- Global CDN distribution

Authentication on the free tier is limited to the built-in identity providers. If you need custom authentication flows (e.g. Azure AD B2C, your own OpenID Connect provider), [Blazorade ID](https://github.com/Blazorade/Blazorade-Id) works with Blazor WebAssembly and requires no server-side component.

[LinkButton href="https://azure.microsoft.com/products/app-service/static" openinnewtab="true" btn-secondary]Learn more about Azure Static Web Apps[/LinkButton]

For how publish produces these route mappings, see [Publishing](../operations/publishing.md).
