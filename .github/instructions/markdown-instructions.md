---
applyTo: "**/*.md"
---

# Instructions for writing markdown documents

These instructions define the standards for all markdown documents in this repository. Consistent formatting and structure ensure readability, maintainability, and professional presentation across all documentation.

## Document Structure

- All documents must start with a single top-level heading (H1)
- After the title, include introductory content (one or more paragraphs or a brief overview section) that provides context and explains the document's purpose
- Introductory content can be a plain paragraph, multiple paragraphs, or an introductory section (e.g., "Overview", "Introduction")
- Keep introductory content concise; even one or two sentences is acceptable if it clearly establishes context
- Use hierarchical headings (H1 → H2 → H3) as the primary structural mechanism
- Do not skip heading levels (e.g., H1 → H3 without H2)
- NEVER use horizontal rulers (`---` or `***`) to separate sections
- Each section should be self-contained and logically organized

## Headings

- Use sentence case for all headings (capitalize only the first word and proper nouns)
- H1 (`#`) - Document title only, use exactly once per document
- H2 (`##`) - Major sections
- H3 (`###`) - Subsections
- Avoid going deeper than H4 (`####`) unless absolutely necessary
- Leave one blank line before and after each heading

## Lists

- Use hyphens (`-`) for unordered lists, NEVER asterisks (`*`) or plus signs (`+`)
- Use consistent indentation (2 spaces) for nested list items
- Use numbered lists (`1.`, `2.`, etc.) only for sequential steps or ordered items
- Keep list items concise; use multiple paragraphs within list items only when essential
- Maintain parallel structure (start all items with same part of speech when possible)

## Code and Technical Content

- Use inline code (`` `code` ``) for:
  - Variable names, method names, class names
  - File paths and file names
  - Command names and short code snippets
  - Technical terms that represent code elements
- Use fenced code blocks (` ``` `) for:
  - Multi-line code examples
  - Command sequences
  - Configuration files
- Always specify language identifier for syntax highlighting (e.g., ` ```csharp `, ` ```json `, ` ```bash `)
- Prefer explicit code over placeholders; avoid `...` or `// TODO` in examples unless specifically illustrating incomplete code

## Links

- Use descriptive link text that makes sense out of context
- NEVER use "click here" or "this link" as link text
- Prefer inline links: ``[descriptive text](https://example.com)`` over reference-style links
- For internal repository links, use relative paths from the repository root
- External links should use HTTPS when available
- Ensure all links are functional and point to stable, authoritative sources

## Emphasis

- Use **bold** (`**text**`) for important terms, warnings, or emphasis
- Use *italics* (`*text*`) sparingly for subtle emphasis or introducing new terms
- NEVER use underscores (`_text_`) for emphasis; always use asterisks
- Avoid overusing emphasis; let content clarity speak for itself

## Tables

- Use tables only when tabular data presentation adds clarity
- Always include a header row with column names
- Align columns for readability in source markdown
- Keep table content concise; avoid lengthy paragraphs in cells
- For complex data, consider using alternative formats (lists, nested sections, or Mermaid diagrams)

## Diagrams

- Use Mermaid diagrams when describing complex relations between entities
- Mermaid is preferred for:
  - Entity relationships and data models
  - System architecture and component interactions
  - Process flows and state transitions
  - Sequence diagrams for interactions between components
- Always use fenced code blocks with `mermaid` language identifier
- Keep diagrams focused and uncluttered; break complex diagrams into multiple smaller ones
- Include a brief description before the diagram explaining what it represents
- Ensure diagram syntax is valid and renders correctly

## Technical Accuracy

- All code examples must be syntactically correct and executable
- Use actual values, not placeholders like `your-value-here`, unless explicitly documenting configuration
- Include necessary context (imports, prerequisites) for code examples
- Test all commands and code snippets before documenting
- Specify versions, prerequisites, or platform requirements when relevant

## Language and Tone

- Write in clear, direct language
- Use active voice
- Avoid conversational filler and marketing language
- Be explicit and precise; avoid ambiguous terms like "might", "could", or "generally" unless uncertainty is real
- Define acronyms on first use in each document
- Assume the reader is technically competent; do not over-explain fundamentals

## Content Quality

- Every document must serve a clear purpose
- Remove or update obsolete content immediately
- Keep documentation close to the code it describes
- Focus on "why" and "how" rather than restating "what" the code does
- Include practical examples that demonstrate real-world usage
