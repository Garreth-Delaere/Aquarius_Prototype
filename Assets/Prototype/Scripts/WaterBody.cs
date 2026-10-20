using UnityEngine;

namespace Aquarius.Prototype
{
    // One connected ribbon of water. Shape memory keeps distant regions stable while
    // a local grab sculpts the chain; gravity produces bounded sag rather than a fluid sim.
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(WaterMesh))]
    public sealed class WaterBody : MonoBehaviour
    {
        [Range(8, 40)] public int pointCount = 21;
        [Min(0.5f)] public float initialLength = 2.4f;
        [Min(0.1f)] public float volume = 3f;
        [Min(1f)] public float maximumLength = 12f;
        [Range(0.2f, 1f)] public float thicknessRatio = 0.4f;
        [Min(0f)] public float elasticity = 12f;
        [Min(0f)] public float damping = 9f;
        [Min(0f)] public float sagGravity = 0.7f;
        [Min(1f)] public float gentlePull = 35f;
        [Min(1f)] public float fastPull = 100f;
        [Range(1f, 6f)] public float influenceRadius = 3f;
        public LayerMask environmentMask = ~0;
        public Transform ignoredPlayer;

        public Vector3[] Points { get; private set; }
        public float Radius { get; private set; }
        public float Length { get; private set; }
        public bool IsGrabbed => grabbed >= 0;
        public bool NearStretchLimit => Length >= Mathf.Max(initialLength, maximumLength) * 0.94f;
        Vector3[] velocities, memory;
        int grabbed = -1;
        Vector3 target;
        float swipe, accumulator;
        WaterMesh surface;
        readonly RaycastHit[] hits = new RaycastHit[32];

        void Awake() { ResetWater(); }
        public void ResetWater()
        {
            pointCount = Mathf.Clamp(pointCount, 8, 40);
            Points = new Vector3[pointCount]; velocities = new Vector3[pointCount]; memory = new Vector3[pointCount];
            for (int i = 0; i < pointCount; i++)
                memory[i] = Points[i] = transform.position + transform.right * ((i / (float)(pointCount - 1) - 0.5f) * initialLength);
            grabbed = -1; accumulator = 0;
            surface = GetComponent<WaterMesh>();
            Measure(); surface.Rebuild(this);
        }
        public void BeginGrab(int index, Vector3 position) { grabbed = Mathf.Clamp(index, 0, Points.Length - 1); target = position; swipe = 0; }
        public void SetTarget(Vector3 position, float speed) { target = position; swipe = Mathf.Clamp01(speed); }
        public void EndGrab() { grabbed = -1; }
        void OnDisable() { EndGrab(); }
        void LateUpdate()
        {
            if (Points == null) return;
            const float step = 1f / 90f;
            accumulator += Mathf.Min(Time.deltaTime, 0.05f);
            while (accumulator >= step) { Simulate(step); accumulator -= step; }
            Measure(); surface.Rebuild(this);
        }
        void Measure()
        {
            Length = 0;
            for (int i = 1; i < Points.Length; i++) Length += Vector3.Distance(Points[i - 1], Points[i]);
            // Elliptical section area * length. No added volume when stretching.
            Radius = Mathf.Sqrt(Mathf.Max(0.1f, volume) / (Mathf.PI * Mathf.Clamp(thicknessRatio, 0.2f, 1f) * Mathf.Max(Length, 0.5f)));
        }
        void Simulate(float dt)
        {
            Measure();
            for (int i = 0; i < Points.Length; i++)
            {
                float weight = grabbed < 0 ? 0 : Mathf.Exp(-Mathf.Pow((i - grabbed) / Mathf.Max(1f, influenceRadius), 2f) * 1.5f);
                Vector3 acceleration = (memory[i] - Points[i]) * elasticity + Vector3.down * sagGravity;
                if (i > 0 && i < Points.Length - 1)
                    acceleration += ((Points[i - 1] + Points[i + 1]) * 0.5f - Points[i]) * (elasticity * 0.5f);
                if (weight > 0.001f)
                    acceleration += Vector3.ClampMagnitude(target - Points[grabbed], 2f) * (Mathf.Lerp(gentlePull, fastPull, swipe) * weight);
                velocities[i] = Vector3.ClampMagnitude((velocities[i] + acceleration * dt) * Mathf.Exp(-damping * dt), 8f);
                Vector3 next = Points[i] + velocities[i] * dt;
                Points[i] = ResolveSupport(Points[i], next);
                if (weight > 0.001f) memory[i] = Vector3.Lerp(memory[i], Points[i], 1f - Mathf.Exp(-12f * weight * dt));
            }
            // Iterative distance constraints stop a fast swipe creating an arbitrarily long spike.
            float maximumSegment = Mathf.Max(initialLength, maximumLength) / (Points.Length - 1);
            float minimumSegment = initialLength / (Points.Length - 1) * 0.3f;
            for (int iteration = 0; iteration < 10; iteration++)
                for (int i = 1; i < Points.Length; i++)
                {
                    Vector3 delta = Points[i] - Points[i - 1];
                    float distance = delta.magnitude;
                    if (distance < 0.00001f) delta = transform.right * (distance = 0.0001f);
                    float desired = Mathf.Clamp(distance, minimumSegment, maximumSegment);
                    Vector3 correction = delta * ((distance - desired) / distance * 0.5f);
                    Points[i - 1] += correction; Points[i] -= correction;
                }
            // Recheck support after constraints. This is a centreline approximation, not full liquid collision.
            for (int i = 0; i < Points.Length; i++)
            {
                Points[i] = ResolveSupport(Points[i], Points[i]);
                if (Points[i].y < -12f) { ResetWater(); break; }
            }
        }
        Vector3 ResolveSupport(Vector3 previous, Vector3 next)
        {
            float clearance = Radius * Mathf.Clamp(thicknessRatio, 0.2f, 1f);
            Vector3 movement = next - previous;
            if (movement.sqrMagnitude > 0.000001f)
            {
                int count = Physics.SphereCastNonAlloc(previous, Mathf.Min(clearance, 0.18f), movement.normalized, hits,
                    movement.magnitude, environmentMask, QueryTriggerInteraction.Ignore);
                float nearest = movement.magnitude;
                for (int j = 0; j < count; j++)
                    if (Valid(hits[j].collider)) nearest = Mathf.Min(nearest, Mathf.Max(0, hits[j].distance - 0.01f));
                next = previous + movement.normalized * nearest;
            }
            int supports = Physics.RaycastNonAlloc(next + Vector3.up * (clearance + 0.1f), Vector3.down, hits,
                clearance * 2f + 0.2f, environmentMask, QueryTriggerInteraction.Ignore);
            float highest = float.NegativeInfinity;
            for (int j = 0; j < supports; j++)
                if (Valid(hits[j].collider) && hits[j].normal.y > 0.1f) highest = Mathf.Max(highest, hits[j].point.y + clearance);
            if (next.y < highest) next.y = highest;
            return next;
        }
        bool Valid(Collider collider) => collider != null && !collider.transform.IsChildOf(transform) &&
            (ignoredPlayer == null || !collider.transform.IsChildOf(ignoredPlayer));
    }
}

