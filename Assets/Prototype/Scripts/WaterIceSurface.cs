using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Aquarius.Prototype
{
    // Rebuilt only on freeze. A single static, non-convex collider matches the rendered
    // closed ribbons. Distant runs remain separate: no hull across unrelated puddles.
    public sealed class WaterIceSurface : MonoBehaviour
    {
        public Material iceMaterial;
        [Range(1, 8)] public int dropletsPerSection = 5;
        [Min(0.05f)] public float surfacePadding = 0.10f;
        [Min(0.05f)] public float thickness = 0.18f;
        [Min(0.1f)] public float maximumConnectionGap = 0.8f;
        [Range(0, 2)] public int smoothingPasses = 2;
        [Range(0.01f, 0.25f)] public float edgeBevel = 0.15f;
        [Header("Magic ice hover")]
        [Range(0f, 0.1f)] public float hoverAmplitude = 0.06f;
        [Min(0f)] public float hoverFrequency = 0.35f;
        [Tooltip("Surface contact tolerance in metres. Floors, walls and grounded ice connections stop hover.")]
        [Range(0.001f, 0.05f)] public float anchorContactTolerance = 0.02f;
        GameObject iceObject;
        Mesh mesh;
        MeshCollider collision;
        Material fallbackMaterial;
        public MeshCollider Collision => collision;
        public bool IsSolid => collision != null && collision.enabled && collision.sharedMesh != null;
        struct Section { public Vector3 centre; public float width; }

        public bool Build(WaterStream water, out string reason) => Build(water.Droplets, water.dropletRadius, water.ignoredPlayer, out reason);

        public void CopySettingsFrom(WaterIceSurface template)
        {
            hoverAmplitude = template.hoverAmplitude; hoverFrequency = template.hoverFrequency; anchorContactTolerance = template.anchorContactTolerance;
            iceMaterial = template.iceMaterial; dropletsPerSection = template.dropletsPerSection;
            surfacePadding = template.surfacePadding; thickness = template.thickness;
            maximumConnectionGap = template.maximumConnectionGap; smoothingPasses = template.smoothingPasses; edgeBevel = template.edgeBevel;
        }

        public bool Build(IReadOnlyList<WaterStream.Droplet> drops, float radius, Transform ignoredPlayer, out string reason)
        {
            Clear(); reason = null;
            if (drops.Count == 0) { reason = "Draw some water before freezing."; return false; }
            var groups = new Dictionary<int, List<Vector3>>();
            foreach (var drop in drops)
            {
                if (!groups.TryGetValue(drop.shapeId, out var group)) { group = new List<Vector3>(); groups.Add(drop.shapeId, group); }
                group.Add(drop.position);
            }
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            foreach (var group in groups.Values)
            {
                var run = new List<Section>();
                for (int start = 0; start < group.Count;)
                {
                    Vector3 centre = Vector3.zero;
                    int count = 0;
                    // Never average a distant droplet into a section: preserve disconnected water.
                    while (start + count < group.Count && count < Mathf.Max(1, dropletsPerSection) &&
                        (count == 0 || Vector3.Distance(group[start + count], group[start]) < maximumConnectionGap))
                    { centre += group[start + count]; count++; }
                    centre /= count;
                    float spread = 0;
                    for (int j = 0; j < count; j++) spread = Mathf.Max(spread, Vector3.ProjectOnPlane(group[start + j] - centre, Vector3.up).magnitude);
                    var section = new Section { centre = centre, width = Mathf.Clamp(spread + radius + surfacePadding, 0.14f, 0.5f) };
                    if (run.Count > 0)
                    {
                        float distance = Vector3.Distance(run[run.Count - 1].centre, centre);
                        if (distance > maximumConnectionGap) { AddRun(run, vertices, triangles); run.Clear(); }
                        else if (distance < 0.04f)
                        {
                            Section previous = run[run.Count - 1]; previous.width = Mathf.Max(previous.width, section.width);
                            run[run.Count - 1] = previous; start += count; continue;
                        }
                    }
                    run.Add(section); start += count;
                }
                AddRun(run, vertices, triangles);
            }
            if (iceObject == null)
            {
                iceObject = new GameObject("Frozen water - surface and collision");
                iceObject.transform.SetParent(transform, false);
                iceObject.AddComponent<MeshFilter>(); iceObject.AddComponent<MeshRenderer>();
                collision = iceObject.AddComponent<MeshCollider>();
            }
            mesh = new Mesh { name = "Frozen water surface", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            iceObject.GetComponent<MeshFilter>().sharedMesh = mesh;
            if (iceMaterial == null && fallbackMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader != null) fallbackMaterial = new Material(shader) { color = new Color(0.65f, 0.9f, 1f) };
            }
            var renderer = iceObject.GetComponent<MeshRenderer>(); renderer.sharedMaterial = iceMaterial != null ? iceMaterial : fallbackMaterial;
            renderer.enabled = true; collision.convex = false; collision.sharedMesh = mesh; collision.enabled = true;
            Physics.SyncTransforms();
            // Reject creation through the player's capsule rather than ejecting or trapping them.
            var player = ignoredPlayer != null ? ignoredPlayer.GetComponent<CharacterController>() : null;
            if (player != null && player.enabled)
            {
                Vector3 centre = player.transform.TransformPoint(player.center);
                float half = Mathf.Max(0, player.height * 0.5f - player.radius);
                Collider[] overlaps = Physics.OverlapCapsule(centre + player.transform.up * half, centre - player.transform.up * half,
                    player.radius + 0.015f, ~0, QueryTriggerInteraction.Ignore);
                foreach (var hit in overlaps) if (hit == collision)
                { reason = "Step clear of the water before freezing it."; Clear(); return false; }
            }
            return true;
        }
        void AddRun(List<Section> run, List<Vector3> vertices, List<int> triangles)
        {
            if (run.Count == 0) return;
            if (run.Count == 1)
            {
                Section a = run[0], b = a; a.centre -= Vector3.forward * a.width * 0.6f; b.centre += Vector3.forward * b.width * 0.6f;
                run = new List<Section> { a, b };
            }
            // Include the end droplets in the envelope rather than ending at their averaged centre.
            Section startSection = run[0], endSection = run[run.Count - 1];
            startSection.centre += (run[0].centre - run[1].centre).normalized * surfacePadding;
            endSection.centre += (run[run.Count - 1].centre - run[run.Count - 2].centre).normalized * surfacePadding;
            run[0] = startSection; run[run.Count - 1] = endSection;
            // Endpoint-preserving corner cutting smooths the centreline and width.
            // Separate runs remain separate; refinement happens only on freezing.
            for (int pass = 0; pass < Mathf.Clamp(smoothingPasses, 0, 2); pass++)
            {
                var refined = new List<Section>(run.Count * 2);
                refined.Add(run[0]);
                for (int i = 0; i < run.Count - 1; i++)
                {
                    refined.Add(Blend(run[i], run[i + 1], 0.25f));
                    refined.Add(Blend(run[i], run[i + 1], 0.75f));
                }
                refined.Add(run[run.Count - 1]); run = refined;
            }
            const int sides = 8;
            int first = vertices.Count;
            Vector3 previousSide = Vector3.right;
            for (int i = 0; i < run.Count; i++)
            {
                Vector3 tangent = run[Mathf.Min(i + 1, run.Count - 1)].centre - run[Mathf.Max(0, i - 1)].centre;
                Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                if (side.sqrMagnitude < 0.01f) side = previousSide;
                if (Vector3.Dot(side, previousSide) < 0 && i > 0) side = -side;
                previousSide = side;
                float width = run[i].width;
                float top = thickness * 0.3f, bottom = -thickness * 0.7f;
                float bevel = Mathf.Max(0.001f, Mathf.Min(width * edgeBevel, thickness * 0.25f));
                // Keep a flat walking surface, with small bevels along the sides.
                Vector2[] ring = {
                    new Vector2(-width + bevel, bottom), new Vector2(width - bevel, bottom),
                    new Vector2(width, bottom + bevel), new Vector2(width, top - bevel),
                    new Vector2(width - bevel, top), new Vector2(-width + bevel, top),
                    new Vector2(-width, top - bevel), new Vector2(-width, bottom + bevel)
                };
                foreach (var point in ring)
                    vertices.Add(transform.InverseTransformPoint(run[i].centre + side * point.x + Vector3.up * point.y));
                if (i == 0) continue;
                int a = first + (i - 1) * sides, b = a + sides;
                for (int j = 0; j < sides; j++)
                {
                    int k = (j + 1) % sides;
                    Triangle(triangles, a + j, a + k, b + j); Triangle(triangles, a + k, b + k, b + j);
                }
            }
            int end = vertices.Count - sides;
            for (int j = 1; j < sides - 1; j++)
            {
                Triangle(triangles, first, first + j + 1, first + j);
                Triangle(triangles, end, end + j, end + j + 1);
            }
        }
        static Section Blend(Section a, Section b, float t) => new Section
        { centre = Vector3.Lerp(a.centre, b.centre, t), width = Mathf.Lerp(a.width, b.width, t) };
        static void Triangle(List<int> triangles, int a, int b, int c) { triangles.Add(a); triangles.Add(b); triangles.Add(c); }
        public void Clear()
        {
            if (collision != null) { collision.enabled = false; collision.sharedMesh = null; }
            if (iceObject != null) { iceObject.GetComponent<MeshRenderer>().enabled = false; iceObject.GetComponent<MeshFilter>().sharedMesh = null; }
            Dispose(mesh); mesh = null;
        }
        static void Dispose(Object obj) { if (obj == null) return; if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }
        void OnDestroy() { Clear(); Dispose(fallbackMaterial); }
    }
}



