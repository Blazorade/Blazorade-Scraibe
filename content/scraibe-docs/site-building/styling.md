---
title: Styling
description: CSS conventions, customisation points, and component-scoped styling for Blazorade Scraibe sites.
keywords: styling, CSS, customise, app.css, component-scoped CSS, design, Bootstrap
changefreq: monthly
priority: 0.7
---

# Styling

Blazorade Scraibe uses [Bootstrap 5](https://getbootstrap.com/) as its UI framework, compiled from SCSS source so you can customise every design token before the CSS is generated. No Node.js or npm is required — the entire pipeline is driven by `dotnet build`.

## How the CSS pipeline works

Bootstrap's SCSS source files are downloaded by `libman restore` into `{ComponentLibraryPath}/Styles/lib/bootstrap/` at build time. On every `dotnet build`, `AspNetCore.SassCompiler` compiles the entry point `{ComponentLibraryPath}/Styles/app.scss` into `{ComponentLibraryPath}/wwwroot/css/app.css`, which is then served as a static Blazor web asset at:

```
_content/{AppName}.Components/css/app.css
```

This URL is referenced from both `index.html` (the Blazor host page) and `page-template.html` (the static HTML bootstrapper for each published page), so Bootstrap is available at runtime and in the crawler-visible static HTML.

The Bootstrap JS bundle (`bootstrap.bundle.min.js`) is copied to `{ComponentLibraryPath}/wwwroot/js/` at build time and served at `_content/{AppName}.Components/js/bootstrap.bundle.min.js`.

### Files in `{ComponentLibraryPath}/Styles/`

| File | Who edits it | Purpose |
|---|---|---|
| `_variables.scss` | **You** | Bootstrap variable overrides — the only file you need to edit to customise the look |
| `app.scss` | Agent-managed | Entry point: imports `_variables.scss` then Bootstrap; do not edit |
| `lib/` | Downloaded (gitignored) | Bootstrap SCSS source downloaded by `libman restore`; never edit or commit |

## Customising Bootstrap variables

Open `{ComponentLibraryPath}/Styles/_variables.scss` and add your overrides **before** the Bootstrap import (the ordering is handled by `app.scss`). Bootstrap uses `!default` on all its variables, so any value you declare here takes precedence.

Common examples:

```scss
// Colours
$primary:   #0d6efd;
$secondary: #6c757d;
$body-bg:   #ffffff;
$body-color: #212529;

// Typography
$font-family-sans-serif: 'Inter', system-ui, -apple-system, sans-serif;
$font-size-base: 1rem;
$line-height-base: 1.6;

// Spacing
$spacer: 1rem;

// Borders
$border-radius: 0.5rem;
$border-radius-lg: 0.75rem;
```

The full list of Bootstrap variables is in `{ComponentLibraryPath}/Styles/lib/bootstrap/scss/_variables.scss` — browsing that file is the best way to discover what is overridable.

After saving changes to `_variables.scss`, run `dotnet build` to recompile.

## Adding custom SCSS

You can add your own SCSS files to `{ComponentLibraryPath}/Styles/` and import them from `app.scss`. For example, to add a custom mixin file:

```scss
// app.scss (add your import after the _variables import, before or after Bootstrap)
@use "variables";
@use "lib/bootstrap/scss/bootstrap";
@use "mixins";          // your Styles/_mixins.scss
@use "site-overrides";  // your Styles/_site-overrides.scss
```

Note that `AspNetCore.SassCompiler` is configured to compile only `Styles/app.scss` as the entry point; it will pick up anything imported from there.

## Component-scoped styles

Reusable UI components in the component library support **CSS isolation** via co-located `.razor.css` files. To add scoped styles to a component, create a file with the same name but a `.razor.css` extension in the same directory:

- Component: `{ComponentLibraryPath}/ShortCodes/Alert.razor`
- Scoped CSS: `{ComponentLibraryPath}/ShortCodes/Alert.razor.css`

Write normal CSS (not SCSS) in that file. The .NET build generates a unique attribute selector so the styles only apply to that component's output. Scoped styles are bundled automatically and require no manual wiring.

## Blazor infrastructure styles

`{WebAppPath}/wwwroot/css/app.css` is reserved for Blazor runtime UI that has nothing to do with Bootstrap — the error toast (`#blazor-error-ui`), the error boundary panel (`.blazor-error-boundary`), and the loading spinner (`.loading-progress`). Do not add site-specific styles here; put them in `Styles/_variables.scss` or a custom SCSS file instead.

## Dark mode

Bootstrap 5.3+ includes a built-in dark mode mechanism driven by the `data-bs-theme` attribute. To enable it globally, add `data-bs-theme="dark"` to the `<html>` element in `index.html`. To support automatic switching based on the user's OS preference, set it with a small script:

```html
<script>
  document.documentElement.setAttribute(
    'data-bs-theme',
    window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
  );
</script>
```

To customise dark mode colours, override Bootstrap's dark-mode variables in `_variables.scss` — they are all prefixed with `$dark-`.

## Build outputs (gitignored)

The following files are generated by `dotnet build` and are never committed:

| File | Origin |
|---|---|
| `{ComponentLibraryPath}/Styles/lib/` | Downloaded by `libman restore` |
| `{ComponentLibraryPath}/wwwroot/css/app.css` | Compiled by `AspNetCore.SassCompiler` |
| `{ComponentLibraryPath}/wwwroot/css/app.css.map` | CSS source map |
| `{ComponentLibraryPath}/wwwroot/js/bootstrap.bundle.min.js` | Copied by MSBuild target |
