# Todo Management

The `/todo` folder is a lightweight task-tracking system for ongoing and future work.

- `/todo/home.md` is the **index** with two sections:
  - **Active tasks** — a bullet list where each item links to a detail document and has a one-sentence description. Do not put task detail here.
  - **Backlog** — a plain bullet list of short ideas with no detail document yet. No links required.
- Each active task has its own detail document (e.g. `/todo/reusable-ai-site-builder.md`) containing the full context, decisions made, and next steps.
- `/todo/completed.md` is the permanent completion log. It contains a short paragraph per completed task (name, date, and a few sentences on what was done and where). It is never deleted.
- **Active tasks are always ordered by recommended implementation sequence** — the task you would start next is first, the task furthest out is last. When adding a new task, consider whether its introduction affects the order of existing tasks, not just where the new item slots in, and reorder the full list if needed.
- When a new active task is identified: create a detail document in `/todo/` and add a bullet to the Active tasks section of `/todo/home.md`.
- When a backlog idea is ready to be worked on: create its detail document, move it from the Backlog section to the Active tasks section as a linked bullet, and remove it from the backlog.
- **Never auto-promote a backlog item.** Only promote a backlog item to an active task when explicitly asked.
- **Never close a task without explicit user confirmation.** After implementing a task, report what was done and ask the user to confirm it is working before performing any completion steps. Only proceed with closing once the user gives an unambiguous confirmation (e.g. *"looks good"*, *"confirmed"*, *"close it"*). A question or partial agreement is not confirmation.
- **Documentation is a mandatory part of every task.** A task cannot be closed unless all documentation files listed in its detail document have been updated or created. If the user wants to close a task without completing its documentation, they must explicitly state that no documentation is needed for this task — that statement alone is sufficient to waive the requirement. Without such a statement, the documentation work must be completed before closing.
- **Help capability documentation must always be considered.** When defining documentation changes for a todo item, explicitly evaluate whether `.github/instructions/help.instructions.md` needs updates so the AI agent can assist content authors with the new or changed feature. If updates are needed, list them in the task's documentation section; if not, explicitly state that no help-instruction changes are required. Do not omit this decision.
- **Every active task detail document must include a `Definition of done` section.** The section must contain concrete, verifiable checklist items that determine completion, including behavior validation and required documentation updates. A task is not implementation-ready if this section is missing.
- When a task is fully complete and confirmed: write a short summary entry in `/todo/completed.md` **at the top, immediately after the introductory paragraph** (entries are newest-first), then remove its bullet from `/todo/home.md` and delete its detail document.
- Never load all detail documents speculatively. Read `/todo/home.md` to get an overview, then read a specific detail document only when that task is actively being worked on.
- If `/todo/home.md` does not exist, the todo system has not been initialised yet. Copy `templates/todo/home.md` to `/todo/home.md`, then proceed as normal.

## Implementation-readiness rule

A detail document is **implementation-ready** when all architectural and design decisions required to implement the task are captured in the document itself — a new agent instance must be able to implement the task correctly by reading the detail document alone, without needing to ask clarifying questions about design intent. Decisions that are not yet made must appear as **open questions** until resolved. An active task with open questions is not yet implementation-ready.

A detail document is not implementation-ready if the help-instruction impact decision is missing. It must either include the required `.github/instructions/help.instructions.md` updates in the documentation plan or explicitly state that no help-instruction changes are needed.

A detail document is not implementation-ready if its `Definition of done` section is missing, vague, or not testable.

**Every detail document must be self-contained.** It must not rely on chat history, prior conversation context, or assumptions that exist only in a previous session. Anything a future implementer needs to know — architectural decisions, naming conventions, token formats, behavioural rules, cross-file dependencies — must be written explicitly in the document.

This does **not** mean the document must describe everything discoverable from the codebase (existing file contents, surrounding conventions, etc.) — those can be found with tools. It only means that design choices, architectural decisions, specific formats, and behavioural rules that cannot be inferred from code must be written down explicitly.

A detail document describes the **intended design**, not the state of the code when the document was written. When implementing a task, always work against the **current state of the codebase** — check actual file contents with tools rather than assuming the files still look as they did at plan-time. Other tasks may have been completed in the meantime and the files may have changed.

All todo documents are Markdown files. Follow the rules in [markdown-instructions.md](markdown-instructions.md) when creating or editing any file in `/todo/`.

## Backlog self-containment rule

A backlog bullet must contain enough information for a new agent instance — with no access to prior chat history — to understand what the item is, why it matters, and how it fits into the project well enough to write a complete, implementation-ready detail document for it.

This does **not** require the same depth as a detail document. A backlog bullet does not need open questions listed, file names enumerated, or implementation steps described. It only needs:

- a clear statement of what the feature or change is,
- enough context to understand *why* it is wanted, and
- any key design decisions already made that would affect how the detail document is written.

When adding or editing a backlog item, check it against this rule. If a new agent reading only the bullet would have to guess at intent or invent design decisions from scratch, the bullet needs more information.
