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
        public bool IsSolid => Surface != null && Surface.IsSolid;

        public bool Build(WaterStream owner, List<WaterStream.Droplet> drops, WaterIceSurface template, out string reason)
        {
            Owner = owner; snapshot.AddRange(drops);
            Surface = gameObject.AddComponent<WaterIceSurface>(); Surface.CopySettingsFrom(template);
            return Surface.Build(snapshot, owner.dropletRadius, owner.ignoredPlayer, out reason);
        }
        public void DisableCollision() { if (Surface != null) Surface.Clear(); }
        public bool Melt() => Owner != null && Owner.Melt(this);
    }
}
