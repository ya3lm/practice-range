using System.Collections.Generic;
using UnityEngine;
using PracticeRange.Core;
using PracticeRange.AI;

namespace PracticeRange.Targets
{
    /// <summary>Spawns one target of the configured style inside the mode's distance band; respawns on kill. Builds targets procedurally, no prefabs needed.</summary>
    public class TargetSpawner : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] Transform arenaRoot;
        [SerializeField] int simultaneous = 1;
        [SerializeField] float respawnDelay = 0.25f;

        ModeConfig _mode;
        TargetStyle _style;
        Color _color;
        AiLevel _ai;
        readonly List<TargetBase> _alive = new List<TargetBase>();
        readonly List<Transform> _cover = new List<Transform>();
        Bounds _arena;
        public BanditLearner SharedBandit { get; } = new BanditLearner(seed: 1);

        public IReadOnlyList<TargetBase> Alive => _alive;
        /// <summary>Set by TrainingArenaConfigurator so agents are created in BehaviorType.Default (trainer-connected).</summary>
        public bool IsTraining { get; set; }
        public ModeConfig Mode => _mode;

        public void Configure(ModeConfig mode, TargetStyle style, Color color, AiLevel ai)
        {
            _mode = mode; _style = style; _color = color; _ai = ai;
            BuildArena();
        }

        public void ApplyStyle(TargetStyle style, Color color, AiLevel ai)
        {
            _style = style; _color = color; _ai = ai;
            foreach (var t in _alive) t.ApplyColor(color);
        }

        void BuildArena()
        {
            if (arenaRoot == null) { arenaRoot = new GameObject("Arena").transform; arenaRoot.SetParent(transform, false); }
            for (int i = arenaRoot.childCount - 1; i >= 0; i--) Destroy(arenaRoot.GetChild(i).gameObject);
            _cover.Clear();
            float w = _mode.arenaHalfWidth, d = _mode.arenaHalfDepth;
            // Targets never come closer than half the mode's minimum spawn distance (a target at the player's feet is not a drill).
            float standoff = Mathf.Max(2f, _mode.minDistance * 0.5f);
            _arena = new Bounds(transform.position + new Vector3(0, 0, (d + standoff) * 0.5f), new Vector3(w * 2f, 10f, d - standoff));
            var floor = PracticeRange.Core.Primitives.Create(PrimitiveType.Cube);
            floor.name = "Floor"; floor.transform.SetParent(arenaRoot, false);
            floor.transform.localScale = new Vector3(w * 2f + 4f, 0.2f, d + 10f);
            floor.transform.localPosition = new Vector3(0, -0.1f, d * 0.5f);
            Tint(floor, _mode.floor);
            // Back wall and side walls, so stray shots land somewhere and the space reads as a room.
            Wall(new Vector3(0, 3f, d + 4f), new Vector3(w * 2f + 4f, 6f, 0.3f));
            Wall(new Vector3(-w - 2f, 3f, d * 0.5f), new Vector3(0.3f, 6f, d + 10f));
            Wall(new Vector3(w + 2f, 3f, d * 0.5f), new Vector3(0.3f, 6f, d + 10f));
            if (_mode.hasCover)
            {
                var rng = new System.Random(42);
                for (int i = 0; i < _mode.coverCount; i++)
                {
                    float x = Mathf.Lerp(-w * 0.8f, w * 0.8f, (i + 0.5f) / _mode.coverCount);
                    float z = Mathf.Lerp(_mode.minDistance, _mode.maxDistance, (float)rng.NextDouble());
                    var c = PracticeRange.Core.Primitives.Create(PrimitiveType.Cube);
                    c.name = "Cover"; c.transform.SetParent(arenaRoot, false);
                    c.transform.localScale = new Vector3(2.2f, 1.6f, 0.6f);
                    c.transform.localPosition = new Vector3(x, 0.8f, z);
                    Tint(c, _mode.floor * 1.6f);
                    _cover.Add(c.transform);
                }
            }
        }

        void Wall(Vector3 pos, Vector3 size)
        {
            var g = PracticeRange.Core.Primitives.Create(PrimitiveType.Cube);
            g.name = "Wall"; g.transform.SetParent(arenaRoot, false);
            g.transform.localPosition = pos; g.transform.localScale = size;
            Tint(g, _mode.background * 1.3f);
        }

        static void Tint(GameObject g, Color c)
        {
            var r = g.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = PracticeRange.Core.Shaders.NewUnlit(c);
        }

        public void SpawnInitial()
        {
            DespawnAll();
            for (int i = 0; i < simultaneous; i++) Spawn();
        }

        public void DespawnAll()
        {
            foreach (var t in _alive) if (t != null) Destroy(t.gameObject);
            _alive.Clear();
        }

        public Vector3 RandomSpawnPoint()
        {
            float z = Random.Range(_mode.minDistance, _mode.maxDistance);
            float lateral = Mathf.Min(_mode.arenaHalfWidth * 0.7f, z * Mathf.Tan(_mode.maxLateralAngleDeg * Mathf.Deg2Rad) * 0.8f);
            float x = Random.Range(-lateral, lateral);
            return transform.position + new Vector3(x, 0f, z);
        }

        void SpawnOne() => Spawn();

        public TargetBase Spawn()
        {
            var style = _style == TargetStyle.Custom && CustomTarget.ImportedTemplate == null ? _mode.defaultStyle : _style;
            var go = new GameObject("Target_" + style);
            go.transform.SetParent(transform, false);
            go.transform.position = RandomSpawnPoint();

            TargetBase target;
            float s = _mode.targetScale;
            switch (style)
            {
                case TargetStyle.Sphere:
                {
                    var body = PracticeRange.Core.Primitives.Create(PrimitiveType.Sphere);
                    body.transform.SetParent(go.transform, false);
                    body.transform.localPosition = Vector3.up * s * 0.6f;
                    body.transform.localScale = Vector3.one * s;
                    target = go.AddComponent<SphereTarget>();
                    break;
                }
                case TargetStyle.Mannequin:
                    target = go.AddComponent<MannequinTarget>();
                    break;
                case TargetStyle.Custom:
                    target = go.AddComponent<CustomTarget>();
                    break;
                default:
                {
                    var body = PracticeRange.Core.Primitives.Create(PrimitiveType.Cube);
                    body.transform.SetParent(go.transform, false);
                    body.transform.localPosition = Vector3.up * s * 0.6f;
                    body.transform.localScale = Vector3.one * s;
                    var rb = go.AddComponent<Rigidbody>();
                    rb.isKinematic = true; rb.useGravity = false;
                    target = go.AddComponent<CubeTarget>();
                    break;
                }
            }

            var motor = go.AddComponent<DodgeMotor>();
            motor.Configure(_mode, _arena, transform.position.y);
            if (_cover.Count > 0) motor.SetCover(NearestCover(go.transform.position));

            var brain = go.AddComponent<DodgeBrain>();
            brain.Setup(_ai, player, _mode, SharedBandit, null, IsTraining);

            target.Initialize(_mode, _color);
            target.Killed += OnKilled;
            _alive.Add(target);
            return target;
        }

        Transform NearestCover(Vector3 p)
        {
            Transform best = null; float bd = float.MaxValue;
            foreach (var c in _cover) { float d = (c.position - p).sqrMagnitude; if (d < bd) { bd = d; best = c; } }
            return best;
        }

        void OnKilled(TargetBase t)
        {
            _alive.Remove(t);
            if (GameLoop.Instance != null && GameLoop.Instance.Phase == GamePhase.Round) Invoke(nameof(SpawnOne), respawnDelay);
        }
    }
}
