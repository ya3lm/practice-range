using UnityEngine;
using PracticeRange.Core;
using PracticeRange.Targets;

namespace PracticeRange.AI
{
    /// <summary>Training scenes have no GameLoop; this configures the spawner for the arena's mode at startup. Runs before TrainingArena.Start (script order via Awake).</summary>
    [DefaultExecutionOrder(-50)]
    public class TrainingArenaConfigurator : MonoBehaviour
    {
        [SerializeField] ModeConfig mode;
        [SerializeField] TargetSpawner spawner;
        [SerializeField] Shader unlitShader;
        [SerializeField] Mesh[] primitiveMeshes;
        [SerializeField] Material baseMaterial;
        void Awake()
        {
            Shaders.Unlit = unlitShader;
            Shaders.Base = baseMaterial;
            Primitives.Assign(primitiveMeshes);
            spawner.IsTraining = true;
            spawner.Configure(mode, mode.defaultStyle, mode.accent, AiLevel.Trained);
        }
    }
}
