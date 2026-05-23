using UnityEngine;
using PhotalFrame.Input;

namespace PhotalFrame.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputReader inputReader;

        [Header("Movement Speeds")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float runSpeed = 5.5f;
        [SerializeField] private float viewfinderSpeed = 1f;
        [SerializeField] private float rotationSpeed = 135f;

        [Header("Stamina System")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaConsumeRate = 20f;
        [SerializeField] private float staminaRegenRate = 12f;
        [SerializeField] private float staminaRecoveryThreshold = 20f; // Minimum stamina to run again

        private Rigidbody rb;
        private CapsuleCollider capsuleCollider;
        
        private float currentStamina;
        private bool isExhausted;
        private bool isViewfinderMode;

        // Public properties for external systems (like UI and Camera)
        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;
        public bool IsExhausted => isExhausted;
        public bool IsViewfinderMode => isViewfinderMode;
        public bool IsRunning { get; private set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            capsuleCollider = GetComponent<CapsuleCollider>();

            // Configure Rigidbody for character movement
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            // Freeze rotation on X and Z axes to prevent the capsule from tipping over
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            currentStamina = maxStamina;
        }

        private void Start()
        {
            if (inputReader == null)
            {
                inputReader = FindAnyObjectByType<InputReader>();
                if (inputReader == null)
                {
                    Debug.LogError("PlayerController: InputReader not found in the scene.");
                }
            }
        }

        private void Update()
        {
            if (inputReader == null) return;

            // Toggle Viewfinder mode when button is pressed
            if (inputReader.ToggleViewfinder)
            {
                SetViewfinderMode(!isViewfinderMode);
            }

            HandleStamina();
        }

        private void FixedUpdate()
        {
            if (inputReader == null) return;

            if (isViewfinderMode)
            {
                MoveInViewfinder();
            }
            else
            {
                MoveInExploration();
            }
        }

        /// <summary>
        /// Movement logic in standard exploration mode (Tank Controls).
        /// </summary>
        private void MoveInExploration()
        {
            Vector2 moveInput = inputReader.Move;

            // 1. Rotation (Horizontal input A/D or Left Stick X)
            float turn = moveInput.x * rotationSpeed * Time.fixedDeltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);
            rb.MoveRotation(rb.rotation * turnRotation);

            // 2. Linear Movement (Vertical input W/S or Left Stick Y)
            // No strafing allowed in exploration mode
            float speed = walkSpeed;

            // Sprint check
            IsRunning = inputReader.Sprint && moveInput.y > 0.1f && currentStamina > 0f && !isExhausted;
            if (IsRunning)
            {
                speed = runSpeed;
            }

            Vector3 movement = transform.forward * moveInput.y * speed;
            
            // Preserve vertical velocity for gravity
            Vector3 velocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
            rb.linearVelocity = velocity;
        }

        /// <summary>
        /// Movement logic in Viewfinder mode (1st person).
        /// Slow movement and allows strafing, rotation is handled by the Camera/Look inputs.
        /// </summary>
        private void MoveInViewfinder()
        {
            Vector2 moveInput = inputReader.Move;
            IsRunning = false; // Cannot run in viewfinder mode

            // Move relative to player facing direction (supports slow strafing and forward/backward)
            Vector3 moveDirection = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
            Vector3 movement = moveDirection * viewfinderSpeed;

            // Preserve gravity
            Vector3 velocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);
            rb.linearVelocity = velocity;
        }

        /// <summary>
        /// Manages the stamina bar consumption and regeneration.
        /// </summary>
        private void HandleStamina()
        {
            if (isViewfinderMode)
            {
                // Regenerate stamina slowly in viewfinder mode
                RegenerateStamina();
                return;
            }

            Vector2 moveInput = inputReader.Move;
            bool isMovingForward = moveInput.y > 0.1f;

            if (IsRunning && isMovingForward)
            {
                // Consume stamina
                currentStamina -= staminaConsumeRate * Time.deltaTime;
                if (currentStamina <= 0f)
                {
                    currentStamina = 0f;
                    isExhausted = true;
                }
            }
            else
            {
                RegenerateStamina();
            }
        }

        private void RegenerateStamina()
        {
            if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                if (currentStamina >= maxStamina)
                {
                    currentStamina = maxStamina;
                }

                // Recover from exhaustion once stamina reaches threshold
                if (isExhausted && currentStamina >= staminaRecoveryThreshold)
                {
                    isExhausted = false;
                }
            }
        }

        /// <summary>
        /// Toggles the Viewfinder mode state.
        /// </summary>
        public void SetViewfinderMode(bool active)
        {
            if (isViewfinderMode == active) return;

            isViewfinderMode = active;
            IsRunning = false;
            
            // Slow down physical rotations when changing mode
            rb.angularVelocity = Vector3.zero;

            Debug.Log($"Viewfinder mode set to: {isViewfinderMode}");
        }

        /// <summary>
        /// Allows external rotation when in Viewfinder mode.
        /// </summary>
        public void RotateCharacter(float angleY)
        {
            Quaternion turnRotation = Quaternion.Euler(0f, angleY, 0f);
            rb.MoveRotation(rb.rotation * turnRotation);
        }
    }
}
