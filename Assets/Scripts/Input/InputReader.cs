using UnityEngine;
using UnityEngine.InputSystem;

namespace PhotalFrame.Input
{
    public class InputReader : MonoBehaviour
    {
        [Header("Input Action Asset")]
        [SerializeField] private InputActionAsset inputActionsAsset;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool Sprint { get; private set; }
        public bool ToggleViewfinder { get; private set; }

        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction toggleViewfinderAction;

        private void Awake()
        {
            if (inputActionsAsset != null)
            {
                var playerMap = inputActionsAsset.FindActionMap("Player");
                if (playerMap != null)
                {
                    moveAction = playerMap.FindAction("Move");
                    lookAction = playerMap.FindAction("Look");
                    sprintAction = playerMap.FindAction("Sprint");
                    toggleViewfinderAction = playerMap.FindAction("ToggleViewfinder");
                }
                else
                {
                    Debug.LogWarning("InputReader: 'Player' Action Map not found in the action asset.");
                }
            }
            else
            {
                Debug.LogWarning("InputReader: InputActionAsset is not assigned. Falling back to direct Input System polling.");
            }
        }

        private void OnEnable()
        {
            moveAction?.Enable();
            lookAction?.Enable();
            sprintAction?.Enable();
            toggleViewfinderAction?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.Disable();
            lookAction?.Disable();
            sprintAction?.Disable();
            toggleViewfinderAction?.Disable();
        }

        private void Update()
        {
            // Read values from Action Asset if available
            if (moveAction != null) Move = moveAction.ReadValue<Vector2>();
            if (lookAction != null) Look = lookAction.ReadValue<Vector2>();
            if (sprintAction != null) Sprint = sprintAction.IsPressed();

            // Handle Viewfinder Toggle input
            if (toggleViewfinderAction != null)
            {
                ToggleViewfinder = toggleViewfinderAction.WasPressedThisFrame();
            }
            else
            {
                // Fallback direct polling: Right Click on Mouse or Left Trigger on Gamepad
                bool mouseRight = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
                bool gamepadTrigger = Gamepad.current != null && Gamepad.current.leftTrigger.wasPressedThisFrame;
                ToggleViewfinder = mouseRight || gamepadTrigger;
            }
        }
    }
}
