---
title: What Scraibe is and is not
description: Boundaries and non-goals of Blazorade Scraibe to set expectations before implementation.
keywords: product boundaries, non-goals, static publishing, blazor wasm, architecture tradeoffs
changefreq: monthly
priority: 0.8
---

# What Scraibe is and is not

This page sets expectations about Scraibe's intended model.

## What Scraibe is

- A Markdown-first publishing framework for static Blazor WebAssembly sites.
- A publish pipeline that generates crawler-readable static HTML per page.
- A progressive enhancement model where runtime interactivity is layered on top of static content.
- A C# and Razor component ecosystem for reusable interactive shortcodes.
- A workflow designed to be driven through GitHub Copilot instructions and repository automation.

## What Scraibe is not

- Not a traditional server-rendered CMS.
- Not a runtime that requires an always-on app server to render page content.
- Not a JavaScript-first framework requiring Node-based app pipelines for core authoring.
- Not a system where runtime enhancement replaces the need for crawlable static content.
- Not a no-opinion platform: architecture boundaries are explicit and intentional.

## Tradeoff summary

| Boundary | Benefit | Tradeoff |
|---|---|---|
| Static hosting first | Simple and low-cost deployment model | Less aligned with server-rendered dynamic stacks |
| Crawler-readable baseline | Better SEO and AI discoverability | Requires publish-time discipline around content output |
| Blazor WebAssembly runtime | Reusable interactive components in C# | Runtime model differs from SSR-first expectations |
| Shortcode sentinel model | Clear separation between publish and runtime responsibilities | Requires understanding shortcode fallback and enhancement behavior |

## Where to continue

- [Architecture positioning](architecture-positioning.md)
- [Constraints and rationale](constraints-and-rationale.md)
- [Publishing](../operations/publishing.md)
- [Runtime glossary](runtime-glossary.md)
