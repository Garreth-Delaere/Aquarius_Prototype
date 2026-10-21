using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aquarius.Prototype
{
    // Controlled droplets, not a fluid solver. Only held water follows a guide;
    // release removes all guide forces and leaves ordinary gravity/contact motion.
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(WaterDropletRenderer))]
    public sealed class WaterStream : MonoBehaviour
    {
        [Range(24, 300)] public int dropletsPerDraw = 160;
        [Range(256, 8192)] public int dropletBudget = 4096;
        [Min(1)] public float emissionRate = 100f;
        [Min(0.02f)] public float dropletRadius = 0.075f;
        [Min(1)] public float maximumStreamLength = 14f;
        [HideInInspector] public float tailFollowFraction = 0.2f; // Legacy scene data; superseded by timed catch-up.
        [Header("Gesture stretching")]
        [Tooltip("Tail travel along the curve in metres/second while guiding. Head motion must outrun this to stretch the stream.")]
        [Min(0f)] public float tailCatchUpSpeed = 3.5f;
        [Tooltip("Tail travel in metres/second once the guided head comes to rest.")]
        [Min(0f)] public float gatheringSpeed = 2.5f;
        [Tooltip("Short resting trail, in metres. Particle size stays unchanged.")]
        [Min(0.1f)] public float compactTrailLength = 0.25f;
        [Tooltip("Radius of the gathered droplet cloud, not individual particle size.")]
        [Min(0f)] public float restingRadius = 0.22f;
        [Min(0.1f)] public float pickupRadius = 1.8f;
        [Min(0)] public float gravity = 9.81f;
        [Min(1)] public float gentleResponse = 28f;
        [Min(1)] public float fastResponse = 65f;
        [Min(1)] public float heldDrag = 7f;
        [Header("Held water motion")]
        [Min(0.1f)] public float followResponsiveness = 4.5f;
        [Min(0.1f)] public float maximumFollowSpeed = 12f;
        [Min(0f)] public float wobbleAmplitude = 0.22f;
        [Min(0f)] public float wobbleSpeed = 0.6f;
        [Range(0.25f, 2f)] public float wobbleCycles = 1.1f;
        [Tooltip("Fast-gesture head tracking boost. 1 restores the earlier follow response; the tail keeps its original resistance.")]
        [Range(1f, 4f)] public float fastFollowBoost = 3f;
        public Vector3 SmoothedTarget => guidedTarget;
        public LayerMask environmentMask = ~0;
        public Transform ignoredPlayer;

        public struct Droplet
        {
            public Vector3 position, velocity;
            public bool grounded;
            public int shapeId;
        }
        readonly List<Droplet> droplets = new List<Droplet>();
        readonly List<int> selected = new List<int>();
        readonly List<Vector3> guide = new List<Vector3>();
        readonly List<float> distances = new List<float>();
        readonly RaycastHit[] hits = new RaycastHit[32];
        readonly HashSet<int> selectedSet = new HashSet<int>();
        Vector3 sourcePosition, target, guidedTarget, followVelocity;
        float emission, swipe, elapsed, accumulator, guideLength;
        int emitted, nextShapeId, currentShapeId;
        bool drawing;
        WaterDropletRenderer display;
        WaterIceSurface ice;
        readonly List<FrozenWaterStructure> frozen = new List<FrozenWaterStructure>();
        public IReadOnlyList<FrozenWaterStructure> FrozenStructures => frozen;
        public int FrozenCount => frozen.Count;
        public string FreezeMessage { get; private set; }
        public IReadOnlyList<Droplet> Droplets => droplets;
        public bool IsHeld { get; private set; }
        public int SelectedCount => selected.Count;
        public bool BudgetFull => droplets.Count >= dropletBudget;
        public bool AtLengthLimit { get; private set; }

        void Awake() { Initialize(); }
        public void Initialize()
        {
            display = GetComponent<WaterDropletRenderer>();
            display.Initialize(dropletBudget);
            ice = GetComponent<WaterIceSurface>();
            if (ice == null) ice = gameObject.AddComponent<WaterIceSurface>();
        }
        public void BeginSource(Vector3 position)
        {
            Release();
            if (BudgetFull) return;
            FreezeMessage = null;
            currentShapeId = nextShapeId++;
            IsHeld = drawing = true; sourcePosition = target = position + Vector3.up * (dropletRadius + 0.03f);
            guidedTarget = target; followVelocity = Vector3.zero;
            guide.Add(sourcePosition); guide.Add(sourcePosition); emitted = 0; emission = 1;
        }
        public bool BeginPickup(int index)
        {
            Release();
            if (index < 0 || index >= droplets.Count) return false;
            FreezeMessage = null;
            currentShapeId = nextShapeId++;
            target = droplets[index].position;
            for (int i = 0; i < droplets.Count; i++)
                if ((droplets[i].position - target).sqrMagnitude <= pickupRadius * pickupRadius)
                { selected.Add(i); selectedSet.Add(i); var drop = droplets[i]; drop.shapeId = currentShapeId; droplets[i] = drop; }
            // Start exactly where the selected water is; no source-position reset.
            sourcePosition = guidedTarget = target; followVelocity = Vector3.zero;
            guide.Add(target); guide.Add(target);
            IsHeld = selected.Count > 0; return IsHeld;
        }
        public void SetTarget(Vector3 position, float speed) { target = position; swipe = Mathf.Clamp01(speed); }
        public void Release()
        {
            IsHeld = drawing = false; selected.Clear(); selectedSet.Clear(); guide.Clear(); distances.Clear();
            emission = 0; swipe = 0; followVelocity = Vector3.zero; AtLengthLimit = false;
        }
        public void ResetWater()
        {
            Release(); ClearStructures(); droplets.Clear(); nextShapeId = 0; accumulator = 0; FreezeMessage = null;
            if (display != null) display.Show(this);
        }
        public bool FreezeHeld()
        {
            if (!IsHeld || selected.Count == 0)
            { FreezeMessage = "Hold water to freeze it, or aim at ice and press F to melt that structure."; return false; }
            if (ice == null) Initialize();
            var snapshot = new List<Droplet>(selected.Count);
            foreach (int index in selected) snapshot.Add(droplets[index]);
            var obj = new GameObject("Ice structure " + (currentShapeId + 1));
            // Siblings, not children of liquid: liquid collision must see earlier ice.
            SceneManager.MoveGameObjectToScene(obj, gameObject.scene);
            var structure = obj.AddComponent<FrozenWaterStructure>();
            if (!structure.Build(this, snapshot, ice, out string reason))
            { FreezeMessage = reason; Dispose(obj); return false; }
            frozen.Add(structure);
            // Selection indices are ascending; remove backwards to preserve other liquid.
            for (int i = selected.Count - 1; i >= 0; i--) droplets.RemoveAt(selected[i]);
            Release(); FreezeMessage = null;
            if (display != null) display.Show(this);
            return true;
        }
        public bool Melt(FrozenWaterStructure structure)
        {
            if (structure == null || structure.Owner != this || !frozen.Remove(structure)) return false;
            structure.DisableCollision(); // Remove support immediately, before the next liquid step.
            int restoredShape = nextShapeId++;
            foreach (var saved in structure.Snapshot)
            {
                var drop = saved; drop.position += structure.Displacement; drop.velocity = Vector3.zero; drop.grounded = false; drop.shapeId = restoredShape;
                droplets.Add(drop);
            }
            // Puddles that were sleeping on this structure must fall too.
            for (int i = 0; i < droplets.Count; i++)
            { var drop = droplets[i]; drop.grounded = false; droplets[i] = drop; }
            FreezeMessage = null; Dispose(structure.gameObject);
            if (display != null) display.Show(this);
            return true;
        }
        void ClearStructures()
        {
            foreach (var structure in frozen)
                if (structure != null) { structure.DisableCollision(); Dispose(structure.gameObject); }
            frozen.Clear();
        }
        static void Dispose(Object obj) { if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }
        void OnDisable() { Release(); }
        void OnDestroy() { ClearStructures(); }
        void LateUpdate()
        {
            const float step = 1f / 90f;
            accumulator += Mathf.Min(Time.deltaTime, 0.06f);
            while (accumulator >= step) { SimulateStep(step); accumulator -= step; }
            if (display != null) display.Show(this);
        }
        // Also used by editor validation to exercise the actual simulation deterministically.
        public void SimulateStep(float dt)
        {
            elapsed += dt;
            if (IsHeld)
            {
                // Smooth the actual sculpting guide, then let each droplet catch up through its spring/drag.
                // Fast swipes remain stronger, but never bypass the physical following step.
                float response = Mathf.Max(0.1f, followResponsiveness) * Mathf.Lerp(1f, 1.35f * Mathf.Max(1f, fastFollowBoost), swipe);
                guidedTarget = Vector3.SmoothDamp(guidedTarget, target, ref followVelocity, 1f / response,
                    Mathf.Max(0.1f, maximumFollowSpeed) * Mathf.Lerp(1f, Mathf.Max(1f, fastFollowBoost), swipe), dt);
                UpdateGuide(dt);
                sourcePosition = guide[0]; // New droplets join the moving tail, never a permanent source tether.
                if (drawing)
                {
                    emission += dt * emissionRate;
                    while (emission >= 1 && emitted < dropletsPerDraw && !BudgetFull)
                    {
                        emission -= 1;
                        float angle = emitted * 2.399963f;
                        Vector3 spread = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (dropletRadius * 1.5f);
                        droplets.Add(new Droplet { position = sourcePosition + spread, shapeId = currentShapeId });
                        selected.Add(droplets.Count - 1); selectedSet.Add(droplets.Count - 1); emitted++;
                    }
                    emission = Mathf.Min(emission, 1f);
                }
            }
            int slot = 0;
            for (int i = 0; i < droplets.Count; i++)
            {
                Droplet drop = droplets[i];
                bool held = IsHeld && selectedSet.Contains(i);
                if (!held && drop.grounded && drop.velocity.sqrMagnitude < 0.0009f) continue; // Static greybox: let settled puddles sleep.
                Vector3 acceleration = Vector3.down * gravity;
                float headFollow = 0;
                if (held)
                {
                    // Distributed moving lanes retain a readable stream while particles flow within it.
                    int count = Mathf.Max(1, selected.Count);
                    float phase = count == 1 ? 1f : 1f - slot / (float)(count - 1);
                    headFollow = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.65f, 1f, phase)) * swipe;
                    float ripple = Mathf.Sin(elapsed * (3f + swipe * 4f) - phase * 15f) * 0.025f;
                    Vector3 along;
                    Vector3 desired = SampleGuide(Mathf.Clamp01(phase + ripple), out along);
                    Vector3 side = Vector3.Cross(Vector3.up, along).normalized;
                    if (side.sqrMagnitude < 0.01f) side = Vector3.right;
                    float lane = (slot % 5 - 2) * dropletRadius * 1.1f;
                    desired += side * lane + Vector3.up * (Mathf.Sin(slot * 2.4f + elapsed * 4f) * dropletRadius * 0.55f);
                    float roundness = restingRadius <= 0 ? 0 : 1f - Mathf.SmoothStep(0, 1,
                        Mathf.InverseLerp(compactTrailLength, Mathf.Max(compactTrailLength + 0.01f, restingRadius * 4f), guideLength));
                    if (roundness > 0)
                    {
                        Vector3 centre = SampleGuide(0.5f, out Vector3 axis);
                        if (axis.sqrMagnitude < 0.01f) axis = Vector3.forward;
                        Vector3 across = Vector3.Cross(Vector3.up, axis).normalized;
                        if (across.sqrMagnitude < 0.01f) across = Vector3.right;
                        Vector3 normal = Vector3.Cross(axis, across).normalized;
                        float u = phase * 2f - 1f;
                        float ring = Mathf.Sqrt(Mathf.Max(0, 1f - u * u)) * restingRadius;
                        float angle = slot * 2.399963f + elapsed * 0.5f;
                        Vector3 sphere = centre + axis * (u * restingRadius) +
                            (across * Mathf.Cos(angle) + normal * Mathf.Sin(angle)) * ring;
                        desired = Vector3.Lerp(desired, sphere, roundness);
                    }
                    // A coherent travelling wave moves the centreline, not just individual lanes.
                    // Its wavelength spans many freeze sections, so averaging does not erase it.
                    float wavePhase = Mathf.Lerp(phase, 0.5f, roundness);
                    float wave = elapsed * wobbleSpeed * Mathf.PI * 2f - wavePhase * wobbleCycles * Mathf.PI * 2f;
                    float envelope = 0.15f + 0.85f * Mathf.Sin(Mathf.PI * phase);
                    float amplitude = Mathf.Max(0, wobbleAmplitude) * Mathf.Lerp(envelope, 0.5f, roundness);
                    desired += side * (Mathf.Sin(wave) * amplitude) + Vector3.up * (Mathf.Cos(wave * 0.83f + wavePhase * 1.7f) * amplitude * 0.5f);
                    // Counter gravity only while held. No tether survives release.
                    // Let the leading drops inherit some guide velocity during fast gestures.
                    // The tail retains its original lag, so improved aiming does not erase the snake.
                    Vector3 leadingVelocity = followVelocity * headFollow * (1f - 1f / Mathf.Max(1f, fastFollowBoost));
                    acceleration = (desired - drop.position) * Mathf.Lerp(gentleResponse, fastResponse, swipe) - (drop.velocity - leadingVelocity) * heldDrag;
                    slot++;
                }
                drop.velocity = Vector3.ClampMagnitude(drop.velocity + acceleration * dt, held ? 12f * Mathf.Lerp(1f, Mathf.Max(1f, fastFollowBoost), headFollow) : 22f);
                if (!held) drop.velocity *= Mathf.Exp(-0.15f * dt);
                MoveWithCollision(ref drop, dt, held);
                droplets[i] = drop;
            }
        }
        void UpdateGuide(float dt)
        {
            int last = guide.Count - 1;
            float headSpeed = Vector3.Distance(guidedTarget, guide[last]) / Mathf.Max(dt, 0.00001f);
            // The head records its actual smoothed trajectory. The tail travels along that
            // same curve every second, so only sufficiently fast motion can leave a long wake.
            guide[last] = guidedTarget;
            if (Vector3.Distance(guide[last - 1], guide[last]) > 0.4f && guide.Count < 100)
                guide.Add(guidedTarget);

            guideLength = 0;
            for (int i = 1; i < guide.Count; i++) guideLength += Vector3.Distance(guide[i - 1], guide[i]);
            float maximum = Mathf.Max(1f, maximumStreamLength);
            float restingLength = Mathf.Clamp(compactTrailLength, 0.1f, maximum);
            float catchUp = Mathf.Lerp(Mathf.Max(0, gatheringSpeed), Mathf.Max(0, tailCatchUpSpeed),
                Mathf.InverseLerp(0.1f, 0.75f, headSpeed));
            float excess = Mathf.Max(Mathf.Max(0, guideLength - maximum),
                Mathf.Min(catchUp * dt, Mathf.Max(0, guideLength - restingLength)));
            AtLengthLimit = guideLength > maximum;
            // Remove distance from the oldest end, including while stationary. Curves retract
            // along their history rather than being straightened or teleported toward the head.
            while (excess > 0.0001f && guide.Count >= 2)
            {
                float length = Vector3.Distance(guide[0], guide[1]);
                if (guide.Count > 2 && length <= excess)
                { guide.RemoveAt(0); excess -= length; continue; }
                if (length > 0.00001f) guide[0] = Vector3.MoveTowards(guide[0], guide[1], excess);
                break;
            }
            distances.Clear(); distances.Add(0); guideLength = 0;
            for (int i = 1; i < guide.Count; i++)
            { guideLength += Vector3.Distance(guide[i - 1], guide[i]); distances.Add(guideLength); }
        }
        Vector3 SampleGuide(float t, out Vector3 tangent)
        {
            tangent = Vector3.forward;
            float distance = t * guideLength;
            for (int i = 1; i < guide.Count; i++)
            {
                float length = distances[i] - distances[i - 1];
                if (distance > distances[i] && i < guide.Count - 1) continue;
                tangent = (guide[i] - guide[i - 1]).normalized;
                return Vector3.Lerp(guide[i - 1], guide[i], length > 0.0001f ? (distance - distances[i - 1]) / length : 0);
            }
            return sourcePosition;
        }
        void MoveWithCollision(ref Droplet drop, float dt, bool held)
        {
            drop.grounded = false;
            float radius = dropletRadius * 0.6f;
            Vector3 delta = drop.velocity * dt;
            // Consume tangential motion after a hit rather than pinning water to ice.
            // A few sweeps handle a corner without adding a full rigid-body simulation.
            for (int sweep = 0; sweep < 3 && delta.sqrMagnitude > 0.000001f; sweep++)
            {
                float length = delta.magnitude;
                int count = Physics.SphereCastNonAlloc(drop.position, radius, delta / length, hits, length + 0.003f, environmentMask, QueryTriggerInteraction.Ignore);
                float nearest = length + 0.003f; int hit = -1;
                for (int j = 0; j < count; j++)
                    if (Valid(hits[j].collider) && hits[j].distance < nearest) { nearest = hits[j].distance; hit = j; }
                if (hit < 0) { drop.position += delta; break; }
                Vector3 normal = hits[hit].normal;
                Vector3 travelled = delta / length * Mathf.Clamp(nearest - 0.003f, 0, length);
                drop.position += travelled;
                delta = Vector3.ProjectOnPlane(delta - travelled, normal);
                drop.velocity = Vector3.ProjectOnPlane(drop.velocity, normal);
                drop.grounded |= normal.y > 0.45f;
            }
            // A short support probe keeps settled drops stable and lets them fall off edges.
            if (drop.velocity.y <= 0.1f)
            {
                int count = Physics.RaycastNonAlloc(drop.position + Vector3.up * 0.06f, Vector3.down, hits, radius + 0.075f, environmentMask, QueryTriggerInteraction.Ignore);
                float top = float.NegativeInfinity;
                for (int j = 0; j < count; j++)
                    if (Valid(hits[j].collider) && hits[j].normal.y > 0.45f) top = Mathf.Max(top, hits[j].point.y);
                if (drop.position.y <= top + radius + 0.015f)
                {
                    drop.position.y = top + radius + 0.002f; drop.grounded = true;
                    drop.velocity.y = Mathf.Max(0, drop.velocity.y);
                }
            }
            if (drop.grounded && !held) drop.velocity *= Mathf.Exp(-12f * dt);
        }
        bool Valid(Collider collider) => collider != null && !collider.transform.IsChildOf(transform) &&
            (ignoredPlayer == null || !collider.transform.IsChildOf(ignoredPlayer));
        public bool TryPick(Ray ray, float reach, out int index, out float distance)
        {
            index = -1; distance = reach;
            for (int i = 0; i < droplets.Count; i++)
            {
                Vector3 delta = droplets[i].position - ray.origin;
                float projected = Vector3.Dot(delta, ray.direction);
                float radius = dropletRadius * (droplets[i].grounded ? 2.2f : 1.6f);
                float perpendicular = delta.sqrMagnitude - projected * projected;
                if (perpendicular > radius * radius) continue;
                float hit = projected - Mathf.Sqrt(Mathf.Max(0, radius * radius - perpendicular));
                if (hit > 0 && hit < distance) { distance = hit; index = i; }
            }
            return index >= 0;
        }
    }
}
