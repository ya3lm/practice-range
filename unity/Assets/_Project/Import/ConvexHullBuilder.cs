using System;
using System.Collections.Generic;
using UnityEngine;

namespace PracticeRange.Import
{
    /// <summary>3D quickhull plus a grid-clustering decimation loop so the hull fits Unity's 255-triangle convex MeshCollider cap.
    /// Pure C#, no UnityEngine dependency beyond Vector3. Tested in EditMode.</summary>
    public static class ConvexHullBuilder
    {
        public const int MaxTriangles = 255;

        public struct Hull { public Vector3[] vertices; public int[] triangles; public int TriangleCount => triangles.Length / 3; }

        /// <summary>Returns a convex hull with at most maxTriangles triangles. Clusters input points on an increasingly coarse grid until it fits.</summary>
        public static Hull Build(IList<Vector3> points, int maxTriangles = MaxTriangles)
        {
            if (points == null || points.Count < 4) throw new ArgumentException("Need at least 4 points");
            var pts = new List<Vector3>(points);
            Bounds b = new Bounds(pts[0], Vector3.zero);
            foreach (var p in pts) b.Encapsulate(p);
            float cell = b.size.magnitude / 64f;
            Hull h = QuickHull(pts);
            int guard = 0;
            while (h.TriangleCount > maxTriangles && guard++ < 12)
            {
                pts = Cluster(pts, cell);
                cell *= 1.5f;
                if (pts.Count < 4) break;
                h = QuickHull(pts);
            }
            if (h.TriangleCount > maxTriangles)
            {
                // Last resort: box hull of the bounds. Always 12 triangles.
                h = BoxHull(b);
            }
            return h;
        }

        public static List<Vector3> Cluster(List<Vector3> pts, float cell)
        {
            var map = new Dictionary<(int, int, int), (Vector3 sum, int n)>();
            foreach (var p in pts)
            {
                var k = (Mathf.FloorToInt(p.x / cell), Mathf.FloorToInt(p.y / cell), Mathf.FloorToInt(p.z / cell));
                if (map.TryGetValue(k, out var v)) map[k] = (v.sum + p, v.n + 1); else map[k] = (p, 1);
            }
            var outp = new List<Vector3>(map.Count);
            foreach (var v in map.Values) outp.Add(v.sum / v.n);
            return outp;
        }

        public static Hull BoxHull(Bounds b)
        {
            var mn = b.min; var mx = b.max;
            var v = new[]
            {
                new Vector3(mn.x, mn.y, mn.z), new Vector3(mx.x, mn.y, mn.z), new Vector3(mx.x, mx.y, mn.z), new Vector3(mn.x, mx.y, mn.z),
                new Vector3(mn.x, mn.y, mx.z), new Vector3(mx.x, mn.y, mx.z), new Vector3(mx.x, mx.y, mx.z), new Vector3(mn.x, mx.y, mx.z),
            };
            var t = new[] { 0,2,1, 0,3,2, 4,5,6, 4,6,7, 0,1,5, 0,5,4, 1,2,6, 1,6,5, 2,3,7, 2,7,6, 3,0,4, 3,4,7 };
            return new Hull { vertices = v, triangles = t };
        }

        // ---------- Quickhull ----------
        class Face
        {
            public int a, b, c; public Vector3 n; public float d; public bool dead;
            public List<int> outside = new List<int>();
        }

        public static Hull QuickHull(List<Vector3> p)
        {
            int n = p.Count;
            // Initial tetrahedron from extreme points.
            int i0 = 0, i1 = 0;
            for (int i = 1; i < n; i++) { if (p[i].x < p[i0].x) i0 = i; if (p[i].x > p[i1].x) i1 = i; }
            if (i0 == i1) { i1 = (i0 + 1) % n; }
            int i2 = -1; float best = -1f;
            for (int i = 0; i < n; i++) { float d = DistToLine(p[i], p[i0], p[i1]); if (d > best) { best = d; i2 = i; } }
            if (i2 < 0 || best < 1e-7f) return Degenerate(p);
            Vector3 nrm = Vector3.Cross(p[i1] - p[i0], p[i2] - p[i0]).normalized;
            int i3 = -1; best = -1f;
            for (int i = 0; i < n; i++) { float d = Mathf.Abs(Vector3.Dot(p[i] - p[i0], nrm)); if (d > best) { best = d; i3 = i; } }
            if (i3 < 0 || best < 1e-7f) return Degenerate(p);

            var faces = new List<Face>();
            Vector3 centroid = (p[i0] + p[i1] + p[i2] + p[i3]) * 0.25f;
            AddFace(faces, p, i0, i1, i2, centroid);
            AddFace(faces, p, i0, i1, i3, centroid);
            AddFace(faces, p, i0, i2, i3, centroid);
            AddFace(faces, p, i1, i2, i3, centroid);

            var assigned = new bool[n];
            assigned[i0] = assigned[i1] = assigned[i2] = assigned[i3] = true;
            for (int i = 0; i < n; i++)
            {
                if (assigned[i]) continue;
                foreach (var f in faces) if (Vector3.Dot(f.n, p[i]) - f.d > 1e-6f) { f.outside.Add(i); assigned[i] = true; break; }
            }

            int guard = 0;
            while (guard++ < 100000)
            {
                Face cur = null;
                foreach (var f in faces) if (!f.dead && f.outside.Count > 0) { cur = f; break; }
                if (cur == null) break;
                int far = cur.outside[0]; float fd = -1f;
                foreach (var idx in cur.outside) { float d = Vector3.Dot(cur.n, p[idx]) - cur.d; if (d > fd) { fd = d; far = idx; } }
                var eye = p[far];
                var visible = new List<Face>();
                foreach (var f in faces) if (!f.dead && Vector3.Dot(f.n, eye) - f.d > 1e-6f) visible.Add(f);
                // Horizon edges: edges of visible faces not shared with another visible face.
                var edgeCount = new Dictionary<(int, int), int>();
                foreach (var f in visible) { Count(edgeCount, f.a, f.b); Count(edgeCount, f.b, f.c); Count(edgeCount, f.c, f.a); }
                var horizon = new List<(int, int)>();
                foreach (var kv in edgeCount) if (kv.Value == 1) horizon.Add(kv.Key);
                var orphan = new List<int>();
                foreach (var f in visible) { f.dead = true; foreach (var o in f.outside) if (o != far) orphan.Add(o); }
                var newFaces = new List<Face>();
                foreach (var (ea, eb) in horizon) newFaces.Add(AddFace(faces, p, ea, eb, far, centroid));
                foreach (var o in orphan)
                    foreach (var f in newFaces) if (Vector3.Dot(f.n, p[o]) - f.d > 1e-6f) { f.outside.Add(o); break; }
                if (faces.Count > 50000) break;
            }

            // Compact.
            var remap = new Dictionary<int, int>();
            var verts = new List<Vector3>();
            var tris = new List<int>();
            foreach (var f in faces)
            {
                if (f.dead) continue;
                foreach (var idx in new[] { f.a, f.b, f.c })
                {
                    if (!remap.TryGetValue(idx, out int r)) { r = verts.Count; verts.Add(p[idx]); remap[idx] = r; }
                    tris.Add(r);
                }
            }
            return new Hull { vertices = verts.ToArray(), triangles = tris.ToArray() };
        }

        static void Count(Dictionary<(int, int), int> d, int a, int b)
        {
            var k = a < b ? (a, b) : (b, a);
            d[k] = d.TryGetValue(k, out var c) ? c + 1 : 1;
        }

        static Face AddFace(List<Face> faces, List<Vector3> p, int a, int b, int c, Vector3 inside)
        {
            var n = Vector3.Cross(p[b] - p[a], p[c] - p[a]).normalized;
            float d = Vector3.Dot(n, p[a]);
            if (Vector3.Dot(n, inside) - d > 0f) { (b, c) = (c, b); n = -n; d = -d; }
            var f = new Face { a = a, b = b, c = c, n = n, d = d };
            faces.Add(f);
            return f;
        }

        static float DistToLine(Vector3 p, Vector3 a, Vector3 b)
        {
            var ab = b - a; float len = ab.magnitude; if (len < 1e-9f) return (p - a).magnitude;
            return Vector3.Cross(ab, p - a).magnitude / len;
        }

        static Hull Degenerate(List<Vector3> p)
        {
            var b = new Bounds(p[0], Vector3.zero); foreach (var v in p) b.Encapsulate(v);
            if (b.size.magnitude < 1e-6f) b.Expand(0.01f);
            return BoxHull(b);
        }
    }
}
