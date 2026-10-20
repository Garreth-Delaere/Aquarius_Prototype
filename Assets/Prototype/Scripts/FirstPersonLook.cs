using UnityEngine;
using UnityEngine.InputSystem;

namespace Aquarius.Prototype
{
    public sealed class FirstPersonLook : MonoBehaviour
    {
        public Transform cameraPivot;
        [Range(0.01f, 1f)] public float mouseSensitivity = 0.1f;
        [Range(30f, 89f)] public float pitchLimit = 80f;
        float pitch;
        bool skipDelta;

        void Start() { Capture(); }
        void Capture() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; skipDelta = true; }
        void Release() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        void OnDisable() { Release(); }
        void OnApplicationFocus(bool focus) { if (!focus) Release(); }
        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { Release(); return; }
            if (Mouse.current == null) return;
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame) Capture();
                return;
            }
            if (skipDelta) { skipDelta = false; return; }
            // Pointer delta is already accumulated per frame; do not multiply by deltaTime.
            Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(0f, delta.x, 0f);
            pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
        public void ResetPitch() { pitch = 0f; if (cameraPivot != null) cameraPivot.localRotation = Quaternion.identity; }
        void OnGUI()
        {
            GUI.Box(new Rect(12, 12, 510, 54), "Movement test: WASD move | Mouse look | Space jump\nR reset | Esc release cursor | Click to resume");
            GUI.Label(new Rect(Screen.width / 2f - 4, Screen.height / 2f - 10, 20, 20), "+");
        }
    }
}

