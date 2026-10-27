using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using PracticeRange.Import;

namespace PracticeRange.Tests
{
    public class ConvexHullBuilderTests
    {
        [Test]
        public void Cube_Corners_Give12Triangles()
        {
            var pts = new List<Vector3>();
            for (int i = 0; i < 8; i++) pts.Add(new Vector3((i & 1) * 2 - 1, ((i >> 1) & 1) * 2 - 1, ((i >> 2) & 1) * 2 - 1));
            var h = ConvexHullBuilder.QuickHull(pts);
            Assert.AreEqual(8, h.vertices.Length);
            Assert.AreEqual(12, h.TriangleCount);
        }

        [Test]
        public void InteriorPoints_AreDiscarded()
        {
            var pts = new List<Vector3>();
            for (int i = 0; i < 8; i++) pts.Add(new Vector3((i & 1) * 2 - 1, ((i >> 1) & 1) * 2 - 1, ((i >> 2) & 1) * 2 - 1));
            var rng = new System.Random(1);
            for (int i = 0; i < 500; i++) pts.Add(new Vector3((float)rng.NextDouble() * 1.8f - 0.9f, (float)rng.NextDouble() * 1.8f - 0.9f, (float)rng.NextDouble() * 1.8f - 0.9f));
            var h = ConvexHullBuilder.QuickHull(pts);
            Assert.AreEqual(8, h.vertices.Length);
        }

        [Test]
        public void DenseSphere_Decimates_Under255Triangles()
        {
            var pts = new List<Vector3>();
            var rng = new System.Random(2);
            for (int i = 0; i < 5000; i++) pts.Add(Random(rng).normalized);
            var h = ConvexHullBuilder.Build(pts);
            Assert.LessOrEqual(h.TriangleCount, ConvexHullBuilder.MaxTriangles);
            Assert.Greater(h.TriangleCount, 12, "should be a real hull, not the box fallback");
        }

        [Test]
        public void Hull_Contains_AllInputPoints()
        {
            var pts = new List<Vector3>();
            var rng = new System.Random(5);
            for (int i = 0; i < 300; i++) pts.Add(Random(rng) * 3f);
            var h = ConvexHullBuilder.QuickHull(pts);
            // Every input point must be on or inside every face plane.
            for (int t = 0; t < h.triangles.Length; t += 3)
            {
                var a = h.vertices[h.triangles[t]]; var b = h.vertices[h.triangles[t + 1]]; var c = h.vertices[h.triangles[t + 2]];
                var n = Vector3.Cross(b - a, c - a).normalized;
                foreach (var p in pts) Assert.LessOrEqual(Vector3.Dot(n, p - a), 1e-3f);
            }
        }

        [Test]
        public void Degenerate_Planar_FallsBackToBox()
        {
            var pts = new List<Vector3> { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(0.5f, 0, 0.5f) };
            var h = ConvexHullBuilder.Build(pts);
            Assert.AreEqual(12, h.TriangleCount);
        }

        static Vector3 Random(System.Random r) => new Vector3((float)r.NextDouble() * 2 - 1, (float)r.NextDouble() * 2 - 1, (float)r.NextDouble() * 2 - 1);
    }
}
