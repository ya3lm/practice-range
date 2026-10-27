using System.Collections.Generic;
using UnityEngine;
using PracticeRange.Core;

namespace PracticeRange.Targets
{
    /// <summary>Close-range humanoid. Faceless, two-tone. Kinematic while alive with a procedural strafe bob; ragdolls on death.
    /// Headshot: 2x impulse and a 0.3 s slow-motion. No other headshot feedback, by content policy.</summary>
    public class MannequinTarget : TargetBase
    {
        RagdollBuilder.Result _rig;
        List<Material> _mats = new List<Material>();
        float _bobPhase;
        Color _secondary;

        protected override void Awake()
        {
            base.Awake();
            Style = TargetStyle.Mannequin;
        }

        public override void Initialize(ModeConfig mode, Color color)
        {
            _secondary = new Color(0.16f, 0.17f, 0.2f);
            if (_rig.root == null)
            {
                _rig = RagdollBuilder.Build(transform, color, _secondary, mode.targetScale);
                foreach (var r in _rig.renderers) _mats.Add(r.sharedMaterial);
            }
            base.Initialize(mode, color);
        }

        public override void ApplyColor(Color c)
        {
            if (_rig.renderers == null) return;
            for (int i = 0; i < _rig.renderers.Count; i++)
            {
                var name = _rig.renderers[i].name;
                bool primary = name == "Pelvis" || name == "Head" || name.Contains("Forearm");
                SetMatColor(_mats[i], primary ? c * PracticeRange.Core.Shaders.AccentIntensity : _secondary);
            }
        }

        void Update()
        {
            if (IsDead || Motor == null) return;
            // Procedural locomotion: lean into velocity, bob with speed. Cheap, readable, no animation assets.
            float speed = Motor.Velocity.magnitude;
            _bobPhase += Time.deltaTime * (4f + speed * 2.5f);
            var lean = Quaternion.FromToRotation(Vector3.up, Vector3.up + Motor.Velocity * 0.06f);
            _rig.root.localRotation = lean;
            _rig.root.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(_bobPhase)) * 0.04f * Mathf.Clamp01(speed), 0f);
            float swing = Mathf.Sin(_bobPhase) * 25f * Mathf.Clamp01(speed / 3f);
            _rig.bodies[7].transform.localRotation = Quaternion.Euler(swing, 0, 0);
            _rig.bodies[8].transform.localRotation = Quaternion.Euler(-swing, 0, 0);
            _rig.bodies[3].transform.localRotation = Quaternion.Euler(-swing, 0, 8);
            _rig.bodies[4].transform.localRotation = Quaternion.Euler(swing, 0, -8);
        }

        protected override void OnDamaged(HitInfo hit) => StartCoroutine(Flash());

        System.Collections.IEnumerator Flash()
        {
            foreach (var m in _mats) SetMatColor(m, Color.white * 3f);
            yield return new WaitForSeconds(0.05f);
            ApplyColor(_color);
        }

        protected override void OnDeath(HitInfo hit, bool head)
        {
            foreach (var rb in _rig.bodies) { rb.isKinematic = false; rb.useGravity = true; }
            var target = hit.collider != null ? hit.collider.attachedRigidbody : _rig.pelvis;
            if (target == null) target = _rig.pelvis;
            target.AddForceAtPosition(hit.direction * hit.force, hit.point, ForceMode.Impulse);
            // A little of the impulse goes to the pelvis so the whole body reads the hit, not just a limb.
            _rig.pelvis.AddForce(hit.direction * hit.force * 0.35f, ForceMode.Impulse);
            if (head) SlowMo.Trigger(0.3f, 0.3f);
            KillRing.Spawn(hit.point, _color, 1.2f);
            foreach (var c in GetComponentsInChildren<Collider>()) c.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            Destroy(gameObject, 3.5f);
        }
    }

    /// <summary>Global slow-motion helper. Real-time duration so it reads the same on any machine.</summary>
    public class SlowMo : MonoBehaviour
    {
        static SlowMo _inst;
        float _until; float _scale = 1f;
        public static void Trigger(float scale, float realSeconds)
        {
            if (_inst == null) { _inst = new GameObject("SlowMo").AddComponent<SlowMo>(); DontDestroyOnLoad(_inst.gameObject); }
            _inst._scale = scale; _inst._until = Time.unscaledTime + realSeconds;
            Time.timeScale = scale;
        }
        void Update()
        {
            if (_until > 0f && Time.unscaledTime >= _until)
            {
                var loop = GameLoop.Instance;
                if (loop == null || !loop.MenuOpen) Time.timeScale = 1f;
                _until = 0f;
            }
        }
    }
}
