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
        public bool Attack { get; private set; }
        public bool CyclePrevious { get; private set; }
        public bool CycleNext { get; private set; }
        public bool Heal { get; private set; }
        public bool ToggleUpgradeMenu { get; private set; }
        public bool ActivateSpecialLens { get; private set; }
        public bool Interact { get; private set; }


        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction toggleViewfinderAction;
        private InputAction attackAction;
        private InputAction previousAction;
        private InputAction nextAction;
        private InputAction healAction;
        private InputAction toggleUpgradeMenuAction;
        private InputAction activateSpecialLensAction;
        private InputAction interactAction;


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
                    attackAction = playerMap.FindAction("Attack");
                    previousAction = playerMap.FindAction("Previous");
                    nextAction = playerMap.FindAction("Next");
                    healAction = playerMap.FindAction("Heal");
                    toggleUpgradeMenuAction = playerMap.FindAction("ToggleUpgradeMenu");
                    activateSpecialLensAction = playerMap.FindAction("ActivateSpecialLens");
                    interactAction = playerMap.FindAction("Interact");
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
            attackAction?.Enable();
            previousAction?.Enable();
            nextAction?.Enable();
            healAction?.Enable();
            toggleUpgradeMenuAction?.Enable();
            activateSpecialLensAction?.Enable();
            interactAction?.Enable();
        }

        private void OnDisable()
        {
            moveAction?.Disable();
            lookAction?.Disable();
            sprintAction?.Disable();
            toggleViewfinderAction?.Disable();
            attackAction?.Disable();
            previousAction?.Disable();
            nextAction?.Disable();
            healAction?.Disable();
            toggleUpgradeMenuAction?.Disable();
            activateSpecialLensAction?.Disable();
            interactAction?.Disable();
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
                bool mouseRight = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
                bool gamepadTrigger = Gamepad.current != null && Gamepad.current.leftTrigger.wasPressedThisFrame;
                ToggleViewfinder = mouseRight || gamepadTrigger;
            }

            // Handle Attack (Photo Capture) input
            if (attackAction != null)
            {
                Attack = attackAction.WasPressedThisFrame();
            }
            else
            {
                bool mouseLeft = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
                bool gamepadWest = Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame;
                Attack = mouseLeft || gamepadWest;
            }

            // Handle Cycle Film Previous (Key 1 / Gamepad Left Dpad)
            if (previousAction != null)
            {
                CyclePrevious = previousAction.WasPressedThisFrame();
            }
            else
            {
                bool key1 = Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame;
                bool gamepadLeft = Gamepad.current != null && Gamepad.current.dpad.left.wasPressedThisFrame;
                CyclePrevious = key1 || gamepadLeft;
            }

            // Handle Cycle Film Next (Key 2 / Gamepad Right Dpad)
            if (nextAction != null)
            {
                CycleNext = nextAction.WasPressedThisFrame();
            }
            else
            {
                bool key2 = Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame;
                bool gamepadRight = Gamepad.current != null && Gamepad.current.dpad.right.wasPressedThisFrame;
                CycleNext = key2 || gamepadRight;
            }

            // Handle Heal (Key H / Gamepad Up Dpad)
            if (healAction != null)
            {
                Heal = healAction.WasPressedThisFrame();
            }
            else
            {
                bool keyH = Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame;
                bool gamepadUp = Gamepad.current != null && Gamepad.current.dpad.up.wasPressedThisFrame;
                Heal = keyH || gamepadUp;
            }

            // Handle Toggle Upgrade Menu (Key TAB / Gamepad Select Button)
            if (toggleUpgradeMenuAction != null)
            {
                ToggleUpgradeMenu = toggleUpgradeMenuAction.WasPressedThisFrame();
            }
            else
            {
                bool keyTab = Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
                bool gamepadSelect = Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame;
                ToggleUpgradeMenu = keyTab || gamepadSelect;
            }

            // Handle Activate Special Lens (Key F / Gamepad North Button / Middle Mouse)
            if (activateSpecialLensAction != null)
            {
                ActivateSpecialLens = activateSpecialLensAction.WasPressedThisFrame();
            }
            else
            {
                bool keyF = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
                bool middleMouse = Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame;
                bool gamepadNorth = Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
                ActivateSpecialLens = keyF || middleMouse || gamepadNorth;
            }

            // Handle Interact (Key E / Gamepad South Button)
            if (interactAction != null)
            {
                Interact = interactAction.WasPressedThisFrame();
            }
            else
            {
                bool keyE = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
                bool gamepadSouth = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
                Interact = keyE || gamepadSouth;
            }
        }
    }
}
