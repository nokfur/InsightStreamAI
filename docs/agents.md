# AI Agent Architecture & Registry

This document defines the multi-agent topology, agent personas, plugins, handoff contracts, and execution lifecycle within **InsightStream AI**.

---

## 1. Multi-Agent Topology Overview

To replace the monolithic single-agent chatbot, InsightStream AI has been upgraded to a **Hierarchical Orchestrator-Subagent multi-agent system** utilizing the Semantic Kernel Agent Framework.

```mermaid
graph TD
    User([User Prompt]) --> Router[RouterAgent]
    Router -->|HandOverToDocumentResearch| DocAgent[DocumentResearchAgent]
    Router -->|HandOverToWorkspaceAutomation| AutoAgent[WorkspaceAutomationAgent]
    Router -->|Direct Response| User
    DocAgent -->|HandOverToRouter| Router
    AutoAgent -->|HandOverToRouter| Router
```

### Agent Roles & Configurations

Each agent runs on a dedicated Kernel instance cloned from the root container, ensuring strict scoping of capabilities (least-privilege tool execution):

| Agent Name | Description | Active Plugins | System Instruction Prompt |
|---|---|---|---|
| **`RouterAgent`** | Central orchestrator evaluating user intent to delegate tasks. | `HandoffPlugin` | Evaluates user input. Routes to `DocumentResearchAgent` for document queries, `WorkspaceAutomationAgent` for time/web queries, or responds directly for general conversation. |
| **`DocumentResearchAgent`** | Specialist in querying local document vector database embeddings. | `DocumentQueryPlugin`, `HandoffPlugin` | Calls `QueryDocumentsAsync` when asked about indexed knowledge/PDFs, summarizes the findings, and hands control back. |
| **`WorkspaceAutomationAgent`** | Specialist in searching the web and checking time. | `WebSearchPlugin`, `TimePlugin`, `HandoffPlugin` | Calls `SearchAsync` or `GetCurrentTime` to execute platform workflows and hands control back. |

---

## 2. Handoff Coordination Strategy

Rather than rigid hardcoded routing or manual prompt parsing, the agent topology relies on a dynamic, tool-driven handoff orchestration.

### The Handoff Plugin (`HandoffPlugin`)
Agents call explicit function tools to yield control to other nodes:
- `HandOverToDocumentResearch()`: Returns `"HANDOFF_TO: DocumentResearchAgent"`.
- `HandOverToWorkspaceAutomation()`: Returns `"HANDOFF_TO: WorkspaceAutomationAgent"`.
- `HandOverToRouter()`: Returns `"HANDOFF_TO: RouterAgent"`.

### Routing & Turn Management
Two custom strategies govern the `AgentGroupChat` turn lifecycle:

1. **`MultiAgentSelectionStrategy`**:
   - Inspects the last message content and tool execution outputs in the group history.
   - If a handoff token (e.g., `HANDOFF_TO: DocumentResearchAgent`) is detected, it selects that agent as the next speaker.
   - Otherwise, defaults to the `RouterAgent`.

2. **`MultiAgentTerminationStrategy`**:
   - Decides when the execution loop terminates.
   - If the `RouterAgent` decides to delegate to a specialist agent (calling a handoff function), the strategy does **not** terminate, allowing the specialist to speak.
   - Once a specialist completes its turn, or if the `RouterAgent` responds directly to the user (e.g., greetings), the strategy terminates execution so control is yielded back to the Blazor chat UI.

---

## 3. Core Agent Plugins

### 1. `DocumentQueryPlugin`
*   **Purpose:** Local semantic retrieval from uploaded documents.
*   **Trigger Function:** `QueryDocumentsAsync(query)`
*   **Mechanism:** Generates vector embeddings for query, performs in-memory cosine similarity against lightweight projections, fetches full matching text chunks from SQLite, and logs source citations.

### 2. `WebSearchPlugin`
*   **Purpose:** DuckDuckGo queries for live context.
*   **Trigger Function:** `SearchAsync(query)`
*   **Mechanism:** Scrapes DuckDuckGo Lite search page or yields simulated snippets if offline.

### 3. `TimePlugin`
*   **Purpose:** Real-time date and clock checks.
*   **Trigger Function:** `GetCurrentTime()`
*   **Mechanism:** Returns C# local datetime.

---

## 4. Execution Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Idle
    Idle --> MessageReceived : User submits prompt
    MessageReceived --> SeedHistory : Load past DB messages into AgentGroupChat
    SeedHistory --> InvokeGroupChat : chat.InvokeStreamingAsync()
    
    state InvokeGroupChat {
        [*] --> RouterSpeak : RouterAgent evaluates prompt
        RouterSpeak --> RouterHandoff : Router calls Handoff tool
        RouterSpeak --> RouterDirect : Router responds directly
        
        RouterHandoff --> SpecialistSpeak : SelectionStrategy switches active agent
        SpecialistSpeak --> SpecialistHandoff : Specialist completes & hands back
        SpecialistHandoff --> RouterSpeak
        
        RouterDirect --> [*] : TerminationStrategy terminates loop
        SpecialistSpeak --> [*] : TerminationStrategy terminates loop
    }

    InvokeGroupChat --> StreamAndFilter : Suppress HANDOFF_TO tokens from output
    StreamAndFilter --> RenderUI : Yield text chunk-by-chunk to Blazor page
    RenderUI --> SaveDB : Persist assistant response to SQLite
    SaveDB --> Idle
```

---

## 5. Citations Tracking
- Matching document excerpts, source document names, match percentages, and page numbers are captured via the `ICitationTracker` during search.
- Citations are saved to the `CitationsJson` database column and rendered as clickable source badges in the Blazor UI.
