# Site Playbooks

The `/playbooks` folder is the site-specific procedure library. Playbooks are natural-language instructions that teach Copilot how to carry out recurring, multi-step processes that are unique to this site. They complement the built-in framework workflows (publish, first-run, todo) but are not part of the Blazorade Scraibe framework — they are authored by the site owner.

## What a playbook is

A playbook is a documented, repeatable procedure written in plain language. It describes *when* to run it, *what* it does, and *how* to do it step by step. Playbooks do not contain shell commands, JSON config, or code — they contain instructions that Copilot reads and follows.

A playbook is appropriate for things like:
- A content review and freshness audit
- A pre-launch readiness checklist
- A process for adding a new content section
- Anything the site owner wants to be able to trigger by asking Copilot

## Discovery and loading

- `/playbooks/home.md` is the **index**. It contains a bullet list of all available playbooks, each with:
  - **Name** — the human-readable title
  - **Trigger** — a sentence beginning with *"Trigger when..."* that describes the user intent that should activate this playbook. Write it in terms of what the user might ask for, not what the playbook does internally. Example: *"Trigger when the user asks to review, refresh, or audit the site's content."*
  - **File** — a link to the playbook document to load
- Never load all playbook documents speculatively. Read `/playbooks/home.md` first to identify which playbook matches the user's request, then load only that one.
- If `/playbooks/home.md` does not exist, no playbooks have been defined for this site yet. Copy `templates/playbooks/home.md` to `/playbooks/home.md`, then inform the user and offer to create the first one.
- If no playbook clearly matches, tell the user what playbooks are available and ask which one they want to run.

## Playbook document structure

Each playbook document should contain:
1. **Purpose** — one short paragraph describing what this playbook accomplishes and when it is used
2. **Steps** — a numbered list of concrete instructions for Copilot to follow, written in plain language
3. **Notes** (optional) — any caveats, edge cases, or decisions the site owner wants Copilot to be aware of

## Managing playbooks

- When a new playbook is created: add a bullet to `/playbooks/home.md` with its name, trigger description, and file link.
- When a playbook is retired: remove its bullet from `/playbooks/home.md` and delete its document.
- Playbooks do not have an active/completed lifecycle — they are either available or removed. They are not todo items.

All playbook documents are Markdown files. Follow the rules in [markdown-instructions.md](markdown-instructions.md) when creating or editing any file in `/playbooks/`.
