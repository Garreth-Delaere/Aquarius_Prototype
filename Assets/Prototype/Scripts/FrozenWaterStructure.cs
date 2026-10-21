using System.Collections.Generic;
using UnityEngine;

namespace Aquarius.Prototype
{
    // Owns one committed mass, its snapshot, and its independent collision geometry.
    public sealed class FrozenWaterStructure : MonoBehaviour
    {
        readonly List<WaterStream.Droplet> snapshot = new List<WaterStream.Droplet>();
        public WaterStream Owner { get; private set; }
        public WaterIceSurface Surface { get; private set; }
        public IReadOnlyList<WaterStream.Droplet> Snapshot => snapshot;
        Vector3 restPosition, hoverCentre;
        float hoverTime, contactTimer;
        Vector3[] contactVertices;
        Collider[] touching = new Collider[8];
        Collider[] candidates = new Collider[16];
        readonly HashSet<FrozenWaterStructure> visited = new HashSet<FrozenWaterStructure>();
        public bool IsAnchored { get; private set; }
        public Vector3 Displacement => transform.position - restPosition;
        public bool IsSolid => Surface != null && Surface.IsSolid;

        public bool Build(WaterStream owner, List<WaterStream.Droplet> drops, WaterIceSurface template, out string reason)
        {
            Owner = owner; snapshot.AddRange(drops);
            Surface = gameObject.AddComponent<WaterIceSurface>(); Surface.CopySettingsFrom(template);
            if (!Surface.Build(snapshot, owner.dropletRadius, owner.ignoredPlayer, out reason)) return false;
            restPosition = hoverCentre = transform.position; hoverTime = 0;
            contactVertices = Surface.Collision.sharedMesh.vertices;
            CheckAnchoring();
            return true;
        }
        void FixedUpdate() { SimulateHover(Time.fixedDeltaTime); }
        public void SimulateHover(float dt)
        {
            if (!IsSolid || dt <= 0) return;
            contactTimer -= dt;
            if (contactTimer <= 0) { CheckAnchoring(); contactTimer = 0.2f; }
            if (IsAnchored) return;
            hoverTime += dt;
            float offset = Mathf.Sin(hoverTime * Mathf.Max(0, Surface.hoverFrequency) * Mathf.PI * 2f) * Surface.hoverAmplitude;
            // Move the original closed mesh and its collider together; never recook the mesh.
            transform.position = hoverCentre + Vector3.up * offset;
        }
        public void CheckAnchoring()
        {
            if (!IsSolid) return;
            visited.Clear(); bool connected = TouchesEnvironment(visited);
            if (connected != IsAnchored)
            {
                // Stop exactly where contact occurred; resume from that same place if support is removed.
                hoverCentre = transform.position; hoverTime = 0;
            }
            IsAnchored = connected;
        }
        bool TouchesEnvironment(HashSet<FrozenWaterStructure> chain)
        {
            if (!IsSolid || !chain.Add(this)) return false;
            Bounds bounds = Surface.Collision.bounds;
            float tolerance = Mathf.Max(0.001f, Surface.anchorContactTolerance);
            int count;
            while (true)
            {
                count = Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents + Vector3.one * tolerance,
                    candidates, Quaternion.identity, Owner.environmentMask, QueryTriggerInteraction.Ignore);
                if (count < candidates.Length) break;
                System.Array.Resize(ref candidates, candidates.Length * 2);
            }
            for (int i = 0; i < count; i++)
            {
                Collider other = candidates[i];
                if (other == null || other.transform.IsChildOf(transform) || other is CharacterController ||
                    (Owner.ignoredPlayer != null && other.transform.IsChildOf(Owner.ignoredPlayer))) continue;
                var neighbour = other.GetComponentInParent<FrozenWaterStructure>();
                if (neighbour != null && (neighbour == this || chain.Contains(neighbour))) continue;
                if (neighbour == null && other.attachedRigidbody != null && !other.attachedRigidbody.isKinematic) continue;
                if (!SurfaceTouches(other, tolerance)) continue;
                if (neighbour == null || neighbour.TouchesEnvironment(chain)) return true;
            }
            return false;
        }
        bool SurfaceTouches(Collider other, float tolerance)
        {
            // Use the actual frozen mesh against primitives/convex colliders first.
            bool convex = other is BoxCollider || other is SphereCollider || other is CapsuleCollider ||
                (other is MeshCollider meshCollider && meshCollider.convex);
            if (convex && Physics.ComputePenetration(Surface.Collision, Surface.Collision.transform.position,
                Surface.Collision.transform.rotation, other, other.transform.position, other.transform.rotation, out _, out _)) return true;
            // Vertex probes also handle flush contact, terrain and non-convex ice neighbours.
            // A broad-phase AABB alone would incorrectly anchor curved bridges across empty space.
            foreach (Vector3 vertex in contactVertices)
            {
                int count;
                while (true)
                {
                    count = Physics.OverlapSphereNonAlloc(Surface.Collision.transform.TransformPoint(vertex), tolerance,
                        touching, Owner.environmentMask, QueryTriggerInteraction.Ignore);
                    if (count < touching.Length) break;
                    System.Array.Resize(ref touching, touching.Length * 2);
                }
                for (int i = 0; i < count; i++) if (touching[i] == other) return true;
            }
            return false;
        }
        public void DisableCollision() { if (Surface != null) Surface.Clear(); }
        public bool Melt() => Owner != null && Owner.Melt(this);
    }
}
