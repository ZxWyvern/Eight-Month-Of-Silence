# Coding Standards — Eight Months of Silence

## 1. Naming

| Element | Convention | Example |
|---|---|---|
| Namespace | `EOS.<Layer>.<Feature>` | `EOS.Application.Investigation` |
| Class / Interface | PascalCase, interfaces prefixed `I` | `EvidenceCollectionService`, `IInteractable` |
| Private field | `_camelCase` | `_currentMission` |
| Serialized private field | `_camelCase` + `[SerializeField]`, never public | `[SerializeField] private float _walkSpeed;` |
| Property | PascalCase | `public MissionId CurrentMission { get; }` |
| Method | PascalCase, verb-first | `CollectEvidence()`, `TryResolvePuzzle()` |
| Async method | suffix `Async` | `LoadSceneAsync()` |
| Event / delegate | PascalCase, past tense for "already happened" | `EvidenceCollected`, `MissionCompleted` |
| ScriptableObject asset file | `SO_<Name>` in project, class name `<Name>Asset` or `<Name>Definition` | `SO_Mission_TKPGang.asset` → `MissionDefinition` |
| Assembly Definition | `EOS.<Layer>[.<Feature>]` | `EOS.Presentation.Horror` |

## 2. File & Folder Rules

- One public type per file. File name matches type name exactly.
- Presentation feature folders mirror the GDD system table in `docs/ARCHITECTURE.md` §4 — don't invent new top-level feature folders without updating that table.
- Test files live in `Tests/EditMode` or `Tests/PlayMode`, mirroring the folder path of the type under test, suffixed `Tests` (`EvidenceCollectionServiceTests.cs`).

## 3. Required Patterns

- **Constructor injection** for Application/Domain classes; `[Inject]` method injection only where VContainer requires it for MonoBehaviours.
- **Interfaces at layer boundaries.** Any Application service that needs disk, network, Addressables, or platform APIs depends on an interface it or Domain defines; Infrastructure implements it.
- **Guard clauses over nested conditionals.** Validate and return/throw early; avoid arrow-code (`if { if { if {`).
- **Immutable value objects** for Domain data that represents "a fact" (an `EvidenceId`, a `CheckpointId`) — `readonly struct` or `sealed record`, not mutable classes with public setters.
- **Explicit state machines** for anything with more than 3 states (mission flow, Adrian's encounter, puzzle state) — a real class with named states and legal-transition checks, not a web of booleans (`isActive`, `isTriggered`, `isDone`).
- **Object pooling** for anything instantiated repeatedly at runtime in a hot path (trap VFX, hit effects, footstep audio sources).

## 4. Forbidden in Code Review (agent must self-check before calling a task done)

- `FindObjectOfType` / `GameObject.Find` outside of one-time editor tooling.
- Public mutable fields on any Domain or Application type (`public int health;`) — use properties with private setters or explicit methods.
- `catch (Exception) { }` (swallowed exceptions) — log or rethrow, always.
- `async void` outside a genuine Unity event entry point.
- Business logic (mission rules, puzzle solution checks, evidence validity) written inside a MonoBehaviour's `Update()` — it belongs in Application/Domain and is *called from* Presentation.
- New singletons (`public static X Instance`) for anything holding mutable gameplay state. A true, justified exception (e.g., a low-level engine bootstrap) must be documented with a comment explaining why DI wasn't viable, and logged in `docs/DECISIONS.md`.
- Copy-pasted logic across two systems that should share an Application service — extract instead.
- Hardcoded tuning numbers (trap damage, puzzle timers, flashlight battery drain) — these belong in a `ScriptableObject` config asset.

## 5. Commit & PR Conventions

Conventional Commits, scoped to the GDD system table when possible:

```
feat(investigation): add evidence-to-location linking rule
fix(save): prevent checkpoint write during scene transition
refactor(mission): extract MissionFlowService from MissionManager
perf(horror): pool trap VFX particle systems
test(puzzle): cover PuzzleResolutionService edge cases for partial solutions
docs(architecture): record ADR for save schema v2
```

One logical change per commit. A commit that touches Domain and Presentation for unrelated reasons should be split.

## 6. Bug Investigation Protocol

When fixing a bug, an agent must record, in the PR description or task response:

1. **Root cause** (not the symptom) — the actual defect in logic, state, or timing.
2. **Reproduction steps** — how it occurs.
3. **Impact** — Low / Medium / High / Critical (Critical = save corruption, crash, or final-encounter softlock).
4. **Fix** — short-term patch applied now, and (if different) the long-term correct fix, with a `docs/DECISIONS.md` entry if the long-term fix is deferred.

Never guess a root cause without evidence (logs, repro, stack trace). If the information needed to find the root cause isn't available, say so explicitly and state what's needed to proceed, rather than shipping a fix for a guessed cause.

## 7. Refactoring Protocol

When asked to refactor:

1. State what's wrong with the current design and why it's harmful (coupling, testability, duplication, violated layer boundary).
2. State severity and the consequence of *not* fixing it.
3. Propose the improved design.
4. Propose a migration strategy (can it be done incrementally, or does it require a breaking change to a save schema, API, or asset format?).
5. Implement, preserving observable behavior unless the task explicitly asked for a behavior change.

Never perform a drive-by rewrite of code unrelated to the current task, even if it's tempting — flag it as a candidate task instead.
