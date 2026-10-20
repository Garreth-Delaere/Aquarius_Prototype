using UnityEngine;
using UnityEngine.Rendering;

namespace Aquarius.Prototype
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class WaterMesh : MonoBehaviour
    {
        const int Sides = 12;
        Mesh mesh;
        Vector3[] vertices;
        int[] triangles;
        public void Rebuild(WaterBody body)
        {
            int count = body.Points.Length;
            if (mesh == null)
            {
                mesh = new Mesh { name = "Sculpted water (runtime)" }; mesh.MarkDynamic();
                GetComponent<MeshFilter>().sharedMesh = mesh;
                GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            if (vertices == null || vertices.Length != count * Sides + 2)
            {
                vertices = new Vector3[count * Sides + 2];
                triangles = new int[((count - 1) * Sides * 2 + Sides * 2) * 3];
                int t = 0;
                for (int i = 0; i < count - 1; i++)
                    for (int j = 0; j < Sides; j++)
                    {
                        int a = i * Sides + j, b = i * Sides + (j + 1) % Sides, c = a + Sides, d = b + Sides;
                        triangles[t++] = a; triangles[t++] = b; triangles[t++] = c;
                        triangles[t++] = b; triangles[t++] = d; triangles[t++] = c;
                    }
                for (int j = 0; j < Sides; j++)
                {
                    triangles[t++] = count * Sides; triangles[t++] = (j + 1) % Sides; triangles[t++] = j;
                    triangles[t++] = count * Sides + 1; triangles[t++] = (count - 1) * Sides + j; triangles[t++] = (count - 1) * Sides + (j + 1) % Sides;
                }
                mesh.Clear();
            }
            Vector3 previousRight = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                Vector3 tangent = (body.Points[Mathf.Min(i + 1, count - 1)] - body.Points[Mathf.Max(i - 1, 0)]).normalized;
                if (tangent.sqrMagnitude < 0.01f) tangent = transform.right;
                Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;
                if (right.sqrMagnitude < 0.01f) right = previousRight.sqrMagnitude > 0 ? Vector3.ProjectOnPlane(previousRight, tangent).normalized : Vector3.forward;
                if (i > 0 && Vector3.Dot(right, previousRight) < 0) right = -right;
                Vector3 up = Vector3.Cross(tangent, right).normalized;
                previousRight = right;
                for (int j = 0; j < Sides; j++)
                {
                    float angle = j * Mathf.PI * 2f / Sides;
                    Vector3 offset = right * (Mathf.Cos(angle) * body.Radius) + up * (Mathf.Sin(angle) * body.Radius * body.thicknessRatio);
                    vertices[i * Sides + j] = transform.InverseTransformPoint(body.Points[i] + offset);
                }
            }
            vertices[count * Sides] = transform.InverseTransformPoint(body.Points[0]);
            vertices[count * Sides + 1] = transform.InverseTransformPoint(body.Points[count - 1]);
            mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        }
        // Ray/triangle picking keeps liquid non-solid and avoids recooking a collider every frame.
        public bool TryPick(Ray ray, float range, out int index, out float distance)
        {
            index = -1; distance = range;
            if (vertices == null) return false;
            Vector3 origin = transform.InverseTransformPoint(ray.origin);
            Vector3 direction = transform.InverseTransformVector(ray.direction);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]], e1 = vertices[triangles[i + 1]] - a, e2 = vertices[triangles[i + 2]] - a;
                Vector3 p = Vector3.Cross(direction, e2);
                float determinant = Vector3.Dot(e1, p);
                if (Mathf.Abs(determinant) < 0.000001f) continue;
                Vector3 s = origin - a;
                float u = Vector3.Dot(s, p) / determinant;
                if (u < 0 || u > 1) continue;
                Vector3 q = Vector3.Cross(s, e1);
                float v = Vector3.Dot(direction, q) / determinant;
                if (v < 0 || u + v > 1) continue;
                float hit = Vector3.Dot(e2, q) / determinant;
                if (hit <= 0 || hit >= distance) continue;
                distance = hit;
                index = Mathf.Clamp(triangles[i + 1] / Sides, 0, (vertices.Length - 2) / Sides - 1);
            }
            return index >= 0;
        }
        void OnDestroy() { if (mesh != null) Destroy(mesh); }
    }
}

