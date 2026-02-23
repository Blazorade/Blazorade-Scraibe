# Web App — Application Layer Guidelines

The web app project (at the path specified by `WebAppPath` in `blazorade.config.md`) is the Blazor WASM application. It references the component library (`ComponentLibraryPath`) for all reusable UI components.

## Use component library components

When building pages or layouts in the web app, always check the component library first for existing components before writing inline markup. Prefer composing pages from components in the component library rather than embedding one-off HTML structures directly in `.razor` pages.

If a suitable component does not yet exist in the component library, note this and ask the user whether to create one there before adding inline markup to the web app.

## Separation of concerns

| Belongs in the component library | Belongs in the web app |
|---|---|
| Reusable UI components (buttons, cards, layouts, etc.) | Page components (`@page` directive) |
| Component-scoped CSS (`.razor.css`) | Application routing (`App.razor`) |
| JS interop helpers for UI behaviour | `HttpClient` content loading logic |
| Design system primitives | `staticwebapp.config.json` |

## NuGet dependencies

The web app has one required NuGet dependency beyond the standard Blazor WASM packages:

- **`AngleSharp`** — HTML5 DOM parser used by `ContentSegmentParser` to walk the parsed HTML tree and identify `<x-shortcode>` sentinels that are nested inline within block-level elements. Required for inline shortcode support to work correctly at runtime.

Do not remove this package. If updating it, verify that `ContentSegmentParser` still compiles and that inline shortcodes render correctly.

## Content loading

All content pages in the web app load their content by fetching the corresponding `.html` file from `wwwroot` (e.g. `/about.html`), extracting the `<main>` element via the JS helper in `wwwroot/js/`, and rendering it as a `MarkupString`. Do not duplicate this logic; extend `ContentPage.razor` if new variations are needed.

## Do not modify the component library without explicit permission

You may read the component library freely. Do not write to it unless the user has explicitly requested it in the current conversation.
