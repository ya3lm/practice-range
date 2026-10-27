using UnityEngine;
using Unity.MLAgents;
using PracticeRange.Core;
using PracticeRange.Targets;

namespace PracticeRange.AI
{
    /// <summary>One self-contained arena for training: a synthetic player, a target, episode reset. Tiled N times by the Training scene.</summary>
    public class TrainingArena : MonoBehaviour
    {
        [SerializeField] TargetSpawner spawner;
        [SerializeField] SyntheticPlayer synthetic;
        [SerializeField] float maxEpisodeSeconds = 10f;

        TargetBase _target;
        float _episodeStart;

        void Start() => ResetEpisode();

        void Update()
        {
            if (_target == null || _target.IsDead || Time.time - _episodeStart > maxEpisodeSeconds)
            {
                if (_target != null && !_target.IsDead && _target.Agent != null) _target.Agent.EndEpisode(); // survived: episode ends with accumulated reward
                ResetEpisode();
            }
        }

        void ResetEpisode()
        {
            spawner.DespawnAll();
            _target = spawner.Spawn();
            var brain = _target.GetComponent<DodgeBrain>();
            brain.IsTraining = true;
            synthetic.Target = _target.transform;
            _episodeStart = Time.time;
        }
    }
}
