---
title: Styling
description: CSS conventions, customisation points, and component-scoped styling for Blazorade Scraibe sites.
keywords: styling, CSS, customise, app.css, component-scoped CSS, design, Bootstrap
changefreq: monthly
priority: 0.7
---

# Styling

Blazorade Scraibe sites use a straightforward CSS layering model. Global styles live in one place, component-scoped styles live next to their components, and everything is overridable.

## Global Stylesheet

The main stylesheet is `{WebAppPath}/wwwroot/css/app.css`. This file is referenced by the page template and applies to every published page. It is the right place for:

- Font and colour definitions
- Global element resets
- Shared utility classes
- Layout and spacing rules that apply site-wide

The default `app.css` ships with a minimal set of styles — base typography, link colours, primary button colours, focus ring styles, and Blazor-specific helpers (`#blazor-error-ui`, `.blazor-error-boundary`, `.loading-progress`). These are safe to modify or extend.

### CSS Framework

The default setup does not prescribe a specific CSS framework. If you want to use Bootstrap, Tailwind, or any other framework, add its CDN link or package reference to the page template at `{WebAppPath}/page-template.html` and import any required CSS there. The `app.css` file is then the right place to add any overrides or site-specific rules on top of the framework's base styles.

## Component-Scoped Styles

Reusable UI components live in the component library at `{ComponentLibraryPath}`. Razor components support **CSS isolation** via co-located `.razor.css` files. To add scoped styles to a component:

1. Create a file with the same name as the component but with a `.razor.css` extension, in the same directory. For example:
   - Component: `{ComponentLibraryPath}/Components/CalloutBox.razor`
   - Scoped CSS: `{ComponentLibraryPath}/Components/CalloutBox.razor.css`

2. Write normal CSS in that file. The .NET build automatically generates a unique attribute selector so the styles only apply to that component's rendered output.

Scoped styles are bundled into `{AppName}.Components.styles.css`, which is referenced by the page template automatically via `{AppName}.styles.css`.

## Page-Specific Styles

If a single page needs custom styles that do not belong in the global stylesheet or a reusable component, you have two options:

- **`ai_instructions` frontmatter** — instruct the publish pipeline to add a `<style>` block to the generated `<main>` section for this page only.
- **A dedicated component** — if the styling is complex or reusable, create a new component in the component library and reference it from the page via a shortcode.

Avoid inline `style` attributes in Markdown content where possible; they are fragile and hard to maintain.

## Customising the Look

The most common customisation points in `app.css` are:

| Selector / Variable | Effect |
|---------------------|--------|
| `html, body { font-family: ... }` | Global typeface |
| `a, .btn-link { color: ... }` | Link colour |
| `.btn-primary { background-color: ... }` | Primary button colour |
| `.content { padding-top: ... }` | Main content area top padding |

## Dark Mode

The default stylesheet does not include a dark mode. To add one, use a `prefers-color-scheme` media query in `app.css`:

```css
@media (prefers-color-scheme: dark) {
    html, body {
        background-color: #1a1a1a;
        color: #e0e0e0;
    }

    a, .btn-link {
        color: #58a6ff;
    }
}
```

## Build Output

The .NET build pipeline automatically bundles and scopes component CSS. The final output written to `wwwroot` is:

- `wwwroot/css/app.css` — your global stylesheet (copied as-is).
- `wwwroot/{AppName}.styles.css` — auto-generated bundle of all component-scoped `.razor.css` files.

Both files are referenced from `page-template.html` and do not need to be managed manually.
