using UnityEngine;
using UnityEngine.UI;
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
        [SerializeField] private World.SavePoint savePoint;

        private bool isOpen = false;

        private void Start()
        {
            if (inputReader == null)
                inputReader = FindAnyObjectByType<InputReader>();

            if (playerInventory == null)
                playerInventory = FindAnyObjectByType<PlayerInventory>();

            if (savePoint == null)
                savePoint = FindAnyObjectByType<World.SavePoint>();

            if (savePanel != null)
                savePanel.SetActive(false);

            if (confirmButton != null)
                confirmButton.onClick.AddListener(ConfirmSave);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(CancelSave);
        }

        private void Update()
        {
            if (inputReader == null) return;

            // Hook into save point interaction via Interact key
            // SavePoint handles this directly now, but this UI can be shown by it
            if (isOpen)
            {
                if (inputReader.Interact)
                {
                    CancelSave();
                }
            }
        }

        public void OpenSavePrompt()
        {
            if (isOpen) return;

            isOpen = true;
            if (savePanel != null)
                savePanel.SetActive(true);

            // Pause the game
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (savePromptText != null)
            {
                bool hasTape = playerInventory != null && playerInventory.VirginTapeCount > 0;
                savePromptText.text = hasTape ?
                    $"Save game? (Consumes 1 Virgin Tape - {playerInventory.VirginTapeCount} left)" :
                    "No Virgin Tape! Find one to save.";
            }

            if (confirmButton != null)
                confirmButton.interactable = playerInventory != null && playerInventory.VirginTapeCount > 0;
        }

        public void ConfirmSave()
        {
            if (savePoint != null && playerInventory != null && playerInventory.VirginTapeCount > 0)
            {
                savePoint.PerformSave();
                CloseSavePrompt();
            }
        }

        public void CancelSave()
        {
            CloseSavePrompt();
        }

        private void CloseSavePrompt()
        {
            isOpen = false;
            if (savePanel != null)
                savePanel.SetActive(false);

            // Restore game
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
