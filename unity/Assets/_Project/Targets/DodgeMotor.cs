using UnityEngine;
using PracticeRange.Core;

namespace PracticeRange.Targets
{
    /// <summary>Moves a target on the XZ plane toward a desired velocity, clamped to the arena. The brain (scripted, bandit, or RL) only sets DesiredVelocity and Peek.</summary>
    public class DodgeMotor : MonoBehaviour
    {
        public Vector2 DesiredVelocity;   // XZ, in m/s, clamped to maxSpeed
        public bool Hide;                 // far mode: duck behind cover
        public Vector3 Velocity { get; private set; }

        float _maxSpeed = 6f, _accel = 25f;
        Bounds _bounds;
        float _tanLateral = 0.7f;   // tan(maxLateralAngleDeg)
        Vector3 _origin;            // player position (arena origin); lateral clamp is measured from here
        Transform _cover;      // nearest cover block, if any
        float _hideBlend;
        float _baseY;

        public void Configure(ModeConfig mode, Bounds arena, float baseY)
        {
            _maxSpeed = mode.maxSpeed;
            _accel = mode.acceleration;
            _bounds = arena;
            _baseY = baseY;
            _tanLateral = Mathf.Tan(mode.maxLateralAngleDeg * Mathf.Deg2Rad);
            _origin = new Vector3(arena.center.x, baseY, arena.min.z - Mathf.Max(2f, mode.minDistance * 0.5f)); // player position: arena starts one standoff in front of it
        }

        public void SetCover(Transform cover) => _cover = cover;
        public float MaxSpeed => _maxSpeed;

        void Update()
        {
            if (Time.timeScale == 0f) return;
            var desired = Vector2.ClampMagnitude(DesiredVelocity, 1f) * _maxSpeed;
            var v3 = new Vector3(desired.x, 0f, desired.y);
            Velocity = Vector3.MoveTowards(Velocity, v3, _accel * Time.deltaTime);
            var p = transform.position + Velocity * Time.deltaTime;
            p.z = Mathf.Clamp(p.z, _bounds.min.z, _bounds.max.z);
            // Keep the target inside the player's view cone: |x - origin.x| <= (z - origin.z) * tan(maxLateralAngle).
            float lateral = Mathf.Min(_bounds.extents.x, Mathf.Max(0.5f, (p.z - _origin.z) * _tanLateral));
            p.x = Mathf.Clamp(p.x, _origin.x - lateral, _origin.x + lateral);

            // Hide = sink behind cover. Visible as a duck, costs time to recover.
            _hideBlend = Mathf.MoveTowards(_hideBlend, Hide && _cover != null ? 1f : 0f, 4f * Time.deltaTime);
            p.y = _baseY - _hideBlend * 1.6f;
            transform.position = p;
        }

        public bool IsHidden => _hideBlend > 0.7f;
    }
}
