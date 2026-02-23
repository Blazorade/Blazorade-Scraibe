# First-Run Setup Instructions

These instructions are loaded **only** when `blazorade.config.md` does not exist at the repository root. Once the first-run process is complete and `blazorade.config.md` has been written, these instructions must never be loaded again unless `blazorade.config.md` is explicitly deleted.

**Never overwrite or replace any file that already exists. Only add new files.**

## Agent behaviour during first-run

The first-run process is mandatory and must be completed in full before the site can be used. When these instructions are active, the agent must:

- **Stay focused.** Do not perform any work outside of the first-run steps below, regardless of what the user asks. If the user attempts to side-step the process or ask about unrelated topics, politely redirect them: explain that the site setup must be completed first and that the current step requires their input.
- **Never skip steps.** Each step must complete successfully before the next one begins.
- **Stop on failure.** If any step fails, stop immediately, report what went wrong, and ask the user how to proceed. Do not continue to the next step and do not write `blazorade.config.md`.
- **Write `blazorade.config.md` last.** This file is written only after all previous steps have completed successfully. It is the completion marker — if it does not exist, the entire first-run process will re-run next time. This is intentional: it ensures a failed or incomplete setup is always detected and resumed.

## Overview

The first-run process does the following:

1. Collect site identity from the user.
2. Create the Razor component library under `src/`.
3. Create the Blazor WebAssembly application under `src/`, with a project reference to the component library.
4. Copy and configure the template files from `/templates/` into the new projects.
5. Generate the scoped instruction bridge files in `.github/instructions/`.
6. Set up the `content/` folder with sample pages.
7. Write `blazorade.config.md` to the repository root — **only after all previous steps have succeeded**.

## Step 1 — Collect site identity

Ask the user for the following values before doing anything else. Do not proceed until all three are explicitly confirmed by the user — do not assume or infer values without confirmation.

- **DisplayName** — the human-readable name of the site. Used in the navbar brand, page titles, and meta tags. Example: `My Awesome Site`.
- **AppName** — the technical/code-safe name. Used for project names, namespaces, and folder names. Must be a valid C# identifier (no spaces or special characters). Example: `MyAwesomeSite`. Suggest a derived value from `DisplayName` for the user to confirm.
- **HostName** — the hostname where the site will be published. Used for canonical URLs and the sitemap. Example: `www.mysite.com`.

Do not proceed to step 2 until all three values are explicitly confirmed by the user.

## Step 2 — Create the Razor component library

Create a new Razor Class Library project at `src/{AppName}.Components`.

- Project name: `{AppName}.Components`
- Root namespace: `{AppName}.Components`
- Framework: latest available .NET version

After creating the project, create a `ShortCodes/` folder inside `src/{AppName}.Components/`. This is the designated location for all shortcode components.

Then add the following NuGet packages to the component library project. **Before adding each package, check NuGet for the latest stable published version** (no prerelease) and use that version number. The versions shown below were current at the time these instructions were written and are provided as a fallback only.

```
dotnet add src/{AppName}.Components/{AppName}.Components.csproj package Blazorade.Core --version 4.0.0
dotnet add src/{AppName}.Components/{AppName}.Components.csproj package Blazorade.Mermaid --version 2.0.2
```

## Step 3 — Create the Blazor WebAssembly application

Create a new Blazor WebAssembly project at `src/{AppName}.Web`.

- Project name: `{AppName}.Web`
- Root namespace: `{AppName}.Web`
- Framework: same .NET version as the component library
- Template: Blazor WebAssembly standalone app (no ASP.NET Core host)

Wire up the following:
- Add a project reference from `{AppName}.Web` to `{AppName}.Components`.
- Ensure the solution file (if one exists) includes both projects.

Then add the following NuGet package to the web app project. **Before adding it, check NuGet for the latest stable published version** (no prerelease) and use that version number. The version shown below was current at the time these instructions were written and is provided as a fallback only.

```
dotnet add src/{AppName}.Web/{AppName}.Web.csproj package AngleSharp --version 1.2.0
```

`AngleSharp` is required by `ContentSegmentParser` — it provides the HTML5 DOM parser used to detect inline `<x-shortcode>` sentinels inside block-level elements at runtime.

## Step 4 — Copy and configure template files

Copy files from `/templates/web-app/` into `src/{AppName}.Web/` and from `/templates/component-library/` into `src/{AppName}.Components/`. While copying, substitute all `{{TokenName}}` tokens in **file contents** (not filenames):

- `{{WebAppName}}` → `{AppName}.Web`
- `{{ComponentLibraryName}}` → `{AppName}.Components`

Files to copy from `/templates/web-app/` → `src/{AppName}.Web/`:
- `Content/ContentNode.cs`
- `Content/ContentSegmentParser.cs`
- `Components/ContentRenderer.razor`
- `Utilities/PageContentUtilities.cs`
- `Pages/ContentPage.razor`
- `_Imports.razor` — **overwrite** the default `_Imports.razor` generated by `dotnet new`
- `wwwroot/js/blazorade-publisher.js`

Files to copy from `/templates/component-library/` → `src/{AppName}.Components/`:
- `_Imports.razor`
- `ShortCodes/**` — copy all files from `templates/component-library/ShortCodes/` into `src/{AppName}.Components/ShortCodes/`, substituting `{{ComponentLibraryName}}` tokens in file contents

After copying, build the solution to verify everything compiles before continuing.

## Step 5 — Generate scoped instruction bridge files

Create the following two files in `.github/instructions/`. These files have the correct `applyTo` glob pattern for this site and link through to the generic instruction files that contain the full content.

**`.github/instructions/{AppName}-component-library.instructions.md`:**
```markdown
---
applyTo: "src/{AppName}.Components/**"
---

For instructions on working with this project, see [component-library.instructions.md](component-library.instructions.md).
```

**`.github/instructions/{AppName}-web-app.instructions.md`:**
```markdown
---
applyTo: "src/{AppName}.Web/**"
---

For instructions on working with this project, see [web-app.instructions.md](web-app.instructions.md).
```

## Step 6 — Verify the `content/` folder

Check that a `content/` folder exists at the repository root. If it does not exist, create it. Do not create or modify any files inside it.

## Step 7 — Write `blazorade.config.md`

**Only execute this step if all previous steps have completed successfully.** If any earlier step failed or was skipped, do not write this file.

Write the following file to the repository root. Substitute the actual values collected in step 1:

```markdown
# Site Configuration

This file stores the configuration for this site. It is read by the AI agent to understand the identity and structure of the project. Its presence also signals that the first-run setup has already been completed.

- `DisplayName`: {DisplayName}
- `AppName`: {AppName}
- `HostName`: {HostName}
- `WebAppPath`: src/{AppName}.Web
- `ComponentLibraryPath`: src/{AppName}.Components
```

Writing this file marks the first-run process as complete. These instructions must not be followed again unless this file is deleted.
