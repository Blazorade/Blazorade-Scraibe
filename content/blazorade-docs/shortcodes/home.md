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
- The opening tag can contain named parameters and hint tokens (see below); closing tags carry nothing.
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

## Named Parameters

Named parameters use `Key=value` or `Key="value"` syntax and map to `[Parameter]` properties on the component. Parameter names are matched case-insensitively, so `[Alert cssclasses="alert-danger" /]` works the same as `[Alert CssClasses="alert-danger" /]`. Duplicate names (after case normalisation) are fatal errors.

## Hint Tokens

Any token in the opening tag that is not a `Key=value` pair is a **hint token**. Bare words and quoted strings are both valid:

```
[Alert error /]
[Alert "error" /]
[Alert error dismissible /]
[Alert "error dismissible" /]
```

All four are equivalent. The publish pipeline collects hint tokens in order, joins them with a space, and uses the component's `[AgentInstructions]` attribute to translate the result into CSS class names, which are set as the `CssClasses` parameter. If the component has no `[AgentInstructions]` attribute, hint tokens produce a warning and are discarded.

Hint tokens and named parameters can be freely mixed:

```
[Carousel round Interval=3000 /]
```

Here `round` is a hint token and `Interval=3000` is a named parameter.

## Validation Rules

- Duplicate named parameters in the same tag are fatal errors.
- A `[...]` token that does not match shortcode syntax is left as plain text.

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
- Fenced code blocks (with or without a language hint) and indented code blocks: shortcode-like text is not parsed and remains verbatim.

## Tips

- Prefer the inline form for very short child content; use multi-line for anything longer or nested.
- Reserve `home.md` files for directory landing pages; avoid naming conflicts between files and folders with the same stem.
- If a shortcode name is not recognised, the text passes through unchanged, making authoring failures obvious during preview.
