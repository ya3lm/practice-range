using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using PracticeRange.Core;
using PracticeRange.Targets;

namespace PracticeRange.Tests
{
    public class TargetHitTests
    {
        ModeConfig Mode()
        {
            var c = ScriptableObject.CreateInstance<ModeConfig>();
            ModeDefaults.Apply(c, RangeMode.Mid);
            return c;
        }

        [UnityTest]
        public IEnumerator Cube_Dies_OnOneHit_AndRaisesKilled()
        {
            var go = new GameObject("T");
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube); body.transform.SetParent(go.transform, false);
            go.AddComponent<Rigidbody>().isKinematic = true;
            var t = go.AddComponent<CubeTarget>();
            t.Initialize(Mode(), Color.cyan);
            bool killed = false; t.Killed += _ => killed = true;
            yield return null;
            t.Hit(new HitInfo { point = body.transform.position, direction = Vector3.forward, force = 10f, collider = body.GetComponent<Collider>() });
            Assert.IsTrue(killed);
            Assert.IsTrue(t.IsDead);
            yield return new WaitForSeconds(0.1f);
        }

        [UnityTest]
        public IEnumerator Mannequin_Ragdolls_OnDeath_AllBodiesNonKinematic()
        {
            var go = new GameObject("M");
            var t = go.AddComponent<MannequinTarget>();
            var m = Mode(); m.targetScale = 1f;
            t.Initialize(m, Color.white);
            yield return null;
            var bodies = go.GetComponentsInChildren<Rigidbody>();
            Assert.AreEqual(9, bodies.Length);
            foreach (var b in bodies) Assert.IsTrue(b.isKinematic);
            var head = go.transform.Find("Mannequin/Head").GetComponent<Collider>();
            t.Hit(new HitInfo { point = head.transform.position, direction = Vector3.forward, force = 10f, collider = head });
            foreach (var b in bodies) Assert.IsFalse(b.isKinematic);
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Raycast_Hits_MannequinBone_Through_IHittable()
        {
            var go = new GameObject("M"); go.transform.position = new Vector3(0, 0, 5);
            var t = go.AddComponent<MannequinTarget>();
            t.Initialize(Mode(), Color.white);
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(Physics.Raycast(new Vector3(0, 1.4f, 0), Vector3.forward, out var hit, 20f));
            Assert.IsNotNull(hit.collider.GetComponentInParent<IHittable>());
        }
    }
}
