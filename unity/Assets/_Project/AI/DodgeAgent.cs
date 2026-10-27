using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using PracticeRange.Targets;

namespace PracticeRange.AI
{
    /// <summary>ML-Agents PPO agent. Observations: 12 floats. Actions: 2 continuous (vx, vz) + 1 discrete (peek/hide).
    /// Attached by DodgeBrain for Trained/Learning levels and in training arenas. Requires BehaviorParameters + DecisionRequester on the same object.</summary>
    public class DodgeAgent : Agent
    {
        public const int ObservationSize = 12;
        public DodgeBrain Brain { get; set; }
        bool _hitThisStep;
        public Vector2 LastAction { get; private set; }
        public bool LastHide { get; private set; }

        public void NotifyHit() => _hitThisStep = true;

        public override void OnEpisodeBegin()
        {
            _hitThisStep = false;
            Brain?.OnEpisodeBegin();
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            var o = Brain != null ? Brain.Observation : default;
            float dn = Mathf.Clamp01(o.distance / 80f);
            sensor.AddObservation(o.toPlayerLocal.x / 40f);
            sensor.AddObservation(o.toPlayerLocal.z / 80f);
            sensor.AddObservation(dn);
            sensor.AddObservation(Mathf.Clamp(o.aimAngleDeg / 30f, 0f, 2f));
            sensor.AddObservation(Mathf.Clamp(o.aimOffsetXZ.x / 3f, -2f, 2f));
            sensor.AddObservation(Mathf.Clamp(o.aimOffsetXZ.y / 3f, -2f, 2f));
            sensor.AddObservation(Mathf.Clamp(o.aimAngularVelocity / 400f, -3f, 3f));
            sensor.AddObservation(Mathf.Clamp01(o.timeSinceShot / 2f));
            sensor.AddObservation(o.shotThisStep ? 1f : 0f);
            sensor.AddObservation(o.velocityXZ.x / 8f);
            sensor.AddObservation(o.velocityXZ.y / 8f);
            sensor.AddObservation(o.hasCover ? (o.hidden ? 1f : 0.5f) : 0f);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            var c = actions.ContinuousActions;
            LastAction = new Vector2(Mathf.Clamp(c[0], -1f, 1f), Mathf.Clamp(c[1], -1f, 1f));
            LastHide = actions.DiscreteActions.Length > 0 && actions.DiscreteActions[0] == 1;
            if (Brain == null) return;
            var o = Brain.Observation;
            float r = RewardShaping.Step(_hitThisStep, LastAction.magnitude, o.aimAngleDeg, o.shotThisStep, o.hidden);
            AddReward(r);
            if (_hitThisStep)
            {
                _hitThisStep = false;
                if (Brain.IsTraining) EndEpisode();
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            // Heuristic = the scripted dodge, so the scene plays without a model.
            var c = actionsOut.ContinuousActions;
            var v = Brain != null ? Brain.ScriptedAction() : Vector2.zero;
            c[0] = v.x; c[1] = v.y;
            var d = actionsOut.DiscreteActions;
            if (d.Length > 0) d[0] = 0;
        }
    }
}
