using UnityEngine;
using TMPro;
using VContainer;
using Yumiwa.Application;
using Yumiwa.Domain;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Surface footstep cue (MVP-01, docs/08): cadence from FootstepService,
    /// surface from a downward raycast, subtitle text as the always-readable
    /// cue. Audio clips are optional: generic Starter Assets clips are assigned
    /// as placeholders until surface-specific SFX exist.
    /// One raycast per frame, no per-frame allocation.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FootstepPresenter : MonoBehaviour
    {
        [Tooltip("Material/object-name substring → spoken surface id. Parallel arrays.")]
        [SerializeField] private string[] materialKeys = new[]
        {
            "prototypemap_ground", "ground", "asphalt", "wet", "wood", "concrete", "stone"
        };
        [SerializeField] private string[] surfaceIds = new[]
        {
            "tanah", "tanah", "aspal basah", "aspal basah", "lantai kayu", "beton", "beton"
        };
        [SerializeField] private string defaultSurfaceId = "tanah";
        [Tooltip("Optional fallback step clip when no surface clip is assigned.")]
        [SerializeField] private AudioClip stepClip;
        [Tooltip("Parallel to surfaceIds. Null entries fall back to stepClip.")]
        [SerializeField] private AudioClip[] surfaceClips = new AudioClip[0];
        [Range(0f, 1f)]
        [SerializeField] private float stepVolume = 0.55f;

        private CharacterController controller;
        private IMovementStateMachine machine;
        private IEventBus bus;
        private AudioSource stepAudio;
        private FootstepService steps;
        private TMP_Text subtitleText;
        private float subtitleTimer;
        private bool subtitlesEnabled = true;

        [Inject]
        public void Construct(IEventBus bus, IMovementStateMachine machine)
        {
            this.bus = bus;
            this.machine = machine;
        }

        public void ApplySubtitles(bool enabled)
        {
            subtitlesEnabled = enabled;
            if (!enabled && subtitleText != null) subtitleText.text = "";
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            steps = new FootstepService();
            stepAudio = GetComponent<AudioSource>();
            if (stepAudio == null) stepAudio = gameObject.AddComponent<AudioSource>();
            stepAudio.playOnAwake = false;
            stepAudio.loop = false;
            stepAudio.spatialBlend = 0f;
            stepAudio.volume = 1f;
            BuildSubtitle();
        }

        private void Update()
        {
            if (controller == null || machine == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return; // paused: no cadence, no subtitle decay
            float speed = new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude;
            bool sprinting = machine.Current == MovementState.Sprint;
            bool crouching = machine.Current == MovementState.Crouch;
            if (steps.Tick(dt, speed, controller.isGrounded, sprinting, crouching))
                OnStep(sprinting);
            if (subtitleTimer > 0f)
            {
                subtitleTimer -= dt;
                if (subtitleTimer <= 0f && subtitleText != null) subtitleText.text = "";
            }
        }

        private void OnStep(bool sprinting)
        {
            string surface = DetectSurface();
            bus?.Publish(new FootstepEvent(surface, sprinting));
            var clip = ResolveClip(surface);
            if (clip != null) stepAudio.PlayOneShot(clip, stepVolume);
            if (subtitlesEnabled && subtitleText != null)
            {
                subtitleText.text = "Langkah: " + surface;
                subtitleTimer = 1.2f;
            }
        }

        private AudioClip ResolveClip(string surface)
        {
            if (surfaceClips != null && surfaceIds != null)
            {
                for (int i = 0; i < surfaceIds.Length && i < surfaceClips.Length; i++)
                {
                    if (surfaceIds[i] == surface && surfaceClips[i] != null)
                        return surfaceClips[i];
                }
            }
            if (stepClip != null) return stepClip;
            return surfaceClips != null && surfaceClips.Length > 0 ? surfaceClips[0] : null;
        }

        private string DetectSurface()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1.6f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return defaultSurfaceId;
            var col = hit.collider;
            string mat = col != null && col.sharedMaterial != null ? col.sharedMaterial.name : "";
            string obj = col != null ? col.gameObject.name : "";
            string hay = (mat + " " + obj).ToLowerInvariant();
            for (int i = 0; i < materialKeys.Length && i < surfaceIds.Length; i++)
            {
                if (!string.IsNullOrEmpty(materialKeys[i])
                    && hay.Contains(materialKeys[i].ToLowerInvariant()))
                    return surfaceIds[i];
            }
            return defaultSurfaceId;
        }

        private void BuildSubtitle()
        {
            var canvasGO = new GameObject("FootstepCanvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var go = new GameObject("FootstepSubtitle");
            go.transform.SetParent(canvasGO.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(600, 30);
            rt.anchoredPosition = new Vector2(0, 90);
            subtitleText = go.AddComponent<TextMeshProUGUI>();
            subtitleText.fontSize = 16;
            subtitleText.alignment = TextAlignmentOptions.Center;
            subtitleText.color = new Color(1f, 1f, 1f, 0.85f);
            subtitleText.text = "";
        }
    }
}
