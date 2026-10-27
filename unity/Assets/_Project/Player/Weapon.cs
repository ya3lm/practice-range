using UnityEngine;
using UnityEngine.InputSystem;
using PracticeRange.Core;
using PracticeRange.Targets;
using PracticeRange.Audio;

namespace PracticeRange.Player
{
    /// <summary>Hitscan weapon. No gun mesh. Fires a tracer (default) or a foam dart (Nerf skin).</summary>
    public class Weapon : MonoBehaviour
    {
        [SerializeField] PlayerLook look;
        [SerializeField] float range = 300f;
        [SerializeField] float fireInterval = 0.08f;
        [SerializeField] float hitForce = 12f;
        [SerializeField] LayerMask hitMask = ~0;
        [SerializeField] Tracer tracerPrefab;
        [SerializeField] NerfDart dartPrefab;

        float _cooldown;
        public bool AutomatedFire { get; set; } // set by SyntheticPlayer in training scenes
        public float LastShotTime { get; private set; } = -10f;
        public int ShotsFired { get; private set; }

        void Awake() { if (look == null) look = GetComponentInParent<PlayerLook>(); }

        void Update()
        {
            _cooldown -= Time.deltaTime;
            if (AutomatedFire) return;
            var loop = GameLoop.Instance;
            if (loop != null && (loop.MenuOpen || loop.Phase != GamePhase.Round)) return;
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && _cooldown <= 0f) Fire();
        }

        public void Fire()
        {
            _cooldown = fireInterval;
            LastShotTime = Time.time;
            ShotsFired++;
            GameLoop.Instance?.RegisterShot();
            var origin = look.AimOrigin;
            var dir = look.AimDirection;
            var skin = SettingsService.Current.weaponSkin;

            if (skin == WeaponSkin.Nerf && dartPrefab != null)
            {
                var dart = Instantiate(dartPrefab, origin + dir * 0.4f, Quaternion.LookRotation(dir));
                dart.Launch(dir, hitForce, hitMask);
                SfxPlayer.Play(SfxKind.Shoot);
                return;
            }

            Vector3 end = origin + dir * range;
            bool hit = Physics.Raycast(origin, dir, out var info, range, hitMask, QueryTriggerInteraction.Ignore);
            if (hit)
            {
                end = info.point;
                var hittable = info.collider.GetComponentInParent<IHittable>();
                if (hittable != null)
                {
                    hittable.Hit(new HitInfo
                    {
                        point = info.point,
                        normal = info.normal,
                        direction = dir,
                        force = hitForce,
                        collider = info.collider,
                        shooterTime = Time.time
                    });
                }
            }
            if (tracerPrefab != null)
            {
                var t = Instantiate(tracerPrefab);
                t.Show(origin + dir * 0.4f + Vector3.down * 0.15f + look.transform.right * 0.12f, end);
            }
            SfxPlayer.Play(SfxKind.Shoot);
        }
    }
}
