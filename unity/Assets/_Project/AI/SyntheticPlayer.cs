using UnityEngine;
using Unity.MLAgents;
using PracticeRange.Player;
using PracticeRange.Targets;

namespace PracticeRange.AI
{
    /// <summary>A model of a human for training: tracks the target with a reaction delay, aim noise, and limited turn speed, firing on a cadence.
    /// Curriculum parameters (aim_noise_deg, reaction_ms, fire_interval) come from the ML-Agents EnvironmentParameters.</summary>
    public class SyntheticPlayer : MonoBehaviour
    {
        [SerializeField] PlayerLook look;
        [SerializeField] Weapon weapon;
        [SerializeField] Transform cameraTransform;
        public Transform Target;

        public float aimNoiseDeg = 3f;
        public float reactionMs = 180f;
        public float fireInterval = 0.35f;
        public float maxTurnDegPerSec = 540f;
        [Tooltip("Seconds a human needs to notice a new target and start aiming; sampled per target.")]
        public Vector2 acquireDelayRange = new Vector2(0.25f, 0.6f);

        Vector3 _delayedTargetPos;
        Transform _lastTarget;
        float _nextFire;
        float _noisePhase;
        Vector3 _noise;
        readonly System.Collections.Generic.Queue<(float t, Vector3 p)> _history = new System.Collections.Generic.Queue<(float, Vector3)>();

        void Start()
        {
            if (weapon != null) weapon.AutomatedFire = true;
            if (cameraTransform == null && look != null) cameraTransform = look.GetComponentInChildren<Camera>().transform;
        }

        void Update()
        {
            if (Target == null || look == null) return;
            if (Target != _lastTarget)
            {
                // New target: a human needs time to acquire it, and the reaction buffer must refill before the aim is trustworthy.
                _lastTarget = Target;
                _history.Clear();
                float acquire = Random.Range(acquireDelayRange.x, acquireDelayRange.y) + reactionMs / 1000f;
                _nextFire = Time.time + acquire;
                // Start the aim somewhere plausible but off-target, so the first shot is a real flick, not a spawn snipe.
                cameraTransform.rotation = Quaternion.LookRotation(Target.position + Vector3.up * 0.8f - cameraTransform.position) * Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(-15f, 15f), 0f);
            }
            var ep = Academy.IsInitialized ? Academy.Instance.EnvironmentParameters : null;
            if (ep != null)
            {
                aimNoiseDeg = ep.GetWithDefault("aim_noise_deg", aimNoiseDeg);
                reactionMs = ep.GetWithDefault("reaction_ms", reactionMs);
                fireInterval = ep.GetWithDefault("fire_interval", fireInterval);
            }

            // Reaction delay: aim at where the target was reactionMs ago.
            var tp = Target.position + Vector3.up * 0.8f;
            _history.Enqueue((Time.time, tp));
            while (_history.Count > 1 && Time.time - _history.Peek().t > reactionMs / 1000f) _history.Dequeue();
            _delayedTargetPos = _history.Peek().p;
            bool reactionBufferFull = Time.time - _history.Peek().t >= reactionMs / 1000f * 0.9f;

            // Low-frequency aim noise (hand tremor + overshoot), not white noise.
            _noisePhase += Time.deltaTime * 2.5f;
            _noise = new Vector3(Mathf.PerlinNoise(_noisePhase, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, _noisePhase) - 0.5f, 0f) * 2f * aimNoiseDeg;

            var desired = Quaternion.LookRotation(_delayedTargetPos - cameraTransform.position) * Quaternion.Euler(_noise.y, _noise.x, 0f);
            cameraTransform.rotation = Quaternion.RotateTowards(cameraTransform.rotation, desired, maxTurnDegPerSec * Time.deltaTime);

            if (Time.time >= _nextFire && reactionBufferFull)
            {
                _nextFire = Time.time + fireInterval * Random.Range(0.8f, 1.2f);
                weapon.Fire();
            }
        }
    }
}
