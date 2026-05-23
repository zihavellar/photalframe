using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using PhotalFrame.Player;
using PhotalFrame.Input;

namespace PhotalFrame.UI
{
    public class SaveMenuUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject savePanel;
        [SerializeField] private Text savePromptText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private InputReader inputReader;
        [SerializeField] private PlayerInventory playerInventory;

        private World.SavePoint currentSavePoint;
        private bool isOpen = false;
        private bool isCountingDown = false;
        private int countdownValue = 3;
        private float countdownTimer = 0f;

        public bool IsOpen => isOpen;

        private void Start()
        {
            if (inputReader == null)
                inputReader = FindAnyObjectByType<InputReader>();

            if (playerInventory == null)
                playerInventory = FindAnyObjectByType<PlayerInventory>();

            if (savePanel != null)
                savePanel.SetActive(false);

            if (confirmButton != null)
                confirmButton.onClick.AddListener(ConfirmSave);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(CancelSave);
        }

        private void Update()
        {
            if (isOpen && !isCountingDown)
            {
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.escapeKey.wasPressedThisFrame)
                        CancelSave();
                    if (Keyboard.current.yKey.wasPressedThisFrame)
                        ConfirmSave();
                }
            }

            if (isCountingDown)
            {
                countdownTimer += Time.unscaledDeltaTime;
                int display = Mathf.Max(0, countdownValue - Mathf.FloorToInt(countdownTimer));
                if (savePromptText != null)
                    savePromptText.text = $"Saved!  {display}";

                if (countdownTimer >= countdownValue)
                {
                    CloseSavePrompt();
                }
            }
        }

        public void OpenSavePrompt(World.SavePoint savePoint)
        {
            if (isOpen) return;

            currentSavePoint = savePoint;
            isOpen = true;
            if (savePanel != null)
                savePanel.SetActive(true);

            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (savePromptText != null)
            {
                bool hasTape = playerInventory != null && playerInventory.VirginTapeCount > 0;
                savePromptText.text = hasTape ?
                    $"Save game? (Consumes 1 Virgin Tape - {playerInventory.VirginTapeCount} left)\nESC to cancel" :
                    "No Virgin Tape! Find one to save.\nESC to cancel";
            }

            if (confirmButton != null)
                confirmButton.interactable = playerInventory != null && playerInventory.VirginTapeCount > 0;
        }

        public void ConfirmSave()
        {
            if (currentSavePoint != null && playerInventory != null && playerInventory.VirginTapeCount > 0 && playerInventory.ConsumeVirginTape())
            {
                currentSavePoint.PerformSave();
                StartCountdown();
            }
        }

        private void StartCountdown()
        {
            isCountingDown = true;
            countdownTimer = 0f;
            countdownValue = 3;

            if (confirmButton != null) confirmButton.gameObject.SetActive(false);
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);

            if (savePromptText != null)
                savePromptText.text = "Saved!  3";
        }

        public void CancelSave()
        {
            if (isCountingDown) return;
            CloseSavePrompt();
        }

        private void CloseSavePrompt()
        {
            isOpen = false;
            isCountingDown = false;
            currentSavePoint = null;
            if (savePanel != null)
                savePanel.SetActive(false);

            if (confirmButton != null) confirmButton.gameObject.SetActive(true);
            if (cancelButton != null) cancelButton.gameObject.SetActive(true);

            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
