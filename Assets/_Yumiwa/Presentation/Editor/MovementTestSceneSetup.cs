using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Yumiwa.Infrastructure;
using Yumiwa.Presentation;

namespace Yumiwa.Presentation.Editor
{
    /// <summary>One-click builder for the movement and stamina test scene.</summary>
    public static class MovementTestSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/YumiwaMovementTest.unity";
        private const string ConfigFolder = "Assets/Yumiwa/Configs";

        [MenuItem("Yumiwa/Create Movement Test Scene")]
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var movement = GetOrCreateConfig<MovementConfig>("MovementConfig.asset");
            var stamina = GetOrCreateConfig<StaminaConfig>("StaminaConfig.asset");
            var inspection = GetOrCreateConfig<InspectionConfig>("InspectionConfig.asset");
            var censor = GetOrCreateConfig<CensorEffectConfig>("CensorEffectConfig.asset");
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (inputAsset == null)
            {
                Debug.LogError("[Yumiwa] Missing Assets/InputSystem_Actions.inputactions (needs Player + Inspection maps).");
                return;
            }

            GameObject player = BuildPlayer(out Transform cameraTarget, movement.StandEyeHeight);
            AttachMainCamera(cameraTarget);
            BuildGround();
            BuildEventSystem();
            GameObject hud = BuildHud();
            var inspectableData = GetOrCreateSampleItem();
            // Inspection rig + UI first so the interactor can reference the controller.
            var rig = BuildInspectionRig(inspection != null ? inspection.InspectionLayerName : "Inspection");
            var ui = BuildInspectionUI(rig.GetComponent<ItemInspectionController>());
            BuildPlayerInteractor(player, rig.GetComponent<ItemInspectionController>());
            BuildSampleInspectable(player.transform, inspectableData);
            var injectTargets = new System.Collections.Generic.List<GameObject> { player, hud, rig, ui };
            BuildScope(movement, stamina, inputAsset, player.transform, player.GetComponent<PlayerMovementPresenter>(), cameraTarget,
                null, injectTargets, censor, inspection);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Yumiwa] Movement test scene saved to {ScenePath} (with inspection rig + sample pickup)");
        }

        private static T GetOrCreateConfig<T>(string fileName) where T : ScriptableObject
        {
            if (!AssetDatabase.IsValidFolder("Assets/Yumiwa"))
                AssetDatabase.CreateFolder("Assets", "Yumiwa");
            if (!AssetDatabase.IsValidFolder(ConfigFolder))
                AssetDatabase.CreateFolder("Assets/Yumiwa", "Configs");
            string path = $"{ConfigFolder}/{fileName}";
            var config = AssetDatabase.LoadAssetAtPath<T>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(config, path);
            }
            return config;
        }

        private static GameObject BuildPlayer(out Transform cameraTarget, float eyeHeight)
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "YumiwaPlayer";
            Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            var mesh = player.GetComponent<MeshRenderer>();
            if (mesh != null)
                mesh.enabled = false;
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.4f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.3f;
            controller.skinWidth = 0.08f;
            player.AddComponent<PlayerMovementPresenter>();
            player.transform.position = new Vector3(0f, 0.1f, -6f);

            var target = new GameObject("CameraTarget");
            target.transform.SetParent(player.transform);
            target.transform.localPosition = new Vector3(0f, eyeHeight, 0f);
            cameraTarget = target.transform;
            return player;
        }

        private static void AttachMainCamera(Transform cameraTarget)
        {
            var main = Camera.main;
            if (main == null)
                main = new GameObject("Main Camera").AddComponent<Camera>();
            main.tag = "MainCamera";
            main.transform.SetParent(cameraTarget, false);
            main.transform.localPosition = Vector3.zero;
            main.transform.localEulerAngles = Vector3.zero;
            main.nearClipPlane = 0.05f;
        }

        private static void BuildGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(40f, 1f, 40f);
        }

        private static GameObject BuildHud()
        {
            var canvasObject = new GameObject("StaminaHud");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<StaminaHudPresenter>();

            var sliderObject = new GameObject("StaminaSlider");
            sliderObject.transform.SetParent(canvasObject.transform);
            var slider = sliderObject.AddComponent<Slider>();
            slider.maxValue = 1f;
            slider.value = 1f;
            var rect = slider.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.05f);
            rect.anchorMax = new Vector2(0.9f, 0.1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(sliderObject.transform);
            var backgroundImage = backgroundObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            var backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            var fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(sliderObject.transform);
            var fillImage = fillObject.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.9f, 0.3f, 1f);
            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            slider.fillRect = fillRect;
            slider.targetGraphic = fillImage;

            var labelObject = new GameObject("StateLabel");
            labelObject.transform.SetParent(canvasObject.transform);
            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = "Idle";
            label.font = Resources.GetBuiltinResource<TMP_FontAsset>("LegacyRuntime.ttf");
            label.fontSize = 28;
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.1f, 0.88f);
            labelRect.anchorMax = new Vector2(0.9f, 0.96f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            return canvasObject;
        }

        private static void BuildScope(
            MovementConfig movement, StaminaConfig stamina, InputActionAsset inputAsset,
            Transform playerRoot, PlayerMovementPresenter presenter,
            Transform cameraTarget, Transform orbitPivot, System.Collections.Generic.List<GameObject> injectTargets,
            CensorEffectConfig censor = null, InspectionConfig inspection = null)
        {
            var scopeObject = new GameObject("GameScope");
            var scope = scopeObject.AddComponent<GameLifetimeScope>();
            scope.WireSceneReferences(movement, stamina, inputAsset, playerRoot);
            if (censor != null)
                scope.WireCensorConfig(censor);
            if (inspection != null)
                scope.WireInspectionConfig(inspection);
            // NOTE: GameLifetimeScope.Awake auto-discovers Player/Interactor/Inspection
            // components via FindFirstObjectByType, so no manual autoInject list needed here.

            presenter.WireCamera(cameraTarget, orbitPivot);
        }

        private static ItemData GetOrCreateSampleItem()
        {
            const string path = "Assets/_Yumiwa/Configs/BrassKey.asset";
            var data = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (data == null)
            {
                string alt = $"{ConfigFolder}/BrassKey.asset";
                data = AssetDatabase.LoadAssetAtPath<ItemData>(alt);
            }
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<ItemData>();
                data.ItemId = "brass_key";
                data.DisplayName = "Brass Key";
                data.Description = "Small brass key, warm to the touch.";
                AssetDatabase.CreateAsset(data, $"{ConfigFolder}/BrassKey.asset");
            }
            return data;
        }

        /// <summary>Tanpa ini tombol UI Take/Back tidak bisa diklik.</summary>
        private static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        private static void BuildPlayerInteractor(GameObject player, ItemInspectionController controller)
        {
            var interactor = player.GetComponent<PlayerInteractor>();
            if (interactor == null)
                interactor = player.AddComponent<PlayerInteractor>();
            var so = new SerializedObject(interactor);
            so.FindProperty("interactRadius").floatValue = 2.5f;
            so.FindProperty("inspection").objectReferenceValue = controller;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildSampleInspectable(Transform player, ItemData data)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "SampleBrassKey";
            go.transform.position = player.position + player.forward * 1.6f + Vector3.up * 0.5f;
            go.transform.localScale = new Vector3(0.14f, 0.08f, 0.38f);
            var item = go.AddComponent<WorldInspectableItem>();
            var so = new SerializedObject(item);
            so.FindProperty("data").objectReferenceValue = data;
            so.FindProperty("destroyOnTake").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject BuildInspectionRig(string layerName)
        {
            var rig = new GameObject("InspectionRig");
            rig.transform.position = new Vector3(0f, 1.4f, 2f);

            var pivot = new GameObject("InspectionRoot");
            pivot.transform.SetParent(rig.transform, false);

            var camGo = new GameObject("InspectionCamera");
            camGo.transform.SetParent(rig.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, 0.9f);
            var cam = camGo.AddComponent<Camera>();
            cam.enabled = false; // hanya nyala saat inspection (via ItemInspectionController)
            cam.clearFlags = CameraClearFlags.Depth;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f, 1f);
            cam.depth = 0f;
            cam.nearClipPlane = 0.05f;
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
                cam.cullingMask = 1 << layer;

            var lightGo = new GameObject("InspectionLight");
            lightGo.transform.SetParent(rig.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.4f, 0.6f);
            var light = lightGo.AddComponent<Light>();
            light.enabled = false; // presenter menyalakan hanya saat item ditampilkan
            light.type = LightType.Spot;
            light.intensity = 1.4f;
            light.range = 6f;

            var presenter = rig.AddComponent<ItemInspectionPresenter>();
            var pso = new SerializedObject(presenter);
            pso.FindProperty("inspectionRoot").objectReferenceValue = pivot.transform;
            pso.FindProperty("inspectionLight").objectReferenceValue = light;
            pso.FindProperty("inspectionLayerName").stringValue = layerName;
            pso.ApplyModifiedPropertiesWithoutUndo();

            var rotator = rig.AddComponent<ItemInspectionRotator>();
            var rso = new SerializedObject(rotator);
            rso.FindProperty("inspectionCamera").objectReferenceValue = cam;
            rso.FindProperty("pivot").objectReferenceValue = pivot.transform;
            rso.ApplyModifiedPropertiesWithoutUndo();

            rig.AddComponent<ItemInspectionController>();
            var cso = new SerializedObject(rig.GetComponent<ItemInspectionController>());
            cso.FindProperty("presenter").objectReferenceValue = presenter;
            cso.FindProperty("rotator").objectReferenceValue = rotator;
            // UI is created next and back-wired by BuildInspectionUI.
            cso.ApplyModifiedPropertiesWithoutUndo();
            return rig;
        }

        private static GameObject BuildInspectionUI(ItemInspectionController controller)
        {
            var canvasGo = new GameObject("InspectionUI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            var ui = canvasGo.AddComponent<ItemInspectionUI>();

            var panel = new GameObject("DimPanel");
            panel.transform.SetParent(canvasGo.transform, false);
            var img = panel.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.6f);
            Stretch((RectTransform)panel.transform);

            TMP_Text name = MakeLabel(canvasGo.transform, "NameText", 40, new Vector2(0.1f, 0.84f), new Vector2(0.9f, 0.92f));
            TMP_Text desc = MakeLabel(canvasGo.transform, "DescText", 26, new Vector2(0.15f, 0.74f), new Vector2(0.85f, 0.82f));
            TMP_Text msg = MakeLabel(canvasGo.transform, "MessageText", 28, new Vector2(0.2f, 0.22f), new Vector2(0.8f, 0.3f));
            msg.color = Color.yellow;
            Button take = MakeButton(canvasGo.transform, "Take", "Take (E)", new Vector2(0.28f, 0.07f), new Vector2(0.46f, 0.15f));
            Button back = MakeButton(canvasGo.transform, "Back", "Back (Esc)", new Vector2(0.54f, 0.07f), new Vector2(0.72f, 0.15f));

            var uso = new SerializedObject(ui);
            uso.FindProperty("panel").objectReferenceValue = panel;
            uso.FindProperty("nameText").objectReferenceValue = name;
            uso.FindProperty("descriptionText").objectReferenceValue = desc;
            uso.FindProperty("messageText").objectReferenceValue = msg;
            uso.FindProperty("takeButton").objectReferenceValue = take;
            uso.FindProperty("backButton").objectReferenceValue = back;
            uso.ApplyModifiedPropertiesWithoutUndo();

            var cso = new SerializedObject(controller);
            cso.FindProperty("ui").objectReferenceValue = ui;
            cso.ApplyModifiedPropertiesWithoutUndo();
            return canvasGo;
        }

        private static TMP_Text MakeLabel(Transform parent, string name, int size, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.font = Resources.GetBuiltinResource<TMP_FontAsset>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.alignment = TextAlignmentOptions.Center;
            var r = (RectTransform)go.transform;
            r.anchorMin = min; r.anchorMax = max; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            return t;
        }

        private static Button MakeButton(Transform parent, string name, string label, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var b = go.AddComponent<Button>();
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);
            var r = (RectTransform)go.transform;
            r.anchorMin = min; r.anchorMax = max; r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            var lgo = new GameObject("Label");
            lgo.transform.SetParent(go.transform, false);
            var t = lgo.AddComponent<TextMeshProUGUI>();
            t.text = label;
            t.font = Resources.GetBuiltinResource<TMP_FontAsset>("LegacyRuntime.ttf");
            t.fontSize = 28;
            t.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)lgo.transform);
            return b;
        }

        private static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        }
    }
}
