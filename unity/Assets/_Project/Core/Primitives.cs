using UnityEngine;

namespace PracticeRange.Core
{
    /// <summary>Replacement for GameObject.CreatePrimitive that works in stripped player builds.
    /// CreatePrimitive depends on the built-in default material and on MeshFilter surviving code stripping; neither is guaranteed in a URP WebGL build.
    /// GameLoop / TrainingArenaConfigurator hand over serialized built-in meshes at startup.</summary>
    public static class Primitives
    {
        public static Mesh Cube, Sphere, Capsule, Quad;

        public static void Assign(Mesh[] meshes)
        {
            if (meshes == null || meshes.Length < 4) return;
            Cube = meshes[0]; Sphere = meshes[1]; Capsule = meshes[2]; Quad = meshes[3];
        }

        public static GameObject Create(PrimitiveType type)
        {
            Mesh mesh = type switch
            {
                PrimitiveType.Cube => Cube,
                PrimitiveType.Sphere => Sphere,
                PrimitiveType.Capsule => Capsule,
                PrimitiveType.Quad => Quad,
                _ => null
            };
            if (mesh == null)
            {
#if UNITY_EDITOR
                return GameObject.CreatePrimitive(type); // editor fallback when nothing assigned (tests, training scenes)
#else
                Debug.LogError("[Primitives] no mesh assigned for " + type);
                return new GameObject(type.ToString());
#endif
            }
            var go = new GameObject(type.ToString());
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            switch (type)
            {
                case PrimitiveType.Cube: go.AddComponent<BoxCollider>(); break;
                case PrimitiveType.Sphere: go.AddComponent<SphereCollider>(); break;
                case PrimitiveType.Capsule: { var c = go.AddComponent<CapsuleCollider>(); c.height = 2f; c.radius = 0.5f; c.direction = 1; break; }
                case PrimitiveType.Quad: { var b = go.AddComponent<BoxCollider>(); b.size = new Vector3(1f, 1f, 0.01f); break; }
            }
            return go;
        }
    }
}
