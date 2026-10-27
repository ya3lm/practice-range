using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using PracticeRange.Targets;

namespace PracticeRange.Core
{
    public enum GamePhase { Menu, Countdown, Round, Scoreboard }

    /// <summary>Countdown → Round → Scoreboard → (restart key) → Countdown. Owns SessionStats.</summary>
    public class GameLoop : MonoBehaviour
    {
        public static GameLoop Instance { get; private set; }

        [SerializeField] ModeConfig[] modes;
        [SerializeField] TargetSpawner spawner;
        [SerializeField] Shader unlitShader;   // serialized so the shader ships in the build
        [SerializeField] Mesh[] primitiveMeshes; // cube, sphere, capsule, quad (built-in meshes, serialized so they ship)
        [SerializeField] Material baseMaterial;  // Base.mat, URP Unlit; runtime materials are copies of it

        public GamePhase Phase { get; private set; } = GamePhase.Menu;
        public float PhaseTime { get; private set; }
        public float RoundRemaining => Mathf.Max(0f, Settings.roundSeconds - PhaseTime);
        public SessionStats Stats { get; } = new SessionStats();
        public ModeConfig ActiveMode { get; private set; }
        public SettingsData Settings => SettingsService.Current;
        public bool MenuOpen { get; private set; }

        public event Action<GamePhase> PhaseChanged;
        public event Action<bool> MenuToggled;

        float _accSampleTimer;

        void Awake()
        {
            Instance = this;
            Shaders.Unlit = unlitShader;
            Shaders.Base = baseMaterial;
            Primitives.Assign(primitiveMeshes);
            Debug.Log($"[Boot] shaderRef={(unlitShader == null ? "null" : unlitShader.name)} supported={(unlitShader != null && unlitShader.isSupported)} baseMat={(baseMaterial == null ? "null" : baseMaterial.shader == null ? "no-shader" : baseMaterial.shader.name)} find={(Shader.Find("Universal Render Pipeline/Unlit") == null ? "null" : "ok")} meshes={(primitiveMeshes == null ? -1 : primitiveMeshes.Length)} cube={(Primitives.Cube == null ? "null" : "ok")} rp={(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null ? "builtin" : UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name)}");
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
        }

        void Start()
        {
            SettingsService.Changed += OnSettingsChanged;
            SelectMode(Settings.mode);
            SetMenu(true);
        }

        void OnDestroy()
        {
            SettingsService.Changed -= OnSettingsChanged;
            if (Instance == this) Instance = null;
        }

        void OnSettingsChanged(SettingsData s)
        {
            if (ActiveMode == null || ActiveMode.mode != s.mode) SelectMode(s.mode);
            spawner.ApplyStyle(s.targetStyle, s.targetColor, s.aiLevel);
        }

        public void SelectMode(RangeMode m)
        {
            foreach (var c in modes) if (c != null && c.mode == m) { ActiveMode = c; break; }
            if (ActiveMode == null && modes.Length > 0) ActiveMode = modes[0];
            spawner.Configure(ActiveMode, Settings.targetStyle, Settings.targetColor, Settings.aiLevel);
            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = ActiveMode.background;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame)
                {
                    if (Phase == GamePhase.Scoreboard) { Phase = GamePhase.Menu; PhaseTime = 0f; PhaseChanged?.Invoke(Phase); SetMenu(true); }
                    else SetMenu(!MenuOpen);
                }
                var restart = RestartControl(kb);
                if (restart != null && restart.wasPressedThisFrame && !MenuOpen) StartRound();
            }

            PhaseTime += Time.deltaTime;
            switch (Phase)
            {
                case GamePhase.Countdown:
                    if (PhaseTime >= Settings.countdownSeconds) EnterPhase(GamePhase.Round);
                    break;
                case GamePhase.Round:
                    Stats.roundSeconds = PhaseTime;
                    _accSampleTimer += Time.deltaTime;
                    if (_accSampleTimer >= 1f) { _accSampleTimer = 0f; Stats.accuracyTimeline.Add(Stats.Accuracy); }
                    if (PhaseTime >= Settings.roundSeconds) EnterPhase(GamePhase.Scoreboard);
                    break;
            }
        }

        ButtonControl RestartControl(Keyboard kb)
        {
            var key = Settings.restartKey;
            try { return kb[key] as ButtonControl; } catch { return kb.f6Key; }
        }

        public void StartRound()
        {
            Stats.Reset();
            Stats.policyCheckpoint = (int)Settings.aiLevel;
            spawner.DespawnAll();
            SetMenu(false);
            EnterPhase(GamePhase.Countdown);
        }

        public void SetMenu(bool open)
        {
            MenuOpen = open;
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
            if (open && Phase == GamePhase.Round) Time.timeScale = 0f; else Time.timeScale = 1f;
            MenuToggled?.Invoke(open);
        }

        void EnterPhase(GamePhase p)
        {
            Phase = p;
            PhaseTime = 0f;
            if (p == GamePhase.Round) spawner.SpawnInitial();
            if (p == GamePhase.Scoreboard) { spawner.DespawnAll(); SetMenu(true); }
            PhaseChanged?.Invoke(p);
        }

        // Called by Weapon / targets
        public void RegisterShot() { if (Phase == GamePhase.Round) Stats.shots++; }
        public void RegisterHit(bool headshot) { if (Phase == GamePhase.Round) { Stats.hits++; if (headshot) Stats.headshots++; } }
        public void RegisterKill(float reactionTime)
        {
            if (Phase != GamePhase.Round) return;
            Stats.kills++;
            if (reactionTime > 0f) Stats.reactionTimes.Add(reactionTime);
        }
    }
}
