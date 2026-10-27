using System.Collections.Generic;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;
using PracticeRange.Targets;

namespace PracticeRange.Import
{
    /// <summary>Turns an uploaded .glb or image into a rigid target template. Rejects skinned meshes (content policy: no ragdolling uploads). Caps 50k triangles.</summary>
    public class CustomTargetImporter : MonoBehaviour
    {
        public const int MaxTriangles = 50000;
        public const float FitSize = 2f;
        public static event System.Action<string> Status;

        void OnEnable() { FileBridge.FileLoaded += OnFile; }
        void OnDisable() { FileBridge.FileLoaded -= OnFile; }

        async void OnFile(string name, byte[] bytes)
        {
            Status?.Invoke("Importing " + name + "…");
            try
            {
                var lower = name.ToLowerInvariant();
                GameObject template = lower.EndsWith(".glb") || lower.EndsWith(".gltf") ? await ImportGltf(bytes) : ImportImage(bytes);
                if (template == null) return;
                if (CustomTarget.ImportedTemplate != null) Destroy(CustomTarget.ImportedTemplate);
                template.SetActive(false);
                CustomTarget.ImportedTemplate = template;
                Status?.Invoke("Imported " + name + ". Select target style: Custom.");
            }
            catch (System.Exception e) { Status?.Invoke("Import failed: " + e.Message); }
        }

        async Task<GameObject> ImportGltf(byte[] bytes)
        {
            var gltf = new GltfImport();
            bool ok = await gltf.Load(bytes);
            if (!ok) { Status?.Invoke("Not a valid glTF/GLB."); return null; }
            var root = new GameObject("ImportedTemplate");
            ok = await gltf.InstantiateMainSceneAsync(root.transform);
            if (!ok) { Status?.Invoke("Could not instantiate scene."); Destroy(root); return null; }

            if (root.GetComponentInChildren<SkinnedMeshRenderer>() != null)
            {
                Status?.Invoke("Skinned meshes are not accepted. Export as a static mesh.");
                Destroy(root); return null;
            }
            var filters = root.GetComponentsInChildren<MeshFilter>();
            int tris = 0; var pts = new List<Vector3>();
            foreach (var mf in filters)
            {
                if (mf.sharedMesh == null) continue;
                tris += (int)(mf.sharedMesh.GetIndexCount(0) / 3);
                var verts = mf.sharedMesh.vertices;
                int step = Mathf.Max(1, verts.Length / 4000);
                for (int i = 0; i < verts.Length; i += step) pts.Add(mf.transform.TransformPoint(verts[i]));
            }
            if (tris > MaxTriangles) { Status?.Invoke($"Too many triangles ({tris:N0} > {MaxTriangles:N0})."); Destroy(root); return null; }
            if (pts.Count < 4) { Status?.Invoke("Mesh has no geometry."); Destroy(root); return null; }

            Normalize(root, pts);
            AttachHull(root, pts);
            return root;
        }

        GameObject ImportImage(byte[] bytes)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
            if (!tex.LoadImage(bytes)) { Status?.Invoke("Not a valid image."); return null; }
            if (tex.width > 2048 || tex.height > 2048) { Status?.Invoke("Image larger than 2048². Resize it."); Destroy(tex); return null; }
            var root = new GameObject("ImportedTemplate");
            var quad = PracticeRange.Core.Primitives.Create(PrimitiveType.Quad);
            quad.transform.SetParent(root.transform, false);
            float aspect = tex.width / (float)tex.height;
            quad.transform.localScale = new Vector3(FitSize * Mathf.Min(1f, aspect), FitSize * Mathf.Min(1f, 1f / aspect), 1f);
            quad.transform.localRotation = Quaternion.Euler(0, 180f, 0); // face the player (who looks down +Z)
            var m = PracticeRange.Core.Shaders.NewUnlit(Color.white);
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex); else m.mainTexture = tex;
            if (m.HasProperty("_Surface")) { m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.renderQueue = 3000; m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.SetOverrideTag("RenderType", "Transparent"); }
            quad.GetComponent<Renderer>().sharedMaterial = m;
            Destroy(quad.GetComponent<Collider>());
            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(quad.transform.localScale.x, quad.transform.localScale.y, 0.1f);
            CustomTarget.ImportedSize = quad.transform.localScale.y;
            return root;
        }

        static void Normalize(GameObject root, List<Vector3> pts)
        {
            var b = new Bounds(pts[0], Vector3.zero); foreach (var p in pts) b.Encapsulate(p);
            float scale = FitSize / Mathf.Max(b.size.x, b.size.y, b.size.z, 1e-4f);
            root.transform.localScale = Vector3.one * scale;
            // Re-center on the bottom of the bounds so it sits on the floor.
            var offset = -(b.center - Vector3.up * b.extents.y) * scale;
            foreach (Transform child in root.transform) child.localPosition += offset;
            for (int i = 0; i < pts.Count; i++) pts[i] = (pts[i] - (b.center - Vector3.up * b.extents.y)) * scale;
            CustomTarget.ImportedSize = b.size.y * scale;
        }

        static void AttachHull(GameObject root, List<Vector3> pts)
        {
            var hull = ConvexHullBuilder.Build(pts);
            var mesh = new Mesh { name = "ImportedHull" };
            mesh.SetVertices(hull.vertices);
            mesh.SetTriangles(hull.triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var mc = root.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = true;
            Debug.Log($"[Import] Convex hull: {hull.vertices.Length} verts, {hull.TriangleCount} tris");
        }
    }
}
