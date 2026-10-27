using UnityEngine;
using UnityEngine.InputSystem;
using PracticeRange.Core;

namespace PracticeRange.Player
{
    /// <summary>Stationary first-person look. Sensitivity in cm/360 so it transfers between games.</summary>
    public class PlayerLook : MonoBehaviour
    {
        [SerializeField] Camera cam;
        [SerializeField] float pitchLimit = 89f;

        float _yaw, _pitch;
        public Vector3 AimDirection => cam.transform.forward;
        public Vector3 AimOrigin => cam.transform.position;
        /// <summary>Angular velocity of the crosshair in deg/s, used by the RL observation.</summary>
        public float AimAngularVelocity { get; private set; }
        Vector3 _lastForward;

        void Awake() { if (cam == null) cam = GetComponentInChildren<Camera>(); }

        void Start()
        {
            ApplyFov(SettingsService.Current);
            SettingsService.Changed += ApplyFov;
            _lastForward = cam.transform.forward;
        }

        void OnDestroy() => SettingsService.Changed -= ApplyFov;

        void ApplyFov(SettingsData s)
        {
            cam.fieldOfView = Sensitivity.VerticalFov(s.horizontalFov, cam.aspect);
        }

        void Update()
        {
            var loop = GameLoop.Instance;
            bool locked = Cursor.lockState == CursorLockMode.Locked && (loop == null || !loop.MenuOpen);
            var mouse = Mouse.current;
            if (locked && mouse != null)
            {
                Vector2 delta = mouse.delta.ReadValue();
                var s = SettingsService.Current;
                float degPerCount = Sensitivity.DegreesPerCount(s.cmPer360, s.mouseDpi);
                _yaw += delta.x * degPerCount;
                _pitch -= delta.y * degPerCount;
                _pitch = Mathf.Clamp(_pitch, -pitchLimit, pitchLimit);
                transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
                cam.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
            if (Time.deltaTime > 0f)
            {
                AimAngularVelocity = Vector3.Angle(_lastForward, cam.transform.forward) / Time.deltaTime;
                _lastForward = cam.transform.forward;
            }
        }

        public void ResetView()
        {
            _yaw = 0f; _pitch = 0f;
            transform.rotation = Quaternion.identity;
            cam.transform.localRotation = Quaternion.identity;
        }
    }
}
