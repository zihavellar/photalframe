using UnityEngine;
using UnityEngine.UI;
using PhotalFrame.Player;

namespace PhotalFrame.UI
{
    public class PlayerUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;

        [Header("UI Panels")]
        [SerializeField] private GameObject viewfinderPanel;
        [SerializeField] private GameObject staminaPanel;
        [SerializeField] private Slider staminaSlider;
        [SerializeField] private CanvasGroup staminaCanvasGroup;

        [Header("Stamina UI Settings")]
        [SerializeField] private float fadeSpeed = 5f;
        [SerializeField] private bool hideStaminaWhenFull = true;

        private void Start()
        {
            if (playerController == null)
                playerController = FindAnyObjectByType<PlayerController>();

            if (viewfinderPanel != null)
                viewfinderPanel.SetActive(false);

            if (staminaSlider != null && playerController != null)
            {
                staminaSlider.maxValue = playerController.MaxStamina;
                staminaSlider.value = playerController.CurrentStamina;
            }
        }

        private void Update()
        {
            if (playerController == null) return;

            // 1. Toggle Viewfinder Panel UI
            if (viewfinderPanel != null)
            {
                viewfinderPanel.SetActive(playerController.IsViewfinderMode);
            }

            // 2. Update and Fade Stamina UI
            if (staminaSlider != null && staminaCanvasGroup != null)
            {
                staminaSlider.value = playerController.CurrentStamina;

                bool shouldShowStamina = playerController.IsRunning || 
                                         playerController.CurrentStamina < playerController.MaxStamina || 
                                         playerController.IsExhausted;

                float targetAlpha = (shouldShowStamina && hideStaminaWhenFull) || !hideStaminaWhenFull ? 1f : 0f;
                staminaCanvasGroup.alpha = Mathf.MoveTowards(staminaCanvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);

                if (staminaPanel != null)
                {
                    staminaPanel.SetActive(staminaCanvasGroup.alpha > 0.01f);
                }
            }
        }
    }
}
