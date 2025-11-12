# Terminology Proposal - Visual Diagrams

This document provides visual diagrams to illustrate the proposed terminology changes for Terminal.Gui's `Application.Top` and related APIs.

## Current vs Proposed Terminology

```mermaid
graph TB
    subgraph Current["Current Terminology (Confusing)"]
        A1[Application.Top]
        A2[Application.TopLevels]
        A3[Toplevel class]
        
        A1 -.->|"unclear relationship"| A2
        A1 -.->|"what is 'Top'?"| A3
        
        style A1 fill:#ffcccc
        style A2 fill:#ffcccc
    end
    
    subgraph Proposed["Proposed Terminology (Clear)"]
        B1[Application.Current]
        B2[Application.SessionStack]
        B3[Toplevel class<br/>keep as-is]
        
        B1 -->|"top item in"| B2
        B1 -.->|"returns instance of"| B3
        
        style B1 fill:#ccffcc
        style B2 fill:#ccffcc
        style B3 fill:#ffffcc
    end
    
    Current -.->|"rename to"| Proposed
```

## Application.Current - Stack Relationship

```mermaid
graph TD
    subgraph SessionStack["Application.SessionStack (ConcurrentStack&lt;Toplevel&gt;)"]
        direction TB
        Dialog[Dialog<br/>Modal: true]
        Window[Window<br/>Modal: false]
        MainView[Main Toplevel<br/>Modal: false]
        
        Dialog -->|"on top of"| Window
        Window -->|"on top of"| MainView
    end
    
    Current[Application.Current] -->|"returns top of stack"| Dialog
    
    style Current fill:#ccffcc,stroke:#339933,stroke-width:3px
    style Dialog fill:#ffd6cc,stroke:#ff6633,stroke-width:2px
    style Window fill:#cce6ff
    style MainView fill:#cce6ff
```

## Before: Confusing Naming Pattern

```mermaid
sequenceDiagram
    participant Dev as Developer
    participant Code as Code
    participant API as Application API
    
    Dev->>Code: Application.Top?
    Code->>Dev: 🤔 Top of what?
    
    Dev->>Code: Application.TopLevels?
    Code->>Dev: 🤔 How does Top relate to TopLevels?
    
    Dev->>Code: Is this the topmost view?
    Code->>Dev: 🤔 Or the currently running one?
    
    Note over Dev,API: Requires documentation lookup
    Dev->>API: Read docs...
    API->>Dev: Top = currently active view
```

## After: Self-Documenting Pattern

```mermaid
sequenceDiagram
    participant Dev as Developer
    participant Code as Code
    participant API as Application API
    
    Dev->>Code: Application.Current?
    Code->>Dev: ✓ Obviously the current view!
    
    Dev->>Code: Application.SessionStack?
    Code->>Dev: ✓ Stack of running views!
    
    Dev->>Code: Current from SessionStack?
    Code->>Dev: ✓ Top item in the stack!
    
    Note over Dev,API: Self-documenting, no docs needed
```

## .NET Pattern Consistency

```mermaid
graph LR
    subgraph NET[".NET Framework Patterns"]
        T1[Thread.CurrentThread]
        T2[HttpContext.Current]
        T3[SynchronizationContext.Current]
    end
    
    subgraph TG["Terminal.Gui (Proposed)"]
        T4[Application.Current]
    end
    
    NET -->|"follows established pattern"| TG
    
    style T4 fill:#ccffcc,stroke:#339933,stroke-width:3px
    style T1 fill:#e6f3ff
    style T2 fill:#e6f3ff
    style T3 fill:#e6f3ff
```

## View Hierarchy and Run Stack

```mermaid
graph TB
    subgraph ViewTree["View Hierarchy (SuperView/SubView)"]
        direction TB
        Top[Application.Current<br/>Window]
        Menu[MenuBar]
        Status[StatusBar]
        Content[Content View]
        Button1[Button]
        Button2[Button]
        
        Top --> Menu
        Top --> Status
        Top --> Content
        Content --> Button1
        Content --> Button2
    end
    
    subgraph Stack["Application.SessionStack"]
        direction TB
        S1[Window<br/>Currently Active]
        S2[Previous Toplevel<br/>Waiting]
        S3[Base Toplevel<br/>Waiting]
        
        S1 -.-> S2 -.-> S3
    end
    
    Top -.->|"same instance"| S1
    
    style Top fill:#ccffcc,stroke:#339933,stroke-width:3px
    style S1 fill:#ccffcc,stroke:#339933,stroke-width:3px
```

## Usage Example Flow

```mermaid
sequenceDiagram
    participant App as Application
    participant Main as Main Window
    participant Dialog as Dialog
    
    Note over App: Initially empty SessionStack
    
    App->>Main: Run(mainWindow)
    activate Main
    Note over App: SessionStack: [Main]<br/>Current: Main
    
    Main->>Dialog: Run(dialog)
    activate Dialog
    Note over App: SessionStack: [Dialog, Main]<br/>Current: Dialog
    
    Dialog->>App: RequestStop()
    deactivate Dialog
    Note over App: SessionStack: [Main]<br/>Current: Main
    
    Main->>App: RequestStop()
    deactivate Main
    Note over App: SessionStack: []<br/>Current: null
```

## Terminology Evolution Path

```mermaid
graph LR
    subgraph Current["v2 Current State"]
        C1[Application.Top]
        C2[Application.TopLevels]
        C3[Toplevel class]
    end
    
    subgraph Phase1["Phase 1: Add New APIs"]
        P1[Application.Current]
        P2[Application.SessionStack]
        P3[Toplevel class]
        P1O["Application.Top<br/>[Obsolete]"]
        P2O["Application.TopLevels<br/>[Obsolete]"]
    end
    
    subgraph Phase2["Phase 2-4: Migration"]
        M1[Application.Current]
        M2[Application.SessionStack]
        M3[Toplevel class]
        M1O["Application.Top<br/>[Obsolete Warning]"]
        M2O["Application.TopLevels<br/>[Obsolete Warning]"]
    end
    
    subgraph Future["Phase 5: Future State"]
        F1[Application.Current]
        F2[Application.SessionStack]
        F3["IRunnable interface"]
        F4["Toplevel : IRunnable"]
    end
    
    Current --> Phase1
    Phase1 --> Phase2
    Phase2 --> Future
    
    style P1 fill:#ccffcc
    style P2 fill:#ccffcc
    style M1 fill:#ccffcc
    style M2 fill:#ccffcc
    style F1 fill:#ccffcc
    style F2 fill:#ccffcc
    style F3 fill:#ffffcc
    style P1O fill:#ffcccc
    style P2O fill:#ffcccc
    style M1O fill:#ffcccc
    style M2O fill:#ffcccc
```

## Comparison: Code Clarity

```mermaid
graph TB
    subgraph Before["Before: Application.Top"]
        B1["var top = Application.Top;"]
        B2{"What is 'Top'?"}
        B3["Read documentation"]
        B4["Understand: currently active view"]
        
        B1 --> B2 --> B3 --> B4
    end
    
    subgraph After["After: Application.Current"]
        A1["var current = Application.Current;"]
        A2["✓ Immediately clear:<br/>currently active view"]
        
        A1 --> A2
    end
    
    Before -.->|"improved to"| After
    
    style B2 fill:#ffcccc
    style B3 fill:#ffcccc
    style A2 fill:#ccffcc
```

## Migration Phases Overview

```mermaid
gantt
    title Migration Timeline
    dateFormat YYYY-MM
    section API
    Add new APIs (backward compatible)           :done, phase1, 2024-01, 1M
    Update documentation                          :active, phase2, 2024-02, 1M
    Refactor internal code                        :phase3, 2024-03, 2M
    Enable deprecation warnings                   :phase4, 2024-05, 1M
    Remove deprecated APIs (major version)        :phase5, 2025-01, 1M
    
    section IRunnable Evolution
    Design IRunnable interface                    :future1, 2024-06, 3M
    Implement IRunnable                           :future2, 2024-09, 3M
    Migrate to IRunnable                          :future3, 2025-01, 6M
```

## Key Benefits Visualization

```mermaid
mindmap
  root((Application.Current<br/>& SessionStack))
    Clarity
      Self-documenting
      No ambiguity
      Immediate understanding
    Consistency
      Follows .NET patterns
      Thread.CurrentThread
      HttpContext.Current
    Maintainability
      Easier for new developers
      Less documentation needed
      Reduced cognitive load
    Future-proof
      Works with IRunnable
      Supports evolution
      Non-breaking changes
    Migration
      Backward compatible
      Gradual deprecation
      Clear upgrade path
```

## Summary

These diagrams illustrate:

1. **Clear Relationships**: The new terminology makes the relationship between `Current` and `SessionStack` obvious
2. **Self-Documenting**: Names that immediately convey their purpose without documentation
3. **.NET Alignment**: Consistency with established .NET framework patterns
4. **Migration Safety**: Backward-compatible approach with clear phases
5. **Future-Proof**: Supports evolution toward `IRunnable` interface

The proposed terminology (`Application.Current` and `Application.SessionStack`) provides immediate clarity while maintaining compatibility and supporting future architectural improvements.

---

**See also:**
- [terminology-proposal.md](terminology-proposal.md) - Complete detailed proposal
- [terminology-proposal-summary.md](terminology-proposal-summary.md) - Quick reference
- [terminology-before-after.md](terminology-before-after.md) - Code comparison examples
- [terminology-index.md](terminology-index.md) - Navigation guide
