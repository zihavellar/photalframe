using UnityEngine;
using PhotalFrame.Player;
using PhotalFrame.Input;
using PhotalFrame.UI;
using PhotalFrame.Ghost;

namespace PhotalFrame.World
{
    public class SavePoint : MonoBehaviour
    {
        [Header("Save Point Settings")]
        [SerializeField] private float activationRadius = 2.5f;

        [Header("References")]
        [SerializeField] private InputReader inputReader;
        [SerializeField] private SaveMenuUI saveMenuUI;

        private Transform playerTransform;
        private PlayerHealth playerHealth;
        private PlayerInventory playerInventory;
        private PlayerUpgrades playerUpgrades;
        private bool isPlayerInRange = false;

        private void Start()
        {
            if (inputReader == null)
                inputReader = FindAnyObjectByType<InputReader>();

            if (saveMenuUI == null)
                saveMenuUI = FindAnyObjectByType<SaveMenuUI>();

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerHealth = player.GetComponent<PlayerHealth>();
                playerInventory = player.GetComponent<PlayerInventory>();
                playerUpgrades = player.GetComponent<PlayerUpgrades>();
            }
        }

        private void Update()
        {
            if (playerTransform == null || inputReader == null) return;

            // Check distance
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            isPlayerInRange = distance <= activationRadius && !playerHealth.IsDead;

            if (isPlayerInRange && inputReader.Interact && !saveMenuUI.IsOpen)
            {
                TrySave();
            }
        }

        private void TrySave()
        {
            // Show confirmation pop-up instead of saving directly
            if (saveMenuUI != null)
            {
                saveMenuUI.OpenSavePrompt(this);
            }
        }

        public void PerformSave()
        {
            Save.SaveData data = new Save.SaveData();

            // Position and rotation
            if (playerTransform != null)
            {
                data.posX = playerTransform.position.x;
                data.posY = playerTransform.position.y;
                data.posZ = playerTransform.position.z;
                data.rotX = playerTransform.rotation.x;
                data.rotY = playerTransform.rotation.y;
                data.rotZ = playerTransform.rotation.z;
                data.rotW = playerTransform.rotation.w;
            }

            // Health
            if (playerHealth != null)
            {
                data.currentHealth = playerHealth.CurrentHealth;
            }

            // Upgrades
            if (playerUpgrades != null)
            {
                data.spiritPoints = playerUpgrades.SpiritPoints;
                data.spiritOrbs = playerUpgrades.SpiritOrbs;
                data.powerLevel = playerUpgrades.PowerLevel;
                data.reloadLevel = playerUpgrades.ReloadLevel;
                data.rangeLevel = playerUpgrades.RangeLevel;
            }

            // Inventory
            if (playerInventory != null)
            {
                data.filmType61Count = playerInventory.GetFilmCount(FilmType.Type61);
                data.filmType90Count = playerInventory.GetFilmCount(FilmType.Type90);
                data.herbalMedicineCount = playerInventory.HerbalMedicineCount;
                data.virginTapeCount = playerInventory.VirginTapeCount;

                if (playerInventory.CollectedItemIds != null)
                {
                    data.collectedItemIds = new System.Collections.Generic.List<string>(playerInventory.CollectedItemIds);
                }
            }

            // Ghost defeated state
            GameObject ghost = GameObject.Find("Ghost_Test");
            if (ghost != null)
            {
                GhostTarget ghostTarget = ghost.GetComponent<GhostTarget>();
                data.ghostTestDefeated = ghostTarget == null || ghostTarget.CurrentHealth <= 0 || !ghost.activeInHierarchy;
            }
            else
            {
                data.ghostTestDefeated = true;
            }

            Save.SaveSystem.Save(data);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position, activationRadius);
        }
    }
}
