using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEngine;
using PracticeRange.Core;
using PracticeRange.Player;
using PracticeRange.Targets;

namespace PracticeRange.AI
{
    /// <summary>Owns the per-target decision pipeline. Dumb = scripted strafes. Trained = ML-Agents policy (ONNX via Sentis, CPU in WebGL).
    /// Learning = Trained + contextual bandit overlay that re-biases dodge direction from live hits and misses.</summary>
    public class DodgeBrain : MonoBehaviour
    {
        public DodgeObservation Observation { get; private set; }
        public bool IsTraining { get; set; }
        public AiLevel Level { get; private set; }

        Transform _player;
        PlayerLook _look;
        Weapon _weapon;
        DodgeMotor _motor;
        TargetBase _target;
        DodgeAgent _agent;
        BanditLearner _bandit;
        ModeConfig _mode;

        // scripted
        Vector2 _scriptedDir; float _scriptedUntil;
        // bandit
        int _ctx, _arm; bool _armActive; float _armUntil; int _lastMissBucket = 8; float _armStartShots;
        float _lastSeenShot = -1f;
        float _shotsAtArmStart; bool _hitDuringArm;

        public void Setup(AiLevel level, Transform player, ModeConfig mode, BanditLearner sharedBandit, Weapon weaponOverride = null, bool training = false)
        {
            Level = level; _player = player; _mode = mode; _bandit = sharedBandit; IsTraining = training;
            _look = player.GetComponentInChildren<PlayerLook>() ?? player.GetComponentInParent<PlayerLook>();
            _weapon = weaponOverride != null ? weaponOverride : player.GetComponentInChildren<Weapon>();
            _motor = GetComponent<DodgeMotor>();
            _target = GetComponent<TargetBase>();
            if (_target != null) _target.Damaged += OnDamaged;

            if (level != AiLevel.Dumb || training)
            {
                var bp = gameObject.GetComponent<BehaviorParameters>() ?? gameObject.AddComponent<BehaviorParameters>();
                bp.BehaviorName = mode.behaviorName;
                bp.BrainParameters.VectorObservationSize = DodgeAgent.ObservationSize;
                bp.BrainParameters.NumStackedVectorObservations = 2;
                bp.BrainParameters.ActionSpec = new Unity.MLAgents.Actuators.ActionSpec(2, new[] { 2 });
                bp.InferenceDevice = InferenceDevice.Burst;
                var model = PolicyRegistry.ModelFor(mode.mode);
                bp.Model = model;
                bp.BehaviorType = training ? BehaviorType.Default : (model != null ? BehaviorType.InferenceOnly : BehaviorType.HeuristicOnly);
                // Order matters: Agent.OnEnable reads BehaviorParameters; DecisionRequester.Awake requires the Agent.
                _agent = gameObject.GetComponent<DodgeAgent>() ?? gameObject.AddComponent<DodgeAgent>();
                _agent.Brain = this;
                var dr = gameObject.GetComponent<DecisionRequester>() ?? gameObject.AddComponent<DecisionRequester>();
                dr.DecisionPeriod = Mathf.Max(1, (int)mode.decisionPeriod);
                dr.TakeActionsBetweenDecisions = true;
            }
        }

        void OnDestroy() { if (_target != null) _target.Damaged -= OnDamaged; }

        public void OnEpisodeBegin() { _armActive = false; }

        void Update()
        {
            if (_player == null || _look == null || _weapon == null || _motor == null) return;
            var o = DodgeObservation.Compute(transform, _player, _look, _weapon, new Vector2(_motor.Velocity.x, _motor.Velocity.z), _motor.IsHidden, _mode.hasCover, _lastSeenShot);
            Observation = o;
            if (o.shotThisStep)
            {
                _lastSeenShot = _weapon.LastShotTime;
                // Record which side the miss went to; this becomes the bandit's context.
                if (o.aimAngleDeg > RewardShaping.MissedAngleThresholdDeg) _lastMissBucket = RewardShaping.DirectionBucket(o.aimOffsetXZ);
            }

            Vector2 action; bool hide = false;
            switch (Level)
            {
                case AiLevel.Dumb:
                    action = ScriptedAction();
                    break;
                case AiLevel.Trained:
                    action = _agent != null ? _agent.LastAction : ScriptedAction();
                    hide = _agent != null && _agent.LastHide;
                    break;
                default:
                    action = BanditOverlay(_agent != null ? _agent.LastAction : ScriptedAction());
                    hide = _agent != null && _agent.LastHide;
                    break;
            }
            // Move in the target's own frame: x = lateral relative to the player, y = toward/away.
            Vector3 toPlayer = (_player.position - transform.position); toPlayer.y = 0f; toPlayer.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, toPlayer);
            Vector3 world = right * action.x + toPlayer * action.y;
            _motor.DesiredVelocity = new Vector2(world.x, world.z);
            _motor.Hide = hide;
        }

        public Vector2 ScriptedAction()
        {
            if (Time.time >= _scriptedUntil)
            {
                _scriptedDir = Random.value < 0.75f ? new Vector2(Random.value < 0.5f ? -1f : 1f, Random.Range(-0.3f, 0.3f)) : Vector2.zero;
                _scriptedUntil = Time.time + Random.Range(0.3f, 0.9f);
            }
            return _scriptedDir;
        }

        Vector2 BanditOverlay(Vector2 policyAction)
        {
            if (_bandit == null) return policyAction;
            if (!_armActive || Time.time >= _armUntil)
            {
                // Close the previous arm: reward = survived without being hit while the player fired.
                if (_armActive)
                {
                    bool fired = _weapon.ShotsFired > _shotsAtArmStart;
                    float r = _hitDuringArm ? -1f : (fired ? 1f : 0.1f);
                    _bandit.Update(_ctx, _arm, r);
                }
                _ctx = BanditLearner.Context(_lastMissBucket, RewardShaping.DistanceBucket(Observation.distance));
                _arm = _bandit.Choose(_ctx, out _);
                _armActive = true; _armUntil = Time.time + 0.5f;
                _shotsAtArmStart = _weapon.ShotsFired; _hitDuringArm = false;
            }
            var bias = RewardShaping.BucketToDirection(_arm);
            return Vector2.ClampMagnitude(policyAction * 0.5f + bias * 0.8f, 1f);
        }

        void OnDamaged(TargetBase t, HitInfo h)
        {
            _hitDuringArm = true;
            if (Level == AiLevel.Learning && _armActive && _bandit != null) { _bandit.Update(_ctx, _arm, -1f); _armActive = false; }
        }
    }
}
