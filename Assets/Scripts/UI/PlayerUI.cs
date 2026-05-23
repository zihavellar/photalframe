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
        [SerializeField] private GameObject fatalWarningText; // Text GameObject displaying "FATAL" during lunge
        [SerializeField] private float reticleFlashSpeed = 15f;
        [SerializeField] private Color fatalReticleColor1 = new Color(1f, 0.1f, 0.1f, 0.9f);
        [SerializeField] private Color fatalReticleColor2 = new Color(1f, 0.78f, 0.05f, 0.9f); // Golden yellow

        [Header("Settings")]
        [SerializeField] private float staminaFadeSpeed = 5f;
        [SerializeField] private bool hideStaminaWhenFull = true;
        [SerializeField] private float flashFadeSpeed = 6f;

        [Header("Reticle Colors")]
        [SerializeField] private Color defaultReticleColor = new Color(0.9f, 0.9f, 0.9f, 0.45f);
        [SerializeField] private Color lockOnReticleColor = new Color(1.0f, 0.2f, 0.2f, 0.8f);

        private void Start()
        {
            if (playerController == null)
                playerController = FindAnyObjectByType<PlayerController>();

            if (cameraObscura == null)
                cameraObscura = FindAnyObjectByType<CameraObscura>();

            // Setup Event listeners for the camera flash and Fatal Frame hits
            if (cameraObscura != null)
            {
                cameraObscura.OnPhotoTaken += TriggerCameraFlash;
                cameraObscura.OnFatalFrameHit += TriggerFatalFrameFlash;
            }

            if (viewfinderPanel != null)
                viewfinderPanel.SetActive(false);

            if (staminaSlider != null && playerController != null)
            {
                staminaSlider.maxValue = playerController.MaxStamina;
                staminaSlider.value = playerController.CurrentStamina;
            }

            if (flashCanvasGroup != null)
            {
                flashCanvasGroup.alpha = 0f;
            }

            if (fatalWarningText != null)
            {
                fatalWarningText.SetActive(false);
            }

            if (filmText != null)
            {
                filmText.text = "Type-14  [ ∞ ]";
            }
        }

        private void OnDestroy()
        {
            if (cameraObscura != null)
            {
                cameraObscura.OnPhotoTaken -= TriggerCameraFlash;
                cameraObscura.OnFatalFrameHit -= TriggerFatalFrameFlash;
            }
        }

        private void Update()
        {
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
            FadeFlashUI();
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
                    // Pulsating red/yellow flash
                    float t = Mathf.PingPong(Time.time * reticleFlashSpeed, 1f);
                    reticleImage.color = Color.Lerp(fatalReticleColor1, fatalReticleColor2, t);
                    reticleImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.0f, 1.2f, t);
                }
                else
                {
                    // Standard color behavior and normal scale
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

        private void TriggerCameraFlash()
        {
            if (flashCanvasGroup != null)
            {
                Image flashImg = flashCanvasGroup.GetComponent<Image>();
                if (flashImg != null)
                {
                    flashImg.color = Color.white; // Standard white flash
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
                    // Intense red-orange flash for Fatal Frame strike
                    flashImg.color = new Color(1.0f, 0.15f, 0.1f, 0.9f);
                }
                flashCanvasGroup.alpha = 1f;
            }
        }

        private void FadeFlashUI()
        {
            if (flashCanvasGroup != null && flashCanvasGroup.alpha > 0f)
            {
                // Fade out flash over real time to ignore hitstop time slowdown
                flashCanvasGroup.alpha = Mathf.MoveTowards(flashCanvasGroup.alpha, 0f, flashFadeSpeed * Time.unscaledDeltaTime);
            }
        }
    }
}
