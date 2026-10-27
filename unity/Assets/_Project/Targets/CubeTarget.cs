using UnityEngine;

namespace PracticeRange.Targets
{
    /// <summary>Mid-range target. Off-center hits produce visible rotation: torque = r × F.</summary>
    public class CubeTarget : TargetBase
    {
        [SerializeField] Renderer body;
        [SerializeField] Rigidbody rb;
        [SerializeField] float shatterPieces = 8;

        protected override void Awake()
        {
            base.Awake();
            if (body == null) body = GetComponentInChildren<Renderer>();
            if (rb == null) rb = GetComponent<Rigidbody>();
            Style = Core.TargetStyle.Cube;
        }

        public override void ApplyColor(Color c)
        {
            if (_material == null) { _material = MakeUnlit(c); body.sharedMaterial = _material; }
            else SetMatColor(_material, c);
        }

        protected override void OnDamaged(HitInfo hit)
        {
            // Nudge without killing: small impulse at the hit point so multi-health cubes wobble.
            if (rb != null && !rb.isKinematic) rb.AddForceAtPosition(hit.direction * hit.force * 0.3f, hit.point, ForceMode.Impulse);
            StartCoroutine(Flash());
        }

        System.Collections.IEnumerator Flash()
        {
            SetMatColor(_material, Color.white);
            yield return new WaitForSeconds(0.05f);
            SetMatColor(_material, _color);
        }

        protected override void OnDeath(HitInfo hit, bool head)
        {
            // Let the rigidbody take over, apply the impulse at the hit point, then shatter a beat later.
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.AddForceAtPosition(hit.direction * hit.force, hit.point, ForceMode.Impulse);
            }
            Shatter.Spawn(transform.position, transform.localScale.x, _color, (int)shatterPieces, hit.direction * hit.force * 0.5f);
            Destroy(gameObject, 0.02f);
        }
    }
}
