---
title: Folder configuration
description: How to use .config.json files for folder-level settings, inheritance, and publish behavior in Blazorade Scraibe.
keywords: folder configuration, .config.json, scoped settings, local settings, inheritance, scraibe.layout.default
changefreq: monthly
priority: 0.7
---

# Folder configuration

Blazorade Scraibe supports folder-level `.config.json` files so you can define machine-readable settings that control publish-time behavior. Their effects are visible at runtime through generated output files, but `.config.json` itself is not read by the running site. Configuration files are metadata, not content, so they are read by the pipeline but never published as pages.

For architecture context on publish versus runtime responsibilities, see [Architecture positioning](../core/architecture-positioning.md) and [Runtime glossary](../core/runtime-glossary.md).

## What `.config.json` is used for

A `.config.json` file can define site-level and folder-level settings, such as default layout and excluded content paths. The key behavior is inheritance: settings can apply to only one folder or flow to descendants.

For page-by-page behavior (layout and navigation), effective values are resolved from repository root to the page folder. For publish-run behavior (`scraibe.publish.excludedContent`), values are resolved from the repository-root `.config.json` used by the publish entry script.

Configuration files are never emitted as website artifacts:

- They do not generate HTML pages.
- They are not copied as static assets.
- They are not treated as alternate Markdown content.

## File format

A `.config.json` file is a single JSON object with two optional objects:

- `local`: applies only to the folder where the file exists.
- `scoped`: applies to the folder and all descendant folders.

```json
{
  "local": {
    "setting1": "value1"
  },
  "scoped": {
    "setting2": "value2"
  }
}
```

Rules:

- `local` and `scoped` are optional.
- Empty config files are valid if the root is an object, such as `{}`.
- If present, `local` and `scoped` must be JSON objects.
- The same key cannot appear in both `local` and `scoped` within one file.
- Values can be any valid JSON value.

## Inheritance and precedence

When resolving effective settings for a page or folder, the pipeline walks from repository root to the target folder.

At each folder level:

1. Apply that folder's `scoped` keys.
2. After walking the full chain, apply `local` from the closest folder that has a `.config.json` file.

Nearest definition wins by key.

Practical consequences:

- Child `scoped` overrides parent `scoped`.
- Child `local` overrides inherited `scoped` for that folder.
- If a folder has no `.config.json`, the nearest parent `.config.json` provides the effective `local` layer.
- `local` in one folder does not block descendant `scoped` evaluation.

This means you do not need a `.config.json` in every folder for settings to apply.

## Example

Root content config:

```json
{
  "local": {
    "nav_mode": "compact-root"
  },
  "scoped": {
    "page_layout": "docs",
    "nav_mode": "full"
  }
}
```

Nested docs config:

```json
{
  "local": {
    "nav_mode": "compact"
  }
}
```

Effective values:

- Files directly in `/content/`: `page_layout=docs`, `nav_mode=compact-root`
- Files directly in `/content/scraibe-docs/`: `page_layout=docs`, `nav_mode=compact`
- Files in `/content/scraibe-docs/authoring/shortcodes/`: `page_layout=docs`, `nav_mode=full`

If `/tools` has no `.config.json`, the repository-root `.config.json` still applies there: all inherited `scoped` keys apply, and the root `local` keys are treated as the effective local layer for `/tools`.

## Common framework keys

These keys are currently used by the framework:

- `scraibe.site.displayName`: Human-readable site name used in generated UI and metadata. This value is used by generated navigation branding and other site identity output.
- `scraibe.site.appName`: Technical app identifier used for project and namespace identity.
- `scraibe.site.hostName`: Host name used for canonical URLs and sitemap `<loc>` generation. See [publishing](../operations/publishing.md).
- `scraibe.site.webAppPath`: Repository-relative path to the Blazor WebAssembly web app project.
- `scraibe.site.componentLibraryPath`: Repository-relative path to the component library project where shortcodes, layouts, and styling assets live.
- `scraibe.layout.default`: Default layout name when page frontmatter does not define `layout`. See [page layouts](../site-building/page-layouts.md) and [content authoring](./content-authoring.md).
- `scraibe.publish.excludedContent`: Array of content-relative paths that publish skips entirely. See [publishing](../operations/publishing.md).
- `scraibe.navigation.provider.default`: Default navigation provider name when the active layout does not set `x-provider` on its `x-slot="nav"` slot. See [page layouts](../site-building/page-layouts.md) and [publishing](../operations/publishing.md).
- `scraibe.navigation.includedSchemaTypes`: Array of schema type strings that are included in default navigation. If omitted, the effective default is `['WebPage']`.
- `scraibe.content.slot.provider.default`: Default slot content provider name when a non-navigation layout slot does not set `x-provider`. If both are missing, publish fails for that slot.
- `scraibe.navigation.children.depth`: Number of descendant folder levels to include under navigation item children. `0` means no descendant folder expansion, `1` includes one level, and larger values include deeper nesting. If omitted or invalid, the effective default is `1`. Negative values are clamped to `0`.
- `scraibe.navigation.context.pinned`: Enables pinned navigation context from the folder where it is set. Supports boolean values (`true`/`false`) and boolean strings (`"true"`/`"false"`, case-insensitive).

Recommended placement at repository root:

- Put `scraibe.site.webAppPath`, `scraibe.site.componentLibraryPath`, and `scraibe.publish.excludedContent` in `local`.
- Put shared identity/layout defaults (`scraibe.site.displayName`, `scraibe.site.appName`, `scraibe.site.hostName`, `scraibe.layout.default`, `scraibe.navigation.provider.default`, `scraibe.content.slot.provider.default`) in `scoped`.

If frontmatter `layout` is missing on a page, publish resolves layout from `scraibe.layout.default`. If neither exists, publish fails with a clear error.

## Navigation configuration keys

Navigation behavior is controlled by three primary keys that can be used together:

- `scraibe.navigation.provider.default`: selects the navigation provider to render markup when the layout does not explicitly pick one.
- `scraibe.navigation.includedSchemaTypes`: selects which page `schema_type` values are included in default navigation output.
- `scraibe.navigation.children.depth`: controls how many descendant folder levels are included in navigation item trees.
- `scraibe.navigation.context.pinned`: controls whether descendant pages keep using an ancestor folder as their navigation context.

`scraibe.navigation.includedSchemaTypes` is matched case-insensitively. Any string value is allowed for navigation filtering. A page whose effective `schema_type` is not present in this array is excluded from default navigation.

In practice, use these keys as a set: choose the provider, choose how deep child links should go, then choose whether folder context should stay anchored. Provider rendering details still belong to the selected navigation provider implementation.

For layout slot behavior and provider selection context, see [page layouts](../site-building/page-layouts.md). For publish output implications, see [publishing](../operations/publishing.md).

## Pinned navigation context

Use `scraibe.navigation.context.pinned` to control whether a folder becomes the navigation context root for descendant pages.

- `true`: Navigation context becomes sticky at the folder where the effective `true` value comes from.
- `false`: Explicitly breaks inherited sticky context for that branch.

In practice, sticky means pages can keep using an ancestor folder as their navigation context even when the page itself is deeper in the tree. Instead of switching context at every subfolder boundary, navigation stays anchored to the sticky folder until another sticky decision overrides it.

When a lower folder sets `scraibe.navigation.context.pinned` to `false`, it stops inheriting pinned context behavior from higher folders for that branch. From that point downward, navigation context falls back to the normal per-folder behavior unless that branch sets `scraibe.navigation.context.pinned` to `true` again.

`local` versus `scoped` semantics follow normal configuration inheritance rules, with one important detail for pinned context:

- `scoped` applies to the folder and descendants until overridden.
- `local` from the nearest `.config.json` remains the effective local layer for the target folder resolution.
- If a folder has a `.config.json` but no `local.scraibe.navigation.context.pinned`, pinned behavior for that branch falls back to inherited `scoped` values.

Example:

- `/content/.config.json` sets `scoped.scraibe.navigation.context.pinned = true`.
- `/content/docs/getting-started/intro.md` uses `/content` as navigation context because pinned context is inherited.
- `/content/docs/.config.json` sets `local.scraibe.navigation.context.pinned = false`.
- `/content/docs/getting-started/intro.md` now stops inheriting pinned context from `/content` and uses normal folder context rules under `/content/docs`.
- `/content/docs/api/.config.json` can set `scoped.scraibe.navigation.context.pinned = true` to start a new pinned context for `/content/docs/api` and its descendants.

The publish pipeline only builds the navigation model context. Rendering details, including whether an up-link is shown, remain the responsibility of the selected navigation provider.

## Troubleshooting

Typical validation failures are:

- Invalid JSON syntax in `.config.json`.
- `.config.json` root is not an object.
- `local` exists but is not an object.
- `scoped` exists but is not an object.
- A key appears in both `local` and `scoped` in the same file.

The publish pipeline reports file-path-based errors to help you locate and fix invalid configuration quickly.
