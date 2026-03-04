---
title: Hosting
description: How and where to host a site built with Blazorade Scraibe, and why your choice of host affects SEO.
keywords: hosting, Azure Static Web Apps, SWA, static hosting, SEO, clean URLs, URL rewriting
changefreq: monthly
priority: 0.7
---

# Hosting

A Blazorade Scraibe site is a Blazor WebAssembly application that serves pre-generated static HTML pages. Because there is no app server or runtime, it can run on any host that serves static files — but not all hosts deliver the same experience to search engines and bots.

## Why hosting matters for SEO

Blazorade Scraibe generates a static HTML file for every content page (e.g. `scraibe-docs/mermaid.html`) and a `staticwebapp.config.json` that rewrites clean URLs to those files:

```
/scraibe-docs/mermaid  →  scraibe-docs/mermaid.html
```

On a host that supports routing rules, a crawler following a link to `/scraibe-docs/mermaid` receives the full HTML page — title, description, body text — exactly what it needs to index the content correctly.

On a host that does **not** support routing rules, every URL falls through to `index.html`, the Blazor application shell. The crawler receives an empty shell with no page-specific content. The page will eventually render in a browser, but most crawlers do not execute JavaScript, so the content remains invisible to them.

This is the same experience as running the app locally in the Visual Studio dev server — everything works for human visitors, but bots see only the shell.

## Azure Static Web Apps (recommended)

Blazorade Scraibe is built and tested against **[Azure Static Web Apps](https://azure.microsoft.com/products/app-service/static)**. It natively reads `staticwebapp.config.json`, applying per-page URL rewrite rules automatically. The free tier includes:

- Custom domains with automatic HTTPS
- Clean-URL routing via `staticwebapp.config.json`
- Global CDN distribution

Authentication on the free tier is limited to the built-in identity providers. If you need custom authentication flows (e.g. Azure AD B2C, your own OpenID Connect provider), [Blazorade ID](https://github.com/Blazorade/Blazorade-Id) works with Blazor WebAssembly and requires no server-side component.

[LinkButton href="https://azure.microsoft.com/products/app-service/static" openinnewtab="true" btn-secondary]Learn more about Azure Static Web Apps[/LinkButton]

## Other platforms

Other platforms with routing rule or redirect support — such as Netlify, Vercel, and Cloudflare Pages — likely work equally well, though they have not been tested with Blazorade Scraibe. Each has its own configuration file format; you would need to translate the rewrite rules from `staticwebapp.config.json` into the equivalent format for your chosen platform.

Any host **without** routing rule support still serves the site correctly to human visitors. You simply lose the SEO benefit of pre-rendered per-page HTML for crawlers.
