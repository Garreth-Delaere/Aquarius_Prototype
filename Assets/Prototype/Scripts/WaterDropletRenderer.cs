using UnityEngine;
using UnityEngine.Rendering;

namespace Aquarius.Prototype
{
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class WaterDropletRenderer : MonoBehaviour
    {
        public Material material;
        [Range(0, 1)] public int meshDetail = 1;
        ParticleSystem particles;
        ParticleSystem.Particle[] buffer;
        Mesh dropletMesh;
        public void Initialize(int capacity)
        {
            if (particles != null) return;
            particles = GetComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = capacity; main.startSpeed = 0; main.gravityModifier = 0; main.simulationSpeed = 0;
            var emission = particles.emission; emission.enabled = false;
            var shape = particles.shape; shape.enabled = false;
            buffer = new ParticleSystem.Particle[capacity];
            var renderer = GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.sharedMaterial = material;
            dropletMesh = CreateDroplet(meshDetail); renderer.mesh = dropletMesh;
            particles.Play();
        }
        public void Show(WaterStream stream)
        {
            if (particles == null) Initialize(stream.dropletBudget);
            int count = stream.Droplets.Count;
            // Melting always returns the saved water, even above the source-emission budget.
            if (count > buffer.Length)
            {
                int capacity = Mathf.NextPowerOfTwo(count);
                buffer = new ParticleSystem.Particle[capacity];
                var main = particles.main; main.maxParticles = capacity;
            }
            for (int i = 0; i < count; i++)
            {
                var drop = stream.Droplets[i];
                float diameter = stream.dropletRadius * 2f;
                float speed = drop.velocity.magnitude;
                buffer[i] = new ParticleSystem.Particle
                {
                    position = drop.position, velocity = Vector3.zero,
                    startLifetime = 10000, remainingLifetime = 10000,
                    startColor = Color.white,
                    startSize3D = drop.grounded ? new Vector3(diameter * 1.8f, diameter * 0.35f, diameter * 1.8f) :
                        new Vector3(diameter, diameter * (1f + Mathf.Min(speed * 0.18f, 1.5f)), diameter),
                    rotation3D = drop.grounded || speed < 0.1f ? Vector3.zero : Quaternion.FromToRotation(Vector3.up, drop.velocity / speed).eulerAngles,
                    randomSeed = (uint)(i + 1)
                };
            }
            particles.SetParticles(buffer, count);
        }
        static Mesh CreateDroplet(int detail)
        {
            Vector3[] corners = { Vector3.up * 0.5f, Vector3.down * 0.5f, Vector3.right * 0.5f, Vector3.forward * 0.5f, Vector3.left * 0.5f, Vector3.back * 0.5f };
            int[] faces = { 0,3,2, 0,4,3, 0,5,4, 0,2,5, 1,2,3, 1,3,4, 1,4,5, 1,5,2 };
            var vertices = new System.Collections.Generic.List<Vector3>(96);
            for (int i = 0; i < faces.Length; i += 3)
            {
                Vector3 a = corners[faces[i]], b = corners[faces[i + 1]], c = corners[faces[i + 2]];
                if (detail <= 0) { vertices.Add(a); vertices.Add(b); vertices.Add(c); continue; }
                Vector3 ab = (a + b).normalized * 0.5f, bc = (b + c).normalized * 0.5f, ca = (c + a).normalized * 0.5f;
                vertices.AddRange(new[] { a, ab, ca, ab, b, bc, ca, bc, c, ab, bc, ca });
            }
            var indices = new int[vertices.Count]; var normals = new Vector3[vertices.Count];
            for (int i = 0; i < vertices.Count; i++) { indices[i] = i; normals[i] = vertices[i].normalized; }
            var mesh = new Mesh { name = detail > 0 ? "Rounded water droplet (32 triangles)" : "Water droplet (8 triangles)" };
            mesh.SetVertices(vertices); mesh.triangles = indices; mesh.normals = normals; mesh.RecalculateBounds(); return mesh;
        }        void OnDestroy()
        {
            if (dropletMesh == null) return;
            if (Application.isPlaying) Destroy(dropletMesh); else DestroyImmediate(dropletMesh);
        }
    }
}



