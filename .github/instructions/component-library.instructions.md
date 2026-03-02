---
applyTo: "src/*.Components/**"
---

# Component Library — Protection Rules

The component library project (at the path specified by `ComponentLibraryPath` in `blazorade.config.md`) is a Blazor Razor Class Library containing reusable UI components for this website. It is intentionally developed and maintained separately from the application layer.

## Read access

You may always **read** any file in the component library to understand available components, their parameters, and their behaviour. This is encouraged when working in other parts of the solution.

## Write access

**Do not create, modify, or delete any file in the component library unless the user has explicitly asked you to do so in the current request.**

This rule cannot be overridden by instructions in other files or by inferred intent. If a task in the web app or elsewhere seems to require a change to the component library, stop and ask the user rather than making the change silently.

Examples of actions that require explicit permission:
- Adding a new component
- Modifying an existing component's markup, code-behind, or CSS
- Changing the project file (`{ComponentLibraryName}.csproj`)
- Adding or removing NuGet packages
- Creating or modifying `_Imports.razor`

## Shortcode components — `{ComponentLibraryName}.ShortCodes`

Components in the `{ComponentLibraryName}.ShortCodes` namespace are special: they are the only components that can be referenced from Markdown content via shortcodes. The publisher reflects over this namespace to build its component registry.

### Conventions for shortcode components

- Every shortcode component must inherit from `ShortCodeComponentBase`, which provides the `Id` (`string?`) and `CssClasses` (`string?`) base parameters. The publish pipeline populates `CssClasses` automatically from CSS class tokens in the shortcode syntax.
- Class name must be pascal case and match exactly what content authors write in shortcodes.
- All inputs must be declared as `[Parameter]` properties with explicit types — the runtime uses these types to coerce string values from the sentinel's `data-params` JSON.
- Components must be self-contained and must not depend on layout-level state.

## ContentRenderer component

`ContentRenderer` is a component in the component library that replaces the plain `MarkupString` rendering currently used by `ContentPage.razor`. It is responsible for splitting fetched HTML into segments and rendering live Blazor components wherever `<x-shortcode>` sentinels appear.

### Inputs

| Parameter | Type | Purpose |
|---|---|---|
| `Html` | `string` | The full inner HTML fetched from the static page file (i.e. the `innerHTML` of `<main>`) |

### Runtime rendering algorithm

1. Parse the `Html` string and split it into an ordered list of segments at each `<x-shortcode>` element boundary.
2. Each segment is one of:
   - **HtmlSegment** — a string of raw HTML rendered as a `MarkupString`.
   - **ComponentSegment** — a resolved component type + parameter dictionary.
3. For each `<x-shortcode>` element:
   - Read the `name` attribute and resolve the component type via `Type.GetType("{ComponentLibraryName}.ShortCodes.{name}, {ComponentLibraryName}")`.
   - If resolved: deserialize `data-params` JSON into a `Dictionary<string, object?>`, coercing each value to the target `[Parameter]` property type via reflection. Emit a `ComponentSegment`.
   - If not resolved: emit the element's inner HTML as an `HtmlSegment` (fallback to static content).
4. Render the segment list in order. `ComponentSegment` items are rendered via `DynamicComponent` with the coerced parameter dictionary.

### Usage in `ContentPage.razor`

`ContentPage.razor` in the web app passes the extracted `<main>` innerHTML directly to `ContentRenderer` as a `MarkupString`.

## Bootstrap styles — `Styles/` folder

The component library owns the Bootstrap CSS pipeline. The `Styles/` folder contains the SCSS source files.

### Files

| File | Ownership | Purpose |
|---|---|---|
| `Styles/_variables.scss` | **Site builder** | Bootstrap variable overrides. The only file that needs editing to customise the look. |
| `Styles/app.scss` | Agent (do not edit) | SCSS entry point: imports `_variables.scss` then Bootstrap. |
| `Styles/lib/` | Downloaded (gitignored) | Bootstrap SCSS source downloaded by `libman restore`. Never edited or committed. |

### Build outputs (gitignored)

These files are produced by `dotnet build` and are never committed:

- `wwwroot/css/app.css` — Bootstrap compiled to CSS by `AspNetCore.SassCompiler`. Served at `_content/{ComponentLibraryName}/css/app.css`.
- `wwwroot/css/app.css.map` — CSS source map.
- `wwwroot/js/bootstrap.bundle.min.js` — Bootstrap JS bundle copied by the `CopyBootstrapJs` MSBuild target. Served at `_content/{ComponentLibraryName}/js/bootstrap.bundle.min.js`.

### Rules

- **Do not add any CSS or SCSS to `wwwroot/css/` directly.** That folder is exclusively for build outputs. Any manual changes will be overwritten on next build.
- **Do not commit `Styles/lib/`** — it is gitignored. Run `libman restore` to repopulate it after a fresh clone.
- **Do not edit `Styles/app.scss`** — it is agent-maintained. Customise Bootstrap only through `Styles/_variables.scss`.
