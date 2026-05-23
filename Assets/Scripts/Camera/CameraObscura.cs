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
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerUpgrades playerUpgrades;

        [Header("Camera Obscura Properties")]
        [SerializeField] private float baseDamage = 35f;
        [SerializeField] private float maxCaptureDistance = 15f;
        [SerializeField] private float shutterCooldown = 1.5f;
        [SerializeField] private float reticleRadius = 0.25f; // Boundary of capture circle in viewport units

        [Header("Audio Settings")]
        [SerializeField] private AudioClip shutterSound;
        [SerializeField] private AudioClip rechargeCompleteSound;
        [SerializeField] private AudioClip fatalFrameHitSound;

        private UnityEngine.Camera cam;
        private AudioSource audioSource;
        private float cooldownTimer = 0f;
        private bool wasReady = true;

        private GhostTarget currentTargetInReticle = null;
        private Vector3 targetViewportPosition = Vector3.zero;

        // Events for UI and visual feedbacks
        public event Action OnPhotoTaken;
        public event Action OnFatalFrameHit;
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

            if (playerInventory == null && playerController != null)
                playerInventory = playerController.GetComponent<PlayerInventory>();

            if (playerUpgrades == null && playerController != null)
                playerUpgrades = playerController.GetComponent<PlayerUpgrades>();
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
                HandleSpecialLens();
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

                // Skip invisible or hidden ghosts
                if (ghost.CurrentState == GhostState.Oculto || ghost.CurrentState == GhostState.Disappearing)
                    continue;

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

                        float activeMaxDistance = maxCaptureDistance;
                        if (playerUpgrades != null)
                        {
                            activeMaxDistance *= playerUpgrades.GetRangeMultiplier();
                        }

                        if (distToGhost <= activeMaxDistance)
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
            // Read active film type and consume it
            FilmType filmUsed = FilmType.Type14;
            if (playerInventory != null)
            {
                filmUsed = playerInventory.ActiveFilm;
                playerInventory.ConsumeFilm(); // Deduct one, auto-switch to infinite Type-14 if empty
            }

            // Start recharge cooldown (scaled by upgrade)
            float activeCooldown = shutterCooldown;
            if (playerUpgrades != null)
            {
                activeCooldown *= playerUpgrades.GetReloadMultiplier();
            }
            cooldownTimer = activeCooldown;
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
                float activeMaxDistance = maxCaptureDistance;
                if (playerUpgrades != null)
                {
                    activeMaxDistance *= playerUpgrades.GetRangeMultiplier();
                }
                float distanceFactor = Mathf.Clamp01(1f - (currentDistance / activeMaxDistance));
                
                // 2. Centralization multiplier (closer to center = more damage)
                Vector2 viewportXY = new Vector2(targetViewportPosition.x, targetViewportPosition.y);
                float distFromCenter = Vector2.Distance(viewportXY, new Vector2(0.5f, 0.5f));
                float centerFactor = Mathf.Clamp01(1f - (distFromCenter / reticleRadius));

                // 3. Film multiplier
                float filmMultiplier = 1f;
                if (filmUsed == FilmType.Type61) filmMultiplier = 1.7f;
                else if (filmUsed == FilmType.Type90) filmMultiplier = 2.8f;

                // Formula: base * (30% to 100% based on distance) * (50% to 100% based on centering) * filmMultiplier
                float distanceMultiplier = Mathf.Lerp(0.3f, 1.0f, distanceFactor);
                float centerMultiplier = Mathf.Lerp(0.5f, 1.0f, centerFactor);
                float finalDamage = baseDamage * distanceMultiplier * centerMultiplier * filmMultiplier;
                
                if (playerUpgrades != null)
                {
                    finalDamage *= playerUpgrades.GetPowerMultiplier();
                }

                // Deliver damage to ghost and let it know if it was a Fatal Frame hit
                currentTargetInReticle.TakeDamage(finalDamage, isFatalFrame);

                // --- SPIRIT POINTS CALCULATION ---
                int basePts = 100;
                int proximityPts = Mathf.RoundToInt(100f * distanceFactor);
                int centeringPts = Mathf.RoundToInt(150f * centerFactor);
                
                float filmPointsMultiplier = 1.0f;
                if (filmUsed == FilmType.Type61) filmPointsMultiplier = 1.5f;
                else if (filmUsed == FilmType.Type90) filmPointsMultiplier = 2.0f;

                int photoPoints = Mathf.RoundToInt((basePts + proximityPts + centeringPts) * filmPointsMultiplier);
                
                string bonusString = "";
                // Check for Shot Types and accumulate bonuses
                if (centerFactor > 0.85f)
                {
                    photoPoints += 200;
                    bonusString = "Core Shot!";
                }
                if (distanceFactor > 0.8f)
                {
                    photoPoints += 300;
                    bonusString = string.IsNullOrEmpty(bonusString) ? "Close Shot!" : "Core + Close Shot!";
                }
                if (isFatalFrame)
                {
                    photoPoints += 1000;
                    bonusString = "FATAL FRAME!";
                    
                    // Recover 1 spirit orb on successful Fatal Frame
                    if (playerUpgrades != null)
                    {
                        playerUpgrades.AddSpiritOrb();
                    }
                }

                // Add points to upgrades tracker
                if (playerUpgrades != null)
                {
                    playerUpgrades.AddPoints(photoPoints);
                }

                // Spawn points feedback on the ghost
                currentTargetInReticle.SpawnSpiritPointsText(photoPoints, bonusString);

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

        private void HandleSpecialLens()
        {
            if (inputReader.ActivateSpecialLens && currentTargetInReticle != null && playerUpgrades != null)
            {
                if (playerUpgrades.ConsumeSpiritOrb())
                {
                    currentTargetInReticle.Paralyze(3.0f);
                    // Play special sound effect (reuse hit sound for feedback)
                    if (fatalFrameHitSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(fatalFrameHitSound);
                    }
                    Debug.Log("SPECIAL LENS ACTIVATED: PARALYZE!");
                }
                else
                {
                    Debug.Log("No Spirit Orbs left to activate Special Lens!");
                }
            }
        }

        private IEnumerator HitStopRoutine(float timeScaleAmount, float durationInRealtime)
        {
            float previousTimeScale = Time.timeScale;
            Time.timeScale = timeScaleAmount;

            yield return new WaitForSecondsRealtime(durationInRealtime);

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
