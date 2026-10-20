using UnityEngine;
using UnityEngine.InputSystem;

namespace Aquarius.Prototype
{
    [DefaultExecutionOrder(100)]
    public sealed class StreamManipulator : MonoBehaviour
    {
        public WaterSource source;
        public WaterSource[] additionalSources = new WaterSource[0];
        public WaterStream water;
        public Camera view;
        [Min(1)] public float reach = 6f;
        [Min(100)] public float fastSwipePixelsPerSecond = 1500f;
        [Header("Sculpting depth")]
        [Min(0.01f)] public float scrollSensitivity = 0.6f;
        [Min(0.25f)] public float minimumManipulationDistance = 1f;
        [Min(0.25f)] public float maximumManipulationDistance = 10f;
        public float CurrentHoldDistance => holdDistance;
        public Vector3 ManipulationTarget => view.transform.position + view.transform.forward * holdDistance;
        float holdDistance, swipe;
        bool wasCaptured;
        string hint = "Aim at a water source or fallen water";
        RaycastHit[] hits = new RaycastHit[32];
        void LateUpdate()
        {
            if (water == null || view == null || Mouse.current == null) return;
            bool captured = Application.isFocused && Cursor.lockState == CursorLockMode.Locked;
            if (!captured) { water.Release(); wasCaptured = false; return; }
            if (!wasCaptured) { wasCaptured = true; return; }
            if (Keyboard.current != null && Keyboard.current.backspaceKey.wasPressedThisFrame) { water.ResetWater(); return; }
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) { water.Release(); return; }
            Ray ray = new Ray(view.transform.position, view.transform.forward);
            FrozenWaterStructure aimedIce = FindIce(ray);
            if (!Mouse.current.leftButton.isPressed) water.Release();
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                if (water.IsHeld) water.FreezeHeld();
                else if (aimedIce != null) water.Melt(aimedIce);
                else water.FreezeHeld(); // Explain the required target; never melt an arbitrary structure.
                return;
            }
            if (!water.IsHeld)
            {
                bool foundWater = water.TryPick(ray, reach, out int index, out float waterDistance) && !Obstructed(ray, waterDistance);
                float sourceDistance = reach;
                bool foundSource = TryFindSource(ray, out _, out sourceDistance);
                bool pickup = foundWater && (!foundSource || waterDistance <= sourceDistance);
                hint = pickup ? "Hold LMB to bend fallen water" : aimedIce != null ? "F melts the aimed ice structure" : foundSource ? "Hold LMB to draw from the water source" : "Aim at water to bend, or ice to melt";
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    swipe = 0;
                    if (pickup) { SetHoldDistance(waterDistance); water.BeginPickup(index); }
                    else if (foundSource) { SetHoldDistance(sourceDistance); water.BeginSource(ray.GetPoint(sourceDistance)); }
                }
            }
            if (water.IsHeld)
            {
                float speed = Mouse.current.delta.ReadValue().magnitude / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
                swipe = Mathf.Lerp(swipe, Mathf.Clamp01(speed / fastSwipePixelsPerSecond), 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
                float wheel = Mouse.current.scroll.ReadValue().y;
                // Input System 1.20 defaults to normalized wheel steps. Honour legacy Windows settings too.
                if (InputSystem.settings.scrollDeltaBehavior == InputSettings.ScrollDeltaBehavior.KeepPlatformSpecificInputRange &&
                    (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)) wheel /= 120f;
                ApplyScrollDepth(wheel);
                water.SetTarget(ManipulationTarget, swipe);
                hint = "BENDING - F commits this mass; earlier ice stays solid";
            }
        }
        public void SetHoldDistance(float distance)
        {
            float minimum = Mathf.Max(0.25f, minimumManipulationDistance);
            holdDistance = Mathf.Clamp(distance, minimum, Mathf.Max(minimum, maximumManipulationDistance));
        }
        public void ApplyScrollDepth(float wheelSteps)
        {
            if (water != null && water.IsHeld) SetHoldDistance(holdDistance + wheelSteps * scrollSensitivity);
        }
        public bool TryFindSource(Ray ray, out WaterSource found, out float distance)
        {
            found = null; distance = reach;
            if (source != null && source.isActiveAndEnabled && source.TryPick(ray, reach, out float first) && first < distance && !Obstructed(ray, first))
            { found = source; distance = first; }
            if (additionalSources != null)
                foreach (var candidate in additionalSources)
                    if (candidate != null && candidate.isActiveAndEnabled && candidate.TryPick(ray, reach, out float hit) && hit < distance && !Obstructed(ray, hit))
                    { found = candidate; distance = hit; }
            return found != null;
        }
        FrozenWaterStructure FindIce(Ray ray)
        {
            int count = Cast(ray, reach);
            float nearest = reach; FrozenWaterStructure target = null;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].transform.IsChildOf(transform) || hits[i].distance >= nearest) continue;
                nearest = hits[i].distance;
                var structure = hits[i].collider.GetComponentInParent<FrozenWaterStructure>();
                target = structure != null && structure.Owner == water ? structure : null;
            }
            return target;
        }
        int Cast(Ray ray, float distance)
        {
            while (true)
            {
                int count = Physics.RaycastNonAlloc(ray, hits, distance, water.environmentMask, QueryTriggerInteraction.Ignore);
                if (count < hits.Length) return count;
                System.Array.Resize(ref hits, hits.Length * 2);
            }
        }
        bool Obstructed(Ray ray, float distance)
        {
            int count = Cast(ray, distance);
            for (int i = 0; i < count; i++) if (!hits[i].transform.IsChildOf(transform)) return true;
            return false;
        }
        void OnDisable() { if (water != null) water.Release(); }
        void OnGUI()
        {
            GUI.Box(new Rect(12, 74, 660, 75), "WATER + ICE | " + hint + "\nF while bending: freeze held mass | Aim at ice + F: melt only that structure\nEarlier ice persists | Backspace clears all water/ice | R resets player");
            if (water == null) return;
            GUI.Label(new Rect(12, 150, 660, 24), "Ice structures: " + water.FrozenCount + (water.IsHeld ? " | Depth: " + holdDistance.ToString("F1") + " m | Wheel pushes/pulls" : ""));
            if (water.AtLengthLimit) GUI.Label(new Rect(12, 174, 660, 24), "Full stream length: the tail follows your guidance.");
            if (water.BudgetFull) GUI.Label(new Rect(12, 198, 660, 24), "Liquid budget full. Freeze or reuse water before drawing more.");
            if (!string.IsNullOrEmpty(water.FreezeMessage)) GUI.Label(new Rect(12, 222, 660, 24), water.FreezeMessage);
        }
    }
}




