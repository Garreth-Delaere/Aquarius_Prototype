using UnityEngine;
using UnityEngine.InputSystem;

namespace Aquarius.Prototype
{
    [DefaultExecutionOrder(100)]
    public sealed class WaterManipulator : MonoBehaviour
    {
        public WaterBody water;
        public Camera view;
        [Min(0.5f)] public float grabReach = 5f;
        [Min(100f)] public float fastSwipePixelsPerSecond = 1500f;
        float grabDistance, swipe;
        Vector3 grabOffset;
        bool wasCaptured, hovering;
        readonly RaycastHit[] hits = new RaycastHit[24];
        void LateUpdate()
        {
            if (water == null || view == null || Mouse.current == null) return;
            bool captured = Cursor.lockState == CursorLockMode.Locked && Application.isFocused;
            if (!captured) { water.EndGrab(); wasCaptured = false; hovering = false; return; }
            if (!wasCaptured) { wasCaptured = true; return; }
            if (Keyboard.current != null && (Keyboard.current.rKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame))
            { water.ResetWater(); return; }
            Ray ray = new Ray(view.transform.position, view.transform.forward);
            if (!Mouse.current.leftButton.isPressed) water.EndGrab();
            if (!water.IsGrabbed)
            {
                hovering = water.GetComponent<WaterMesh>().TryPick(ray, grabReach, out int index, out float distance) && !Obstructed(ray, distance);
                if (hovering && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    grabDistance = distance;
                    grabOffset = water.Points[index] - ray.GetPoint(distance);
                    swipe = 0;
                    water.BeginGrab(index, water.Points[index]);
                }
            }
            if (water.IsGrabbed)
            {
                float speed = Mouse.current.delta.ReadValue().magnitude / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
                swipe = Mathf.Lerp(swipe, Mathf.Clamp01(speed / fastSwipePixelsPerSecond), 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
                water.SetTarget(ray.GetPoint(grabDistance) + grabOffset, swipe);
            }
        }
        bool Obstructed(Ray ray, float distance)
        {
            int count = Physics.RaycastNonAlloc(ray, hits, distance, water.environmentMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (!hits[i].transform.IsChildOf(transform) && !hits[i].transform.IsChildOf(water.transform)) return true;
            return false;
        }
        void OnDisable() { if (water != null) water.EndGrab(); wasCaptured = false; }
        void OnGUI()
        {
            if (water == null) return;
            string state = water.IsGrabbed ? "BENDING - move mouse and walk to sculpt" : hovering ? "Hold LMB to grab water" : "Aim at water within 5 m";
            GUI.Box(new Rect(12, 74, 540, 72), "LIQUID SCULPTING | " + state + "\nSlow drag = gentle pull | Fast swipe = stronger stretch\nBackspace resets water | F freezing comes in the next stage");
            if (water.NearStretchLimit) GUI.Label(new Rect(12, 150, 540, 24), "Water is near its stretch limit. Move closer or reshape another section.");
        }
    }
}
