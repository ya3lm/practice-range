using UnityEngine;
using PracticeRange.Core;

namespace PracticeRange.Targets
{
    /// <summary>User-imported mesh or image. Rigid statue only: impulse + shatter, never ragdoll (content policy + no skinned meshes accepted).</summary>
    public class CustomTarget : TargetBase
    {
        public static GameObject ImportedTemplate; // set by CustomTargetImporter; cloned per spawn
        public static float ImportedSize = 1f;

        Rigidbody _rb;
        GameObject _visual;

        protected override void Awake()
        {
            base.Awake();
            Style = TargetStyle.Custom;
        }

        public override void Initialize(ModeConfig mode, Color color)
        {
            if (_visual == null && ImportedTemplate != null)
            {
                _visual = Instantiate(ImportedTemplate, transform);
                _visual.SetActive(true);
                _visual.transform.localPosition = Vector3.up * ImportedSize * 0.5f;
                _rb = gameObject.GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
                _rb.isKinematic = true; _rb.useGravity = false;
            }
            base.Initialize(mode, color);
        }

        public override void ApplyColor(Color c)
        {
            // Imported meshes keep their own materials (a logo should look like the logo). Images tint only on flash.
        }

        protected override void OnDamaged(HitInfo hit)
        {
            if (_rb != null) { _rb.isKinematic = false; _rb.AddForceAtPosition(hit.direction * hit.force * 0.2f, hit.point, ForceMode.Impulse); }
        }

        protected override void OnDeath(HitInfo hit, bool head)
        {
            if (_rb != null)
            {
                _rb.isKinematic = false; _rb.useGravity = true;
                _rb.AddForceAtPosition(hit.direction * hit.force, hit.point, ForceMode.Impulse);
            }
            Shatter.Spawn(transform.position + Vector3.up * ImportedSize * 0.5f, ImportedSize, _color, 8, hit.direction * hit.force * 0.5f);
            Destroy(gameObject, 0.05f);
        }
    }
}
