using UnityEngine;

namespace PracticeRange.Core
{
    /// <summary>Material factory that works in stripped player builds. GameLoop / TrainingArenaConfigurator hand over serialized
    /// base materials at startup; runtime materials are copies. Shader.Find is only an Editor fallback.</summary>
    public static class Shaders
    {
        static Shader _unlit;
        public static Material Base;            // Base.mat, URP Unlit opaque
        public static Material BaseTransparent; // BaseTransparent.mat, URP Unlit alpha-blended (kill rings, shatter fade)
        public static Material BaseGrid;        // Grid.mat, PracticeRange/Grid

        /// <summary>HDR multiplier for emissive accents. Above 1 so URP bloom picks them up.</summary>
        public const float AccentIntensity = 2.8f;

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
            SetColor(m, c);
            return m;
        }

        /// <summary>Accent colour pushed above 1.0 so it glows under bloom.</summary>
        public static Material NewEmissive(Color c, float intensity = AccentIntensity) => NewUnlit(c * intensity);

        public static Material NewTransparent(Color c)
        {
            Material m;
            if (BaseTransparent != null) m = new Material(BaseTransparent);
            else
            {
                m = new Material(Unlit);
                m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = 3000;
            }
            SetColor(m, c);
            return m;
        }

        public static Material NewGrid(Color baseColor, Color line, float lineIntensity, float minorStrength = 0.22f)
        {
            if (BaseGrid == null) return NewUnlit(baseColor);
            var m = new Material(BaseGrid);
            m.SetColor("_BaseColor", baseColor);
            m.SetColor("_LineColor", line * lineIntensity);
            m.SetFloat("_MinorStrength", minorStrength);
            return m;
        }

        public static void SetColor(Material m, Color c)
        {
            if (m == null) return;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }
    }
}
