using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PracticeRange.AI;
using PracticeRange.Core;
using PracticeRange.Player;
using PracticeRange.Targets;

namespace PracticeRange.Tests
{
    /// <summary>The synthetic player is the only thing the policy trains against. If it cannot hit a static cube, training is meaningless.</summary>
    public class SyntheticPlayerTests
    {
        [UnityTest]
        public IEnumerator Synthetic_Fires_And_Kills_A_Static_Cube_Within_3_Seconds()
        {
            var player = new GameObject("Synthetic"); player.transform.position = new Vector3(0, 1.6f, 0);
            var camGo = new GameObject("Camera"); camGo.transform.SetParent(player.transform, false);
            var cam = camGo.AddComponent<Camera>();
            var look = player.AddComponent<PlayerLook>();
            Set(look, "cam", cam);
            var weapon = camGo.AddComponent<Weapon>();
            Set(weapon, "look", look);
            var synth = player.AddComponent<SyntheticPlayer>();
            Set(synth, "look", look); Set(synth, "weapon", weapon); Set(synth, "cameraTransform", camGo.transform);
            synth.aimNoiseDeg = 1f; synth.reactionMs = 200f; synth.fireInterval = 0.25f;

            var go = new GameObject("T"); go.transform.position = new Vector3(0, 0, 15);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube); body.transform.SetParent(go.transform, false); body.transform.localPosition = Vector3.up * 0.6f;
            go.AddComponent<Rigidbody>().isKinematic = true;
            var target = go.AddComponent<CubeTarget>();
            var mode = ScriptableObject.CreateInstance<ModeConfig>(); ModeDefaults.Apply(mode, RangeMode.Mid);
            target.Initialize(mode, Color.cyan);
            synth.Target = go.transform;

            float t0 = Time.time;
            while (Time.time - t0 < 3f && !target.IsDead) yield return null;
            Assert.Greater(weapon.ShotsFired, 0, "synthetic player never fired");
            Assert.IsTrue(target.IsDead, $"synthetic player fired {weapon.ShotsFired} shots in 3 s and never killed a static cube at 15 m");
        }

        [UnityTest]
        public IEnumerator Synthetic_Still_Kills_Under_Training_Time_Settings()
        {
            // Trainer settings from ml/configs: capture_frame_rate 60, time_scale 1. (time_scale 20 made every frame 0.33 s of game time
            // and the Update-based synthetic player never fired; this test guards that.) Arena at a world offset like Arena_5.
            int cfr = Time.captureFramerate; float ts = Time.timeScale;
            Time.captureFramerate = 60; Time.timeScale = 1f;
            var offset = new Vector3(60f, 0f, 50f);
            var player = new GameObject("Synthetic"); player.transform.position = offset + new Vector3(0, 1.6f, 0);
            var camGo = new GameObject("Camera"); camGo.transform.SetParent(player.transform, false);
            var cam = camGo.AddComponent<Camera>();
            var look = player.AddComponent<PlayerLook>(); Set(look, "cam", cam);
            var weapon = camGo.AddComponent<Weapon>(); Set(weapon, "look", look);
            var synth = player.AddComponent<SyntheticPlayer>();
            Set(synth, "look", look); Set(synth, "weapon", weapon); Set(synth, "cameraTransform", camGo.transform);
            synth.aimNoiseDeg = 1.2f; synth.reactionMs = 160f; synth.fireInterval = 0.3f;
            var go = new GameObject("T"); go.transform.position = offset + new Vector3(0, 0, 15);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube); body.transform.SetParent(go.transform, false); body.transform.localPosition = Vector3.up * 0.6f;
            go.AddComponent<Rigidbody>().isKinematic = true;
            var target = go.AddComponent<CubeTarget>();
            var mode = ScriptableObject.CreateInstance<ModeConfig>(); ModeDefaults.Apply(mode, RangeMode.Mid);
            target.Initialize(mode, Color.cyan);
            synth.Target = go.transform;
            float t0 = Time.time; int frames = 0; float dtSum = 0f;
            while (Time.time - t0 < 4f && !target.IsDead) { frames++; dtSum += Time.deltaTime; yield return null; }
            float meanDt = frames > 0 ? dtSum / frames : -1f;
            Time.captureFramerate = cfr; Time.timeScale = ts;
            Assert.Less(meanDt, 0.03f, $"training frames must be ~1/60 s of game time, got {meanDt:0.0000}; check time_scale in ml/configs");
            Assert.IsTrue(target.IsDead, $"never killed under training time settings: shots={weapon.ShotsFired} frames={frames} meanDeltaTime={meanDt:0.0000}");
        }

        static void Set(Object target, string field, Object value)
        {
            var f = target.GetType().GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(f, "no field " + field); f.SetValue(target, value);
        }
    }
}
