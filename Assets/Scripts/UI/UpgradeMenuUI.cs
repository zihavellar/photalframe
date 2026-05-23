using UnityEngine;
using UnityEngine.UI;
using PhotalFrame.Player;
using PhotalFrame.Camera;
using PhotalFrame.Input;

namespace PhotalFrame.UI
{
    public class UpgradeMenuUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputReader inputReader;
        [SerializeField] private PlayerUpgrades playerUpgrades;
        [SerializeField] private GameObject menuPanel;

        [Header("Menu Elements")]
        [SerializeField] private Text pointsText;
        
        [Space]
        [SerializeField] private Text powerLevelText;
        [SerializeField] private Text powerCostText;
        [SerializeField] private Button powerUpgradeButton;

        [Space]
        [SerializeField] private Text reloadLevelText;
        [SerializeField] private Text reloadCostText;
        [SerializeField] private Button reloadUpgradeButton;

        [Space]
        [SerializeField] private Text rangeLevelText;
        [SerializeField] private Text rangeCostText;
        [SerializeField] private Button rangeUpgradeButton;

        private PlayerController playerController;
        private CameraController cameraController;
        private bool isMenuOpen = false;

        private void Start()
        {
            playerController = FindAnyObjectByType<PlayerController>();
            cameraController = FindAnyObjectByType<CameraController>();

            if (inputReader == null)
                inputReader = FindAnyObjectByType<InputReader>();

            if (playerUpgrades == null && playerController != null)
                playerUpgrades = playerController.GetComponent<PlayerUpgrades>();

            // Setup button listeners
            if (powerUpgradeButton != null)
                powerUpgradeButton.onClick.AddListener(UpgradePower);

            if (reloadUpgradeButton != null)
                reloadUpgradeButton.onClick.AddListener(UpgradeReload);

            if (rangeUpgradeButton != null)
                rangeUpgradeButton.onClick.AddListener(UpgradeRange);

            // Start closed
            if (menuPanel != null)
                menuPanel.SetActive(false);
            
            isMenuOpen = false;
        }

        private void Update()
        {
            if (inputReader == null) return;

            // Only allow toggle if player is alive
            PlayerHealth health = playerController != null ? playerController.GetComponent<PlayerHealth>() : null;
            if (health != null && health.IsDead) return;

            if (inputReader.ToggleUpgradeMenu)
            {
                ToggleMenu();
            }
        }

        public void ToggleMenu()
        {
            isMenuOpen = !isMenuOpen;

            if (menuPanel != null)
                menuPanel.SetActive(isMenuOpen);

            if (isMenuOpen)
            {
                // Pause time
                Time.timeScale = 0f;

                // Unlock cursor
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Disable player controls so we can't move/look while upgrading
                if (playerController != null) playerController.enabled = false;
                if (cameraController != null) cameraController.enabled = false;

                RefreshMenu();
            }
            else
            {
                // Unpause time
                Time.timeScale = 1f;

                // Re-lock cursor
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                // Re-enable controls
                if (playerController != null) playerController.enabled = true;
                if (cameraController != null) cameraController.enabled = true;
            }
        }

        private void RefreshMenu()
        {
            if (playerUpgrades == null) return;

            // Update total points display
            if (pointsText != null)
            {
                pointsText.text = $"PONTOS ESPIRITUAIS: {playerUpgrades.SpiritPoints:N0}";
            }

            // Power
            UpdateAttributeUI(playerUpgrades.PowerLevel, powerLevelText, powerCostText, powerUpgradeButton);

            // Reload
            UpdateAttributeUI(playerUpgrades.ReloadLevel, reloadLevelText, reloadCostText, reloadUpgradeButton);

            // Range
            UpdateAttributeUI(playerUpgrades.RangeLevel, rangeLevelText, rangeCostText, rangeUpgradeButton);
        }

        private void UpdateAttributeUI(int currentLevel, Text levelText, Text costText, Button upgradeButton)
        {
            if (playerUpgrades == null) return;

            // 1. Level string formatting like: [ X ][   ][   ][   ]
            if (levelText != null)
            {
                string lvlStr = "";
                for (int i = 1; i <= 4; i++)
                {
                    lvlStr += (i <= currentLevel) ? "[ X ]" : "[   ]";
                }
                levelText.text = lvlStr;
            }

            // 2. Cost display and button interactivity
            int nextCost = playerUpgrades.GetUpgradeCost(currentLevel);
            if (nextCost == -1) // Max level reached
            {
                if (costText != null) costText.text = "MÁXIMO";
                if (upgradeButton != null) upgradeButton.interactable = false;
            }
            else
            {
                if (costText != null) costText.text = $"{nextCost:N0} PTS";
                if (upgradeButton != null)
                {
                    upgradeButton.interactable = playerUpgrades.SpiritPoints >= nextCost;
                }
            }
        }

        private void UpgradePower()
        {
            if (playerUpgrades != null && playerUpgrades.TryUpgradePower())
            {
                RefreshMenu();
            }
        }

        private void UpgradeReload()
        {
            if (playerUpgrades != null && playerUpgrades.TryUpgradeReload())
            {
                RefreshMenu();
            }
        }

        private void UpgradeRange()
        {
            if (playerUpgrades != null && playerUpgrades.TryUpgradeRange())
            {
                RefreshMenu();
            }
        }
    }
}
