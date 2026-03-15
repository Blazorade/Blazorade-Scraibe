---
title: Shortcodes
description: How to embed live Blazor components in Markdown using Blazorade shortcodes.
keywords: Blazorade, shortcodes, Blazor, Markdown, components
changefreq: weekly
priority: 0.8
---

# Shortcodes

## What Are Shortcodes?

Shortcodes let you drop fully fledged Blazor components into Markdown using a bracketed syntax. They compile to `<x-shortcode>` sentinels at publish time and are replaced by live components at runtime, while the surrounding HTML stays crawlable.

If you have used WordPress shortcodes, these follow the same `[Name Param="value"]...[/Name]` pattern, so the syntax will feel familiar. For more background, see the [WordPress shortcode reference](https://codex.wordpress.org/Shortcode).

For runtime terminology used on this page, see [Runtime glossary](../../core/runtime-glossary.md).

## When to use shortcodes

Use shortcodes when you need reusable interactive behavior or component-level presentation logic.

Prefer plain Markdown when static text structure is enough.

Use this decision guide:

- Use Markdown for headings, paragraphs, lists, tables, and links that do not require component logic.
- Use shortcodes for interactive UI, parameterized reusable blocks, or content that should be rendered by a Razor component.
- Avoid wrapping plain prose in shortcodes when no component behavior is needed.

## Anatomy of a Shortcode

Use the self-closing form when no child content is needed:

```
[Badge pill info /]
```

Or the inline wrapping form when the component renders child content:

```
[Badge pill info]Beta[/Badge]
```

Key points:
- Component name is pascal case and must match a component in the `{ComponentLibraryName}.ShortCodes` namespace.
- The opening tag can contain named parameters and CSS class tokens (see below); closing tags carry nothing.
- String parameter values are quoted; booleans and numbers are unquoted.

## Wrapping Shortcodes

Use wrapping syntax when the component renders child content.

**Inline wrapping** — opening tag, inner text, and closing tag on one line. Only plain text and inline Markdown are allowed inside:

```
[Callout Tone="info"]Inline child text[/Callout]
```

**Multi-line wrapping** — opening and closing tags on their own lines; inner content can mix Markdown and nested shortcodes:

```
[Article Title="Getting Started"]
## Welcome
Here is some intro text.

[ContactButton Label="Email Us" /]
[/Article]
```

Tags are token-based, so they can also be placed back-to-back on one line when balanced:

```
[Carousel][Slide][Heading Display="2"]One-liner[/Heading][/Slide][/Carousel]
```

Leading indentation before shortcode tags is allowed and does not prevent shortcode detection.

## Named Parameters

Named parameters use `Key=value` or `Key="value"` syntax and map to `[Parameter]` properties on the component. Parameter names are matched case-insensitively, so `[Alert cssclasses="alert-danger" /]` works the same as `[Alert CssClasses="alert-danger" /]`. Duplicate names (after case normalisation) are fatal errors.

## CSS Class Tokens

Any token in the opening tag that is not a `Key=value` pair is a **CSS class token**. Bare words and quoted strings are both valid:

```
[Alert alert-danger /]
[Alert "alert-danger" /]
[Alert alert-danger alert-dismissible /]
[Alert "alert-danger alert-dismissible" /]
```

All four are equivalent. The publish pipeline collects the tokens in order, joins them with a space, and stores the result directly as the `CssClasses` parameter. Content authors write the actual CSS class names (e.g. Bootstrap utility classes) as CSS class tokens.

CSS class tokens and named parameters can be freely mixed:

```
[Carousel rounded Interval=3000 /]
```

Here `rounded` is a CSS class token (becomes `CssClasses="rounded"`) and `Interval=3000` is a named parameter.

## Validation Rules

- Duplicate named parameters in the same tag are fatal errors.
- A `[...]` token that does not match shortcode syntax is left as plain text.

## Indentation and markdown behavior

Wrapping shortcode inner content is dedented before Markdown conversion. This means visual indentation is treated as authoring layout, not as implicit Markdown block syntax.

- If you want a blockquote, write `>` explicitly.
- If you want code blocks, prefer fenced code blocks.

Example:

```
[Heading]
	Indented Heading
[/Heading]
```

The indented text above is treated as normal heading content, not as a blockquote.

To intentionally create a blockquote inside the shortcode:

```
[Heading]
> Indented Heading
[/Heading]
```

## Nesting and Child Content

Shortcodes can nest to any depth in multi-line form. For example:

```
[Carousel]
[Slide Title="First"]
## Slide 1
Intro text.
[/Slide]
[Slide Title="Second"]
## Slide 2
More detail.
[/Slide]
[/Carousel]
```

## Keeping Shortcodes Literal in Code

Shortcode detection is skipped inside code contexts so you can document examples safely:
- Inline code spans: `[Badge ui="pill"]` stays literal when wrapped in single backticks.
- Fenced code blocks (with or without a language hint): shortcode-like text is not parsed and remains verbatim.

## Sentinel and fallback behavior

At publish time, matched shortcodes are converted into `<x-shortcode>` sentinel elements. The static HTML remains crawler-readable, and runtime enhancement resolves each sentinel into a live component when the app executes.

If a shortcode component cannot be resolved at runtime, the static content path remains the baseline for crawler readability and graceful degradation.

## Tips

- Prefer the inline form for very short child content; use multi-line for anything longer or nested.
- Reserve `home.md` files for directory landing pages; avoid naming conflicts between files and folders with the same stem.
- If a shortcode name is not recognised, the text passes through unchanged, making authoring failures obvious during preview.
