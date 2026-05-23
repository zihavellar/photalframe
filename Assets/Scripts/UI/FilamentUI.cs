using UnityEngine;
using UnityEngine.UI;
using PhotalFrame.Player;
using PhotalFrame.Ghost;

namespace PhotalFrame.UI
{
    public class FilamentUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private RectTransform pointerRect; // UI element that rotates (the needle/indicator)
        [SerializeField] private CanvasGroup canvasGroup; // Controls visibility of the indicator
        [SerializeField] private Image filamentImage; // Visual needle image to change color

        [Header("Detection Settings")]
        [SerializeField] private float maxDetectionDistance = 15f;
        [SerializeField] private float minDetectionDistance = 3f;

        [Header("Visual Feedback Colors")]
        [SerializeField] private Color normalColor = new Color(0.2f, 0.8f, 1.0f, 0.8f); // Ethereal light blue
        [SerializeField] private Color hostileColor = new Color(1.0f, 0.15f, 0.15f, 0.9f); // Alert red

        private void Start()
        {
            if (playerController == null)
                playerController = FindAnyObjectByType<PlayerController>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        private void Update()
        {
            if (playerController == null || canvasGroup == null) return;

            // Option B: Filament is only active when in viewfinder mode (camera raised)
            if (!playerController.IsViewfinderMode)
            {
                FadeOutUI();
                return;
            }

            GhostTarget nearestGhost = FindNearestGhost();

            if (nearestGhost != null)
            {
                float distance = Vector3.Distance(playerController.transform.position, nearestGhost.transform.position);

                if (distance <= maxDetectionDistance)
                {
                    // 1. Calculate relative angle to rotate the UI needle
                    Vector3 playerPos = playerController.transform.position;
                    Vector3 ghostPos = nearestGhost.transform.position;
                    Vector3 dirToGhost = ghostPos - playerPos;
                    dirToGhost.y = 0; // Flat direction on X-Z plane

                    // SignedAngle gives the angle in degrees relative to the player's forward vector
                    float angle = Vector3.SignedAngle(playerController.transform.forward, dirToGhost.normalized, Vector3.up);

                    // Set rotation on Z-axis (2D rotation) to point at the ghost
                    if (pointerRect != null)
                    {
                        pointerRect.localRotation = Quaternion.Euler(0f, 0f, -angle);
                    }

                    // 2. Adjust Alpha opacity based on distance (closer = brighter)
                    float targetAlpha = 1f;
                    if (distance > minDetectionDistance)
                    {
                        // Lerp alpha from 0 to 1 between max and min detection distances
                        targetAlpha = 1f - ((distance - minDetectionDistance) / (maxDetectionDistance - minDetectionDistance));
                    }
                    canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, 5f * Time.deltaTime);

                    // 3. Set Color based on Ghost state (Red = attacking, Blue = idle/chase/invisible)
                    if (filamentImage != null)
                    {
                        bool isAttacking = nearestGhost.CurrentState == GhostState.WindingUp || 
                                           nearestGhost.CurrentState == GhostState.Lunging;
                        
                        Color targetColor = isAttacking ? hostileColor : normalColor;
                        filamentImage.color = Color.Lerp(filamentImage.color, targetColor, 7f * Time.deltaTime);
                    }
                }
                else
                {
                    FadeOutUI();
                }
            }
            else
            {
                FadeOutUI();
            }
        }

        private void FadeOutUI()
        {
            if (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, 5f * Time.deltaTime);
            }
        }

        /// <summary>
        /// Scans the scene to find the closest active (undefeated) ghost.
        /// </summary>
        private GhostTarget FindNearestGhost()
        {
            GhostTarget[] ghosts = FindObjectsByType<GhostTarget>(FindObjectsSortMode.None);
            GhostTarget closest = null;
            float minDistance = float.MaxValue;

            foreach (var ghost in ghosts)
            {
                if (ghost == null) continue;

                float dist = Vector3.Distance(playerController.transform.position, ghost.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = ghost;
                }
            }

            return closest;
        }
    }
}
