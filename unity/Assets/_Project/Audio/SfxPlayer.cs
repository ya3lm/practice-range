using UnityEngine;
using PracticeRange.Core;

namespace PracticeRange.Audio
{
    public enum SfxKind { Shoot, Hit, Kill, LevelUp, CountdownTick, CountdownGo }

    /// <summary>Single AudioSource pool playing synthesized clips. Static entry point so targets and weapons don't need references.</summary>
    public class SfxPlayer : MonoBehaviour
    {
        static SfxPlayer _inst;
        AudioSource[] _sources;
        int _next;
        AudioClip[] _hit, _kill, _shoot;
        AudioClip _levelUp, _cdTick, _cdGo;

        void Awake()
        {
            if (_inst != null && _inst != this) { Destroy(gameObject); return; }
            _inst = this;
            _sources = new AudioSource[8];
            for (int i = 0; i < _sources.Length; i++) { _sources[i] = gameObject.AddComponent<AudioSource>(); _sources[i].playOnAwake = false; _sources[i].spatialBlend = 0f; }
            _hit = new[] { ProceduralSfx.Tick(0), ProceduralSfx.Tick(1), ProceduralSfx.Tick(2) };
            _kill = new[] { ProceduralSfx.Thump(0), ProceduralSfx.Thump(1), ProceduralSfx.Thump(2) };
            _shoot = new[] { ProceduralSfx.Shoot(0), ProceduralSfx.Shoot(1) };
            _levelUp = ProceduralSfx.LevelUp();
            _cdTick = ProceduralSfx.Countdown(false);
            _cdGo = ProceduralSfx.Countdown(true);
        }

        public static void Play(SfxKind kind)
        {
            if (_inst == null) return;
            var s = SettingsService.Current;
            AudioClip clip = null; float vol = s.volume;
            switch (kind)
            {
                case SfxKind.Shoot: clip = _inst._shoot[s.weaponSkin == WeaponSkin.Nerf ? 1 : 0]; vol *= 0.35f; break;
                case SfxKind.Hit: clip = _inst._hit[Mathf.Clamp(s.hitSound, 0, 2)]; break;
                case SfxKind.Kill: clip = _inst._kill[Mathf.Clamp(s.killSound, 0, 2)]; break;
                case SfxKind.LevelUp: clip = _inst._levelUp; break;
                case SfxKind.CountdownTick: clip = _inst._cdTick; break;
                case SfxKind.CountdownGo: clip = _inst._cdGo; break;
            }
            if (clip == null) return;
            var src = _inst._sources[_inst._next++ % _inst._sources.Length];
            src.pitch = Random.Range(0.97f, 1.03f);
            src.PlayOneShot(clip, vol);
        }

        public static AudioClip Preview(SfxKind kind, int variant)
        {
            if (_inst == null) return null;
            return kind == SfxKind.Hit ? _inst._hit[Mathf.Clamp(variant, 0, 2)] : kind == SfxKind.Kill ? _inst._kill[Mathf.Clamp(variant, 0, 2)] : null;
        }
    }
}
