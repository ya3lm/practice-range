using System;
using UnityEngine;

namespace PracticeRange.Core
{
    public enum TargetStyle { Cube = 0, Sphere = 1, Mannequin = 2, Custom = 3 }
    public enum WeaponSkin { Tracer = 0, Nerf = 1 }
    public enum AiLevel { Dumb = 0, Trained = 1, Learning = 2 }
    public enum RangeMode { Close = 0, Mid = 1, Far = 2 }

    /// <summary>The eight v1 settings plus the mode/target/AI selectors. Serialized to JSON.</summary>
    [Serializable]
    public class SettingsData
    {
        // 1. Sensitivity
        public float cmPer360 = 35f;
        public float mouseDpi = 800f;
        // 2. FOV
        public float horizontalFov = 103f;
        // 3. Crosshair
        public float crosshairSize = 6f;
        public float crosshairThickness = 2f;
        public float crosshairGap = 3f;
        public bool crosshairDot = true;
        public Color crosshairColor = new Color(0f, 1f, 1f, 1f);
        // 4. Target color
        public Color targetColor = new Color(0f, 1f, 1f, 1f);
        // 5. Sounds (index into ProceduralSfx variants)
        public int hitSound = 0;
        public int killSound = 0;
        public float volume = 0.8f;
        // 6. Countdown
        public float countdownSeconds = 3f;
        // 7. Restart key (Input System key name)
        public string restartKey = "f6";
        // 8. Mode / target / AI
        public RangeMode mode = RangeMode.Mid;
        public TargetStyle targetStyle = TargetStyle.Cube;
        public AiLevel aiLevel = AiLevel.Trained;
        public WeaponSkin weaponSkin = WeaponSkin.Tracer;
        public float roundSeconds = 60f;

        public SettingsData Clone() => JsonUtility.FromJson<SettingsData>(JsonUtility.ToJson(this));
    }

    /// <summary>Persists SettingsData in PlayerPrefs (works in WebGL via IndexedDB).</summary>
    public static class SettingsService
    {
        const string Key = "practice-range.settings.v1";
        static SettingsData _current;
        public static event Action<SettingsData> Changed;

        public static SettingsData Current
        {
            get
            {
                if (_current == null) _current = Load();
                return _current;
            }
        }

        public static SettingsData Load()
        {
            var json = PlayerPrefs.GetString(Key, string.Empty);
            if (string.IsNullOrEmpty(json)) return new SettingsData();
            try { return JsonUtility.FromJson<SettingsData>(json) ?? new SettingsData(); }
            catch { return new SettingsData(); }
        }

        public static void Save(SettingsData data)
        {
            _current = data;
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            Changed?.Invoke(data);
        }

        public static void Reset() => Save(new SettingsData());

        /// <summary>Call after mutating Current in place.</summary>
        public static void Apply() => Save(Current);
    }
}
