using UnityEngine;

namespace PracticeRange.Targets
{
    public struct HitInfo
    {
        public Vector3 point;
        public Vector3 normal;
        public Vector3 direction;
        public float force;
        public Collider collider;
        public float shooterTime;
    }

    public interface IHittable
    {
        void Hit(HitInfo hit);
    }

    /// <summary>Marks a collider as a special zone (head → slow-mo, 2x force).</summary>
    public class HitZone : MonoBehaviour
    {
        public bool isHead;
        public float forceMultiplier = 1f;
    }
}
