using UnityEngine;
using PracticeRange.Player;

namespace PracticeRange.AI
{
    /// <summary>Everything a dodging brain is allowed to know. Computed once per frame by DodgeBrain and shared by scripted, bandit, and RL policies.</summary>
    public struct DodgeObservation
    {
        public Vector3 toPlayerLocal;      // player position relative to target
        public float distance;
        public float aimAngleDeg;          // angle between player aim ray and direction to target
        public Vector2 aimOffsetXZ;        // where the aim ray passes relative to target, in target's XZ frame (left/right, front/back)
        public float aimAngularVelocity;   // deg/s
        public float timeSinceShot;        // seconds
        public bool shotThisStep;
        public Vector2 velocityXZ;
        public bool hidden;
        public bool hasCover;

        public static DodgeObservation Compute(Transform target, Transform player, PlayerLook look, Weapon weapon, Vector2 velocityXZ, bool hidden, bool hasCover, float lastSeenShot)
        {
            var o = new DodgeObservation();
            var tp = target.position + Vector3.up * 0.8f;
            Vector3 toTarget = tp - look.AimOrigin;
            o.distance = toTarget.magnitude;
            o.toPlayerLocal = target.InverseTransformPoint(player.position);
            Vector3 aim = look.AimDirection;
            o.aimAngleDeg = Vector3.Angle(aim, toTarget);
            // Closest point of the aim ray at the target's depth, expressed relative to target.
            float depth = Vector3.Dot(toTarget, aim);
            Vector3 rayPoint = look.AimOrigin + aim * Mathf.Max(depth, 0.01f);
            Vector3 off = rayPoint - tp;
            Vector3 right = Vector3.Cross(Vector3.up, toTarget.normalized);
            o.aimOffsetXZ = new Vector2(Vector3.Dot(off, right), off.y);
            o.aimAngularVelocity = look.AimAngularVelocity;
            o.timeSinceShot = Time.time - weapon.LastShotTime;
            o.shotThisStep = weapon.LastShotTime > lastSeenShot;
            o.velocityXZ = velocityXZ;
            o.hidden = hidden; o.hasCover = hasCover;
            return o;
        }
    }
}
