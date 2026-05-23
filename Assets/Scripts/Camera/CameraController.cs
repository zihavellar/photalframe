using UnityEngine;
using PhotalFrame.Player;
using PhotalFrame.Input;

namespace PhotalFrame.Camera
{
    public class CameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private InputReader inputReader;

        [Header("Exploration Camera (3rd Person)")]
        [SerializeField] private Vector3 explorationOffset = new Vector3(0f, 2.2f, -3.5f);
        [SerializeField] private float lookAtHeightOffset = 1.2f;
        [SerializeField] private float explorationFollowSpeed = 6f;
        [SerializeField] private float explorationRotationSpeed = 6f;

        [Header("Viewfinder Camera (1st Person)")]
        [SerializeField] private Vector3 viewfinderOffset = new Vector3(0f, 1.6f, 0.1f);
        [SerializeField] private float lookSensitivity = 15f;
        [SerializeField] private float verticalClampMin = -65f;
        [SerializeField] private float verticalClampMax = 65f;
        [SerializeField] private float viewfinderFollowSpeed = 20f;
        
        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 0.25f;

        private float cameraPitch = 0f;
        private float transitionProgress = 1f; // 0 = fully Exploration, 1 = fully Viewfinder (or vice-versa depending on state)
        private bool wasViewfinder = false;

        private Vector3 startTransitionPosition;
        private Quaternion startTransitionRotation;

        private void Start()
        {
            if (playerController == null)
                playerController = FindAnyObjectByType<PlayerController>();

            if (inputReader == null)
                inputReader = FindAnyObjectByType<InputReader>();

            if (playerController != null)
            {
                // Force camera position at start to avoid jump
                transform.position = playerController.transform.position + playerController.transform.rotation * explorationOffset;
                transform.rotation = Quaternion.LookRotation((playerController.transform.position + Vector3.up * lookAtHeightOffset) - transform.position);
                wasViewfinder = playerController.IsViewfinderMode;
            }
        }

        private void LateUpdate()
        {
            if (playerController == null || inputReader == null) return;

            bool isViewfinder = playerController.IsViewfinderMode;

            // Check if state changed to start transition tracking
            if (isViewfinder != wasViewfinder)
            {
                startTransitionPosition = transform.position;
                startTransitionRotation = transform.rotation;
                transitionProgress = 0f;
                wasViewfinder = isViewfinder;

                // Reset camera pitch when entering viewfinder mode
                if (isViewfinder)
                {
                    cameraPitch = 0f;
                    // Lock cursor in viewfinder mode
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    // Release cursor in exploration mode
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }

            if (transitionProgress < 1f)
            {
                transitionProgress += Time.deltaTime / transitionDuration;
                if (transitionProgress > 1f) transitionProgress = 1f;
            }

            if (isViewfinder)
            {
                UpdateViewfinderCamera();
            }
            else
            {
                UpdateExplorationCamera();
            }
        }

        /// <summary>
        /// Logic for third-person follow camera during exploration.
        /// </summary>
        private void UpdateExplorationCamera()
        {
            Transform playerTransform = playerController.transform;

            // Calculate target position and rotation in exploration mode
            Vector3 targetPosition = playerTransform.position + playerTransform.rotation * explorationOffset;
            
            Vector3 lookAtTarget = playerTransform.position + Vector3.up * lookAtHeightOffset;
            Quaternion targetRotation = Quaternion.LookRotation(lookAtTarget - transform.position);

            if (transitionProgress < 1f)
            {
                // Smoothly transition from previous camera state
                transform.position = Vector3.Lerp(startTransitionPosition, targetPosition, transitionProgress);
                transform.rotation = Quaternion.Slerp(startTransitionRotation, targetRotation, transitionProgress);
            }
            else
            {
                // Normal follow behaviour
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * explorationFollowSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * explorationRotationSpeed);
            }
        }

        /// <summary>
        /// Logic for first-person camera inside the viewfinder.
        /// </summary>
        private void UpdateViewfinderCamera()
        {
            Transform playerTransform = playerController.transform;
            Vector2 lookInput = inputReader.Look;

            // 1. Horizontal rotation: Rotates the character's body directly
            float yawInput = lookInput.x * lookSensitivity * Time.deltaTime;
            playerController.RotateCharacter(yawInput);

            // 2. Vertical rotation (Pitch): Rotates the camera locally and clamps it
            cameraPitch -= lookInput.y * lookSensitivity * Time.deltaTime;
            cameraPitch = Mathf.Clamp(cameraPitch, verticalClampMin, verticalClampMax);

            // 3. Position and Rotation Targets
            Vector3 targetPosition = playerTransform.position + playerTransform.rotation * viewfinderOffset;
            Quaternion targetRotation = playerTransform.rotation * Quaternion.Euler(cameraPitch, 0f, 0f);

            if (transitionProgress < 1f)
            {
                // Lerp from the third person camera state to the eye position
                transform.position = Vector3.Lerp(startTransitionPosition, targetPosition, transitionProgress);
                transform.rotation = Quaternion.Slerp(startTransitionRotation, targetRotation, transitionProgress);
            }
            else
            {
                // Locked to the player eye position and tracking rotation instantly
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * viewfinderFollowSpeed);
                transform.rotation = targetRotation;
            }
        }
    }
}
