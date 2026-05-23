using UnityEngine;
using UnityEngine.UI;
using PhotalFrame.Player;
using PhotalFrame.Camera;

namespace PhotalFrame.UI
{
    public class PlayerUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private CameraObscura cameraObscura;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerInventory playerInventory;

        [Header("UI Panels")]
        [SerializeField] private GameObject viewfinderPanel;
        [SerializeField] private GameObject staminaPanel;
        [SerializeField] private Slider staminaSlider;
        [SerializeField] private CanvasGroup staminaCanvasGroup;

        [Header("Camera Obscura UI")]
        [SerializeField] private Image reticleImage;
        [SerializeField] private Slider rechargeSlider;
        [SerializeField] private CanvasGroup flashCanvasGroup;
        [SerializeField] private Text filmText;

        [Header("Fatal Frame UI Settings")]
        [SerializeField] private GameObject fatalWarningText;
        [SerializeField] private float reticleFlashSpeed = 15f;
        [SerializeField] private Color fatalReticleColor1 = new Color(1f, 0.1f, 0.1f, 0.9f);
        [SerializeField] private Color fatalReticleColor2 = new Color(1f, 0.78f, 0.05f, 0.9f);

        [Header("Resource & State UI")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Slider damageCatchUpSlider; // Red bar that lags behind on damage
        [SerializeField] private Text statusText; // FINE, CAUTION, DANGER status indicator
        [SerializeField] private Text medicineText;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private CanvasGroup damageIndicatorCanvasGroup; // Red vignette flash on take damage

        [Header("Spirit Progress UI")]
        [SerializeField] private Text spiritPointsText;
        [SerializeField] private Text spiritOrbsText;
        [SerializeField] private PlayerUpgrades playerUpgrades;

        [Header("Save Items")]
    [SerializeField] private Text virginTapeText;

        [Header("Settings")]
        [SerializeField] private float staminaFadeSpeed = 5f;
        [SerializeField] private bool hideStaminaWhenFull = true;
        [SerializeField] private float flashFadeSpeed = 6f;
        [SerializeField] private float damageIndicatorFadeSpeed = 2f;

        [Header("Reticle Colors")]
        [SerializeField] private Color defaultReticleColor = new Color(0.9f, 0.9f, 0.9f, 0.45f);
        [SerializeField] private Color lockOnReticleColor = new Color(1.0f, 0.2f, 0.2f, 0.8f);

        [Header("Health Bar Color Settings")]
        [SerializeField] private Color healthyColor = new Color(0.15f, 0.8f, 0.9f, 0.85f); // Beautiful cyan
        [SerializeField] private Color warningColor = new Color(0.95f, 0.65f, 0.05f, 0.85f); // Orange-yellow
        [SerializeField] private Color dangerColor = new Color(0.95f, 0.1f, 0.1f, 0.95f); // Red

        private float targetHealth;
        private Image healthFillImage;
        private float statusPulseTimer = 0f;

        private void Start()
        {
            if (playerController == null)
                playerController = FindAnyObjectByType<PlayerController>();

            if (cameraObscura == null)
                cameraObscura = FindAnyObjectByType<CameraObscura>();

            if (playerHealth == null)
                playerHealth = FindAnyObjectByType<PlayerHealth>();

            if (playerInventory == null)
                playerInventory = FindAnyObjectByType<PlayerInventory>();

            // Setup Event listeners
            if (cameraObscura != null)
            {
                cameraObscura.OnPhotoTaken += TriggerCameraFlash;
                cameraObscura.OnFatalFrameHit += TriggerFatalFrameFlash;
            }

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateHealthBar;
                playerHealth.OnDamageTaken += TriggerDamageIndicator;
                playerHealth.OnDeath += TriggerGameOver;

                targetHealth = playerHealth.CurrentHealth;

                // Set initial HP slider bounds
                if (healthSlider != null)
                {
                    healthSlider.maxValue = playerHealth.MaxHealth;
                    healthSlider.value = playerHealth.CurrentHealth;
                    if (healthSlider.fillRect != null)
                    {
                        healthFillImage = healthSlider.fillRect.GetComponent<Image>();
                    }
                }

                if (damageCatchUpSlider != null)
                {
                    damageCatchUpSlider.maxValue = playerHealth.MaxHealth;
                    damageCatchUpSlider.value = playerHealth.CurrentHealth;
                }
            }

            if (playerInventory != null)
            {
                playerInventory.OnInventoryChanged += UpdateInventoryText;
            }

            if (playerUpgrades == null)
                playerUpgrades = FindAnyObjectByType<PlayerUpgrades>();

            if (playerUpgrades != null)
            {
                playerUpgrades.OnUpgradesChanged += UpdateUpgradeUIElements;
            }

            if (viewfinderPanel != null)
                viewfinderPanel.SetActive(false);

            if (staminaSlider != null && playerController != null)
            {
                staminaSlider.maxValue = playerController.MaxStamina;
                staminaSlider.value = playerController.CurrentStamina;
            }

            if (flashCanvasGroup != null)
                flashCanvasGroup.alpha = 0f;

            if (damageIndicatorCanvasGroup != null)
                damageIndicatorCanvasGroup.alpha = 0f;

            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            if (fatalWarningText != null)
                fatalWarningText.SetActive(false);

            UpdateInventoryText();
            UpdateUpgradeUIElements();
        }

        private void OnDestroy()
        {
            if (cameraObscura != null)
            {
                cameraObscura.OnPhotoTaken -= TriggerCameraFlash;
                cameraObscura.OnFatalFrameHit -= TriggerFatalFrameFlash;
            }

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateHealthBar;
                playerHealth.OnDamageTaken -= TriggerDamageIndicator;
                playerHealth.OnDeath -= TriggerGameOver;
            }

            if (playerInventory != null)
            {
                playerInventory.OnInventoryChanged -= UpdateInventoryText;
            }

            if (playerUpgrades != null)
            {
                playerUpgrades.OnUpgradesChanged -= UpdateUpgradeUIElements;
            }
        }

        private void Update()
        {
            // Even when time scale is 0 (Game Over), keep UI fading transitions running
            FadeFlashUI();
            FadeDamageIndicatorUI();
            InterpolateHealthBar();

            if (playerController == null) return;

            // 1. Toggle Viewfinder Panel UI
            bool isViewfinder = playerController.IsViewfinderMode;
            if (viewfinderPanel != null && viewfinderPanel.activeSelf != isViewfinder)
            {
                viewfinderPanel.SetActive(isViewfinder);
            }

            if (isViewfinder)
            {
                UpdateViewfinderUI();
            }

            UpdateStaminaUI();
        }

        /// <summary>
        /// Updates UI elements specific to viewfinder mode (Reticle and Recharge).
        /// </summary>
        private void UpdateViewfinderUI()
        {
            if (cameraObscura == null) return;

            bool isFatalActive = cameraObscura.IsFatalFrameActive;

            // 1. Reticle Animation and Color
            if (reticleImage != null)
            {
                if (isFatalActive)
                {
                    float t = Mathf.PingPong(Time.time * reticleFlashSpeed, 1f);
                    reticleImage.color = Color.Lerp(fatalReticleColor1, fatalReticleColor2, t);
                    reticleImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.0f, 1.2f, t);
                }
                else
                {
                    reticleImage.color = cameraObscura.HasTargetInReticle ? lockOnReticleColor : defaultReticleColor;
                    reticleImage.rectTransform.localScale = Vector3.one;
                }
            }

            // 2. Fatal Warning text activation
            if (fatalWarningText != null)
            {
                fatalWarningText.SetActive(isFatalActive);
            }

            // 3. Camera Recharge Slider Progress
            if (rechargeSlider != null)
            {
                rechargeSlider.value = cameraObscura.CooldownProgress;
                rechargeSlider.gameObject.SetActive(cameraObscura.CooldownTimer > 0f);
            }
        }

        /// <summary>
        /// Update and Fade Stamina UI based on character state.
        /// </summary>
        private void UpdateStaminaUI()
        {
            if (staminaSlider == null || staminaCanvasGroup == null) return;

            staminaSlider.value = playerController.CurrentStamina;

            bool shouldShowStamina = playerController.IsRunning || 
                                     playerController.CurrentStamina < playerController.MaxStamina || 
                                     playerController.IsExhausted;

            float targetAlpha = (shouldShowStamina && hideStaminaWhenFull) || !hideStaminaWhenFull ? 1f : 0f;
            staminaCanvasGroup.alpha = Mathf.MoveTowards(staminaCanvasGroup.alpha, targetAlpha, staminaFadeSpeed * Time.deltaTime);

            if (staminaPanel != null)
            {
                staminaPanel.SetActive(staminaCanvasGroup.alpha > 0.01f);
            }
        }

        private void UpdateHealthBar(float hp)
        {
            targetHealth = hp;
        }

        private void InterpolateHealthBar()
        {
            if (healthSlider != null)
            {
                // Smoothly slide health slider
                healthSlider.value = Mathf.MoveTowards(healthSlider.value, targetHealth, Time.unscaledDeltaTime * 100f);

                float healthPercent = healthSlider.value / healthSlider.maxValue;
                Color activeColor = healthyColor;
                string statusStr = "FINE";
                Color statusCol = healthyColor;

                if (healthPercent <= 0.25f)
                {
                    activeColor = dangerColor;
                    statusStr = "DANGER";
                    statusCol = dangerColor;
                }
                else if (healthPercent <= 0.5f)
                {
                    activeColor = warningColor;
                    statusStr = "CAUTION";
                    statusCol = warningColor;
                }

                if (healthFillImage != null)
                {
                    healthFillImage.color = activeColor;
                }

                if (statusText != null)
                {
                    statusText.text = statusStr;
                    statusText.color = statusCol;
                }

                // Handle warning pulse for low HP (Danger state)
                if (healthPercent <= 0.25f && targetHealth > 0f && playerHealth != null && !playerHealth.IsDead)
                {
                    statusPulseTimer += Time.unscaledDeltaTime * 5f;
                    float pulse = Mathf.PingPong(statusPulseTimer, 1f);

                    // Pulse scale of the status text
                    if (statusText != null)
                    {
                        statusText.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.15f, pulse);
                    }

                    // Persistent pulsing vignette visual warning
                    if (damageIndicatorCanvasGroup != null)
                    {
                        damageIndicatorCanvasGroup.alpha = Mathf.Lerp(0.15f, 0.45f, pulse);
                    }
                }
                else
                {
                    if (statusText != null)
                    {
                        statusText.rectTransform.localScale = Vector3.one;
                    }
                }
            }

            if (damageCatchUpSlider != null)
            {
                // Smoothly lag the damage bar behind
                damageCatchUpSlider.value = Mathf.MoveTowards(damageCatchUpSlider.value, targetHealth, Time.unscaledDeltaTime * 25f);
            }
        }

        private void UpdateUpgradeUIElements()
        {
            if (playerUpgrades == null) return;

            if (spiritPointsText != null)
            {
                spiritPointsText.text = $"PTS: {playerUpgrades.SpiritPoints:N0}";
            }

            if (spiritOrbsText != null)
            {
                int currentOrbs = playerUpgrades.SpiritOrbs;
                int maxOrbs = playerUpgrades.MaxSpiritOrbs;
                string orbsStr = "";
                for (int i = 0; i < maxOrbs; i++)
                {
                    orbsStr += (i < currentOrbs) ? "● " : "○ ";
                }
                spiritOrbsText.text = $"LENS: {orbsStr.Trim()}";
            }
        }

        private void UpdateInventoryText()
        {
            if (playerInventory == null) return;

            // 1. Film counter (formatted cleanly)
            if (filmText != null)
            {
                FilmType film = playerInventory.ActiveFilm;
                int count = playerInventory.GetFilmCount(film);
                
                string filmName = film == FilmType.Type14 ? "Type-14" : 
                                 (film == FilmType.Type61 ? "Type-61" : "Type-90");
                
                string countStr = count == -1 ? "[ ∞ ]" : $"[ {count} ]";
                filmText.text = $"{filmName}  {countStr}";
            }

            // 3. Virgin tape count
            if (virginTapeText != null)
            {
                virginTapeText.text = $"Tape: {playerInventory.VirginTapeCount}";
            }

            // 2. Medicine count
            if (medicineText != null)
            {
                medicineText.text = $"Med: {playerInventory.HerbalMedicineCount}";
            }
        }

        private void TriggerCameraFlash()
        {
            if (flashCanvasGroup != null)
            {
                Image flashImg = flashCanvasGroup.GetComponent<Image>();
                if (flashImg != null)
                {
                    flashImg.color = Color.white;
                }
                flashCanvasGroup.alpha = 1f;
            }
        }

        private void TriggerFatalFrameFlash()
        {
            if (flashCanvasGroup != null)
            {
                Image flashImg = flashCanvasGroup.GetComponent<Image>();
                if (flashImg != null)
                {
                    flashImg.color = new Color(1.0f, 0.15f, 0.1f, 0.9f);
                }
                flashCanvasGroup.alpha = 1f;
            }
        }

        private void TriggerDamageIndicator()
        {
            if (damageIndicatorCanvasGroup != null)
            {
                damageIndicatorCanvasGroup.alpha = 0.7f;
            }
        }

        private void TriggerGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }

        private void FadeFlashUI()
        {
            if (flashCanvasGroup != null && flashCanvasGroup.alpha > 0f)
            {
                flashCanvasGroup.alpha = Mathf.MoveTowards(flashCanvasGroup.alpha, 0f, flashFadeSpeed * Time.unscaledDeltaTime);
            }
        }

        private void FadeDamageIndicatorUI()
        {
            // Only fade to 0 if the player is healthy/caution. Critical health has a persistent pulse.
            bool isCritical = playerHealth != null && (playerHealth.CurrentHealth / playerHealth.MaxHealth) <= 0.25f && !playerHealth.IsDead;
            if (isCritical) return;

            if (damageIndicatorCanvasGroup != null && damageIndicatorCanvasGroup.alpha > 0f)
            {
                damageIndicatorCanvasGroup.alpha = Mathf.MoveTowards(damageIndicatorCanvasGroup.alpha, 0f, damageIndicatorFadeSpeed * Time.unscaledDeltaTime);
            }
        }
    }
}
