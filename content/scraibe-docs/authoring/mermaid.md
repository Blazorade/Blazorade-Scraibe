---
title: Mermaid Diagrams
description: How to embed Mermaid diagrams in Blazorade Scraibe content pages using fenced code blocks.
keywords: Mermaid, diagrams, flowchart, sequence diagram, class diagram, state diagram, content authoring
changefreq: monthly
priority: 0.8
---

# Mermaid Diagrams

Blazorade Scraibe has built-in support for [Mermaid](https://mermaid.js.org/) diagrams. Write a fenced code block with the `mermaid` language identifier and the diagram is rendered as an interactive, scalable graphic in the browser — nothing else required.

For runtime context on how diagram content is published and enhanced, see [Architecture positioning](../core/architecture-positioning.md) and [Runtime glossary](../core/runtime-glossary.md).

````markdown
```mermaid
flowchart LR
    A[Start] --> B{Decision}
    B -->|Yes| C[Do it]
    B -->|No| D[Skip it]

```
````

Fenced `mermaid` blocks are powered by the [Mermaid shortcode component](./shortcodes/mermaid.md). Anyone interested in the underlying implementation can start there.

## Supported Diagram Types

Blazorade Mermaid renders any diagram type supported by [Mermaid 11](https://mermaid.js.org/). All types listed in the official Mermaid documentation are available — the examples below cover the full set.

### Flowchart

Describes processes, decisions, and data flows.

```mermaid
flowchart TD
    A([Start]) --> B[Read input]
    B --> C{Valid?}
    C -->|Yes| D[Process]
    C -->|No| E[Show error]
    D --> F([End])
    E --> B
```

Direction keywords: `TD` (top-down), `LR` (left-right), `BT` (bottom-top), `RL` (right-left).

### Sequence Diagram

Shows interactions between participants over time.

```mermaid
sequenceDiagram
    participant Browser
    participant BlazorApp
    participant ContentServer

    Browser->>BlazorApp: Navigate to /about
    BlazorApp->>ContentServer: Fetch /about.html
    ContentServer-->>BlazorApp: Return static HTML
    BlazorApp-->>Browser: Render page
```

### Class Diagram

Describes object-oriented structures and relationships.

```mermaid
classDiagram
    class ContentPage {
        +string Slug
        +Frontmatter Metadata
        +Render()
    }
    class Frontmatter {
        +string Title
        +string? Description
        +string Layout
    }
    ContentPage "1" --> "1" Frontmatter
```

### State Diagram

Models states and transitions, useful for workflows or UI logic.

```mermaid
stateDiagram-v2
    [*] --> Draft
    Draft --> Published : publish
    Published --> Draft : edit
    Published --> Archived : archive
    Archived --> [*]
```

### Entity Relationship Diagram

Documents data models and their relationships.

```mermaid
erDiagram
    PAGE ||--o{ PART : contains
    PAGE {
        string slug
        string title
        string layout
    }
    PART {
        string name
        string html
    }
```

### Gantt Chart

Visualises project schedules and timelines.

```mermaid
gantt
    title Site Build Plan
    dateFormat YYYY-MM-DD
    section Content
        Write pages     :a1, 2026-03-01, 7d
        Review          :a2, after a1, 3d
    section Publishing
        Initial publish :b1, after a2, 1d
```

### Pie Chart

Shows proportional data as a labelled pie.

```mermaid
pie title Traffic by Source
    "Organic search" : 52
    "Direct"         : 28
    "Social"         : 14
    "Referral"       : 6
```

### Git Graph

Illustrates branch and commit history.

```mermaid
gitGraph
    commit id: "Initial commit"
    branch feature/nav
    checkout feature/nav
    commit id: "Add navbar"
    checkout main
    merge feature/nav
    commit id: "Release v1"
```

### User Journey

Maps the steps a user takes through a process and scores the experience at each step.

```mermaid
journey
    title Publishing a page
    section Author
        Write Markdown : 5: Author
        Run publish    : 4: Author
    section Reader
        Open page      : 5: Reader
        Read content   : 5: Reader
```

### Quadrant Chart

Plots items on a two-axis quadrant for prioritisation or comparison.

```mermaid
quadrantChart
    title Effort vs Impact
    x-axis Low Effort --> High Effort
    y-axis Low Impact --> High Impact
    quadrant-1 High priority
    quadrant-2 Quick wins
    quadrant-3 Low priority
    quadrant-4 Fill-ins
    Mermaid support: [0.2, 0.9]
    Dark mode: [0.6, 0.7]
    Animations: [0.8, 0.3]
```

### Requirement Diagram

Documents requirements and their relationships to system elements.

```mermaid
requirementDiagram
    requirement auth {
        id: 1
        text: System shall authenticate users
        risk: high
        verifyMethod: test
    }
    element webApp {
        type: system
    }
    webApp - satisfies -> auth
```

### C4 Diagram

Models software architecture at different levels of abstraction (Context, Container, Component, Code).

```mermaid
C4Context
    Person(user, "Visitor", "Reads site content")
    System(app, "Blazor App", "Serves the website")
    System_Ext(cdn, "CDN", "Delivers static assets")
    Rel(user, app, "Browses")
    Rel(app, cdn, "Fetches assets from")
```

### Mindmap

Visualises hierarchical concepts branching from a central idea.

```mermaid
mindmap
    root((Blazorade Scraibe))
        Content
            Markdown
            Shortcodes
            Mermaid
        Publishing
            HTML generation
            Sitemap
        Components
            Layouts
            ShortCodes
```

### Timeline

Shows events or milestones along a chronological axis.

```mermaid
timeline
    title Blazorade Scraibe milestones
    2022 : Initial release
    2023 : Shortcode system
    2024 : Page layouts
    2026 : Mermaid support
```

### Sankey Diagram

Visualises flow quantities between nodes — useful for showing traffic, energy, or data movement.

```mermaid
sankey-beta

Organic,Visitors,520
Direct,Visitors,280
Social,Visitors,140
Referral,Visitors,60
```

### XY Chart

Renders bar and line charts on XY axes for quantitative data.

```mermaid
xychart-beta
    title "Monthly page views"
    x-axis [Jan, Feb, Mar, Apr, May, Jun]
    y-axis 0 --> 5000
    bar  [1200, 1800, 2400, 3100, 3800, 4200]
    line [1200, 1800, 2400, 3100, 3800, 4200]
```

### Block Diagram

Describes systems as labelled blocks and connections — simpler than flowcharts for high-level architecture sketches.

```mermaid
block-beta
    columns 3
    A["Markdown"] B["Publisher"] C["Static HTML"]
    A --> B --> C
```

### Packet Diagram

Shows network packet structure and bit-field layouts.

```mermaid
packet-beta
    0-7: "Version"
    8-15: "IHL"
    16-31: "Total Length"
```

### Kanban

Visualises work items distributed across workflow stages.

```mermaid
kanban
    todo
        id1["Write release notes"]
        id2["Update screenshots"]
    in-progress
        id3["Mermaid support"]
    done
        id4["Shortcode system"]
```

### Architecture Diagram

Describes infrastructure and service topology with icons for common resource types.

```mermaid
architecture-beta
    service browser(internet)[Browser]
    service cdn(server)[CDN]
    service app(server)[Blazor App]
    browser:R --> L:cdn
    cdn:R --> L:app
```

### Radar Chart

Compares multiple attributes of one or more subjects on a radial axis.

```mermaid
---
title: "Grades"
---
radar-beta
  axis m["Math"], s["Science"], e["English"]
  axis h["History"], g["Geography"], a["Art"]
  curve a["Alice"]{85, 90, 80, 70, 75, 90}
  curve b["Bob"]{70, 75, 85, 80, 90, 85}

  max 100
  min 0
```

### Treemap

Displays hierarchical data as nested rectangles, sized proportionally to a value.

```mermaid
treemap-beta
"Products"
    "Electronics"
        "Phones": 50
        "Computers": 30
        "Accessories": 20
    "Clothing"
        "Men's": 40
        "Women's": 40
```

## Writing Guidelines

- **One diagram per block.** Each fenced `mermaid` block contains exactly one diagram definition.
- **Add a description.** Precede every diagram with a short sentence explaining what it shows — this aids accessibility and makes the page readable even when diagrams cannot be rendered.
- **Keep diagrams focused.** If a diagram becomes hard to read, split it into two or more smaller diagrams, each covering a distinct concern.
- **Validate syntax.** Use the [Mermaid Live Editor](https://mermaid.live/) to preview and validate your diagram before adding it to a content file. Invalid syntax causes the diagram to fail silently at runtime.
- **Avoid very long labels.** Long text in nodes or edges makes diagrams hard to read on small screens. Use concise labels and explain details in surrounding prose.
- **Prefer diagrams over complex tables.** For relationships, flows, and hierarchies, a Mermaid diagram communicates the structure more clearly than a wide Markdown table.

## Example Page

A complete example showing diagram usage in context:

````markdown
---
title: Publish Pipeline
description: How the Blazorade Scraibe publish pipeline processes Markdown into static HTML.
---

# Publish Pipeline

The pipeline reads each Markdown file, processes shortcodes, converts to HTML, and writes
a static bootstrapper to `wwwroot/`. The diagram below shows the key stages.

```mermaid
flowchart LR
    A([.md file]) --> B[Parse frontmatter]
    B --> C[Process shortcodes]
    C --> D[Convert to HTML]
    D --> E[Apply layout template]
    E --> F([.html bootstrapper])
```

After publishing, the Blazor runtime fetches the bootstrapper and composes the final page
by splicing each content part into its layout slot.
````
