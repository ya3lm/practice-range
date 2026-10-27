using UnityEngine;
using PracticeRange.Targets;

namespace PracticeRange.Player
{
    /// <summary>Foam dart projectile for the Nerf skin. Visible travel time makes the dodge legible on video.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class NerfDart : MonoBehaviour
    {
        [SerializeField] float speed = 60f;
        [SerializeField] float life = 3f;
        float _force;
        LayerMask _mask;
        Rigidbody _rb;
        Vector3 _lastPos, _vel;

        void Awake() { _rb = GetComponent<Rigidbody>(); _rb.useGravity = false; _rb.isKinematic = true; }

        public void Launch(Vector3 dir, float force, LayerMask mask)
        {
            _force = force; _mask = mask;
            _vel = dir * speed;
            _lastPos = transform.position;
            Destroy(gameObject, life);
        }

        void FixedUpdate()
        {
            // Manual sweep so fast darts never tunnel through thin targets.
            var pos = transform.position;
            var delta = pos - _lastPos;
            float dist = delta.magnitude;
            if (dist > 0f && Physics.Raycast(_lastPos, delta / dist, out var info, dist, _mask, QueryTriggerInteraction.Ignore))
            {
                var h = info.collider.GetComponentInParent<IHittable>();
                h?.Hit(new HitInfo { point = info.point, normal = info.normal, direction = delta / dist, force = _force, collider = info.collider, shooterTime = Time.time });
                Destroy(gameObject);
                return;
            }
            _lastPos = pos;
            transform.position = pos + _vel * Time.fixedDeltaTime;
        }
    }
}
