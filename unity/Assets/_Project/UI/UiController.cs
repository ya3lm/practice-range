using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using PracticeRange.Core;
using PracticeRange.Audio;
using PracticeRange.Import;
using PracticeRange.Targets;

namespace PracticeRange.UI
{
    /// <summary>Binds MainUI.uxml to SettingsService and GameLoop. HUD, settings menu, scoreboard.</summary>
    [RequireComponent(typeof(UIDocument))]
    public class UiController : MonoBehaviour
    {
        [SerializeField] TargetSpawner spawner;
        [SerializeField] FileBridge fileBridge;

        VisualElement _root, _hud, _menu, _score, _crosshair, _timeline;
        Label _timer, _accuracy, _aiStatus, _bandit, _countdown, _importStatus, _edpi;
        float _lastCountdownInt = -1;
        GameLoop Loop => GameLoop.Instance;

        void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _hud = _root.Q("hud"); _menu = _root.Q("menu"); _score = _root.Q("scoreboard");
            _crosshair = _root.Q("crosshair"); _timeline = _root.Q("timeline");
            _timer = _root.Q<Label>("timer"); _accuracy = _root.Q<Label>("accuracy");
            _aiStatus = _root.Q<Label>("ai-status"); _bandit = _root.Q<Label>("bandit");
            _countdown = _root.Q<Label>("countdown"); _importStatus = _root.Q<Label>("import-status");
            _edpi = _root.Q<Label>("edpi");
            _root.Q<Label>("version").text = "v" + Application.version + "  ·  " + SystemInfo.graphicsDeviceType;

            _root.Q<Button>("btn-start").clicked += () => Loop.StartRound();
            _root.Q<Button>("btn-import").clicked += () => fileBridge?.Open();
            _root.Q<Button>("btn-reset").clicked += () => { SettingsService.Reset(); BindSettings(); };
            CustomTargetImporter.Status += s => _importStatus.text = s;
            FileBridge.Failed += s => _importStatus.text = s;

            BindSettings();
            ApplyCrosshair(SettingsService.Current);
            SettingsService.Changed += ApplyCrosshair;
        }

        void Start()
        {
            if (Loop != null) { Loop.PhaseChanged += OnPhase; Loop.MenuToggled += OnMenu; }
            OnMenu(true);
        }

        void OnDisable()
        {
            SettingsService.Changed -= ApplyCrosshair;
            if (Loop != null) { Loop.PhaseChanged -= OnPhase; Loop.MenuToggled -= OnMenu; }
        }

        void OnMenu(bool open)
        {
            bool scoreboard = Loop != null && Loop.Phase == GamePhase.Scoreboard;
            _menu.style.display = open && !scoreboard ? DisplayStyle.Flex : DisplayStyle.None;
            if (open && scoreboard) ShowScoreboard();
            else _score.style.display = DisplayStyle.None;
        }

        void OnPhase(GamePhase p)
        {
            _countdown.style.display = p == GamePhase.Countdown ? DisplayStyle.Flex : DisplayStyle.None;
            if (p == GamePhase.Round) SfxPlayer.Play(SfxKind.CountdownGo);
            if (p == GamePhase.Menu) OnMenu(true);
        }

        void Update()
        {
            if (Loop == null) return;
            var s = SettingsService.Current;
            if (Loop.Phase == GamePhase.Round) _timer.text = Loop.RoundRemaining.ToString("0.0");
            else if (Loop.Phase == GamePhase.Countdown)
            {
                float remaining = s.countdownSeconds - Loop.PhaseTime;
                int whole = Mathf.CeilToInt(remaining);
                _countdown.text = whole.ToString();
                if (whole != _lastCountdownInt) { _lastCountdownInt = whole; SfxPlayer.Play(SfxKind.CountdownTick); }
                _timer.text = s.roundSeconds.ToString("0.0");
            }
            _accuracy.text = (Loop.Stats.Accuracy * 100f).ToString("0") + "%  ·  " + Loop.Stats.kills + " kills";
            _root.Q<Label>("mode-status").text = (s.mode + " · " + s.targetStyle).ToUpperInvariant();
            _aiStatus.text = "AI: " + s.aiLevel + (s.aiLevel != AiLevel.Dumb && !AI.PolicyRegistry.HasModel(s.mode) ? " (no model → scripted)" : "");
            _bandit.text = s.aiLevel == AiLevel.Learning && spawner != null ? $"learning: {spawner.SharedBandit.totalUpdates} updates · conf {spawner.SharedBandit.Confidence():0.00}" : "";
        }

        void ApplyCrosshair(SettingsData s)
        {
            var c = s.crosshairColor;
            float size = s.crosshairSize, t = s.crosshairThickness, g = s.crosshairGap;
            Set(_root.Q("ch-top"), -t / 2, -(g + size), t, size, c);
            Set(_root.Q("ch-bottom"), -t / 2, g, t, size, c);
            Set(_root.Q("ch-left"), -(g + size), -t / 2, size, t, c);
            Set(_root.Q("ch-right"), g, -t / 2, size, t, c);
            var dot = _root.Q("ch-dot");
            Set(dot, -t / 2, -t / 2, t, t, c);
            dot.style.display = s.crosshairDot ? DisplayStyle.Flex : DisplayStyle.None;
            _edpi.text = $"eDPI {Sensitivity.EDpi(s.cmPer360, s.mouseDpi):0}  ·  {Sensitivity.DegreesPerCount(s.cmPer360, s.mouseDpi):0.000} °/count";
        }

        static void Set(VisualElement e, float left, float top, float w, float h, Color c)
        {
            e.style.left = left; e.style.top = top; e.style.width = w; e.style.height = h; e.style.backgroundColor = c;
        }

        // ---------- settings binding ----------
        void BindSettings()
        {
            var s = SettingsService.Current;
            Dropdown("mode", Enum.GetNames(typeof(RangeMode)), (int)s.mode, i => { s.mode = (RangeMode)i; SettingsService.Apply(); });
            Dropdown("target-style", new[] { "Cube", "Sphere", "Mannequin", "Custom (imported)" }, (int)s.targetStyle, i => { s.targetStyle = (TargetStyle)i; SettingsService.Apply(); });
            Dropdown("ai-level", new[] { "Dumb (scripted)", "Trained (PPO policy)", "Learning (policy + live bandit)" }, (int)s.aiLevel, i => { s.aiLevel = (AiLevel)i; SettingsService.Apply(); });
            Dropdown("weapon-skin", new[] { "Tracer", "Nerf dart" }, (int)s.weaponSkin, i => { s.weaponSkin = (WeaponSkin)i; SettingsService.Apply(); });
            SliderPair("round", "round-v", s.roundSeconds, v => { s.roundSeconds = Mathf.Round(v); SettingsService.Apply(); });
            SliderPair("cm360", "cm360-v", s.cmPer360, v => { s.cmPer360 = v; SettingsService.Apply(); });
            SliderPair("dpi", "dpi-v", s.mouseDpi, v => { s.mouseDpi = Mathf.Round(v); SettingsService.Apply(); });
            SliderPair("fov", "fov-v", s.horizontalFov, v => { s.horizontalFov = v; SettingsService.Apply(); });
            SliderPair("ch-size", "ch-size-v", s.crosshairSize, v => { s.crosshairSize = v; SettingsService.Apply(); });
            SliderPair("ch-thick", "ch-thick-v", s.crosshairThickness, v => { s.crosshairThickness = v; SettingsService.Apply(); });
            SliderPair("ch-gap", "ch-gap-v", s.crosshairGap, v => { s.crosshairGap = v; SettingsService.Apply(); });
            SliderPair("countdown-s", "countdown-v", s.countdownSeconds, v => { s.countdownSeconds = Mathf.Round(v); SettingsService.Apply(); });
            var vol = _root.Q<Slider>("volume"); vol.SetValueWithoutNotify(s.volume); vol.RegisterValueChangedCallback(e => { s.volume = e.newValue; SettingsService.Apply(); });
            var dot = _root.Q<Toggle>("ch-dot-toggle"); dot.SetValueWithoutNotify(s.crosshairDot); dot.RegisterValueChangedCallback(e => { s.crosshairDot = e.newValue; SettingsService.Apply(); });
            ColorField("ch-color", s.crosshairColor, c => { s.crosshairColor = c; SettingsService.Apply(); });
            ColorField("target-color", s.targetColor, c => { s.targetColor = c; SettingsService.Apply(); });
            Dropdown("hit-sound", new[] { "Tick 2.0k", "Tick 2.6k", "Tick 1.6k" }, s.hitSound, i => { s.hitSound = i; SettingsService.Apply(); SfxPlayer.Play(SfxKind.Hit); });
            Dropdown("kill-sound", new[] { "Thump A", "Thump B", "Thump C" }, s.killSound, i => { s.killSound = i; SettingsService.Apply(); SfxPlayer.Play(SfxKind.Kill); });
            var rk = _root.Q<TextField>("restart-key"); rk.SetValueWithoutNotify(s.restartKey); rk.RegisterValueChangedCallback(e => { s.restartKey = e.newValue.Trim().ToLowerInvariant(); SettingsService.Apply(); });
        }

        void Dropdown(string name, string[] choices, int index, Action<int> onChange)
        {
            var d = _root.Q<DropdownField>(name);
            d.choices = new List<string>(choices);
            d.SetValueWithoutNotify(choices[Mathf.Clamp(index, 0, choices.Length - 1)]);
            d.RegisterValueChangedCallback(e => onChange(d.choices.IndexOf(e.newValue)));
        }

        void SliderPair(string slider, string field, float value, Action<float> onChange)
        {
            var sl = _root.Q<Slider>(slider); var f = _root.Q<FloatField>(field);
            sl.SetValueWithoutNotify(value); f.SetValueWithoutNotify(value);
            sl.RegisterValueChangedCallback(e => { f.SetValueWithoutNotify(e.newValue); onChange(e.newValue); });
            f.RegisterValueChangedCallback(e => { float v = Mathf.Clamp(e.newValue, sl.lowValue, sl.highValue); sl.SetValueWithoutNotify(v); onChange(v); });
        }

        void ColorField(string name, Color value, Action<Color> onChange)
        {
            var tf = _root.Q<TextField>(name);
            tf.SetValueWithoutNotify("#" + ColorUtility.ToHtmlStringRGB(value));
            tf.RegisterValueChangedCallback(e => { if (ColorUtility.TryParseHtmlString(e.newValue, out var c)) onChange(c); });
        }

        void ShowScoreboard()
        {
            var st = Loop.Stats; var s = SettingsService.Current;
            _score.style.display = DisplayStyle.Flex;
            _root.Q<Label>("s-accuracy").text = $"Accuracy {st.Accuracy * 100f:0.0}%";
            _root.Q<Label>("s-kills").text = $"Kills {st.kills}  ({st.KillsPerMinute:0.0}/min)";
            _root.Q<Label>("s-reaction").text = $"Mean time-to-kill {st.MeanReactionMs:0} ms";
            _root.Q<Label>("s-shots").text = $"Shots {st.shots}  ·  Hits {st.hits}";
            _root.Q<Label>("s-ai").text = $"AI {s.aiLevel}  ·  {s.mode} · {s.targetStyle}";
            _root.Q<Label>("s-bandit").text = s.aiLevel == AiLevel.Learning ? $"Bandit updates {spawner.SharedBandit.totalUpdates}" : "";
            DrawTimeline(st.accuracyTimeline);
            Debug.Log("[Stats] " + st.ToJson());
        }

        void DrawTimeline(List<float> acc)
        {
            _timeline.Clear();
            if (acc.Count < 2) return;
            float w = 640f / acc.Count;
            for (int i = 0; i < acc.Count; i++)
            {
                var bar = new VisualElement();
                bar.style.position = Position.Absolute;
                bar.style.left = i * w; bar.style.width = Mathf.Max(1f, w - 1f);
                float h = 88f * Mathf.Clamp01(acc[i]);
                bar.style.top = 90f - h; bar.style.height = h;
                bar.style.backgroundColor = new Color(0.1f, 0.78f, 0.78f);
                _timeline.Add(bar);
            }
        }
    }
}
