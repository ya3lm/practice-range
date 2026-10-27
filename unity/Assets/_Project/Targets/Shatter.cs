using UnityEngine;

namespace PracticeRange.Targets
{
    /// <summary>Spawns N small rigidbody fragments that fly, fall, shrink, and despawn. Procedural, no prefab.</summary>
    public class Shatter : MonoBehaviour
    {
        static Material _sharedMat;
        float _life = 1.2f, _t;
        Vector3 _s0;

        public static void Spawn(Vector3 center, float size, Color color, int count, Vector3 impulse, bool sphereFragments = false)
        {
            for (int i = 0; i < count; i++)
            {
                var go = PracticeRange.Core.Primitives.Create(sphereFragments ? PrimitiveType.Sphere : PrimitiveType.Cube);
                go.name = "Fragment";
                float fs = size * Random.Range(0.18f, 0.32f);
                go.transform.position = center + Random.insideUnitSphere * size * 0.35f;
                go.transform.rotation = Random.rotation;
                go.transform.localScale = Vector3.one * fs;
                go.layer = LayerMask.NameToLayer("Ignore Raycast");
                var r = go.GetComponent<Renderer>();
                var m = PracticeRange.Core.Shaders.NewEmissive(color, 1.6f);
                r.sharedMaterial = m;
                var rb = go.AddComponent<Rigidbody>();
                rb.mass = 0.2f;
                rb.AddForce(impulse + Random.insideUnitSphere * impulse.magnitude * 0.6f + Vector3.up * 2f, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);
                var s = go.AddComponent<Shatter>();
                s._s0 = go.transform.localScale;
            }
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = _t / _life;
            if (k > 0.6f) transform.localScale = _s0 * Mathf.Lerp(1f, 0f, (k - 0.6f) / 0.4f);
            if (_t >= _life) Destroy(gameObject);
        }
    }
}
