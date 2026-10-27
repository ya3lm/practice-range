using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using PracticeRange.Core;
using PracticeRange.Player;
using PracticeRange.Targets;
using PracticeRange.AI;
using PracticeRange.Audio;
using PracticeRange.UI;
using PracticeRange.Import;

namespace PracticeRange.Editor
{
    /// <summary>Idempotent project setup. Creates mode assets, panel settings, the Main scene, and the Training scenes, and registers them in Build Settings.
    /// Run from the menu or `tools/bootstrap.sh` (batch mode, -executeMethod PracticeRange.Editor.ProjectBootstrap.Run).</summary>
    public static class ProjectBootstrap
    {
        const string Root = "Assets/_Project";
        const string ModesDir = Root + "/Modes";
        const string ScenesDir = Root + "/Scenes";
        const string UiDir = Root + "/UI";

        [MenuItem("Practice Range/Bootstrap Project")]
        public static void Run()
        {
            Directory.CreateDirectory(ModesDir); Directory.CreateDirectory(ScenesDir);
            AssetDatabase.Refresh();

            // Create assets first. NOTE: EditorSceneManager.NewScene unloads assets held only by managed references,
            // so each Build*Scene re-fetches its assets *after* creating the scene (otherwise refs serialize as null).
            foreach (RangeMode m in System.Enum.GetValues(typeof(RangeMode))) EnsureMode(m);
            EnsurePanelSettings(); EnsureTracerPrefab(); EnsureDartPrefab();

            ConfigureProjectSettings();
            EnsureUrp();
            BuildMainScene();
            foreach (RangeMode m in System.Enum.GetValues(typeof(RangeMode))) BuildTrainingScene(m);
            RegisterScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Bootstrap] done");
        }

        /// <summary>Creates and assigns a URP asset so "Universal Render Pipeline/Unlit" materials render (otherwise Built-in RP draws them magenta).</summary>
        static void EnsureUrp()
        {
            string dir = Root + "/Settings"; Directory.CreateDirectory(dir);
            string rdPath = dir + "/UniversalRenderer.asset", rpPath = dir + "/UniversalRP.asset";
            var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rdPath);
            if (rd == null) { rd = ScriptableObject.CreateInstance<UniversalRendererData>(); AssetDatabase.CreateAsset(rd, rdPath); }
            var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(rpPath);
            if (rp == null)
            {
                rp = UniversalRenderPipelineAsset.Create(rd);
                rp.msaaSampleCount = 4;
                rp.supportsHDR = false;
                rp.shadowDistance = 30f;
                AssetDatabase.CreateAsset(rp, rpPath);
            }
            GraphicsSettings.defaultRenderPipeline = rp;
            // Always include the shaders that runtime-created materials use.
            var gs = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var always = gs.FindProperty("m_AlwaysIncludedShaders");
            foreach (var sh in new[] { Shaders.Unlit, Shader.Find("Universal Render Pipeline/Lit"), Shader.Find("Hidden/Universal Render Pipeline/FallbackError") })
            {
                if (sh == null) continue;
                bool present = false;
                for (int i = 0; i < always.arraySize; i++) if (always.GetArrayElementAtIndex(i).objectReferenceValue == sh) { present = true; break; }
                if (!present) { always.InsertArrayElementAtIndex(always.arraySize); always.GetArrayElementAtIndex(always.arraySize - 1).objectReferenceValue = sh; }
            }
            gs.ApplyModifiedPropertiesWithoutUndo();
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = rp; }
            QualitySettings.SetQualityLevel(current, false);
            AssetDatabase.SaveAssets();
        }


        [MenuItem("Practice Range/Diagnose Asset Refs")]
        public static void Diagnose()
        {
            foreach (var path in new[] { ModesDir + "/Mid.asset", Root + "/Player/Tracer.prefab", UiDir + "/MainPanelSettings.asset", UiDir + "/MainUI.uxml", UiDir + "/UnityDefaultRuntimeTheme.tss" })
            {
                var o = AssetDatabase.LoadAssetAtPath<Object>(path);
                var typed = AssetDatabase.LoadMainAssetAtPath(path);
                Debug.Log($"[Diag] {path} exists={File.Exists(path)} guid={AssetDatabase.AssetPathToGUID(path)} obj={(o == null ? "null" : o.GetType().Name)} main={(typed == null ? "null" : typed.GetType().Name)} persistent={(o != null && EditorUtility.IsPersistent(o))}");
            }
            var mc = AssetDatabase.LoadAssetAtPath<ModeConfig>(ModesDir + "/Mid.asset");
            Debug.Log($"[Diag] typed ModeConfig load: {(mc == null ? "null" : mc.name)}; script guid ok={MonoScript.FromScriptableObject(ScriptableObject.CreateInstance<ModeConfig>()) != null}");
            var txt = File.ReadAllText(ModesDir + "/Mid.asset"); Debug.Log("[Diag] Mid.asset head:\n" + txt.Substring(0, Mathf.Min(400, txt.Length)));
        }



        static ModeConfig EnsureMode(RangeMode m)
        {
            string path = $"{ModesDir}/{m}.asset";
            var c = AssetDatabase.LoadAssetAtPath<ModeConfig>(path);
            if (c == null)
            {
                c = ScriptableObject.CreateInstance<ModeConfig>();
                ModeDefaults.Apply(c, m);
                AssetDatabase.CreateAsset(c, path);
            }
            return c;
        }

        static PanelSettings EnsurePanelSettings()
        {
            string path = UiDir + "/MainPanelSettings.asset";
            var p = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (p == null)
            {
                p = ScriptableObject.CreateInstance<PanelSettings>();
                p.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(UiDir + "/UnityDefaultRuntimeTheme.tss");
                p.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                p.referenceResolution = new Vector2Int(1920, 1080);
                p.match = 0.5f;
                AssetDatabase.CreateAsset(p, path);
            }
            return p;
        }

        static Tracer EnsureTracerPrefab()
        {
            string path = Root + "/Player/Tracer.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<Tracer>();
            var go = new GameObject("Tracer");
            var lr = go.AddComponent<LineRenderer>();
            lr.startWidth = 0.004f; lr.endWidth = 0.012f; lr.useWorldSpace = true;
            lr.material = UnlitMat(new Color(1f, 1f, 1f, 0.9f), "Tracer");
            lr.startColor = lr.endColor = new Color(1f, 1f, 1f, 0.9f);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<Tracer>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<Tracer>();
        }

        static NerfDart EnsureDartPrefab()
        {
            string path = Root + "/Player/NerfDart.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing.GetComponent<NerfDart>();
            var go = new GameObject("NerfDart");
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(go.transform, false);
            body.transform.localRotation = Quaternion.Euler(90, 0, 0);
            body.transform.localScale = new Vector3(0.05f, 0.09f, 0.05f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = UnlitMat(new Color(1f, 0.45f, 0.05f), "NerfOrange");
            var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tip.transform.SetParent(go.transform, false);
            tip.transform.localPosition = new Vector3(0, 0, 0.09f);
            tip.transform.localScale = Vector3.one * 0.055f;
            Object.DestroyImmediate(tip.GetComponent<Collider>());
            tip.GetComponent<Renderer>().sharedMaterial = UnlitMat(new Color(0.1f, 0.5f, 1f), "NerfBlue");
            go.AddComponent<Rigidbody>();
            go.AddComponent<NerfDart>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<NerfDart>();
        }

        static Material UnlitMat(Color c, string name)
        {
            string path = $"{Root}/Player/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            var sh = PracticeRange.Core.Shaders.Unlit;
            m = new Material(sh);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static void ConfigureProjectSettings()
        {
            PlayerSettings.productName = "Practice Range";
            PlayerSettings.companyName = "Aden Jessee";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = true;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.template = "PROJECT:PracticeRange";
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.WebGL, ManagedStrippingLevel.Medium);
            Physics.defaultSolverIterations = 8;
            Time.fixedDeltaTime = 1f / 120f;
            // Input System only. (activeInputHandler = 1 → Input System; set via serialized ProjectSettings when bootstrap runs in batch.)
            var ps = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var prop = ps.FindProperty("activeInputHandler");
            if (prop != null) { prop.intValue = 1; ps.ApplyModifiedPropertiesWithoutUndo(); }
        }

        static void BuildMainScene()
        {
            string path = ScenesDir + "/Main.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var modes = new ModeConfig[3];
            foreach (RangeMode m in System.Enum.GetValues(typeof(RangeMode))) modes[(int)m] = EnsureMode(m);
            var panel = EnsurePanelSettings();
            var tracer = EnsureTracerPrefab();
            var dart = EnsureDartPrefab();
            Shaders.Unlit = Shader.Find("Universal Render Pipeline/Unlit");

            // Camera rig
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0, 1.6f, 0);
            var camGo = new GameObject("Camera");
            camGo.transform.SetParent(player.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = modes[1].background;
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 400f;
            camGo.AddComponent<AudioListener>();
            var look = player.AddComponent<PlayerLook>();
            SetField(look, "cam", cam);
            var weapon = camGo.AddComponent<Weapon>();
            SetField(weapon, "look", look);
            SetField(weapon, "tracerPrefab", tracer);
            SetField(weapon, "dartPrefab", dart);

            // Systems
            var systems = new GameObject("Systems");
            var spawner = systems.AddComponent<TargetSpawner>();
            SetField(spawner, "player", player.transform);
            var loop = systems.AddComponent<GameLoop>();
            SetField(loop, "modes", modes);
            SetField(loop, "spawner", spawner);
            SetField(loop, "unlitShader", Shaders.Unlit);
            SetField(loop, "primitiveMeshes", BuiltinMeshes());
            SetField(loop, "baseMaterial", UnlitMat(Color.white, "Base"));
            systems.AddComponent<SfxPlayer>();
            var bridge = new GameObject("FileBridge").AddComponent<FileBridge>();
            bridge.gameObject.AddComponent<CustomTargetImporter>();

            // UI
            var uiGo = new GameObject("UI");
            var doc = uiGo.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiDir + "/MainUI.uxml");
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(UiDir + "/MainUI.uss");
            var ui = uiGo.AddComponent<UiController>();
            SetField(ui, "spawner", spawner);
            SetField(ui, "fileBridge", bridge);
            var styleHook = uiGo.AddComponent<UssAttach>();
            SetField(styleHook, "sheet", uss);

            // Light (unlit materials don't need it, but imported glTF materials do)
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);

            EditorSceneManager.SaveScene(scene, path);
        }

        static void BuildTrainingScene(RangeMode rangeMode)
        {
            string path = $"{ScenesDir}/Training_{rangeMode}.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var mode = EnsureMode(rangeMode);
            var tracer = EnsureTracerPrefab();
            Shaders.Unlit = Shader.Find("Universal Render Pipeline/Unlit");
            int arenas = 16;
            float spacing = mode.arenaHalfWidth * 2f + 12f;
            for (int i = 0; i < arenas; i++)
            {
                var root = new GameObject($"Arena_{i}");
                root.transform.position = new Vector3((i % 4) * spacing, 0, (i / 4) * (mode.arenaHalfDepth + 20f));
                var player = new GameObject("SyntheticPlayer"); player.transform.SetParent(root.transform, false);
                player.transform.localPosition = new Vector3(0, 1.6f, 0);
                var camGo = new GameObject("Camera"); camGo.transform.SetParent(player.transform, false);
                var cam = camGo.AddComponent<Camera>(); cam.enabled = i == 0; if (i == 0) { cam.tag = "MainCamera"; cam.backgroundColor = mode.background; cam.clearFlags = CameraClearFlags.SolidColor; }
                var look = player.AddComponent<PlayerLook>(); SetField(look, "cam", cam);
                var weapon = camGo.AddComponent<Weapon>(); SetField(weapon, "look", look); SetField(weapon, "tracerPrefab", i == 0 ? tracer : null);
                var synth = player.AddComponent<SyntheticPlayer>(); SetField(synth, "look", look); SetField(synth, "weapon", weapon); SetField(synth, "cameraTransform", camGo.transform);
                var spawnerGo = new GameObject("Spawner"); spawnerGo.transform.SetParent(root.transform, false);
                var spawner = spawnerGo.AddComponent<TargetSpawner>(); SetField(spawner, "player", player.transform);
                var arenaComp = root.AddComponent<TrainingArena>();
                SetField(arenaComp, "spawner", spawner); SetField(arenaComp, "synthetic", synth);
                var cfg = root.AddComponent<TrainingArenaConfigurator>(); SetField(cfg, "mode", mode); SetField(cfg, "spawner", spawner); SetField(cfg, "unlitShader", Shaders.Unlit); SetField(cfg, "primitiveMeshes", BuiltinMeshes()); SetField(cfg, "baseMaterial", UnlitMat(Color.white, "Base"));
            }
            EditorSceneManager.SaveScene(scene, path);
        }

        static void RegisterScenes()
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenesDir + "/Main.unity", true),
                new EditorBuildSettingsScene(ScenesDir + "/Training_Close.unity", false),
                new EditorBuildSettingsScene(ScenesDir + "/Training_Mid.unity", false),
                new EditorBuildSettingsScene(ScenesDir + "/Training_Far.unity", false),
            };
            EditorBuildSettings.scenes = list.ToArray();
        }

        static Mesh[] BuiltinMeshes() => new[]
        {
            Resources.GetBuiltinResource<Mesh>("Cube.fbx"),
            Resources.GetBuiltinResource<Mesh>("Sphere.fbx"),
            Resources.GetBuiltinResource<Mesh>("Capsule.fbx"),
            Resources.GetBuiltinResource<Mesh>("Quad.fbx"),
        };

        static void SetField(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"[Bootstrap] no field {field} on {target.GetType().Name}"); return; }
            if (value is Object o) p.objectReferenceValue = o;
            else if (value is Object[] arr) { p.arraySize = arr.Length; for (int i = 0; i < arr.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = arr[i]; }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
