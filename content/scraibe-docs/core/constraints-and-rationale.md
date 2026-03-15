---
title: Constraints and rationale
description: Plain-language explanation of Scraibe's core architecture constraints and why they exist.
keywords: constraints, architecture, rationale, static hosting, blazor wasm, crawler visibility
changefreq: monthly
priority: 0.8
---

# Constraints and rationale

This page explains the core architecture constraints in plain language.

These are intentional design boundaries, not temporary limitations.

## Constraint 1: Static hosting first

Scraibe is designed for static hosting. Publish produces static artifacts that can be served directly.

Rationale:

- Reduces hosting complexity.
- Keeps costs low.
- Improves portability across static host providers.

## Constraint 2: Blazor WebAssembly runtime

Scraibe uses a Blazor WebAssembly runtime model rather than a persistent server-rendered runtime.

Rationale:

- Keeps runtime architecture consistent with static hosting.
- Enables reusable interactive components without introducing a server dependency.
- Preserves the framework's C#-first development model.

## Constraint 3: Crawler-readable baseline

Published page content must remain visible to crawlers and AI bots without JavaScript execution.

Rationale:

- SEO quality depends on crawlable page content.
- AI indexing and retrieval also depend on static, readable outputs.
- Content should remain accessible even when runtime enhancement does not execute.

## Constraint 4: Shortcode sentinel contract

Shortcodes are represented as `<x-shortcode>` sentinel elements in published HTML and resolved at runtime.

Rationale:

- Preserves static HTML context for crawlers.
- Allows runtime enhancement to be deterministic.
- Keeps publish/runtime responsibilities separate and testable.

For detailed shortcode behavior, see [Shortcodes](../authoring/shortcodes/home.md).

## Design consequences

These constraints lead to predictable tradeoffs:

- Strength: strong crawler visibility and simple hosting model.
- Strength: reusable interactive components in a static site context.
- Tradeoff: architecture differs from server-side rendering pipelines.
- Tradeoff: hosts without routing support reduce clean-URL and SEO benefits.

See [Hosting](../site-building/hosting.md) for host-specific implications.
