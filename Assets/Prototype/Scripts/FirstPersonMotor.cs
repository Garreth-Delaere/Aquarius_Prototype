using UnityEngine;
using UnityEngine.InputSystem;

namespace Aquarius.Prototype
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonMotor : MonoBehaviour
    {
        public InputActionAsset inputActions;
        [Min(0.1f)] public float walkingSpeed = 3f;
        [Min(0f)] public float jumpHeight = 1f;
        [Min(0.1f)] public float gravity = 18f;
        [Range(0f, 80f)] public float slopeLimit = 55f;
        [Min(0f)] public float stepHeight = 0.2f;
        [Min(0f)] public float airAcceleration = 18f;
        [Min(0f)] public float slideSpeed = 3f;
        public float resetBelowY = -8f;

        CharacterController controller;
        InputActionAsset actions;
        InputAction move, jump;
        Vector3 horizontalVelocity, spawnPosition;
        Quaternion spawnRotation;
        float verticalVelocity;
        readonly RaycastHit[] groundHits = new RaycastHit[12];

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            if (inputActions == null) { Debug.LogError("Assign the input actions asset.", this); enabled = false; return; }
            actions = Instantiate(inputActions);
            move = actions.FindAction("Player/Move", true);
            jump = actions.FindAction("Player/Jump", true);
        }
        void OnEnable() { move?.Enable(); jump?.Enable(); }
        void OnDisable() { move?.Disable(); jump?.Disable(); }
        void OnDestroy() { if (actions != null) Destroy(actions); }

        void Update()
        {
            bool captured = Cursor.lockState == CursorLockMode.Locked;
            Vector2 input = captured ? move.ReadValue<Vector2>() : Vector2.zero;
            SimulateMovement(input, captured && jump.WasPressedThisFrame(), Time.deltaTime);
            if (transform.position.y < resetBelowY || (captured && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)) ResetPlayer();
        }

        // Separate input sampling from movement so traversal can also be checked in the editor.
        public void SimulateMovement(Vector2 input, bool jumpPressed, float dt)
        {
            if (dt <= 0) return;
            if (controller == null) controller = GetComponent<CharacterController>();
            controller.slopeLimit = slopeLimit;
            controller.stepOffset = stepHeight;
            input = Vector2.ClampMagnitude(input, 1f);
            Vector3 desired = (transform.right * input.x + transform.forward * input.y) * walkingSpeed;
            bool supported = FindGround(out Vector3 normal) && verticalVelocity <= 0f;
            bool steep = supported && Vector3.Angle(normal, Vector3.up) > slopeLimit + 0.5f;
            bool grounded = controller.isGrounded || supported;
            if (grounded && verticalVelocity <= 0f)
            {
                horizontalVelocity = desired;
                verticalVelocity = -2f;
                if (steep)
                {
                    Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, normal).normalized;
                    Vector3 flatDownhill = new Vector3(downhill.x, 0f, downhill.z).normalized;
                    float uphill = Vector3.Dot(horizontalVelocity, flatDownhill);
                    if (uphill < 0f) horizontalVelocity -= flatDownhill * uphill;
                    horizontalVelocity += flatDownhill * slideSpeed;
                    verticalVelocity += downhill.y * slideSpeed;
                }
                // Supported steep slopes can also be jumped from. Suppress the sliding
                // impulse on takeoff so steering can pull the player away from the snag.
                if (jumpPressed)
                {
                    verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
                    horizontalVelocity = desired;
                }
            }
            else horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desired, airAcceleration * dt);
            verticalVelocity = Mathf.Max(verticalVelocity - gravity * dt, -35f);
            CollisionFlags flags = controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
        }
        bool FindGround(out Vector3 normal)
        {
            // Probe only just below the capsule; ignore the player's own collider.
            Vector3 bottom = transform.TransformPoint(controller.center) - Vector3.up * (controller.height * 0.5f - controller.radius);
            int count = Physics.SphereCastNonAlloc(bottom + Vector3.up * 0.05f, controller.radius * 0.85f,
                Vector3.down, groundHits, 0.05f + controller.radius * 0.15f + 0.08f, ~0, QueryTriggerInteraction.Ignore);
            normal = Vector3.up;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (groundHits[i].collider == controller || groundHits[i].normal.y < 0.17f || groundHits[i].distance >= nearest) continue;
                nearest = groundHits[i].distance;
                normal = groundHits[i].normal;
            }
            return !float.IsPositiveInfinity(nearest);
        }

        public void ResetPlayer()
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            controller.enabled = true;
            GetComponent<FirstPersonLook>()?.ResetPitch();
        }
    }
}



