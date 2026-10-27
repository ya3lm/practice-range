using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace PracticeRange.Import
{
    /// <summary>Opens the browser's file picker (WebGL) or the Editor's file panel, returns bytes + filename.
    /// WebGL side lives in Plugins/WebGL/FileBridge.jslib and calls back via SendMessage("FileBridge", "OnFileLoaded", json).</summary>
    public class FileBridge : MonoBehaviour
    {
        public const int MaxBytes = 10 * 1024 * 1024;
        public static event Action<string, byte[]> FileLoaded;
        public static event Action<string> Failed;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void PR_OpenFilePicker(string accept, int maxBytes, string gameObjectName);
#endif

        void Awake() { gameObject.name = "FileBridge"; DontDestroyOnLoad(gameObject); }

        public void Open()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            PR_OpenFilePicker(".glb,.gltf,.png,.jpg,.jpeg", MaxBytes, gameObject.name);
#elif UNITY_EDITOR
            var path = UnityEditor.EditorUtility.OpenFilePanel("Import target", "", "glb,png,jpg,jpeg");
            if (string.IsNullOrEmpty(path)) return;
            var bytes = System.IO.File.ReadAllBytes(path);
            if (bytes.Length > MaxBytes) { Failed?.Invoke("File exceeds 10 MB"); return; }
            FileLoaded?.Invoke(System.IO.Path.GetFileName(path), bytes);
#else
            Failed?.Invoke("File import is only available in the browser build.");
#endif
        }

        [Serializable] class Payload { public string name; public string base64; public string error; }

        // Called from JS.
        public void OnFileLoaded(string json)
        {
            try
            {
                var p = JsonUtility.FromJson<Payload>(json);
                if (!string.IsNullOrEmpty(p.error)) { Failed?.Invoke(p.error); return; }
                var bytes = Convert.FromBase64String(p.base64);
                if (bytes.Length > MaxBytes) { Failed?.Invoke("File exceeds 10 MB"); return; }
                FileLoaded?.Invoke(p.name, bytes);
            }
            catch (Exception e) { Failed?.Invoke(e.Message); }
        }
    }
}
