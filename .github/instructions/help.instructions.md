# What I Can Help With

This file is the authoritative index of capabilities. When the user asks what you can help with, read this file and present the relevant items based on their context (content author, site builder, or both). For full details on any topic, refer to the linked source files in `/content/scraibe-docs` — those files are the master documentation and must not be duplicated here.

## Content authoring

- **Writing and editing pages** — create or update Markdown files in `/content`, with correct YAML frontmatter fields (`title`, `description`, `slug`, `keywords`, `author`, `date`, `changefreq`, `priority`). See [content/scraibe-docs/content-authoring.md](../../content/scraibe-docs/content-authoring.md).
- **SEO metadata** — not just field names but actively drafting effective `title`, `description`, and `keywords` values for a page: concise titles that work in browser tabs and search results, meta descriptions that summarise the page in 150–160 characters, and relevant keyword sets derived from the page content.
- **Content quality** — proofread pages for spelling, grammar, and clarity; improve sentence structure and flow; suggest better phrasing; ensure consistent tone and terminology across multiple pages. Just ask with the page open or attached.
- **File structure and routing** — advise on where a page should live, what URL it will get, and the rules around reserved filenames (`home.md`, blocked `index.md`). See [content/scraibe-docs/content-authoring.md](../../content/scraibe-docs/content-authoring.md).
- **Folder configuration (`.config.json`)** — create and edit folder-level configuration files, explain `local` vs `scoped` inheritance, and help choose where a setting should live for the intended effective behavior. See [content/scraibe-docs/folder-configuration.md](../../content/scraibe-docs/folder-configuration.md).
- **Content structure and information architecture** — advise on how to organise a section into pages, when a flat file should become a folder, and how the resulting navigation will look.
- **Shortcodes** — embed live Blazor components in Markdown using self-closing or wrapping shortcode syntax, including nested and multi-line forms. See [content/scraibe-docs/shortcodes/home.md](../../content/scraibe-docs/shortcodes/home.md).
- **Relative links in Markdown** — author normal relative links and image paths that work in Markdown preview; the publish pipeline rewrites them to root-relative URLs in generated HTML and resolves `.md` links to clean URLs automatically. See [content/scraibe-docs/publishing.md](../../content/scraibe-docs/publishing.md).
- **Static assets** — place images and other files alongside Markdown under `/content`; eligible assets are copied to `wwwroot/` at the same relative path on publish, and links are rewritten to root-relative URLs.
- **Mermaid diagrams** — generate Mermaid markup from a plain-language description, or embed/edit diagrams directly. Supported types: flowcharts, sequence diagrams, class diagrams, state machines, ER diagrams, Gantt charts, pie charts, and more. See [content/scraibe-docs/mermaid.md](../../content/scraibe-docs/mermaid.md).

## Publishing

- **Running the publish pipeline** — regenerate all static HTML bootstrappers and `sitemap.xml`, sync eligible static assets from `/content`, and update `staticwebapp.config.json` route/exclude settings from publish output. See [content/scraibe-docs/publishing.md](../../content/scraibe-docs/publishing.md).
- **Excluding content** — manage `scraibe.publish.excludedContent` in `.config.json` to skip pages from publishing without deleting their source files.
- **Config-related publish troubleshooting** — diagnose invalid `.config.json` shape errors, resolve duplicate `local`/`scoped` keys, and explain layout resolution failures tied to missing `scraibe.layout.default`.
- **Navigation configuration and behavior** — help configure folder-level navigation settings and explain how those settings affect generated pages and publish output. See [content/scraibe-docs/folder-configuration.md](../../content/scraibe-docs/folder-configuration.md) and [content/scraibe-docs/publishing.md](../../content/scraibe-docs/publishing.md).

## Site building

- **Components** — create or update reusable Blazor components in `{ComponentLibraryPath}`, including shortcode components that content authors can embed in Markdown. See [content/scraibe-docs/content-authoring.md](../../content/scraibe-docs/content-authoring.md).
- **Styling** — a broad area where the agent can actively generate and advise, not just point to documentation:
  - **Theme generation** — given just a primary (and optionally secondary) colour, derive a full harmonious Bootstrap theme: `$success`, `$info`, `$warning`, `$danger`, `$light`, `$dark` that work together visually and meet contrast requirements. Edit `{ComponentLibraryPath}/Styles/_variables.scss` directly.
  - **Bootstrap variable overrides** — typography (`$font-family-base`, `$font-size-base`, `$headings-font-family`), spacing (`$spacer`), borders (`$border-radius`, `$border-color`), shadows, transitions, and any other Bootstrap Sass variable. A full reference is at https://getbootstrap.com/docs/5.3/customize/sass/#variable-defaults.
  - **Custom SCSS** — write new `.scss` partial files and import them into `app.scss`; advise on nesting, mixins, `@extend`, and Bootstrap's own mixins (`color-contrast`, `media-breakpoint-up`, etc.).
  - **Bootstrap utility classes** — advise which utility classes to use in Markdown, layout templates, or Blazor components for spacing, typography, display, flexbox/grid, and responsive breakpoints.
  - **Component-scoped styles** — add `.razor.css` isolation files for Blazor components so styles don't leak.
  - **Troubleshooting** — diagnose compiled CSS output issues, specificity conflicts, or unexpected Bootstrap overrides.
  See [content/scraibe-docs/styling.md](../../content/scraibe-docs/styling.md).
- **Prerequisites and setup** — guide through the tools needed to run the site locally. See [content/scraibe-docs/prerequisites.md](../../content/scraibe-docs/prerequisites.md).

## Todos

- **Log, review, and close todo items** stored in `/todo`. Useful for tracking content ideas, feature requests, or anything to revisit later.

## Playbooks

- **Discover and run playbooks** — site-specific repeatable procedures stored in `/playbooks`. I read `playbooks/home.md` to find the right one, then load and follow it.
- **Create new playbooks** — if you have a procedure you repeat often, I can codify it as a playbook.
