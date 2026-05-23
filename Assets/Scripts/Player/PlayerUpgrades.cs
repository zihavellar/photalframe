using UnityEngine;
using System;

namespace PhotalFrame.Player
{
    public class PlayerUpgrades : MonoBehaviour
    {
        [Header("Spirit Resources")]
        [SerializeField] private int spiritPoints = 0;
        [SerializeField] private int spiritOrbs = 1;
        [SerializeField] private int maxSpiritOrbs = 3;

        [Header("Upgrade Levels (1-4)")]
        [SerializeField] private int powerLevel = 1;
        [SerializeField] private int reloadLevel = 1;
        [SerializeField] private int rangeLevel = 1;

        public event Action OnUpgradesChanged;

        public int SpiritPoints => spiritPoints;
        public int SpiritOrbs => spiritOrbs;
        public int MaxSpiritOrbs => maxSpiritOrbs;
        public int PowerLevel => powerLevel;
        public int ReloadLevel => reloadLevel;
        public int RangeLevel => rangeLevel;

        /// <summary>
        /// Gets the points cost for the next level. Returns -1 if already at max.
        /// </summary>
        public int GetUpgradeCost(int currentLevel)
        {
            switch (currentLevel)
            {
                case 1: return 1500;
                case 2: return 3500;
                case 3: return 7000;
                default: return -1; // Max level reached
            }
        }

        public void AddPoints(int points)
        {
            spiritPoints += points;
            OnUpgradesChanged?.Invoke();
            Debug.Log($"+{points} Spirit Points! Total: {spiritPoints}");
        }

        public bool TryUpgradePower()
        {
            if (powerLevel >= 4) return false;
            int cost = GetUpgradeCost(powerLevel);
            if (spiritPoints >= cost)
            {
                spiritPoints -= cost;
                powerLevel++;
                OnUpgradesChanged?.Invoke();
                Debug.Log($"Camera Power upgraded to Level {powerLevel}!");
                return true;
            }
            return false;
        }

        public bool TryUpgradeReload()
        {
            if (reloadLevel >= 4) return false;
            int cost = GetUpgradeCost(reloadLevel);
            if (spiritPoints >= cost)
            {
                spiritPoints -= cost;
                reloadLevel++;
                OnUpgradesChanged?.Invoke();
                Debug.Log($"Camera Reload Speed upgraded to Level {reloadLevel}!");
                return true;
            }
            return false;
        }

        public bool TryUpgradeRange()
        {
            if (rangeLevel >= 4) return false;
            int cost = GetUpgradeCost(rangeLevel);
            if (spiritPoints >= cost)
            {
                spiritPoints -= cost;
                rangeLevel++;
                OnUpgradesChanged?.Invoke();
                Debug.Log($"Camera Range upgraded to Level {rangeLevel}!");
                return true;
            }
            return false;
        }

        public void AddSpiritOrb()
        {
            if (spiritOrbs < maxSpiritOrbs)
            {
                spiritOrbs++;
                OnUpgradesChanged?.Invoke();
                Debug.Log($"Spirit Orb restored! Orbs: {spiritOrbs}/{maxSpiritOrbs}");
            }
        }

        public bool ConsumeSpiritOrb()
        {
            if (spiritOrbs > 0)
            {
                spiritOrbs--;
                OnUpgradesChanged?.Invoke();
                Debug.Log($"Consumed Spirit Orb! Orbs: {spiritOrbs}/{maxSpiritOrbs}");
                return true;
            }
            return false;
        }

        // Multipliers applied to Camera Obscura
        public float GetPowerMultiplier()
        {
            switch (powerLevel)
            {
                case 1: return 1.0f;
                case 2: return 1.2f;
                case 3: return 1.4f;
                case 4: return 1.6f;
                default: return 1.0f;
            }
        }

        public float GetReloadMultiplier()
        {
            switch (reloadLevel)
            {
                case 1: return 1.0f;
                case 2: return 0.85f;
                case 3: return 0.7f;
                case 4: return 0.55f; // Almost twice as fast
                default: return 1.0f;
            }
        }

        public float GetRangeMultiplier()
        {
            switch (rangeLevel)
            {
                case 1: return 1.0f;
                case 2: return 1.15f;
                case 3: return 1.3f;
                case 4: return 1.45f;
                default: return 1.0f;
            }
        }
    }
}
