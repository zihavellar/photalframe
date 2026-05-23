using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using PhotalFrame.Player;
using PhotalFrame.Input;
using PhotalFrame.Ghost;

namespace PhotalFrame.Camera
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    [RequireComponent(typeof(AudioSource))]
    public class CameraObscura : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private InputReader inputReader;

        [Header("Camera Obscura Properties")]
        [SerializeField] private float baseDamage = 35f;
        [SerializeField] private float maxCaptureDistance = 15f;
        [SerializeField] private float shutterCooldown = 1.5f;
        [SerializeField] private float reticleRadius = 0.25f; // Boundary of capture circle in viewport units

        [Header("Audio Settings")]
        [SerializeField] private AudioClip shutterSound;
        [SerializeField] private AudioClip rechargeCompleteSound;
        [SerializeField] private AudioClip fatalFrameHitSound; // Sound played on successful Fatal Frame timing

        private UnityEngine.Camera cam;
        private AudioSource audioSource;
        private float cooldownTimer = 0f;
        private bool wasReady = true;

        private GhostTarget currentTargetInReticle = null;
        private Vector3 targetViewportPosition = Vector3.zero;

        // Events for UI and visual feedbacks
        public event Action OnPhotoTaken;
        public event Action OnFatalFrameHit; // Triggers extra impact flash in UI
        public event Action OnRechargeComplete;

        public float CooldownTimer => cooldownTimer;
        public float ShutterCooldown => shutterCooldown;
        public float CooldownProgress => Mathf.Clamp01(1f - (cooldownTimer / shutterCooldown));
        public bool IsReady => cooldownTimer <= 0f;
        public bool HasTargetInReticle => currentTargetInReticle != null;
        public GhostTarget CurrentTarget => currentTargetInReticle;
        public bool IsFatalFrameActive => currentTargetInReticle != null && currentTargetInReticle.CanFatalFrame;

        private void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();
            audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            if (playerController == null)
                playerController = FindAnyObjectByType<PlayerController>();

            if (inputReader == null)
                inputReader = FindAnyObjectByType<InputReader>();
        }

        private void Update()
        {
            if (playerController == null || inputReader == null) return;

            // Handle Cooldown Timer
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
                if (cooldownTimer <= 0f)
                {
                    cooldownTimer = 0f;
                    if (!wasReady)
                    {
                        wasReady = true;
                        OnRechargeComplete?.Invoke();
                        PlayRechargeSound();
                    }
                }
            }

            // Only detect targets and allow shooting when inside the viewfinder
            if (playerController.IsViewfinderMode)
            {
                ScanForGhosts();
                HandleShooting();
            }
            else
            {
                currentTargetInReticle = null;
            }
        }

        /// <summary>
        /// Scans the scene for GhostTarget objects and checks if any is currently in the viewfinder reticle.
        /// </summary>
        private void ScanForGhosts()
        {
            GhostTarget[] ghosts = FindObjectsByType<GhostTarget>(FindObjectsSortMode.None);
            GhostTarget bestTarget = null;
            float closestDistanceToCenter = float.MaxValue;

            foreach (var ghost in ghosts)
            {
                if (ghost == null) continue;

                // 1. Check if ghost is inside the viewport frustum
                Vector3 viewportPos = cam.WorldToViewportPoint(ghost.transform.position);

                // z > 0 means the object is in front of the camera
                if (viewportPos.z > 0f)
                {
                    // Check if ghost is inside the capture circle (reticle bounds)
                    Vector2 screenCenter = new Vector2(0.5f, 0.5f);
                    Vector2 viewportXY = new Vector2(viewportPos.x, viewportPos.y);
                    float distanceToCenter = Vector2.Distance(viewportXY, screenCenter);

                    if (distanceToCenter <= reticleRadius)
                    {
                        // 2. Line of sight check (Occlusion)
                        Vector3 dirToGhost = ghost.transform.position - transform.position;
                        float distToGhost = dirToGhost.magnitude;

                        if (distToGhost <= maxCaptureDistance)
                        {
                            RaycastHit hit;
                            int layerMask = ~LayerMask.GetMask("Player");
                            if (Physics.Raycast(transform.position, dirToGhost.normalized, out hit, distToGhost, layerMask))
                            {
                                if (hit.transform == ghost.transform || hit.transform.IsChildOf(ghost.transform))
                                {
                                    if (distanceToCenter < closestDistanceToCenter)
                                    {
                                        closestDistanceToCenter = distanceToCenter;
                                        bestTarget = ghost;
                                        targetViewportPosition = viewportPos;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            currentTargetInReticle = bestTarget;
        }

        /// <summary>
        /// Handles shooting input, damage calculations, and audio/visual triggers.
        /// </summary>
        private void HandleShooting()
        {
            if (inputReader.Attack && IsReady)
            {
                CapturePhoto();
            }
        }

        private void CapturePhoto()
        {
            // Start recharge cooldown
            cooldownTimer = shutterCooldown;
            wasReady = false;

            // Trigger visual flash event
            OnPhotoTaken?.Invoke();

            // Play normal shutter sound
            if (shutterSound != null)
            {
                audioSource.PlayOneShot(shutterSound);
            }

            // Check if this photo hit is a Fatal Frame timing
            bool isFatalFrame = IsFatalFrameActive;

            // Apply damage if we had a valid target locked in the viewfinder
            if (currentTargetInReticle != null)
            {
                // Calculate damage multipliers
                
                // 1. Distance multiplier (closer = more damage)
                float currentDistance = Vector3.Distance(transform.position, currentTargetInReticle.transform.position);
                float distanceFactor = Mathf.Clamp01(1f - (currentDistance / maxCaptureDistance));
                
                // 2. Centralization multiplier (closer to center = more damage)
                Vector2 viewportXY = new Vector2(targetViewportPosition.x, targetViewportPosition.y);
                float distFromCenter = Vector2.Distance(viewportXY, new Vector2(0.5f, 0.5f));
                float centerFactor = Mathf.Clamp01(1f - (distFromCenter / reticleRadius));

                // Formula: base * (30% to 100% based on distance) * (50% to 100% based on centering)
                float distanceMultiplier = Mathf.Lerp(0.3f, 1.0f, distanceFactor);
                float centerMultiplier = Mathf.Lerp(0.5f, 1.0f, centerFactor);
                float finalDamage = baseDamage * distanceMultiplier * centerMultiplier;

                // Deliver damage to ghost and let it know if it was a Fatal Frame hit
                currentTargetInReticle.TakeDamage(finalDamage, isFatalFrame);

                // Handle extra feedback on Fatal Frame hit
                if (isFatalFrame)
                {
                    OnFatalFrameHit?.Invoke();
                    
                    if (fatalFrameHitSound != null)
                    {
                        audioSource.PlayOneShot(fatalFrameHitSound);
                    }

                    // Start time-dilation (Hitstop effect)
                    StartCoroutine(HitStopRoutine(0.15f, 0.3f));
                    Debug.Log("FATAL FRAME CAPTURED!");
                }
            }

            Debug.Log("Photo captured!");
        }

        /// <summary>
        /// Coroutine to handle hitstop visual delay on time scale.
        /// </summary>
        private IEnumerator HitStopRoutine(float timeScaleAmount, float durationInRealtime)
        {
            float previousTimeScale = Time.timeScale;
            Time.timeScale = timeScaleAmount;

            yield return new WaitForSecondsRealtime(durationInRealtime);

            // Restore normal time scale
            Time.timeScale = 1.0f;
        }

        private void PlayRechargeSound()
        {
            if (rechargeCompleteSound != null)
            {
                audioSource.PlayOneShot(rechargeCompleteSound);
            }
        }
    }
}
