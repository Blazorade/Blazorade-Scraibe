---
applyTo: "**/*.cs"
---

# C# XML documentation requirements

These rules apply to all C# source files.

## Public members must be documented

- Every `public` member must include XML documentation comments (`///`).
- This includes public types, constructors, methods, properties, fields, events, and delegates.

## Focus on why, not what

- Document the purpose, intent, and contract of the public API.
- Explain why the member exists, when it should be used, and any important constraints or invariants.
- Do not restate obvious implementation details that are already clear from names, signatures, or straightforward code.

## Writing guidance

- Keep documentation concise and decision-oriented.
- Prefer meaningful context over narration of code flow.
- Use `<summary>` for intent and role.
- Use `<param>`, `<returns>`, `<remarks>`, and `<exception>` only when they add useful behavioral or usage context.

## Example intent

- Preferred: "Coordinates publish-time content validation so callers get consistent, user-facing errors before file generation begins."
- Avoid: "Validates content and returns true or false."
