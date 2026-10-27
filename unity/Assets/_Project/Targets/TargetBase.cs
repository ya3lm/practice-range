using System;
using UnityEngine;
using PracticeRange.Core;
using PracticeRange.AI;
using PracticeRange.Audio;

namespace PracticeRange.Targets
{
    /// <summary>Common target lifecycle: spawn, take hits, die with an effect, report to GameLoop and the agent.</summary>
    public abstract class TargetBase : MonoBehaviour, IHittable
    {
        public TargetStyle Style { get; protected set; }
        public int Health { get; protected set; } = 1;
        public bool IsDead { get; protected set; }
        public float SpawnTime { get; private set; }
        public DodgeMotor Motor { get; private set; }
        public DodgeAgent Agent { get; private set; }

        public event Action<TargetBase> Killed;
        public event Action<TargetBase, HitInfo> Damaged;

        protected Color _color = Color.cyan;
        protected Material _material;

        protected virtual void Awake()
        {
            Motor = GetComponent<DodgeMotor>();
            Agent = GetComponent<DodgeAgent>();
        }

        public virtual void Initialize(ModeConfig mode, Color color)
        {
            Motor = GetComponent<DodgeMotor>();
            Agent = GetComponent<DodgeAgent>();
            _color = color;
            Health = Mathf.Max(1, mode.health);
            IsDead = false;
            SpawnTime = Time.time;
            ApplyColor(color);
        }

        public abstract void ApplyColor(Color c);

        public virtual void Hit(HitInfo hit)
        {
            if (IsDead) return;
            var zone = hit.collider != null ? hit.collider.GetComponent<HitZone>() : null;
            bool head = zone != null && zone.isHead;
            if (zone != null) hit.force *= zone.forceMultiplier;

            GameLoop.Instance?.RegisterHit(head);
            Agent?.NotifyHit();
            Damaged?.Invoke(this, hit);
            Health--;
            if (Health <= 0) Die(hit, head);
            else { OnDamaged(hit); SfxPlayer.Play(SfxKind.Hit); }
        }

        protected virtual void OnDamaged(HitInfo hit) { }

        protected virtual void Die(HitInfo hit, bool head)
        {
            IsDead = true;
            float reaction = Time.time - SpawnTime;
            GameLoop.Instance?.RegisterKill(reaction);
            SfxPlayer.Play(SfxKind.Kill);
            if (Motor != null) Motor.enabled = false;
            OnDeath(hit, head);
            Killed?.Invoke(this);
        }

        protected abstract void OnDeath(HitInfo hit, bool head);

        protected static Material MakeUnlit(Color c) => PracticeRange.Core.Shaders.NewUnlit(c);

        protected static void SetMatColor(Material m, Color c)
        {
            if (m == null) return;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }
    }
}
