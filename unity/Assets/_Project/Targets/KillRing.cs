using UnityEngine;
using PracticeRange.Core;

namespace PracticeRange.Targets
{
    /// <summary>Expanding, fading ring on a camera-facing quad. Procedural ring texture generated once. The "kill" read for video.</summary>
    public class KillRing : MonoBehaviour
    {
        static Texture2D _ring;
        float _t, _life = 0.35f, _from = 0.6f, _to = 3.2f;
        Material _m;
        Color _c;

        public static void Spawn(Vector3 center, Color color, float size = 1f)
        {
            if (_ring == null) _ring = MakeRing(256);
            var go = Primitives.Create(PrimitiveType.Quad);
            go.name = "KillRing";
            Object.Destroy(go.GetComponent<Collider>());
            go.layer = LayerMask.NameToLayer("Ignore Raycast");
            go.transform.position = center;
            var r = go.GetComponent<Renderer>();
            var m = Shaders.NewTransparent(color * Shaders.AccentIntensity);
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", _ring); else m.mainTexture = _ring;
            r.sharedMaterial = m;
            var k = go.AddComponent<KillRing>();
            k._m = m; k._c = color * Shaders.AccentIntensity; k._from *= size; k._to *= size;
        }

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_t / _life);
            float e = 1f - (1f - k) * (1f - k); // ease-out
            transform.localScale = Vector3.one * Mathf.Lerp(_from, _to, e);
            var cam = Camera.main; if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            var c = _c; c.a = 1f - k;
            Shaders.SetColor(_m, c);
            if (k >= 1f) Destroy(gameObject);
        }

        static Texture2D MakeRing(int n)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c; // 0 center .. 1 edge
                    float ring = Mathf.Exp(-Mathf.Pow((d - 0.82f) / 0.06f, 2f));         // thin gaussian ring
                    byte a = (byte)(255f * Mathf.Clamp01(ring));
                    px[y * n + x] = new Color32(255, 255, 255, a);
                }
            t.SetPixels32(px); t.Apply(false, true);
            return t;
        }
    }
}
