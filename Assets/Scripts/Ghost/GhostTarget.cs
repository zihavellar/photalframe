using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PhotalFrame.Ghost
{
    public enum GhostState
    {
        Oculto,       // Hidden: invisible and untouchable
        Aparicao,     // Materializing: fading in
        Chasing,      // Active chase
        Disappearing, // Dematerializing: fading out to flank the player
        WindingUp,    // Pre-attack warning
        Lunging,      // Attack dash (Fatal Frame window)
        Cooldown      // Recoiling from damage or hit
    }

    public class GhostTarget : MonoBehaviour
    {
        [Header("Ghost Properties")]
        [SerializeField] private bool isHostile = true;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;

        [Header("Movement Settings")]
        [SerializeField] private float chaseSpeed = 1.3f;
        [SerializeField] private float lungeSpeed = 4.2f;
        [SerializeField] private float disappearChaseSpeed = 2.5f; // Moves faster when invisible to flank
        [SerializeField] private float attackRange = 3.0f;
        [SerializeField] private float hoverAmplitude = 0.18f;
        [SerializeField] private float hoverFrequency = 2.0f;
        [SerializeField] private float hoverBaseHeight = 1.0f;

        [Header("Timing Settings")]
        [SerializeField] private float appearDuration = 1.5f;
        [SerializeField] private float disappearDuration = 2.2f;
        [SerializeField] private float chaseDurationBeforeDisappearing = 4.5f;
        [SerializeField] private float windUpDuration = 0.5f;
        [SerializeField] private float lungeDuration = 0.5f; // Fatal Frame window
        [SerializeField] private float cooldownDuration = 1.8f;

        [Header("Visual Settings")]
        [SerializeField] private float targetAlpha = 0.55f; // Translucent blue target opacity
        [SerializeField] private float fadeSpeed = 1.0f;
        [SerializeField] private float flashDuration = 0.15f;
        [SerializeField] private Color standardFlashColor = Color.white;
        [SerializeField] private Color fatalFlashColor = new Color(1f, 0.1f, 0.1f);

        private float health;
        private Renderer[] childRenderers;
        private Collider ghostCollider;
        private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
        private Material standardFlashMaterial;
        private Material fatalFlashMaterial;
        private bool isFlashing = false;

        // State Machine variables
        private GhostState currentState = GhostState.Aparicao; // Start by materializing
        private float stateTimer = 0f;
        private Transform playerTransform;
        private bool canFatalFrame = false;
        private float currentVisualAlpha = 0f;
        private Vector3 flankTargetPosition;
        private bool hasChosenFlankTarget = false;

        public bool IsHostile => isHostile;
        public float CurrentHealth => health;
        public float MaxHealth => maxHealth;
        public GhostState CurrentState => currentState;
        public bool CanFatalFrame => canFatalFrame && currentState == GhostState.Lunging;

        private void Awake()
        {
            health = maxHealth;
            childRenderers = GetComponentsInChildren<Renderer>();
            ghostCollider = GetComponent<Collider>();

            // Cache original materials
            foreach (var renderer in childRenderers)
            {
                originalMaterials[renderer] = renderer.sharedMaterials;
            }

            // Create default unlit materials for flash effects
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");
            
            standardFlashMaterial = new Material(unlitShader);
            standardFlashMaterial.color = standardFlashColor;

            fatalFlashMaterial = new Material(unlitShader);
            fatalFlashMaterial.color = fatalFlashColor;
        }

        private void Start()
        {
            GameObject playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                playerTransform = playerGo.transform;
            }
            else
            {
                Debug.LogWarning("GhostTarget: Player object not found. Make sure the Capsule has the tag 'Player'.");
            }

            // Start invisible
            currentVisualAlpha = 0f;
            SetGhostAlpha(0f);
            if (ghostCollider != null) ghostCollider.enabled = false;
        }

        private void Update()
        {
            if (playerTransform == null || !isHostile || health <= 0) return;

            stateTimer += Time.deltaTime;

            // 1. Process State Actions
            switch (currentState)
            {
                case GhostState.Oculto:
                    ProcessOculto();
                    break;

                case GhostState.Aparicao:
                    ProcessAparicao();
                    break;

                case GhostState.Chasing:
                    ProcessChasing();
                    break;

                case GhostState.Disappearing:
                    ProcessDisappearing();
                    break;

                case GhostState.WindingUp:
                    ProcessWindingUp();
                    break;

                case GhostState.Lunging:
                    ProcessLunging();
                    break;

                case GhostState.Cooldown:
                    ProcessCooldown();
                    break;
            }

            // 2. Smoothly Update Material Alpha
            UpdateAlphaInterpolation();
        }

        private void ProcessOculto()
        {
            // Hover in place invisibly
            HoverInPlace();

            // Wait for player to be within 7 meters to trigger appearance
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= 7f)
            {
                TransitionToState(GhostState.Aparicao);
            }
        }

        private void ProcessAparicao()
        {
            // Stay still or slide slowly while materializing
            HoverInPlace();

            // Face the player
            LookAtPlayer(4f);

            if (stateTimer >= appearDuration)
            {
                TransitionToState(GhostState.Chasing);
            }
        }

        private void ProcessChasing()
        {
            Vector3 dirToPlayer = playerTransform.position - transform.position;
            dirToPlayer.y = 0;
            float distance = dirToPlayer.magnitude;

            // Face the player
            LookAtPlayer(5f);

            // Move towards player
            transform.position += dirToPlayer.normalized * chaseSpeed * Time.deltaTime;

            // Hover height
            ApplyHoverHeight();

            // Check if player is in attack range
            if (distance <= attackRange)
            {
                TransitionToState(GhostState.WindingUp);
            }
            // If chased for too long, disappear to flank the player (erratic behavior Option A)
            else if (stateTimer >= chaseDurationBeforeDisappearing)
            {
                TransitionToState(GhostState.Disappearing);
            }
        }

        private void ProcessDisappearing()
        {
            // The ghost is invisible now. It flanking-moves towards left/right of player.
            if (!hasChosenFlankTarget)
            {
                hasChosenFlankTarget = true;
                
                // Choose a random side (1 = right, -1 = left)
                float side = UnityEngine.Random.value > 0.5f ? 1f : -1f;
                
                // Target is 4m to the side and 2m forward relative to the player
                flankTargetPosition = playerTransform.position + 
                                      playerTransform.right * (side * 4f) + 
                                      playerTransform.forward * 2f;
                
                // Ensure it stays at a reasonable height
                flankTargetPosition.y = playerTransform.position.y + hoverBaseHeight;
                
                Debug.Log($"Ghost disappearing to flank player on the {(side > 0 ? "RIGHT" : "LEFT")}");
            }

            // Move towards the flank target position at faster speed
            Vector3 dirToTarget = flankTargetPosition - transform.position;
            float distance = dirToTarget.magnitude;

            if (distance > 0.2f)
            {
                transform.position += dirToTarget.normalized * disappearChaseSpeed * Time.deltaTime;
                LookAtPlayer(6f);
            }
            else
            {
                HoverInPlace();
            }

            // End invisible flanking phase and materialize
            if (stateTimer >= disappearDuration)
            {
                TransitionToState(GhostState.Aparicao);
            }
        }

        private void ProcessWindingUp()
        {
            LookAtPlayer(10f);
            HoverInPlace();

            if (stateTimer >= windUpDuration)
            {
                TransitionToState(GhostState.Lunging);
            }
        }

        private void ProcessLunging()
        {
            Vector3 dirToPlayer = playerTransform.position - transform.position;
            dirToPlayer.y = 0;
            
            if (dirToPlayer.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(dirToPlayer);
            }

            // Lunge speed charging
            transform.position += dirToPlayer.normalized * lungeSpeed * Time.deltaTime;

            // Slight vibration height
            Vector3 pos = transform.position;
            pos.y = hoverBaseHeight + Mathf.Sin(Time.time * hoverFrequency * 2.5f) * (hoverAmplitude * 0.5f);
            transform.position = pos;

            if (stateTimer >= lungeDuration)
            {
                float finalDistance = Vector3.Distance(transform.position, playerTransform.position);
                if (finalDistance <= 1.6f)
                {
                    Debug.Log("PLAYER HIT BY GHOST!");
                }
                TransitionToState(GhostState.Cooldown);
            }
        }

        private void ProcessCooldown()
        {
            // Back away slowly from player
            Vector3 dirFromPlayer = transform.position - playerTransform.position;
            dirFromPlayer.y = 0;

            if (dirFromPlayer.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(-dirFromPlayer);
            }

            transform.position += dirFromPlayer.normalized * (chaseSpeed * 0.7f) * Time.deltaTime;
            ApplyHoverHeight();

            if (stateTimer >= cooldownDuration)
            {
                // After cooldown, transition to Hidden state (Oculto) or Chasing
                // Let's go to Chasing for dynamic loop, or Oculto for surprises
                TransitionToState(GhostState.Chasing);
            }
        }

        private void HoverInPlace()
        {
            Vector3 pos = transform.position;
            pos.y = hoverBaseHeight + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
            transform.position = pos;
        }

        private void ApplyHoverHeight()
        {
            Vector3 pos = transform.position;
            pos.y = hoverBaseHeight + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
            transform.position = pos;
        }

        private void LookAtPlayer(float speed)
        {
            Vector3 dir = playerTransform.position - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * speed);
            }
        }

        private void TransitionToState(GhostState newState)
        {
            currentState = newState;
            stateTimer = 0f;

            // Toggle collider to prevent shooting invisible ghosts
            if (ghostCollider != null)
            {
                ghostCollider.enabled = (currentState != GhostState.Oculto && currentState != GhostState.Disappearing);
            }

            if (currentState == GhostState.Lunging)
            {
                canFatalFrame = true;
            }
            else
            {
                canFatalFrame = false;
            }

            if (currentState != GhostState.Disappearing)
            {
                hasChosenFlankTarget = false;
            }

            Debug.Log($"Ghost state: {currentState}");
        }

        private void UpdateAlphaInterpolation()
        {
            float targetVisualAlpha = targetAlpha;

            if (currentState == GhostState.Oculto || currentState == GhostState.Disappearing)
            {
                targetVisualAlpha = 0f;
            }
            else if (currentState == GhostState.Cooldown)
            {
                targetVisualAlpha = 0.2f; // Fade out partially during recoil
            }
            else if (currentState == GhostState.Aparicao)
            {
                // Smooth rise from 0 to targetAlpha
                targetVisualAlpha = targetAlpha;
            }

            if (Mathf.Abs(currentVisualAlpha - targetVisualAlpha) > 0.01f)
            {
                currentVisualAlpha = Mathf.MoveTowards(currentVisualAlpha, targetVisualAlpha, fadeSpeed * Time.deltaTime);
                SetGhostAlpha(currentVisualAlpha);
            }
        }

        private void SetGhostAlpha(float alpha)
        {
            foreach (var r in childRenderers)
            {
                if (r == null) continue;
                
                // Update BaseColor alpha
                if (r.material.HasProperty("_BaseColor"))
                {
                    Color baseCol = r.material.GetColor("_BaseColor");
                    baseCol.a = alpha;
                    r.material.SetColor("_BaseColor", baseCol);
                }
                
                // Scale Emission color
                if (r.material.HasProperty("_EmissionColor"))
                {
                    if (alpha <= 0.02f)
                    {
                        r.material.SetColor("_EmissionColor", Color.clear);
                    }
                    else
                    {
                        // Lerp emission strength based on visibility
                        Color emissionBase = new Color(0.08f, 0.25f, 0.5f);
                        r.material.SetColor("_EmissionColor", emissionBase * (alpha / targetAlpha));
                    }
                }
            }
        }

        public bool TakeDamage(float damage, bool isFatalFrame)
        {
            if (health <= 0) return false;

            float actualDamage = damage;
            if (isFatalFrame)
            {
                actualDamage = damage * 5f;
            }

            health -= actualDamage;
            Debug.Log($"{(isFatalFrame ? "FATAL FRAME! " : "")}{gameObject.name} took {actualDamage:F1} damage. HP: {health}/{maxHealth}");

            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(FlashRoutine(isFatalFrame));
                SpawnDamageText(actualDamage, isFatalFrame);
            }

            // Always recoil and fade out/recoil on Fatal Frame or standard hit in charge
            if (isFatalFrame || currentState == GhostState.Lunging || currentState == GhostState.WindingUp)
            {
                TransitionToState(GhostState.Cooldown);
                
                if (playerTransform != null)
                {
                    Vector3 pushDir = (transform.position - playerTransform.position);
                    pushDir.y = 0;
                    float pushDistance = isFatalFrame ? 4.5f : 2.5f;
                    transform.position += pushDir.normalized * pushDistance;
                }
            }

            if (health <= 0)
            {
                Defeat();
                return true;
            }

            return false;
        }

        private IEnumerator FlashRoutine(bool isFatalFrame)
        {
            if (isFlashing) yield break;
            isFlashing = true;

            Material activeFlashMaterial = isFatalFrame ? fatalFlashMaterial : standardFlashMaterial;

            foreach (var r in childRenderers)
            {
                if (r == null) continue;
                Material[] flashMats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < flashMats.Length; i++)
                {
                    flashMats[i] = activeFlashMaterial;
                }
                r.sharedMaterials = flashMats;
            }

            yield return new WaitForSeconds(flashDuration);

            foreach (var r in childRenderers)
            {
                if (r == null) continue;
                if (originalMaterials.ContainsKey(r))
                {
                    r.sharedMaterials = originalMaterials[r];
                }
            }

            isFlashing = false;
        }

        private void SpawnDamageText(float damage, bool isFatalFrame)
        {
            GameObject textGo = new GameObject("DamageText");
            textGo.transform.position = transform.position + Vector3.up * 1.3f;
            textGo.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);

            TextMesh textMesh = textGo.AddComponent<TextMesh>();
            
            if (isFatalFrame)
            {
                textMesh.text = $"FATAL FRAME!\n-{damage:F0}";
                textMesh.color = new Color(1.0f, 0.82f, 0.0f);
                textMesh.fontSize = 54;
            }
            else
            {
                textMesh.text = $"-{damage:F0}";
                textMesh.color = isHostile ? new Color(1f, 0.2f, 0.2f) : new Color(0.9f, 0.9f, 0.9f);
                textMesh.fontSize = 44;
            }

            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;

            StartCoroutine(DamageTextRoutine(textGo, textMesh, isFatalFrame));
        }

        private IEnumerator DamageTextRoutine(GameObject obj, TextMesh mesh, bool isFatalFrame)
        {
            float duration = isFatalFrame ? 1.4f : 0.9f;
            float elapsed = 0f;
            Vector3 startPos = obj.transform.position;
            Vector3 endPos = startPos + Vector3.up * 0.9f + UnityEngine.Random.insideUnitSphere * 0.12f;
            endPos.y = startPos.y + 0.9f;

            UnityEngine.Camera mainCam = UnityEngine.Camera.main;

            while (elapsed < duration)
            {
                if (obj == null) yield break;

                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                obj.transform.position = Vector3.Lerp(startPos, endPos, t);

                if (mainCam != null)
                {
                    obj.transform.rotation = Quaternion.LookRotation(obj.transform.position - mainCam.transform.position);
                }

                Color c = mesh.color;
                c.a = 1f - t;
                mesh.color = c;

                yield return null;
            }

            Destroy(obj);
        }

        private void Defeat()
        {
            Debug.Log($"{gameObject.name} defeated!");

            GameObject deathParticles = new GameObject("DeathParticles");
            deathParticles.transform.position = transform.position;
            
            ParticleSystem ps = deathParticles.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = new Color(0.4f, 0.75f, 1f, 0.8f);
            main.startSize = 0.22f;
            main.startSpeed = 1.8f;
            main.duration = 1.2f;
            main.loop = false;
            
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.burstCount = 1;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 50));

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            ps.Play();

            Destroy(deathParticles, 2.5f);
            Destroy(gameObject, 0.05f);
        }
    }
}
