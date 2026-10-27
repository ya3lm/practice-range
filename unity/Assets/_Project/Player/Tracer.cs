using UnityEngine;

namespace PracticeRange.Player
{
    /// <summary>A thin line that fades out. Reads as "range instrument", not "gunfire".</summary>
    [RequireComponent(typeof(LineRenderer))]
    public class Tracer : MonoBehaviour
    {
        [SerializeField] float life = 0.12f;
        LineRenderer _lr;
        float _t;
        Color _c;

        void Awake() { _lr = GetComponent<LineRenderer>(); }

        public void Show(Vector3 from, Vector3 to)
        {
            _lr.positionCount = 2;
            _lr.SetPosition(0, from);
            _lr.SetPosition(1, to);
            _c = _lr.startColor;
            _t = 0f;
        }

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            float a = 1f - _t / life;
            var c = _c; c.a = Mathf.Clamp01(a);
            _lr.startColor = c; _lr.endColor = c;
            if (_t >= life) Destroy(gameObject);
        }
    }
}
