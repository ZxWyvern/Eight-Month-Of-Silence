using UnityEngine;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>Thin presenter mapping input and state to momentum-based CharacterController motion. Split via partial for SIZE-001.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed partial class PlayerMovementPresenter : MonoBehaviour
    {
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private Transform orbitPivot;
        [SerializeField] private Camera playerCamera;

        private CharacterController controller;
        private IMovementStateMachine machine;
        private IStaminaService stamina;
        private IPlayerInput input;
        private IWaterProvider water;
        private IEventBus bus;
        private System.Action<MovementConfigReloadedEvent> configReloadHandler;
        private System.Action<InspectionStartedEvent> inspectionStartedHandler;
        private System.Action<InspectionEndedEvent> inspectionEndedHandler;
        private System.Action<InventoryOpenedEvent> inventoryOpenedHandler;
        private System.Action<InventoryClosedEvent> inventoryClosedHandler;
        private System.Action<PauseOpenedEvent> pauseOpenedHandler;
        private System.Action<PauseClosedEvent> pauseClosedHandler;
        private System.Action<ReconstructionOpenedEvent> reconstructionOpenedHandler;
        private System.Action<ReconstructionClosedEvent> reconstructionClosedHandler;
        private System.Action<PlayerWoundedEvent> woundedHandler;
        private System.Action<PlayerHealedEvent> healedHandler;
        private bool controlsLocked;
        private IHealthService health;
        private bool sprinting;
        private bool crouching;
        private float cameraPitch;
        private bool wounded;
        private MovementConfigData movement;
        private StaminaConfigData staminaCfg;
        private SettingsData settings = SettingsData.Defaults();

        private Vector3 planarVelocity;
        private float verticalVelocity;

        private float coyoteTimer;
        private float jumpBufferTimer;
        private float airMaxSpeed;
        private float fallPeak;
        private bool wasGrounded = true;

        private float baseFov = 75f;
        private float bobPhase;
        private float dipOffset;
        private float dipVelocity;
        private float shakeTimer;
        private float landPunch;

        [Inject]
        public void Construct(
            IMovementStateMachine machine,
            IStaminaService stamina,
            IPlayerInput input,
            IWaterProvider water,
            IEventBus bus,
            IHealthService health,
            MovementConfigData movement,
            StaminaConfigData staminaCfg)
        {
            this.machine = machine;
            this.stamina = stamina;
            this.input = input;
            this.water = water;
            this.bus = bus;
            this.health = health;
            this.movement = movement;
            this.staminaCfg = staminaCfg;
            if (health != null)
            {
                woundedHandler = _ => wounded = true;
                healedHandler = e => wounded = false;
            }
        }

        public void ApplySettings(SettingsData s)
        {
            settings = s;
            // MVP-01: the settings FOV is authoritative for the base (boosts still compose on top).
            // Guard invalid legacy data: a zero FOV would zoom the camera shut on the first run.
            if (s.Fov >= 1f && s.Fov <= 179f)
                baseFov = s.Fov;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
                baseFov = playerCamera.fieldOfView;
        }

        public void WireCamera(Transform target, Transform pivot)
        {
            cameraTarget = target;
            orbitPivot = pivot;
        }

        public void ApplyMovementConfig(MovementConfigData data)
        {
            movement = data;
        }

        private bool subscribed;

        private void Start()
        {
            if (input == null || bus == null || machine == null || stamina == null)
            {
                UnityEngine.Debug.LogError("[Yumiwa] PlayerMovementPresenter injection missing.", this);
                enabled = false;
                return;
            }
            LockCursor();
            SubscribeOnce();
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
                if (playerCamera != null)
                    baseFov = playerCamera.fieldOfView;
            }
        }

        private void OnEnable()
        {
            SubscribeOnce();
        }

        private void SubscribeOnce()
        {
            if (subscribed)
                return;
            if (input == null || bus == null || machine == null || stamina == null)
                return;
            input.Enable();
            input.JumpPressed += OnJumpPressed;
            input.JumpReleased += OnJumpReleased;
            input.CrouchPressed += OnCrouchPressed;
            if (configReloadHandler == null)
                configReloadHandler = OnConfigReloaded;
            bus.Subscribe(configReloadHandler);
            if (inspectionStartedHandler == null)
                inspectionStartedHandler = _ => controlsLocked = true;
            if (inspectionEndedHandler == null)
                inspectionEndedHandler = _ => controlsLocked = false;
            bus.Subscribe(inspectionStartedHandler);
            bus.Subscribe(inspectionEndedHandler);
            if (inventoryOpenedHandler == null)
                inventoryOpenedHandler = _ => controlsLocked = true;
            if (inventoryClosedHandler == null)
                inventoryClosedHandler = _ => controlsLocked = false;
            bus.Subscribe(inventoryOpenedHandler);
            bus.Subscribe(inventoryClosedHandler);
            // MVP-01: pause is modal — freeze look/motion while the menu owns the cursor.
            if (pauseOpenedHandler == null)
                pauseOpenedHandler = _ => controlsLocked = true;
            if (pauseClosedHandler == null)
                pauseClosedHandler = _ => controlsLocked = false;
            bus.Subscribe(pauseOpenedHandler);
            bus.Subscribe(pauseClosedHandler);
            // B1-03: the reconstruction board is modal while the player concludes.
            if (reconstructionOpenedHandler == null)
                reconstructionOpenedHandler = _ => controlsLocked = true;
            if (reconstructionClosedHandler == null)
                reconstructionClosedHandler = _ => controlsLocked = false;
            bus.Subscribe(reconstructionOpenedHandler);
            bus.Subscribe(reconstructionClosedHandler);
            if (health != null)
            {
                if (woundedHandler == null)
                    woundedHandler = _ => wounded = true;
                if (healedHandler == null)
                    healedHandler = e => wounded = false;
                health.OnWounded += woundedHandler;
                health.OnHealed += healedHandler;
            }
            subscribed = true;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (input == null)
                return;
            input.JumpPressed -= OnJumpPressed;
            input.JumpReleased -= OnJumpReleased;
            input.CrouchPressed -= OnCrouchPressed;
            if (bus != null && configReloadHandler != null)
                bus.Unsubscribe(configReloadHandler);
            if (bus != null)
            {
                if (inspectionStartedHandler != null) bus.Unsubscribe(inspectionStartedHandler);
                if (inspectionEndedHandler != null) bus.Unsubscribe(inspectionEndedHandler);
                if (inventoryOpenedHandler != null) bus.Unsubscribe(inventoryOpenedHandler);
                if (inventoryClosedHandler != null) bus.Unsubscribe(inventoryClosedHandler);
                if (pauseOpenedHandler != null) bus.Unsubscribe(pauseOpenedHandler);
                if (pauseClosedHandler != null) bus.Unsubscribe(pauseClosedHandler);
                if (reconstructionOpenedHandler != null) bus.Unsubscribe(reconstructionOpenedHandler);
                if (reconstructionClosedHandler != null) bus.Unsubscribe(reconstructionClosedHandler);
            }
            if (health != null)
            {
                if (woundedHandler != null) health.OnWounded -= woundedHandler;
                if (healedHandler != null) health.OnHealed -= healedHandler;
            }
            input.Disable();
            subscribed = false;
        }

        private void Update()
        {
            if (machine == null)
            {
                UnityEngine.Debug.LogWarning("[Yumiwa] PRESENTER_UNINJECTED");
                enabled = false;
                return;
            }
            if (controlsLocked)
                return;
            if (Cursor.lockState != CursorLockMode.Locked && IsClickPressed())
                LockCursor();
            float dt = Time.deltaTime;
            bool grounded = controller.isGrounded;
            if (!grounded && wasGrounded && TrySlopeSnap())
                grounded = true;

            HandleLandingTransition(grounded, dt);
            machine.SetGrounded(grounded);

            if (orbitPivot != null)
                orbitPivot.position = transform.position;
            ApplyLook(dt);
            machine.Tick(dt);
            UpdateSprint(dt, grounded);
            UpdateJump(dt, grounded);
            ApplyMovement(dt, grounded);
            ApplyCameraPose(dt, grounded);

            wasGrounded = grounded;
        }
    }
}
