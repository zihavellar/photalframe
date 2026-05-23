using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using PhotalFrame.Save;

namespace PhotalFrame.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float invincibilityDuration = 1.5f;

        private float currentHealth;
        private float invincibilityTimer = 0f;
        private bool isDead = false;

        public event Action<float> OnHealthChanged;
        public event Action OnDamageTaken;
        public event Action OnDeath;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;
        public bool IsInvincible => invincibilityTimer > 0f;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        private void Start()
        {
            if (SaveSystem.ShouldLoadOnStart)
            {
                LoadFromSave();
            }
        }

        private void Update()
        {
            if (isDead)
            {
                // Restart scene on R press when dead
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                {
                    RestartScene();
                }
                return;
            }

            // Invincibility countdown
            if (invincibilityTimer > 0f)
            {
                invincibilityTimer -= Time.deltaTime;
            }
        }

        /// <summary>
        /// Deals damage to the player if they are not dead or invincible.
        /// </summary>
        /// <param name="damage">Amount of damage to take.</param>
        public void TakeDamage(float damage)
        {
            if (isDead || IsInvincible) return;

            currentHealth -= damage;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            
            OnHealthChanged?.Invoke(currentHealth);
            OnDamageTaken?.Invoke();

            Debug.Log($"Player took {damage} damage! HP: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0f)
            {
                Die();
            }
            else
            {
                // Trigger invincibility frame
                invincibilityTimer = invincibilityDuration;
                StartCoroutine(DamageFlashRoutine());
            }
        }

        /// <summary>
        /// Visual damage indicator (flashes player renderer red if visible).
        /// </summary>
        private System.Collections.IEnumerator DamageFlashRoutine()
        {
            Renderer playerRenderer = GetComponent<Renderer>();
            if (playerRenderer == null) yield break;

            Color originalColor = playerRenderer.material.HasProperty("_BaseColor") ? 
                                  playerRenderer.material.GetColor("_BaseColor") : Color.white;

            float elapsed = 0f;
            bool isFlashColor = false;

            while (elapsed < invincibilityDuration)
            {
                elapsed += 0.15f;
                isFlashColor = !isFlashColor;

                if (playerRenderer.material.HasProperty("_BaseColor"))
                {
                    playerRenderer.material.SetColor("_BaseColor", isFlashColor ? Color.red : originalColor);
                }

                yield return new WaitForSeconds(0.15f);
            }

            // Restore color
            if (playerRenderer.material.HasProperty("_BaseColor"))
            {
                playerRenderer.material.SetColor("_BaseColor", originalColor);
            }
        }

        /// <summary>
        /// Heals the player by a specific amount.
        /// </summary>
        public void Heal(float amount)
        {
            if (isDead) return;

            currentHealth += amount;
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            
            OnHealthChanged?.Invoke(currentHealth);
            Debug.Log($"Player healed by {amount}! HP: {currentHealth}/{maxHealth}");
        }

        private void Die()
        {
            isDead = true;
            OnDeath?.Invoke();
            Debug.Log("PLAYER DEFEATED - GAME OVER");

            // Pause the game time
            Time.timeScale = 0f;
        }

        private void RestartScene()
        {
            Time.timeScale = 1.0f;
            if (SaveSystem.SaveExists())
            {
                SaveSystem.ShouldLoadOnStart = true;
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    
        private void LoadFromSave()
        {
            SaveData data = SaveSystem.Load();
            if (data == null) return;

            // Restore position and rotation
            Transform playerTrans = transform;
            playerTrans.position = new Vector3(data.posX, data.posY, data.posZ);
            playerTrans.rotation = new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW);

            // Restore health
            currentHealth = data.currentHealth;
            if (currentHealth <= 0f) currentHealth = maxHealth;
            isDead = false;
            OnHealthChanged?.Invoke(currentHealth);

            // Restore upgrades
            PlayerUpgrades upgrades = GetComponent<PlayerUpgrades>();
            if (upgrades != null)
            {
                while (upgrades.PowerLevel < data.powerLevel) upgrades.TryUpgradePower();
                while (upgrades.ReloadLevel < data.reloadLevel) upgrades.TryUpgradeReload();
                while (upgrades.RangeLevel < data.rangeLevel) upgrades.TryUpgradeRange();
                // Manually set points and orbs via reflection-like approach
                var pointsField = typeof(PlayerUpgrades).GetField("spiritPoints", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (pointsField != null) pointsField.SetValue(upgrades, data.spiritPoints);
                var orbsField = typeof(PlayerUpgrades).GetField("spiritOrbs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (orbsField != null) orbsField.SetValue(upgrades, data.spiritOrbs);
            }

            // Restore inventory
            PlayerInventory inventory = GetComponent<PlayerInventory>();
            if (inventory != null && data.collectedItemIds != null)
            {
                inventory.SetVirginTapeState(data.virginTapeCount);
                inventory.SetCollectedItems(data.collectedItemIds);
            }

            // Handle ghost defeated state
            GameObject ghost = GameObject.Find("Ghost_Test");
            if (ghost != null && data.ghostTestDefeated)
            {
                UnityEngine.Object.Destroy(ghost);
            }

            // Reset the flag so it only loads once
            SaveSystem.ShouldLoadOnStart = false;

            // Restore time scale (in case it was paused)
            Time.timeScale = 1f;

            Debug.Log("Game loaded from save file. Position restored." );
        }
    }
}
