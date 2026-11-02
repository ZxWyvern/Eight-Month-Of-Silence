# ADR-0027: Production Main Menu Boot and Menu-Scoped New-Game Reset

## Status

Accepted — implemented and Play-verified on 2026-11-02.

## Date

2026-11-02

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.3.8f1 (6000.3 LTS) + URP 17.3.0 |
| **Domain** | Core / Build / UI |
| **Knowledge Risk** | LOW — `EditorBuildSettings`, `SceneManager.LoadSceneAsync`, `Application.CanStreamedLevelBeLoaded`, and VContainer `LifetimeScope` are stable documented APIs. `docs/engine-reference/` is absent in this repository, so engine facts were checked against Context7 Unity 6000.6 SceneManager/Application docs and the existing ADR-0015 baseline. |
| **References Consulted** | `docs/ARCHITECTURE.md:2` (composition root), `docs/07-ui-ux.md:16` (Pause Menu includes Main Menu), `docs/10-production-scope.md` (MVP), `docs/architecture/adr-0015-build-canonical-punyapandu.md`, `docs/architecture/adr-0018-presentation-infrastructure-full-decoupling.md`, Context7 `/websites/unity_en-us_engine_6000_6` (SceneManager / CanStreamedLevelBeLoaded), Context7 `/hadashia/vcontainer` (MonoBehaviour injection), Unity MCP build-order readback |
| **Post-Cutoff APIs Used** | None beyond the pinned Unity 6 / VContainer APIs already used by the project. |
| **Verification Required** | Built player launches `MainMenu`; `Mulai` clears chapter/house saves then loads `PunyaPandu`; Pause → `Menu Utama` returns to `MainMenu`; no console errors or missing references. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0009 (`presenterRoots` explicit, no scene search), ADR-0018 (`GameLifetimeScope` is the only Presentation composition-root file allowed to import Infrastructure) |
| **Enables** | Production build entry shell; `PunyaPandu` remains the first playable level at index 1; Tkp1/Chapter 1 integration can attach to a real boot path |
| **Blocks** | Tkp1 playable-slice verification — cannot be treated as a production boot flow until this ADR is accepted |
| **Ordering Note** | Supersedes ADR-0015. Build settings must keep `MainMenu` first and enabled; reordering or disabling it reintroduces the pre-menu boot behavior. |

## Context

### Problem Statement

ADR-0015 made `PunyaPandu` build index 0 so a built player would not land in the blank `SampleScene`. That fixed the blank-build symptom but left the production build without a menu: launching the game dropped directly into gameplay, `Mulai`/new-game had no explicit save reset, and the only menu scene in the repository was an archived box-scene menu whose `TutupMata` transition still pointed at `Office`.

`docs/07-ui-ux.md:16` requires a Pause Menu with a Main Menu route, and the MVP needs a repeatable start flow that resets a previous run rather than silently resuming old chapter/house data. The project also requires one composition root per scene and forbids `FindObjectOfType`/`GameObject.Find` discovery in gameplay wiring.

### Constraints

- `ARCH-004`: `Yumiwa.Presentation` may import `Yumiwa.Infrastructure` only in `GameLifetimeScope.cs` (and Editor tooling); a new menu `LifetimeScope` file would either duplicate the exception or require a new ADR.
- `AGENTS.md` §5: no singletons, no `Find*`, no scene YAML hand-editing, no legacy input.
- Save schema is version 4 (`CheckpointData`) and must not change. `ISaveRepository.ClearAll()` deletes only the chapter and house JSON files; settings remain separate.
- The existing `UnitySceneLoader` validates that a scene is in Build Settings before loading.
- The menu UI text is serialized in the scene and also normalized in `MainMenuPresenter` at startup; no localization system exists yet, so localization-ready text is a follow-up rather than a new hardcoded string layer being introduced here.
- The `MainMenu` scene is a single-scene `LifetimeScope`; no persistent parent scope may be introduced without a separate architecture decision.

### Requirements

- Built player starts in `MainMenu`, not gameplay.
- `MainMenu` has `Mulai` and `Keluar`; `Mulai` resets the previous run through the save boundary and loads `PunyaPandu`.
- `Keluar` quits the player (and exits Play Mode in the Editor).
- Pause → `Menu Utama` returns to `MainMenu` with `timeScale=1` and a visible cursor.
- Menu services must be injected through the existing composition root, not discovered at runtime.
- `PunyaPandu` must remain playable at build index 1; `Tkp1` remains a shell at index 2.

## Decision

Adopt `Assets/Scenes/MainMenu.unity` as the production boot scene at build index 0 and keep `Assets/Scenes/PunyaPandu.unity` at index 1, with `Assets/Scenes/Tkp1.unity` at index 2. The old placeholder is archived as `Assets/_Yumiwa/Archive/MainMenuBox.unity`.

`MenuManager` hosts the existing `Yumiwa.Presentation.GameLifetimeScope` with serialized `mainMenuMode=true` and `presenterRoots=[MenuManager]`. In `GameLifetimeScope.Configure`, menu mode registers only:

```csharp
builder.Register<ISaveRepository>(_ => new JsonFileSaveRepository(), Lifetime.Singleton);
builder.Register<ISceneLoader>(_ => new UnitySceneLoader(), Lifetime.Singleton);
return;
```

It returns before gameplay registrations, so the menu scene never builds movement, inventory, mission, or dialogue graphs.

`MainMenuPresenter` is injected through VContainer method injection:

```csharp
[Inject]
public void Construct(ISaveRepository saveRepository, ISceneLoader sceneLoader)
```

`OnStartGame` validates `firstSceneName` with `UnityEngine.Application.CanStreamedLevelBeLoaded`, calls `saveRepository.ClearAll()`, then awaits `sceneLoader.LoadSceneAsync(firstSceneName)`. The scene name is serialized as `PunyaPandu`. `OnQuit` uses `UnityEditor.EditorApplication.isPlaying = false` in the Editor and `UnityEngine.Application.Quit()` in a player.

`PauseMenuPresenter.OnMainMenu` saves settings, sets `Time.timeScale = 1f`, clears pause state, and calls `SceneManager.LoadScene("MainMenu")`.

### Architecture Diagram

```text
Build (StandaloneWindows64)
  [0] MainMenu.unity
        MenuManager
          GameLifetimeScope (mainMenuMode=true, presenterRoots=[MenuManager])
            ISaveRepository -> JsonFileSaveRepository
            ISceneLoader    -> UnitySceneLoader
          MainMenuPresenter
            Mulai  -> CanStreamedLevelBeLoaded("PunyaPandu")
                   -> ISaveRepository.ClearAll()
                   -> ISceneLoader.LoadSceneAsync("PunyaPandu")
            Keluar -> Editor stop / Application.Quit

  [1] PunyaPandu.unity
        GameLifetimeScope (normal gameplay registrations)
        PauseMenuPresenter
          Menu Utama -> SceneManager.LoadScene("MainMenu")

  [2] Tkp1.unity (shell; next implementation slice)
```

### Key Interfaces

- `ISaveRepository.ClearAll()` — existing Domain contract; deletes chapter and house saves only.
- `ISceneLoader.LoadSceneAsync(string sceneName)` — existing Domain/Infrastructure boundary; validates the scene is in Build Settings.
- `MainMenuPresenter.Construct(ISaveRepository, ISceneLoader)` — new presentation injection point; no new Domain interface.
- `PauseMenuPresenter.OnMainMenu()` — existing pause callback route.

## Alternatives Considered

### Alternative 1: Separate bootstrap scene plus a dedicated menu `LifetimeScope`

- **Description**: Add a bootstrap scene at index 0, instantiate a `MainMenuLifetimeScope` there, and load `MainMenu` or `PunyaPandu` from it.
- **Pros**: Explicit boot phase; menu services isolated in their own scope; no `mainMenuMode` switch on the gameplay scope.
- **Cons**: Adds a scene and a second composition root, plus a new Infrastructure import outside `GameLifetimeScope` (ARCH-004 exception churn) for a two-scene MVP.
- **Rejection Reason**: Over-engineering and exception expansion for a menu that needs only two services; KISS favors a scene-local mode branch in the existing root.

### Alternative 2: Keep `PunyaPandu` at index 0 and add the menu additively

- **Description**: Start gameplay and overlay the menu; `Mulai` would hide the overlay.
- **Pros**: No build-order change; menu can query live gameplay state.
- **Cons**: Gameplay services, save loading, and HUDs initialize behind the menu; expensive and semantically wrong for a new-game start; pause/menu ownership becomes ambiguous.
- **Rejection Reason**: Violates the "menu before gameplay" production flow and risks double-saving/loading during boot.

### Alternative 3: Direct `SceneManager` menu with no save reset

- **Description**: Keep the menu as a thin view that calls `SceneManager.LoadScene("PunyaPandu")` directly, without `ISaveRepository`.
- **Pros**: Fewest moving parts; no menu mode branch.
- **Cons**: `Mulai` would resume old chapter/house data instead of starting a new run; bypasses the tested save boundary and makes the reset behavior untestable.
- **Rejection Reason**: Does not satisfy the new-game requirement; the save clear must be explicit and verifiable.

## Consequences

### Positive

- The build configuration now starts in a real production menu instead of immediate gameplay.
- `Mulai` is deterministic: chapter/house saves are cleared before the first playable scene loads.
- Pause → Main Menu is restored, satisfying the UI specification.
- Menu wiring uses explicit VContainer injection and `presenterRoots`; no `FindObjectOfType` or singleton.
- `PunyaPandu` remains the first gameplay scene at index 1; existing gameplay registrations are untouched.
- The menu scene's composition root can evolve independently of gameplay because menu mode returns before gameplay registrations.

### Negative

- `GameLifetimeScope` now has two modes; `mainMenuMode` is a serialized switch that must not be enabled in a gameplay scene.
- `ClearAll()` is destructive by design. If the scene load fails after the clear, the previous run is lost; the pre-load `CanStreamedLevelBeLoaded` check reduces but does not eliminate this.
- The menu adds one scene load to the boot path (small; 475 KB scene file).
- Menu text remains serialized plus presenter-normalized because the project has no localization system; localization-ready text is a follow-up.
- ADR-0015 is now history; build-order discipline is required to keep `MainMenu` at index 0.

### Risks

- **Wrong build order**: someone puts `PunyaPandu` first again. Mitigation: keep ADR-0015 marked superseded; verify `EditorBuildSettings.scenes[0]` in build checks.
- **Menu mode leakage**: `mainMenuMode` enabled on a gameplay scope would leave gameplay presenters without services. Mitigation: menu-only flag is set only on `MenuManager` in `MainMenu`; gameplay scenes leave it false.
- **Save loss on failed load**: pre-validate `CanStreamedLevelBeLoaded`; if load still fails, report the error and leave the menu interactable. A future revision could load into a safe bootstrap before clearing.
- **Scope lifecycle**: the menu's `GameLifetimeScope` is scene-local; crossing into `PunyaPandu` creates a new gameplay scope. Any future persistent state must use a parent scope, not cross-scene resolution.
- **Localization drift**: `docs/09-technical-spec.md` asks for localization-ready UI; this ADR does not introduce a localization layer. Deferred as a separate decision.
- **Standalone build not smoke-tested**: Play Mode confirms the boot flow against `EditorBuildSettings`, but no `StandaloneWindows64` build was launched in this slice. A build smoke test remains required before release sign-off.
- **Engine specialist review unavailable**: the requested Unity specialist subagent could not run (`Model not found`); API validation used Context7 docs plus Play verification instead. This should be rechecked in a fresh reviewer session.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| `07-ui-ux.md` | Pause Menu: Resume, Load Checkpoint, Settings, Main Menu | `PauseMenuPresenter.OnMainMenu` returns to `MainMenu`; settings remain accessible in pause. |
| `07-ui-ux.md` | Settings toggles and menu surfaces | The menu is the production shell; pause settings are preserved and the menu route does not bypass them. |
| `10-production-scope.md` | MVP: one crime scene completable | `Mulai` clears old saves and loads the playable `PunyaPandu` scene; `Tkp1` remains the next shell. |
| `ARCHITECTURE.md:2` | One `LifetimeScope` per scene/composition root | `MainMenu` and `PunyaPandu` each own a scope; menu services are registered only in menu mode. |

## Performance Implications

- **CPU**: Negligible; one menu scene and two synchronous file deletions on Start.
- **Memory**: One additional scene (~475 KB serialized) plus the menu UI; gameplay scenes are unloaded when the menu loads.
- **Load Time**: One extra scene transition from boot to gameplay; no additive loading.
- **Network**: N/A.

## Migration Plan

1. Move the archived menu scene to `Assets/Scenes/MainMenu.unity`; preserve the old placeholder as `Assets/_Yumiwa/Archive/MainMenuBox.unity`.
2. Set `EditorBuildSettings.scenes` to `MainMenu` (0), `PunyaPandu` (1), `Tkp1` (2), all enabled.
3. Add `GameLifetimeScope` to `MenuManager`, set `mainMenuMode=true`, and set `presenterRoots=[MenuManager]`.
4. Add `MainMenuPresenter` and wire `startButton`, `quitButton`, and labels.
5. Add `Menu Utama` to the pause menu and route it to `MainMenu`.
6. No save-schema migration; `ClearAll()` is the existing v4 repository contract.

## Validation Criteria

- `EditorBuildSettings.scenes` readback: `0:MainMenu true`, `1:PunyaPandu true`, `2:Tkp1 true`.
- `MainMenuPresenter` services injected in Play Mode (`saveRepository != null`, `sceneLoader != null`).
- Start with seeded chapter/house saves: both files removed, active scene becomes `PunyaPandu`, no console errors.
- Pause → `Menu Utama`: active scene becomes `MainMenu`, `timeScale=1`, cursor visible.
- Compilation 0 errors; scene missing references 0.
- EditMode regression: 157/158; only `CheckpointLoadEventTests.Load_PublishesCheckpointLoadedEvent_WithSnapshotIds` remains failing (pre-existing baseline).
- Static workflow validator: PASS.
- Final guard Play (port 7891, 2026-11-02): services injected True/True, `CanStreamedLevelBeLoaded("PunyaPandu")` True, active scene `PunyaPandu`, `timeScale=1`, console errors 0.
- Final EditMode job `9ba670976386`: 157/158; the only failure is the pre-existing `CheckpointLoadEventTests` case.
- StandaloneWindows64 build launch smoke test: pending (not run in this slice).

## Related Decisions

- Supersedes ADR-0015 (Build Canonical — PunyaPandu as buildIndex 0).
- ADR-0009 (explicit `presenterRoots`; no scene-search injection).
- ADR-0018 (`GameLifetimeScope` remains the only Presentation composition-root file allowed to import Infrastructure).
- `docs/07-ui-ux.md`, `docs/10-production-scope.md`, `docs/ARCHITECTURE.md`.
