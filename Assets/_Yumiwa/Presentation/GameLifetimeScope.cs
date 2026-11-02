using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;
using Yumiwa.Application;
using Yumiwa.Domain;
using Yumiwa.Infrastructure;

namespace Yumiwa.Presentation
{
    /// <summary>Composition root wiring one gameplay scene's movement and stamina graph.</summary>
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private MovementConfig movementConfig;
        [SerializeField] private StaminaConfig staminaConfig;
        [SerializeField] private CensorEffectConfig censorConfig;
        [SerializeField] private InspectionConfig inspectionConfig;
        [SerializeField] private InputActionAsset inputAsset;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private FlashlightConfig flashlightConfig;
        [SerializeField] private List<ItemData> itemDatabase = new List<ItemData>();
        [SerializeField] private List<GameObject> presenterRoots = new List<GameObject>();
        [SerializeField] private bool mainMenuMode;
        [SerializeField] private MissionSequenceAsset missionSequence;
        [SerializeField] private List<PuzzleDefinitionAsset> puzzleAssets = new List<PuzzleDefinitionAsset>();
        [SerializeField] private List<TrapDefinitionAsset> trapAssets = new List<TrapDefinitionAsset>();
        [SerializeField] private List<DialogueLineAsset> dialogueLineAssets = new List<DialogueLineAsset>();
        [SerializeField] private List<ReconstructionCaseAsset> reconstructionAssets = new List<ReconstructionCaseAsset>();
        [SerializeField] private Act1DefinitionAsset act1Definition;
        [SerializeField] private List<SpeakerAsset> speakerAssets = new List<SpeakerAsset>();
        [SerializeField] private List<DialogueSequenceAsset> dialogueSequenceAssets = new List<DialogueSequenceAsset>();

        protected override void Awake()
        {
            UnityEngine.Application.runInBackground = true;
            if (presenterRoots != null && presenterRoots.Count > 0)
            {
                autoInjectGameObjects ??= new List<GameObject>();
                foreach (var presenterRoot in presenterRoots)
                {
                    if (presenterRoot != null && !autoInjectGameObjects.Contains(presenterRoot))
                        autoInjectGameObjects.Add(presenterRoot);
                }
            }
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            if (mainMenuMode)
            {
                builder.Register<ISaveRepository>(_ => new JsonFileSaveRepository(), Lifetime.Singleton);
                builder.Register<ISceneLoader>(_ => new UnitySceneLoader(), Lifetime.Singleton);
                return;
            }

            if (movementConfig == null || staminaConfig == null || inputAsset == null || playerRoot == null)
                throw new InvalidOperationException(
                    "[Yumiwa] GameLifetimeScope references unassigned. Run Yumiwa/Create Movement Test Scene.");
            builder.RegisterInstance(movementConfig);
            builder.RegisterInstance(staminaConfig);
            if (censorConfig != null)
                builder.RegisterInstance(censorConfig);
            builder.RegisterInstance(inputAsset);
            builder.RegisterInstance(itemDatabase ?? new List<ItemData>());

            builder.Register<IConfigProvider>(c =>
            {
                var items = c.Resolve<List<ItemData>>();
                return new DirectConfigProvider(movementConfig, staminaConfig, censorConfig, inspectionConfig, items);
            }, Lifetime.Singleton);
            builder.Register(c => c.Resolve<IConfigProvider>().GetMovement(), Lifetime.Singleton);
            builder.Register(c => c.Resolve<IConfigProvider>().GetStamina(), Lifetime.Singleton);
            builder.Register(c => c.Resolve<IConfigProvider>().GetCensor(), Lifetime.Singleton);
            builder.Register(c => c.Resolve<IConfigProvider>().GetInspection(), Lifetime.Singleton);
            builder.Register<IEventBus, EventBus>(Lifetime.Singleton);
            builder.Register<ITimeSource, UnityTimeSource>(Lifetime.Singleton);
            builder.Register(c =>
            {
                var s = c.Resolve<StaminaConfigData>();
                return new StaminaPool(s.MaxStamina, s.RegenPerSecond, s.RegenDelaySeconds);
            }, Lifetime.Singleton);

            builder.Register<IStaminaService, StaminaService>(Lifetime.Singleton);
            builder.Register<ICensorService, CensorService>(Lifetime.Singleton);
            builder.Register<PlayerMovementState>(Lifetime.Singleton);
            builder.Register<WeaponState>(_ => new WeaponState(7), Lifetime.Singleton); // <!-- TBD#6 -->
            builder.Register<IAmmoReadModel>(c => c.Resolve<WeaponState>(), Lifetime.Singleton);
            builder.Register<CombatService>(Lifetime.Singleton);
            builder.Register<IMovementStateMachine>(c => new MovementStateMachine(
                c.Resolve<PlayerMovementState>(),
                c.Resolve<IStaminaService>(),
                c.Resolve<IAmmoReadModel>(),
                c.Resolve<IEventBus>(),
                c.Resolve<MovementConfigData>()), Lifetime.Singleton);
            builder.Register<ITargetingService, TargetingService>(Lifetime.Singleton);
            builder.Register<IPlayerInput>(_ => new NewInputAdapter(inputAsset), Lifetime.Singleton);
            builder.Register(_ => new InspectionInputHandler(inputAsset), Lifetime.Singleton);
            var waterSensor = new WaterSensor();
            builder.RegisterInstance(waterSensor);
            builder.RegisterInstance<IWaterProvider>(waterSensor);
            builder.Register<IHealthService>(c => new HealthService(bus: c.Resolve<IEventBus>()), Lifetime.Singleton);
            builder.Register<IFlashlightService>(c =>
            {
                if (flashlightConfig != null) return flashlightConfig.CreateService(c.Resolve<IEventBus>());
                return new FlashlightService(c.Resolve<IEventBus>());
            }, Lifetime.Singleton);
            builder.RegisterInstance(new InspectionProgress());
            // Player settings (plan.json persistence): versioned local file, fallback Defaults; independent from CheckpointData
            builder.Register<ISettingsRepository>(_ => new JsonFileSettingsRepository(), Lifetime.Singleton);
            builder.Register<ISettingsStore>(c => (ISettingsStore)c.Resolve<ISettingsRepository>(), Lifetime.Singleton);
            builder.Register(c => c.Resolve<ISettingsRepository>().LoadOrDefaults(), Lifetime.Singleton);

            // ADR-0012: anti soft-lock recovery point
            builder.Register<IRecoveryPoint>(c => new AutoReturnService(c.Resolve<IEventBus>()), Lifetime.Singleton);
            builder.Register<IInventoryService>(c => new PhysicalInventoryService(c.Resolve<IEventBus>(), c.Resolve<IRecoveryPoint>()), Lifetime.Singleton);
            // Evidence-aware inspection needs IEvidenceLog — register before session
            builder.Register<IEvidenceLog, EvidenceLogService>(Lifetime.Singleton);
            builder.Register<InspectionSessionService>(c => new InspectionSessionService(c.Resolve<IInventoryService>(), c.Resolve<IEventBus>(), c.Resolve<IEvidenceLog>()), Lifetime.Singleton);
            builder.Register<ISaveRepository>(_ => new JsonFileSaveRepository(), Lifetime.Singleton);
            builder.Register<IObjectiveService, ObjectiveService>(Lifetime.Singleton);
            if (missionSequence != null)
                builder.RegisterInstance(missionSequence);
            builder.Register(c =>
            {
                IReadOnlyList<MissionDefinition> sequence = missionSequence != null
                    ? missionSequence.GetDefinitions()
                    : Array.Empty<MissionDefinition>();
                return new MissionFlowService(sequence, c.Resolve<IEventBus>());
            }, Lifetime.Singleton);
            builder.Register(c => new Act1ObjectiveChain(
                c.Resolve<IEventBus>(),
                c.Resolve<IObjectiveService>(),
                c.Resolve<MissionFlowService>(),
                c.Resolve<IEvidenceLog>(),
                act1Definition != null ? act1Definition.ToConfig() : default), Lifetime.Singleton);
            builder.Register(c =>
            {
                var cases = new List<ReconstructionCase>();
                if (reconstructionAssets != null)
                {
                    foreach (var asset in reconstructionAssets)
                    {
                        if (asset != null) cases.Add(asset.ToCase());
                    }
                }
                return new ReconstructionService(
                    cases,
                    c.Resolve<IEvidenceLog>(),
                    c.Resolve<InspectionSessionService>(),
                    c.Resolve<IEventBus>());
            }, Lifetime.Singleton);
            builder.Register(c =>
            {
                PuzzleDefinition[] definitions;
                if (puzzleAssets != null && puzzleAssets.Count > 0)
                {
                    var list = new List<PuzzleDefinition>();
                    foreach (var asset in puzzleAssets)
                    {
                        if (asset != null) list.Add(asset.ToDefinition());
                    }
                    definitions = list.ToArray();
                }
                else
                {
                    Debug.LogWarning("[Yumiwa] puzzleAssets empty - using fallback PuzzleDefinition (2347). Create PuzzleDefinitionAsset to make data-driven per docs/09:32.");
                    definitions = new[]
                    {
                        new PuzzleDefinition("puzzle_alley_code", "Alley Code", "Buka gembok gang",
                            "watch_bloody", "crowbar", "2347")
                    };
                }
                return new PuzzleService(definitions, c.Resolve<IEvidenceLog>(), c.Resolve<IInventoryService>(), c.Resolve<IEventBus>());
            }, Lifetime.Singleton);
            builder.Register<ICheckpointService>(c => new CheckpointService(
                c.Resolve<IEventBus>(),
                c.Resolve<ISaveRepository>(),
                c.Resolve<IRecoveryPoint>(),
                c.Resolve<InspectionSessionService>(),
                c.Resolve<IInventoryService>(),
                c.Resolve<IEvidenceLog>(),
                c.Resolve<PuzzleService>(),
                c.Resolve<MissionFlowService>()), Lifetime.Singleton);
            builder.Register(c =>
            {
                TrapDefinition[] definitions;
                if (trapAssets != null && trapAssets.Count > 0)
                {
                    var list = new List<TrapDefinition>();
                    foreach (var asset in trapAssets)
                    {
                        if (asset != null) list.Add(asset.ToDefinition());
                    }
                    definitions = list.ToArray();
                }
                else
                {
                    definitions = new[]
                    {
                        new TrapDefinition("trap_board_01", "Papan Berpaku", 25f, "trap_click", "trap_board")
                    };
                }
                return new TrapService(definitions, c.Resolve<IHealthService>(), c.Resolve<IEventBus>());
            }, Lifetime.Singleton);
            builder.Register(c =>
            {
                var lines = new List<DialogueLine>();
                if (dialogueLineAssets != null)
                {
                    foreach (var asset in dialogueLineAssets)
                    {
                        if (asset == null) continue;
                        var line = asset.ToDefinition();
                        if (line.IsValid) lines.Add(line);
                    }
                }
                var speakers = new List<Speaker>();
                if (speakerAssets != null)
                {
                    foreach (var asset in speakerAssets)
                    {
                        if (asset == null) continue;
                        var speaker = asset.ToDefinition();
                        if (speaker.IsValid) speakers.Add(speaker);
                    }
                }
                var sequences = new List<DialogueSequence>();
                if (dialogueSequenceAssets != null)
                {
                    foreach (var asset in dialogueSequenceAssets)
                    {
                        if (asset == null) continue;
                        var sequence = asset.ToDefinition();
                        if (sequence.IsValid) sequences.Add(sequence);
                    }
                }
                var dialogue = new DialogueQueueService(lines, speakers, sequences,
                    c.Resolve<IEventBus>(), c.Resolve<ITimeSource>());
                dialogue.SetReadingSpeedMultiplier(c.Resolve<SettingsData>().ReadingSpeed.ToMultiplier());
                return dialogue;
            }, Lifetime.Singleton);
            builder.Register<ISceneLoader>(_ => new UnitySceneLoader(), Lifetime.Singleton);
            builder.Register<ISceneTransitionService, SceneTransitionService>(Lifetime.Singleton);
        }

        [ContextMenu("Yumiwa/Apply Movement Config Now")]
        public void ApplyMovementConfigNow()
        {
            if (Container == null)
            {
                UnityEngine.Debug.LogWarning("[Yumiwa] APPLY_BEFORE_CONTAINER_READY");
                return;
            }
            var movement = Container.Resolve<IConfigProvider>().GetMovement();
            Container.Resolve<IEventBus>().Publish(new MovementConfigReloadedEvent(movement));
            UnityEngine.Debug.Log(
                $"[Yumiwa] Movement config reloaded (walk {movement.WalkSpeed}, " +
                $"sprint x{movement.SprintMultiplier}).");
        }

        public void WireSceneReferences(
            MovementConfig movement, StaminaConfig stamina, InputActionAsset asset, Transform player)
        {
            movementConfig = movement;
            staminaConfig = stamina;
            inputAsset = asset;
            playerRoot = player;
        }

        public void WireCensorConfig(CensorEffectConfig censor)
        {
            censorConfig = censor;
        }

        public void WireInspectionConfig(InspectionConfig inspection)
        {
            inspectionConfig = inspection;
        }

        public void WireItemDatabase(List<ItemData> items)
        {
            itemDatabase = items;
        }
    }
}
