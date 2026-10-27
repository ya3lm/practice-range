using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PracticeRange.Editor
{
    /// <summary>Batch-mode builds. `tools/build_webgl.sh` calls WebGL(). Output: ../build/webgl relative to the Unity project.</summary>
    public static class BuildScript
    {
        static string[] Scenes => new[] { "Assets/_Project/Scenes/Main.unity" };

        [MenuItem("Practice Range/Build WebGL")]
        public static void WebGL()
        {
            string outDir = Arg("-outputPath") ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/webgl"));
            Directory.CreateDirectory(outDir);
            bool webgpu = Arg("-webgpu") != null;
            if (webgpu)
            {
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { UnityEngine.Rendering.GraphicsDeviceType.WebGPU, UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            }
            // -dev: keep managed stack traces in exceptions, but stay on the release link path
            // (6000.6 development WebGL builds fail to link il2cpp's embind bindings on this machine).
            bool dev = Arg("-dev") != null;
            PlayerSettings.WebGL.exceptionSupport = dev ? WebGLExceptionSupport.FullWithStacktrace : WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            var opts = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = outDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            };
            Report(BuildPipeline.BuildPlayer(opts));
        }

        [MenuItem("Practice Range/Build macOS (training headless)")]
        public static void MacTraining()
        {
            string outDir = Arg("-outputPath") ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../build/training/PracticeRangeTraining.app"));
            var mode = Arg("-mode") ?? "Mid";
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { $"Assets/_Project/Scenes/Training_{mode}.unity" },
                locationPathName = outDir,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None,
            };
            Report(BuildPipeline.BuildPlayer(opts));
        }

        static void Report(BuildReport r)
        {
            var s = r.summary;
            Debug.Log($"[Build] {s.result} · {s.totalSize / (1024 * 1024)} MB · {s.totalTime.TotalSeconds:0}s · errors {s.totalErrors} warnings {s.totalWarnings}");
            if (s.result != BuildResult.Succeeded && Application.isBatchMode) EditorApplication.Exit(1);
        }

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == name) return i + 1 < args.Length && !args[i + 1].StartsWith("-") ? args[i + 1] : "";
            return null;
        }
    }
}
