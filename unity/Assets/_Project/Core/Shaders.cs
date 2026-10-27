using UnityEngine;

namespace PracticeRange.Core
{
    /// <summary>Shader access that works in player builds. Shader.Find only succeeds for shaders already in the build,
    /// so GameLoop / TrainingArenaConfigurator hand over a serialized reference at startup; Shader.Find is the Editor fallback.</summary>
    public static class Shaders
    {
        static Shader _unlit;
        /// <summary>Serialized base material (Base.mat); copying it is the only material path guaranteed to work in a stripped player.</summary>
        public static Material Base;
        public static Shader Unlit
        {
            get
            {
                if (_unlit == null) _unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
                return _unlit;
            }
            set { if (value != null) _unlit = value; }
        }

        public static Material NewUnlit(Color c)
        {
            var m = Base != null ? new Material(Base) : new Material(Unlit);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            return m;
        }
    }
}
