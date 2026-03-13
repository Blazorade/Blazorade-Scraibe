---
title: Prerequisites
description: Everything you need to install on your machine before running Blazorade Scraibe.
keywords: prerequisites, setup, install, IDE, GitHub Copilot, .NET SDK, Git, posh-git, PowerShell
changefreq: monthly
priority: 0.7
---

# Prerequisites

This page lists all software that must be available on your machine before Blazorade Scraibe will run correctly. Each item explains what it is used for and where to get it.

## Required Software

### An IDE with GitHub Copilot Support

Blazorade Scraibe is entirely driven by GitHub Copilot running in **agent mode**. Any IDE or development environment that supports GitHub Copilot agent mode will work — the framework has no hard dependency on a specific editor.

**Visual Studio Code** is the most widely used option and is the IDE referenced throughout this documentation.

**Download VS Code:** [code.visualstudio.com](https://code.visualstudio.com/)
**GitHub Copilot extension for VS Code:** [marketplace.visualstudio.com](https://marketplace.visualstudio.com/items?itemName=GitHub.copilot)

Install the stable release for your operating system (Windows, macOS, or Linux).

### GitHub Copilot

**Learn more:** [github.com/features/copilot](https://github.com/features/copilot)

GitHub Copilot is the AI engine behind Blazorade Scraibe. It reads your Markdown source files, applies the publishing instructions, and runs the repository workflow that generates static HTML bootstrappers, updates the sitemap, and regenerates navigation. Without an active Copilot session, none of the publishing or setup workflows will run.

You will need:

- A **GitHub account** — [github.com/join](https://github.com/join)
- An active **GitHub Copilot subscription** (Individual, Business, or Enterprise) — [github.com/features/copilot](https://github.com/features/copilot)

How you install and activate GitHub Copilot depends on your IDE. In **VS Code**, install the [GitHub Copilot extension](https://marketplace.visualstudio.com/items?itemName=GitHub.copilot) from the Marketplace and sign in with your GitHub account from the Accounts menu.

### .NET SDK

**Download:** [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)

The Blazor WebAssembly application that powers the runtime rendering of your site is a .NET project. The .NET SDK provides the `dotnet` CLI used to build and run that application. **Version 10.0 or later is required.**

To verify your installed version, run:

```
dotnet --version
```

The output should be `10.0.0` or higher. If it is not, download and install the latest SDK from the link above.

### libman CLI

**Install:** [learn.microsoft.com/aspnet/core/client-side/libman/libman-cli](https://learn.microsoft.com/aspnet/core/client-side/libman/libman-cli)

The `libman` CLI (Microsoft Library Manager) is a .NET global tool that downloads client-side library files — in Blazorade Scraibe's case, Bootstrap's SCSS source files and JS bundle — directly from a CDN into your project. No Node.js or npm required.

Install it once on your machine:

```
dotnet tool install -g Microsoft.Web.LibraryManager.Cli
```

To verify the installation:

```
libman --version
```

The build pipeline calls `libman restore` automatically on first build when Bootstrap source files are not yet present, so you do not need to run it manually.

### Git

**Download:** [git-scm.com/downloads](https://git-scm.com/downloads)

Git is required to clone this repository, create a repository from the template, and commit your content changes. VS Code's built-in source control panel uses the system Git installation.

To verify that Git is installed, run:

```
git --version
```

On Windows, Git for Windows includes Git Bash and the Git credential manager, both of which are useful when authenticating with GitHub.

### GitHub Account

**Sign up:** [github.com/join](https://github.com/join)

A GitHub account is needed to:

- Use the **Use this template** button to create a new repository from the Blazorade Scraibe template.
- Authenticate with GitHub Copilot inside VS Code.
- Push your content to GitHub for version control and CI/CD.
- Host your site for free on **Azure Static Web Apps** — including custom domains, HTTPS, and clean-URL routing rules.

## Optional Software

The following tools are not required to author content or run the local development server, but they are useful for deploying your site, working with Azure Static Web Apps, or improving your general development experience.

### Azure Static Web Apps CLI

**Install:** [github.com/Azure/static-web-apps-cli](https://github.com/Azure/static-web-apps-cli)

The SWA CLI lets you run a local emulation of Azure Static Web Apps, including routing rules defined in `staticwebapp.config.json`. Install it globally via npm:

```
npm install -g @azure/static-web-apps-cli
```

### Azure CLI

**Download:** [learn.microsoft.com/cli/azure/install-azure-cli](https://learn.microsoft.com/cli/azure/install-azure-cli)

The Azure CLI (`az`) is useful if you want to manage your Azure Static Web Apps deployment from the command line — for example, to create a new SWA resource, link a GitHub repository, or manage deployment tokens.

### posh-git

**Install:** [github.com/dahlbyk/posh-git](https://github.com/dahlbyk/posh-git)

posh-git is a PowerShell module that enriches your terminal prompt with live Git status information. While you are inside a Git repository, the prompt shows the current branch name and a set of concise symbols indicating the sync state and any pending changes:

| Symbol | Meaning |
|--------|---------|
| `≡` | Branch is in sync with remote |
| `↑n` | n commits ahead of remote |
| `↓n` | n commits behind remote |
| `*` | Unstaged changes present |
| `+` | Staged changes ready to commit |

To install posh-git from the PowerShell Gallery, run:

```powershell
Install-Module posh-git -Scope CurrentUser -Force
Add-PoshGitToProfile
```

The second command adds the import line to your PowerShell profile so the prompt enhancement is active in every new terminal session automatically.

## Verifying Your Setup

Once everything is installed, open the repository folder in VS Code. If GitHub Copilot is signed in and the `.github/copilot-instructions.md` file is present, the agent will automatically detect the configuration state and guide you through any remaining setup steps.
