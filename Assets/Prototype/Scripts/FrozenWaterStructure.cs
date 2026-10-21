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
        Vector3 restPosition;
        float hoverTime;
        public Vector3 Displacement => transform.position - restPosition;
        public bool IsSolid => Surface != null && Surface.IsSolid;

        public bool Build(WaterStream owner, List<WaterStream.Droplet> drops, WaterIceSurface template, out string reason)
        {
            Owner = owner; snapshot.AddRange(drops);
            Surface = gameObject.AddComponent<WaterIceSurface>(); Surface.CopySettingsFrom(template);
            if (!Surface.Build(snapshot, owner.dropletRadius, owner.ignoredPlayer, out reason)) return false;
            restPosition = transform.position; hoverTime = 0;
            return true;
        }
        void FixedUpdate() { SimulateHover(Time.fixedDeltaTime); }
        public void SimulateHover(float dt)
        {
            if (!IsSolid || dt <= 0) return;
            hoverTime += dt;
            float offset = Mathf.Sin(hoverTime * Mathf.Max(0, Surface.hoverFrequency) * Mathf.PI * 2f) * Surface.hoverAmplitude;
            // Move the original closed mesh and its collider together; never recook the mesh.
            transform.position = restPosition + Vector3.up * offset;
        }
        public void DisableCollision() { if (Surface != null) Surface.Clear(); }
        public bool Melt() => Owner != null && Owner.Melt(this);
    }
}
