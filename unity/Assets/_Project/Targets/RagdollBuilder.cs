using System.Collections.Generic;
using UnityEngine;

namespace PracticeRange.Targets
{
    /// <summary>Builds a faceless two-tone mannequin from primitives: 9 rigidbodies joined by CharacterJoints.
    /// Pelvis, spine, head, 2 upper arms, 2 forearms, 2 legs (thigh+shin merged). No skin, no face, no textures.</summary>
    public static class RagdollBuilder
    {
        public struct Result
        {
            public Transform root;
            public Rigidbody pelvis;
            public Rigidbody head;
            public List<Rigidbody> bodies;
            public List<Renderer> renderers;
        }

        public static Result Build(Transform parent, Color primary, Color secondary, float scale = 1f)
        {
            var res = new Result { bodies = new List<Rigidbody>(), renderers = new List<Renderer>() };
            var root = new GameObject("Mannequin").transform;
            root.SetParent(parent, false);
            res.root = root;

            var pelvis = Segment(root, "Pelvis", new Vector3(0, 1.0f, 0) * scale, new Vector3(0.36f, 0.22f, 0.22f) * scale, primary, PrimitiveType.Capsule, 6f, false);
            var spine = Segment(root, "Spine", new Vector3(0, 1.38f, 0) * scale, new Vector3(0.38f, 0.5f, 0.24f) * scale, secondary, PrimitiveType.Capsule, 8f, false);
            var head = Segment(root, "Head", new Vector3(0, 1.82f, 0) * scale, new Vector3(0.24f, 0.26f, 0.24f) * scale, primary, PrimitiveType.Sphere, 2f, true);
            var lUpper = Segment(root, "L_UpperArm", new Vector3(-0.30f, 1.52f, 0) * scale, new Vector3(0.12f, 0.30f, 0.12f) * scale, secondary, PrimitiveType.Capsule, 1.5f, false);
            var rUpper = Segment(root, "R_UpperArm", new Vector3(0.30f, 1.52f, 0) * scale, new Vector3(0.12f, 0.30f, 0.12f) * scale, secondary, PrimitiveType.Capsule, 1.5f, false);
            var lFore = Segment(root, "L_Forearm", new Vector3(-0.30f, 1.22f, 0) * scale, new Vector3(0.10f, 0.30f, 0.10f) * scale, primary, PrimitiveType.Capsule, 1f, false);
            var rFore = Segment(root, "R_Forearm", new Vector3(0.30f, 1.22f, 0) * scale, new Vector3(0.10f, 0.30f, 0.10f) * scale, primary, PrimitiveType.Capsule, 1f, false);
            var lLeg = Segment(root, "L_Leg", new Vector3(-0.11f, 0.48f, 0) * scale, new Vector3(0.14f, 0.92f, 0.14f) * scale, secondary, PrimitiveType.Capsule, 5f, false);
            var rLeg = Segment(root, "R_Leg", new Vector3(0.11f, 0.48f, 0) * scale, new Vector3(0.14f, 0.92f, 0.14f) * scale, secondary, PrimitiveType.Capsule, 5f, false);

            Joint(spine, pelvis, new Vector3(0, -0.25f, 0), 20, 10);
            Joint(head, spine, new Vector3(0, -0.13f, 0), 30, 20);
            Joint(lUpper, spine, new Vector3(0, 0.15f, 0), 60, 40);
            Joint(rUpper, spine, new Vector3(0, 0.15f, 0), 60, 40);
            Joint(lFore, lUpper, new Vector3(0, 0.15f, 0), 70, 5);
            Joint(rFore, rUpper, new Vector3(0, 0.15f, 0), 70, 5);
            Joint(lLeg, pelvis, new Vector3(0, 0.46f, 0), 50, 20);
            Joint(rLeg, pelvis, new Vector3(0, 0.46f, 0), 50, 20);

            foreach (var rb in new[] { pelvis, spine, head, lUpper, rUpper, lFore, rFore, lLeg, rLeg })
            {
                rb.isKinematic = true;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                res.bodies.Add(rb);
                res.renderers.Add(rb.GetComponent<Renderer>());
            }
            res.pelvis = pelvis;
            res.head = head;
            return res;
        }

        static Rigidbody Segment(Transform root, string name, Vector3 pos, Vector3 size, Color color, PrimitiveType type, float mass, bool isHead)
        {
            var go = PracticeRange.Core.Primitives.Create(type);
            go.name = name;
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            var m = PracticeRange.Core.Shaders.NewUnlit(color);
            r.sharedMaterial = m;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            var zone = go.AddComponent<HitZone>();
            zone.isHead = isHead;
            zone.forceMultiplier = isHead ? 2f : 1f;
            return rb;
        }

        static void Joint(Rigidbody child, Rigidbody parent, Vector3 anchorLocal, float swing, float twist)
        {
            var j = child.gameObject.AddComponent<CharacterJoint>();
            j.connectedBody = parent;
            j.anchor = anchorLocal;
            j.enableProjection = true;
            j.lowTwistLimit = new SoftJointLimit { limit = -twist };
            j.highTwistLimit = new SoftJointLimit { limit = twist };
            j.swing1Limit = new SoftJointLimit { limit = swing };
            j.swing2Limit = new SoftJointLimit { limit = swing };
            j.enableCollision = false;
        }
    }
}
