---
title: Runtime glossary
description: Definitions for key terms used in Scraibe authoring, publishing, and runtime documentation.
keywords: glossary, runtime, publishing, shortcode sentinel, static html bootstrapper
changefreq: monthly
priority: 0.7
---

# Runtime glossary

This glossary defines the core terms used across Scraibe documentation.

## Authoring time

The phase where content is created and edited in Markdown source files under `/content`.

## Publish time

The phase where the publisher processes Markdown, metadata, shortcodes, and layout composition into static output files.

## Runtime

The phase where the browser loads the Blazor WebAssembly app and page content is rendered for visitors.

## Server-side rendering (SSR)

Server-side rendering (SSR) means the server renders HTML for a request before sending the response to the browser.

For Microsoft's definition and rendering-mode details in Blazor, see [ASP.NET Core Blazor render modes](https://learn.microsoft.com/aspnet/core/blazor/components/render-modes).

## Static HTML bootstrapper

A generated `.html` page file in `wwwroot` that contains crawler-readable content and metadata for a specific source Markdown page.

## Shortcode

A bracket-based content syntax that references a Blazor component, for example `[Alert Type="warning" /]`.

## x-shortcode sentinel

A published HTML element (`<x-shortcode>`) that represents a shortcode region. Runtime enhancement resolves it into a live Blazor component.

## Static fallback content

The static HTML content available in published output so crawlers and non-enhanced contexts can still read page content.

## Progressive enhancement

A rendering model where static content works first and runtime interactivity is layered on top.

## Layout part

A named content fragment (for example `nav`, `main`, `footer`) inserted into matching layout slots.

## Clean URL

A route without `.html` in the address bar, such as `/scraibe-docs/publishing`.

## Canonical URL

The preferred URL recorded in page metadata to represent the authoritative address of a page.
