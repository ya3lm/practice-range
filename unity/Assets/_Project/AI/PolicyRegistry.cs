using Unity.InferenceEngine;
using UnityEngine;
using PracticeRange.Core;

namespace PracticeRange.AI
{
    /// <summary>Trained ONNX policies, one per mode, loaded from Resources/Models/Dodge{Mode}.onnx. Missing model → heuristic (scripted) behaviour.</summary>
    public static class PolicyRegistry
    {
        static readonly ModelAsset[] _cache = new ModelAsset[3];
        static readonly bool[] _tried = new bool[3];

        public static ModelAsset ModelFor(RangeMode mode)
        {
            int i = (int)mode;
            if (!_tried[i])
            {
                _tried[i] = true;
                _cache[i] = Resources.Load<ModelAsset>("Models/Dodge" + mode);
            }
            return _cache[i];
        }

        public static bool HasModel(RangeMode mode) => ModelFor(mode) != null;
    }
}
