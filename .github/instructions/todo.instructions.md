# Todo Management

The `/todo` folder is a lightweight task-tracking system for ongoing and future work.

- `/todo/home.md` is the **index** with two sections:
  - **Active tasks** — a bullet list where each item links to a detail document and has a one-sentence description. Do not put task detail here.
  - **Backlog** — a plain bullet list of short ideas with no detail document yet. No links required.
- Each active task has its own detail document (e.g. `/todo/reusable-ai-site-builder.md`) containing the full context, decisions made, and next steps.
- `/todo/completed.md` is the permanent completion log. It contains a short paragraph per completed task (name, date, and a few sentences on what was done and where). It is never deleted.
- When a new active task is identified: create a detail document in `/todo/` and add a bullet to the Active tasks section of `/todo/home.md`.
- When a backlog idea is ready to be worked on: create its detail document, move it from the Backlog section to the Active tasks section as a linked bullet, and remove it from the backlog.
- **Never auto-promote a backlog item.** Only promote a backlog item to an active task when explicitly asked.
- When a task is fully complete: write a short summary entry in `/todo/completed.md` **at the top, immediately after the introductory paragraph** (entries are newest-first), then remove its bullet from `/todo/home.md` and delete its detail document.
- Never load all detail documents speculatively. Read `/todo/home.md` to get an overview, then read a specific detail document only when that task is actively being worked on.

All todo documents are Markdown files. Follow the rules in [markdown-instructions.md](markdown-instructions.md) when creating or editing any file in `/todo/`.
