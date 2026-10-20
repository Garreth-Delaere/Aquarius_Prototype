using UnityEngine;

namespace Aquarius.Prototype
{
    // A replenishing rectangular water surface; independent of the finite drawn water.
    public sealed class WaterSource : MonoBehaviour
    {
        public Vector2 surfaceSize = new Vector2(1.8f, 22f);
        public bool TryPick(Ray ray, float reach, out float distance)
        {
            distance = 0;
            var plane = new Plane(transform.up, transform.position);
            if (!plane.Raycast(ray, out distance) || distance > reach) return false;
            Vector3 local = transform.InverseTransformPoint(ray.GetPoint(distance));
            return Mathf.Abs(local.x) <= surfaceSize.x * 0.5f && Mathf.Abs(local.z) <= surfaceSize.y * 0.5f;
        }
    }
}
