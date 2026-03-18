# First-Run Setup Instructions

These instructions are loaded **only** when `.config.json` does not exist at the repository root and there is no legacy `blazorade.config.md` to upgrade. Once the first-run process is complete and `.config.json` has been written, these instructions must never be loaded again unless `.config.json` is explicitly deleted.

**Never overwrite or replace any file that already exists. Only add new files.**

## Agent behaviour during first-run

The first-run process is mandatory and must be completed in full before the site can be used. When these instructions are active, the agent must:

- **Stay focused.** Do not perform any work outside of the first-run steps below, regardless of what the user asks. If the user attempts to side-step the process or ask about unrelated topics, politely redirect them: explain that the site setup must be completed first and that the current step requires their input.
- **Never skip steps.** Each step must complete successfully before the next one begins.
- **Stop on failure.** If any step fails, stop immediately, report what went wrong, and ask the user how to proceed. Do not continue to the next step and do not write `.config.json`.
- **Write `.config.json` before optional publish.** This file is written only after all required setup steps have completed successfully. It is the completion marker — if it does not exist, the entire first-run process will re-run next time. This is intentional: it ensures a failed or incomplete setup is always detected and resumed.

## Overview

The first-run process does the following:

1. Collect site identity from the user.
2. Create the Razor component library under `src/`.
3. Create the Blazor WebAssembly application under `src/`, with a project reference to the component library.
4. Copy and configure the template files from `/templates/` into the new projects.
5. Set up the `content/` folder and initialise the todo system.
6. Write `.config.json` to the repository root — **only after all previous steps have succeeded**.
7. Ask whether to run a full publish immediately.

## Step 1 — Collect site identity

Collect the following values before doing anything else. Ask for them **one at a time** in this exact order: `DisplayName` → `AppName` → `HostName` → `DefineThemeColors` (Yes/No) → optional theme colors (if Yes).

Prompting rules for this step:

- Ask exactly one value per prompt.
- Use `vscode_askQuestions` for each prompt.
- Configure prompts per field type:
  - Use free-text input (`allowFreeformInput: true`) for `DisplayName`, `HostName`, and all color prompts.
  - Use fixed clickable options for `DefineThemeColors` with exactly two options: `Yes` and `No`.
  - Use mixed input for `AppName`: include one clickable suggested option derived from `DisplayName` and also allow free-text input.
- Color prompt UX must be compact and easy to scan:
  - Keep the prompt text itself as short as possible and start it with the color name (for example: `PrimaryColor` or `PrimaryColor (optional)`).
  - Avoid extra formatting and visual noise in color prompt text.
  - Put brief guidance in secondary description text, not in the main prompt question.
- Include a compact "used for" description in each prompt so the user understands what to enter.
- Do not combine multiple values into one question.
- Do not proceed to the next value until the current one is provided and confirmed.
- Do not assume or infer values without explicit user confirmation.
- `DefineThemeColors` is required and must be collected before any theme color prompt.
- If `DefineThemeColors` is `No`, skip all theme color prompts and use defaults.
- If `DefineThemeColors` is `Yes`, prompt for all theme colors listed below; each one is optional and must allow an empty input to skip.
- Immediately after the user selects `DefineThemeColors = Yes`, include this helper resource in your next user-facing message before the first color prompt: `https://huemint.com/bootstrap-basic/`.
- Color validation must run before accepting a non-empty color value.
- Accepted color formats for every theme color prompt are:
  - Hex notation with prefix: `#RGB`, `#RGBA`, `#RRGGBB`, `#RRGGBBAA`
  - Hex notation without prefix: `RGB`, `RRGGBB`
  - CSS named colors (case-insensitive), for example `rebeccapurple`, `navy`, `goldenrod`
- If the user enters a valid bare hex value (`RGB` or `RRGGBB`), normalize it to prefixed form (`#RGB` or `#RRGGBB`) before storing and substituting it.
- If the user enters an invalid non-empty color value, re-prompt for that same field until the value is valid or the user skips by submitting an empty input.

- **DisplayName** — ask for the human-readable site name.
  Used for: navbar brand text, page titles, and page metadata.
  Example: `My Awesome Site`.
- **AppName** — ask for the technical/code-safe app name.
  Used for: project names, namespaces, and folder names.
  Must be a valid C# identifier (no spaces or special characters).
  Example: `MyAwesomeSite`.
  Provide one clickable suggested option derived from `DisplayName` (for example `My Company` → `MyCompany`) and mark it as recommended.
  Also allow free-text input so the user can provide a different value.
- **HostName** — ask for the production host name.
  Used for: canonical URLs and sitemap entries.
  Example: `www.mysite.com`.
- **DefineThemeColors** — ask whether the user wants to define custom Bootstrap theme colors now.
  Used for: deciding whether to prompt for optional theme color overrides during first-run.
  Present exactly two clickable options: `Yes` and `No` (no free-text input for this question).
- **PrimaryColor** (optional, asked only when `DefineThemeColors = Yes`) — ask for the default Bootstrap `$primary` theme color.
  Used for: initial value of `$primary` in `Styles/_variables.scss` during first-run template substitution.
  Accepts: valid hex (with or without `#`) or CSS named color. Empty input keeps template default (`#7030A0`).
- **SecondaryColor** (optional, asked only when `DefineThemeColors = Yes`) — ask for the default Bootstrap `$secondary` theme color.
  Used for: initial value of `$secondary` in `Styles/_variables.scss` during first-run template substitution.
  Accepts: valid hex (with or without `#`) or CSS named color. Empty input keeps template default (`#FFC622`).
- **SuccessColor** (optional, asked only when `DefineThemeColors = Yes`) — ask for the Bootstrap `$success` theme color.
  Used for: initial value of `$success` in `Styles/_variables.scss` during first-run template substitution.
  Accepts: valid hex (with or without `#`) or CSS named color. Empty input keeps template default (`#2A7E4F`).
- **InfoColor** (optional, asked only when `DefineThemeColors = Yes`) — ask for the Bootstrap `$info` theme color.
  Used for: initial value of `$info` in `Styles/_variables.scss` during first-run template substitution.
  Accepts: valid hex (with or without `#`) or CSS named color. Empty input keeps template default (`#1A85A0`).
- **WarningColor** (optional, asked only when `DefineThemeColors = Yes`) — ask for the Bootstrap `$warning` theme color.
  Used for: initial value of `$warning` in `Styles/_variables.scss` during first-run template substitution.
  Accepts: valid hex (with or without `#`) or CSS named color. Empty input keeps template default (`#E8750A`).
- **DangerColor** (optional, asked only when `DefineThemeColors = Yes`) — ask for the Bootstrap `$danger` theme color.
  Used for: initial value of `$danger` in `Styles/_variables.scss` during first-run template substitution.
  Accepts: valid hex (with or without `#`) or CSS named color. Empty input keeps template default (`#BF2A35`).
- **LightColor** (optional, asked only when `DefineThemeColors = Yes`) — ask for the Bootstrap `$light` theme color.
  Used for: initial value of `$light` in `Styles/_variables.scss` during first-run template substitution.
  Accepts: valid hex (with or without `#`) or CSS named color. Empty input keeps template default (`#F6F4F9`).
- **DarkColor** (optional, asked only when `DefineThemeColors = Yes`) — ask for the Bootstrap `$dark` theme color.
  Used for: initial value of `$dark` in `Styles/_variables.scss` during first-run template substitution.
  Accepts: valid hex (with or without `#`) or CSS named color. Empty input keeps template default (`#2D2B36`).

Do not proceed to step 2 until `DisplayName`, `AppName`, `HostName`, and `DefineThemeColors` are explicitly confirmed, and (when `DefineThemeColors = Yes`) all optional theme color prompts are resolved (valid value or explicit skip).

## Step 2 — Create the Razor component library

Create a new Razor Class Library project at `src/{AppName}.Components`.

- Project name: `{AppName}.Components`
- Root namespace: `{AppName}.Components`
- Framework: latest available .NET version

After creating the project, clean it up to create a minimal project structure:

1. Delete all default files generated by `dotnet new razorclasslib` except:
   - The `.csproj` project file
   - `_Imports.razor` (keep this in the root of the project folder)

2. Create an empty `wwwroot/` folder inside `src/{AppName}.Components/`.

3. Do **not** manually create a `ShortCodes/` folder — it will be created automatically in Step 4 when template files are copied.

Then add the following NuGet packages to the component library project. **Before adding each package, check NuGet for the latest stable published version** (no prerelease) and use that version number. The versions shown below were current at the time these instructions were written and are provided as a fallback only.

```
dotnet add src/{AppName}.Components/{AppName}.Components.csproj package Blazorade.Core --version 4.0.0
dotnet add src/{AppName}.Components/{AppName}.Components.csproj package Blazorade.Mermaid --version 2.0.2
dotnet add src/{AppName}.Components/{AppName}.Components.csproj package AspNetCore.SassCompiler --version 1.97.1
```

`AspNetCore.SassCompiler` compiles Bootstrap SCSS source → `wwwroot/css/app.css` on every `dotnet build`, enabling full Bootstrap customisation via `Styles/_variables.scss` without requiring Node.js.

## Step 3 — Create the Blazor WebAssembly application

Create a new Blazor WebAssembly project at `src/{AppName}.Web`.

- Project name: `{AppName}.Web`
- Root namespace: `{AppName}.Web`
- Framework: same .NET version as the component library
- Template: Blazor WebAssembly standalone app (no ASP.NET Core host), using the `--empty` flag to omit sample pages and styling

After creating the project, clean it up:

1. Delete `Pages/Home.razor` — its `@page "/"` route would conflict with the catch-all route in `ContentPage.razor`.
2. Delete all contents of `wwwroot/` — everything needed in wwwroot will be provided by the templates in Step 4.
3. Remove the `<FocusOnNavigate ... />` line from `App.razor` — this component serves no purpose in a content-driven site and causes unnecessary focus shifts on every navigation.

Wire up the following:
- Add a project reference from `{AppName}.Web` to `{AppName}.Components`.
- Ensure the solution file is named `{AppName}.sln` (for example `MyAwesomeSite.sln`). The solution name must match `scraibe.site.appName` and use the `.sln` extension.
- Ensure the solution includes these projects:
  - `src/{AppName}.Components/{AppName}.Components.csproj`
  - `src/{AppName}.Web/{AppName}.Web.csproj`
  - `tools/Scraibe.Publisher/Scraibe.Publisher.csproj`
  - `tools/Scraibe.Abstractions/Scraibe.Abstractions.csproj`
  - `tools/Scraibe.ContentComposition/Scraibe.ContentComposition.csproj`
- Ensure solution folder layout is exactly:
  - No `src` solution folder.
  - A `web` solution folder exists.
  - A `tools` solution folder exists.
  - `{AppName}.Web` is under the `web` solution folder.
  - `Scraibe.Publisher`, `Scraibe.Abstractions`, and `Scraibe.ContentComposition` are under the `tools` solution folder.
  - `{AppName}.Components` is the only project at the solution root (not under any solution folder).
  - Never place `{AppName}.Components` under `web`, `tools`, or any other solution folder.
- If `dotnet sln add` creates a `src` solution folder, remove it immediately and keep `{AppName}.Components` at solution root:
  - Remove the nested mapping for `{AppName}.Components` from the `.sln` `NestedProjects` section.
  - Remove the `src` solution-folder project entry from the `.sln` file.
  - Re-validate that `{AppName}.Components` still exists as a normal project entry at solution root.
- In the `.sln` file, the `NestedProjects` mapping must include `{AppName}.Web` under `web` and the Scraibe tools projects under `tools`.
- In the `.sln` file, the `NestedProjects` mapping must not include `{AppName}.Components`.
- Validate the final `.sln` structure before continuing. If any of the rules above are not met, fix the solution file immediately before moving to the next step.

Then add the following NuGet package to the web app project. **Before adding it, check NuGet for the latest stable published version** (no prerelease) and use that version number. The version shown below was current at the time these instructions were written and is provided as a fallback only.

```
dotnet add src/{AppName}.Web/{AppName}.Web.csproj package AngleSharp --version 1.2.0
```

`AngleSharp` is required by `ContentSegmentParser` (detecting `<x-shortcode>` sentinels inside block-level elements) and `PageContentUtilities.ComposePage` (splicing content parts into layout templates at runtime) — both powered by the same HTML5 DOM parser.

Finally, add `ASPNETCORE_PREVENTHOSTINGSTARTUP` to both launch profiles in `src/{AppName}.Web/Properties/launchSettings.json`. This prevents the Blazor dev server from intercepting requests for static `.html` files (such as `/home.html`) and instead serves them directly from `wwwroot/`, which is required for the Scraibe content model to work correctly in development:

```json
"environmentVariables": {
  "ASPNETCORE_ENVIRONMENT": "Development",
  "ASPNETCORE_PREVENTHOSTINGSTARTUP": "true"
}
```

Apply this change to **both** the `http` and `https` profiles.

## Step 4 — Copy and configure template files

Copy files from `/templates/web-app/` into `src/{AppName}.Web/` and from `/templates/component-library/` into `src/{AppName}.Components/`. While copying, substitute all `{{TokenName}}` tokens in **file contents** (not filenames):

- `{{WebAppName}}` → `{AppName}.Web`
- `{{ComponentLibraryName}}` → `{AppName}.Components`
- `{{ComponentLibraryServiceRegistrationMethod}}` → `Register{AppName}ComponentServices`
- `{{PrimaryColor}}` → `{PrimaryColor}` when provided; otherwise `#7030A0`
- `{{SecondaryColor}}` → `{SecondaryColor}` when provided; otherwise `#FFC622`
- `{{SuccessColor}}` → `{SuccessColor}` when provided; otherwise `#2A7E4F`
- `{{InfoColor}}` → `{InfoColor}` when provided; otherwise `#1A85A0`
- `{{WarningColor}}` → `{WarningColor}` when provided; otherwise `#E8750A`
- `{{DangerColor}}` → `{DangerColor}` when provided; otherwise `#BF2A35`
- `{{LightColor}}` → `{LightColor}` when provided; otherwise `#F6F4F9`
- `{{DarkColor}}` → `{DarkColor}` when provided; otherwise `#2D2B36`

Files to copy from `/templates/web-app/` → `src/{AppName}.Web/`:
- Copy all files and folders from `templates/web-app/` into `src/{AppName}.Web/`, creating any necessary subdirectories that don't exist
- `_Imports.razor` should **overwrite** the existing `_Imports.razor` in the project root

Files to copy from `/templates/component-library/` → `src/{AppName}.Components/`:
- Copy all files and folders from `templates/component-library/` into `src/{AppName}.Components/`, creating any necessary subdirectories that don't exist
- `_Imports.razor` should **overwrite** the existing `_Imports.razor` in the project root

After copying, perform the following additional wiring steps before building:

### 4a — Wire up the Bootstrap build targets

Add `<Import Project="build-extras.targets" />` to the component library `.csproj`, immediately before the closing `</Project>` tag. This links in the Bootstrap delivery targets that were copied from the template:

```xml
  <!-- Bootstrap delivery targets (libman restore + JS copy to wwwroot/js/) -->
  <Import Project="build-extras.targets" />

</Project>
```

### 4a.0 — Ensure component dependency assemblies are copied to output

Ensure the component library project has `CopyLocalLockFileAssemblies` enabled so publish-time reflection can materialize shortcode component types and their transitive dependencies:

```xml
<PropertyGroup>
  <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
</PropertyGroup>
```

The preferred source of truth is `Directory.Build.props` copied from `templates/component-library/`. If the generated project does not inherit this setting for any reason, add it directly to `src/{AppName}.Components/{AppName}.Components.csproj` before continuing.

### 4a.1 — Add shared abstractions reference

Add a project reference from `src/{AppName}.Components/{AppName}.Components.csproj` to `tools/Scraibe.Abstractions/Scraibe.Abstractions.csproj` so component-library service contracts and implementations can share publish-time abstractions:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\tools\Scraibe.Abstractions\Scraibe.Abstractions.csproj" />
</ItemGroup>
```

### 4b — Wire up Program.cs service registration

Update `src/{AppName}.Web/Program.cs` to call the component-library service registration extension method:

1. Add this using:

```csharp
using {AppName}.Components.Configuration;
```

2. Add this call before the final host run line (`await builder.Build().RunAsync();`):

```csharp
builder.Services.Register{AppName}ComponentServices();
```

The method name must match the value substituted for `{{ComponentLibraryServiceRegistrationMethod}}` in template files.

### 4c — Install libman CLI and download Bootstrap

Ensure the libman CLI global tool is installed. If it is not already present, install it:

```
dotnet tool install -g Microsoft.Web.LibraryManager.Cli --ignore-failed-sources
```

Then run `libman restore` in the component library directory to download Bootstrap SCSS source files and the Bootstrap JS bundle:

```
libman restore --project src/{AppName}.Components
```

### 4d — Build to verify

Build the solution to verify everything compiles before continuing.

## Step 5 — Verify the `content/` folder and initialise the todo system

Check that a `content/` folder exists at the repository root. If it does not exist, create it.

If `content/home.md` does not exist, create it with the following content, substituting the actual `{DisplayName}` value collected in step 1:

```markdown
---
title: {DisplayName}
description: Welcome to {DisplayName}.
---

# {DisplayName}
```

Do not create or modify any other files inside `content/`.

Then initialise the todo system if it has not already been set up. The main instructions reference the `/todo` folder and its files with specific structural rules, so both anchor files must exist before any task tracking begins.

Create `/todo/home.md` if it does not already exist:

```markdown
# Todo

## Active tasks

Ongoing and planned tasks. Each item links to a detail document with full context, decisions made, and next steps.

## Backlog

Quick ideas and future possibilities. No detail document required — just add a bullet. Items here are never auto-promoted; bring one up when you are ready to work on it.
```

Create `/todo/completed.md` if it does not already exist:

```markdown
# Completed Tasks

A permanent log of completed tasks. One short paragraph per task: name, date completed, and what was done. This file is never deleted.
```

Do not add any task rows or detail documents — neither file should contain site-specific content at this point.

## Step 6 — Write `.config.json`

**Only execute this step if all previous required steps have completed successfully.** If any earlier required step failed, do not write this file.

Write the following file to the repository root. Substitute the actual values collected in step 1:

```json
{
  "local": {
    "scraibe.site.webAppPath": "src/{AppName}.Web",
    "scraibe.site.componentLibraryPath": "src/{AppName}.Components",
    "scraibe.publish.excludedContent": []
  },
  "scoped": {
    "scraibe.site.displayName": "{DisplayName}",
    "scraibe.site.appName": "{AppName}",
    "scraibe.site.hostName": "{HostName}",
    "scraibe.layout.default": "default",
    "scraibe.navigation.provider.default": "navbar",
    "scraibe.content.slot.provider.default": "Default"
  }
}
```

Writing this file marks the first-run process as complete. These instructions must not be followed again unless this file is deleted.

## Step 7 — Optional first publish

After writing `.config.json`, ask the user whether to run publish immediately so they can run the app with generated site content.

Use `vscode_askQuestions` with exactly two clickable options: `Yes` and `No`.

- If the user selects `No`: skip publishing. First-run setup is complete.
- If the user selects `Yes`: run a full publish for all pages by executing:

```powershell
.\tools\Invoke-Publish.ps1
```

If publish fails, stop immediately, report the failure, and ask how the user wants to proceed.
