---
title: Part Shortcode
description: Reference for the Part shortcode — how to define named content parts inline in a page body and map them to layout slots.
keywords: Part, shortcode, content parts, layout slots, x-part, named parts
changefreq: monthly
priority: 0.7
---

# Part Shortcode

The `Part` shortcode defines a named content part inline in a page body. The block is fully extracted from the primary content flow and serialised as a sibling element in the generated HTML, where it can be spliced into the matching `x-part` slot of the page layout at runtime.

## Syntax

`[Part]` is a wrapping shortcode only — it cannot be used in self-closing form.

```
[Part Name="part-name"]
Content for this part goes here.
[/Part]
```

Opening and closing tags may be adjacent to other shortcode tags on the same line when balanced. For example:

```
[Part Name="hero"][Carousel][Slide][Heading Display="1"]Blazorade[/Heading][/Slide][/Carousel][/Part]
```

Leading indentation before shortcode tags is allowed.

The entire block is removed from the primary page body. No placeholder or sentinel remains in its place.

## Parameters

### `Name` (required)

The part name. This value is used as the `x-part` attribute on the generated HTML wrapper element and must match the `x-part` slot name declared in the page layout.

```
[Part Name="sidebar"]
...
[/Part]
```

The name is normalised to lowercase. The element name for the HTML wrapper is derived from the name using the standard element name convention:

| `Name` value | HTML element |
|---|---|
| `header` | `<header>` |
| `nav` | `<nav>` |
| `main` | `<main>` |
| `footer` | `<footer>` |
| anything else | `<aside>` |

### `ElementName` (optional)

Overrides the element name convention above. Use this when you want a specific HTML element for the generated wrapper regardless of the part name.

```
[Part Name="callout" ElementName="section"]
This wraps in a <section> instead of <aside>.
[/Part]
```

## Usage Notes

- `[Part]` is only valid at the **root level** of a page body. Nesting it inside another shortcode is a fatal publish error.
- If the same part name is defined more than once in a page (whether through `[Part]` blocks or `_name.md` files), the publish run fails with an error.
- If a part has no matching slot in the layout, it is still present in the static HTML for crawlers but is silently ignored during runtime composition.
- Wrapping content inside `[Part]` is dedented before Markdown conversion, so indentation used for readability does not accidentally create blockquotes or code blocks.
- To intentionally render a blockquote in a part, use `>` explicitly.

## Example

A page using a two-column layout with a sidebar part:

```yaml
---
title: Widget Documentation
layout: two-column
---
```

```
# Widget Documentation

Main content goes here.

[Part Name="sidebar"]
## On This Page

- [Overview](#widget-documentation)
- [Configuration](#configuration)
- [Examples](#examples)
[/Part]

## Configuration

...
```

The sidebar content is extracted at publish time and placed in the `<aside hidden x-part="sidebar">` element in the generated HTML. At runtime, the layout's `x-part="sidebar"` slot is filled with this content.

## See Also

- [Page Layouts](../page-layouts.md) — how layouts and content parts work together.
- [Content Authoring](../content-authoring.md) — the full frontmatter reference.
