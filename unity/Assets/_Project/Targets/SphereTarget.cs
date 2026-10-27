using UnityEngine;

namespace PracticeRange.Targets
{
    /// <summary>Far-range precision target. No joints, so: shrink-and-pop plus fragments.</summary>
    public class SphereTarget : TargetBase
    {
        [SerializeField] Renderer body;

        protected override void Awake()
        {
            base.Awake();
            if (body == null) body = GetComponentInChildren<Renderer>();
            Style = Core.TargetStyle.Sphere;
        }

        public override void ApplyColor(Color c)
        {
            if (_material == null) { _material = PracticeRange.Core.Shaders.NewEmissive(c); body.sharedMaterial = _material; }
            else SetMatColor(_material, c * PracticeRange.Core.Shaders.AccentIntensity);
        }

        protected override void OnDamaged(HitInfo hit)
        {
            StartCoroutine(Dent(hit));
        }

        System.Collections.IEnumerator Dent(HitInfo hit)
        {
            var s0 = transform.localScale;
            SetMatColor(_material, Color.white * 3f);
            transform.localScale = s0 * 0.85f;
            yield return new WaitForSeconds(0.06f);
            transform.localScale = s0;
            SetMatColor(_material, _color * PracticeRange.Core.Shaders.AccentIntensity);
        }

        protected override void OnDeath(HitInfo hit, bool head)
        {
            Shatter.Spawn(transform.position, transform.localScale.x, _color, 6, hit.direction * hit.force * 0.4f, sphereFragments: true);
            KillRing.Spawn(hit.point, _color, 1.4f);
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            StartCoroutine(Pop());
        }

        System.Collections.IEnumerator Pop()
        {
            var s0 = transform.localScale;
            float t = 0f;
            while (t < 0.12f)
            {
                t += Time.deltaTime;
                float k = t / 0.12f;
                transform.localScale = s0 * Mathf.Lerp(1.3f, 0f, k);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
