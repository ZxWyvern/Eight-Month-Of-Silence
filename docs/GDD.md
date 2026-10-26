# Game Design Reference — Eight Months of Silence

> This is an **engineering-facing distillation** of `GAME_DESIGN_DOCUMENT.docx` (GDD v1.0), reorganized for implementation use. It is not a replacement for the full document — when a task needs narrative, art-direction, or level-layout detail beyond what's here, read the source GDD directly. Section numbers below map loosely to the source document's sections for cross-reference.

## 1. Identity

| | |
|---|---|
| Title | Eight Months of Silence |
| Genre | Investigation / Psychological & Survival Horror |
| Perspective | First person |
| Visual style | PS1/PS2-inspired 3D |
| Platform | PC (Windows) |
| Target length | 2+ hours |
| Player character | James Forryas (investigator) |
| Antagonist | Adrian Thorne |
| Team | 4 people, final-semester studio project — Sinergasia Studio |

## 2. Premise (for context, not for reproduction in marketing copy)

James investigates a series of murders. A witness (Arudt) saw the killer's face; the trail then goes cold for eight months. The case reopens when Officer Ray spots a suspect matching the sketch and traces him to a house on Route 9 — which turns out to be a prepared trap. The final act is a confrontation with Adrian Thorne. Adrian's full motive is intentionally left unresolved, reserved for a planned sequel — **do not implement content that forecloses that ambiguity** (e.g., don't hardcode backstory exposition beyond what's specified).

## 3. Design Pillars (drive every implementation decision)

1. **Investigation first** — the player is not an action character; most systems exist to support searching and understanding evidence, not combat.
2. **Environmental horror** — fear comes from darkness, sound, restricted information, traps, and uncomfortable space, not jump-scare spam or enemy density.
3. **Linear mystery** — missions gate strictly; no player-directed case order. Mission progression state must be a simple, inspectable linear sequence, not a graph.
4. **Limited information** — Adrian is not shown clearly before the climax. Any system that reveals him (AI vision cones, silhouette rendering, audio cues) must be tuned to *withhold*, not reveal, by default.
5. **Simple mechanics, strong atmosphere** — resist adding mechanical complexity. Engineering effort should go toward level design support, environment systems, audio, and pacing tools rather than new player verbs.

## 4. Core Player Fantasy

Early game: *"I'm a detective trying to understand what happened before it's too late."*
Late game (inside the trap house): *"I'm in the killer's territory now. I have to get out alive and catch him."*

This shift should be legible in system behavior — e.g., investigation systems (journal, evidence linking) dominate early missions; survival systems (traps, limited combat) dominate the final level.

## 5. Core Gameplay Loop

**Main loop (Missions 1–N-1):**
`Explore → Inspect → Discover Evidence → James's Thought (internal monologue trigger) → Follow Clue → Explore New Location → Complete Mission → Report → Receive New Case`

**Final level loop:**
`Explore → Search → Solve Item Puzzle → Avoid Traps → Reach Basement → Locate Adrian → Listen → Aim → Shoot → Survive → End`

These are two distinct state machines. Do not force the final-level loop's combat/survival states into the shared mission-flow system used by earlier missions — see `docs/ARCHITECTURE.md` §Mission System.

## 6. Game Structure

Linear mission sequence (source GDD lists these in order): TKP Gang (crime scene) → Motel → TKP Arudt (Arudt's crime scene) → Gas Station / Route 9 → ... → final mission (trap house / basement / Adrian confrontation). Consult the full GDD for the complete, current mission list before implementing mission-order logic — **treat the mission list itself as data (ScriptableObject sequence), never as a hardcoded enum switch**, since the exact count and order is still subject to design iteration.

## 7. Core Systems (from GDD §51, "Technical Architecture" — Unity, PC Windows target)

```
Game
├── Player          — Movement, Camera, Interaction, Flashlight, Weapon
├── Investigation    — Evidence, Inspection, Dialogue, Journal
├── Mission          — Objectives, Progression, Transition
├── Inventory         — Puzzle Items
├── Puzzle            — Keys, Codes, Item Interaction
├── Horror            — Traps, Audio, Events
├── Enemy             — Adrian
├── Combat            — Aim, Shoot, Reload
└── Save              — Checkpoints
```

This is the authoritative feature/system list for scoping tasks. `docs/ARCHITECTURE.md` maps each of these to a concrete layer (Domain / Application / Infrastructure / Presentation) and Assembly Definition.

## 8. Save / Checkpoint Rules (GDD §52 — read before touching save code)

- Checkpoints exist primarily for the final house level:
  1. Display-case key milestone
  2. Shovel milestone
  3. Basement-access milestone
- Death reloads to the last checkpoint.
- A separate, non-checkpoint save system exists in Chapters 1 and 2 (earlier missions) — **this is a different mechanism from the final-level checkpoint system and must not be unified without a design decision**, since the source document deliberately distinguishes them.

## 9. Accessibility (GDD §53 — not yet finalized, treat as a backlog, not a spec)

Candidate settings, not committed: subtitles, volume control, brightness, mouse sensitivity, key rebinding. When implementing any of these, build them as swappable, data-driven settings (not hardcoded), since the final list is undecided.

## 10. Known Design Risk (GDD §54)

**Risk: "Investigation becomes a walking simulator."** Because the core loop is search → inspect → read → walk, there's a real risk that the moment-to-moment feel is passive. This has engineering implications:
- Evidence/inspection systems should support tight feedback (sound, camera focus, UI response) so interaction feels active, not just a flag flip.
- Pacing-support tooling (e.g., a way for designers to preview mission flow, evidence dependency graphs) is a legitimate and valuable engineering investment given this risk — see backlog candidates in `docs/DECISIONS.md`.

## 11. Production Priorities (GDD §49, condensed)

Rough build order emphasis (confirm current priority list with the team before large scoping decisions): core investigation loop first → mission progression → puzzle/inventory → horror/trap systems → Adrian + combat → presentation polish (PSX rendering, animation, audio). This means early engineering effort should favor Investigation and Mission systems being rock-solid before Combat/Enemy systems are built out.

## 12. When This Document Is Insufficient

For narrative detail, level layout, specific evidence content, dialogue, or asset lists, read `GAME_DESIGN_DOCUMENT.docx` directly rather than guessing — this file intentionally omits content-level detail (specific clues, room layouts, exact dialogue) that changes frequently during production and would go stale here.
