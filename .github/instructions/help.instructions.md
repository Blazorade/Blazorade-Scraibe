# What I Can Help With

This file is the authoritative index of capabilities. When the user asks what you can help with, read this file and present the relevant items based on their context (content author, site builder, or both). For full details on any topic, refer to the linked source files in `/content/scraibe-docs` — those files are the master documentation and must not be duplicated here.

## Content authoring

- **Writing and editing pages** — create or update Markdown files in `/content`, with correct YAML frontmatter fields (`title`, `description`, `slug`, `keywords`, `author`, `date`, `changefreq`, `priority`). See [content/scraibe-docs/content-authoring.md](../../content/scraibe-docs/content-authoring.md).
- **File structure and routing** — advise on where a page should live, what URL it will get, and the rules around reserved filenames (`home.md`, blocked `index.md`). See [content/scraibe-docs/content-authoring.md](../../content/scraibe-docs/content-authoring.md).
- **Shortcodes** — embed live Blazor components in Markdown using self-closing or wrapping shortcode syntax, including nested and multi-line forms. See [content/scraibe-docs/shortcodes/home.md](../../content/scraibe-docs/shortcodes/home.md).

## Publishing

- **Running the publish pipeline** — regenerate all static HTML bootstrappers, `sitemap.xml`, `staticwebapp.config.json`, and `NavMenu.razor` from the Markdown sources. See [content/scraibe-docs/publishing.md](../../content/scraibe-docs/publishing.md).
- **Excluding content** — manage the exclusion list in `blazorade.config.md` to skip pages from publishing without deleting their source files.

## Site building

- **Components** — create or update reusable Blazor components in `{ComponentLibraryPath}`, including shortcode components that content authors can embed in Markdown. See [content/scraibe-docs/content-authoring.md](../../content/scraibe-docs/content-authoring.md).
- **Styling** — advise on CSS conventions, customisation points (`app.css`), and component-scoped styles. See [content/scraibe-docs/styling.md](../../content/scraibe-docs/styling.md).
- **Prerequisites and setup** — guide through the tools needed to run the site locally. See [content/scraibe-docs/prerequisites.md](../../content/scraibe-docs/prerequisites.md).

## Todos

- **Log, review, and close todo items** stored in `/todo`. Useful for tracking content ideas, feature requests, or anything to revisit later.

## Playbooks

- **Discover and run playbooks** — site-specific repeatable procedures stored in `/playbooks`. I read `playbooks/home.md` to find the right one, then load and follow it.
- **Create new playbooks** — if you have a procedure you repeat often, I can codify it as a playbook.
